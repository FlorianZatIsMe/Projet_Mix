using Alarm_Management;
using Database;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DRIVER.RS232.Weight
{
    public static class RS232Weight
    {
        private static SerialPort scaleConnection;
        private static string receivedData;
        private static decimal weight;
        private static bool isWeightStable;
        private static string lastCommand;
        private static bool isFree;
        private static MyDatabase db = new MyDatabase();
        private static int nAlarms = 1;
        public static bool[] areAlarmActive = new bool[nAlarms];
        private static bool[] wereAlarmActive = new bool[nAlarms];
        private static Task taskAlarmScan;
        private static bool isRS232Active;

        static RS232Weight()
        {
            isWeightStable = false;
            isRS232Active = false;

            scaleConnection = new SerialPort();
            scaleConnection.BaudRate = 9600;
            scaleConnection.DataBits = 8;
            scaleConnection.Parity = Parity.None;
            scaleConnection.StopBits = StopBits.One;
            scaleConnection.Handshake = Handshake.XOnXOff;
            scaleConnection.NewLine = "\n";
            scaleConnection.PortName = "COM6";

            lastCommand = "";

            scaleConnection.DataReceived += new SerialDataReceivedEventHandler(weightReceivedData);
            isFree = true;

            taskAlarmScan = Task.Factory.StartNew(() => scanAlarms());
        }
        private static async void scanAlarms()
        {
            while (true)
            {
                if (isRS232Active && !IsOpen() && !areAlarmActive[0])
                {
                    AlarmManagement.NewAlarm(0, 0);
                    //db.NewAlarm("ALARM 00.01 - Connexion à la balance échouée");
                    areAlarmActive[0] = true;
                }
                else if (IsOpen() && areAlarmActive[0])
                {
                    AlarmManagement.InactivateAlarm(0, 0);
                    //db.InactivateAlarm("ALARM 00.01 - Connexion à la balance échouée");
                    areAlarmActive[0] = false;
                }
                else
                {
                    //MessageBox.Show(isRS232Active.ToString() + IsOpen().ToString() + areAlarmActive[0].ToString());
                }
                await Task.Delay(1000);
            }
        }
        public static void BlockUse()
        {
            isFree = false;
        }
        public static void FreeUse()
        {
            isFree = true;
        }
        public static bool IsFree()
        {
            return isFree;
        }
        public static void Open()
        {
            if (!IsOpen())
            {
                try
                {
                    scaleConnection.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            isRS232Active = true;
        }
        public static bool IsOpen()
        {
            return scaleConnection.IsOpen;
        }
        public static void SetCommand(string command)
        {
            if (IsOpen())
            {
                if (!isFree)
                {
                    lastCommand = command;
                    scaleConnection.WriteLine(command);
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Connexion bloqué");
                }
            }
            else
            {
                MessageBox.Show("La balance n'est pas connectée");
            }
        }
        public static string GetData()
        {
            return receivedData;
        }
        public static decimal GetWeight()
        {
            return weight;
        }
        public static bool IsWeightStable()
        {
            return isWeightStable;
        }
        private static void weightReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            receivedData = scaleConnection.ReadLine();
            //MessageBox.Show(data);

            if (lastCommand == "SIR" || lastCommand == "S")
            {
                if (receivedData.StartsWith("S S"))
                {
                    weight = decimal.Parse(receivedData.Substring(3, 12));
                    isWeightStable = true;
                }
                else if (receivedData.StartsWith("S D"))
                {
                    weight = decimal.Parse(receivedData.Substring(3, 12));
                    isWeightStable = false;
                }
                else
                {
                    weight = -1;
                }
            }
            else
            {
                weight = -1;
            }
        }
    }
}
