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
        private static MyDatabase db = new MyDatabase();
        private static int nAlarms = 1;
        public static bool[] areAlarmActive = new bool[nAlarms];
        private static bool[] wereAlarmActive = new bool[nAlarms];
        private static Task taskAlarmScan;
        private static bool isRS232Active;

        static RS232Pump()
        {
            isRS232Active = false;

            pump = new SerialPort();
            pump.BaudRate = 9600;
            pump.DataBits = 8;
            pump.Parity = Parity.None;
            //pump.StopBits = StopBits.One;
            pump.Handshake = Handshake.XOnXOff;
            pump.NewLine = "\r";
            pump.PortName = "COM2";

            pump.DataReceived += new SerialDataReceivedEventHandler(RecivedData);
            isFree = true;

            taskAlarmScan = Task.Factory.StartNew(() => scanAlarms());
        }
        private static async void scanAlarms()
        {
            while (true)
            {
                if (isRS232Active && !IsOpen() && !areAlarmActive[0])
                {
                    AlarmManagement.NewAlarm(AlarmManagement.alarms[2, 0]);
                    // db.NewAlarm("ALARM 02.01 - Connexion à la pompe à vide échouée");
                    areAlarmActive[0] = true;
                }
                else if (IsOpen() && areAlarmActive[0])
                {
                    AlarmManagement.InactivateAlarm(AlarmManagement.alarms[2, 0]);
                    //db.InactivateAlarm("ALARM 02.01 - Connexion à la pompe à vide échouée");
                    areAlarmActive[0] = false;
                }
                else if (isRS232Active && !IsOpen())
                {
                    try
                    {
                        pump.Open();
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                    }
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
            //MessageBox.Show("Bloqué");
        }
        public static void FreeUse()
        {
            isFree = true;
            //MessageBox.Show("Libéré");
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
                    pump.Open();
                    //pump.WriteLine("!C802 0");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                isRS232Active = true;
            }
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
