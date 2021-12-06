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
    class peltierTCP
    {
        // all intialisation done here
        private int portInput;
        private string serverIpAd;
        private bool serverOpen = false;

        TextBox svrLog;
        Button serverConnect;
        String[,] basyCurStats = new String[41, 2]; // spare row to store bad data

        // constructor for the class
        public peltierTCP(TextBox tbox, Button connect)
        {
            svrLog = tbox;  // pass in sererLog textbox
            serverConnect = connect;
        }

        private void appendToSvrLog(string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");     // creates a timestamp for when the message was printed to the messagelog

            // checks to see if the serverlog is on a seperate thread 
            if (svrLog.InvokeRequired)
            {
                svrLog.Invoke((MethodInvoker)delegate
                {
                    // Running on the UI thread
                    svrLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
                });
            }
            else
            {
                svrLog.AppendText(timestamp + " => " + message + System.Environment.NewLine);
            }
        }
        public void InitialiseServer(string port, string IP)
        {
            try
            {
                portInput = Convert.ToInt32(port);
                serverIpAd = IP;
            }
            catch
            {
                appendToSvrLog("Are port and IP in the right format");
            }
            appendToSvrLog("Creating a thread at, IP:" + IP + " and port:" + port);

            // start a thread for updating the windows form screen every second. Used for logging data too
            Thread serverThread = new Thread(new ThreadStart(openServer));
            serverThread.IsBackground = true;
            serverThread.Start();

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

                appendToSvrLog("Waiting for a connection...");
                serverOpen = true;

                // awaiting connection
                Socket handler = listener.Accept();

                // connection established
                appendToSvrLog("Connection Established");
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
                    int indexNumber = Convert.ToInt32(chanNum);
                    //appendToServerLog("Text received:" + data);
                    //appendToServerLog("Channel #:" + chanNum);

                    string line = "0";
                    string loop = "0";
                    try
                    {
                        line = basyCurStats[indexNumber, 0];
                        loop = basyCurStats[indexNumber, 1];
                    }
                    catch (NullReferenceException)
                    {
                        Console.WriteLine("Info requested from blank line");
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine("Out of range, channel #: " + chanNum);
                    }

                    Console.WriteLine("Requested channel number:" + chanNum);
                    Console.WriteLine("Index number:" + indexNumber);
                    Console.WriteLine("Line:" + line);
                    Console.WriteLine("Loop:" + loop);

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
                appendToSvrLog("Disconnected");

                serverConnect.Invoke((MethodInvoker)delegate
                {
                    serverConnect.Text = "Open";
                    serverConnect.Enabled = true;
                });
                serverOpen = false;
            }
        }
        public void CloseServer()
        {
            serverOpen = false;
            Thread.Sleep(500);
        }
        public void updateBasyStats(String[,] newStats)
        {
            basyCurStats = newStats;
        }
    }
    
}
