using Database;
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
        private MyDatabase db;
        private ReadOnlyCollection<DbColumn> columns;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;

        public AuditTrail()
        {
            db = new MyDatabase();
            InitializeComponent();
        }   

        ~AuditTrail()
        {
            //MessageBox.Show("Audit Trail: Au revoir");
        }

        private void LoadAuditTrail(object sender, RoutedEventArgs e)
        {
            DataTable dt = new DataTable();
            DataRow row;
            string[] array;
            string[] columnNames = MySettings["Columns"].Split(',');
            int i = 0;


            if (db.IsConnected()) // while loop is better
            {
                columns = db.SendCommand_readAll(MySettings["Table_Name"].ToString());

                //Création des colonnes
                foreach (DbColumn column in columns)
                {
                    dt.Columns.Add(new DataColumn(columnNames[int.Parse(column.ColumnName.Substring(1))]));
                    i++;
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
                db.Disconnect();
            }
            else
            {
                dt.Columns.Add(new DataColumn("Erreur"));
                row = dt.NewRow();
                row.ItemArray = new string[]  { "Base de données déconnectée" };
                dt.Rows.Add(row);
                dataGridAuditTrail.ItemsSource = dt.DefaultView;
            }
        }
    }
}
