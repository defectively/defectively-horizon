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

        public async Task<bool> Connect(string host, int port, string id, byte[] hash) {
            wrapper = new ClientWrapper {
                Id = id,
                PasswordHash = hash
            };

            var success = await wrapper.Connect(host, port);

            if (success) {
                Show();
            }

            return success;
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            wrapper.Client.Disconnect();
            File.Create(Path.Combine(ClientWrapper.SessionFolderPath, "EXPIRED"));
        }
    }
}
