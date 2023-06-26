using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Main.Properties;
using Driver_MODBUS_SpeedMixer;
using Driver_RS232_Pump;
using Driver_Ethernet_Balance;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class Status : UserControl
    {
        private readonly System.Timers.Timer timer;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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

        private void Timer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateLabels();
            });
        }

        private void UpdateLabels()
        {
            labelSpeedmixerStatus.Text = SpeedMixerModbus.IsConnected() ? Settings.Default.Status_Connected : Settings.Default.Status_Disconnected;
            labelBalanceStatus.Text = Balance.IsConnected() ? Settings.Default.Status_Connected : Settings.Default.Status_Disconnected;
            labelPumpStatus.Text = RS232Pump.IsOpen() ? Settings.Default.Status_Connected : Settings.Default.Status_Disconnected;
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
