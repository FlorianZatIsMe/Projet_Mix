using Alarm_Management;
using Database;
using MixingApplication.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour AuditTrail.xaml
    /// </summary>
    public partial class AuditTrail : Page
    {
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private bool tbBefSelToUpdate = false;
        private bool tbBefFull = false;
        private bool tbAftSelToUpdate = false;
        private bool tbAftFull = false;
        private bool dpBefSelToUpdate = false;
        private bool dpAftSelToUpdate = false;

        private readonly int numberOfDaysBefore = 2;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public AuditTrail()
        {
            logger.Debug("Start");

            InitializeComponent();
            
            dpDateAfter.SelectedDate = DateTime.Now;
            dpDateAfter.DisplayDateStart = DateTime.Now.AddDays(-numberOfDaysBefore);
            dpDateAfter.DisplayDateEnd = DateTime.Now;
            dpDateAfter.SelectedDateFormat = DatePickerFormat.Long;

            dpDateBefore.SelectedDate = dpDateAfter.DisplayDateStart;
            dpDateBefore.DisplayDateEnd = dpDateAfter.SelectedDate;
            dpDateBefore.SelectedDateFormat = DatePickerFormat.Long;

            tbTimeBefore.Text = DateTime.Now.ToString("HH:mm:ss");
            tbTimeAfter.Text = tbTimeBefore.Text;
        }   
        ~AuditTrail()
        {
            //General.ShowMessageBox("Audit Trail: Au revoir");
        }
        private void LoadAuditTrail(object sender, RoutedEventArgs e)
        {
            logger.Debug("LoadAuditTrail");

            UpdateAuditTrail();
        }
        private void UpdateAuditTrail()
        {
            logger.Debug("UpdateAuditTrail");

            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            //string[] columnNames = MySettings["Columns"].Split(',');
            DateTime dtBefore = Convert.ToDateTime(((DateTime)dpDateBefore.SelectedDate).ToString("dd.MM.yyyy") + " " + tbTimeBefore.Text);
            DateTime dtAfter = Convert.ToDateTime(((DateTime)dpDateAfter.SelectedDate).ToString("dd.MM.yyyy") + " " + tbTimeAfter.Text);
            List<string> eventTypes = new List<string>();
            int mutexID = -1;
            // change mutex by a read all (penser à limiter le nombre de ligne max)
            if ((bool)cbEvent.IsChecked) eventTypes.Add(Settings.Default.General_AuditTrailEvent_Event);
            if ((bool)cbAlarm.IsChecked) eventTypes.Add(AlarmSettings.AlarmType_Alarm);
            if ((bool)cbWarning.IsChecked) eventTypes.Add(AlarmSettings.AlarmType_Warning);

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            //if (!MyDatabase.IsConnected()) // while loop is better
            if(false)
            {
                dt.Columns.Add(new DataColumn(Settings.Default.AuditTrail_ErrorTitle));
                row = dt.NewRow();
                row.ItemArray = new string[] { DatabaseSettings.Error_connectToDbFailed };
                dt.Rows.Add(row);
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
            }
            else
            {
                ReadInfo readInfo = new ReadInfo(
                    _dtBefore: dtBefore,
                    _dtAfter: dtAfter, 
                    _eventTypes: eventTypes.ToArray(),
                    _orderBy: auditTrailInfo.Columns[auditTrailInfo.Id].Id,
                    _isOrderAsc: false);

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetAuditTrailRows(readInfo); });
                List<string[]> tables = (List<string[]>)t.Result;
                //List<string[]> tables = MyDatabase.GetAuditTrailRows(readInfo);

                //mutexID = MyDatabase.SendCommand_ReadAuditTrail(dtBefore: dtBefore, dtAfter: dtAfter, eventTypes: eventTypes.ToArray(), orderBy: auditTrailInfo.columns[auditTrailInfo.id].id, isOrderAsc: false, isMutexReleased: false);

                //Création des colonnes
                foreach (Column column in auditTrailInfo.Columns)
                {
                    dt.Columns.Add(new DataColumn(column.DisplayName));
                }

                for (int i = 0; i < tables.Count; i++)
                {
                    try
                    {
                        tables[i][auditTrailInfo.DateTime] = Convert.ToDateTime(tables[i][auditTrailInfo.DateTime]).ToString("dd.MMMyyyy HH:mm:ss");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                    }

                    row = dt.NewRow();
                    row.ItemArray = tables[i];
                    dt.Rows.Add(row);
                }
                /*
                //Ajout des lignes
                do
                {
                    array = MyDatabase.ReadNext(mutexID);

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
                } while (array != null);
                */
                //Implémentation dans la DataGrid dataGridAuditTrail
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
                dataGridAuditTrail.Columns[auditTrailInfo.Id].Visibility = Visibility.Collapsed;
            }

            //MyDatabase.Disconnect();
            //MyDatabase.Disconnect(mutexID);
        }
        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonFilter_Click");

            UpdateAuditTrail();
        }
        private void TbTimeBefore_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logger.Debug("TbTimeBefore_PreviewMouseLeftButtonDown");

            tbBefSelToUpdate = true;
        }
        private void TbTimeBefore_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbBefSelToUpdate)
            {
                logger.Debug("TbTimeBefore_LayoutUpdated tbBefSelToUpdate");
                int n = (int)(tbTimeBefore.CaretIndex / 3);
                tbTimeBefore.Select(n * 3, 2);
                tbBefSelToUpdate = false;
            }
            else if (tbBefFull)
            {
                logger.Debug("TbTimeBefore_LayoutUpdated tbBefFull");
                MoveTimeCursor(tbTimeBefore, false);
                tbBefFull = false;
            }
        }
        private void TbTimeBefore_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            logger.Debug("TbTimeBefore_PreviewKeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == System.Windows.Input.Key.Right)
            {
                MoveTimeCursor(textbox, false);
                tbBefSelToUpdate = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                MoveTimeCursor(textbox, true);
                tbBefSelToUpdate = true;
            }
            else if (textbox.Text.Length == 7)
            {
                tbBefFull = true;
            }
        }
        private void TbTimeAfter_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logger.Debug("TbTimeAfter_PreviewMouseLeftButtonDown");

            tbAftSelToUpdate = true;
        }
        private void TbTimeAfter_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbAftSelToUpdate)
            {
                logger.Debug("TbTimeAfter_LayoutUpdated tbAftSelToUpdate");
                int n = (int)(tbTimeAfter.CaretIndex / 3);
                tbTimeAfter.Select(n * 3, 2);
                tbAftSelToUpdate = false;
            }
            else if (tbAftFull)
            {
                logger.Debug("TbTimeAfter_LayoutUpdated tbAftFull");
                MoveTimeCursor(tbTimeAfter, false);
                tbAftFull = false;
            }
        }
        private void TbTimeAfter_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            logger.Debug("TbTimeAfter_PreviewKeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == System.Windows.Input.Key.Right)
            {
                MoveTimeCursor(textbox, false);
                tbAftSelToUpdate = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                MoveTimeCursor(textbox, true);
                tbAftSelToUpdate = true;
            }
            else if (textbox.Text.Length == 7)
            {
                tbAftFull = true;
            }
        }
        private void CheckTime(TextBox textbox, int n)
        {
            logger.Debug("CheckTime");

            int max = n == 0 ? 23 : (n == 1 ? 59 : 59);
            string nextText;

            string time;

            if (textbox.Text.Length == 8)
            {
                time = textbox.Text.Substring(n * 3, 2);
                textbox.Text = textbox.Text.Remove(n * 3, 2);
            }
            else if (textbox.Text.Length == 7)
            {
                time = textbox.Text.Substring(n * 3, 1);
                time = "0" + time;
                textbox.Text = textbox.Text.Remove(n * 3, 1);
            }
            else
            {
                time = "00";
            }

            try
            {
                if (int.Parse(time) > max)
                {
                    time = max.ToString();
                }
            }
            catch (Exception)
            {
                time = "00";
            }

            nextText = textbox.Text.Insert(n * 3, time);

            try
            {
                Convert.ToDateTime(nextText);
            }
            catch (Exception)
            {
                nextText = "00:00:00";
            }

            textbox.Text = nextText;
        }
        private void MoveTimeCursor(TextBox textbox, bool left)
        {
            logger.Debug("MoveTimeCursor");

            int n = (int)(textbox.CaretIndex / 3);

            CheckTime(textbox, n);

            if (left)
            {
                n = n == 0 ? 0 : n - 1;
            }
            else
            {
                n = n == 2 ? 2 : n + 1;
            }
            textbox.Select(n * 3, 2);
        }
        private void DpDateBefore_LayoutUpdated(object sender, EventArgs e)
        {
            if (dpBefSelToUpdate)
            {
                logger.Debug("DpDateBefore_LayoutUpdated dpBefSelToUpdate");
                dpDateAfter.DisplayDateStart = dpDateBefore.SelectedDate;
                dpBefSelToUpdate = false;
            }
        }
        private void DpDateBefore_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logger.Debug("DpDateBefore_PreviewMouseDown");

            dpBefSelToUpdate = true;
        }
        private void DpDateAfter_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            logger.Debug("DpDateAfter_PreviewMouseDown");

            dpAftSelToUpdate = true;
        }
        private void DpDateAfter_LayoutUpdated(object sender, EventArgs e)
        {
            if (dpAftSelToUpdate)
            {
                logger.Debug("DpDateAfter_LayoutUpdated dpAftSelToUpdate");
                dpDateBefore.DisplayDateEnd = dpDateAfter.SelectedDate;
                dpAftSelToUpdate = false;
            }
        }
        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            logger.Debug("FrameMain_ContentRendered");
            Frame frameMain = new Frame();
            if (frameMain.Content != this)
            {
                // if no alarm and not deconected... (to add)
                //MyDatabase.Disconnect();

                frameMain.ContentRendered -= FrameMain_ContentRendered;
                //updateAlarmTimer.Dispose();
                //Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
            }

        }
    }
}
