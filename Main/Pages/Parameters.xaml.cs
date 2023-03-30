using Main.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
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

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Parameters.xaml
    /// </summary>
    public partial class Parameters : Page
    {
        bool dpNextCalDateToUpdt = false;
        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Parameters()
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
                Message.MyMessageBox.Show(ex.Message);
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
