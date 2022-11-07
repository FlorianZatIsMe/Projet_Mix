using Alarm_Management;
using Database;
using Driver.RS232.Pump;
using DRIVER.RS232.Weight;
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
        //private MyDatabase db = new MyDatabase();
        //private readonly List<string[]> thisCycleInfo = new List<string[]>();
        //private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;

        public PreCycle(Frame frameMain_arg, Frame inputInfoCycleFrame)
        {
            frameMain = frameMain_arg;
            frameInfoCycle = inputInfoCycleFrame;
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            InitializeComponent();
            
            General.Update_RecipeNames(cbxProgramName, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PROD);
            MyDatabase.Disconnect();
        }
        private void FxOK(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Voulez-vous démarrer le cycle?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                General.StartCycle(ProgramIDs[cbxProgramName.SelectedIndex], tbOFnumber.Text, tbFinalWeight.Text, frameMain, frameInfoCycle, false);
            }
        }
        private void FxAnnuler(object sender, RoutedEventArgs e)
        {
            MyDatabase.Disconnect();
            frameMain.Content = new Status();
        }
        private void TbOFnumber_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;   

            if (e.Key == Key.Enter)
            {
                MessageBox.Show(RS232Weight.GetData());

                if (RS232Weight.IsFree())
                {
                    RS232Weight.BlockUse();
                    if (RS232Weight.IsOpen()) RS232Weight.Open();
                    //RS232Weight.SetCommand("@");
                    RS232Weight.SetCommand(textbox.Text);
                }
            }
        }
        private void TbFinalWeight_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                //MessageBox.Show(RS232Pump.GetData());
                RS232Pump.SetCommand(textbox.Text);
            }
        }
    }
}
