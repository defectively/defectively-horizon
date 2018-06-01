using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Defectively.Horizon
{
    public partial class MainWindow : Window
    {
        private ClientWrapper wrapper;

        public MainWindow() {
            InitializeComponent();
        }

        public async Task<bool> Connect(string host, int port, string id, byte[] hash, Action<bool, string> callback) {
            wrapper = new ClientWrapper {
                Id = id,
                PasswordHash = hash
            };

            return await wrapper.Connect(host, port, callback);
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            wrapper.Client.Disconnect();
            File.Create(Path.Combine(ClientWrapper.SessionFolderPath, "EXPIRED"));
            Environment.Exit(0);
        }
    }
}
