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
using Microsoft.Extensions.Configuration;

namespace Database
{
    public static class MyDatabase
    {
        private static readonly Configuration_old.Connection_Info MySettings = System.Configuration.ConfigurationManager.GetSection("Database/Connection_Info") as Configuration_old.Connection_Info;
        //private static readonly NameValueCollection ConnectionAttempSettings = ConfigurationManager.GetSection("Database/Connection_Attempt") as NameValueCollection;
        private static MySqlConnection connection;
        private static MySqlDataReader reader;
        public static List<int> AlarmListID = new List<int>();
        public static List<string> AlarmListDescription = new List<string>();
        public static List<string> AlarmListStatus = new List<string>();
        private static Task scanTask;
        //private static bool dbReady = true;
        private readonly static List<int> mutexIDs = new List<int>();
        private static int lastMutexID = 0;
        private static bool isScanTaskRunning = true;
        public static IConfig config;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public enum RecipeStatus
        {
            PROD,
            DRAFT,
            OBSOLETE,
            PRODnDRAFT
        }
        static MyDatabase()
        {
            Connect();
            //*
            IConfiguration configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile("appSettings.json", optional: false)
            .Build();

            config = configuration.Get<Config>();
            //*/


            //MessageBox.Show(config.FileWriterDestination + ", " + config.MaxRandomInt.ToString() + ", " + config.RandomIntCount.ToString());
        }
        private static async void ScanConnect()
        {

            // to change for a timer

            while (isScanTaskRunning)
            {
                //WriteInTextFile("ScanConnect " + IsConnected().ToString(), 13);

                if (!IsConnected())
                {
                    Connect();
                    logger.Info("ScanConnect" + IsConnected().ToString());
                }
                await Task.Delay(config.ScanConnect_Timer);
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
                logger.Error(ex.Message);
            }
        }
        public static int Wait()
        {
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);
            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("Wait " + GetMutexIDs());

            return mutexID;
        }
        public static int Connect(bool isMutexReleased = true)
        {
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);
            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("Connect " + isMutexReleased.ToString() + GetMutexIDs());

            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = MySettings.DB_Features.Server,
                UserID = MySettings.DB_Features.UserID,
                Password = MySettings.DB_Features.Password,
                Database = MySettings.DB_Features.Database,
                AllowZeroDateTime = true,
                Pooling = true,
                MinimumPoolSize = 2
            };

            connection = new MySqlConnection(builder.ConnectionString);

            try { connection.Open(); }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

            isScanTaskRunning = true;

            // this needs to change put a timer instead

            scanTask = Task.Factory.StartNew(() => ScanConnect());

            if (isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static void Disconnect(int mutex = -1)
        {
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

            logger.Debug("Disconnect " + GetMutexIDs());
            /*
             * Try something
             */
            isScanTaskRunning = false;
            scanTask.Wait(2000);
            connection.Close();

            Signal(mutexID);
            //MessageBox.Show("MAIS ICI !!!");
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

            logger.Debug("SendCommand_readAllRecipe " + tableName + GetMutexIDs());


            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_readAllRecipe - Connection à la base de données échouée");
            }
            else
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
                    logger.Error("SendCommand_readAllRecipe - " + e.Message);
                    MessageBox.Show("SendCommand_readAllRecipe - " + e.Message);
                    reader = null;
                }
            }

            if (mutex == -1) Signal(mutexID);
        }
        public static int SendCommand_Read(string tableName, string selectColumns = "*", string[] whereColumns = null, string[] whereValues = null, string orderBy = null, bool isOrderAsc = true, string groupBy = null, bool isMutexReleased = true, int mutex = -1)
        {
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
            if (whereColumns != null)
            {
                for (int j = 0; j < whereColumns.Length; j++)
                {
                    whereColumns_s = whereColumns_s + whereColumns[j] + " ";
                }
            }

            string whereValues_s = "";
            if (whereValues != null)
            {
                for (int j = 0; j < whereValues.Length; j++)
                {
                    whereValues_s = whereValues_s + whereValues[j] + " ";
                }
            }

            logger.Debug("SendCommand_Read " + tableName + " " + selectColumns + " " + whereColumns_s + " " + whereValues_s + GetMutexIDs());
            
            if (!IsConnected())
            {
                logger.Error("SendCommand_Read - Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_Read - Connection à la base de données échouée");
            }
            else
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
                if (whereColumns != null && whereValues != null)
                {
                    isCommandOk = SetCommand(command, whereColumns, whereValues);
                }

                if (!isCommandOk)
                {
                    logger.Error("SendCommand_Read - Création de la commande incorrecte");
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_Read - Création de la commande incorrecte");
                }
                else
                {
                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        reader = null;
                        logger.Error("SendCommand_Read - " + ex.Message);
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_Read - " + ex.Message);
                    }
                }
            }

            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static int SendCommand(string commandText, bool isMutexReleased = true, int mutex = -1)
        {
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

            logger.Debug("SendCommand_Read " + commandText + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("SendCommand - Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;

                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    reader = null;
                    logger.Error(ex.Message);
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
                }
            }

            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static List<string> GetTablesName()
        {
            //wait();
            int mutexID;
            List<string> list = new List<string>();
            string[] array;

            mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("SendCommand_GetTablesName " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = \""+ MySettings.DB_Features.Database + "\" AND table_type = \"BASE TABLE\";";

                try
                {
                    reader = command.ExecuteReader();

                    array = ReadNext(mutexID);

                    while (array.Length != 0)
                    {
                        list.Add(array[0]);
                        array = ReadNext(mutexID);
                    }

                    Close_reader();
                }
                catch (Exception ex)
                {
                    reader = null;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
                }
            }

            Signal(mutexID);
            return list;
        }
        public static void SendCommand_ReadAuditTrail(DateTime dtBefore, DateTime dtAfter, string[] eventTypes =  null, string orderBy = null, bool isOrderAsc = true)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("SendCommand_ReadAuditTrail " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadAuditTrail - Connection à la base de données échouée");
            }
            else
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
                if (!isCommandOk)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Création de la commande incorrecte");
                }
                else
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
            }

            Signal(mutexID);
        }
        public static void SendCommand_ReadAlarms(int firstId = -1, int lastId = -1, bool readAlert = false)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("SendCommand_ReadAlarms " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadAlarms - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                bool isCommandOk = firstId == -1 || firstId < lastId;
                string whereId = firstId == -1 ? "" : "id >= @0 AND id <= @1 AND ";
                string eventType = readAlert ? "(event_type = 'Alarme' OR event_type = 'Alerte')" : "event_type = 'Alarme'";

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT * FROM audit_trail WHERE " + whereId + eventType;
                //MessageBox.Show(command.CommandText);
                if (firstId != -1) SetCommand(command, new string[] { "firstId", "lastId" }, new string[] { firstId.ToString(), lastId.ToString() });

                MessageBox.Show(command.CommandText);

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

            Signal(mutexID);
            //dbReady = true;
        }
        public static int SendCommand_ReadPart(string tableName, int start, int end, string selectColumns = "*", bool isMutexReleased = true, int mutex = -1)
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

            logger.Debug("SendCommand_ReadPart " + tableName + " " + selectColumns + " " + start.ToString() + " " + end.ToString() + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadPart - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT " + selectColumns + " FROM " + tableName + " WHERE id > " + start.ToString() + " AND id < " + end.ToString();
                //MessageBox.Show(command.CommandText);

                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                {
                    reader = null;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_ReadPart - " + ex.Message);
                }
            }

            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static void SendCommand_GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("SendCommand_GetLastRecipes" + GetMutexIDs());

            // only prod pour la prod
            // only draft pour les tests de recette
            // only obsolete pour faire revivre une vieille recette
            // prod and draft pour modifier une recette

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_GetLastRecipes - Connection à la base de données échouée");
            }
            else
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

            logger.Debug("ReadNext" + GetMutexIDs());

            string[] array = new string[0];

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ReadNext - Connection à la base de données échouée");
            }
            else
            {
                if (!IsReaderNotAvailable())
                {
                    array = new string[reader.FieldCount];

                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(IsConnected().ToString() + " - " + ex.Message);
                        array = new string[0];
                    }
                }
                else
                {
                    array = new string[0];
                }
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

            logger.Debug("ReadNextBool " + GetMutexIDs());

            bool[] array = new bool[0];

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ReadNextBool - Connection à la base de données échouée");
            }
            else
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

            logger.Debug("GetOneRow" + GetMutexIDs());

            string[] array = new string[0];

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetOneRow - Connection à la base de données échouée");
            }
            else
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

            Signal(mutexID);
            //dbReady = true;
            return array;
        }
        public static int GetMax(string tableName, string column, string[] whereColumns = null, string[] whereValues = null, int mutex = -1)
        {
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

            logger.Debug("GetMax " + tableName + GetMutexIDs());

            int result = -1;

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GetMax - Connection à la base de données échouée");
            }
            else
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

            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static ReadOnlyCollection<DbColumn> GetColumnCollection()
        {
            //if (IsReaderNotAvailable()) return null;

            return reader.GetColumnSchema();
        }
        public static bool InsertRow(string tableName, string columnFields, string[] values, int mutex = -1)
        {
            int mutexID;
            bool result = false;

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

            logger.Debug("InsertRow " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - InsertRow - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                int valuesNumber = values.Count();
                string[] valueTags = new string[valuesNumber];
                string valueFields = "";

                if (columnFields.Split().Count() != valuesNumber)
                {
                    MessageBox.Show("SendCommand_insertRecord: C'est pas bien ce que tu fais là");
                }
                else
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
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - InsertRow - " + tableName + " - " + ex.Message + " - " + DateTime.Now.ToString());
                    }

                }
            }

            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static bool Update_Row(string tableName, string[] setColumns, string[] setValues, string id)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("Update_Row" + GetMutexIDs());

            bool result = false;

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Update_Row - Connection à la base de données échouée");
            }
            else
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

            logger.Debug("DeleteRow " + tableName + GetMutexIDs());

            bool result = false;

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - DeleteRow - Connection à la base de données échouée");
            }
            else
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

            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static bool DeleteRows(string tableName, DateTime lastRecordDate)
        {
            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("DeleteRows " + tableName + GetMutexIDs());

            bool result = false;

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - DeleteRow - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                string whereArg;
                MySqlCommand command;
                bool isCommandOk = true;

                command = connection.CreateCommand();
                command.CommandText = @"DELETE FROM " + tableName + " WHERE date_time < \"" + lastRecordDate.ToString("yyyy-MM-dd HH:mm:ss") + "\"";

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

            logger.Debug("CreateTempTable " + fields + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SendCommand_readAllRecipe - Connection à la base de données échouée");
            }
            else
            {
                Close_reader();

                // On supprimer la table temp si jamais elle existe
                MySqlCommand command = connection.CreateCommand();

                try
                {
                    command.CommandText = @"DROP TABLE IF EXISTS temp";
                    reader = command.ExecuteReader();
                    Close_reader();
                }
                catch (Exception ex)
                {
                    logger.Error(command.CommandText + " " + ex.Message);
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
                    logger.Error(command.CommandText + " " + ex.Message);
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - CreateTempTable: " + ex.Message);
                }

                Close_reader();
            }

            Signal(mutexID);
            //dbReady = true;
        }
        public static void SelectFromTemp(string select)
        {

            //wait();
            int mutexID = GetNextMutex();
            mutexIDs.Add(mutexID);

            while (mutexIDs[0] != mutexID) Task.Delay(25);

            logger.Debug("SelectFromTemp " + select + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error("Connection à la base de données échouée");
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - SelectFromTemp - Connection à la base de données échouée");
            }
            else
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

            Signal(mutexID);
            //dbReady = true;
        }

        // Méthode outils
        private static string GetArg(string[] columns, string[] values, string separator, string prefix = "")
        {
            string arg;

            if (columns != null && values != null && columns.Count() == values.Count())
            {
                //arg = prefix + columns[0] + "=@" + columns[0];
                arg = prefix + columns[0] + "=@0";

                for (int i = 1; i < columns.Count(); i++)
                {
                    //arg += separator + columns[i] + "=@" + columns[i];
                    arg += separator + columns[i] + "=@" + i.ToString();
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
                    command.Parameters.AddWithValue("@" + i.ToString(), values[i]);
                }
                return true;
            }

            return false;
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
                mutexIDs.RemoveAt(0);
                logger.Debug("Signal " + GetMutexIDs());
            }
            else
            {
                logger.Error("Signal - On a un problème chef " + GetMutexIDs());
            }
        }
        private static string GetMutexIDs()
        {
            string text = " ";
            for (int i = 0; i < mutexIDs.Count; i++)
            {
                text = text + mutexIDs[i].ToString() + "_";
            }

            return text.TrimEnd('_');
        }
        private static void WriteInTextFile2(string title)
        {
            string text = title + ": ";
            for (int j = 0; j < mutexIDs.Count; j++)
            {
                text = text + mutexIDs[j].ToString() + " ";
            }

            Console.WriteLine(DateTime.Now.ToString() + " " + text);
            /*

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
        {

            Console.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss_ff") + " " + text);
            /*
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
                        byte[] author = new UTF8Encoding(true).GetBytes(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss_ff") + " " + text + "\n");
                        fs.Write(author, 0, author.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss_ff") + " - " + ex);
                }
            }//*/
        }
    }
}
