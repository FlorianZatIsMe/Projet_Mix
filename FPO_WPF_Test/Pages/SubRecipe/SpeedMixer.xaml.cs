using System;
using System.Collections.Generic;
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
using static FPO_WPF_Test.Pages.Recipe;

namespace FPO_WPF_Test.Pages.SubRecipe
{
    /// <summary>
    /// Logique d'interaction pour SpeedMixer.xaml
    /// </summary>
    public partial class SpeedMixer : Page
    {
        private Frame parentFrame;
        private readonly WrapPanel[] wrapPanels = new WrapPanel[10];
        private readonly CheckBox[] checkBoxes = new CheckBox[10];
        private readonly TextBox[] speeds = new TextBox[10];
        private readonly TextBox[] times = new TextBox[10];
        private readonly TextBox[] pressures = new TextBox[10];
        public SpeedMixer()
        {
            InitializeComponent();

            wrapPanels[0] = Phase00;
            wrapPanels[1] = Phase01;
            wrapPanels[2] = Phase02;
            wrapPanels[3] = Phase03;
            wrapPanels[4] = Phase04;
            wrapPanels[5] = Phase05;
            wrapPanels[6] = Phase06;
            wrapPanels[7] = Phase07;
            wrapPanels[8] = Phase08;
            wrapPanels[9] = Phase09;

            checkBoxes[1] = cbPhase01;
            checkBoxes[2] = cbPhase02;
            checkBoxes[3] = cbPhase03;
            checkBoxes[4] = cbPhase04;
            checkBoxes[5] = cbPhase05;
            checkBoxes[6] = cbPhase06;
            checkBoxes[7] = cbPhase07;
            checkBoxes[8] = cbPhase08;
            checkBoxes[9] = cbPhase09;

            speeds[0] = tbSpeed00;
            speeds[1] = tbSpeed01;
            speeds[2] = tbSpeed02;
            speeds[3] = tbSpeed03;
            speeds[4] = tbSpeed04;
            speeds[5] = tbSpeed05;
            speeds[6] = tbSpeed06;
            speeds[7] = tbSpeed07;
            speeds[8] = tbSpeed08;
            speeds[9] = tbSpeed09;

            times[0] = tbTime00;
            times[1] = tbTime01;
            times[2] = tbTime02;
            times[3] = tbTime03;
            times[4] = tbTime04;
            times[5] = tbTime05;
            times[6] = tbTime06;
            times[7] = tbTime07;
            times[8] = tbTime08;
            times[9] = tbTime09;

            pressures[0] = tbPression00;
            pressures[1] = tbPression01;
            pressures[2] = tbPression02;
            pressures[3] = tbPression03;
            pressures[4] = tbPression04;
            pressures[5] = tbPression05;
            pressures[6] = tbPression06;
            pressures[7] = tbPression07;
            pressures[8] = tbPression08;
            pressures[9] = tbPression09;
        }
        public SpeedMixer(Frame frame, string seqNumber)
        {
            parentFrame = frame;
            InitializeComponent();
            tbSeqNumber.Text = seqNumber;

            wrapPanels[0] = Phase00;
            wrapPanels[1] = Phase01;
            wrapPanels[2] = Phase02;
            wrapPanels[3] = Phase03;
            wrapPanels[4] = Phase04;
            wrapPanels[5] = Phase05;
            wrapPanels[6] = Phase06;
            wrapPanels[7] = Phase07;
            wrapPanels[8] = Phase08;
            wrapPanels[9] = Phase09;

            checkBoxes[1] = cbPhase01;
            checkBoxes[2] = cbPhase02;
            checkBoxes[3] = cbPhase03;
            checkBoxes[4] = cbPhase04;
            checkBoxes[5] = cbPhase05;
            checkBoxes[6] = cbPhase06;
            checkBoxes[7] = cbPhase07;
            checkBoxes[8] = cbPhase08;
            checkBoxes[9] = cbPhase09;

            speeds[0] = tbSpeed00;
            speeds[1] = tbSpeed01;
            speeds[2] = tbSpeed02;
            speeds[3] = tbSpeed03;
            speeds[4] = tbSpeed04;
            speeds[5] = tbSpeed05;
            speeds[6] = tbSpeed06;
            speeds[7] = tbSpeed07;
            speeds[8] = tbSpeed08;
            speeds[9] = tbSpeed09;

            times[0] = tbTime00;
            times[1] = tbTime01;
            times[2] = tbTime02;
            times[3] = tbTime03;
            times[4] = tbTime04;
            times[5] = tbTime05;
            times[6] = tbTime06;
            times[7] = tbTime07;
            times[8] = tbTime08;
            times[9] = tbTime09;

            pressures[0] = tbPression00;
            pressures[1] = tbPression01;
            pressures[2] = tbPression02;
            pressures[3] = tbPression03;
            pressures[4] = tbPression04;
            pressures[5] = tbPression05;
            pressures[6] = tbPression06;
            pressures[7] = tbPression07;
            pressures[8] = tbPression08;
            pressures[9] = tbPression09;
        }

        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = new Weight(parentFrame, tbSeqNumber.Text);
        }

        public void setSeqNumber(string n)
        {
            tbSeqNumber.Text = n;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = null;
        }

        private void cbPhase_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            int id = int.Parse(checkbox.Name.Substring(checkbox.Name.Length - 2, 2)) + 1;

            if (id != 10)
            {
                wrapPanels[id].Visibility = Visibility.Collapsed;
                checkBoxes[id].IsChecked = false;
            }
        }

        private void cbPhase_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            int id = int.Parse(checkbox.Name.Substring(checkbox.Name.Length - 2, 2)) + 1;

            if (id != 10)
            {
                wrapPanels[id].Visibility = Visibility.Visible;
            }
        }

        public void SetPage(string[] array)
        {
            int i;

            tbProgramName.Text = array[3];
            tbAcceleration.Text = array[4];
            tbDeceleration.Text = array[5];
            cbVacuum.IsChecked = array[6] == "True";
            if (array[7] == "True") rbNitrogen.IsChecked = true;
            cbMonitorType.IsChecked = array[8] == "True";
            cbxPressureUnit.Text = array[9];
            tbSCurve.Text = array[10];
            cbColdTrap.IsChecked = array[11] == "True";

            i = 0;
            while (i != 10 && array[12 + 3 * i] != "")
            {
                speeds[i].Text = array[12 + 3 * i];
                times[i].Text = array[13 + 3 * i];
                pressures[i].Text = array[14 + 3 * i];

                if (i > 0 && i < 9)
                {
                    checkBoxes[i].IsChecked = true;
                }
                i++;
            }
        }

        public string[] GetPage()
        {
            int i;
            int n = 1;
            string[] array = new string[42 - n];

            array[3 - n] = tbProgramName.Text;
            array[4 - n] = tbAcceleration.Text;
            array[5 - n] = tbDeceleration.Text;
            array[6 - n] = (bool)cbVacuum.IsChecked ? "1" : "0";
            array[7 - n] = (bool)rbNitrogen.IsChecked ? "1" : "0";
            array[8 - n] = (bool)cbMonitorType.IsChecked ? "1" : "0";
            array[9 - n] = cbxPressureUnit.Text;
            array[10 - n] = tbSCurve.Text;
            array[11 - n] = (bool)cbColdTrap.IsChecked ? "1" : "0";

            i = 0;
            do
            {
                array[12 - n + 3 * i] = speeds[i].Text;
                array[13 - n + 3 * i] = times[i].Text;
                array[14 - n + 3 * i] = pressures[i].Text;
                i++;
            } while (i != 10 && (bool)checkBoxes[i].IsChecked);

            return array;
        }
    }
}
