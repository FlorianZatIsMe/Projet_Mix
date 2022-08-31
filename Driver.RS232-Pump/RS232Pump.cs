using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Driver.RS232.Pump
{
    public static class RS232Pump
    {
        private static SerialPort pump;
        private static string data;
        private static decimal weightValue;
        static RS232Pump()
        {
            pump = new SerialPort();
            pump.BaudRate = 9600;
            pump.DataBits = 8;
            pump.Parity = Parity.None;
            //pump.StopBits = StopBits.One;
            pump.Handshake = Handshake.XOnXOff;
            pump.NewLine = "\r";
            pump.PortName = "COM2";

            pump.DataReceived += new SerialDataReceivedEventHandler(RecivedData);
        }

        public static void Open()
        {
            if (!pump.IsOpen)
            {
                try
                {
                    pump.Open();
                    //MessageBox.Show("Pump Opened");
                    pump.WriteLine("!C802 0");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }


            //MessageBox.Show("Opened");
            //weight.Write("I4\n");// + ((char)(13)).ToString());// + ((char)(11)).ToString() ;
            //weight.WriteLine("I4");
            //weight.WriteLine("?S0");
        }

        public static bool IsOpen()
        {
            return pump.IsOpen;
        }

        public static void SetCommand(string command)
        {
            pump.WriteLine(command);
            //MessageBox.Show("Et maintenant ?");
            //pump.Write(command + "\r");
            /*
            pump.Write(command + "\n");
            MessageBox.Show("Et maintenant ???");*/
        }

        public static string GetData()
        {
            return data;
        }

        public static decimal GetWeight()
        {
            return weightValue;
        }

        private static void RecivedData(object sender, SerialDataReceivedEventArgs e)
        {//*
            //MessageBox.Show("Bonjour");
            data = pump.ReadLine();// (b, 0, 18);
            //*/



            /*
            byte[] b = new byte[30];
            int n;

            //MessageBox.Show("salut\rça va ?");
            n = pump.Read(b, 0, 30);

            char[] c = new char[n];

            for (int i = 0; i < n; i++)
            {
                c[i] = (char)b[i];
            }

            MessageBox.Show(n.ToString() + " - " + new string(c));
            //*/
        }
    }
}
