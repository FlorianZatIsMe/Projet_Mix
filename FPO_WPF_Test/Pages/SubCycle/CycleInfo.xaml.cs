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

namespace FPO_WPF_Test.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour CycleInfo.xaml
    /// </summary>
    public partial class CycleInfo : Page // Les pages ne peuvent pas être static
    {
        private List<WrapPanel> wrapPanels = new List<WrapPanel>();
        private static int seqNumber;

        public CycleInfo(string[] info)
        {
            seqNumber = -1;

            InitializeComponent();

            if (info.Length == 4)
            {
                labelOFNumber.Text = info[0];
                labelRecipeName.Text = info[1];
                labelRecipeVersion.Text = info[2];
                labelFinalWeight.Text = info[3];
            }
            else
            {
                MessageBox.Show("Il y a un problème là");
            }
        }
        public void NewInfoWeight(string[] info)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock productName = new TextBlock();
            productName.Foreground = Brushes.Wheat;
            productName.Text = "Produit pesé: " + info[0];

            TextBlock min = new TextBlock();
            min.Foreground = Brushes.Wheat;
            min.Margin = new Thickness(20, 0, 0, 0);
            min.Text = "Minimum: " + info[1];

            TextBlock max = new TextBlock();
            max.Foreground = Brushes.Wheat;
            max.Margin = new Thickness(20, 0, 0, 0);
            max.Text = "Maximum: " + info[2];

            TextBlock actualWeight = new TextBlock();
            actualWeight.Foreground = Brushes.Wheat;
            actualWeight.Margin = new Thickness(20, 0, 0, 0);
            actualWeight.Text = "Masse pesée: -";

            wrapPanel.Children.Add(productName);
            wrapPanel.Children.Add(min);
            wrapPanel.Children.Add(max);
            wrapPanel.Children.Add(actualWeight);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void NewInfoSpeedMixer(string[] info)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock programName = new TextBlock();
            programName.Foreground = Brushes.Wheat;
            programName.Text = "Nom du mélange: " + info[0];

            TextBlock status = new TextBlock();
            status.Foreground = Brushes.Wheat;
            status.Margin = new Thickness(20, 0, 0, 0);
            status.Text = "Status: En attente";

            wrapPanel.Children.Add(programName);
            wrapPanel.Children.Add(status);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void UpdateCurrentWeightInfo(string[] info)
        {
            (wrapPanels[seqNumber].Children[3] as TextBlock).Text = "Masse pesée: " + info[0];
        }
        public void UpdateCurrentSpeedMixerInfo(string[] info)
        {
            (wrapPanels[seqNumber].Children[1] as TextBlock).Text = "Status: " + info[0];
        }
        public void AddRow(string rowText)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock actualWeight = new TextBlock();
            actualWeight.Foreground = Brushes.Wheat;
//            actualWeight.Margin = new Thickness(0, 10, 0, 0);
            actualWeight.Text = rowText;

            wrapPanel.Children.Add(actualWeight);
            StackMain.Children.Add(wrapPanel);
        }
        public void UpdateSequenceNumber()
        {
            if (wrapPanels == null)
            {
                wrapPanels.Clear();
            }

            seqNumber++;
        }
        public void InitializeSequenceNumber()
        {
            seqNumber = -1;
        }
    }
}
