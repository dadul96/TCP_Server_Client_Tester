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
        static readonly TcpConnection serverConnection = new TcpConnection();
        static readonly TcpConnection clientConnection = new TcpConnection();

        enum Tcp
        {
            LISTEN,
            INITIAL_CONNECT,
            CONNECT,
            IDLE,
            SENDING,
            RECEIVING,
            DISCONNECT,
            RECONNECT,
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
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                serverStartButton.IsEnabled = false;
                                serverSendButton1.IsEnabled = false;
                                serverSendButton2.IsEnabled = false;
                                serverTextboxBig.AppendText("Waiting for a client..." + Environment.NewLine);
                                serverTextboxBig.ScrollToEnd();
                            }));

                            if (serverConnection.TryListen(port, out string RemoteEndpointAddress))
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    serverStartButton.IsEnabled = true;
                                    serverSendButton1.IsEnabled = true;
                                    serverSendButton2.IsEnabled = true;
                                    serverTextboxBig.AppendText("Connected to " + RemoteEndpointAddress + Environment.NewLine);
                                    serverTextboxBig.ScrollToEnd();
                                }));

                                if (serverConnection.TryReadingData())
                                {
                                    tcpConnectedFlag = true;
                                    serverState = Tcp.IDLE;
                                }
                                else
                                {
                                    serverState = Tcp.DISCONNECT;
                                }
                            }
                            else
                            {
                                serverState = Tcp.DISCONNECT;
                            }
                            break;

                        case Tcp.IDLE:
                            if (serverConnection.TcpIsConnected == true)
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
                                serverState = Tcp.DISCONNECT;
                            }

                            break;

                        case Tcp.SENDING:
                            if (serverConnection.TrySend(sendingString))
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
                                serverState = Tcp.DISCONNECT;
                            }
                            break;

                        case Tcp.RECEIVING:
                            receivingString = serverConnection.GetReceivedString();

                            if (receivingString != null)
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    serverTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                                    serverTextboxBig.ScrollToEnd();
                                }));

                                receivingString = null;
                            }
                            serverState = Tcp.IDLE;
                            break;

                        case Tcp.DISCONNECT:
                            sendingString = "";
                            receivingString = "";
                            sendingStringFilledFlag = false;
                            tcpConnectedFlag = false;

                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                serverStartButton.IsEnabled = false;
                                serverSendButton1.IsEnabled = false;
                                serverSendButton2.IsEnabled = false;
                                serverTextboxBig.AppendText("Problem occurred! Reconnecting..." + Environment.NewLine);
                                serverTextboxBig.ScrollToEnd();
                            }));

                            serverConnection.Disconnect();

                            serverState = Tcp.LISTEN;
                            break;

                        case Tcp.CLOSE:
                            sendingString = "";
                            receivingString = "";
                            sendingStringFilledFlag = false;
                            tcpConnectedFlag = false;

                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                serverTextboxBig.AppendText("Disconnected..." + Environment.NewLine);
                                serverTextboxBig.ScrollToEnd();
                            }));

                            serverConnection.Dispose();
                            closeFlag = true;
                            break;

                        default:
                            serverState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("PLEASE RESTART! TcpServerLoop-thread error: \n\n" + Ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                clientState = Tcp.INITIAL_CONNECT;

                while (!closeFlag)
                {
                    if (!clientStartButtonFlag)
                    {
                        clientState = Tcp.CLOSE;
                    }

                    switch (clientState)
                    {
                        case Tcp.INITIAL_CONNECT:
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                clientStartButton.IsEnabled = false;
                                clientSendButton1.IsEnabled = false;
                                clientSendButton2.IsEnabled = false;
                                clientTextboxBig.AppendText("Trying to connect..." + Environment.NewLine);
                                clientTextboxBig.ScrollToEnd();
                            }));

                            if (clientConnection.TryConnect(ip, port))
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    clientStartButton.IsEnabled = true;
                                    clientSendButton1.IsEnabled = true;
                                    clientSendButton2.IsEnabled = true;
                                    clientTextboxBig.AppendText("Connected" + Environment.NewLine);
                                    clientTextboxBig.ScrollToEnd();
                                }));

                                if (clientConnection.TryReadingData())
                                {
                                    tcpConnectedFlag = true;
                                    clientState = Tcp.IDLE;
                                }
                                else
                                {
                                    clientState = Tcp.RECONNECT;
                                }
                            }
                            else
                            {
                                clientState = Tcp.RECONNECT;
                            }
                            break;

                        case Tcp.CONNECT:
                            if (clientConnection.TryConnect(ip, port))
                            {
                                if (clientConnection.TryReadingData())
                                {
                                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                    {
                                        clientStartButton.IsEnabled = true;
                                        clientSendButton1.IsEnabled = true;
                                        clientSendButton2.IsEnabled = true;
                                        clientTextboxBig.AppendText("Connected" + Environment.NewLine);
                                        clientTextboxBig.ScrollToEnd();
                                    }));

                                    tcpConnectedFlag = true;
                                    clientState = Tcp.IDLE;
                                }
                                else
                                {
                                    clientState = Tcp.DISCONNECT;
                                }
                            }
                            else
                            {
                                clientState = Tcp.DISCONNECT;
                            }
                            break;

                        case Tcp.IDLE:
                            if (clientConnection.TcpIsConnected == true)
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
                                clientState = Tcp.RECONNECT;
                            }

                            break;

                        case Tcp.SENDING:
                            if (clientConnection.TrySend(sendingString))
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
                                clientState = Tcp.RECONNECT;
                            }
                            break;

                        case Tcp.RECEIVING:
                            receivingString = clientConnection.GetReceivedString();

                            if (receivingString != null)
                            {
                                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    clientTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                                    clientTextboxBig.ScrollToEnd();
                                }));

                                receivingString = null;
                            }
                            clientState = Tcp.IDLE;
                            break;

                        case Tcp.RECONNECT:
                            sendingString = "";
                            receivingString = "";
                            sendingStringFilledFlag = false;
                            tcpConnectedFlag = false;

                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                clientStartButton.IsEnabled = false;
                                clientSendButton1.IsEnabled = false;
                                clientSendButton2.IsEnabled = false;
                                clientTextboxBig.AppendText("Problem occurred! Reconnecting..." + Environment.NewLine);
                                clientTextboxBig.ScrollToEnd();
                            }));

                            clientConnection.Disconnect();

                            clientState = Tcp.CONNECT;
                            break;

                        case Tcp.DISCONNECT:
                            clientConnection.Disconnect();

                            clientState = Tcp.CONNECT;
                            break;

                        case Tcp.CLOSE:
                            sendingString = "";
                            receivingString = "";
                            sendingStringFilledFlag = false;
                            tcpConnectedFlag = false;

                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                clientTextboxBig.AppendText("Disconnected..." + Environment.NewLine);
                                clientTextboxBig.ScrollToEnd();
                            }));

                            clientConnection.Disconnect();
                            closeFlag = true;
                            break;

                        default:
                            clientState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("PLEASE RESTART! TcpClientLoop-thread error: \n\n" + Ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (sendingString != null && sendingString != "")
                {
                    sendingStringFilledFlag = true;
                }
            }
        }

        private void serverSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = serverSendTextbox2.Text;
                if (sendingString != null && sendingString != "")
                {
                    sendingStringFilledFlag = true;
                }
            }
        }

        private void clientSendButton1_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = clientSendTextbox1.Text;
                if (sendingString != null && sendingString != "")
                {
                    sendingStringFilledFlag = true;
                }
            }
        }

        private void clientSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingStringFilledFlag && tcpConnectedFlag)
            {
                sendingString = clientSendTextbox2.Text;
                if (sendingString != null && sendingString != "")
                {
                    sendingStringFilledFlag = true;
                }
            }
        }
    }
}
