using Alarm_Management;
using Database;
using FPO_WPF_Test.Properties;
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
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

        private readonly Frame frameMain;
        private readonly System.Timers.Timer updateAlarmTimer;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ActiveAlarms(Frame frameMain_arg)
        {
            logger.Debug("Start");

            // if alarm active and not connected... (to add)
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            frameMain = frameMain_arg;
            frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            // Initialisation des timers
            updateAlarmTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.ActiveAlarms_updateAlarmTimer_Interval,
                AutoReset = false
            };

            updateAlarmTimer.Elapsed += UpdateAlarmTimer_OnTimedEvent;

            InitializeComponent(); 
        }
        private void LoadAlarms()
        {
            logger.Debug("LoadAlarms");

            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            //string[] columnNames = MySettings["Columns"].Split(',');

            if (MyDatabase.IsConnected()) // while loop is better
            {
                try
                {
                    //Création des colonnes
                    foreach (Column column in auditTrailInfo.columns)
                    {
                        dt.Columns.Add(new DataColumn(column.displayName));
                    }

                    foreach (Tuple<int, int> id in AlarmManagement.ActiveAlarms)
                    {
                        array = MyDatabase.GetOneArrayRow(new AuditTrailInfo(), AlarmManagement.Alarms[id.Item1, id.Item2].id.ToString());

                        if (array != null)
                        {
                            try
                            {
                                array[auditTrailInfo.dateTime] = Convert.ToDateTime(array[auditTrailInfo.dateTime]).ToString("dd.MMMyyyy HH:mm:ss");
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                            }

                            row = dt.NewRow();
                            row.ItemArray = array;
                            dt.Rows.Add(row);
                        }
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        //Implémentation dans la DataGrid 
                        dataGridAlarms.ItemsSource = dt.DefaultView;
                        dataGridAlarms.Columns[auditTrailInfo.id].Visibility = Visibility.Collapsed;
                    });
                }
                catch (Exception) { }
            }
            else
            {
                dt.Columns.Add(new DataColumn(Settings.Default.AuditTrail_ErrorTitle));
                row = dt.NewRow();
                row.ItemArray = new string[] { DatabaseSettings.Error01 };
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
            logger.Debug("ButtonAckAll_Click");

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
            logger.Debug("dataGridAlarms_Loaded");

            updateAlarmTimer.Start();
        }
        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            logger.Debug("FrameMain_ContentRendered");

            if (frameMain.Content != this)
            {
                // if no alarm and not deconected... (to add)
                MyDatabase.Disconnect();

                frameMain.ContentRendered -= FrameMain_ContentRendered;
                //stopUpdating = true;
                updateAlarmTimer.Dispose();
                //Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
            }

        }
        private void UpdateAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            logger.Debug("UpdateAlarmTimer_OnTimedEvent");

            LoadAlarms();
            updateAlarmTimer.Enabled = true;
        }
    }
}
