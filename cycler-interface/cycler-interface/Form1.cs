using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using WindowsFormsApp1;
using System.Net;
using System.Net.Sockets;

namespace cycler_interface
{
    public partial class Form1 : Form
    {
        // initialise objects
        basytecAPI basy;

        // threads
        Thread screenUpdateThread;  // creates a new thread for updating the screen
        //Thread serverThread;  // creates a new thread for updating the screen

        // initialise timers
        System.Timers.Timer requestFromBasyTimer;
        System.Timers.Timer basyConnectionTimer;

        // variables
        int basyRequestInterval =5000;
        int screenRefereshTime = 1000;            // sets the screen refresh time in ms
        int portInput;
        string serverIpAd;

        String[,] basyCurrentStats = new String[41, 2]; // spare row to store bad data

        private bool basyConnected = false;
        private bool serverOpen = false;
        bool serverInitFlag = true;

        public Form1()
        {
            InitializeComponent();

            // start a thread for updating the windows form screen every second. Used for logging data too
            screenUpdateThread = new Thread(new ThreadStart(UpdateScreen));
            screenUpdateThread.IsBackground = true;
            screenUpdateThread.Start();

            //instance of timers
            requestFromBasyTimer = new System.Timers.Timer();
            requestFromBasyTimer.Elapsed += new ElapsedEventHandler(requestFromBasy);
            requestFromBasyTimer.Interval = basyRequestInterval;
            requestFromBasyTimer.AutoReset = true;
            requestFromBasyTimer.Enabled = false;

            // create a timer for testing connection with the basytec. Only fires once
            basyConnectionTimer = new System.Timers.Timer();
            basyConnectionTimer.Elapsed += new ElapsedEventHandler(TestBasyConnection);     // runs every second
            basyConnectionTimer.Interval = 5000;
            basyConnectionTimer.AutoReset = false;
            basyConnectionTimer.Enabled = false;

            // create instance
            basy = new basytecAPI();

            // example numbers
            basyCurrentStats[21, 0] = "1";
            basyCurrentStats[21, 1] = "11";
            basyCurrentStats[22, 0] = "2";
            basyCurrentStats[22, 1] = "12";

        }
        public void UpdateScreen()
        {
            var basyLine = new[]
{
                basyLine0,
                basyLine1,
                basyLine2,
                basyLine3,
                basyLine4,
                basyLine5,
                basyLine6,
                basyLine7,
                basyLine8,
                basyLine9,
                basyLine10,
                basyLine11,
                basyLine12,
                basyLine13,
                basyLine14,
                basyLine15,
                basyLine16,
                basyLine17,
                basyLine18,
                basyLine19,
                basyLine20,
                basyLine21,
                basyLine22,
                basyLine23,
                basyLine24,
                basyLine25,
                basyLine26,
                basyLine27,
                basyLine28,
                basyLine29,
                basyLine30,
                basyLine31,
                basyLine32,
                basyLine33,
                basyLine34,
                basyLine35,
                basyLine36,
                basyLine37,
                basyLine38,
                basyLine39,
            };
            var basyCycle = new[]
{
                basyCycle0,
                basyCycle1,
                basyCycle2,
                basyCycle3,
                basyCycle4,
                basyCycle5,
                basyCycle6,
                basyCycle7,
                basyCycle8,
                basyCycle9,
                basyCycle10,
                basyCycle11,
                basyCycle12,
                basyCycle13,
                basyCycle14,
                basyCycle15,
                basyCycle16,
                basyCycle17,
                basyCycle18,
                basyCycle19,
                basyCycle20,
                basyCycle21,
                basyCycle22,
                basyCycle23,
                basyCycle24,
                basyCycle25,
                basyCycle26,
                basyCycle27,
                basyCycle28,
                basyCycle29,
                basyCycle30,
                basyCycle31,
                basyCycle32,
                basyCycle33,
                basyCycle34,
                basyCycle35,
                basyCycle36,
                basyCycle37,
                basyCycle38,
                basyCycle39
            };

            while (true)
            {      // creates an infinite loop
                Thread.Sleep(screenRefereshTime);           // this sets the refresh time for temps on the screen
                for (int i = 0; i < 40; i++)
                {
                    messageLog.Invoke((MethodInvoker)delegate
                    {
                        basyLine[i].Text = basyCurrentStats[i, 0];
                        basyCycle[i].Text = basyCurrentStats[i, 1];
                    });
                }

                if (serverOpen)
                {
                    portStatus.Invoke((MethodInvoker)delegate
                    {
                        portStatus.BackColor = Color.FromArgb(255, 0, 210, 100);
                        portStatus.Text = "Server Port Open";
                    });

                }
                else
                {
                    portStatus.Invoke((MethodInvoker)delegate
                    {
                        portStatus.BackColor = Color.FromName("Coral");
                        portStatus.Text = "Server Port Closed";
                    });
                }
            }
        }
        public void requestFromBasy(object source, ElapsedEventArgs e)
        {
            /*
            // TEST CODE
            Console.WriteLine("Starting basy connection");
            string[] lineNum = basy.getLoop(22);
            */

            // cycle through each channel and request the info
            for (int i = 0; i < 40; i++)
            {
                int actualChannel = 41;

                // first get the line number
                string[] lineNum = basy.getLine(i);
                try
                {
                    actualChannel = Convert.ToInt32(lineNum[0]);
                }
                catch
                {
                    Console.WriteLine("Line # in wrong format:" + lineNum[0]);
                }
                basyCurrentStats[actualChannel, 0] = lineNum[1];

                actualChannel = 41; // write into the spare row if error below

                // then get the loop number
                string[] loopNum = basy.getLine(i);
                try
                {
                    actualChannel = Convert.ToInt32(lineNum[0]);
                }
                catch
                {
                    Console.WriteLine("Line # in wrong format:" + lineNum[0]);
                }
                basyCurrentStats[actualChannel, 1] = loopNum[1];
            }

        }
        private void ConnectToBasy()
        {
            string ip = basyIP.Text;
            string port = basyPort.Text;

            // disable the connection button
            connectBasy.Enabled = false;

            if (basy.connectToBasy(ip, port))
            {
                // start timer to check basy connection
                basyConnectionTimer.Enabled = true;
            }
            else
            {
                connectBasy.Enabled = true;     // re-enable the button
                basyConnected = false;

            }
        }
        private void DisconnectBasy()
        {
            requestFromBasyTimer.Enabled = false;  // stop sending info to the basy
            basyConnected = false;      // disconnect
            basy.closeConnection();     // if already connected then disconnect
        }
        private void TestBasyConnection(object source, ElapsedEventArgs e)
        {
            if (basy.isConnected())
            {
                basyConnected = true;
            }
            else
            {
                basyConnected = false;
                appendToMessageLog("Failed to connect to BaSyTec");
            }

            // enable the button again
            connectBasy.Invoke((MethodInvoker)delegate
            { connectBasy.Enabled = true; });

            // now if the basy in connected start the new inifite thread
            if (basyConnected)
            {
                requestFromBasyTimer.Start();
            }
        }
        private void appendToMessageLog(string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");     // creates a timestamp for when the message was printed to the messagelog

            // checks to see if the graph is on a seperate thread 


            if (messageLog.InvokeRequired)
            {
                messageLog.Invoke((MethodInvoker)delegate
                {
                    // Running on the UI thread
                    messageLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
                });
            }
            else
            {
                messageLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
            }
        }
        private void appendToServerLog(string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");     // creates a timestamp for when the message was printed to the messagelog

            // checks to see if the graph is on a seperate thread 


            if (serverLog.InvokeRequired)
            {
                serverLog.Invoke((MethodInvoker)delegate
                {
                    // Running on the UI thread
                    serverLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
                });
            }
            else
            {
                serverLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
            }
        }
        private void InitialiseServer()
        {
            try
            {
                portInput = Convert.ToInt32(serverPortInput.Text);
                serverIpAd = serverIPInput.Text;
            }
            catch
            {
                appendToServerLog("Are port and IP in the right format");
                return;
            }
            Console.WriteLine("Creating a thread");
            // start a thread for updating the windows form screen every second. Used for logging data too
            Thread serverThread = new Thread(new ThreadStart(openServer));
            serverThread.IsBackground = true;
            serverThread.Start();
        }
        private void CloseServer()
        {
            serverOpen = false;
            Thread.Sleep(500);
        }
        private void openServer()
        {
            // create connection
            IPAddress ipAddress = System.Net.IPAddress.Parse(serverIpAd);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, portInput);

            // Create a Socket that will use Tcp protocol

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);

                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                appendToServerLog("Waiting for a connection...");
                serverOpen = true;

                // awaiting connection
                Socket handler = listener.Accept();

                // connection established
                appendToServerLog("Connection Established");
                serverConnect.Invoke((MethodInvoker)delegate
                {
                    serverConnect.Enabled = true;
                    serverConnect.Text = "Close";
                });
                

                // now infinite loop to listen for requests
                while (serverOpen)
                {
                    // Incoming data from the client.
                    string data = null;
                    byte[] bytes = null;

                    while (serverOpen)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }
                    string chanNum = data.Split('<')[0];
                    //appendToServerLog("Text received:" + data);
                    //appendToServerLog("Channel #:" + chanNum);

                    string line = "0";
                    string loop = "0";
                    try
                    {
                        line = basyCurrentStats[Convert.ToInt32(chanNum) - 1, 0];
                        loop = basyCurrentStats[Convert.ToInt32(chanNum) - 1, 1];
                    }
                    catch (NullReferenceException)
                    {
                        Console.WriteLine("Info requested from blank line");
                    }

                    string dataSend = chanNum + ":" + line + ":" + loop;
                    byte[] msg = Encoding.ASCII.GetBytes(dataSend);
                    handler.Send(msg);
                }
                handler.Close();
                listener.Close();
                Console.WriteLine("Exited loop");
                

            }
            catch (Exception e)
            {
                listener.Close();
                Console.WriteLine("Error..... " + e.StackTrace);
                appendToServerLog("Disconnected");

                serverConnect.Invoke((MethodInvoker)delegate
                {
                    serverConnect.Text = "Open";
                    serverConnect.Enabled = true;
                });
                serverOpen = false;
            }
        }
        /*
        *  BUTTONS
        */
        private void ConnectBasy_Click(object sender, EventArgs e)
        {
            
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            if (serverConnect.Text == "Open")
            {
                InitialiseServer();
                serverConnect.Text = "Awaiting Connection";
                serverConnect.Enabled = false;
            }
            else
            {
                CloseServer();
                serverConnect.Text = "Open";
            }
        }
        private void Label43_Click(object sender, EventArgs e)
        {

        }

        private void connectBasy_Click_1(object sender, EventArgs e)
        {
            if (basyConnected)
            {
                DisconnectBasy();
            }
            else
            {
                ConnectToBasy();
            }
        }
    }
}
