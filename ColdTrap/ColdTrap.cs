using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using NationalInstruments.DAQmx;

namespace Driver_ColdTrap
{
    public struct IniInfo
    {
        public Window Window;
    }
    public static class ColdTrap
    {
        private readonly static Task myTask;
        private readonly static DigitalSingleChannelReader myDigitalReader;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static IniInfo info;

        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");

            info = info_arg;
        }

        private static void ShowMessageBox(string message)
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
        }

        static ColdTrap()
        {
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
                ShowMessageBox("DaqException: " + exception.Message);
                //dispose task
                myTask.Dispose();
            }
        }
        public static bool IsTempOK()
        {
            try
            {
                bool[] readData;
                readData = myDigitalReader.ReadSingleSampleMultiLine();

                return !readData[0];
            }

            catch (DaqException exception)
            {
                //dispose task
                myTask.Dispose();
                ShowMessageBox("DaqException_2: " + exception.Message);
            }

            catch (IndexOutOfRangeException exception)
            {
                //dispose task
                myTask.Dispose();
                ShowMessageBox("Error: You must specify eight lines in the channel string (i.e., 0:7). " + exception.Message);
            }
            return false;
        }
    }
}
