using Alarm_Management;
using Database;
using Driver_RS232;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DRIVER_RS232_Weight
{
    public static class RS232Weight
    {
        public static RS232 rs232;
        private readonly static SerialPort scaleConnection;
        private static string receivedData;
        private static decimal weight;
        private static bool isWeightStable;

        static RS232Weight()
        {
            isWeightStable = false;

            scaleConnection = new SerialPort
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.XOnXOff,
                NewLine = "\n",
                PortName = "COM6"
            };

            rs232 = new RS232(scaleConnection, 0, 0, new SerialDataReceivedEventHandler(ReceivedData));
        }
        public static string GetData() { return receivedData; }
        public static decimal GetWeight() { return weight; }
        public static bool IsWeightStable() { return isWeightStable; }
        private static void ReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = sender as SerialPort;
            receivedData = port.ReadLine();

            if (rs232.GetLastCommand() == "SIR" || rs232.GetLastCommand() == "S")
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

/*
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

namespace DRIVER_RS232_Weight
{
    public static class RS232Weight
    {
        private readonly static SerialPort scaleConnection;
        private static string receivedData;
        private static decimal weight;
        private static bool isWeightStable;
        private static string lastCommand;
        private static bool isFree;
        //private static MyDatabase db = new MyDatabase();
        private readonly static int nAlarms = 1;
        public static bool[] areAlarmActive = new bool[nAlarms];
        //private readonly static bool[] wereAlarmActive = new bool[nAlarms];
        //private readonly static Task taskAlarmScan;
        private static bool isRS232Active;
        private static readonly System.Timers.Timer scanAlarmTimer;
        //private static readonly AlarmManagement alarmManagement = new AlarmManagement();

        static RS232Weight()
        {
            isWeightStable = false;
            isRS232Active = false;

            scaleConnection = new SerialPort
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.XOnXOff,
                NewLine = "\n",
                PortName = "COM6"
            };

            lastCommand = "";

            scaleConnection.DataReceived += new SerialDataReceivedEventHandler(WeightReceivedData);
            isFree = true;

            //taskAlarmScan = Task.Factory.StartNew(() => scanAlarms());
            //Task.Factory.StartNew(() => ScanAlarms());

            // Initialisation des timers
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };

            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            scanAlarmTimer.Start();
        }
        private static void ScanAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
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
                //Message.MyMessageBox.Show(isRS232Active.ToString() + IsOpen().ToString() + areAlarmActive[0].ToString());
            }

            if (isRS232Active && !IsOpen())
            {
                Open();
            }

            //Message.MyMessageBox.Show("Salut");
            scanAlarmTimer.Enabled = true;
        }
        public static void Initialize()
        {
            Open();
            isRS232Active = true;
        }
        private static async void ScanAlarms()
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
                    //Message.MyMessageBox.Show(isRS232Active.ToString() + IsOpen().ToString() + areAlarmActive[0].ToString());
                }

                if (isRS232Active && !IsOpen())
                {
                    Open();
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
            try { scaleConnection.Open(); }
            catch (Exception) { }
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
                    Message.MyMessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Connexion bloqué");
                }
            }
            else
            {
                Message.MyMessageBox.Show("La balance n'est pas connectée");
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
        private static void WeightReceivedData(object sender, SerialDataReceivedEventArgs e)
        {
            receivedData = scaleConnection.ReadLine();
            //Message.MyMessageBox.Show(data);

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

 */
