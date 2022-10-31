using Alarm_Management;
using Database;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Driver.RS232.Pump
{
    public static class RS232Pump
    {
        private static readonly SerialPort pump;
        private static string data;
        private static string lastCommand;
        private static bool isFree;
        //private static MyDatabase db = new MyDatabase();
        private readonly static int nAlarms = 1;
        public static bool[] areAlarmActive = new bool[nAlarms];
        //private readonly static bool[] wereAlarmActive = new bool[nAlarms];
        //private readonly static Task taskAlarmScan;
        private static bool isRS232Active;
        private static readonly System.Timers.Timer scanAlarmTimer;
        private static readonly AlarmManagement alarmManagement;

        static RS232Pump()
        {
            isRS232Active = false;

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

            pump.DataReceived += new SerialDataReceivedEventHandler(RecivedData);
            isFree = true;

            //taskAlarmScan = Task.Factory.StartNew(() => ScanAlarms());
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
                AlarmManagement.NewAlarm(2, 0);
                areAlarmActive[0] = true;
            }
            else if (IsOpen() && areAlarmActive[0])
            {
                alarmManagement.InactivateAlarm(2, 0);
                areAlarmActive[0] = false;
            }

            if (isRS232Active && !IsOpen())
            {
                Open();
            }

            //MessageBox.Show("Salut");
            scanAlarmTimer.Enabled = true;
        }
        private static async void ScanAlarms()
        {
            while (true)
            {
                if (isRS232Active && !IsOpen() && !areAlarmActive[0])
                {
                    AlarmManagement.NewAlarm(2, 0);
                    areAlarmActive[0] = true;
                }
                else if (IsOpen() && areAlarmActive[0])
                {
                    alarmManagement.InactivateAlarm(2, 0);
                    areAlarmActive[0] = false;
                }
                
                if (isRS232Active && !IsOpen())
                {
                    Open();
                }
                await Task.Delay(1000);
            }
        }
        public static void Initialize()
        {
            Open();
            isRS232Active = true;
        }
        public static void BlockUse() { isFree = false; }
        public static void FreeUse() { isFree = true; }
        public static bool IsFree() { return isFree; }
        public static void Open()
        {
            try { pump.Open(); }
            catch (Exception) { }
        }
        public static bool IsOpen()
        {
            return pump.IsOpen;
        }
        public static void SetCommand(string command)
        {
            try
            {
                if (!isFree)
                {
                    lastCommand = command;
                    pump.WriteLine(command);
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Connexion bloqué");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static string GetData()
        {
            return data;
        }
        private static void RecivedData(object sender, SerialDataReceivedEventArgs e)
        {
            data = pump.ReadLine();

            if (lastCommand == "!C802 0" || lastCommand == "!C802 1")
            {
                // Il va falloir faire quelque chose s'il y une erreur: data != *C802 0
                //MessageBox.Show(data);
            }
        }
    }
}
