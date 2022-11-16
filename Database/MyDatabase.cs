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
using System.Threading;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Database.Properties;

namespace Database
{
    /*
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class AuditTrail_Columns
    {
        public Column id { get; set; }
        public Column dateTime { get; set; }
        public Column username { get; set; }
        public Column eventType { get; set; }
        public Column description { get; set; }
        public Column valueBefore { get; set; }
        public Column valueAfter { get; set; }
        public Column comment { get; set; }
    }
    public interface IColumn
    {
        string name { get; }
        string displayName { get; }
        string value { get; set; }
    }
    public class AuditTrail_InsertColumns
    {
        public string name;
        public Column[] columnNames;
        public string[] columnValues { get; set; }

        public AuditTrail_InsertColumns()
        {
            StringCollection colNames = Settings.Default.AuditTrail_ColIds;
            List<Column> colList = new List<Column>();

            columnValues = new string[colNames.Count];

            for (int i = 0; i < colNames.Count; i++) colList.Add(new Column(colNames[i]));
            columnNames = colList.ToArray();
        }
    }
//*/


    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ConnectionInfo
    {
        public string server { get; set; }
        public string userID { get; set; }
        public string password { get; set; }
        public string db { get; set; }

    }
    public interface IColumn
    {
        string name { get; }
        string displayName { get; }
        string value { get; set; }
    }

    public struct IniInfo
    {
        public string AlarmType_Alarm;
        public string AlarmType_Warning;
    }

    public static class MyDatabase
    {
        //private static readonly Configuration_old.Connection_Info MySettings = System.Configuration.ConfigurationManager.GetSection("Database/Connection_Info") as Configuration_old.Connection_Info;
        private static MySqlConnection connection;
        private static MySqlDataReader reader;
        public static List<int> AlarmListID = new List<int>();
        public static List<string> AlarmListDescription = new List<string>();
        public static List<string> AlarmListStatus = new List<string>();
        private readonly static List<int> mutexIDs = new List<int>();
        private static int lastMutexID = 0;
        private static bool StopScan = false;
        private static bool isConnecting = false;
        private static readonly System.Timers.Timer scanConnectTimer;
        public static IConfig config;
        private static ManualResetEvent signal = new ManualResetEvent(true);

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private static RecipeInfo recipeInfo = new RecipeInfo();

        //private static bool isInitialized = false;

        // Future interface
        private static IniInfo info;

        public enum RecipeStatus
        {
            PROD,
            DRAFT,
            OBSOLETE,
            PRODnDRAFT,
            None
        }
        static MyDatabase()
        {
            // Initialisation des timers
            scanConnectTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.scanConnectTimer_Interval,
                AutoReset = false
            };
            scanConnectTimer.Elapsed += ScanConnectTimer_OnTimedEvent;

            Connect();
        }
        public static void Initialize(IniInfo info_arg)
        {
            // From AlarmManagement
            if (info.AlarmType_Alarm == null && info_arg.AlarmType_Alarm != null) info.AlarmType_Alarm = info_arg.AlarmType_Alarm;
            if (info.AlarmType_Warning == null && info_arg.AlarmType_Warning != null) info.AlarmType_Warning = info_arg.AlarmType_Warning;
            // From Recipe
/*            if (info.recipe_tableName == null && info_arg.recipe_tableName != null) info.recipe_tableName = info_arg.recipe_tableName;
            if (info.recipe_colNameId == null && info_arg.recipe_colNameId != null) info.recipe_colNameId = info_arg.recipe_colNameId;
            if (info.recipe_colNameName == null && info_arg.recipe_colNameName != null) info.recipe_colNameName = info_arg.recipe_colNameName;
            if (info.recipe_colNameVersion == null && info_arg.recipe_colNameVersion != null) info.recipe_colNameVersion = info_arg.recipe_colNameVersion;
            if (info.recipe_colNameStatus == null && info_arg.recipe_colNameStatus != null) info.recipe_colNameStatus = info_arg.recipe_colNameStatus;
            if (info.recipe_statusDraft == null && info_arg.recipe_statusDraft != null) info.recipe_statusDraft = info_arg.recipe_statusDraft;
            if (info.recipe_statusProd == null && info_arg.recipe_statusProd != null) info.recipe_statusProd = info_arg.recipe_statusProd;
            if (info.recipe_statusObsol == null && info_arg.recipe_statusObsol != null) info.recipe_statusObsol = info_arg.recipe_statusObsol;
            */
            logger.Trace(info.AlarmType_Alarm);
            logger.Trace(info.AlarmType_Warning);
            /*
            logger.Trace(info.recipe_tableName);
            logger.Trace(info.recipe_colNameId);
            logger.Trace(info.recipe_colNameName);
            logger.Trace(info.recipe_colNameVersion);
            logger.Trace(info.recipe_colNameStatus);
            logger.Trace(info.recipe_statusDraft);
            logger.Trace(info.recipe_statusProd);
            logger.Trace(info.recipe_statusObsol);
            */
            //isInitialized = true;
        }
        private static void ScanConnectTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //logger.Debug("ScanConnect");
            if (!IsConnected())
            {
                Connect();
                logger.Info(Settings.Default.Info01 + IsConnected().ToString());
            }
            scanConnectTimer.Enabled = true;
        }
        public static async void ConnectAsync()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = Settings.Default.ConnectionInfo.server,
                UserID = Settings.Default.ConnectionInfo.userID,
                Password = Settings.Default.ConnectionInfo.password,
                Database = Settings.Default.ConnectionInfo.db,
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
        public static int Wait(int mutex = -1)
        {
            int mutexID;

            if (mutex == -1)
            {
                mutexID = GetNextMutex();
                mutexIDs.Add(mutexID);
                while (mutexIDs[0] != mutexID) signal.WaitOne();
                signal.Reset();
            }
            else
            {
                mutexID = mutex;
            }

            logger.Debug("Wait " + GetMutexIDs());

            return mutexID;
        }
        public static int Connect(bool isMutexReleased = true)
        {
            int mutexID = Wait();

            if (IsConnected())
            {
                logger.Debug("No Connect " + isMutexReleased.ToString() + GetMutexIDs());
            }
            else
            {
                logger.Debug("Connect " + isMutexReleased.ToString() + GetMutexIDs());
                isConnecting = true;

                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
                {
                    Server = Settings.Default.ConnectionInfo.server,
                    UserID = Settings.Default.ConnectionInfo.userID,
                    Password = Settings.Default.ConnectionInfo.password,
                    Database = Settings.Default.ConnectionInfo.db,
                    AllowZeroDateTime = true,
                    Pooling = true,
                    MinimumPoolSize = 2
                };



                connection = new MySqlConnection(builder.ConnectionString);

                try { if(!StopScan) connection.Open(); }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }

                if(!StopScan) scanConnectTimer.Start();

                isConnecting = false;
            }

            if (isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public async static void Disconnect(int mutex = -1)
        {
            int mutexID = Wait(mutex);

            if (mutexIDs.Count != 1)
            {
                logger.Debug("No Disconnect" + GetMutexIDs());
            }
            else
            {
                logger.Debug("Disconnect " + GetMutexIDs());

                scanConnectTimer.Stop();
                StopScan = true;

                while (isConnecting)
                {
                    logger.Debug("Disconnect on going");
                    await Task.Delay(100);
                }
                StopScan = false;

                try { connection.Close(); }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }

            Signal(mutexID);
        }
        public static bool IsConnected()
        {
            if (connection == null) return false;

            return connection.State == System.Data.ConnectionState.Open;
        }
        public static int SendCommand_Read(string tableName, string selectColumns = "*", string[] whereColumns = null, string[] whereValues = null, string orderBy = null, bool isOrderAsc = true, string groupBy = null, bool isMutexReleased = true, int mutex = -1)
        {
            int mutexID = Wait(mutex);

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
            
            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

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
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }

        End:
            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static int SendCommand_Read(string tableName, string selectColumns = "*", List<Column> whereColumns = null, bool isMutexReleased = true, int mutex = -1)
        {
            //int mutexID = Wait(mutex);
            int mutexID = -1;

            logger.Debug("SendCommand_Read " + GetMutexIDs());
            
            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            string whereArg = GetArg(whereColumns, " AND ", " WHERE ");

            mutexID = SendCommand(@"SELECT " + selectColumns + " FROM " + tableName + whereArg, whereColumns, isMutexReleased, mutex);


        End:
            //if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static int SendCommand(string commandText, List<Column> columns = null, bool isMutexReleased = true, int mutex = -1)
        {
            int mutexID = Wait(mutex);

            logger.Debug("SendCommand " + commandText + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = commandText;

            bool isCommandOk = true;
            if (columns != null)
            {
                isCommandOk = SetCommand(command, columns);
            }

            if (!isCommandOk)
            {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }

        End:
            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static List<string> GetTablesName()
        {
            int mutexID = Wait();

            MySqlDataReader reader;
            List<string> list = new List<string>();
            string[] array;

            logger.Debug("SendCommand_GetTablesName " + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = \"" + Settings.Default.ConnectionInfo.db + "\" AND table_type = \"BASE TABLE\";";

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
                MyDatabase.reader = null;
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
            }
        End:
            Signal(mutexID);
            return list;
        }
        public static int SendCommand_ReadAuditTrail(DateTime dtBefore, DateTime dtAfter, string[] eventTypes =  null, string orderBy = null, bool isOrderAsc = true, bool isMutexReleased = true)
        {
            int mutexID = Wait();

            logger.Debug("SendCommand_ReadAuditTrail " + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            string dtBefore_v = "dtBefore";
            string dtAfter_v = "dtAfter";
            string whereDateTime = auditTrailInfo.columns[auditTrailInfo.dateTime].id + " >= @" + dtBefore_v + " AND " + auditTrailInfo.columns[auditTrailInfo.dateTime].id + " <= @" + dtAfter_v;
            string eventType = " AND (";
            string orderArg = "";
            bool isCommandOk = true;

            if (eventTypes != null && eventTypes.Length != 0)
            {
                for (int i = 0; i < eventTypes.Length - 1; i++)
                {
                    eventType += auditTrailInfo.columns[auditTrailInfo.eventType].id + " = @" + eventTypes[i] + " OR ";
                }
                eventType += auditTrailInfo.columns[auditTrailInfo.eventType].id + " = @" + eventTypes[eventTypes.Length - 1] + ")";
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
            command.CommandText = @"SELECT * FROM " + auditTrailInfo.name + " WHERE " + whereDateTime + eventType + orderArg;
            command.Parameters.AddWithValue("@" + dtBefore_v, dtBefore.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@" + dtAfter_v, dtAfter.ToString("yyyy-MM-dd HH:mm:ss"));

            //logger.Trace(command.CommandText + " " + dtBefore.ToString("yyyy-MM-dd HH:mm:ss") + " " + dtAfter.ToString("yyyy-MM-dd HH:mm:ss"));

            if (eventTypes != null)
            {
                for (int i = 0; i < eventTypes.Length; i++)
                {
                    command.Parameters.AddWithValue("@" + eventTypes[i], eventTypes[i]);
                }
            }

            if (!isCommandOk) {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            if (isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static void SendCommand_ReadAlarms(int firstId = -1, int lastId = -1, bool readAlert = false)
        {
            int mutexID = Wait();

            logger.Debug("SendCommand_ReadAlarms " + GetMutexIDs());

            if (info.AlarmType_Alarm == null || info.AlarmType_Warning == null) {
                logger.Error(Settings.Default.Error12);
                MessageBox.Show(Settings.Default.Error12);
                goto End;
            }

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }
            
            Close_reader();

            bool isCommandOk = firstId == -1 || firstId < lastId;
            string whereId = firstId == -1 ? "" : 
                auditTrailInfo.columns[auditTrailInfo.id].id + " >= @0 AND " + 
                auditTrailInfo.columns[auditTrailInfo.id].id + " <= @1 AND ";
            string eventType = readAlert ? 
                "(" + auditTrailInfo.columns[auditTrailInfo.eventType].id + " = '" + info.AlarmType_Alarm + "' OR " + 
                auditTrailInfo.columns[auditTrailInfo.eventType].id + " = '" + info.AlarmType_Warning + "')" 
                : auditTrailInfo.columns[auditTrailInfo.eventType].id + " = '" + info.AlarmType_Alarm + "'";

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT * FROM " + auditTrailInfo.name + " WHERE " + whereId + eventType;
            //SetCommand va peut-être devoir être modifiée (columns ne sert vraiment à rien)
            if (firstId != -1) SetCommand(command, new string[] { "firstId", "lastId" }, new string[] { firstId.ToString(), lastId.ToString() });

            if (!isCommandOk) {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            Signal(mutexID);
        }
        public static int SendCommand_ReadPart(string tableName, int start, int end, string selectColumns = "*", bool isMutexReleased = true, int mutex = -1)
        {

            //
            // Repenser complètement ces méthodes
            //

            int mutexID = Wait(mutex);

            logger.Debug("SendCommand_ReadPart " + tableName + " " + selectColumns + " " + start.ToString() + " " + end.ToString() + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT " + selectColumns + " FROM " + tableName + " WHERE id > " + start.ToString() + " AND id < " + end.ToString();

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            if (mutex == -1 && isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static void SendCommand_GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            // only prod pour la prod
            // only draft pour les tests de recette
            // only obsolete pour faire revivre une vieille recette
            // prod and draft pour modifier une recette

            int mutexID = Wait();

            logger.Debug("SendCommand_GetLastRecipes" + GetMutexIDs());
            /*
            if (info.recipe_tableName == null ||
                info.recipe_colNameId == null ||
                info.recipe_colNameName == null ||
                info.recipe_colNameVersion == null ||
                info.recipe_colNameStatus == null ||
                info.recipe_statusDraft == null ||
                info.recipe_statusProd == null ||
                info.recipe_statusObsol == null)
            {
                logger.Error(Settings.Default.Error12);
                MessageBox.Show(Settings.Default.Error12);
                goto End;
            }*/

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            string statusFilter =
                status == RecipeStatus.DRAFT ? recipeInfo.columns[recipeInfo.status].id + " = " + GetRecipeStatus(RecipeStatus.DRAFT) :
                status == RecipeStatus.OBSOLETE ? recipeInfo.columns[recipeInfo.status].id + " = " + GetRecipeStatus(RecipeStatus.OBSOLETE) :
                status == RecipeStatus.PROD ? recipeInfo.columns[recipeInfo.status].id + " = " + GetRecipeStatus(RecipeStatus.PROD) :
                status == RecipeStatus.PRODnDRAFT ? "(" + recipeInfo.columns[recipeInfo.status].id + " = " + GetRecipeStatus(RecipeStatus.PROD) + " OR " + 
                recipeInfo.columns[recipeInfo.status].id + " = " + GetRecipeStatus(RecipeStatus.DRAFT) + ")" : "";

            if (statusFilter == "") {
                logger.Error(Settings.Default.Error03);
                MessageBox.Show(Settings.Default.Error03);
                goto End;
            }

            MySqlCommand command = new MySqlCommand("SELECT " + 
                recipeInfo.columns[recipeInfo.recipeName].id + ", " + 
                recipeInfo.columns[recipeInfo.id].id + 
                " FROM " + recipeInfo.name + 
                " WHERE ((" + recipeInfo.columns[recipeInfo.recipeName].id + ", " + 
                recipeInfo.columns[recipeInfo.version].id + ") IN " +
                "(SELECT " + recipeInfo.columns[recipeInfo.recipeName].id + 
                ", MAX(" + recipeInfo.columns[recipeInfo.version].id + ") " +
                "FROM " + recipeInfo.name + 
                " GROUP BY " + recipeInfo.columns[recipeInfo.recipeName].id + ")) AND " + 
                statusFilter + " ORDER BY " + recipeInfo.columns[recipeInfo.recipeName].id + ";", connection);

            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            Signal(mutexID);
        }
        public static string[] ReadNext(int mutex = -1)
        {
            int mutexID = Wait(mutex);

            logger.Debug("ReadNext" + GetMutexIDs());

            string[] array = new string[0];

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            if (IsReaderNotAvailable()) {
                logger.Error(Settings.Default.Error04);
                MessageBox.Show(Settings.Default.Error04);
                goto End;
            }

            try
            {
                if (reader.Read())
                {
                    array = new string[reader.FieldCount];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        array[i] = reader[i].ToString();
                        //logger.Trace(i.ToString() + ": " + reader[i].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            //logger.Trace(array.Length.ToString());
            return array;
        }
        public static ITableInfo ReadNext(ITableInfo tableInfo,int mutex = -1)
        {
            int mutexID = Wait(mutex);

            logger.Debug("ReadNext ITableInfo" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                return null;
            }

            if (IsReaderNotAvailable())
            {
                logger.Error(Settings.Default.Error04);
                MessageBox.Show(Settings.Default.Error04);
                return null;
            }

            try
            {
                if (reader.Read() && tableInfo.columns.Count == reader.FieldCount)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        tableInfo.columns[i].value = reader[i].ToString();
                        logger.Trace(tableInfo.columns[i].id + ": " + tableInfo.columns[i].value);
                    }
                }
                else
                {
                    if (tableInfo.columns.Count != reader.FieldCount)
                    {
                        logger.Error(Settings.Default.Error14);
                        MessageBox.Show(Settings.Default.Error14);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }

            if (mutex == -1) Signal(mutexID);
            return tableInfo;
        }
        public static bool[] ReadNextBool()
        {
            int mutexID = Wait();
            bool[] array = new bool[0];

            logger.Debug("ReadNextBool " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            if (IsReaderNotAvailable()) {
                logger.Error(Settings.Default.Error04);
                MessageBox.Show(Settings.Default.Error04);
                goto End;
            }

            array = new bool[reader.FieldCount - 2];

            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount - 2; i++)
                {
                    array[i] = reader.GetBoolean(i + 2);
                }
            }
        End:
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
            int mutexID = Wait();
            string[] array = new string[0];

            logger.Debug("GetOneRow" + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            SendCommand_Read(tableName, selectColumns, whereColumns, whereValues, mutex: mutexID);

            array = ReadNext(mutexID);

            if (ReadNext(mutexID).Count() != 0) {
                array = new string[0];
                logger.Error(Settings.Default.Error05);
                MessageBox.Show(Settings.Default.Error05);
            }
            Close_reader();
        End:
            Signal(mutexID);
            return array;
        }
        public static ITableInfo GetOneRow(ITableInfo tableInfo, string id)
        {
            int mutexID = Wait();
            //string[] array = new string[0];

            logger.Debug("GetOneRow" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                //return null;
                tableInfo = null;
                goto End;
            }

            tableInfo.columns[tableInfo.id].value = id;
            SendCommand_Read(tableInfo.name,whereColumns: tableInfo.columns, isMutexReleased: false, mutex: mutexID);
            tableInfo = ReadNext(tableInfo, mutexID);

            if (ReadNext(tableInfo, mutexID) != null)
            {
                logger.Error(Settings.Default.Error15);
                MessageBox.Show(Settings.Default.Error15);
                //return null;
                tableInfo = null;
                goto End;
            }
        End:
            //Close_reader();
            Signal(mutexID);
            return tableInfo;
        }
        public static int GetMax(string tableName, string column, string[] whereColumns = null, string[] whereValues = null, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            int result = -1;

            logger.Debug("GetMax " + tableName + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

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
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static ReadOnlyCollection<DbColumn> GetColumnCollection()
        {
            return reader.GetColumnSchema();
        }
        public static bool InsertRow(string tableName, string columnFields, string[] values, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            bool result = false;

            logger.Debug("InsertRow Standard " + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            int valuesNumber = values.Count();
            string[] valueTags = new string[valuesNumber];
            string valueFields = "";

            if (columnFields.Split().Count() != valuesNumber) {
                logger.Error(Settings.Default.Error06);
                MessageBox.Show(Settings.Default.Error06);
                goto End;
            }

            for (int i = 0; i < valuesNumber - 1; i++)
            {
                valueTags[i] = "@" + i.ToString();
                valueFields = valueFields + "@" + i.ToString() + ", ";
            }

            valueTags[valuesNumber - 1] = "@" + (valuesNumber - 1).ToString();
            valueFields = valueFields + "@" + (valuesNumber - 1).ToString();

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
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static bool InsertRow(ITableInfo tableInfo, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            bool result = false;

            logger.Debug("InsertRow ITableInfo" + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            if (tableInfo.columns == null || tableInfo.columns.Count() == 0) {
                logger.Error(Settings.Default.Error08);
                MessageBox.Show(Settings.Default.Error08);
                goto End;
            }

            //MySqlDataReader reader;
            string valueFields = "";
            string columnFields = "";

            //
            // Get arg, c'est nul !!! GET ARG je te dis
            //
            for (int i = 0; i < tableInfo.columns.Count(); i++)
            {
                if (tableInfo.columns[i].value != "" && tableInfo.columns[i].value != null)
                {
                    columnFields = columnFields + tableInfo.columns[i].id + ", ";
                    valueFields = valueFields + "@" + i.ToString() + ", ";
                    logger.Trace(i.ToString() + ": " + columnFields + " - " + valueFields);
                }
            }

            if (columnFields.Length > 0)
            {
                columnFields = columnFields.Remove(columnFields.Length - 2);
                valueFields = valueFields.Remove(valueFields.Length - 2);
            }

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO " + tableInfo.name + " (" + columnFields + ") VALUES (" + valueFields + ");";
            logger.Trace("Insert command: " + command.CommandText);
            SetCommand(command, tableInfo.columns);

            try
            {
                Close_reader(); // retirer l'un des 2 Close_reader()
                reader = command.ExecuteReader();
                Close_reader();
                result = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }

            End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static bool Update_Row_old(string tableName, string[] setColumns, string[] setValues, string id)
        {
            int mutexID = Wait();
            bool result = false;

            logger.Debug("Update_Row Standard " + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

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

            if (!isCommandOk) {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
                Close_reader();
                result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        End:
            Signal(mutexID);
            return result;
        }
        public static bool Update_Row(ITableInfo tableInfo, string id, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            bool result = false;

            logger.Debug("Update_Row ITableInfo " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            if (tableInfo.columns == null || tableInfo.columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                MessageBox.Show(Settings.Default.Error08);
                goto End;
            }

            Close_reader();

            string whereArg = " WHERE " + tableInfo.columns[tableInfo.id].id + " = @id";
            string setArg = " SET " + GetArg(tableInfo.columns, ", ");
            bool isCommandOk = true;

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"UPDATE " + tableInfo.name + setArg + whereArg;
            logger.Trace(command.CommandText);

            isCommandOk = SetCommand(command, tableInfo.columns);

            command.Parameters.AddWithValue("@id", id);

            if (!isCommandOk)
            {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
                Close_reader();
                result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static bool DeleteRow(string tableName, string[] whereColumns, string[] whereValues)
        {
            int mutexID = Wait();
            bool result = false;

            logger.Debug("DeleteRow " + tableName + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            string whereArg;
            MySqlCommand command;
            bool isCommandOk = true;

            whereArg = " WHERE " + GetArg(whereColumns, whereValues, " AND ");

            command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM " + tableName + whereArg;

            logger.Trace(command.CommandText);

            if (whereColumns != null && whereValues != null)
            {
                isCommandOk = SetCommand(command, whereColumns, whereValues);
            }

            if (!isCommandOk || whereArg == "")
            {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                goto End;
            }

            try
            {
                reader = command.ExecuteReader();
                Close_reader();
                result = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
                Close_reader();
            }
        End:
            Signal(mutexID);
            return result;
        }
        public static bool DeleteRows(string tableName, DateTime lastRecordDate)
        {
            int mutexID = Wait();
            bool result = false;

            logger.Debug("DeleteRows " + tableName + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            string whereArg;
            MySqlCommand command;
            bool isCommandOk = true;

            command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM " + tableName + " WHERE date_time < \"" + lastRecordDate.ToString("yyyy-MM-dd HH:mm:ss") + "\"";

            try
            {
                reader = command.ExecuteReader();
                result = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            Close_reader();
            Signal(mutexID);
            return result;
        }
        public static void CreateTempTable(string fields)
        {
            int mutexID = Wait();

            logger.Debug("CreateTempTable " + fields + GetMutexIDs());

            if (!IsConnected()) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            // On supprimer la table temp si jamais elle existe
            MySqlCommand command = connection.CreateCommand();

            try
            {
                command.CommandText = @"DROP TABLE IF EXISTS " + Settings.Default.TempTableName;
                reader = command.ExecuteReader();
                Close_reader();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }

            try
            {
                command.CommandText = @"CREATE TABLE " + Settings.Default.TempTableName + " (" +
                    "id  INT NOT NULL auto_increment PRIMARY KEY," +
                    fields + ")";
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            Close_reader();
            Signal(mutexID);
            //dbReady = true;
        }
        public static void SelectFromTemp(string select)
        {
            int mutexID = Wait();

            logger.Debug("SelectFromTemp " + select + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
                goto End;
            }

            Close_reader();

            try
            {
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT " + select + " FROM " + Settings.Default.TempTableName + ";";
                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
            }
        End:
            Signal(mutexID);
        }

        // Méthode outils
        private static string GetArg(string[] columns, string[] values, string separator, string prefix = "")
        {
            string arg;

            if (columns != null && values != null && columns.Count() == values.Count())
            {
                arg = prefix + columns[0] + "=@0";

                for (int i = 1; i < columns.Count(); i++)
                {
                    arg += separator + columns[i] + "=@" + i.ToString();
                }
            }
            else
            {
                arg = "";
            }

            return arg;
        }
        private static string GetArg(List<Column> columns, string separator, string prefix = "")
        {
            string arg = "";

            if (columns.Count() == 0) {
                logger.Error(Settings.Default.Error08);
                MessageBox.Show(Settings.Default.Error08);
                return arg;
            }

            arg = prefix;

            for (int i = 0; i < columns.Count(); i++)
            {
                if (columns[i].value != null && columns[i].value != "") arg += (arg == prefix ? "" : separator) + columns[i].id + "=@" + i.ToString();
            }

            return arg;
        }
        private static bool SetCommand(MySqlCommand command, List<Column> columns)
        {
            bool result = false;

            if (columns.Count() == 0) {
                logger.Error(Settings.Default.Error08);
                MessageBox.Show(Settings.Default.Error08);
                return result;
            }

            for (int i = 0; i < columns.Count(); i++)
            {

                if (columns[i].value != "" && columns[i].value != null)
                {
                    logger.Trace("Value " + i.ToString() + ": " + columns[i].value);
                    command.Parameters.AddWithValue("@" + i.ToString(), columns[i].value);
                    result = true;
                }
            }

            /*
            if (columns.Count() < indexes.Count()) {
                logger.Error(Settings.Default.Error10);
                MessageBox.Show(Settings.Default.Error10);
                return;
            }

            for (int i = 0; i < indexes.Count(); i++)
            {
                logger.Trace("Value " + i.ToString() + ": " + columns[indexes[i]].value);
                command.Parameters.AddWithValue("@" + i.ToString(), columns[indexes[i]].value);
            }             
             */
            return result;
        }
        private static bool SetCommand(MySqlCommand command, string[] columns, string[] values)
        {
            if (columns.Count() != values.Count())
            {
                logger.Error(Settings.Default.Error06);
                MessageBox.Show(Settings.Default.Error06);
                return false;
            }

            for (int i = 0; i < columns.Count(); i++)
            {
                logger.Trace("Value " + i.ToString() + ": " + values[i].ToString());
                command.Parameters.AddWithValue("@" + i.ToString(), values[i]);
            }
            return true;
        }
        private static int GetNextMutex()
        { // Assure toi que cette fonction ne peut être appelé plusieurs fois en même temps
            lastMutexID = (lastMutexID + 1) % 200;
            return lastMutexID;
        }
        public static void Signal(int mutex)
        {
            if (mutex != mutexIDs[0]) {
                logger.Error(Settings.Default.Error11);
                MessageBox.Show(Settings.Default.Error11);
            }

            mutexIDs.RemoveAt(0);
            signal.Set();
            logger.Debug("Signal " + GetMutexIDs());
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
        public static int GetMutexIDsCount()
        {
            return mutexIDs.Count;
        }

        // Interface à implémenter
        public static string GetAuditTrail_TableName()
        {
            return Settings.Default.AuditTrail_TableName;
        }
        public static string GetRecipeStatus(RecipeStatus status)
        {
            switch (status)
            {
                case RecipeStatus.DRAFT:
                    return Settings.Default.Recipe_Status_DRAFT;
                case RecipeStatus.PROD:
                    return Settings.Default.Recipe_Status_PROD;
                case RecipeStatus.OBSOLETE:
                    return Settings.Default.Recipe_Status_OBSOL;
                default:
                    return Settings.Default.Recipe_Status_None;
            }
        }
    }
}
