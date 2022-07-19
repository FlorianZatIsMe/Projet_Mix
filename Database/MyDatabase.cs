using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data;

namespace Database
{
    public class MyDatabase
    {
        private Configuration.DB_Connection_Info MySettings;
        private MySqlConnection connection;
        private MySqlDataReader reader;

        public MyDatabase()
        {
            MySettings = ConfigurationManager.GetSection("DB_Connection_Info") as Configuration.DB_Connection_Info;
            if (MySettings == null)
            {
                MessageBox.Show("Database Settings are not defined");
            }
            else
            {
                Connect();
            }
        }
        ~MyDatabase()
        {
            Disconnect();
            //MessageBox.Show("DB: Au revoir");
        }

        public async void Connect()
        {
            /*
             *  Add a try something here
             *  Add a while loop here
             */

            // set these values correctly for your database server
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = MySettings.DB_Features.Server,
                UserID = MySettings.DB_Features.UserID,
                Password = MySettings.DB_Features.Password,
                Database = MySettings.DB_Features.Database,
            };

            connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            if (isConnected())
            {
                // Do a while loop instead with a timeout
            }
            else
            {
                MessageBox.Show("Connection to database failed");
            }
        }

        private void Disconnect()
        {
            /*
             * Try something
             */
            connection.Close();
        }

        public bool isConnected()
        {
            return connection.State == System.Data.ConnectionState.Open;
        }

        public ReadOnlyCollection<DbColumn> sendCommand_readAll()
        {
            MySqlCommand command = new MySqlCommand("SELECT * FROM audit_trail;", connection);

            // create a DB command and set the SQL statement with parameters
            // execute the command and read the results
            reader = command.ExecuteReader();

            return reader.GetColumnSchema();
        }

        public string[] readNext()
        {
            string[] array = new string[reader.FieldCount];

            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    array[i] = reader[i].ToString();
                }
            }
            else
            {
                return new string[0];
            }

            return array;
        }
    }
}
