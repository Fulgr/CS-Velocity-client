using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IPHostEntry ipHostInfo;
        public IPAddress ipAddress;
        public IPEndPoint ipEndPoint;
        public Socket client;
        public List<String> channels = new List<String>();

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);

        private async void SendMessage(string msg)
        {
            var messageBytes = Encoding.UTF8.GetBytes(msg);
            _ = await client.SendAsync(messageBytes, SocketFlags.None);
        }
        private async void InitSocket(string host)
        {
            string[] ip = host.Split(':');
            int port = int.Parse(ip[1]);
            string domain = ip[0];
            ipHostInfo = await Dns.GetHostEntryAsync(domain);
            ipAddress = ipHostInfo.AddressList[0];
            ipEndPoint = new(ipAddress, port);
            client = new(
                ipEndPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            await client.ConnectAsync(ipEndPoint);
            SendMessage("/motd");
            Thread.Sleep(1000);
            SendMessage("/list json");
            while (true)
            {
                string r;
                var buffer = new byte[1_024];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                if (response.StartsWith("/channels"))
                {
                    int letterIndex;
                    string s2 = "";
                    List<String> chans = new List<String>();
                    bool parClosed = true;
                    for (letterIndex=0; letterIndex< response.Length; letterIndex++)
                    {
                        if (response[letterIndex] == '"')
                        {
                            parClosed = !parClosed;
                            if (parClosed)
                            {
                                chans.Add(s2);
                                s2 = "";
                                messagesBox.Text = messagesBox.Text + "\n" + s2;
                            }
                        } 
                        else
                        {
                            if (!parClosed)
                                s2 += response[letterIndex];
                        }
                    }
                    channels = chans;
                } else
                {
                    messagesBox.Text = messagesBox.Text + "\n" + response;
                    _scrollviewer.ScrollToEnd();
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            var value = true;
            DwmSetWindowAttribute(new System.Windows.Interop.WindowInteropHelper(this).Handle, 20, ref value, Marshal.SizeOf(value));
            InitSocket("node2.endelon-hosting.de:34055");
            CurrentTextBox.Focus();
        }

        private void SendCurrentMsg()
        {
            string msg = CurrentTextBox.Text;
            SendMessage(msg);
            CurrentTextBox.Text = "";
        }

        private void SendCurrentMsgHandle(object sender, RoutedEventArgs e)
        {
            SendCurrentMsg();
        }

        private void CurrentTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SendCurrentMsg();
            }
        }
    }
}