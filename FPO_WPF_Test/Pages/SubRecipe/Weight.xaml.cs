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
    public partial class Weight : Page
    {
        private Frame parentFrame;
        public Weight()
        {
            InitializeComponent();
        }
        public Weight(Frame frame, string seqNumber)
        {
            parentFrame = frame;
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

        public void SetPage(string[] array)
        {
            tbProduct.Text = array[3];
            cbIsBarcode.IsChecked = array[4] == "True";
            tbBarcode.Text = array[5];
            cbxUnit.Text = array[6];
            tbDecimalNumber.Text = array[7];
            tbSetpoint.Text = array[8];
            tbMin.Text = array[9];
            tbMax.Text = array[10];
        }

        public string[] GetPage()
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
    }
}
