using Database;
using Main.Properties;
using Message;
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
using static Main.Pages.Recipe;

namespace Main.Pages.SubRecipe
{
    public class WeightViewModel
    {
        public string TextBlockSetpoint { get; } = Settings.Default.RecipeWeight_Setpoint_Label + " " + "[" + Settings.Default.RecipeWeight_Setpoint_Min + " ; " + Settings.Default.RecipeWeight_Setpoint_Max + "]";
        public string TextBlockRange { get; } = Settings.Default.RecipeWeight_Range_Label + " " + "[" + Settings.Default.RecipeWeight_Range_Min + " ; " + Settings.Default.RecipeWeight_Range_Max + "]";
    }

    /// <summary>
    /// Logique d'interaction pour Weight.xaml
    /// </summary>
    public partial class Weight : Page, ISubRecipe
    {
        RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();

        private readonly int[] ControlsIDs;

        private readonly Frame parentFrame;
        private readonly bool[] FormatControl = new bool[Settings.Default.RecipeWeight_IdDBControls.list.Count];
        private bool CurrentFormatControl_tbBarcode;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        

        public Weight(Frame frame, string seqNumber)
        {
            logger.Debug("Start");
            
            ControlsIDs = new int[recipeWeightInfo.Ids.Count()];
            List<int> list = Settings.Default.RecipeWeight_IdDBControls.list;
            int n = 0;

            for (int i = 0; i < ControlsIDs.Length; i++)
            {
                if (n < list.Count && i == list[n])
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
            CurrentFormatControl_tbBarcode = false;
            InitializeComponent();
            tbSeqNumber.Text = seqNumber;

            /*
            List<string> decimalNumbers = new List<string>();
            cbxDecimalNumbers.ItemsSource = decimalNumbers;

            for (int i = Settings.Default.RecipeWeight_DecimalNumber_Min; i <= Settings.Default.RecipeWeight_DecimalNumber_Max; i++)
            {
                decimalNumbers.Add(i.ToString());
            }

            cbxDecimalNumbers.SelectedIndex = 0;
            cbxDecimalNumbers.Items.Refresh();*/
        }
        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            logger.Debug("RadioButton_Click_1");

            parentFrame.Content = new SpeedMixer(parentFrame, tbSeqNumber.Text);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Button_Click");

            parentFrame.Content = null;
        }
        public void SetSeqNumber(int n)
        {
            logger.Debug("SetSeqNumber");

            tbSeqNumber.Text = n.ToString();
        }
        public void SetPage(object[] seqValues)
        {
            logger.Debug("SetPage");
            RecipeWeightInfo recipeInfo = new RecipeWeightInfo();

            if (seqValues.Count() != recipeInfo.Ids.Count())
            {
                logger.Error("On a un problème");
                MyMessageBox.Show("On a un problème");
                return;
            }

            tbProduct.Text = seqValues[recipeInfo.Name].ToString();
            cbIsBarcode.IsChecked = seqValues[recipeInfo.IsBarcodeUsed].ToString() == DatabaseSettings.General_TrueValue_Read;
            tbBarcode.Text = seqValues[recipeInfo.Barcode].ToString();
            //cbxUnit.Text = recipeInfo.Columns[recipeInfo.Unit].Value;
            //cbxDecimalNumbers.Text = recipeInfo.Columns[recipeInfo.DecimalNumber].Value;
            //tbDecimalNumber.Text = recipeInfo.columns[recipeInfo.decimalNumber].value;

            tbSetpoint.Text = ((decimal)seqValues[recipeInfo.Setpoint]).ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            tbRange.Text = ((decimal)seqValues[recipeInfo.Criteria]).ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            /*

            tbSetpoint.Text = decimal.Parse(recipeInfo.Columns[recipeInfo.Setpoint].Value).ToString("N" + int.Parse(cbxDecimalNumbers.Text).ToString());
            tbRange.Text = decimal.Parse(recipeInfo.Columns[recipeInfo.Criteria].Value).ToString("N" + int.Parse(cbxDecimalNumbers.Text).ToString());
             */
            cbIsSolvent.IsChecked = seqValues[recipeInfo.IsSolvent].ToString() == DatabaseSettings.General_TrueValue_Read;

            TbProduct_LostFocus(tbProduct, new RoutedEventArgs());
            if ((bool)cbIsBarcode.IsChecked) TbBarcode_LostFocus(tbBarcode, new RoutedEventArgs());
            else FormatControl[ControlsIDs[recipeWeightInfo.Barcode]] = false;
            //TbDecimalNumber_LostFocus(tbDecimalNumber, new RoutedEventArgs());
            TbSetpoint_LostFocus(tbSetpoint, new RoutedEventArgs());
            TbRange_LostFocus(tbRange, new RoutedEventArgs());
            //TbMax_LostFocus(tbMax, new RoutedEventArgs());
        }
        public ISeqTabInfo GetPage()
        {
            logger.Debug("GetPage");

            RecipeWeightInfo recipeInfo = new RecipeWeightInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];

            // ICI, il faut faire du Parse et des try

            recipeValues[recipeInfo.Name] = tbProduct.Text;
            recipeValues[recipeInfo.IsBarcodeUsed] = (bool)cbIsBarcode.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
            recipeValues[recipeInfo.Barcode] = tbBarcode.Text;
            recipeValues[recipeInfo.Unit] = Settings.Default.RecipeWeight_gG_Unit; // cbxUnit.Text;
            recipeValues[recipeInfo.DecimalNumber] = Settings.Default.RecipeWeight_NbDecimal.ToString();
            //recipeValues[recipeInfo.DecimalNumber] = cbxDecimalNumbers.Text;
            recipeValues[recipeInfo.Setpoint] = tbSetpoint.Text;
            recipeValues[recipeInfo.Criteria] = tbRange.Text;
            recipeValues[recipeInfo.IsSolvent] = (bool)cbIsSolvent.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;

            return recipeInfo;
        }
        public ISeqTabInfo GetRecipeInfo()
        {
            logger.Debug("GetRecipeInfo");
            return new RecipeWeightInfo();
        }
        public object[] GetRecipeValues()
        {
            logger.Debug("GetRecipeValues");

            RecipeWeightInfo recipeInfo = new RecipeWeightInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];

            // ICI, il faut faire du Parse et des try

            recipeValues[recipeInfo.Name] = tbProduct.Text;
            recipeValues[recipeInfo.IsBarcodeUsed] = (bool)cbIsBarcode.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
            recipeValues[recipeInfo.Barcode] = tbBarcode.Text;
            recipeValues[recipeInfo.Unit] = Settings.Default.RecipeWeight_gG_Unit; // cbxUnit.Text;
            recipeValues[recipeInfo.DecimalNumber] = Settings.Default.RecipeWeight_NbDecimal.ToString();
            //recipeValues[recipeInfo.DecimalNumber] = cbxDecimalNumbers.Text;
            recipeValues[recipeInfo.Setpoint] = tbSetpoint.Text;
            recipeValues[recipeInfo.Criteria] = tbRange.Text;
            recipeValues[recipeInfo.IsSolvent] = (bool)cbIsSolvent.IsChecked ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;

            return recipeValues;
        }
        private void CbIsBarcode_Checked(object sender, RoutedEventArgs e)
        {
            logger.Debug("CbIsBarcode_Checked");
            cbIsBarcode.Content = "Code barre";
            tbBarcode.Visibility = Visibility.Visible;
            //labelBarcode.Visibility = Visibility.Visible;
            if(tbBarcode.Text != "") TbBarcode_LostFocus(tbBarcode, new RoutedEventArgs());
            FormatControl[ControlsIDs[recipeWeightInfo.Barcode]] = CurrentFormatControl_tbBarcode;
        }
        private void CbIsBarcode_Unchecked(object sender, RoutedEventArgs e)
        {
            logger.Debug("CbIsBarcode_Unchecked");
            cbIsBarcode.Content = "Contrôle du code barre";
            tbBarcode.Visibility = Visibility.Hidden;
            //labelBarcode.Visibility = Visibility.Hidden;
            FormatControl[ControlsIDs[recipeWeightInfo.Barcode]] = false;
        }
        private void TbProduct_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbProduct_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeWeightInfo.Name];

            General.HideKeyBoard();

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: false, Settings.Default.RecipeWeight_Product_nCharMax))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
                //General.ShowKeyBoard();
            }
            //MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbBarcode_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbBarcode_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeWeightInfo.Barcode];

            General.HideKeyBoard();

            if (General.Verify_Format(textBox, isNotNull:true, isNumber: false, Settings.Default.RecipeWeight_Barcode_nCharMax))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
                //General.ShowKeyBoard();
            }

            CurrentFormatControl_tbBarcode = FormatControl[i];

            //MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbSetpoint_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbSetpoint_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeWeightInfo.Setpoint];
            int n = Settings.Default.RecipeWeight_NbDecimal;
            /*
            try
            {
                //n = int.Parse(tbDecimalNumber.Text);
                n = int.Parse(cbxDecimalNumbers.Text);
            }
            catch (Exception)
            {
                n = 0;
            }*/

            General.HideKeyBoard();

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, 
                min: Settings.Default.RecipeWeight_Setpoint_Min, 
                max: Settings.Default.RecipeWeight_Setpoint_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
                //General.ShowKeyBoard();
            }
            //MyMessageBox.Show(FormatControl[i].ToString());
        }
        private void TbRange_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbMin_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeWeightInfo.Criteria];
            int n = Settings.Default.RecipeWeight_NbDecimal;

            /*
            try
            {
                //n = int.Parse(tbDecimalNumber.Text);
                n = int.Parse(cbxDecimalNumbers.Text);
            }
            catch (Exception)
            {
                n = 0;
            }*/
            General.HideKeyBoard();


            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, 
                min: Settings.Default.RecipeWeight_Range_Min, max: Settings.Default.RecipeWeight_Range_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
                //General.ShowKeyBoard();
            }
            //MyMessageBox.Show(FormatControl[i].ToString());
        }
        /*
        private void TbMax_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbMax_LostFocus");

            TextBox textBox = sender as TextBox;
            int i = ControlsIDs[recipeWeightInfo.Max];
            int n = Settings.Default.RecipeWeight_NbDecimal;

            /*
            try
            {
                //n = int.Parse(tbDecimalNumber.Text);
                n = int.Parse(cbxDecimalNumbers.Text);
            }
            catch (Exception)
            {
                n = 0;
            }

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, 
                min: tbRange.Text == "" ? Settings.Default.RecipeWeight_Min_Max : decimal.Parse(tbRange.Text), max: Settings.Default.RecipeWeight_Max_Max))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MyMessageBox.Show(FormatControl[i].ToString());
        }*/
        public bool IsFormatOk()
        {
            logger.Debug("IsFormatOk");

            int n = 0;
            int x;

            for (int i = 0; i < FormatControl.Length; i++)
            {
                n += FormatControl[i] ? 1 : 0;
            }

            x = (bool)cbIsBarcode.IsChecked ? 0 : 1;
            //MyMessageBox.Show(((n + x) == FormatControl.Length).ToString() + " - " + (n + x).ToString() + " = " + n.ToString() + " + " + x.ToString() + " / " + FormatControl.Length.ToString()); //((n + x) == FormatControl.Length).ToString() + n.ToString + x.ToString() + FormatControl.Length.ToString()
            return (n+x) == FormatControl.Length;
        }
        private void ShowKeyBoard(object sender, RoutedEventArgs e)
        {
            General.ShowKeyBoard();
        }

        private void HideKeyBoardIfEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                General.HideKeyBoard();
            }
        }
    }
}
