using Alarm_Management;
using Database;
using Driver_RS232_Pump;
using DRIVER_RS232_Weight;
using FPO_WPF_Test.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FPO_WPF_Test.Pages.SubCycle
{
    public partial class PreCycle : Page
    {
        private readonly Frame frameMain = new Frame();
        private readonly Frame frameInfoCycle = new Frame();
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<string> ProgramIDs = new List<string>();

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PreCycle(Frame frameMain_arg, Frame inputInfoCycleFrame)
        {
            logger.Debug("Start");

            frameMain = frameMain_arg;
            frameInfoCycle = inputInfoCycleFrame;
            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            InitializeComponent();
            
            General.Update_RecipeNames(cbxProgramName, ProgramNames, ProgramIDs, RecipeStatus.PROD);
            //MyDatabase.Disconnect();
        }
        private void FxOK(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxOK");

            if (MessageBox.Show(Settings.Default.PreCycle_Request_StartCycle, Settings.Default.PreCycle_Request_StartCycle_Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                General.StartCycle(ProgramIDs[cbxProgramName.SelectedIndex], tbOFnumber.Text, tbFinalWeight.Text, frameMain, frameInfoCycle, false);
            }
        }
        private void FxAnnuler(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxAnnuler");

            //MyDatabase.Disconnect();
            frameMain.Content = new Status();
        }
        private void TbOFnumber_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("TbOFnumber_KeyDown");

            TextBox textbox = sender as TextBox;   

            if (e.Key == Key.Enter)
            {
                MessageBox.Show(RS232Weight.GetData());

                if (RS232Weight.rs232.IsFree())
                {
                    RS232Weight.rs232.BlockUse();
                    RS232Weight.rs232.SetCommand(textbox.Text);
                }
            }
        }
        private void TbFinalWeight_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("TbFinalWeight_KeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                RS232Pump.rs232.SetCommand(textbox.Text);
            }
        }
    }
}
