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
        peltierTCP 
            peltierTCP_1,
            peltierTCP_2,
            peltierTCP_3,
            peltierTCP_4,
            peltierTCP_5,
            peltierTCP_6;

        // threads
        Thread screenUpdateThread;  // creates a new thread for updating the screen
        //Thread serverThread;  // creates a new thread for updating the screen

        // initialise timers
        System.Timers.Timer requestFromBasyTimer;
        System.Timers.Timer basyConnectionTimer;

        // variables
        int basyRequestInterval =10000;
        int screenRefereshTime = 500;            // sets the screen refresh time in ms

        String[,] basyCurrentStats = new String[41, 2]; // spare row to store bad data

        private bool basyConnected = false;

        public Form1()
        {
            InitializeComponent();
            loadDefaultSettings();

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
            peltierTCP_1 = new peltierTCP(serverLog, serverConnect1);
            peltierTCP_2 = new peltierTCP(serverLog, serverConnect2);
            peltierTCP_3 = new peltierTCP(serverLog, serverConnect3);
            peltierTCP_4 = new peltierTCP(serverLog, serverConnect4);
            peltierTCP_5 = new peltierTCP(serverLog, serverConnect5);
            peltierTCP_6 = new peltierTCP(serverLog, serverConnect6);

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

                if (basyConnected)
                {
                    basyConnectedLabel.Invoke((MethodInvoker)delegate
                    {
                        basyConnectedLabel.BackColor = Color.FromArgb(255, 0, 210, 100);
                        basyConnectedLabel.Text = "Connected";
                        basyConnectLabel2.BackColor = Color.FromArgb(255, 0, 210, 100);
                        basyConnectLabel2.Text = "BaSyTec Connected";
                        connectBasy.Text = "Disconnect";
                    });
                }
                else
                {
                    basyConnectedLabel.Invoke((MethodInvoker)delegate
                    {
                        basyConnectedLabel.BackColor = Color.FromName("Coral");
                        basyConnectedLabel.Text = "Not connected";
                        basyConnectLabel2.BackColor = Color.FromName("Coral");
                        basyConnectLabel2.Text = "BaSyTec";
                        connectBasy.Text = "Connect";
                    });
                }

                updateServerIndicators();
            }
        }
        private void updateIPs(object sender, EventArgs e)
        {
            var IPInputs = new[]
            {
                IPInput2,
                IPInput3,
                IPInput4,
                IPInput5,
                IPInput6
            };

            // now cycle through each instance and update them one by one
            foreach (TextBox IP in IPInputs)
            {
                IP.Text = serverIPInput1.Text;
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
        private void closeForm()
        {
            saveDefaultSettings();
        }
        private void loadForm()
        {
            loadDefaultSettings();
        }
        /*
         *      SAVE AND LOAD DATA
         */
        private void loadDefaultSettings()
        {
            Console.WriteLine("Settings loaded");

            // load all the default settings from the config file or the temp file on the user PC
            basyIP.Text = Properties.Settings.Default.basyIP;
            basyPort.Text = Properties.Settings.Default.basyPort;
            serverIPInput1.Text = Properties.Settings.Default.localIP;
            serverPortInput1.Text = Properties.Settings.Default.port1;
            serverPortInput2.Text = Properties.Settings.Default.port2;
            serverPortInput3.Text = Properties.Settings.Default.port3;
            serverPortInput4.Text = Properties.Settings.Default.port4;
            serverPortInput5.Text = Properties.Settings.Default.port5;
            serverPortInput6.Text = Properties.Settings.Default.port6;

        }
        private void saveDefaultSettings()
        {
            Console.WriteLine("Settings saved");

            // save all the default settings from the config file or the temp file on the user PC
            Properties.Settings.Default.basyIP = basyIP.Text;
            Properties.Settings.Default.basyPort = basyPort.Text;
            Properties.Settings.Default.localIP = serverIPInput1.Text;
            Properties.Settings.Default.port1 = serverPortInput1.Text;
            Properties.Settings.Default.port2 = serverPortInput2.Text;
            Properties.Settings.Default.port3 = serverPortInput3.Text;
            Properties.Settings.Default.port4 = serverPortInput4.Text;
            Properties.Settings.Default.port5 = serverPortInput5.Text;
            Properties.Settings.Default.port6 = serverPortInput6.Text;

            // now save all the properties
            Properties.Settings.Default.Save();
        }
            /*
             *      BASYTEC CONNECTION
             */
            public void requestFromBasy(object source, ElapsedEventArgs e)
        {
            /*
            // TEST CODE
            Console.WriteLine("Starting basy connection");
            string[] lineNum = basy.getLoop(22);
            */
            Stopwatch stopWatch = System.Diagnostics.Stopwatch.StartNew();
            
            // cycle through each channel and request the info
            for (int j = 0; j < 40; j++)
            {
                int actualChannel = 41;
                //Console.WriteLine("j = " + j);
                
                // first get the line number
                string[] lineNum = basy.getLine(j);
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
                string[] loopNum = basy.getLoop(j);
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

            // finally update the values in each PeltierTCP instance
            updateServerVariables();

            // now report the time to the UI
            double timeDouble = stopWatch.ElapsedMilliseconds / 1000;
            string time = timeDouble.ToString();
            basyReqTime.Invoke((MethodInvoker)delegate
            { basyReqTime.Text = time; });
            
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
                // first fire the time to initialise it then start it
                requestFromBasy(source, e);
                requestFromBasyTimer.Start();
            }
        }
        /*
         *      PELTIER SERVER CONNECTIONS
         */
        private void updateServerVariables()
        {
            var peltierInstances = new[]
            {
                peltierTCP_1,
                peltierTCP_2,
                peltierTCP_3,
                peltierTCP_4,
                peltierTCP_5,
                peltierTCP_6
            };

            // now cycle through each instance and update them one by one
            foreach (peltierTCP instance in peltierInstances)
            {
                instance.updateBasyStats(basyCurrentStats);
            }
        }
        private void updateServerIndicators()
        {
            var peltierInstances = new[]
            {
                serverConnect1,
                serverConnect2,
                serverConnect3,
                serverConnect4,
                serverConnect5,
                serverConnect6
            };

            var indicators = new[]
            {
                portStatus1,
                portStatus2,
                portStatus3,
                portStatus4,
                portStatus5,
                portStatus6
            };

            int i = 0;
            foreach (Label indicator in indicators)
            {
                if (peltierInstances[i].Text == "Open")
                {
                    indicator.Invoke((MethodInvoker)delegate
                    {
                        indicator.BackColor = Color.FromName("Coral");
                        indicator.Text = "Server Port Closed";
                    });
                }
                else
                {
                    indicator.Invoke((MethodInvoker)delegate
                    {
                        indicator.BackColor = Color.FromArgb(255, 0, 210, 100);
                        indicator.Text = "Server Port Open";
                        
                    });
                }
                i++;
            }
        }

        /*
        *  BUTTONS
        */
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
        private void Button1_Click(object sender, EventArgs e)
        {
            if (serverConnect1.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_1.InitialiseServer(serverPortInput1.Text, serverIPInput1.Text);
                serverConnect1.Text = "Awaiting Connection";
                serverConnect1.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_1.CloseServer();
                serverConnect1.Text = "Open";
            }
        }
        private void ServerConnect2_Click(object sender, EventArgs e)
        {
            if (serverConnect2.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_2.InitialiseServer(serverPortInput2.Text, IPInput2.Text);
                serverConnect2.Text = "Awaiting Connection";
                serverConnect2.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_2.CloseServer();
                serverConnect2.Text = "Open";
            }
        }
        private void ServerConnect3_Click(object sender, EventArgs e)
        {
            if (serverConnect3.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_3.InitialiseServer(serverPortInput3.Text, IPInput3.Text);
                serverConnect3.Text = "Awaiting Connection";
                serverConnect3.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_3.CloseServer();
                serverConnect3.Text = "Open";
            }
        }
        private void ServerConnect4_Click(object sender, EventArgs e)
        {
            if (serverConnect4.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_4.InitialiseServer(serverPortInput4.Text, IPInput4.Text);
                serverConnect4.Text = "Awaiting Connection";
                serverConnect4.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_4.CloseServer();
                serverConnect4.Text = "Open";
            }
        }
        private void ServerConnect5_Click(object sender, EventArgs e)
        {
            if (serverConnect5.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_5.InitialiseServer(serverPortInput5.Text, IPInput5.Text);
                serverConnect5.Text = "Awaiting Connection";
                serverConnect5.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_5.CloseServer();
                serverConnect5.Text = "Open";
            }
        }
        private void ServerConnect6_Click(object sender, EventArgs e)
        {
            if (serverConnect6.Text == "Open")
            {
                //InitialiseServer();
                peltierTCP_6.InitialiseServer(serverPortInput6.Text, IPInput6.Text);
                serverConnect6.Text = "Awaiting Connection";
                serverConnect6.Enabled = false;
            }
            else
            {
                //CloseServer();
                peltierTCP_6.CloseServer();
                serverConnect6.Text = "Open";
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            loadForm();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeForm();
        }
        private void Label43_Click(object sender, EventArgs e)
        {

        }
        private void ConnectBasy_Click(object sender, EventArgs e)
        {

        }


    }
}
