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
using System.Globalization;
using static FPO_WPF_Test.Pages.Recipe;
using Database;

namespace FPO_WPF_Test.Pages.SubRecipe
{
    /// <summary>
    /// Logique d'interaction pour SpeedMixer.xaml
    /// </summary>
    public partial class SpeedMixer : Page, ISubRecipe
    {
        //public int seqType { get; }

        private const int ElementNumber = 46; // Nombre de colonne dans la base de données de recette
        private const int PhasesNumber = 10; // Nombre maximum de phase pendant une séquence speedmixer
        private const int ControlNumber = 38;
        private const int IdProgramName = 0;
        private const int IdAcceleration = 1;
        private const int IdDeceleration = 2;
        private const int IdSCurve = 3;
        private const int IdSpeed00 = 4;/*
        private const int IdSpeed01 = 5;
        private const int IdSpeed02 = 6;
        private const int IdSpeed03 = 7;
        private const int IdSpeed04 = 8;
        private const int IdSpeed05 = 9;
        private const int IdSpeed06 = 10;
        private const int IdSpeed07 = 11;
        private const int IdSpeed08 = 12;
        private const int IdSpeed09 = 13;*/
        private const int IdTime00 = 14;/*
        private const int IdTime01 = 15;
        private const int IdTime02 = 16;
        private const int IdTime03 = 17;
        private const int IdTime04 = 18;
        private const int IdTime05 = 19;
        private const int IdTime06 = 20;
        private const int IdTime07 = 21;
        private const int IdTime08 = 22;
        private const int IdTime09 = 23;*/
        private const int IdPressure00 = 24;/*
        private const int IdPressure01 = 25;
        private const int IdPressure02 = 26;
        private const int IdPressure03 = 27;
        private const int IdPressure04 = 28;
        private const int IdPressure05 = 29;
        private const int IdPressure06 = 30;
        private const int IdPressure07 = 31;
        private const int IdPressure08 = 32;
        private const int IdPressure09 = 33;*/
        private const int IdSpeedMin = 34;
        private const int IdSpeedMax = 35;
        private const int IdPressureMin = 36;
        private const int IdPressureMax = 37;

        private readonly Frame parentFrame;
        private readonly WrapPanel[] wrapPanels = new WrapPanel[PhasesNumber];
        private readonly CheckBox[] checkBoxes = new CheckBox[PhasesNumber];
        private readonly TextBox[] speeds = new TextBox[PhasesNumber];
        private readonly TextBox[] times = new TextBox[PhasesNumber];
        private readonly TextBox[] pressures = new TextBox[PhasesNumber];
        private readonly bool[] FormatControl = new bool[ControlNumber];

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public SpeedMixer()
        {
            //seqType = 1;

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
        public void SetSeqNumber(string n)
        {
            tbSeqNumber.Text = n;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = null;
        }
        private void CbPhase_Unchecked(object sender, RoutedEventArgs e)
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
                FormatControl[IdPressure00 + id - 1] = false;

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
        private void CbPhase_Checked(object sender, RoutedEventArgs e)
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
        public void SetPage_2(string[] array)
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

            TbProgramName_LostFocus(tbProgramName, new RoutedEventArgs());
            TbAcceleration_LostFocus(tbAcceleration, new RoutedEventArgs());
            TbDeceleration_LostFocus(tbDeceleration, new RoutedEventArgs());
            TbSCurve_LostFocus(tbSCurve, new RoutedEventArgs());

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

                TbSpeed_LostFocus(speeds[i], new RoutedEventArgs());
                TbTime_LostFocus(times[i], new RoutedEventArgs());
                TbPression_LostFocus(pressures[i], new RoutedEventArgs());

                i++;
            }

            tbSpeedMin.Text = array[42];
            tbSpeedMax.Text = array[43];
            tbPressureMin.Text = array[44];
            tbPressureMax.Text = array[45];

            TbSpeedMin_LostFocus(tbSpeedMin, new RoutedEventArgs());
            TbSpeedMax_LostFocus(tbSpeedMax, new RoutedEventArgs());
            TbPressureMin_LostFocus(tbPressureMin, new RoutedEventArgs());
            TbPressureMax_LostFocus(tbPressureMax, new RoutedEventArgs());
        }
        public void SetPage(ISeqInfo seqInfo)
        {
            RecipeSpeedMixerInfo recipeSpeedMixerInfo = seqInfo as RecipeSpeedMixerInfo;
            int i;

            tbProgramName.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.seqName].value;
            tbAcceleration.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.acceleration].value;
            tbDeceleration.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.deceleration].value;
            cbVacuum.IsChecked = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.vaccum_control].value == DatabaseSettings.General_TrueValue;
            if (recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.isVentgasAir].value == DatabaseSettings.General_TrueValue) rbAir.IsChecked = true;
            cbMonitorType.IsChecked = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.monitorType].value == DatabaseSettings.General_TrueValue;
            cbxPressureUnit.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureUnit].value;
            tbSCurve.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.scurve].value;
            cbColdTrap.IsChecked = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.coldtrap].value == DatabaseSettings.General_TrueValue;

            TbProgramName_LostFocus(tbProgramName, new RoutedEventArgs());
            TbAcceleration_LostFocus(tbAcceleration, new RoutedEventArgs());
            TbDeceleration_LostFocus(tbDeceleration, new RoutedEventArgs());
            TbSCurve_LostFocus(tbSCurve, new RoutedEventArgs());

            i = 0;
            while (i != 10 && recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speed00 + 3 * i].value != "")
            {
                speeds[i].Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speed00 + 3 * i].value;
                times[i].Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * i].value;
                pressures[i].Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressure00 + 3 * i].value;

                if (i > 0)
                {
                    checkBoxes[i].IsChecked = true;
                }

                TbSpeed_LostFocus(speeds[i], new RoutedEventArgs());
                TbTime_LostFocus(times[i], new RoutedEventArgs());
                TbPression_LostFocus(pressures[i], new RoutedEventArgs());

                i++;
            }

            tbSpeedMin.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speedMin].value;
            tbSpeedMax.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speedMax].value;
            tbPressureMin.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureMin].value;
            tbPressureMax.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureMax].value;

            TbSpeedMin_LostFocus(tbSpeedMin, new RoutedEventArgs());
            TbSpeedMax_LostFocus(tbSpeedMax, new RoutedEventArgs());
            TbPressureMin_LostFocus(tbPressureMin, new RoutedEventArgs());
            TbPressureMax_LostFocus(tbPressureMax, new RoutedEventArgs());
        }
        public string[] GetPage_2()
        {
            int i;
            int n = 1;
            string[] array = new string[ElementNumber - n];

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
                array[12 - n + 3 * i] = int.Parse(speeds[i].Text, NumberStyles.AllowThousands).ToString();
                array[13 - n + 3 * i] = int.Parse(times[i].Text, NumberStyles.AllowThousands).ToString();
                array[14 - n + 3 * i] = int.Parse(pressures[i].Text, NumberStyles.AllowThousands).ToString();
                i++;
            } while (i != 10 && (bool)checkBoxes[i].IsChecked);

            array[42 - n] = int.Parse(tbSpeedMin.Text, NumberStyles.AllowThousands).ToString();
            array[43 - n] = int.Parse(tbSpeedMax.Text, NumberStyles.AllowThousands).ToString();
            array[44 - n] = int.Parse(tbPressureMin.Text, NumberStyles.AllowThousands).ToString();
            array[45 - n] = int.Parse(tbPressureMax.Text, NumberStyles.AllowThousands).ToString();

            return array;
        }
        public ISeqInfo GetPage()
        {
            RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();

            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.seqName].value = tbProgramName.Text;
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.acceleration].value = tbAcceleration.Text;
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.deceleration].value = tbDeceleration.Text;
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.vaccum_control].value = (bool)cbVacuum.IsChecked ? "1" : "0";
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.isVentgasAir].value = (bool)rbAir.IsChecked ? "1" : "0";
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.monitorType].value = (bool)cbMonitorType.IsChecked ? "1" : "0";
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureUnit].value = cbxPressureUnit.Text;
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.scurve].value = tbSCurve.Text;
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.coldtrap].value = (bool)cbColdTrap.IsChecked ? "1" : "0";

            int i = 0;
            do
            {
                // peut-être ajouter un try ici (et partout ailleurs, dès qu'on fait un Parse quoi)
                recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speed00 + 3 * i].value = int.Parse(speeds[i].Text, NumberStyles.AllowThousands).ToString();
                recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * i].value = int.Parse(times[i].Text, NumberStyles.AllowThousands).ToString();
                recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressure00 + 3 * i].value = int.Parse(pressures[i].Text, NumberStyles.AllowThousands).ToString();
                i++;
            } while (i != 10 && (bool)checkBoxes[i].IsChecked);

            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speedMin].value = int.Parse(tbSpeedMin.Text, NumberStyles.AllowThousands).ToString();
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.speedMax].value = int.Parse(tbSpeedMax.Text, NumberStyles.AllowThousands).ToString();
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureMin].value = int.Parse(tbPressureMin.Text, NumberStyles.AllowThousands).ToString();
            //recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureMax].value = int.Parse(tbPressureMax.Text, NumberStyles.AllowThousands).ToString();
            recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.pressureMax].value = tbPressureMax.Text;

            return recipeSpeedMixerInfo;
        }
        private void TbProgramName_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbAcceleration_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbDeceleration_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbSCurve_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbSpeed_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbTime_LostFocus(object sender, RoutedEventArgs e)
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
        private void TbPression_LostFocus(object sender, RoutedEventArgs e)
        {
            //pressures[0] = tbPression00;
            TextBox textBox = sender as TextBox;
            int i = IdPressure00;

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
        private void TbSpeedMin_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdSpeedMin;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: tbSpeedMax.Text == "" ? 1200 : decimal.Parse(tbSpeedMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbSpeedMax_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdSpeedMax;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: tbSpeedMin.Text == "" ? 0 : decimal.Parse(tbSpeedMin.Text), max: 1200))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbPressureMin_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdPressureMin;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: tbPressureMax.Text == "" ? 1000 : decimal.Parse(tbPressureMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbPressureMax_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdPressureMax;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: tbPressureMin.Text == "" ? 0 : decimal.Parse(tbPressureMin.Text), max: 1000))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
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
            //MessageBox.Show(n.ToString() + " + " + x.ToString() + " = " + (n+x).ToString());
            return (n + x) == ControlNumber;
        }
    }
}
