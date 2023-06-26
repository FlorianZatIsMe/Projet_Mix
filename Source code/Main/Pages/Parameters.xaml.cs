using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Configuration;
using Message;
using Main.Properties;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Parameters.xaml
    /// </summary>
    public partial class Parameters : UserControl
    {
        //bool dpNextCalDateToUpdt = false;
        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool isCbMonitorCalibAvailable = false;
        private CheckBox[] cbSampleWeights = new CheckBox[4];
        private WrapPanel[] wpSampleWeights = new WrapPanel[4];
        private TextBox[] tbSampleWeights = new TextBox[4];
        private TextBox[] tbSampleWeightIDs = new TextBox[4];

        public Parameters()
        {
            InitializeComponent();

            // Initialize the Calibration date
            bool isCalibMonitored = config.AppSettings.Settings["Main_IsCalibMonitored"].Value == (true).ToString();
            cbMonitorCalib.IsChecked = isCalibMonitored;
            UpdateNextCalibDateVisibility();
            isCbMonitorCalibAvailable = true;

            try
            {
                dpNextCalibDate.SelectedDate = Convert.ToDateTime(config.AppSettings.Settings["NextCalibDate"].Value);
            }
            catch (Exception ex)
            {
                dpNextCalibDate.SelectedDate = DateTime.Now;
            }

            // Initialize the daily tests sample weights
            cbSampleWeights[0] = cbSampleWeight0;
            cbSampleWeights[1] = cbSampleWeight1;
            cbSampleWeights[2] = cbSampleWeight2;
            cbSampleWeights[3] = cbSampleWeight3;

            wpSampleWeights[0] = wpSampleWeight0;
            wpSampleWeights[1] = wpSampleWeight1;
            wpSampleWeights[2] = wpSampleWeight2;
            wpSampleWeights[3] = wpSampleWeight3;

            tbSampleWeights[0] = tbSampleWeight0;
            tbSampleWeights[1] = tbSampleWeight1;
            tbSampleWeights[2] = tbSampleWeight2;
            tbSampleWeights[3] = tbSampleWeight3;

            tbSampleWeightIDs[0] = tbSampleWeightID0;
            tbSampleWeightIDs[1] = tbSampleWeightID1;
            tbSampleWeightIDs[2] = tbSampleWeightID2;
            tbSampleWeightIDs[3] = tbSampleWeightID3;

            for (int i = 0; i < 4; i++)
            {
                //MessageBox.Show(i.ToString());
                try
                {
                    decimal currentSampleWeight = decimal.Parse(config.AppSettings.Settings["DailyTest_Weight" + i.ToString()].Value);
                    string currentSampleWeightID = config.AppSettings.Settings["DailyTest_WeightID" + i.ToString()].Value;
                    if (currentSampleWeightID == "")
                    {
                        i = 4;
                        break;
                    }

                    cbSampleWeights[i].IsChecked = true;
                    tbSampleWeights[i].Text = currentSampleWeight.ToString();
                    tbSampleWeightIDs[i].Text = currentSampleWeightID;

                }
                catch (Exception)
                { 
                    i = 4;
                }
            }
        }

        private void UpdateNextCalibDateVisibility()
        {
            dpNextCalibDate.Visibility = cbMonitorCalib.IsChecked == true ? Visibility.Visible : Visibility.Hidden;
        }


        private void cbMonitorCalib_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateNextCalibDateVisibility();
        }

        private void cbMonitorCalib_Checked(object sender, RoutedEventArgs e)
        {
            UpdateNextCalibDateVisibility();
        }
        private void cbSampleWeight_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            int index = 0;

            for (int i = 1; i < cbSampleWeights.Length; i++)
            {
                if (cbSampleWeights[i].Equals(checkBox))
                {
                    index = i;
                    break;
                }
            }

            if (index > 1 && cbSampleWeights[index - 1].IsChecked == false)
            {
                cbSampleWeights[index - 1].IsChecked = true;
            }

            //MessageBox.Show(index.ToString());
            wpSampleWeights[index].Visibility = Visibility.Visible;
        }

        private void cbSampleWeight_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            int index = 0;

            for (int i = 1; i < cbSampleWeights.Length; i++)
            {
                if (cbSampleWeights[i].Equals(checkBox))
                {
                    index = i;
                    break;
                }
            }

            if (index < 3 && cbSampleWeights[index + 1].IsChecked == true)
            {
                cbSampleWeights[index + 1].IsChecked = false;
            }

            //MessageBox.Show(index.ToString());
            wpSampleWeights[index].Visibility = Visibility.Hidden;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.IsEnabled = false;

            try
            {
                // Set the calibration parameters
                config.AppSettings.Settings["Main_IsCalibMonitored"].Value = cbMonitorCalib.IsChecked.ToString();
                Convert.ToDateTime(dpNextCalibDate.Text);
                config.AppSettings.Settings["NextCalibDate"].Value = dpNextCalibDate.Text;

                // Set the Sample weight
                for (int i = 0; i < 4; i++)
                {
                    if ((bool)cbSampleWeights[i].IsChecked)
                    {
                        if (tbSampleWeights[i].Text == "" || tbSampleWeightIDs[i].Text == "")
                        {
                            MyMessageBox.Show("Information de la masse " + (i + 1).ToString() + " incorrect");
                            goto End;
                        }
                        else
                        {
                            config.AppSettings.Settings["DailyTest_Weight" + i.ToString()].Value = decimal.Parse(tbSampleWeights[i].Text).ToString();
                            config.AppSettings.Settings["DailyTest_WeightID" + i.ToString()].Value = tbSampleWeightIDs[i].Text;
                        }
                    }
                    else
                    {
                        config.AppSettings.Settings["DailyTest_Weight" + i.ToString()].Value = "";
                        config.AppSettings.Settings["DailyTest_WeightID" + i.ToString()].Value = "";
                    }
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                MyMessageBox.Show("Paramètres modifiés");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show("Les paramètres n'ont pas pu être modifiés" + ex.Message);
            }
        End:
            button.IsEnabled = true;
        }
    }
}
