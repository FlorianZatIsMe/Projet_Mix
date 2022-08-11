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
using System.Threading;

namespace Database
{
    public class MyDatabase
    {
        private readonly Configuration.Connection_Info MySettings;
        private readonly NameValueCollection ConnectionAttempSettings = ConfigurationManager.GetSection("Database/Connection_Attempt") as NameValueCollection;
        private MySqlConnection connection;
        private MySqlDataReader reader;

        public MyDatabase()
        {
            MySettings = ConfigurationManager.GetSection("Database/Connection_Info") as Configuration.Connection_Info;
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

        public async void ConnectAsync()
        {
            int attemptsNumber = 0;
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

            while (!this.IsConnected() && attemptsNumber < int.Parse(ConnectionAttempSettings["Max"].ToString()))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                attemptsNumber++;
            }

            if (!this.IsConnected())
            {
                MessageBox.Show("Connection to database failed");
            }
        }

        public void Connect()
        {
            int attemptsNumber = 0;
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

            while (!this.IsConnected() && attemptsNumber < int.Parse(ConnectionAttempSettings["Max"].ToString()))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                attemptsNumber++;
            }

            if (!this.IsConnected())
            {
                MessageBox.Show("Connection to database failed");
            }
        }

        public void Disconnect()
        {
            /*
             * Try something
             */
            connection.Close();
        }

        public bool IsConnected()
        {
            return connection.State == System.Data.ConnectionState.Open;
        }

        public ReadOnlyCollection<DbColumn> SendCommand_readAll(string tableName)
        {
            /*
             * Ajouter un check surtout fussionne avec l'autre
             */

            MySqlCommand command = new MySqlCommand("SELECT * FROM " + tableName + " ORDER BY c00 DESC;", connection);
            reader = command.ExecuteReader();
            return reader.GetColumnSchema();
        }

    public ReadOnlyCollection<DbColumn> SendCommand_readAllRecipe(string tableName, string[] whereColumns, string[] whereValues)
        {
            /*
             * Add check surtout fussionne avec l'autre
             */

            string whereArg = whereColumns[0] + "=@" + whereColumns[0];

            for (int i = 1; i < whereColumns.Count(); i++)
            {
                whereArg = whereArg + " AND " + whereColumns[i] + "=@" + whereColumns[i];
            }

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT * FROM " + tableName + " WHERE " + whereArg;

            for (int i = 0; i < whereColumns.Count(); i++)
            {
                command.Parameters.AddWithValue("@" + whereColumns[i], whereValues[i]);
            }

            try
            {
                reader = command.ExecuteReader();
                return reader.GetColumnSchema();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public int GetMax(string tableName, string column, string[] whereColumns = null, string[] whereValues = null)
        {
            int n;
            string whereArg = "";
            MySqlCommand command;
            
            if (whereColumns != null && whereValues != null)
            {
                whereArg = " WHERE " + whereColumns[0] + "=@" + whereColumns[0];

                for (int i = 1; i < whereColumns.Count(); i++)
                {
                    whereArg = whereArg + " AND " + whereColumns[i] + "=@" + whereColumns[i];
                }
            }

            command = connection.CreateCommand();
            command.CommandText = @"SELECT MAX(" + column + ") FROM " + tableName + whereArg;

            if (whereColumns != null && whereValues != null)
            {
                for (int i = 0; i < whereColumns.Count(); i++)
                {
                    command.Parameters.AddWithValue("@" + whereColumns[i], whereValues[i]);
                }
            }

            try
            {
                reader = command.ExecuteReader();
                reader.Read();

                if (!reader.IsDBNull(0))
                {
                    n = reader.GetInt32(0);
                    reader.Read();
                    if (reader.FieldCount == 0)
                    {
                        n = -1;
                    }
                }
                else
                {
                    n = 0;
                }
                Close_reader();
                return n;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return -1;
            }
        }

    public string[] ReadNext()
        {
            /*
             * Ajouter un check
             */
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
        public bool SendCommand_insertRecord(string tableName, string columnFields, string[] values)
        {
            int valuesNumber = values.Count();
            string[] valueTags = new string[valuesNumber];
            string valueFields = "";

            //MessageBox.Show("columnFields: " + $"{columnFields.Split().Count()}" + " ; valuesNumber: " + $"{valuesNumber}");

            if (columnFields.Split().Count() == valuesNumber)
            {
                for (int i = 0; i < valuesNumber - 1; i++)
                {
                    valueTags[i] = "@value" + i.ToString();
                    valueFields = valueFields + "@value" + i.ToString() + ", ";
                }

                valueTags[valuesNumber - 1] = "@value" + (valuesNumber - 1).ToString();
                valueFields = valueFields + "@value" + (valuesNumber - 1).ToString();

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"INSERT INTO " + tableName + " (" + columnFields + ") VALUES (" + valueFields + ");";
                for (int i = 0; i < valuesNumber; i++)
                {
                    command.Parameters.AddWithValue(valueTags[i], values[i]);
                }

                try
                {
                    reader = command.ExecuteReader();
                    Close_reader();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return false;
                }

            }
            else
            {
                MessageBox.Show("SendCommand_insertRecord: C'est pas bien ce que tu fais là");
            }

            return true;
        }

        public bool DeleteRow(string tableName, string[] whereColumns, string[] whereValues)
        {
            string whereArg;
            MySqlCommand command;
            bool isCommandOk = true;

            whereArg = GetWhereArg(whereColumns, whereValues);

            command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM " + tableName + whereArg;

            if (whereColumns != null && whereValues != null)
            {
                isCommandOk = SetCommand(command, whereColumns, whereValues);
            }

            if (isCommandOk  && whereArg != "")
            {
                try
                {
                    reader = command.ExecuteReader();
                    Close_reader();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                MessageBox.Show("Je n'ai pas pu supprimer la ligne demandée");
            }

            return false;
        }

        public void Close_reader()
        {
            reader.Close();
        }

        private string GetWhereArg(string[] whereColumns, string[] whereValues)
        {
            string whereArg;

            if (whereColumns != null && whereValues != null && whereColumns.Count() == whereValues.Count())
            {
                whereArg = " WHERE " + whereColumns[0] + "=@" + whereColumns[0];

                for (int i = 1; i < whereColumns.Count(); i++)
                {
                    whereArg = whereArg + " AND " + whereColumns[i] + "=@" + whereColumns[i];
                }
            }
            else
            {
                whereArg = "";
            }

            return whereArg;
        }

        private bool SetCommand(MySqlCommand command, string[] columns, string[] values)
        {
            if (columns.Count() == values.Count())
            {
                for (int i = 0; i < columns.Count(); i++)
                {
                    command.Parameters.AddWithValue("@" + columns[i], values[i]);
                }
                return true;
            }

            return false;
        }
    }
}
