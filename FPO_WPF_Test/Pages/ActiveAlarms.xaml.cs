using Alarm_Management;
using Database;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
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
    /// Logique d'interaction pour ActiveAlarms.xaml
    /// </summary>
    public partial class ActiveAlarms : Page
    {
        //private MyDatabase db;
        private readonly Frame frameMain;
        //private Task updateAlarmTask;
        private bool stopUpdating = false;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private readonly System.Timers.Timer updateAlarmTimer;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ActiveAlarms(Frame frameMain_arg)
        {
            // if alarm active and not connected... (to add)
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            frameMain = frameMain_arg;
            frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            // Initialisation des timers
            updateAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };

            updateAlarmTimer.Elapsed += UpdateAlarmTimer_OnTimedEvent;

            InitializeComponent(); 
        }
        private void LoadAlarms()
        {
            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            string[] columnNames = MySettings["Columns"].Split(',');

            if (MyDatabase.IsConnected()) // while loop is better
            {
                try
                {
                    //Création des colonnes
                    foreach (string columnName in columnNames)
                    {
                        dt.Columns.Add(new DataColumn(columnName));
                    }

                    foreach (Tuple<int, int> id in AlarmManagement.ActiveAlarms)
                    {
                        array = MyDatabase.GetOneRow_array(new AuditTrailInfo(), AlarmManagement.alarms[id.Item1, id.Item2].id.ToString());
                        //array = MyDatabase.GetOneRow(MySettings["Table_Name"].ToString(), whereColumns: new string[] { "id" }, whereValues: new string[] { AlarmManagement.alarms[id.Item1, id.Item2].id.ToString() });

                        if (array != null)
                        {
                            row = dt.NewRow();
                            row.ItemArray = array;
                            dt.Rows.Add(row);
                        }
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        //Implémentation dans la DataGrid 
                        dataGridAlarms.ItemsSource = dt.DefaultView;
                        dataGridAlarms.Columns[0].Visibility = Visibility.Collapsed;
                    });
                }
                catch (Exception) { }
            }
            else
            {
                dt.Columns.Add(new DataColumn("Erreur"));
                row = dt.NewRow();
                row.ItemArray = new string[] { "Base de données déconnectée" };
                dt.Rows.Add(row);

                this.Dispatcher.Invoke(() =>
                {
                    //Implémentation dans la DataGrid 
                    dataGridAlarms.ItemsSource = dt.DefaultView;
                });
            }
        }
        private void ButtonAckAll_Click(object sender, RoutedEventArgs e)
        {
            List<Tuple<int, int>> listId = new List<Tuple<int, int>>();

            foreach (Tuple<int, int> id in AlarmManagement.ActiveAlarms)
            {
                listId.Add(id);
            }

            foreach (Tuple<int, int> id in listId)
            {
                AlarmManagement.AcknowledgeAlarm(id.Item1, id.Item2);
            }

            LoadAlarms();
        }
        private void dataGridAlarms_Loaded(object sender, RoutedEventArgs e)
        {
            //updateAlarmTask = Task.Factory.StartNew(() => UpdateAlarms());
            updateAlarmTimer.Start();
        }
        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            if (frameMain.Content != this)
            {
                // if no alarm and not deconected... (to add)
                MyDatabase.Disconnect();

                frameMain.ContentRendered -= FrameMain_ContentRendered;
                stopUpdating = true;
                updateAlarmTimer.Dispose();
                //Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
            }

        }
        private void UpdateAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            logger.Debug("UpdateAlarmTimer");
            LoadAlarms();
            updateAlarmTimer.Enabled = true;
        }
    }
}
