using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Defectively.Core.Networking.Udp;

namespace Defectively.Horizon
{
    public partial class ScanWindow : Window
    {
        public string SelectedServerAddress { get; private set; }

        private UdpReceiver receiver = new UdpReceiver(52001);

        public ScanWindow() {
            InitializeComponent();

            receiver.UdpMessageReceived += OnUdpMessageReceived;
            
            UdpSender.Broadcast("ml.festival.defectively.scan", 52000);

            new Task(async () => { await receiver.ReceiveAsync(); }).Start();
        }

        private void OnUdpMessageReceived(UdpReceiver sender, UdpMessageReceivedEventArgs e) {
            if (e.Content.Split(':')[0] == "ml.festival.defectively.scanResponse") {
                ServersListBox.Dispatcher.Invoke(() => ServersListBox.Items.Add($"{e.RemoteEndPoint.Address}:{e.Content.Split(':')[1]}"));
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            PrimaryButton.IsEnabled = ServersListBox.SelectedIndex != -1;
            SelectedServerAddress = ServersListBox.SelectedItem?.ToString();
        }

        private void OnPrimaryClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void OnSecondaryClick(object sender, RoutedEventArgs e) {
            ServersListBox.Items.Clear();
            UdpSender.Broadcast("ml.festival.defectively.scan", 52000);
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            receiver.Stop();
        }
    }
}
