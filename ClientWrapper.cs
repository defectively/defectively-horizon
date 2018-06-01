using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Defectively.Core.Common;
using Defectively.Core.Communication;
using Defectively.Core.Cryptography;
using Defectively.Core.Extensibility;
using Defectively.Core.Extensibility.Events;
using Defectively.Core.Models;
using Defectively.Core.Networking;

namespace Defectively.Horizon
{
    public class ClientWrapper : IClientWrapper
    {
        public string Id { get; set; }
        public byte[] PasswordHash { get; set; }
        public Client Client { get; set; }
        public static string SessionFolderPath { get; set; }

        private Action<bool, string> authCallback;

        public async Task<bool> Connect(string host, int port, Action<bool, string> callback) {
            ComponentPool.ClientWrapper = this;

            Client = new Client { CryptographicData = CryptographyProvider.Instance.GetRandomData() };
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;

            authCallback = callback;

            try {
                await Client.ConnectAsync(host, port, true);
                return true;
            } catch {
                return false;
            }
        }

        private async void OnConnected(ConnectableBase sender, ConnectedEventArgs e) {
            await Client.WriteAsync(new Package(PackageType.Authentication, Id, PasswordHash));

            var package = await Client.ReadAsync<Package>();

            switch (package.Type) {
            case PackageType.Error:
                Client.Disconnect();


                break;
            case PackageType.Success:
                Client.Account = package.GetContent<Account>();

                SessionFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions", Client.SessionId.ToString());
                Directory.CreateDirectory(SessionFolderPath);

                break;
            }

            authCallback.Invoke(package.Type == PackageType.Success, package.Type == PackageType.Success ? Client.Account.Id : package.GetContent<string>());

            await Listen();
        }

        private async Task Listen() {
            try {
                while (Client.IsAlive) {
                    var package = await Client.ReadAsync<Package>();
                    var @event = new PackageReceivedEvent { Package = package };

                    await EventService.InvokeEvent(EventType.PackageReceived, @event, this);

                    if (@event.SkipLegacyHandling)
                        continue;

                    switch (package.Type) {
                    case PackageType.Assembly:
                        var extPath = Path.Combine(SessionFolderPath, package.GetContent<string>(1));
                        File.WriteAllBytes(extPath, package.GetContent<byte[]>(0));

                        ExtensionManager.InitializeExtension(extPath, false);

                        break;
                    case PackageType.ExternalEvent:
                        await EventService.InvokeEvent(EventType.External, package.GetContent<ExternalEvent>(0), this);
                        break;
                    }
                }
            } catch {
                MessageBox.Show("Disconnected");
            }
        }

        private void OnDisconnected(ConnectableBase sender, DisconnectedEventArgs e) {
            
        }

        public async Task SendExternalEventToServer(string name, params object[] @params) {
            await Client.WriteAsync(new ExternalEvent(name, @params) { EndpointId = Client.Account.Id }.ToPackage());
        }

        public async Task SendPackageToServer(Package package) {
            await Client.WriteAsync(package);
        }

        public void ShowWindow(IExtensionWindow window) {
            window.Wrapper = this;
            ((Window) window).Show();
        }

        public bool? ShowWindowDialog(IExtensionWindow window) {
            window.Wrapper = this;
            return ((Window) window).ShowDialog();
        }
    }
}
