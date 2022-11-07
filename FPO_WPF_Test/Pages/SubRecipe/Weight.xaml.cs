using Database;
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
    /// Logique d'interaction pour Weight.xaml
    /// </summary>
    public partial class Weight : Page, IRecipeSeq
    {
        //public int seqType { get; }

        private const int ControlNumber = 6;
        private const int IdProduct = 0;
        private const int IdBarcode = 1;
        private const int IdDecimalNumber = 2;
        private const int IdSetpoint = 3;
        private const int IdMin = 4;
        private const int IdMax = 5;

        private readonly Frame parentFrame;
        private readonly bool[] FormatControl = new bool[ControlNumber];
        private bool CurrentFormatControl_tbBarcode;
        //private General g = new General();
        public Weight()
        {
            //seqType = 0;

            InitializeComponent();
        }
        public Weight(Frame frame, string seqNumber)
        {
            parentFrame = frame;
            CurrentFormatControl_tbBarcode = false;
            InitializeComponent();
            tbSeqNumber.Text = seqNumber;

        }
        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = new SpeedMixer(parentFrame, tbSeqNumber.Text);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = null;
        }
        public void SetSeqNumber(string n)
        {
            tbSeqNumber.Text = n;
        }
        public int GetSeqNumber()
        {
            MessageBox.Show("get");
            return 42;
        }
        public void SetSeqToSpeedMixer()
        {
            rbSpeedMixer.IsChecked = true;
        }
        public void SetPage_old(string[] array)
        {
            tbProduct.Text = array[3];
            cbIsBarcode.IsChecked = array[4] == "True";
            tbBarcode.Text = array[5];
            cbxUnit.Text = array[6];
            tbDecimalNumber.Text = array[7];

            tbSetpoint.Text = decimal.Parse(array[8]).ToString("N" + int.Parse(tbDecimalNumber.Text).ToString());
            tbMin.Text = decimal.Parse(array[9]).ToString("N" + int.Parse(tbDecimalNumber.Text).ToString());
            tbMax.Text = decimal.Parse(array[10]).ToString("N" + int.Parse(tbDecimalNumber.Text).ToString());

            TbProduct_LostFocus(tbProduct, new RoutedEventArgs());
            if ((bool)cbIsBarcode.IsChecked) TbBarcode_LostFocus(tbBarcode, new RoutedEventArgs());
            else FormatControl[IdBarcode] = false;
            TbDecimalNumber_LostFocus(tbDecimalNumber, new RoutedEventArgs());
            TbSetpoint_LostFocus(tbSetpoint, new RoutedEventArgs());
            TbMin_LostFocus(tbMin, new RoutedEventArgs());
            TbMax_LostFocus(tbMax, new RoutedEventArgs());
        }
        public void SetPage(ISeqInfo seqInfo)
        {

        }
        public string[] GetPage_old()
        {
            int n = 1;
            string[] array = new string[11 - n];

            array[3 - n] = tbProduct.Text;
            array[4 - n] = (bool)cbIsBarcode.IsChecked ? "1" : "0";
            array[5 - n] = tbBarcode.Text;
            array[6 - n] = cbxUnit.Text;
            array[7 - n] = tbDecimalNumber.Text;
            array[8 - n] = tbSetpoint.Text;
            array[9 - n] = tbMin.Text;
            array[10 - n] = tbMax.Text;

            return array;
        }
        public ISeqInfo GetPage()
        {
            RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();

            // ICI, il faut faire du Parse et des try

            recipeWeightInfo.columns[recipeWeightInfo.seqName].value = tbProduct.Text;
            recipeWeightInfo.columns[recipeWeightInfo.isBarcodeUsed].value = (bool)cbIsBarcode.IsChecked ? "1" : "0";
            recipeWeightInfo.columns[recipeWeightInfo.barcode].value = tbBarcode.Text;
            recipeWeightInfo.columns[recipeWeightInfo.unit].value = cbxUnit.Text;
            recipeWeightInfo.columns[recipeWeightInfo.decimalNumber].value = tbDecimalNumber.Text;
            recipeWeightInfo.columns[recipeWeightInfo.setpoint].value = tbSetpoint.Text;
            recipeWeightInfo.columns[recipeWeightInfo.min].value = tbMin.Text;
            recipeWeightInfo.columns[recipeWeightInfo.max].value = tbMax.Text;

            return recipeWeightInfo;
        }
        private void CbIsBarcode_Checked(object sender, RoutedEventArgs e)
        {
            tbBarcode.Visibility = Visibility.Visible;
            labelBarcode.Visibility = Visibility.Visible;
            if(tbBarcode.Text != "") TbBarcode_LostFocus(tbBarcode, new RoutedEventArgs());
            FormatControl[IdBarcode] = CurrentFormatControl_tbBarcode;
        }
        private void CbIsBarcode_Unchecked(object sender, RoutedEventArgs e)
        {
            tbBarcode.Visibility = Visibility.Hidden;
            labelBarcode.Visibility = Visibility.Hidden;
            FormatControl[IdBarcode] = false;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //cbIsBarcode.IsChecked = true;
        }
        private void TbProduct_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdProduct;

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
        private void TbBarcode_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdBarcode;

            if (General.Verify_Format(textBox, isNotNull:true, isNumber: false, 30))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }

            CurrentFormatControl_tbBarcode = FormatControl[i];

            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbDecimalNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdDecimalNumber;

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, 0, min: 0, max: 6))
            {
                FormatControl[i] = true;
                if (FormatControl[IdSetpoint])
                {
                    tbSetpoint.Text = decimal.Parse(tbSetpoint.Text).ToString("N" + int.Parse(textBox.Text).ToString());
                }
                if (FormatControl[IdMin])
                {
                    tbMin.Text = decimal.Parse(tbMin.Text).ToString("N" + int.Parse(textBox.Text).ToString());
                }
                if (FormatControl[IdMax])
                {
                    tbMax.Text = decimal.Parse(tbMax.Text).ToString("N" + int.Parse(textBox.Text).ToString());
                }
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbSetpoint_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdSetpoint;
            int n;

            try
            {
                n = int.Parse(tbDecimalNumber.Text);
            }
            catch (Exception)
            {
                n = 0;
            }


            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, min: tbMin.Text == "" ? 0 : decimal.Parse(tbMin.Text), max: tbMax.Text == "" ? -1 : decimal.Parse(tbMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbMin_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdMin;
            int n;

            try
            {
                n = int.Parse(tbDecimalNumber.Text);
            }
            catch (Exception)
            {
                n = 0;
            }


            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, min: 0, max: tbMax.Text == "" ? -1 : decimal.Parse(tbMax.Text)))
            {
                FormatControl[i] = true;
            }
            else
            {
                FormatControl[i] = false;
            }
            //MessageBox.Show(FormatControl[i].ToString());
        }
        private void TbMax_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int i = IdMax;
            int n;

            try
            {
                n = int.Parse(tbDecimalNumber.Text);
            }
            catch (Exception)
            {
                n = 0;
            }

            if (General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: n, min: tbMin.Text == "" ? 0 : decimal.Parse(tbMin.Text)))
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
            int x;

            for (int i = 0; i < ControlNumber; i++)
            {
                n += FormatControl[i] ? 1 : 0;
            }

            x = (bool)cbIsBarcode.IsChecked ? 0 : 1;

            return (n+x) == ControlNumber;
        }
    }
}
