using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Defectively.Core.Common;
using Defectively.Core.Communication;
using Defectively.Core.Extensibility;
using Defectively.Core.Extensibility.Events;
using Defectively.Core.Models;
using Defectively.Core.Networking;
using Defectively.Core.Storage;

namespace Defectively.Horizon
{
    public class ServerWrapper : IServerWrapper
    {
        public List<Account> Accounts { get; set; }
        public List<Team> Teams { get; set; }
        public List<Channel> Channels { get; set; }

        public Server Server { get; set; }

        public async Task Initialize() {
            ComponentPool.ServerWrapper = this;

            DataStorage.Instance.Directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            DataStorage.Instance.Load();

            Server = new Server(42000);
            Server.Connected += OnConnected;
            Server.Disconnected += OnDisconnected;

            await Server.StartAsync(true);
        }

        private async void OnConnected(ConnectableBase sender, ConnectedEventArgs e) {
            EventManager.InvokeEvent(EventType.Connected, new ConnectedEvent { Client = e.Client }, this);

            Package package = null;
            string id = null;
            byte[] password = null;

            try {
                package = await e.Client.ReadAsync<Package>();
                id = package.GetContent<string>(0);
                password = package.GetContent<byte[]>(1);
            } catch { }

            if (package == null || package.Type != PackageType.Authentication || string.IsNullOrEmpty(id) || password?.Length == 0) {
                await e.Client.WriteAsync(new Package(PackageType.Error, "no auth data"));
                e.Client.Disconnect();
                return;
            }
            
            var account = DataStorage.Instance.Accounts.Find(a => a.Id == id);

            if (account == null) {
                await e.Client.WriteAsync(new Package(PackageType.Error, "account unknown"));
                e.Client.Disconnect();
                return;
            }

            if (!account.Password.SequenceEqual(password)) {
                await e.Client.WriteAsync(new Package(PackageType.Error, "password invalid"));
                e.Client.Disconnect();
                return;
            }

            e.Client.Account = account;
            await e.Client.WriteAsync(new Package(PackageType.Success, account));

            EventManager.InvokeEvent(EventType.Authenticated, new AuthenticatedEvent { Account = account }, this);

            await Listen(e.Client);
        }

        private async Task Listen(Client client) {
            try {
                while (client.IsAlive) {
                    var package = await client.ReadAsync<Package>();

                    var args = new PackageReceivedEvent { EndpointId = client.Account.Id, Package = package };
                    EventManager.InvokeEvent(EventType.PackageReceived, args, this);

                    if (args.SkipLegacyHandling)
                        continue;
                    
                    switch (package.Type) {
                    case PackageType.Debug:
                        Debug.WriteLine("L: " + package.GetContent<string>(0));
                        break;
                    }
                }
            } catch (ClientDisconnectedException ex) { }
        }

        private void OnDisconnected(ConnectableBase sender, DisconnectedEventArgs e) {

        }
        
        public async Task SendPackageTo(Package package, string accountId) {
            await Server.Clients.Find(c => c.Account.Id == accountId).WriteAsync(package);
        }

        public async Task SendPackageToAccounts(Package package, params string[] accountIds) {
            Server.Clients.FindAll(c => accountIds.Contains(c.Account.Id)).ForEach(async c => await c.WriteAsync(package));
        }

        public async Task SendPackageToAccountsWithFlag(Package package, string aresFlag) {
            Server.Clients.FindAll(c => c.Account.AresFlags.HasValue(aresFlag, Teams.Find(t => t.Id == c.Account.TeamId).AresFlags)).ForEach(async c => await c.WriteAsync(package));
        }

        public async Task SendPackageToTeams(Package package, params string[] teamIds) {
        }

        public async Task SendPackageToChannels(Package package, params string[] channelIds) {
        }

        public async Task SendPackageToAll(Package package) {
        }
    }
}
