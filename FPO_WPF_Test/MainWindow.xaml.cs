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

namespace FPO_WPF_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Text { get; set; }
        public SpeedMixerModbus SpeedMixer { get; set; }
        private readonly MyDatabase db;
        private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private ColdTrap coldtrap;

        public MainWindow()
        {

            SpeedMixer = new SpeedMixerModbus();
            db = new MyDatabase();
            coldtrap = new ColdTrap();

            string[] values = new string[] { "Utilisateur connecté", "Démarrage de l'application"};
            db.SendCommand_insertRecord(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

            InitializeComponent();

            if (true) frameMain.Content = new Pages.Status();

            NumberFormatInfo nfi = new CultureInfo(CultureInfo.CurrentCulture.Name, false).NumberFormat;

            /*
            MessageBox.Show(nfi.NumberDecimalSeparator);


            try
            {
                MessageBox.Show(float.Parse("1.2").ToString());
            }
            catch (Exception)
            {
                MessageBox.Show("Weird");
            }

            try
            {
                MessageBox.Show(float.Parse("1,2").ToString());
            }
            catch (Exception)
            {
                MessageBox.Show("VERY Weird");
            }
            */
        }

        ~MainWindow()
        {
            MessageBox.Show("Au revoir");
        }

        private void FxCycleStart(object sender, RoutedEventArgs e)
        {
            menuItemStart.IsEnabled = false;

            menuItemStart.Icon = new Image
            {
                Source = new BitmapImage(new Uri("Resources/img_start_dis.png", UriKind.Relative))
            };

            frameMain.Content = new Pages.Traceability(frameMain);

            //Il faudra penser à bloquer ce qu'il faut
            }

        private void FxCycleStop(object sender, RoutedEventArgs e)
        {

        }
        private void FxSystemStatus(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Status();
        }
        private void FxProgramNew(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe();
        }
        private void FxProgramModify(object sender, RoutedEventArgs e)
        {

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