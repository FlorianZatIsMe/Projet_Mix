using Alarm_Management;
using Database;
using Driver_RS232;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Driver_RS232_Pump
{
    public static class RS232Pump
    {
        private static RS232 rs232;
        private static readonly SerialPort pump;
        private static string data;
        private static bool isFree;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static RS232Pump()
        {
            pump = new SerialPort
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                //pump.StopBits = StopBits.One;
                Handshake = Handshake.XOnXOff,
                NewLine = "\r",
                PortName = "COM2"
            };

            rs232 = new RS232(pump, 2, 0, new SerialDataReceivedEventHandler(ReceivedData));
            isFree = true;
        }
        public static void Initialize()
        {
            rs232.Initialize();
        }
        public static void BlockUse() { isFree = false; }
        public static void FreeUse() { isFree = true; }
        public static bool IsFree() { return isFree; }
        public static bool IsOpen()
        {
            return rs232.IsOpen();
        }
        public static bool StartPump()
        {
            bool result = false;
            if (!isFree)
            {
                rs232.SetCommand("!C802 1");
                result = true;
            }
            return result;
        }
        public static bool StopPump()
        {
            bool result = false;
            if (!isFree)
            {
                rs232.SetCommand("!C802 0");
                result = true;
            }
            return result;
        }
        public static string GetData()
        {
            return data;
        }
        private static void ReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = sender as SerialPort;

            data = port.ReadLine();

            if (rs232.GetLastCommand() == "!C802 0" || rs232.GetLastCommand() == "!C802 1")
            {
                // Il va falloir faire quelque chose s'il y une erreur: data != *C802 0
                //Message.MyMessageBox.Show(data);
            }
        }
    }
}