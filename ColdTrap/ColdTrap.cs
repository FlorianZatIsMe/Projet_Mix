using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using NationalInstruments.DAQmx;

namespace Driver.ColdTrap
{
    public class ColdTrap
    {
        private Task myTask;
        private DigitalSingleChannelReader myDigitalReader;
        public ColdTrap()
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




                try
                {
                    bool[] readData;

                    //                    boom = myDigitalReader.ReadSingleSampleSingleLine();
                    //                    MessageBox.Show(boom.ToString());

                    //Read the digital channel
                    readData = myDigitalReader.ReadSingleSampleMultiLine();
                    //MessageBox.Show(readData[1].ToString());
                    //MessageBox.Show(readData[2].ToString());
                }

                catch (DaqException exception)
                {
                    //dispose task
                    myTask.Dispose();
                    MessageBox.Show("DaqException_2: " + exception.Message);
                }

                catch (IndexOutOfRangeException exception)
                {
                    //dispose task
                    myTask.Dispose();
                    MessageBox.Show("Error: You must specify eight lines in the channel string (i.e., 0:7). " + exception.Message);
                }
            }
            catch (DaqException exception)
            {
                MessageBox.Show("DaqException: " + exception.Message);
                //dispose task
                myTask.Dispose();
            }
        }
    }
}
