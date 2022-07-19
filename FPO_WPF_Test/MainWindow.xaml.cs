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
using EasyModbus;
using System.Configuration;
using MySqlConnector;
using System.Collections;
using System.Collections.Specialized;
using Driver.MODBUS;
using Database;

namespace FPO_WPF_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Text { get; set; }
        public SpeedMixerModbus SpeedMixer { get; set; }
        public MyDatabase db { get; set; }

        public MainWindow()
        {

            SpeedMixer = new SpeedMixerModbus();
            db = new MyDatabase();

            //MessageBox.Show("SpeedMixer - Connection status: " + SpeedMixer.isConnected.ToString());
            //MessageBox.Show("Database - Connection status: " + db.isConnected().ToString());

            /*
            var MySettings = ConfigurationManager.GetSection("MODBUS_Connection_Info") as NameValueCollection;
            if (MySettings.Count == 0)
            {
                MessageBox.Show("Post Settings are not defined");
                Close();
            }
            else
            {
                foreach (var key in MySettings.AllKeys)
                {
                    //MessageBox.Show(key + " = " + MySettings[key]);
                }

                Text = MySettings["port"].ToString();
                //MessageBox.Show(Text);
            }
            */

            InitializeComponent();

            if (true) frameMain.Content = new Pages.Status();
        }

        ~MainWindow()
        {
            MessageBox.Show("Au revoir");
        }

        private void fxCycleStart(object sender, RoutedEventArgs e)
        {
            menuItemStart.IsEnabled = false;

            menuItemStart.Icon = new Image
            {
                Source = new BitmapImage(new Uri("Resources/img_start_dis.png", UriKind.Relative))
            };

            frameMain.Content = new Pages.Traceability(frameMain);

            //Il faudra penser à bloquer ce qu'il faut
            }

        private void fxCycleStop(object sender, RoutedEventArgs e)
        {

        }
        private void fxSystemStatus(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Status();
        }
        private void fxProgramNew(object sender, RoutedEventArgs e)
        {
            
        }
        private void fxProgramModify(object sender, RoutedEventArgs e)
        {

        }
        private void fxProgramCopy(object sender, RoutedEventArgs e)
        {

        }
        private void fxProgramDelete(object sender, RoutedEventArgs e)
        {

        }
        private void fxAuditTrail(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.AuditTrail(db);
        }
        private void fxAlarms(object sender, RoutedEventArgs e)
        {

        }
        private void fxUserLogInOut(object sender, RoutedEventArgs e)
        {

        }
        private void fxUserNew(object sender, RoutedEventArgs e)
        {

        }
        private void fxUserModify(object sender, RoutedEventArgs e)
        {

        }
        private void fxUserDelete(object sender, RoutedEventArgs e)
        {

        }
        private void Close_App_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}