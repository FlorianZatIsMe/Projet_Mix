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
        private MyDatabase db = new MyDatabase();
        private ReadOnlyCollection<DbColumn> columns;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private bool tbBefSelToUpdate = false;
        private bool tbBefFull = false;
        private bool tbAftSelToUpdate = false;
        private bool tbAftFull = false;

        private int numberOfDaysBefore = 2;

        public AuditTrail()
        {
            InitializeComponent();

            dpDateAfter.SelectedDate = DateTime.Now;
            dpDateAfter.DisplayDateStart = DateTime.Now.AddDays(-numberOfDaysBefore);
            dpDateAfter.DisplayDateEnd = DateTime.Now;
            dpDateAfter.SelectedDateFormat = DatePickerFormat.Long;

            dpDateBefore.SelectedDate = DateTime.Now.AddDays(-numberOfDaysBefore);
            //dpDateBefore.DisplayDateStart = DateTime.Now.AddDays(-numberOfDaysBefore);
            dpDateBefore.DisplayDateEnd = DateTime.Now.AddDays(-numberOfDaysBefore);
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
            updateAuditTrail(DateTime.Now.AddDays(-numberOfDaysBefore), DateTime.Now);
        }

        private void updateAuditTrail(DateTime dtBefore, DateTime dtAfter, string[] eventTypes = null)
        {
            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            string[] columnNames = MySettings["Columns"].Split(',');


            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_ReadAuditTrail(dtBefore: dtBefore, dtAfter: dtAfter, eventTypes: eventTypes, orderBy: "id", isOrderAsc: false);

                //Création des colonnes
                foreach (string columnName in columnNames)
                {
                    dt.Columns.Add(new DataColumn(columnName));
                }

                //Ajout des lignes
                do
                {
                    array = db.ReadNext();

                    if (array.Count() != 0)
                    {
                        row = dt.NewRow();
                        row.ItemArray = array;
                        dt.Rows.Add(row);
                    }
                } while (array.Count() != 0);

                //Implémentation dans la DataGrid dataGridAuditTrail
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
                dataGridAuditTrail.Columns[0].Visibility = Visibility.Collapsed;
                //db.Disconnect();
            }
            else
            {
                dt.Columns.Add(new DataColumn("Erreur"));
                row = dt.NewRow();
                row.ItemArray = new string[] { "Base de données déconnectée" };
                dt.Rows.Add(row);
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
            }
        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            List<string> eventTypes = new List<string>();

            if ((bool)cbEvent.IsChecked) eventTypes.Add("Evènement");
            if ((bool)cbAlarm.IsChecked) eventTypes.Add("Alarme");
            if ((bool)cbWarning.IsChecked) eventTypes.Add("Alerte");

            updateAuditTrail(DateTime.Now.AddDays(-2), DateTime.Now, eventTypes.ToArray());
        }
        private void tbTimeBefore_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbBefSelToUpdate = true;
        }
        private void tbTimeBefore_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbBefSelToUpdate)
            {
                int n = (int)(tbTimeBefore.CaretIndex / 3);
                tbTimeBefore.Select(n * 3, 2);
                tbBefSelToUpdate = false;
            }
            else if (tbBefFull)
            {
                moveTimeCursor(tbTimeBefore, false);
                tbBefFull = false;
            }
        }

        private void tbTimeBefore_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (e.Key == System.Windows.Input.Key.Right)
            {
                moveTimeCursor(textbox, false);
                tbBefSelToUpdate = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                moveTimeCursor(textbox, true);
                tbBefSelToUpdate = true;
            }
            else if (textbox.Text.Length == 7)
            {
                tbBefFull = true;
            }
        }
        private void tbTimeAfter_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbAftSelToUpdate = true;
        }
        private void tbTimeAfter_LayoutUpdated(object sender, EventArgs e)
        {
            if (tbAftSelToUpdate)
            {
                int n = (int)(tbTimeAfter.CaretIndex / 3);
                tbTimeAfter.Select(n * 3, 2);
                tbAftSelToUpdate = false;
            }
            else if (tbAftFull)
            {
                moveTimeCursor(tbTimeAfter, false);
                tbAftFull = false;
            }
        }

        private void tbTimeAfter_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (e.Key == System.Windows.Input.Key.Right)
            {
                moveTimeCursor(textbox, false);
                tbAftSelToUpdate = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left)
            {
                moveTimeCursor(textbox, true);
                tbAftSelToUpdate = true;
            }
            else if (textbox.Text.Length == 7)
            {
                tbAftFull = true;
            }
        }

        private void checkTime(TextBox textbox, int n)
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

        private void moveTimeCursor(TextBox textbox, bool left)
        {
            int n = (int)(textbox.CaretIndex / 3);

            checkTime(textbox, n);

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
    }
}
