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

namespace TCP_Server_Client_Tester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum Tcp
        {
            NONE,
            LISTEN,
            CONNECT,
            IDLE,
            SENDING,
            RECEIVING,
            CLOSE
        }

        static volatile Tcp serverState = Tcp.NONE;
        static volatile Tcp clientState = Tcp.NONE;

        const int SIZE1K = 1024;    //bytes
        const int TIMEOUT = 1000;   //ms

        static volatile bool serverStartButtonFlag = false;
        static volatile bool clientStartButtonFlag = false;
        static volatile bool sendingStringFilledFlag = false;
        static volatile bool sendingDoneFlag = false;
        static volatile bool receivingDoneFlag = false;
        static volatile bool TcpConnectedFlag = false;

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
                sendingDoneFlag = false;
                sendingString = "";
                receivingString = "";

                IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                TcpListener server = new TcpListener(ipLocalEndPoint);

                TcpClient client = null;

                byte[] bytesToSend = new byte[SIZE1K];
                byte[] bytesToRead = new byte[SIZE1K];

                NetworkStream TcpStream = null;

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
                            server.Start();
                            serverState = Tcp.CONNECT;
                            break;

                        case Tcp.CONNECT:
                            if (server.Pending()) 
                            {
                                client = server.AcceptTcpClient();

                                server.Stop();

                                client.ReceiveTimeout = TIMEOUT;
                                client.SendTimeout = TIMEOUT;
                                client.SendBufferSize = SIZE1K;
                                client.ReceiveBufferSize = SIZE1K;

                                TcpStream = client.GetStream();

                                serverState = Tcp.IDLE;
                            }
                            break;

                        case Tcp.IDLE:
                            if (client.Connected == true)
                            {
                                TcpConnectedFlag = true;

                                if (sendingStringFilledFlag)
                                {
                                    serverState = Tcp.SENDING;
                                }
                                else if (TcpStream.DataAvailable)
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
                            try
                            {
                                bytesToSend = ASCIIEncoding.ASCII.GetBytes(sendingString);
                                TcpStream.Write(bytesToSend, 0, bytesToSend.Length);
                                sendingStringFilledFlag = false;
                                sendingDoneFlag = true;
                                serverState = Tcp.IDLE;
                            }
                            catch
                            {
                                serverState = Tcp.CLOSE;
                            }
                            break;

                        case Tcp.RECEIVING:
                            try
                            {
                                int bytesRead = TcpStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                                receivingString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                                receivingDoneFlag = true;
                                serverState = Tcp.IDLE;
                            }
                            catch
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
                                sendingDoneFlag = false;
                                receivingDoneFlag = false;
                                TcpConnectedFlag = false;

                                try
                                {
                                    TcpStream.Close();
                                    TcpStream.Dispose();

                                }
                                catch { }
                                try
                                {
                                    client.Close();
                                    client.Client.Dispose();
                                }
                                catch { }
                                try
                                {
                                    server.Stop();
                                    server.Server.Close();
                                    server = null;
                                }
                                catch { }
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        case Tcp.NONE:
                            break;

                        default:
                            serverState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch
            {
                MessageBoxResult errorMessageBox = MessageBox.Show("Server loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        void TcpClientLoop()
        {
            try
            {
                bool closeFlag = false;

                TcpClient client = new TcpClient();
                client.ReceiveTimeout = TIMEOUT;
                client.SendTimeout = TIMEOUT;
                client.SendBufferSize = SIZE1K;
                client.ReceiveBufferSize = SIZE1K;

                byte[] bytesToSend = new byte[client.SendBufferSize];
                byte[] bytesToRead = new byte[client.ReceiveBufferSize];

                NetworkStream TcpStream = null;

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
                            sendingString = "";
                            receivingString = "";

                            try
                            {
                                client.Connect(ip, port);
                                TcpStream = client.GetStream();

                                clientState = Tcp.IDLE;
                            }
                            catch { }

                            break;

                        case Tcp.IDLE:
                            if (client.Connected == true)
                            {
                                TcpConnectedFlag = true;

                                if (sendingStringFilledFlag)
                                {
                                    clientState = Tcp.SENDING;
                                }
                                else if (TcpStream.DataAvailable)
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
                            try
                            {
                                bytesToSend = ASCIIEncoding.ASCII.GetBytes(sendingString);
                                TcpStream.Write(bytesToSend, 0, bytesToSend.Length);
                                sendingStringFilledFlag = false;
                                sendingDoneFlag = true;
                                clientState = Tcp.IDLE;
                            }
                            catch
                            {
                                clientState = Tcp.CLOSE;
                            }

                            break;

                        case Tcp.RECEIVING:
                            try
                            {
                                int bytesRead = TcpStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                                receivingString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                                receivingDoneFlag = true;
                                clientState = Tcp.IDLE;
                            }
                            catch
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
                                sendingDoneFlag = false;
                                receivingDoneFlag = false;
                                TcpConnectedFlag = false;

                                try
                                {
                                    client.Close();
                                    client.Client.Dispose();
                                }
                                catch { }
                                try
                                {
                                    TcpStream.Close();
                                    TcpStream.Dispose();
                                }
                                catch { }
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        case Tcp.NONE:
                            break;

                        default:
                            clientState = Tcp.CLOSE;
                            break;
                    }
                }
            }
            catch
            {
                MessageBoxResult errorMessageBox = MessageBox.Show("Client loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        void PrintingServerLoop()
        {
            try
            {
                bool oldTcpConnectedState = false;

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    serverTextboxBig.Text = "";
                }));

                while (serverStartButtonFlag)
                {
                    if (sendingDoneFlag)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            serverTextboxBig.AppendText("Sending: " + sendingString + Environment.NewLine);
                            serverTextboxBig.ScrollToEnd();
                        }));
                        sendingString = "";
                        sendingDoneFlag = false;
                    }

                    if (receivingDoneFlag)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            serverTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                            serverTextboxBig.ScrollToEnd();
                        }));
                        receivingString = "";
                        receivingDoneFlag = false;
                    }

                    if (oldTcpConnectedState != TcpConnectedFlag)
                    {
                        oldTcpConnectedState = TcpConnectedFlag;

                        if (TcpConnectedFlag) 
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                serverTextboxBig.AppendText("### CONNECTED ###" + Environment.NewLine);
                                serverTextboxBig.ScrollToEnd();
                            }));
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                serverTextboxBig.AppendText("### DISCONNECTED ###" + Environment.NewLine);
                                serverTextboxBig.ScrollToEnd();
                            }));
                        }
                    }
                }
            }
            catch
            {
                MessageBoxResult errorMessageBox = MessageBox.Show("Server printing loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        void PrintingClientLoop()
        {
            try
            {
                bool oldTcpConnectedState = false;

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    clientTextboxBig.Text = "";
                }));

                while (clientStartButtonFlag)
                {
                    if (sendingDoneFlag)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            clientTextboxBig.AppendText("Sending: " + sendingString + Environment.NewLine);
                            clientTextboxBig.ScrollToEnd();
                        }));
                        sendingString = "";
                        sendingDoneFlag = false;
                    }

                    if (receivingDoneFlag)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            clientTextboxBig.AppendText("Receiving: " + receivingString + Environment.NewLine);
                            clientTextboxBig.ScrollToEnd();
                        }));
                        receivingString = "";
                        receivingDoneFlag = false;
                    }

                    if (oldTcpConnectedState != TcpConnectedFlag)
                    {
                        oldTcpConnectedState = TcpConnectedFlag;

                        if (TcpConnectedFlag)
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                clientTextboxBig.AppendText("### CONNECTED ###" + Environment.NewLine);
                                clientTextboxBig.ScrollToEnd();
                            }));
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            {
                                clientTextboxBig.AppendText("### DISCONNECTED ###" + Environment.NewLine);
                                clientTextboxBig.ScrollToEnd();
                            }));
                        }
                    }
                }
            }
            catch
            {
                MessageBoxResult errorMessageBox = MessageBox.Show("Client printing loop error. Please restart the Program!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void serverStartButton_Click(object sender, RoutedEventArgs e)
        {
            serverStartButtonFlag = !serverStartButtonFlag;

            Thread TcpServerThread = new Thread(TcpServerLoop);
            TcpServerThread.IsBackground = true;

            Thread PrintingServerThread = new Thread(PrintingServerLoop);
            PrintingServerThread.IsBackground = true;


            if (serverStartButtonFlag)
            {
                serverStartButton.Content = "Close";
                serverStartButton.Background = Brushes.LightSalmon;

                port = int.Parse(serverPort.Text);

                serverPort.IsEnabled = false;
                clientTab.IsEnabled = false;
                serverSendButton1.IsEnabled = true;
                serverSendButton2.IsEnabled = true;

                PrintingServerThread.Start();
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
                if (PrintingServerThread.IsAlive)
                {
                    PrintingServerThread.Join();
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

            Thread TcpClientThread = new Thread(TcpClientLoop);
            TcpClientThread.IsBackground = true;

            Thread PrintingClientThread = new Thread(PrintingClientLoop);
            PrintingClientThread.IsBackground = true;


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

                PrintingClientThread.Start();
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
                if (PrintingClientThread.IsAlive)
                {
                    PrintingClientThread.Join();
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
            if (!sendingDoneFlag && !sendingStringFilledFlag && TcpConnectedFlag)
            {
                sendingString = serverSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void serverSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && TcpConnectedFlag)
            {
                sendingString = serverSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void clientSendButton1_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && TcpConnectedFlag)
            {
                sendingString = clientSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void clientSendButton2_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && TcpConnectedFlag)
            {
                sendingString = clientSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }
    }
}
