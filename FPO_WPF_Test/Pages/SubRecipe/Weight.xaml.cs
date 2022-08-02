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

namespace FPO_WPF_Test.Pages.SubRecipe
{
    /// <summary>
    /// Logique d'interaction pour Weight.xaml
    /// </summary>
    public partial class Weight : Page
    {
        private Frame parentFrame;
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

        public void setSeqNumber(string n)
        {
            tbSeqNumber.Text = n;
        }

        public int getSeqNumber()
        {
            MessageBox.Show("get");
            return 42;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            parentFrame.Content = null;
        }
    }
}
