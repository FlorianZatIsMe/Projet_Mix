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
        private const int PhasesNumber = 10;
        private const int ControlNumber = 34;
        private const int IdProgramName = 0;
        private const int IdAcceleration = 1;
        private const int IdDeceleration = 2;
        private const int IdSCurve = 3;
        private const int IdSpeed00 = 4;
        private const int IdSpeed01 = 5;
        private const int IdSpeed02 = 6;
        private const int IdSpeed03 = 7;
        private const int IdSpeed04 = 8;
        private const int IdSpeed05 = 9;
        private const int IdSpeed06 = 10;
        private const int IdSpeed07 = 11;
        private const int IdSpeed08 = 12;
        private const int IdSpeed09 = 13;
        private const int IdTime00 = 14;
        private const int IdTime01 = 15;
        private const int IdTime02 = 16;
        private const int IdTime03 = 17;
        private const int IdTime04 = 18;
        private const int IdTime05 = 19;
        private const int IdTime06 = 20;
        private const int IdTime07 = 21;
        private const int IdTime08 = 22;
        private const int IdTime09 = 23;
        private const int IdPression00 = 24;
        private const int IdPression01 = 25;
        private const int IdPression02 = 26;
        private const int IdPression03 = 27;
        private const int IdPression04 = 28;
        private const int IdPression05 = 29;
        private const int IdPression06 = 30;
        private const int IdPression07 = 31;
        private const int IdPression08 = 32;
        private const int IdPression09 = 33;

        private Frame parentFrame;
        private readonly WrapPanel[] wrapPanels = new WrapPanel[PhasesNumber];
        private readonly CheckBox[] checkBoxes = new CheckBox[PhasesNumber];
        private readonly TextBox[] speeds = new TextBox[PhasesNumber];
        private readonly TextBox[] times = new TextBox[PhasesNumber];
        private readonly TextBox[] pressures = new TextBox[PhasesNumber];
        private bool[] FormatControl = new bool[ControlNumber];
        //private General g = new General();

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

            FormatControl[IdSCurve] = true;     
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

            FormatControl[IdSCurve] = true;
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
            int id = -1;// = int.Parse(checkbox.Name.Substring(checkbox.Name.Length - 2, 2)) + 1;

            for (int i = 1; i < PhasesNumber; i++)
            {
                if (checkbox == checkBoxes[i])
                {
                    id = i + 1;
                }
            }

            try
            {
                speeds[id - 1].IsEnabled = false;
                times[id - 1].IsEnabled = false;
                pressures[id - 1].IsEnabled = false;

                speeds[id - 1].Text = "";
                times[id - 1].Text = "";
                pressures[id - 1].Text = "";

                FormatControl[IdSpeed00 + id - 1] = false;
                FormatControl[IdTime00 + id - 1] = false;
                FormatControl[IdPression00 + id - 1] = false;

                if (id != 10)
                {
                    wrapPanels[id].Visibility = Visibility.Collapsed;
                    checkBoxes[id].IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void cbPhase_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            int id = -1;// = int.Parse(checkbox.Name.Substring(checkbox.Name.Length - 2, 2)) + 1;

            for (int i = 1; i < PhasesNumber; i++)
            {
                if (checkbox == checkBoxes[i])
                {
                    id = i + 1;
                }
            }

            try
            {
                speeds[id - 1].IsEnabled = true;
                times[id - 1].IsEnabled = true;
                pressures[id - 1].IsEnabled = true;

                if (id != 10)
                {
                    speeds[id].IsEnabled = false;
                    times[id].IsEnabled = false;
                    pressures[id].IsEnabled = false;
                    wrapPanels[id].Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

            tbProgramName_LostFocus(tbProgramName, new RoutedEventArgs());
            tbAcceleration_LostFocus(tbAcceleration, new RoutedEventArgs());
            tbDeceleration_LostFocus(tbDeceleration, new RoutedEventArgs());
            tbSCurve_LostFocus(tbSCurve, new RoutedEventArgs());

            i = 0;
            while (i != 10 && array[12 + 3 * i] != "")
            {
                speeds[i].Text = array[12 + 3 * i];
                times[i].Text = array[13 + 3 * i];
                pressures[i].Text = array[14 + 3 * i];

                if (i > 0)
                {
                    checkBoxes[i].IsChecked = true;
                }

                tbSpeed_LostFocus(speeds[i], new RoutedEventArgs());
                tbTime_LostFocus(times[i], new RoutedEventArgs());
                tbPression_LostFocus(pressures[i], new RoutedEventArgs());

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
        private void tbProgramName_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdProgramName;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: false, 30))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void tbAcceleration_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdAcceleration;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 2000))
            {
                FormatControl[i] = true;

            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void tbDeceleration_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdDeceleration;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 2000))
            {
                FormatControl[i] = true;

            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void tbSCurve_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdSCurve;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: false, 30))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void tbSpeed_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdSpeed00;

            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == speeds[j])
                {
                    if (j == 0 || (bool)checkBoxes[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 2000))
                        {
                            FormatControl[i + j] = true;

                        }
                        else
                        {
                            FormatControl[i + j] = false;
                        }
                    }
                }
            }
        }
        private void tbTime_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdTime00;

            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == times[j])
                {
                    if (j == 0 || (bool)checkBoxes[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 2000))
                        {
                            FormatControl[i + j] = true;

                        }
                        else
                        {
                            FormatControl[i + j] = false;
                        }
                    }
                }
            }
        }
        private void tbPression_LostFocus(object sender, RoutedEventArgs e)
        {
            //pressures[0] = tbPression00;
            TextBox textBox = sender as TextBox;
            int i = IdPression00;

            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == pressures[j])
                {
                    if (j == 0 || (bool)checkBoxes[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 2000))
                        {
                            FormatControl[i + j] = true;

                        }
                        else
                        {
                            FormatControl[i + j] = false;
                        }
                    }
                }
            }
        }
        public bool IsFormatOk()
        {
            int n = 0;
            int x = 0;

            for (int i = 0; i < ControlNumber; i++)
            {
                n += FormatControl[i] ? 1 : 0;
            }

            for (int i = 1; i < PhasesNumber; i++)
            {
                x += (bool)checkBoxes[i].IsChecked ? 0 : 3; // Pour chaque checkbox décoché, on ajoutera 3 au score final
            }

            return (n + x) == ControlNumber;
        }
    }
}
