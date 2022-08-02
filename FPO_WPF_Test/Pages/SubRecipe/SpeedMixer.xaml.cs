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
    /// Logique d'interaction pour SpeedMixer.xaml
    /// </summary>
    public partial class SpeedMixer : Page
    {
        private Frame parentFrame;
        public SpeedMixer(Frame frame, string seqNumber)
        {
            parentFrame = frame;
            InitializeComponent();
            tbSeqNumber.Text = seqNumber;
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
    }
}
