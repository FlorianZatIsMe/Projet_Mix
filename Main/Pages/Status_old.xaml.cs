using Main.Properties;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Driver_MODBUS_SpeedMixer;
using Driver_Ethernet_Balance;
using Driver_RS232_Pump;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Status.xaml
    /// </summary>
    public partial class StatusOld : Page//, IDisposable
    {
        private readonly System.Timers.Timer timer;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public StatusOld()
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
            logger.Debug("Dispose");
            //MessageBox.Show("Status");
            timer.Dispose();
            General.RemoveChildren(grid.Children);
            grid = null;
            // Forcez le ramasse-miettes à libérer la mémoire inutilisée.
            //GC.Collect();

            // Attendez que tous les finaliseurs soient exécutés.
            //GC.WaitForPendingFinalizers();

            //GC.SuppressFinalize(this);
            //MessageBox.Show("Status Dispose");
        }

        private void Timer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateLabels();
            });
        }

        private void UpdateLabels()
        {/*
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
            (SolidColorBrush)Application.Current.FindResource("FontColor_Disconnected");*/
        }
    }
}
