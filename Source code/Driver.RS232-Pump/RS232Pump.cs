using Alarm_Management;
using Database;
using Driver_RS232;
using Driver_RS232_Pump.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        private static Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static RS232Pump()
        {
            pump = new SerialPort
            {
                BaudRate = Settings.Default.BaudRate,
                DataBits = Settings.Default.DataBits,
                Parity = Settings.Default.Parity,
                //pump.StopBits = StopBits.One;
                Handshake = Settings.Default.Handshake,
                NewLine = config.AppSettings.Settings["PumpNewLine"].Value, // \r: LF (Line Feed)
                PortName = Settings.Default.PortName
            };

            rs232 = new RS232(pump, Settings.Default.Alarm_Connection_id1, Settings.Default.Alarm_Connection_id2, new SerialDataReceivedEventHandler(ReceivedData));
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
                rs232.SetCommand(Settings.Default.StartCommand);
                result = true;
            }
            return result;
        }
        public static bool StopPump()
        {
            bool result = false;
            if (!isFree)
            {
                rs232.SetCommand(Settings.Default.StopCommand);
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

            if (rs232.GetLastCommand() == Settings.Default.StartCommand || rs232.GetLastCommand() == Settings.Default.StopCommand)
            {
                // Il va falloir faire quelque chose s'il y une erreur: data != *C802 0
                //MyMessageBox.Show(data);
            }
        }
    }
}