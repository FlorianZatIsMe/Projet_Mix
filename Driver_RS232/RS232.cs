using Alarm_Management;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Driver_RS232
{
    public struct IniInfo
    {
        public Window Window;
    }

    public class RS232
    {
        private readonly SerialPort serialPort;
        //private string data;
        private string lastCommand;
        private readonly int nAlarms = 1;
        private bool[] areAlarmActive;
        private bool isRS232Active;
        private readonly System.Timers.Timer scanAlarmTimer;
        private readonly int alarmConnectId1;
        private readonly int alarmConnectId2;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //private static IniInfo info;

        /*        private static void ShowMessageBox(string message)
                {
                    if (info.Window != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(info.Window, message);
                        }));
                    }
                    else
                    {
                        MessageBox.Show(message);
                    }
                }*/

        public RS232(SerialPort serialPort_arg, int alarmConnectId1_arg, int alarmConnectId2_arg, SerialDataReceivedEventHandler target)
        {
            serialPort = serialPort_arg;
            alarmConnectId1 = alarmConnectId1_arg;
            alarmConnectId2 = alarmConnectId2_arg;

            areAlarmActive = new bool[nAlarms];
            isRS232Active = false;

            serialPort.DataReceived += target;

            // Initialisation des timers
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            scanAlarmTimer.Start();
        }
        private void ScanAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (isRS232Active && !IsOpen() && !areAlarmActive[0])
            {
                AlarmManagement.NewAlarm(alarmConnectId1, alarmConnectId2);
                areAlarmActive[0] = true;
            }
            else if (IsOpen() && areAlarmActive[0])
            {
                AlarmManagement.InactivateAlarm(alarmConnectId1, alarmConnectId2);
                areAlarmActive[0] = false;
            }

            if (isRS232Active && !IsOpen())
            {
                Open();
            }

            scanAlarmTimer.Enabled = true;
        }
        public void Initialize(/*IniInfo info_arg*/)
        {
            Open();
            //info = info_arg;
            isRS232Active = true;
        }
        private void Open()
        {
            try { serialPort.Open(); }
            catch (Exception) { }
        }
        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }
        public bool SetCommand(string command)
        {
            bool result = false;
            try
            {
                lastCommand = command;
                serialPort.WriteLine(command);
                result = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            return result;
        }
        public string GetLastCommand() { return lastCommand; }
    }
}
