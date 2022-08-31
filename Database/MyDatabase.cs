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
using System.Reflection;

namespace Database
{
    public class MyDatabase
    {
        private readonly Configuration.Connection_Info MySettings = ConfigurationManager.GetSection("Database/Connection_Info") as Configuration.Connection_Info;
        private readonly NameValueCollection ConnectionAttempSettings = ConfigurationManager.GetSection("Database/Connection_Attempt") as NameValueCollection;
        private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private MySqlConnection connection;
        private MySqlDataReader reader;
        public static List<int> AlarmListID = new List<int>();
        public static List<string> AlarmListDescription = new List<string>();
        public static List<string> AlarmListStatus = new List<string>();

        public void NewAlarm(string alarmDescription)
        {
            int n = -1;

            for (int i = 0; i < AlarmListDescription.Count; i++)
            {
                if (AlarmListDescription[i] == alarmDescription)
                {
                    n = i;
                    break;
                }
            }

            if (n == -1 || AlarmListStatus[n] != "ACTIVE")
            {
                int id;
                string statusBefore = (n == -1) ? "RAZ" : AlarmListStatus[n];
                string statusAfter = "ACTIVE";

                string[] values = new string[] { "Système", alarmDescription, statusBefore, statusAfter };
                InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);
                id = GetMax(AuditTrailSettings["Table_Name"], "c00");

                AlarmListID.Add(id);
                AlarmListDescription.Add(alarmDescription);
                AlarmListStatus.Add("ACTIVE");

                if (n != -1)
                {
                    AlarmListID.RemoveAt(n);
                    AlarmListDescription.RemoveAt(n);
                    AlarmListStatus.RemoveAt(n);
                }

                for (int i = 0; i < AlarmListID.Count(); i++)
                {
                    //MessageBox.Show("NewAlarm " + i.ToString() + ", " + AlarmListID[i] + ", " + AlarmListDescription[i] + ", " + AlarmListStatus[i]);
                }
                MessageBox.Show(alarmDescription); // Peut-être afficher la liste des alarmes actives à la place
            }
            else
            {
                MessageBox.Show("Ce n'est pas bien ce que vous faite Monsieur");
            }
        }
        public void InactivateAlarm(string alarmDescription)
        {
            int n = -1;

            for (int i = 0; i < AlarmListDescription.Count; i++)
            {
                if (AlarmListDescription[i] == alarmDescription)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1 && AlarmListStatus[n] != "INACTIVE")
            {
                int id;
                string statusBefore = AlarmListStatus[n];
                string statusAfter = (AlarmListStatus[n] == "ACTIVE") ? "INACTIVE" : "RAZ";

                string[] values = new string[] { "Système", alarmDescription, statusBefore, statusAfter };
                InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                if (statusAfter == "INACTIVE")
                {
                    id = GetMax(AuditTrailSettings["Table_Name"], "c00");

                    AlarmListID.Add(id);
                    AlarmListDescription.Add(alarmDescription);
                    AlarmListStatus.Add("INACTIVE");
                }

                AlarmListID.RemoveAt(n);
                AlarmListDescription.RemoveAt(n);
                AlarmListStatus.RemoveAt(n);

                for (int i = 0; i < AlarmListID.Count(); i++)
                {
                    //MessageBox.Show("InactivateAlarm " + i.ToString() + ", " + AlarmListID[i] + ", " + AlarmListDescription[i] + ", " + AlarmListStatus[i]);
                }

            }
            else
            {
                MessageBox.Show("Tu sais pas ce que tu fais c'est pas vrai !");
            }
        }
        public void AcknowledgeAlarm(string alarmDescription)
        {
            int n = -1;

            for (int i = 0; i < AlarmListDescription.Count; i++)
            {
                if (AlarmListDescription[i] == alarmDescription)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1)
            {
                int id;
                string statusBefore = AlarmListStatus[n];
                string statusAfter = (AlarmListStatus[n] == "ACTIVE") ? "ACK" : "RAZ";

                string[] values = new string[] { "Système", alarmDescription, statusBefore, statusAfter };
                InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                if (statusAfter == "ACK")
                {
                    id = GetMax(AuditTrailSettings["Table_Name"], "c00");

                    AlarmListID.Add(id);
                    AlarmListDescription.Add(alarmDescription);
                    AlarmListStatus.Add("ACK");
                }

                AlarmListID.RemoveAt(n);
                AlarmListDescription.RemoveAt(n);
                AlarmListStatus.RemoveAt(n);


                for (int i = 0; i < AlarmListID.Count(); i++)
                {
                    //MessageBox.Show("AcknowledgeAlarm" + i.ToString() + ", " + AlarmListID[i] + ", " + AlarmListDescription[i] + ", " + AlarmListStatus[i]);
                }

            }
            else
            {
                MessageBox.Show("Tu sais pas ce que tu fais c'est pas vrai !");
            }
        }
        public MyDatabase()
        {
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

            //MessageBox.Show(MySettings.DB_Features.Server + ", " + MySettings.DB_Features.UserID + ", " + MySettings.DB_Features.Password + ", " + MySettings.DB_Features.Database);

            if (!this.IsConnected())
            {
                MessageBox.Show("Connexion à la base de donnée échouée");

                //NewAlarm("ALARME 00.01 - Connexion à la base de donnée échouée");

                //string[] values = new string[] { "Système", "ALARME 00.01 - Connexion à la base de donnée échouée" };
                //InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

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
        public ReadOnlyCollection<DbColumn> SendCommand_Read(string tableName, string selectColumns = "*", string[] whereColumns = null, string[] whereValues = null, string orderBy = null, bool isOrderAsc = true, string groupBy = null)
        {
            string whereArg = " WHERE " + GetArg(whereColumns, whereValues, " AND ");
            string orderArg = "";
            string groupByArg = "";
            bool isCommandOk = true;

            if (orderBy != null)
            {
                orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
            }

            if (groupBy != null)
            {
                groupByArg = " GROUP BY " + groupBy;
            }

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT " + selectColumns + " FROM " + tableName + whereArg + groupByArg + orderArg;

            if (whereColumns != null && whereValues != null)
            {
                isCommandOk = SetCommand(command, whereColumns, whereValues);
            }

            if (isCommandOk)
            {
                try
                {
                    reader = command.ExecuteReader();
                    return reader.GetColumnSchema();
                }
                catch (Exception ex)
                {
                    reader = null;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Création de la commande incorrecte");
            }

            return null;
        }
        public void SendCommand_GetLastRecipes(bool onlyProdRecipes = false)
        {
            string statusFilter = onlyProdRecipes ? "status = 1" : "status <> 2";

            MySqlCommand command = new MySqlCommand("SELECT name, id FROM recipe WHERE ((name, version) IN (SELECT name, MAX(version) FROM recipe GROUP BY name)) AND " + statusFilter + " ORDER BY name;", connection);

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                reader = null;
                MessageBox.Show(e.Message);
            }
        }
        public string[] ReadNext()
        {
            if (!IsReaderNull())
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
            else
            {
                return new string[0];
            }
        }
        public void Close_reader()
        {
            reader.Close();
        }
        public bool IsReaderNull()
        {
            return reader == null;
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

                    if (reader.FieldCount == 0) // je crois que cette vérification ne sert à rien, il faut vérifier que la requête le renvoie plus de résultat
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return -1;
            }
        }
        public bool InsertRow(string tableName, string columnFields, string[] values)
        {
            int valuesNumber = values.Count();
            string[] valueTags = new string[valuesNumber];
            string valueFields = "";

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
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }

            }
            else
            {
                MessageBox.Show("SendCommand_insertRecord: C'est pas bien ce que tu fais là");
            }

            return true;
        }
        public bool Update_Row(string tableName, string[] setColumns, string[] setValues, string id)
        {
            string whereArg = " WHERE id = @id";
            string setArg = " SET " + GetArg(setColumns, setValues, ", ");
            bool isCommandOk = true;

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"UPDATE " + tableName + setArg + whereArg;

            if (setColumns != null && setValues != null)
            {
                isCommandOk = SetCommand(command, setColumns, setValues);
            }

            command.Parameters.AddWithValue("@id", id);

            if (isCommandOk)
            {
                try
                {
                    reader = command.ExecuteReader();
                    reader.Close();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                MessageBox.Show("Ce n'est pas très clair tout ça...");
            }

            reader.Close();
            return false;
        }
        public bool DeleteRow(string tableName, string[] whereColumns, string[] whereValues)
        {
            string whereArg;
            MySqlCommand command;
            bool isCommandOk = true;

            whereArg = " WHERE " + GetArg(whereColumns, whereValues, " AND ");

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
                    Close_reader();
                }
            }
            else
            {
                MessageBox.Show("Je n'ai pas pu supprimer la ligne demandée");
            }

            return false;
        }

        // Méthode outils
        private string GetArg(string[] columns, string[] values, string separator)
        {
            string arg;

            if (columns != null && values != null && columns.Count() == values.Count())
            {
                arg = columns[0] + "=@" + columns[0];

                for (int i = 1; i < columns.Count(); i++)
                {
                    arg = arg + separator + columns[i] + "=@" + columns[i];
                }
            }
            else
            {
                arg = "";
            }

            return arg;
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
