using Database;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
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

        public AuditTrail(MyDatabase db_arg)
        {
            db = db_arg;
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
            int i = 0;

            if (db.isConnected()) // while loop is better
            {
                columns = db.sendCommand_readAll();

                foreach (DbColumn column in columns)
                {
                    dt.Columns.Add(new DataColumn(column.ColumnName));
                    i++;
                }

                do
                {
                    array = db.readNext();

                    if (array.Count() != 0)
                    {
                        row = dt.NewRow();
                        row.ItemArray = array;
                        dt.Rows.Add(row);
                    }
                } while (array.Count() != 0);

                dataGridAuditTrail.ItemsSource = dt.DefaultView;
            }
            else
            {
                MessageBox.Show("Database not connected - Connection status: " + db.isConnected().ToString());
            }
        }
    }
}
