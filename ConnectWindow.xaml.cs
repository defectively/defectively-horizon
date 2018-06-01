using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Defectively.Core.Communication;
using Defectively.Core.Cryptography;
using Defectively.Core.Models;
using Defectively.Core.Networking;

namespace Defectively.Horizon
{
    public partial class ConnectWindow : Window
    {
        private MainWindow mainWindow;
        private bool Connected;

        public ConnectWindow() {
            InitializeComponent();

            foreach (var d in Directory.EnumerateDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions"), "*")) {
                if (File.Exists(Path.Combine(d, "EXPIRED"))) {
                    Directory.Delete(d, true);
                }
            }
        }

        private void OnAddressPreviewKeyUp(object sender, KeyEventArgs e) {
            if (Regex.IsMatch(AddressTextBox.Text, @"(([a-zA-Z0-9\-\.]{1,255})|(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}))(:\d{1,5})")) {
                if (int.TryParse(AddressTextBox.Text.Split(':')[1], out var i) && i <= 65535) {
                    PrimaryButton.IsEnabled = true;
                } else {
                    PrimaryButton.IsEnabled = false;
                }
            } else {
                PrimaryButton.IsEnabled = false;
            }
        }

        private async void OnCredentialsPreviewKeyUp(object sender, KeyEventArgs e) {
            PrimaryButton.IsEnabled = !string.IsNullOrEmpty(IdTextBox.Text) && !string.IsNullOrEmpty(PasswordBox.Password);

            if (e.Key == Key.Enter && PrimaryButton.IsEnabled) {
                await ConnectAndLogin();
            }
        }

        private async void OnPrimaryClick(object sender, RoutedEventArgs e) {
            if (!Connected) {
                await Connect();
            } else {
                await ConnectAndLogin();
            }
        }

        private async void OnSecondaryClick(object sender, RoutedEventArgs e) {
            if (!Connected) {
                var window = new ScanWindow();
                if (window.ShowDialog() == true) {
                    AddressTextBox.Text = window.SelectedServerAddress;

                    await Connect();
                }
            }
        }

        private async Task Connect() {
            var client = new Client { CryptographicData = CryptographyProvider.Instance.GetRandomData() };

            try {
                await client.ConnectAsync(AddressTextBox.Text.Split(':')[0], int.Parse(AddressTextBox.Text.Split(':')[1]), true);
            } catch {
                MessageBox.Show(this, "An error occured while trying to connect to the given server.", "Defectively Horizon", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await client.WriteAsync(new Package(PackageType.Information));

            var package = await client.ReadAsync<Package>();

            if (package.Type == PackageType.Information) {
                var serverInfo = package.GetContent<ServerInformation>();

                // TODO: Use Server Information

                client.Disconnect();

                if (serverInfo.AcceptsClients) {
                    Connected = true;
                    AddressTextBox.IsEnabled = PrimaryButton.IsEnabled = SecondaryButton.IsEnabled = false; // TODO: Enable Secondary if Register is available
                    IdTextBox.IsEnabled = PasswordBox.IsEnabled = true;
                    PrimaryButton.Content = "Login";
                    SecondaryButton.Content = "Register";
                    IdTextBox.Focus();
                }
            }
        }

        private async Task ConnectAndLogin() {
            mainWindow = new MainWindow();
            if (await mainWindow.Connect(AddressTextBox.Text.Split(':')[0], int.Parse(AddressTextBox.Text.Split(':')[1]), IdTextBox.Text, CryptographyProvider.Instance.SHA512ComputeHash(PasswordBox.Password), LoginCallback)) {
                // TODO: Connection failed
            }
        }

        private void LoginCallback(bool success, string details) {
            MessageBox.Show($"Login successful: {success}\nDetails: {details}");

            // TODO: Show Nice Messages

            if (success) {
                mainWindow.Show();
                Close();
            }
        }
    }
}