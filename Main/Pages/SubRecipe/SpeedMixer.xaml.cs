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
using static Main.Pages.Recipe;
using Database;
using Main.Properties;
using System.Configuration;
using System.Windows.Controls.Primitives;

namespace Main.Pages.SubRecipe
{
    public class SpeedMixerViewModel
    {
        public string TextBlockAcceleration { get; } = Settings.Default.RecipeMix_Acceleration_Label + " " + "[" + Settings.Default.RecipeMix_Acceleration_Min + " ; " + Settings.Default.RecipeMix_Acceleration_Max + "]";
        public string TextBlockDeceleration { get; } = Settings.Default.RecipeMix_Deceleration_Label + " " + "[" + Settings.Default.RecipeMix_Deceleration_Min + " ; " + Settings.Default.RecipeMix_Deceleration_Max + "]";
        public string TextBlockSpeedRange { get; } = "[" + Settings.Default.RecipeMix_Speed_Min + " ; " + Settings.Default.RecipeMix_Speed_Max + "]";
        //public string TextBlockSpeedMax { get; } = Settings.Default.RecipeMix_SpeedMax_Label + " " + "[" + Settings.Default.RecipeMix_Speed_Min + " ; " + Settings.Default.RecipeMix_Speed_Max + "]";
        public string TextBlockPressure { get; } = "[" + Settings.Default.RecipeMix_Pressure_Min + " ; " + Settings.Default.RecipeMix_Pressure_Max + "]";
        public string TextBlockTime { get; } = "[0 ; " + Settings.Default.RecipeMix_Time_Max + "]";
    }
    public partial class SpeedMixer : Page, ISubRecipe
    {
        RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();

        private readonly int PhasesNumber = Settings.Default.RecipeMix_MaxPhaseNumber; // Nombre maximum de phase pendant une séquence speedmixer
        private readonly int[] ControlsIDs;
        private readonly Frame parentFrame;
        private readonly WrapPanel[] wrapPanels = new WrapPanel[Settings.Default.RecipeMix_MaxPhaseNumber];
        private readonly ToggleButton[] toggleButtons = new ToggleButton[Settings.Default.RecipeMix_MaxPhaseNumber];
        private readonly TextBox[] speeds = new TextBox[Settings.Default.RecipeMix_MaxPhaseNumber];
        private readonly TextBox[] times = new TextBox[Settings.Default.RecipeMix_MaxPhaseNumber];
        private readonly TextBox[] pressures = new TextBox[Settings.Default.RecipeMix_MaxPhaseNumber];
        private readonly bool[] FormatControl = new bool[Settings.Default.RecipeMix_IdDBControls.list.Count];

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public SpeedMixer(Frame frame, string seqNumber)
        {
            logger.Debug("Start");

            ControlsIDs = new int[recipeSpeedMixerInfo.Ids.Count()];
            List<int> list = Settings.Default.RecipeMix_IdDBControls.list;
            int n = 0;

            for (int i = 0; i < ControlsIDs.Length; i++)
            {
                if (i == list[n])
                {
                    ControlsIDs[i] = n;
                    n++;
                }
                else
                {
                    ControlsIDs[i] = -1;
                }
                //logger.Trace(i.ToString() + ": " + ControlsIDs[i].ToString());
            }

            parentFrame = frame;
            InitializeComponent();
            tbSeqNumber.Text = seqNumber;

            toggleButtons[1] = tgPhase01;
            toggleButtons[2] = tgPhase02;
            toggleButtons[3] = tgPhase03;
            toggleButtons[4] = tgPhase04;
            toggleButtons[5] = tgPhase05;
            toggleButtons[6] = tgPhase06;
            toggleButtons[7] = tgPhase07;
            toggleButtons[8] = tgPhase08;
            toggleButtons[9] = tgPhase09;

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

            pressures[0] = tbPressure00;
            pressures[1] = tbPressure01;
            pressures[2] = tbPressure02;
            pressures[3] = tbPressure03;
            pressures[4] = tbPressure04;
            pressures[5] = tbPressure05;
            pressures[6] = tbPressure06;
            pressures[7] = tbPressure07;
            pressures[8] = tbPressure08;
            pressures[9] = tbPressure09;

            FormatControl[ControlsIDs[recipeSpeedMixerInfo.Scurve]] = true;
        }
        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            logger.Debug("RadioButton_Click_1");

            parentFrame.Content = new Weight(parentFrame, tbSeqNumber.Text);
        }
        public void SetSeqNumber(int n)
        {
            logger.Debug("SetSeqNumber");

            tbSeqNumber.Text = n.ToString();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Button_Click");

            parentFrame.Content = null;
        }
        private void tgPhase_Unchecked(object sender, RoutedEventArgs e)
        {
            logger.Debug("tgPhase_Unchecked");

            ToggleButton toggleButton = sender as ToggleButton;
            int id = -1;

            for (int i = 1; i < PhasesNumber; i++)
            {
                if (toggleButton == toggleButtons[i])
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

                FormatControl[ControlsIDs[recipeSpeedMixerInfo.Speed00] + id - 1] = false;
                FormatControl[ControlsIDs[recipeSpeedMixerInfo.Time00] + id - 1] = false;
                FormatControl[ControlsIDs[recipeSpeedMixerInfo.Pressure00] + id - 1] = false;

                if (id != 10)
                {
                    speeds[id].Visibility = Visibility.Collapsed;
                    times[id].Visibility = Visibility.Collapsed;
                    pressures[id].Visibility = Visibility.Collapsed;
                    //wrapPanels[id].Visibility = Visibility.Collapsed;
                    toggleButtons[id].Visibility = Visibility.Collapsed;
                    toggleButtons[id].IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                Message.MyMessageBox.Show(ex.Message);
            }
        }
        private void tgPhase_Checked(object sender, RoutedEventArgs e)
        {
            logger.Debug("tgPhase_Checked");

            ToggleButton toggleButton = sender as ToggleButton;
            int id = -1;

            for (int i = 1; i < PhasesNumber; i++)
            {
                if (toggleButton == toggleButtons[i])
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
                    speeds[id].Visibility = Visibility.Visible;
                    times[id].Visibility = Visibility.Visible;
                    pressures[id].Visibility = Visibility.Visible;
                    toggleButtons[id].Visibility = Visibility.Visible;
                    //wrapPanels[id].Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Message.MyMessageBox.Show(ex.Message);
            }
        }
        public void SetPage(object[] seqValues)
        {
            logger.Debug("SetPage");

            RecipeSpeedMixerInfo recipeInfo = new RecipeSpeedMixerInfo();
            int i;

            if (seqValues.Count() != recipeInfo.Ids.Count())
            {
                logger.Error("On a un problème");
                Message.MyMessageBox.Show("On a un problème");
                return;
            }

            tbProgramName.Text = seqValues[recipeInfo.Name].ToString();
            tbAcceleration.Text = seqValues[recipeInfo.Acceleration].ToString();
            tbDeceleration.Text = seqValues[recipeInfo.Deceleration].ToString();
            cbVacuum.IsChecked = seqValues[recipeInfo.Vaccum_control].ToString() == DatabaseSettings.General_TrueValue_Read;
            //if (recipeInfo.columns[recipeInfo.isVentgasAir].value == DatabaseSettings.General_TrueValue_Read) rbAir.IsChecked = true;
            //cbMonitorType.IsChecked = recipeInfo.columns[recipeInfo.monitorType].value == DatabaseSettings.General_TrueValue_Read;
            cbxPressureUnit.Text = seqValues[recipeInfo.PressureUnit].ToString();
            //tbSCurve.Text = recipeInfo.columns[recipeInfo.scurve].value;
            cbColdTrap.IsChecked = seqValues[recipeInfo.Coldtrap].ToString() == DatabaseSettings.General_TrueValue_Read;

            TbProgramName_LostFocus(tbProgramName, new RoutedEventArgs());
            TbAcceleration_LostFocus(tbAcceleration, new RoutedEventArgs());
            TbDeceleration_LostFocus(tbDeceleration, new RoutedEventArgs());
            TbSCurve_LostFocus(null, new RoutedEventArgs());
            //TbSCurve_LostFocus(tbSCurve, new RoutedEventArgs());

            i = 0;
            while (i != 10 && seqValues[recipeInfo.Speed00 + 3 * i] != null && seqValues[recipeInfo.Speed00 + 3 * i].ToString() != "")
            {
                speeds[i].Text = seqValues[recipeInfo.Speed00 + 3 * i].ToString();
                times[i].Text = seqValues[recipeInfo.Time00 + 3 * i].ToString();
                pressures[i].Text = seqValues[recipeInfo.Pressure00 + 3 * i].ToString();

                if (i > 0)
                {
                    toggleButtons[i].IsChecked = true;
                }

                TbSpeed_LostFocus(speeds[i], new RoutedEventArgs());
                TbTime_LostFocus(times[i], new RoutedEventArgs());
                tbPressure_LostFocus(pressures[i], new RoutedEventArgs());

                i++;
            }

            tbSpeedMin.Text = seqValues[recipeInfo.SpeedMin].ToString();
            tbSpeedMax.Text = seqValues[recipeInfo.SpeedMax].ToString();
            tbPressureMin.Text = seqValues[recipeInfo.PressureMin].ToString();
            tbPressureMax.Text = seqValues[recipeInfo.PressureMax].ToString();

            TbSpeedMin_LostFocus(tbSpeedMin, new RoutedEventArgs());
            TbSpeedMax_LostFocus(tbSpeedMax, new RoutedEventArgs());
            TbPressureMin_LostFocus(tbPressureMin, new RoutedEventArgs());
            TbPressureMax_LostFocus(tbPressureMax, new RoutedEventArgs());
        }
        public ISeqTabInfo GetPage()
        {
            logger.Debug("GetPage");

            RecipeSpeedMixerInfo recipeInfo = new RecipeSpeedMixerInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];

            try
            {
                recipeValues[recipeInfo.Name] = tbProgramName.Text;
                recipeValues[recipeInfo.Acceleration] = int.Parse(tbAcceleration.Text, NumberStyles.AllowThousands).ToString();
                recipeValues[recipeInfo.Deceleration] = int.Parse(tbDeceleration.Text, NumberStyles.AllowThousands).ToString();
                recipeValues[recipeInfo.Vaccum_control] = (bool)cbVacuum.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.IsVentgasAir] = DatabaseSettings.General_TrueValue_Write;
                //UrecipeValues[recipeInfo.isVentgasAir] = (bool)rbAir.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.MonitorType] = DatabaseSettings.General_TrueValue_Write;
                //recipeValues[recipeInfo.monitorType] = (bool)cbMonitorType.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.PressureUnit] = cbxPressureUnit.Text;
                recipeValues[recipeInfo.Scurve] = "-";// tbSCurve.Text;
                recipeValues[recipeInfo.Coldtrap] = (bool)cbColdTrap.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;

                int i = 0;
                do
                {
                    recipeValues[recipeInfo.Speed00 + 3 * i] = int.Parse(speeds[i].Text, NumberStyles.AllowThousands).ToString();
                    recipeValues[recipeInfo.Time00 + 3 * i] = int.Parse(times[i].Text, NumberStyles.AllowThousands).ToString();
                    recipeValues[recipeInfo.Pressure00 + 3 * i] = int.Parse(pressures[i].Text, NumberStyles.AllowThousands).ToString();
                    i++;
                } while (i != 10 && (bool)toggleButtons[i].IsChecked);

                recipeValues[recipeInfo.SpeedMin] = int.Parse(tbSpeedMin.Text, NumberStyles.AllowThousands).ToString();
                recipeValues[recipeInfo.SpeedMax] = int.Parse(tbSpeedMax.Text, NumberStyles.AllowThousands).ToString();
                recipeValues[recipeInfo.PressureMin] = int.Parse(tbPressureMin.Text, NumberStyles.AllowThousands).ToString();
                recipeValues[recipeInfo.PressureMax] = int.Parse(tbPressureMax.Text, NumberStyles.AllowThousands).ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Message.MyMessageBox.Show(ex.Message);
                recipeInfo = null;
            }

            return recipeInfo;
        }
        public ISeqTabInfo GetRecipeInfo()
        {
            logger.Debug("GetRecipeInfo");
            return new RecipeSpeedMixerInfo();
        }
        public object[] GetRecipeValues()
        {
            logger.Debug("GetRecipeValues");

            RecipeSpeedMixerInfo recipeInfo = new RecipeSpeedMixerInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];

            try
            {
                recipeValues[recipeInfo.Name] = tbProgramName.Text;
                recipeValues[recipeInfo.Acceleration] = int.Parse(tbAcceleration.Text, NumberStyles.AllowThousands);
                recipeValues[recipeInfo.Deceleration] = int.Parse(tbDeceleration.Text, NumberStyles.AllowThousands);
                recipeValues[recipeInfo.Vaccum_control] = (bool)cbVacuum.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.IsVentgasAir] = DatabaseSettings.General_TrueValue_Write;
                //UrecipeValues[recipeInfo.isVentgasAir] = (bool)rbAir.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.MonitorType] = DatabaseSettings.General_TrueValue_Write;
                //recipeValues[recipeInfo.monitorType] = (bool)cbMonitorType.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                recipeValues[recipeInfo.PressureUnit] = cbxPressureUnit.Text;
                recipeValues[recipeInfo.Scurve] = "-";// tbSCurve.Text;
                recipeValues[recipeInfo.Coldtrap] = (bool)cbColdTrap.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;

                int i = 0;
                do
                {
                    recipeValues[recipeInfo.Speed00 + 3 * i] = int.Parse(speeds[i].Text, NumberStyles.AllowThousands);
                    recipeValues[recipeInfo.Time00 + 3 * i] = int.Parse(times[i].Text, NumberStyles.AllowThousands);
                    recipeValues[recipeInfo.Pressure00 + 3 * i] = int.Parse(pressures[i].Text, NumberStyles.AllowThousands);
                    i++;
                } while (i != 10 && (bool)toggleButtons[i].IsChecked);

                recipeValues[recipeInfo.SpeedMin] = int.Parse(tbSpeedMin.Text, NumberStyles.AllowThousands);
                recipeValues[recipeInfo.SpeedMax] = int.Parse(tbSpeedMax.Text, NumberStyles.AllowThousands);
                recipeValues[recipeInfo.PressureMin] = int.Parse(tbPressureMin.Text, NumberStyles.AllowThousands);
                recipeValues[recipeInfo.PressureMax] = int.Parse(tbPressureMax.Text, NumberStyles.AllowThousands);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Message.MyMessageBox.Show(ex.Message);
                recipeValues = null;
            }

            return recipeValues;
        }
        private void TbProgramName_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbProgramName_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Name];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: false, Settings.Default.RecipeMix_ProgramName_nCharMax))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbAcceleration_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbAcceleration_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Acceleration];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: Settings.Default.RecipeMix_Acceleration_Min, max: Settings.Default.RecipeMix_Acceleration_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbDeceleration_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbDeceleration_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Deceleration];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: Settings.Default.RecipeMix_Deceleration_Min, max: Settings.Default.RecipeMix_Deceleration_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbSCurve_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbSCurve_LostFocus");
            /*
            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.scurve];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: false, Settings.Default.RecipeMix_SCurve_nCharMax))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }*/
            //Message.MyMessageBox.Show(FormatControl[i].ToString());

            int i = ControlsIDs[recipeSpeedMixerInfo.Scurve];
            FormatControl[i] = true;
        }
        private void TbSpeed_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbSpeed_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Speed00];

            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == speeds[j])
                {
                    if (j == 0 || (bool)toggleButtons[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                            min: Settings.Default.RecipeMix_Speed_Min, max: Settings.Default.RecipeMix_Speed_Max))
                        {
                            FormatControl[i + 3 * j] = true;
                        }
                        else
                        {
                            FormatControl[i + 3 * j] = false;
                        }
                    }
                }
            }
        }
        private void TbTime_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbTime_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Time00];
            //Message.MyMessageBox.Show(i.ToString() + " - " + recipeSpeedMixerInfo.time00.ToString());
            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == times[j])
                {
                    if (j == 0 || (bool)toggleButtons[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                            min: Settings.Default.RecipeMix_Time_Min, max: Settings.Default.RecipeMix_Time_Max))
                        {
                            FormatControl[i + 3 * j] = true;
                        }
                        else
                        {
                            FormatControl[i + 3 * j] = false;
                        }
                        //Message.MyMessageBox.Show((i+j).ToString() + " - " + FormatControl[i + j].ToString());
                    }
                }
            }
        }
        private void tbPressure_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("tbPressure_LostFocus");

            //pressures[0] = tbPressure00;
            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.Pressure00];

            for (int j = 0; j < PhasesNumber; j++)
            {
                if (textBox == pressures[j])
                {
                    if (j == 0 || (bool)toggleButtons[j].IsChecked)
                    {
                        if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                            min: Settings.Default.RecipeMix_Pressure_Min, max: Settings.Default.RecipeMix_Pressure_Max))
                        {
                            FormatControl[i + 3 * j] = true;
                        }
                        else
                        {
                            FormatControl[i + 3 * j] = false;
                        }
                    }
                }
            }
        }
        private void TbSpeedMin_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbSpeedMin_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.SpeedMin];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: Settings.Default.RecipeMix_Speed_Min, max: tbSpeedMax.Text == "" ? Settings.Default.RecipeMix_Speed_Max : decimal.Parse(tbSpeedMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbSpeedMax_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbSpeedMax_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.SpeedMax];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: tbSpeedMin.Text == "" ? Settings.Default.RecipeMix_Speed_Min : decimal.Parse(tbSpeedMin.Text), max: Settings.Default.RecipeMix_Speed_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbPressureMin_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbPressureMin_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.PressureMin];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: Settings.Default.RecipeMix_Pressure_Min, max: tbPressureMax.Text == "" ? Settings.Default.RecipeMix_Pressure_Max : decimal.Parse(tbPressureMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbPressureMax_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbPressureMax_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeSpeedMixerInfo.PressureMax];

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: tbPressureMin.Text == "" ? Settings.Default.RecipeMix_Pressure_Min : decimal.Parse(tbPressureMin.Text), max: Settings.Default.RecipeMix_Pressure_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //Message.MyMessageBox.Show(FormatControl[i].ToString());
        }
        public bool IsFormatOk()
        {
            logger.Debug("IsFormatOk");

            int n = 0;
            int x = 0;

            for (int i = 0; i < FormatControl.Length; i++)
            {
                n += FormatControl[i] ? 1 : 0;
                //Message.MyMessageBox.Show(i.ToString() + " - " + FormatControl[i].ToString());
            }

            for (int i = 1; i < PhasesNumber; i++)
            {
                x += (bool)toggleButtons[i].IsChecked ? 0 : 3; // Pour chaque checkbox décoché, on ajoutera 3 au score final
            }
            //Message.MyMessageBox.Show(n.ToString() + " + " + x.ToString() + " = " + (n+x).ToString() + " / " + FormatControl.Length.ToString());
            return (n + x) == FormatControl.Length;
        }
    }
}
