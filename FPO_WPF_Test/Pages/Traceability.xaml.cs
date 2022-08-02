﻿using System;
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

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour Traceability.xaml
    /// </summary>
    public partial class Traceability : Page
    {
        Frame outputFrame = new Frame();

        public Traceability(Frame inputFrame)
        {
            outputFrame = inputFrame;
            InitializeComponent();
        }

        private void fxOK(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Voulez-vous démarrer le cycle?","Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                MessageBox.Show("Et bien démarre le alors !", "Et bah");
            }
        }

        private void fxAnnuler(object sender, RoutedEventArgs e)
        {
            outputFrame.Content = new Status();
        }
    }
}
