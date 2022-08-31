using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using MySqlConnector;
using System.Collections;
using System.Collections.Specialized;
using Driver.MODBUS;
using Database;
using Driver.ColdTrap;
using System.Globalization;
using DRIVER.RS232.Weight;
using Driver.RS232.Pump;

namespace FPO_WPF_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum Action
    {
        New,
        Modify,
        Copy,
        Delete
    }

    public partial class MainWindow : Window
    {
        public string Text { get; set; }
        //public SpeedMixerModbus speedMixer { get; set; }
        private readonly MyDatabase db;
        private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        //private ColdTrap coldtrap;

        public MainWindow()
        {
            db = new MyDatabase();

            string[] values = new string[] { "Utilisateur connecté", "Démarrage de l'application"};
            db.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

            InitializeComponent();

            if (true) frameMain.Content = new Pages.Status();
            frameInfoCycle.Content = null;

            // 
            //
            //
            // ICI : PENSER A INITIALISER LA BALANCE ET LA POMPE ET PEUT-ÊTRE LE COLD TRAP
            //
            //
            // 

            RS232Weight.Open();
            RS232Pump.Open();
            SpeedMixerModbus.Connect();





        }

        ~MainWindow()
        {
            MessageBox.Show("Au revoir");
        }

        private void FxCycleStart(object sender, RoutedEventArgs e)
        {
            //menuItemStart.IsEnabled = false;

            menuItemStart.Icon = new Image
            {
                Source = new BitmapImage(new Uri("Resources/img_start_dis.png", UriKind.Relative))
            };

            frameMain.Content = new Pages.SubCycle.PreCycle(frameMain, frameInfoCycle);

            //Il faudra penser à bloquer ce qu'il faut
            db.AcknowledgeAlarm("ALARM 00.01 - Connexion à la balance échouée");
        }

        private void FxCycleStop(object sender, RoutedEventArgs e)
        {

        }
        private void FxSystemStatus(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Status();
            db.InactivateAlarm("ALARM 00.01 - Connexion à la balance échouée");
            RS232Weight.areAlarmActive[0] = false;
        }
        private void FxProgramNew(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.New);
        }
        private void FxProgramModify(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.Modify);
        }
        private void FxProgramCopy(object sender, RoutedEventArgs e)
        {
            
        }
        private void FxProgramDelete(object sender, RoutedEventArgs e)
        {
        }
        private void FxAuditTrail(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.AuditTrail();
        }
        private void FxAlarms(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserLogInOut(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Opérateur (Oui), Superviseur (Non) ou Administareur (Annuler)", "Qui-es tu ?", button: MessageBoxButton.YesNoCancel);

            switch (result.ToString())
            {
                case "Yes":
                    General.Role = "Operator";
                    break;
                case "No":
                    General.Role = "Supervisor";
                    break;
                case "Cancel":
                    General.Role = "Administrator";
                    break;
                default:
                    break;
            }

            MessageBox.Show(General.Role);
        }
        private void FxUserNew(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserModify(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserDelete(object sender, RoutedEventArgs e)
        {

        }
        private void Close_App_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}