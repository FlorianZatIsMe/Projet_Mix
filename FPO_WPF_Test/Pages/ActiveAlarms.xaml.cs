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
        private MyDatabase db;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        public ActiveAlarms()
        {
            db = new MyDatabase();
            InitializeComponent();
        }

        private void ButtonAckAll_Click(object sender, RoutedEventArgs e)
        {
            List<Tuple<int, int>> listId = new List<Tuple<int, int>>();

            foreach (Tuple<int, int> id in AlarmManagement.activeAlarms)
            {
                listId.Add(id);
            }

            foreach (Tuple<int, int> id in listId)
            {
                AlarmManagement.AcknowledgeAlarm(id.Item1, id.Item2);
            }

            LoadAlarms(sender, e);
        }

        private void LoadAlarms(object sender, RoutedEventArgs e)
        {
            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            string[] columnNames = MySettings["Columns"].Split(',');
            int i = 0;
            List<string> listId = new List<string>();

            if (db.IsConnected()) // while loop is better
            {
                foreach (Tuple<int,int> id in AlarmManagement.activeAlarms)
                {
                    listId.Add(AlarmManagement.alarms[id.Item1, id.Item2].id.ToString());
                }

                db.SendCommand_Read(MySettings["Table_Name"].ToString(), whereColumns: new string[] { "id" }, whereValues: listId.ToArray(), orderBy: "id", isOrderAsc: true);

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

                //Implémentation dans la DataGrid 
                dataGridAlarms.ItemsSource = dt.DefaultView;
                dataGridAlarms.Columns[0].Visibility = Visibility.Collapsed;
                //db.Disconnect();
            }
            else
            {
                dt.Columns.Add(new DataColumn("Erreur"));
                row = dt.NewRow();
                row.ItemArray = new string[] { "Base de données déconnectée" };
                dt.Rows.Add(row);
                dataGridAlarms.ItemsSource = dt.DefaultView;
            }
        }
    }
}
