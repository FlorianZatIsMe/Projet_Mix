using Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour AuditTrail.xaml
    /// </summary>
    public partial class AuditTrail : Page
    {
        //private MyDatabase db = new MyDatabase();
        //private ReadOnlyCollection<DbColumn> columns;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private bool tbBefSelToUpdate = false;
        private bool tbBefFull = false;
        private bool tbAftSelToUpdate = false;
        private bool tbAftFull = false;
        private bool dpBefSelToUpdate = false;
        private bool dpAftSelToUpdate = false;

        private readonly int numberOfDaysBefore = 2;

        public AuditTrail()
        {
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
            //MessageBox.Show("Audit Trail: Au revoir");
        }
        private void LoadAuditTrail(object sender, RoutedEventArgs e)
        {
            UpdateAuditTrail();// DateTime.Now.AddDays(-numberOfDaysBefore), DateTime.Now);
        }
        private void UpdateAuditTrail()
        {
            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            string[] columnNames = MySettings["Columns"].Split(',');
            DateTime dtBefore = Convert.ToDateTime(((DateTime)dpDateBefore.SelectedDate).ToString("dd.MM.yyyy") + " " + tbTimeBefore.Text);
            DateTime dtAfter = Convert.ToDateTime(((DateTime)dpDateAfter.SelectedDate).ToString("dd.MM.yyyy") + " " + tbTimeAfter.Text);
            List<string> eventTypes = new List<string>();
            int mutexID = -1;

            if ((bool)cbEvent.IsChecked) eventTypes.Add("Evènement");
            if ((bool)cbAlarm.IsChecked) eventTypes.Add("Alarme");
            if ((bool)cbWarning.IsChecked) eventTypes.Add("Alerte");

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (!MyDatabase.IsConnected()) // while loop is better
            {
                dt.Columns.Add(new DataColumn("Erreur"));
                row = dt.NewRow();
                row.ItemArray = new string[] { "Base de données déconnectée" };
                dt.Rows.Add(row);
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
            }
            else
            {
                mutexID = MyDatabase.SendCommand_ReadAuditTrail(dtBefore: dtBefore, dtAfter: dtAfter, eventTypes: eventTypes.ToArray(), orderBy: "id", isOrderAsc: false, isMutexReleased: false);

                //Création des colonnes
                foreach (string columnName in columnNames)
                {
                    dt.Columns.Add(new DataColumn(columnName));
                }

                //Ajout des lignes
                do
                {
                    array = MyDatabase.ReadNext(mutexID);

                    if (array.Count() != 0)
                    {
                        try
                        {
                            array[1] = Convert.ToDateTime(array[1]).ToString("dd.MMMyyyy HH:mm:ss");
                        }
                        catch (Exception)
                        {

                        }

                        row = dt.NewRow();
                        row.ItemArray = array;
                        dt.Rows.Add(row);
                    }
                } while (array.Count() != 0);

                //Implémentation dans la DataGrid dataGridAuditTrail
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
                dataGridAuditTrail.Columns[0].Visibility = Visibility.Collapsed;
                //MyDatabase.Disconnect();
            }

            MyDatabase.Disconnect(mutexID);
        }
        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            UpdateAuditTrail();
        }
        private void TbTimeBefore_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbBefSelToUpdate = true;
        }
        private void TbTimeBefore_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbBefSelToUpdate)
            {
                int n = (int)(tbTimeBefore.CaretIndex / 3);
                tbTimeBefore.Select(n * 3, 2);
                tbBefSelToUpdate = false;
            }
            else if (tbBefFull)
            {
                MoveTimeCursor(tbTimeBefore, false);
                tbBefFull = false;
            }
        }
        private void TbTimeBefore_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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
            tbAftSelToUpdate = true;
        }
        private void TbTimeAfter_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbAftSelToUpdate)
            {
                int n = (int)(tbTimeAfter.CaretIndex / 3);
                tbTimeAfter.Select(n * 3, 2);
                tbAftSelToUpdate = false;
            }
            else if (tbAftFull)
            {
                MoveTimeCursor(tbTimeAfter, false);
                tbAftFull = false;
            }
        }
        private void TbTimeAfter_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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
                dpDateAfter.DisplayDateStart = dpDateBefore.SelectedDate;
                dpBefSelToUpdate = false;
            }
        }
        private void DpDateBefore_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            dpBefSelToUpdate = true;
        }
        private void DpDateAfter_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            dpAftSelToUpdate = true;
        }
        private void DpDateAfter_LayoutUpdated(object sender, EventArgs e)
        {
            if (dpAftSelToUpdate)
            {
                dpDateBefore.DisplayDateEnd = dpDateAfter.SelectedDate;
                dpAftSelToUpdate = false;
            }
        }
    }
}
