using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using TcpConnection_Lib;

namespace TCP_Server_Client_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly TcpConnection connection = new TcpConnection();

        enum Tcp
        {
            LISTEN,
            CONNECT,
            IDLE,
            SENDING,
            RECEIVING,
            CLOSE
        }

        static Tcp serverState = Tcp.LISTEN;
        static Tcp clientState = Tcp.CONNECT;

        static volatile bool serverStartButtonFlag = false;
        static volatile bool clientStartButtonFlag = false;
        static volatile bool sendingStringFilledFlag = false;
        static volatile bool tcpConnectedFlag = false;

        static volatile string sendingString = "";
        static volatile string receivingString = "";

        static volatile int port = 0;
        static volatile string ip = "";


        public MainWindow()
        {
            InitializeComponent();
        }


        void TcpServerLoop()
        {
            try
            {
                bool closeFlag = false;
                sendingStringFilledFlag = false;
                sendingString = "";
                receivingString = "";

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    serverTextboxBig.Text = "";
                }));

                serverState = Tcp.LISTEN;

                while (!closeFlag)
                {
                    if (!serverStartButtonFlag)
                    {
                        serverState = Tcp.CLOSE;
                    }

                    switch (serverState)
                    {
                        case Tcp.LISTEN:
                            if (connection.Listen(port))
                            {
                                serverState = Tcp.CONNECT;
                            }
                            else
                            {
                                serverState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.CONNECT:
                            if (connection.TcpIsConnected == true)
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    serverTextboxBig.AppendText("Connected to " + connection.RemoteEndpointAddress + Environment.NewLine);
                                    serverTextboxBig.ScrollToEnd();
                                }));

                                tcpConnectedFlag = true;
                                serverState = Tcp.IDLE;
                            }
                            break;

                        case Tcp.IDLE:
                            if (connection.TcpIsConnected == true)
                            {
                                if (sendingStringFilledFlag)
                                {
                                    serverState = Tcp.SENDING;
                                }
                                else
                                {
                                    serverState = Tcp.RECEIVING;
                                }
                            }
                            else
                            {
                                serverState = Tcp.CLOSE;
                            }

                            break;

                        case Tcp.SENDING:
                            if (connection.TcpIsConnected == true)
                            {
                                if (connection.Send(sendingString))
                                {
                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                    {
                                        serverTextboxBig.AppendText("Sending: " + sendingString + Environment.NewLine);
                                        serverTextboxBig.ScrollToEnd();
                                    }));

                                    sendingString = "";
                                    sendingStringFilledFlag = false;
                                    serverState = Tcp.IDLE;
                                }
                                else
                                {
                                    serverState = Tcp.CLOSE;
                                }
                            }
                            else
                            {
                                serverState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.RECEIVING:
                            if (connection.TcpIsConnected == true)
                            {
                                try
                                {
                                    receivingString = connection.GetReceivedString();

                                    if (receivingString != "")
                                    {
                                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                        {
                                            serverTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                                            serverTextboxBig.ScrollToEnd();
                                        }));

                                        receivingString = "";
                                    }
                                }
                                catch { }
                                serverState = Tcp.IDLE;
                            }
                            else
                            {
                                serverState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.CLOSE:
                            try
                            {
                                sendingString = "";
                                receivingString = "";
                                sendingStringFilledFlag = false;
                                tcpConnectedFlag = false;

                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    serverTextboxBig.AppendText("Disconnected..." + Environment.NewLine);
                                    serverTextboxBig.ScrollToEnd();
                                }));

                                connection.Dispose();
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        default:
                            serverState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Server loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void TcpClientLoop()
        {
            try
            {
                bool closeFlag = false;
                sendingString = "";
                receivingString = "";

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    clientTextboxBig.Text = "";
                }));

                clientState = Tcp.CONNECT;

                while (!closeFlag)
                {
                    if (!clientStartButtonFlag)
                    {
                        clientState = Tcp.CLOSE;
                    }

                    switch (clientState)
                    {
                        case Tcp.CONNECT:
                            if (connection.Connect(ip, port))
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    clientTextboxBig.AppendText("Connected" + Environment.NewLine);
                                    clientTextboxBig.ScrollToEnd();
                                }));

                                tcpConnectedFlag = true;
                                clientState = Tcp.IDLE;
                            }
                            break;

                        case Tcp.IDLE:
                            if (connection.TcpIsConnected == true)
                            {
                                if (sendingStringFilledFlag)
                                {
                                    clientState = Tcp.SENDING;
                                }
                                else
                                {
                                    clientState = Tcp.RECEIVING;
                                }
                            }
                            else
                            {
                                clientState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.SENDING:
                            if (connection.TcpIsConnected == true)
                            {
                                if (connection.Send(sendingString))
                                {
                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                    {
                                        clientTextboxBig.AppendText("Sending: " + sendingString + Environment.NewLine);
                                        clientTextboxBig.ScrollToEnd();
                                    }));

                                    sendingString = "";
                                    sendingStringFilledFlag = false;
                                    clientState = Tcp.IDLE;
                                }
                                else
                                {
                                    clientState = Tcp.CLOSE;
                                }
                            }
                            else
                            {
                                clientState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.RECEIVING:
                            if (connection.TcpIsConnected == true)
                            {
                                try
                                {
                                    receivingString = connection.GetReceivedString();

                                    if (receivingString != "")
                                    {
                                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                        {
                                            clientTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                                            clientTextboxBig.ScrollToEnd();
                                        }));

                                        receivingString = "";
                                    }
                                }
                                catch { }
                                clientState = Tcp.IDLE;
                            }
                            else
                            {
                                clientState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.CLOSE:
                            try
                            {
                                sendingString = "";
                                receivingString = "";
                                sendingStringFilledFlag = false;
                                tcpConnectedFlag = false;

                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    serverTextboxBig.AppendText("Disconnected..." + Environment.NewLine);
                                    serverTextboxBig.ScrollToEnd();
                                }));

                                connection.Dispose();
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        default:
                            clientState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Client loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void serverStartButton_Click(object sender, RoutedEventArgs e)
        {
            serverStartButtonFlag = !serverStartButtonFlag;

            Thread TcpServerThread = new Thread(TcpServerLoop)
            {
                IsBackground = true
            };

            if (serverStartButtonFlag)
            {
                serverStartButton.Content = "Close";
                serverStartButton.Background = Brushes.LightSalmon;

                port = int.Parse(serverPort.Text);

                serverPort.IsEnabled = false;
                clientTab.IsEnabled = false;
                serverSendButton1.IsEnabled = true;
                serverSendButton2.IsEnabled = true;

                TcpServerThread.Start();
            }
            else
            {
                serverStartButton.Content = "Listen";
                serverStartButton.Background = Brushes.LightGreen;

                if (TcpServerThread.IsAlive)
                {
                    TcpServerThread.Join();
                }

                clientTab.IsEnabled = true;
                serverPort.IsEnabled = true;
                serverSendButton1.IsEnabled = false;
                serverSendButton2.IsEnabled = false;
            }
        }

        private void clientStartButton_Click(object sender, RoutedEventArgs e)
        {
            clientStartButtonFlag = !clientStartButtonFlag;

            Thread TcpClientThread = new Thread(TcpClientLoop)
            {
                IsBackground = true
            };

            if (clientStartButtonFlag)
            {
                clientStartButton.Content = "Disconnect";
                clientStartButton.Background = Brushes.LightSalmon;

                port = int.Parse(clientPort.Text);
                ip = clientIP.Text;

                clientPort.IsEnabled = false;
                clientIP.IsEnabled = false;
                serverTab.IsEnabled = false;
                clientSendButton1.IsEnabled = true;
                clientSendButton2.IsEnabled = true;

                TcpClientThread.Start();
            }
            else
            {
                clientStartButton.Content = "Connect";
                clientStartButton.Background = Brushes.LightGreen;

                if (TcpClientThread.IsAlive)
                {
                    TcpClientThread.Join();
                }

                serverTab.IsEnabled = true;
                clientPort.IsEnabled = true;
                clientIP.IsEnabled = true;
                clientSendButton1.IsEnabled = false;
                clientSendButton2.IsEnabled = false;
            }
        }

        private void serverSendButton1_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = serverSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void serverSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = serverSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void clientSendButton1_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = clientSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void clientSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = clientSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }
    }
}
