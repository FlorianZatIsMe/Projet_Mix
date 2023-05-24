using Message;
using System;
using System.Configuration;
using System.Windows.Controls;
using System.Windows.Input;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Parameters.xaml
    /// </summary>
    public partial class ParametersOld : Page
    {
        bool dpNextCalDateToUpdt = false;
        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ParametersOld()
        {
            InitializeComponent();

            try
            {
                dpNextCalibDate.SelectedDate = Convert.ToDateTime(config.AppSettings.Settings["NextCalibDate"].Value);
            }
            catch (Exception ex)
            {
                dpNextCalibDate.SelectedDate = DateTime.Now;
            }
        }

        private void dpNextCalibDate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dpNextCalDateToUpdt = true;
        }

        private void dpNextCalibDate_LayoutUpdated(object sender, EventArgs e)
        {
            if (dpNextCalDateToUpdt)
            {
                UpdateNextCalibDate();
            }
        }
        private void UpdateNextCalibDate()
        {
            try
            {
                Convert.ToDateTime(dpNextCalibDate.Text);
                config.AppSettings.Settings["NextCalibDate"].Value = dpNextCalibDate.Text;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                logger.Trace(config.AppSettings.Settings["NextCalibDate"].Value + " - " + dpNextCalibDate.Text);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
            }
            dpNextCalDateToUpdt = false;
        }

        private void dpNextCalibDate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("dpNextCalibDate_KeyDown " + (e.Key == Key.Enter).ToString());

            if (e.Key == Key.Enter)
            {
                UpdateNextCalibDate();
            }
        }
    }
}
