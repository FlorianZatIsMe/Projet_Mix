using Main.Properties;
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
using Driver_MODBUS;
using Driver_Ethernet_Balance;
using Driver_ColdTrap;
using Driver_RS232_Pump;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : Page, IDisposable
    {
        private readonly System.Timers.Timer timer;

        public Status()
        {
            InitializeComponent();
            UpdateLabels();

            // Initialisation des timers
            timer = new System.Timers.Timer
            {
                Interval = Settings.Default.Status_timer_Interval,
                AutoReset = true
            };
            timer.Elapsed += Timer_OnTimedEvent;
            timer.Start();
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        private void Timer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateLabels();
            });
        }

        private void UpdateLabels()
        {
            labelSpeedmixerStatus.Text = SpeedMixerModbus.IsConnected() ? "Connecté" : "Déconnecté";
            labelBalanceStatus.Text = Balance.IsConnected() ? "Connecté" : "Déconnecté";
            labelPumpStatus.Text = RS232Pump.IsOpen() ? "Connecté" : "Déconnecté";
            //labelColtTrapStatus.Text = ColdTrap.IsConnected() ? "Connecté" : "Déconnecté";

            labelSpeedmixerStatus.Foreground = SpeedMixerModbus.IsConnected() ?
            (SolidColorBrush)Application.Current.FindResource("FontColor_Connected") :
            (SolidColorBrush)Application.Current.FindResource("FontColor_Disconnected");

            labelBalanceStatus.Foreground = Balance.IsConnected() ?
            (SolidColorBrush)Application.Current.FindResource("FontColor_Connected") :
            (SolidColorBrush)Application.Current.FindResource("FontColor_Disconnected");

            labelPumpStatus.Foreground = RS232Pump.IsOpen() ?
            (SolidColorBrush)Application.Current.FindResource("FontColor_Connected") :
            (SolidColorBrush)Application.Current.FindResource("FontColor_Disconnected");
        }
    }
}
