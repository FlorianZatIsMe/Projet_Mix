using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using System.Collections.Specialized;
using System.Windows;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace Database
{
    public static class MyDatabase
    {
        private static readonly Configuration.Connection_Info MySettings = ConfigurationManager.GetSection("Database/Connection_Info") as Configuration.Connection_Info;
        //private static readonly NameValueCollection ConnectionAttempSettings = ConfigurationManager.GetSection("Database/Connection_Attempt") as NameValueCollection;
        private static MySqlConnection connection;
        private static MySqlDataReader reader;
        public static List<int> AlarmListID = new List<int>();
        public static List<string> AlarmListDescription = new List<string>();
        public static List<string> AlarmListStatus = new List<string>();
        private readonly static System.Timers.Timer ConnectTimer;
        //private static bool dbReady = true;
        private readonly static List<int> mutexIDs = new List<int>();
        private static int lastMutexID = 0;

        public enum RecipeStatus
        {
            PROD,
            DRAFT,
            OBSOLETE,
            PRODnDRAFT
        }
        private static int GetNextMutex()
        { // Assure toi que cette fonction ne peut être appelé plusieurs fois en même temps
            lastMutexID = (lastMutexID + 1) % 200;
            return lastMutexID;
        }
        public static void Signal(int mutex)
        {
            if (mutex == mutexIDs[0])
            {
                WriteInTextFile2("signal");
                mutexIDs.RemoveAt(0);
            }
            else
            {
                WriteInTextFile2("signal - On a un problème chef");
            }
        }
        private static void WriteInTextFile2(string title)
        {/*
            string text = title + ": ";
            for (int j = 0; j < mutexIDs.Count; j++)
            {
                text = text + mutexIDs[j].ToString() + " ";
            }

            string fileName = @"C:\Temp\Bonjour.txt";
            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
            }


            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = File.Open(fileName, FileMode.Append))
                    {
                        byte[] author = new UTF8Encoding(true).GetBytes(DateTime.Now.ToString() + " " + text + "\n");
                        fs.Write(author, 0, author.Length);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(DateTime.Now.ToString() + " - " + ex);
                }
            }//*/
        }
        private static void WriteInTextFile(string text, int i)
        {/*
            string fileName = @"C:\Temp\Bonjour.txt";
            if (!File.Exists(fileName)) 
            {
                using (FileStream fs = File.Create(fileName)) { }
            }

            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = File.Open(fileName, FileMode.Append))
                    {
                        byte[] author = new UTF8Encoding(true).GetBytes(DateTime.Now.ToString() + " " + text + "\n");
                        fs.Write(author, 0, author.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(DateTime.Now.ToString() + " - " + ex);
                }
            }//*/
        }
        static MyDatabase()
        {
            if (MySettings == null)
            {
                MessageBox.Show("Database Settings are not defined");
            }
            else
            {
                Connect();

                //CreateTempTable2("date_time  TIMESTAMP, description CHAR(100)");

                // Initialisation des timers
                ConnectTimer = new System.Timers.Timer
                {
                    Interval = 1000
                };
                ConnectTimer.Elapsed += SeqTimer_OnTimedEvent;
                ConnectTimer.AutoReset = true;

                ConnectTimer.Start();

            }
        }
        private static void SeqTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e) 
        {
            if (!IsConnected())
            {
                MessageBox.Show("seqTimer_OnTimedEvent " + IsConnected().ToString());
                Connect();
            }
        }
        public static async void ConnectAsync()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = MySettings.DB_Features.Server,
                UserID = MySettings.DB_Features.UserID,
                Password = MySettings.DB_Features.Password,
                Database = MySettings.DB_Features.Database,
            };

            connection = new MySqlConnection(builder.ConnectionString);

            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static void Connect()
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("Connect", 0);
            WriteInTextFile2("wait");

            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = MySettings.DB_Features.Server,
                UserID = MySettings.DB_Features.UserID,
                Password = MySettings.DB_Features.Password,
                Database = MySettings.DB_Features.Database,
                AllowZeroDateTime = true,
            };

            connection = new MySqlConnection(builder.ConnectionString);

            try { connection.Open(); }
            catch (Exception) {}

            Signal(mutexID);
            //dbReady = true;
        }
        public static void Disconnect()
        {
            /*
             * Try something
             */
            ConnectTimer.Stop();
            connection.Close();
        }
        public static bool IsConnected()
        {
            if (connection == null) return false;

            return connection.State == System.Data.ConnectionState.Open;
        }
        public static void SendCommand_readAllRecipe(string tableName, string[] whereColumns, string[] whereValues, int mutex = -1)
        {
            //wait();
            int mutexID;

            if (mutex ==-1)
            {
                mutexID = GetNextMutex();
                mutexIDs.Add(mutexID);
            }
            else
            {
                mutexID = mutex;
            }
            while (mutexIDs[0] != mutexID) Task.Delay(25);
            WriteInTextFile("SendCommand_readAllRecipe " + tableName, 1);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();
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
                }
                catch (Exception e)
                {
                    MessageBox.Show("SendCommand_readAllRecipe - " + e.Message);
                    reader =  null;
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_readAllRecipe - Connection à la base de données échouée");
            }
            if(mutex == -1) Signal(mutexID);
            //dbReady = true;
        }
        public static int SendCommand_Read(string tableName, string selectColumns = "*", string[] whereColumns = null, string[] whereValues = null, string orderBy = null, bool isOrderAsc = true, string groupBy = null, bool isMutexReleased = true, int mutex = -1)
        {
            //wait();
            int mutexID;

            if (mutex == -1)
            {
                mutexID = GetNextMutex();
                mutexIDs.Add(mutexID);
            }
            else
            {
                mutexID = mutex;
            }
            while (mutexIDs[0] != mutexID) Task.Delay(25);

            string whereColumns_s = "";
            for (int j = 0; j < whereColumns.Length; j++)
            {
                whereColumns_s = whereColumns_s + whereColumns[j] + " ";
            }

            string whereValues_s = "";
            for (int j = 0; j < whereValues.Length; j++)
            {
                whereValues_s = whereValues_s + whereValues[j] + " ";
            }


            WriteInTextFile("SendCommand_Read " + tableName + " " + selectColumns + " " + whereColumns_s + " " + whereValues_s + " " + mutexID.ToString(), 2);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();

                string whereArg = GetArg(whereColumns, whereValues, " AND ", " WHERE ");
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
                //MessageBox.Show(command.CommandText + " - id: " + whereValues[0]);
                if (whereColumns != null && whereValues != null)
                {
                    isCommandOk = SetCommand(command, whereColumns, whereValues);
                }

                if (isCommandOk)
                {
                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        reader = null;
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_Read - Création de la commande incorrecte");
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_Read - Connection à la base de données échouée");
            }
            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static void SendCommand_ReadAuditTrail(DateTime dtBefore, DateTime dtAfter, string[] eventTypes =  null, string orderBy = null, bool isOrderAsc = true)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("SendCommand_ReadAuditTrail", 3);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();

                string whereDateTime = "date_time >= @dtBefore AND date_time <= @dtAfter";
                string eventType = " AND (";
                string orderArg = "";
                bool isCommandOk = true;

                if (eventTypes != null && eventTypes.Length != 0)
                {
                    for (int i = 0; i < eventTypes.Length - 1; i++)
                    {
                        eventType += "event_type = @" + eventTypes[i] + " OR ";
                    }
                    eventType += "event_type = @" + eventTypes[eventTypes.Length - 1] + ")";
                }
                else
                {
                    eventType = "";
                }

                if (orderBy != null)
                {
                    orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
                }

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM audit_trail WHERE " + whereDateTime + eventType + orderArg;
                command.Parameters.AddWithValue("@dtBefore", dtBefore.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@dtAfter", dtAfter.ToString("yyyy-MM-dd HH:mm:ss"));

                if (eventTypes != null)
                {
                    for (int i = 0; i < eventTypes.Length; i++)
                    {
                        command.Parameters.AddWithValue("@" + eventTypes[i], eventTypes[i]);
                    }
                }
                //MessageBox.Show(command.CommandText);
                if (isCommandOk)
                {
                    try
                    {
                        reader = command.ExecuteReader();
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
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadAuditTrail - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
        }
        public static void SendCommand_ReadAlarms(int firstId = -1, int lastId = -1, bool readAlert = false)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);
            WriteInTextFile("SendCommand_ReadAlarms", 3);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();

                bool isCommandOk = firstId == -1 || firstId < lastId;
                string whereId = firstId == -1 ? "" : "id >= @firstId AND id <= @lastId AND ";
                string eventType = readAlert ? "(event_type = 'Alarme' OR event_type = 'Alerte')" : "event_type = 'Alarme'";

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM audit_trail WHERE " + whereId + eventType;
                //MessageBox.Show(command.CommandText);
                if (firstId != -1) SetCommand(command, new string[] { "firstId", "lastId" }, new string[] { firstId.ToString(), lastId.ToString() });

                if (isCommandOk)
                {
                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        reader = null;
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadAlarms: Création de la commande incorrecte");
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadAlarms - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
        }
        public static void SendCommand_GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("SendCommand_GetLastRecipes", 5);
            WriteInTextFile2("wait");

            // only prod pour la prod
            // only draft pour les tests de recette
            // only obsolete pour faire revivre une vieille recette
            // prod and draft pour modifier une recette

            if (IsConnected())
            {
                Close_reader();

                string statusFilter = 
                    status == RecipeStatus.DRAFT ? "status = 0" :
                    status == RecipeStatus.OBSOLETE ? "status = 2" :
                    status == RecipeStatus.PROD ? "status = 1" :
                    status == RecipeStatus.PRODnDRAFT ? "(status = 1 OR status = 0)" : "";

                if (statusFilter == "")
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_GetLastRecipes - Ceci est impossible, je ne vous crois pas Monsieur");
                    return;
                }

                MySqlCommand command = new MySqlCommand("SELECT name, id FROM recipe WHERE ((name, version) IN (SELECT name, MAX(version) FROM recipe GROUP BY name)) AND " + statusFilter + " ORDER BY name;", connection);

                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception e)
                {
                    reader = null;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + e.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_GetLastRecipes - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
        }
        public static string[] ReadNext(int mutex = -1)
        {
            //wait();
            int mutexID;

            if (mutex == -1)
            {
                mutexID = GetNextMutex();
                mutexIDs.Add(mutexID);
            }
            else
            {
                mutexID = mutex;
            }
            while (mutexIDs[0] != mutexID) Task.Delay(25);
            WriteInTextFile("ReadNext", 8);
            WriteInTextFile2("wait");

            string[] array;

            if (IsConnected())
            {
                if (!IsReaderNotAvailable())
                {
                    array = new string[reader.FieldCount];

                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            array[i] = reader[i].ToString();
                        }
                    }
                    else
                    {
                        array = new string[0];
                    }

                    //return array;
                }
                else
                {
                    array = new string[0];
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ReadNext - Connection à la base de données échouée");
                array = new string[0];
            }
            if (mutex == -1) Signal(mutexID);
            return array;
        }
        public static bool[] ReadNextBool()
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("ReadNextBool", 8);
            WriteInTextFile2("wait");

            bool[] array;

            if (IsConnected())
            {
                if (!IsReaderNotAvailable())
                {
                    array = new bool[reader.FieldCount - 2];

                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount - 2; i++)
                        {
                            array[i] = reader.GetBoolean(i + 2);
                        }
                    }
                    else
                    {
                        array = new bool[0];
                    }

                    //return array;
                }
                else
                {
                    array = new bool[0];
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ReadNextBool - Connection à la base de données échouée");
                array = new bool[0];
            }
            Signal(mutexID);
            return array;
        }
        public static void Close_reader()
        {
            if(!IsReaderNotAvailable()) reader.Close();
        }
        public static bool IsReaderNotAvailable()
        {
            return reader == null || reader.IsClosed;
        }
        public static string[] GetOneRow(string tableName, string selectColumns = "*", string[] whereColumns = null, string[] whereValues = null)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("GetOneRow " + tableName, 8);
            WriteInTextFile2("wait");

            string[] array = new string[0];

            if (IsConnected())
            {
                SendCommand_Read(tableName, selectColumns, whereColumns, whereValues, mutex: mutexID);

                array = ReadNext(mutexID);

                if (ReadNext(mutexID).Count() != 0)
                {
                    array = new string[0];
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetOneRow : Oula c'est mauvais ça !");
                }
                Close_reader();
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetOneRow - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
            return array;
        }
        public static int GetMax(string tableName, string column, string[] whereColumns = null, string[] whereValues = null)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("GetMax " + tableName, 9);
            WriteInTextFile2("wait");

            int result = -1;

            if (IsConnected())
            {
                Close_reader();

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
                    result = n;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetMax - " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetMax - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static bool InsertRow(string tableName, string columnFields, string[] values)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("InsertRow " + tableName, 10);
            WriteInTextFile2("wait");

            bool result = false;
            // it must change !!!
            //Disconnect();
            //Connect();

            if (IsConnected())
            {
                Close_reader();

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
                        //command.ExecuteReader();
                        Close_reader();
                        //Disconnect();
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - InsertRow - " + tableName + " - " + ex.Message + " - " + DateTime.Now.ToString());
                    }

                }
                else
                {
                    MessageBox.Show("SendCommand_insertRecord: C'est pas bien ce que tu fais là");
                }

            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - InsertRow - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static bool Update_Row(string tableName, string[] setColumns, string[] setValues, string id)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("Update_Row " + tableName, 11);
            WriteInTextFile2("wait");

            bool result = false;

            if (IsConnected())
            {
                Close_reader();

                string whereArg = " WHERE id = @id";
                string setArg = " SET " + GetArg(setColumns, setValues, ", ");
                bool isCommandOk = true;

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"UPDATE " + tableName + setArg + whereArg;
                //MessageBox.Show(command.CommandText + " - id: " + id);
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
                        Close_reader();
                        result = true;
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
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Update_Row - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static bool DeleteRow(string tableName, string[] whereColumns, string[] whereValues)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("DeleteRow " + tableName, 12);
            WriteInTextFile2("wait");

            bool result = false;

            if (IsConnected())
            {
                Close_reader();

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

                if (isCommandOk && whereArg != "")
                {
                    try
                    {
                        reader = command.ExecuteReader();
                        Close_reader();
                        result = true;
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
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - DeleteRow - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static void CreateTempTable(string fields)
        {

            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("CreateTempTable " + fields, 13);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();

                // On supprimer la table temp si jamais elle existe
                MySqlCommand command = connection.CreateCommand();

                try
                {
                    command.CommandText = @"DROP TABLE temp";
                    reader = command.ExecuteReader();
                    Close_reader();
                }
                catch (Exception)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - CreateTempTable: " + ex.Message);
                }

                try
                {
                    command.CommandText = @"CREATE TABLE temp (" +
                        "id  INT NOT NULL auto_increment PRIMARY KEY," +
                        fields + ")";
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - CreateTempTable: " + ex.Message);
                }

                Close_reader();
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_readAllRecipe - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
        }
        /*public static void CreateTempTable2(string fields)
        {
            while (!dbReady) Task.Delay(25);
            dbReady = false;
            if (IsConnected())
            {
                Close_reader();

                // On supprimer la table temp si jamais elle existe
                MySqlCommand command = connection.CreateCommand();

                try
                {
                    command.CommandText = @"DROP TABLE temp2";
                    reader = command.ExecuteReader();
                    Close_reader();
                }
                catch (Exception)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - CreateTempTable: " + ex.Message);
                }

                try
                {
                    command.CommandText = @"CREATE TABLE temp2 (" +
                        "id  INT NOT NULL auto_increment PRIMARY KEY," +
                        fields + ")";
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - CreateTempTable: " + ex.Message);
                }

                Close_reader();
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_readAllRecipe - Connection à la base de données échouée");
            }
            dbReady = true;
        }*/
        public static void SelectFromTemp(string select)
        {

            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            WriteInTextFile("SelectFromTemp " + select, 14);
            WriteInTextFile2("wait");

            if (IsConnected())
            {
                Close_reader();

                try
                {
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = @"SELECT " + select + " FROM temp;";
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SelectFromTemp: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SelectFromTemp - Connection à la base de données échouée");
            }
            Signal(mutexID);
            //dbReady = true;
        }

        // Méthode outils
        private static string GetArg(string[] columns, string[] values, string separator, string prefix = "")
        {
            string arg;

            if (columns != null && values != null && columns.Count() == values.Count())
            {
                arg = prefix + columns[0] + "=@" + columns[0];

                for (int i = 1; i < columns.Count(); i++)
                {
                    arg += separator + columns[i] + "=@" + columns[i];
                }
            }
            else
            {
                arg = "";
            }

            return arg;
        }
        private static bool SetCommand(MySqlCommand command, string[] columns, string[] values)
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
