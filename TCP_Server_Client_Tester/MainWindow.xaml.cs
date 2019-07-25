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
            CONNECT,
            IDLE,
            SENDING,
            RECEIVING,
            CLOSE
        }

        static volatile Tcp serverState = Tcp.CONNECT;
        static volatile Tcp clientState = Tcp.CONNECT;

        const int SIZE1K = 1024;    //bytes
        const int TIMEOUT = 2000;   //ms

        static volatile bool serverStartButtonFlag = false;
        static volatile bool clientStartButtonFlag = false;
        static volatile bool sendingStringFilledFlag = false;
        static volatile bool sendingDoneFlag = false;
        static volatile bool receivingDoneFlag = false;

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

                IPAddress localAddr = IPAddress.Parse(ip);
                TcpListener server = new TcpListener(localAddr, port);
                TcpClient client = null;

                byte[] bytesToSend = new byte[SIZE1K];
                byte[] bytesToRead = new byte[SIZE1K];

                NetworkStream TcpStream = null;

                while (serverStartButtonFlag && !closeFlag)
                {
                    switch (serverState)
                    {
                        case Tcp.CONNECT:
                            sendingString = "";
                            receivingString = "";

                            server.Start();
                            client = server.AcceptTcpClient();
                            client.ReceiveTimeout = TIMEOUT;
                            client.SendTimeout = TIMEOUT;
                            client.SendBufferSize = SIZE1K;
                            client.ReceiveBufferSize = SIZE1K;

                            TcpStream = client.GetStream();

                            serverState = Tcp.IDLE;
                            break;

                        case Tcp.IDLE:
                            if (client.Connected == true)
                            {
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
                                serverState = Tcp.CONNECT;
                            }
                            break;

                        case Tcp.SENDING:
                            bytesToSend = ASCIIEncoding.ASCII.GetBytes(sendingString);
                            TcpStream.Write(bytesToSend, 0, bytesToSend.Length);
                            sendingStringFilledFlag = false;
                            sendingDoneFlag = true;
                            serverState = Tcp.IDLE;
                            break;

                        case Tcp.RECEIVING:
                            int bytesRead = TcpStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                            receivingString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                            receivingDoneFlag = true;
                            serverState = Tcp.IDLE;
                            break;

                        case Tcp.CLOSE:
                            try
                            {
                                sendingString = "";
                                receivingString = "";
                                sendingStringFilledFlag = false;
                                sendingDoneFlag = false;
                                receivingDoneFlag = false;
                                client.Close();
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        default:
                            serverState = Tcp.CLOSE;
                            break;
                    }
                }
                try
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                    server.Stop();
                }
                catch { }
                serverState = Tcp.CONNECT;
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

                while (clientStartButtonFlag && !closeFlag)
                {
                    switch (clientState)
                    {
                        case Tcp.CONNECT:
                            sendingString = "";
                            receivingString = "";

                            client.Connect(ip, port);
                            TcpStream = client.GetStream();

                            clientState = Tcp.IDLE;
                            break;

                        case Tcp.IDLE:
                            if (client.Connected == true)
                            {
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
                                clientState = Tcp.CONNECT;
                            }
                            break;

                        case Tcp.SENDING:
                            bytesToSend = ASCIIEncoding.ASCII.GetBytes(sendingString);
                            TcpStream.Write(bytesToSend, 0, bytesToSend.Length);
                            sendingStringFilledFlag = false;
                            sendingDoneFlag = true;
                            clientState = Tcp.IDLE;
                            break;

                        case Tcp.RECEIVING:
                            int bytesRead = TcpStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                            receivingString = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
                            receivingDoneFlag = true;
                            clientState = Tcp.IDLE;
                            break;

                        case Tcp.CLOSE:
                            try
                            {
                                sendingString = "";
                                receivingString = "";
                                sendingStringFilledFlag = false;
                                sendingDoneFlag = false;
                                receivingDoneFlag = false;
                                client.Close();
                            }
                            catch { }
                            closeFlag = true;
                            break;

                        default:
                            clientState = Tcp.CLOSE;
                            break;
                    }
                }
                try
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                }
                catch { }
                clientState = Tcp.CONNECT;
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
            Thread PrintingServerThread = new Thread(PrintingServerLoop);
            Thread TcpServerThread = new Thread(TcpServerLoop);

            if (serverStartButtonFlag)
            {
                serverStartButton.Content = "Close";
                serverStartButton.Background = Brushes.LightSalmon;

                port = int.Parse(serverPort.Text);
                ip = serverIP.Text;

                serverTextboxBig.Text = "";

                serverPort.IsEnabled = false;
                serverIP.IsEnabled = false;
                clientTab.IsEnabled = false;

                sendingString = "";
                receivingString = "";
                sendingStringFilledFlag = false;
                sendingDoneFlag = false;
                receivingDoneFlag = false;

                PrintingServerThread.IsBackground = true;
                TcpServerThread.IsBackground = true;
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
                serverIP.IsEnabled = true;
            }
        }

        private void clientStartButton_Click(object sender, RoutedEventArgs e)
        {
            clientStartButtonFlag = !clientStartButtonFlag;
            Thread PrintingClientThread = new Thread(PrintingClientLoop);
            Thread TcpClientThread = new Thread(TcpClientLoop);

            if (clientStartButtonFlag)
            {
                clientStartButton.Content = "Disconnect";
                clientStartButton.Background = Brushes.LightSalmon;

                port = int.Parse(clientPort.Text);
                ip = clientIP.Text;

                clientTextboxBig.Text = "";

                clientPort.IsEnabled = false;
                clientIP.IsEnabled = false;
                serverTab.IsEnabled = false;

                sendingString = "";
                receivingString = "";
                sendingStringFilledFlag = false;
                sendingDoneFlag = false;
                receivingDoneFlag = false;

                PrintingClientThread.IsBackground = true;
                TcpClientThread.IsBackground = true;
                PrintingClientThread.Start();
                TcpClientThread.Start();
            }
            else
            {
                clientStartButton.Content = "Connect";
                clientStartButton.Background = Brushes.LightGreen;

                if (TcpClientThread.IsAlive) {
                    TcpClientThread.Join();
                }
                if (PrintingClientThread.IsAlive)
                {
                    PrintingClientThread.Join();
                }
                
                serverTab.IsEnabled = true;
                clientPort.IsEnabled = true;
                clientIP.IsEnabled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && (serverState != Tcp.CONNECT))
            {
                sendingString = serverSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && (serverState != Tcp.CONNECT))
            {
                sendingString = serverSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && (clientState != Tcp.CONNECT)) 
            {
                sendingString = clientSendTextbox1.Text;
                sendingStringFilledFlag = true;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (!sendingDoneFlag && !sendingStringFilledFlag && (clientState != Tcp.CONNECT))
            {
                sendingString = clientSendTextbox2.Text;
                sendingStringFilledFlag = true;
            }
        }
    }
}
