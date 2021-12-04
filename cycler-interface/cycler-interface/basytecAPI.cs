using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Environment;

namespace WindowsFormsApp1
{
    class basytecAPI
    {
        Process cmd = new Process();

        public string ipAddress = "";
        public string portNumber = "";
        public string plinkpath = @"C:\Program Files\PuTTY\plink.exe";

        public bool connected = false;

        public bool  connectToBasy(string ipAdd, string port)
        {
            ipAddress = ipAdd;
            portNumber = port;

            string connectionString =
                "-raw " +
                ipAdd +
                " -P " +
                port;

            // add plink start info
            cmd.StartInfo.FileName = plinkpath;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;    // stops it from making a popup application
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.Arguments = connectionString;

            // try and start the plink application, catch any errors
            try
            {
                cmd.Start();
                Console.WriteLine("Establishing basy connection... ");
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Plink is not installed in the correct location.\nInstall in - " + plinkpath);
                Console.WriteLine("Plink is not installed in the correct location.");
                return false;
            }

        }
        public bool isConnected()
        {

            // check if the application has connected sucessfully
            if (cmd.HasExited)
            {
                MessageBox.Show("Application failed - Check Port Number");
                Console.WriteLine("Application failed - Check Port Number");

                return false;
            }
            else
            {
                string msg;
                // now check that the data is recieved correctly
                // write command to basytec and check that the return is in the same format
                try
                {
                    cmd.StandardInput.WriteLine("e");
                    msg = cmd.StandardOutput.ReadLine();
                }
                catch
                {
                    Console.WriteLine("Failed to connect");
                    return false;
                }
                
                // now check the message contents
                if (msg == null)
                {
                    MessageBox.Show("Application failed - Check IP Address");
                    Console.WriteLine("Application failed - Check IP Address");

                    return false;
                }

                if (msg[0] == 'E')
                {
                    // Basytec is connected sucessfully
                    Console.WriteLine("Connected... ");
                    return true;
                }
                MessageBox.Show("Application failed to open");
                Console.WriteLine("Application failed to open");

                return false;
            }
        }

        public string[] getLine(int channel)
        {
            String[] ans = new string[2];
            string chanStr = channel.ToString();

            // string to get line
            string lineRequest = "?L " + channel;
            string lineString = writeToPlink(lineRequest);
            
            // check for errors
            if (lineString == "E1")
            {
                ans[0] = chanStr;
                ans[1] = "E1";
                return ans;
            }
            if (lineString == "E2")
            {
                ans[0] = chanStr;
                ans[1] = "E2";
                return ans;
            }
            if (lineString == "")
            {
                ans[0] = chanStr;
                ans[1] = "E3";
                return ans;
            }

            // write code here to return the channel number
            string channelActual = "1";
            // find first space in line string
            int firstSpace = lineString.IndexOf(" ");
            

            // error here FormatException
            int intLine;
            try
            {
                intLine = Convert.ToInt32(lineString.Remove(0, firstSpace + 1)) + 1; // first add correction for line number
            }
            catch (FormatException)
            {
                Console.WriteLine("Line String in the wrong format:" + lineString);
                intLine = 0;
            }

            ans[0] = channelActual;
            ans[1] = Convert.ToString(intLine);

            /*
            Console.WriteLine("Line String: " + lineString);
            Console.WriteLine("Line String space: " + firstSpace);
            Console.WriteLine("Line String val: " + ans[0]);
            */

            return ans;
        }

        public string[] getLoop(int channel)
        {
            String[] ans = new string[3];
            string chanStr = channel.ToString();

            // string to get loop
            string loopRequest = "?p " + channel + " c";
            string loopString = writeToPlink(loopRequest);

            if (loopString == "E1")
            {
                ans[0] = chanStr;
                ans[1] = "E1";
                return ans;
            }
            if (loopString == "E2")
            {
                ans[0] = chanStr;
                ans[1] = "E2";
                return ans;
            }
            if (loopString == "")
            {
                ans[0] = chanStr;
                ans[1] = "E3";
                return ans;
            }

            // write code here to return the channel number
            string channelActual = "1";

            // find second space in loop string
            int secSpace = loopString.IndexOf(" ", loopString.IndexOf(" ") + 1);


            ans[0] = channelActual;
            ans[1] = loopString.Remove(0, secSpace + 1);

            /*
            Console.WriteLine("Loop String: " + loopString);
            Console.WriteLine("Loop String space: " + secSpace);
            Console.WriteLine("Loop String val: " + ans[1]);
            */

            return ans;
        }

        public string writeToPlink(string msg)
        {
            if (!cmd.HasExited) // don't do this if the process has exited
            {
                // writes the message to plink and then reads the response
                cmd.StandardInput.WriteLine(msg);
                msg = cmd.StandardOutput.ReadLine();

                // now check the message contents
                if (msg.Length == 0)
                {
                    // if the message is blank - read again. This is a workaround
                    msg = cmd.StandardOutput.ReadLine();
                }
                if (msg == null)
                {
                    return "";
                }
                if (msg.Length == 0)
                {
                    // if the message is still blank then it must be an error
                    return "E1";    // no message recieved
                }
                else if (msg[0] == 'E')
                {
                    //Console.Write("Error");
                    return "E2";    // invalid command
                }
                else
                {
                    //Console.WriteLine("Recieved: " + msg);
                    return msg;
                }
            }
            else 
            {
                return "";
            }
            
        }
        public void closeConnection()
        {
            try
            {
                cmd.StandardInput.WriteLine("exit");
                Console.WriteLine("Exiting...");
                //cmd.WaitForExit();
                Console.WriteLine("Exited");
            }
            catch
            {

            }
        }
    }
}
