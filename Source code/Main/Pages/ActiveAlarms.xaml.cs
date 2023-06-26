using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using Alarm_Management;
using Database;
using User_Management;
using Main.Properties;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour ActiveAlarms.xaml
    /// </summary>
    public partial class ActiveAlarms : UserControl
    {
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

        private readonly ContentControl contentControlMain;
        private readonly System.Timers.Timer updateAlarmTimer;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ActiveAlarms(ContentControl contentControlMain_arg)
        {
            logger.Debug("Start");

            contentControlMain = contentControlMain_arg;
            contentControlMain.LayoutUpdated += ContentControlMain_LayoutUpdated;
            //contentControlMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            // Initialisation des timers
            updateAlarmTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.ActiveAlarms_updateAlarmTimer_Interval,
                AutoReset = true
            };

            updateAlarmTimer.Elapsed += UpdateAlarmTimer_OnTimedEvent;

            InitializeComponent();

            bool[] currentAccess = UserManagement.GetCurrentAccessTable();
            btAck.Visibility = currentAccess[AccessTableInfo.AckAlarm] ? Visibility.Visible : Visibility.Hidden;
        }

        private void ContentControlMain_LayoutUpdated(object sender, EventArgs e)
        {
            if (contentControlMain.Content != this)
            {
                logger.Debug("ContentControlMain_LayoutUpdated");
                contentControlMain.LayoutUpdated -= ContentControlMain_LayoutUpdated;
                updateAlarmTimer.Stop();
                updateAlarmTimer.Dispose();
            }
        }

        private void LoadAlarms()
        {
            logger.Debug("LoadAlarms");

            DataTable dt = new DataTable();
            DataRow row;
            object[] values;
            //string[] columnNames = MySettings["Columns"].Split(',');

            try
            {
                //Création des colonnes
                foreach (string columnName in auditTrailInfo.Descriptions)
                {
                    dt.Columns.Add(new DataColumn(columnName));
                }

                foreach (Tuple<int, int> id in AlarmManagement.ActiveAlarms)
                {
                    // A CORRIGER : IF RESULT IS FALSE
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new AuditTrailInfo(), AlarmManagement.Alarms[id.Item1, id.Item2].id); });
                    values = (object[])t.Result;

                    if (values != null)
                    {
                        try
                        {
                            values[auditTrailInfo.DateTime] = Convert.ToDateTime(values[auditTrailInfo.DateTime]).ToString("dd.MMMyyyy HH:mm:ss");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex.Message);
                        }

                        row = dt.NewRow();
                        row.ItemArray = values;
                        dt.Rows.Add(row);
                    }
                }

                this.Dispatcher.Invoke(() =>
                {
                    //Implémentation dans la DataGrid 
                    dataGridAlarms.ItemsSource = dt.DefaultView;
                    dataGridAlarms.Columns[auditTrailInfo.Id].Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception) { }
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
        private void UpdateAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            logger.Debug("UpdateAlarmTimer_OnTimedEvent");

            LoadAlarms();
            //if(updateAlarmTimer != null) updateAlarmTimer.Enabled = true;
        }
    }
}
