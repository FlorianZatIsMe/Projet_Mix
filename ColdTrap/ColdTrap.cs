using Alarm_Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Message;
using NationalInstruments.DAQmx;

namespace Driver_ColdTrap
{/*
    public struct IniInfo
    {
        public Window Window;
    }*/
    public static class ColdTrap
    {
        private readonly static Task myTask;
        private readonly static DigitalSingleChannelReader myDigitalReader;
        private static bool isAlarmActive = false;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //private static IniInfo info;
        /*
        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");

            info = info_arg;
        }*/

        static ColdTrap()
        {
            logger.Debug("Start");
            try
            {
                //Create a task such that it will be disposed after
                //we are done using it.
                myTask = new Task();
                //Create channel
                myTask.DIChannels.CreateChannel(
                    "Dev1/port0/line0:7",
                    "myChannel",
                    ChannelLineGrouping.OneChannelForEachLine);

                myDigitalReader = new DigitalSingleChannelReader(myTask.Stream);
            }
            catch (DaqException exception)
            {
                logger.Error("DaqException: " + exception.Message);
                //dispose task
                myTask.Dispose();
                AlarmManagement.NewAlarm(3, 0);
                isAlarmActive = true;
            }
        }
        public static bool IsTempOK()
        {
            try
            {
                bool[] readData;
                readData = myDigitalReader.ReadSingleSampleMultiLine();

                if (isAlarmActive)
                {
                    AlarmManagement.InactivateAlarm(3, 0);
                    isAlarmActive = false;
                }

                return !readData[0];
            }

            catch (DaqException exception)
            {
                //dispose task
                myTask.Dispose();
                logger.Error("DaqException_2: " + exception.Message);
            }

            catch (IndexOutOfRangeException exception)
            {
                //dispose task
                myTask.Dispose();
                logger.Error("Error: You must specify eight lines in the channel string (i.e., 0:7). " + exception.Message);
            }

            if (!isAlarmActive)
            {
                AlarmManagement.NewAlarm(3, 0);
                isAlarmActive = true;
            }

            return false;
        }
    }
}
