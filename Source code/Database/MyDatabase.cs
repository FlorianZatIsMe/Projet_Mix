using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
using Database.Properties;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Database
{
    /// <summary>
    /// Class to store information of connection to the database in the settings
    /// </summary>
    /// <para>Creation revision: 001</para>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ConnectionInfo
    {
        ///<value>Database server name</value>
        public string Server { get; set; }

        ///<value>User ID to log on the database</value>
        public string UserID { get; set; }

        ///<value>Password of the user ID</value>
        public string Password { get; set; }

        ///<value>Database to use</value>
        public string Db { get; set; }
    }

    /// <summary>
    /// Structure of the information from other projects required to initialize the main class
    /// </summary>
    /// <para>Creation revision: 001</para>
    public struct IniInfo
    {
        ///<value>Event type to put in the audit trail in case of alarm (see class AuditTrailInfo)</value>
        public string AlarmType_Alarm;
        ///<value>Event type to put in the audit trail in case of warning (see class AuditTrailInfo)</value>
        public string AlarmType_Warning;
        ///<value>Unit mg/g to put in the weight recipe database table (used by the class CycleWeightInfo)</value>
        public string RecipeWeight_mgG_Unit;
        ///<value>Conversion value of the unit mg/g. Used to calculate the conversion ratio in the class CycleWeightInfo</value>
        public decimal? RecipeWeight_mgG_Conversion;
        ///<value>Unit g/g to put in the weight recipe database table (used by the class CycleWeightInfo)</value>
        public string RecipeWeight_gG_Unit;
        ///<value>Conversion value of the unit g/g. Used to calculate the conversion ratio in the class CycleWeightInfo</value>
        //public decimal? RecipeWeight_gG_Conversion;
        ///<value>Unit g to put in the cycle database table (used by the class CycleWeightInfo)</value>
        public string CycleFinalWeight_g_Unit;
        ///<value>Conversion value of the unit g. Used to calculate the conversion ratio in the class CycleWeightInfo</value>
        //public decimal? CycleFinalWeight_g_Conversion;
        ///<value>Main window of the application. Is used to attach the MessageBoxes to it</value>
        public Window Window;
    }

    /// <summary>
    /// Enumeration of the status of the recipes. Can be used to filter recipes by one of those status 
    /// </summary>
    /// <remarks>See method GetRecipeStatus to translate the status to put in the applicable database table</remarks>
    public enum RecipeStatus
    {
        ///<value>Production status (recipes in production can be used for production)</value>
        PROD,
        ///<value>Draft status (recipes in draft can't be used in production but can be tested by allowed user)</value>
        DRAFT,
        ///<value>Obsolete status (obsolete recipes can't be used at all but they can be brought back by allowed user)</value>
        OBSOLETE,
        ///<value>Production or draft recipes (value used to filter recipes in production or in draft)</value>
        PRODnDRAFT,
        ///<value>No status. Recipes should have this status</value>
        None
    }

    /// <summary>
    /// Static class used to allow other projects to access some values from the settings
    /// </summary>
    public static class DatabaseSettings
    {
        ///<value>Message displayed if the connection to the database fails</value>
        public static string Error_connectToDbFailed { get; }
        ///<value>Value "true" when read from a database table</value>
        public static string General_TrueValue_Read { get; }
        ///<value>Value "false" when read from a database table</value>
        public static string General_FalseValue_Read { get; }
        ///<value>Value "true" to write to a database table</value>
        public static string General_TrueValue_Write { get; }
        ///<value>Value "false" to write to a database table</value>
        public static string General_FalseValue_Write { get; }
        ///<value>Information of connection to the database</value>
        public static ConnectionInfo ConnectionInfo { get; }
        ///<value>Folder of the application of the database</value>
        public static string DBAppFolder { get; }
        ///<value>Format of timestamps</value>
        public static string Timestamp_Format { get; }

        // Constructor: initialize each variable of the class
        static DatabaseSettings()
        {
            // Initialization of each variable of the class based on settings
            Error_connectToDbFailed = Settings.Default.Error_connectToDbFailed;
            General_TrueValue_Read = Settings.Default.General_TrueValue_Read;
            General_FalseValue_Read = Settings.Default.General_FalseValue_Read;
            General_TrueValue_Write = Settings.Default.General_TrueValue_Write;
            General_FalseValue_Write = Settings.Default.General_FalseValue_Write;
            ConnectionInfo = Settings.Default.ConnectionInfo;
            DBAppFolder = Settings.Default.DBAppFolder;
            Timestamp_Format = Settings.Default.Timestamp_Format;
        }
    }

    /// <summary>
    /// Class containing the parameters required by other method (usually to select one or several row in a database table)
    /// </summary>
    public class ReadInfo
    {
        ///<value>Object of database table. The values of this object can be used to select a set of rows</value>
        public IComTabInfo TableInfo { get; }

        ///<value>Must contains the id of the column to use to sort the rows</value>
        public string OrderBy { get; }

        ///<value>If value value = true then the order of the sort is ascending. Descending if false</value>
        public bool IsOrderAsc { get; }

        ///<value>Used for tables containing a date and time column: date and time of the first row to filter</value>
        public DateTime? DtBefore { get; }

        ///<value>Used for tables containing a date and time column: date and time of the last row to filter</value>
        public DateTime? DtAfter { get; }

        ///<value>Used for tables containing an event type column. List of event types to filter</value>
        public string[] EventTypes { get; }

        ///<value>Parameter containing a custom SQL command to put after the WHERE statetement</value>
        public string CustomWhere { get; }

        /// <summary>
        /// Initialize all variables of the class from parameters
        /// </summary>
        /// <param name="_tableInfo"><see cref="TableInfo"/></param>
        /// <param name="_orderBy"><see cref="OrderBy"/></param>
        /// <param name="_isOrderAsc"><see cref="IsOrderAsc"/></param>
        /// <param name="_dtBefore"><see cref="DtBefore"/></param>
        /// <param name="_dtAfter"><see cref="DtAfter"/></param>
        /// <param name="_eventTypes"><see cref="EventTypes"/></param>
        /// <param name="_customWhere"><see cref="CustomWhere"/></param>
        public ReadInfo(IComTabInfo _tableInfo = null,
            string _orderBy = null,
            bool _isOrderAsc = true,
            DateTime? _dtBefore = null,
            DateTime? _dtAfter = null,
            string[] _eventTypes = null,
            string _customWhere = "")
        {
            // Initialization of each variable of the class from parameters
            TableInfo = _tableInfo;
            OrderBy = _orderBy;
            IsOrderAsc = _isOrderAsc;
            DtBefore = _dtBefore;
            DtAfter = _dtAfter;
            EventTypes = _eventTypes;
            CustomWhere = _customWhere;
        }

        /// <summary>
        /// Initialize all variables of the class from another object of the same class (the database table is replaced though)
        /// </summary>
        /// <param name="_readInfo">Object of the same class</param>
        /// <param name="_tableInfo"><see cref="TableInfo"/></param>
        public ReadInfo(ReadInfo _readInfo, IComTabInfo _tableInfo = null)
        {
            // Initialization of the database table from parameter
            TableInfo = _tableInfo;

            // Initialization of each other variable of the class from parameter
            OrderBy = _readInfo.OrderBy;
            IsOrderAsc = _readInfo.IsOrderAsc;
            DtBefore = _readInfo.DtBefore;
            DtAfter = _readInfo.DtAfter;
            EventTypes = _readInfo.EventTypes;
            CustomWhere = _readInfo.CustomWhere;
        }
    }

    /// <summary>
    /// Main class of the namespace. This class allow the interface with a database (connect, disconnect, insert rows, update rows, delete rows, read rows)
    /// </summary>
    public static class MyDatabase
    {
        /// <value>
        /// Variable initialized by other projects
        /// </value>
        public static IniInfo info;

        //
        // PRIVATE VARIABLES
        //
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();   // Variable allowing to logger events (debug, errors...)
        private static MySqlConnection connection;  // Variable of the connection with the database
        private static MySqlDataReader reader;      // Reader: used to send command and get their result
        private static Task lastTask;

        // TIMER Check Connection
        private static readonly System.Timers.Timer scanConnectTimer;   // Timer used to periodically check the status of the connection to the database. The timer is stopped when a disconnection of the database is required
        private static bool StopScan = false;                           // Flag which is "true" when the connection is disconnecting
        private static bool isConnecting = false;                       // Flag which is "true" when the connection is connecting

        // TIMER Task Queue
        private static readonly Queue<Task<object>> taskQueue = new Queue<Task<object>>(); // Queue of tasks to be executed. Note: to execute a public method of this class, it must be added to the queue
        private static readonly System.Timers.Timer QueueEmptyTimer;        // Timer used to remove the connection to the database if the task queue is empty for too long
        private static readonly System.Timers.Timer IsQueueAvailableTimer;  // Timer used to execute the next task on the queue
        private static int QueueEmptyCount = 0;                             // Counter which increments when the task queue is empty

        private static readonly string key = "J'aime le chocolat";


        // Constructor of the class called automatically
        static MyDatabase()
        {
            logger.Debug("Start");  // Log of the start of the class

            // Initialization of the timer which will check the database connection
            scanConnectTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.scanConnectTimer_Interval,  // Setting of the time in ms for the timer to elapse
                AutoReset = false                                       // The timer isn't automatically reseted when it elapses, to reset the timer, it must be enabled or started
            };
            scanConnectTimer.Elapsed += ScanConnectTimer_OnTimedEvent;  // Reference of the method to be executed when the timer elapses

            // Initialization of the timer which remove the connection to the database if the task queue is empty for too long
            QueueEmptyTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.QueueEmptyTimer_Interval,   // Setting of the time in ms for the timer to elapse
                AutoReset = true                                        // The time is automatically reseted when it elapses
            };
            QueueEmptyTimer.Elapsed += QueueEmptyTimer_OnTimedEvent;    // Reference of the method to be executed when the timer elapses
            QueueEmptyTimer.Start();                                    // Start of the timer

            // Initialization of the timer which executes the task on the queue
            IsQueueAvailableTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.IsQueueAvailableTimer_Interval, // Setting of the time in ms for the timer to elapse
                AutoReset = false                                           // The timer isn't automatically reseted when it elapses, to reset the timer, it must be enabled or started
            };
            IsQueueAvailableTimer.Elapsed += IsQueueAvailableTimer_OnTimedEvent;    // Reference of the method to be executed when the timer elapses
            IsQueueAvailableTimer.Start();                                          // Start of the timer
        }

        //
        // PUBLIC METHODS
        //

        /// <summary>
        /// Method called by other project which initializes the variable <see cref="info"/>
        /// </summary>
        /// <param name="info_arg"><see cref="IniInfo"/> variable which contains the applicable settings from the calling project</param>
        public static void Initialize(IniInfo info_arg)
        {
            // From AlarmManagement
            if (info.AlarmType_Alarm == null && info_arg.AlarmType_Alarm != null) info.AlarmType_Alarm = info_arg.AlarmType_Alarm;          // if alarm type "Alarm" from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.AlarmType_Warning == null && info_arg.AlarmType_Warning != null) info.AlarmType_Warning = info_arg.AlarmType_Warning;  // if alarm type "Warning" from info wasn't already updated and if this value of the parameter isn't empty then we update info

            // From MainWindow
            if (info.RecipeWeight_mgG_Unit == null && info_arg.RecipeWeight_mgG_Unit != null) info.RecipeWeight_mgG_Unit = info_arg.RecipeWeight_mgG_Unit;                                  // if unit mg/g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.RecipeWeight_mgG_Conversion == null && info_arg.RecipeWeight_mgG_Conversion != null) info.RecipeWeight_mgG_Conversion = info_arg.RecipeWeight_mgG_Conversion;          // if conversion of the unit mg/g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.RecipeWeight_gG_Unit == null && info_arg.RecipeWeight_gG_Unit != null) info.RecipeWeight_gG_Unit = info_arg.RecipeWeight_gG_Unit;                                      // if unit g/g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            //if (info.RecipeWeight_gG_Conversion == null && info_arg.RecipeWeight_gG_Conversion != null) info.RecipeWeight_gG_Conversion = info_arg.RecipeWeight_gG_Conversion;              // if conversion of the unit g/g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.CycleFinalWeight_g_Unit == null && info_arg.CycleFinalWeight_g_Unit != null) info.CycleFinalWeight_g_Unit = info_arg.CycleFinalWeight_g_Unit;                          // if unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            //if (info.CycleFinalWeight_g_Conversion == null && info_arg.CycleFinalWeight_g_Conversion != null) info.CycleFinalWeight_g_Conversion = info_arg.CycleFinalWeight_g_Conversion;  // if conversion of the unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.Window == null && info_arg.Window != null) info.Window = info_arg.Window;                                                                                              // if conversion of the unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
        }

        /// <summary>
        /// Only allowed method to interact with the database. This method adds a task to be executed to the task queue 
        /// </summary>
        /// <param name="function">Method to be added to the queue</param>
        /// <returns>The added task, it allows to interact with the task (e.g. wait it ends, get its return value)</returns>
        public static Task<object> TaskEnQueue(Func<object> function)
        {
            Task<object> task = new Task<object>(function); // Creation of a task based on the method in parameter
            taskQueue.Enqueue(task);                        // Task added to the queue (note: the queue isn't empty anymore if it were)
            return task;                                    // The added task is returned
        }

        public static bool InsertRow_new(IBasTabInfo tableInfo, object[] values)
        {
            logger.Debug("InsertRow_new");

            if (tableInfo.Ids.Count() != values.Count())
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return false;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            string valueFields = "";
            string columnFields = "";

            for (int i = 0; i < tableInfo.Ids.Count(); i++)
            {
                if (values[i] != null)
                {
                    columnFields = columnFields + tableInfo.Ids[i] + ", ";
                    valueFields = valueFields + "@" + i.ToString() + ", ";
                    logger.Trace(i.ToString() + ": " + columnFields + " - " + valueFields);
                }
            }

            if (columnFields.Length == 0)
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return false;
            }

            columnFields = columnFields.Remove(columnFields.Length - 2);
            valueFields = valueFields.Remove(valueFields.Length - 2);

            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO " + tableInfo.TabName + " (" + columnFields + ") VALUES (" + valueFields + ");";
            logger.Trace("Insert command: " + command.CommandText);
            SetCommand_new(command, values);

            try
            {
                Close_reader(); // retirer l'un des 2 Close_reader()
                reader = command.ExecuteReader();
                Close_reader();
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBoxShow(ex.Message);
                return false;
            }
            //return false;
        }

        public static bool Update_Row_new(IComTabInfo tableInfo, object[] values, int id)
        {
            bool result = false;

            logger.Debug("Update_Row_new");

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            if (tableInfo.Ids.Count() != values.Count())
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return false;
            }

            Close_reader();

            string whereArg = " WHERE " + tableInfo.Ids[tableInfo.Id] + " = @" + tableInfo.Id.ToString();
            string setArg = " SET " + GetArg_new(tableInfo.Ids, values, ", ");

            if (setArg == " SET ")
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return false;
            }

            values[tableInfo.Id] = id;
            SendCommand_new(@"UPDATE " + tableInfo.TabName + setArg + whereArg, values);
            return result;
        }

        public static bool DeleteRow_new(IComTabInfo tableInfo, object id)
        {
            bool result = false;

            logger.Debug("DeleteRow " + tableInfo.TabName + " " + id);

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return result;
            }

            string whereArg = " WHERE " + tableInfo.Ids[tableInfo.Id] + " = @0 ";
            object[] values = new object[tableInfo.Ids.Count()];
            values[tableInfo.Id] = id;

            SendCommand_new(@"DELETE FROM " + tableInfo.TabName + whereArg, values);

            return result;
        }
        public static bool DeleteRows_new(IDtTabInfo tableInfo, DateTime firstRecordDate)
        {
            logger.Debug("DeleteRows " + tableInfo.TabName);

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            SendCommand_new(@"DELETE FROM " + tableInfo.TabName + " WHERE " + tableInfo.Ids[tableInfo.DateTime] + " < \"" + firstRecordDate.ToString("yyyy-MM-dd HH:mm:ss") + "\"");

            Close_reader();
            return true;
        }

        public static object[] GetOneRow_new(IComTabInfo table, int? id = null, object[] values = null)
        {
            logger.Debug("GetOneRow_new");

            if (id == null && table == null)
            {
                logger.Error(Settings.Default.Error16);
                MessageBoxShow(Settings.Default.Error16);
                return null;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            if (values == null) values = new object[table.Ids.Count()];
            if (id != null) values[table.Id] = id;
            SendCommand_Read_new(table, values);
            object[] result = ReadNext_new();

            if (result == null)
            {
                logger.Error(Settings.Default.Error17);
                MessageBoxShow(Settings.Default.Error17);
                return null;
            }

            if (ReadNext_new() != null)
            {
                logger.Error(Settings.Default.Error15);
                MessageBoxShow(Settings.Default.Error15);
                return null;
            }
            return result;
        }
        public static object[] GetOneRow(string tableName, int nRow, string orderBy = null, bool isOrderAsc = true)
        {
            logger.Debug("GetOneRow");

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            string orderArg = "";
            if (orderBy != null)
            {
                orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
            }

            SendCommand_new("SELECT * FROM " + tableName + orderArg + " LIMIT " + (nRow - 1).ToString() + ", 1");
            object[] row = ReadNext_new();

            if (row == null)
            {
                logger.Error(Settings.Default.Error17);
                MessageBoxShow(Settings.Default.Error17);
                return null;
            }

            if (ReadNext_new() != null)
            {
                logger.Error(Settings.Default.Error15);
                MessageBoxShow(Settings.Default.Error15);
                return null;
            }
            return row;
        }
        public static List<object[]> GetRows_new(ReadInfo readInfo, object[] values = null, int nRows = 0)
        {
            logger.Debug("GetRows");

            if (nRows > Settings.Default.MaxNumbRows || nRows < -1)
            {
                logger.Error(Settings.Default.Error_NumbRowsIncorrect);
                MessageBoxShow(Settings.Default.Error_NumbRowsIncorrect);
                return null;
            }
            if (values != null && readInfo.TableInfo.Ids.Count() != values.Count())
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return null;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            SendCommand_Read_new(readInfo, values);

            List<object[]> tables = new List<object[]>();
            object[] table;
            int i = 0;
            int n = nRows == 0 ? Settings.Default.MaxNumbRows : nRows;

            do
            {
                table = ReadNext_new();
                if (table != null) tables.Add(table);
                i++;
            } while (table != null && (i < n || n == -1));

            if (nRows == 0 && i == n)
            {
                logger.Error(Settings.Default.Error_IDidntReadItAll);
                MessageBoxShow(Settings.Default.Error_IDidntReadItAll);
            }

            return tables;
        }
        public static List<object[]> GetAuditTrailRows_new(ReadInfo _readInfo, int nRows = 0)
        {
            logger.Debug("GetAuditTrailRows_new");
            ReadInfo readInfo = new ReadInfo(_readInfo, new AuditTrailInfo());

            if (nRows > Settings.Default.MaxNumbRows || nRows < 0)
            {
                logger.Error(Settings.Default.Error_NumbRowsIncorrect);
                MessageBoxShow(Settings.Default.Error_NumbRowsIncorrect);
                return null;
            }

            if (readInfo.DtBefore == null || readInfo.DtAfter == null)
            {
                logger.Error(Settings.Default.Error_ReadAudit_ArgIncorrect);
                MessageBoxShow(Settings.Default.Error_ReadAudit_ArgIncorrect);
                return null;
            }

            SendCommand_ReadAuditTrail_new(readInfo: readInfo);

            List<object[]> tables = new List<object[]>();
            object[] table;
            int i = 0;
            int n = nRows == 0 ? Settings.Default.MaxNumbRows : nRows;

            table = ReadNext_new();

            while (table != null && i < n)
            {
                tables.Add(table);
                table = ReadNext_new();
                i++;
            }

            if (nRows == 0 && i == n)
            {
                logger.Error(Settings.Default.Error_IDidntReadItAll);
                MessageBoxShow(Settings.Default.Error_IDidntReadItAll);
            }
            return tables;
        }
        public static List<object[]> GetAlarms_new(int firstId, int lastId, bool readAlert = false)
        {
            logger.Debug("GetAlarms_new");

            SendCommand_ReadAlarms_new(firstId, lastId, readAlert);

            List<object[]> rows = new List<object[]>();
            object[] row;

            row = ReadNext_new();

            while (row != null)
            {
                rows.Add(row);
                row = ReadNext_new();
            }

            return rows;
        }
        public static List<object[]> GetLastRecipes_new(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            logger.Debug("GetLastRecipes");

            SendCommand_GetLastRecipes(status);

            List<object[]> rows = new List<object[]>();
            object[] row;

            row = ReadNext_new();

            while (row != null)
            {
                rows.Add(row);
                row = ReadNext_new();
            }

            return rows;
        }
        public static DateTime? GetLastDailyTestDate(DailyTestInfo sampleInfo, object[] sampleValues, DateTime? lastSample = null)
        {
            logger.Debug("SendCommand_GetLastSampling");

            DateTime? returnDate;

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            string whereArg = " WHERE " + GetArg_new(sampleInfo.Ids, sampleValues, " AND ") + (lastSample == null ? "" : " AND " + sampleInfo.Ids[sampleInfo.DateTime] + " < '" + ((DateTime)lastSample).ToString(Settings.Default.Timestamp_Format)) + "'";                      

            SendCommand_new(@"SELECT MAX(" + sampleInfo.Ids[sampleInfo.DateTime] + ") FROM " + sampleInfo.TabName + whereArg, sampleValues);

            if (IsReaderNotAvailable()) return null;

            try
            {
                reader.Read();

                if (!reader.IsDBNull(0))
                {
                    returnDate = reader.GetDateTime(0);
                    reader.Read();

                    if (reader.FieldCount == 0) // je crois que cette vérification ne sert à rien, il faut vérifier que la requête le renvoie plus de résultat
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
                Close_reader();
                return returnDate;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBoxShow(ex.Message);
            }
            return null;
        }
        public static int GetMax_new(IComTabInfo tableInfo, string column, object[] values = null)
        {
            int result = -1;

            logger.Debug("GetMax_new " + tableInfo.TabName);

            if (values != null && tableInfo.Ids.Count() != values.Count())
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return -1;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return -1;
            }

            int n;
            string whereArg = values == null ? "" : " WHERE " + GetArg_new(tableInfo.Ids, values, " AND ");
            SendCommand_new(@"SELECT MAX(" + column + ") FROM " + tableInfo.TabName + whereArg, values);

            try
            {
                object result_o = ReadNext_new()[0];
                if (result_o == null || result_o.ToString() == "")
                {
                    return 0;
                }
                result = int.Parse(result_o.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBoxShow(ex.Message);
                return -1;
            }

            if (ReadNext_new() != null)
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return -1;
            }

            return result;
        }
        public static int GetRowCount(string tableName)
        {
            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return -1;
            }

            SendCommand_new("SELECT COUNT(*) FROM " + tableName);
            object[] values = ReadNext_new();

            if (values.Count() != 1)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(values[0]);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return -1;
            }
        }
        public static bool CreateTempTable()
        {
            TempInfo tempInfo = new TempInfo();
            string fields = tempInfo.Ids[tempInfo.Speed] + " DECIMAL(5,1) NOT NULL, " + tempInfo.Ids[tempInfo.Pressure] + " DECIMAL(5,1) NOT NULL";
            bool result;

            logger.Debug("CreateTempTable " + fields);

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            result = SendCommand_new(@"DROP TABLE IF EXISTS " + Settings.Default.Temp_TableName);
            if(result) result = SendCommand_new(@"CREATE TABLE " + Settings.Default.Temp_TableName + " (" +
                    "id  INT NOT NULL auto_increment PRIMARY KEY," +
                    fields + ")");
            return result;
        }
        public static object[] GetResultRowTemp_new()
        {
            TempInfo tempInfo = new TempInfo();
            string select = "AVG(" + tempInfo.Ids[tempInfo.Speed] + "), AVG(" + tempInfo.Ids[tempInfo.Pressure] +
                "), STD(" + tempInfo.Ids[tempInfo.Speed] + "), STD(" + tempInfo.Ids[tempInfo.Pressure] + ")";

            logger.Debug("SelectFromTemp " + select);

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            SendCommand_new(@"SELECT " + select + " FROM " + tempInfo.TabName + ";");
            return ReadNext_new();
        }
        public static int GetRecipeStatus(RecipeStatus status)
        {
            switch (status)
            {
                case RecipeStatus.DRAFT:
                    return 0;
                case RecipeStatus.PROD:
                    return 1;
                case RecipeStatus.OBSOLETE:
                    return 2;
                default:
                    return -1;
            }
        }

        //
        // PRIVATE METHODS
        //

        /// <summary>
        /// Connection to the database
        /// </summary>
        private static void Connect()
        {
            // If the database is already connected then the method is stopped
            if (IsConnected())
            {
                logger.Debug("No Connect"); // Log a debug message
                return;                     // End of the method
            }

            logger.Debug("Connect");    // Log a debug message
            isConnecting = true;        // Activate the connecting flag to inform that connection is on going

            //MessageBox.Show(Decrypt(Settings.Default.ConnectionInfo.Password, key) + " - " + Settings.Default.ConnectionInfo.Password);

            // Creation of the connection builer
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = Settings.Default.ConnectionInfo.Server,        // Setting of the server to connect to
                UserID = Settings.Default.ConnectionInfo.UserID,        // Setting of the user ID
                Password = Decrypt(Settings.Default.ConnectionInfo.Password, key),    // Setting of the password of the used user ID
                Database = Settings.Default.ConnectionInfo.Db,          // Setting of the database to use
                AllowZeroDateTime = true,                               // Allow zero date and time (00.00.0000 00:00:00)
                Pooling = true,                                         // I forgot ;-P
                MinimumPoolSize = 2                                     // The minimum number of available connection to the database is set to 2 (it's important to have a pool size for performance issue)
            };

            connection = new MySqlConnection(builder.ConnectionString); // Setting of the connection information from the builder to the variable connection

            try { if (!StopScan) connection.Open(); }           // If a deconnection isn't ongoing then try to connect to the database
            catch (Exception ex) { logger.Error(ex.Message); }  // If the connection generated an exception the log the error message of the exception

            if (!StopScan) scanConnectTimer.Start();            // If a deconnection isn't ongoing then start the timer to check the connection
            isConnecting = false;                               // Desactivate the connecting flag to inform that no connection is on going
        }

        /// <summary>
        /// Disconnection of the database
        /// </summary>
        private async static void Disconnect()
        {
            // If the connection to the database is already removed the end of the method
            if (!IsConnected())
            {
                logger.Debug("Already disconnected");   // Log a debug message
                return;                                 // End of the method
            }

            logger.Debug("Disconnect");     // Log a debug message
            scanConnectTimer.Stop();        // Stop of the timer to check the connection
            StopScan = true;                // Activate the flag to inform that the disconnection is on going

            // If a connection is on going then wait of the end of the connection
            while (isConnecting)
            {
                logger.Debug("Disconnect on going");                    // Log a debug message
                await Task.Delay(Settings.Default.Disconnect_WaitTime); // Wait a setted amount of time in ms
            }
            StopScan = false;   // Desactivate the flag to inform that the no disconnection is on going

            try { connection.Close(); }                         // Try to close the connection
            catch (Exception ex) { logger.Error(ex.Message); }  // In case of error, log the error message
        }

        /// <summary>
        /// Get the status of the connection
        /// </summary>
        /// <returns>True if the database is connected, false otherwise</returns>
        private static bool IsConnected()
        {
            if (connection == null) return false;                           // If the connection variable is null then the method is stopped and return false
            return connection.State == System.Data.ConnectionState.Open;    // Returns True if the connection status is open, false otherwise
        }

        // Method which executes the next task from the queue
        private async static void TaskDeQueue()
        {
            // If the task queue isn't empty then the next task is executed
            if (taskQueue.Count > 0)
            {
                //logger.Debug("Dequeue on going");   // Log a debug message
                QueueEmptyCount = 0;                // Initialize the empty queue counter
                lastTask = taskQueue.Dequeue();     // Get the next task and remove it from the queue
                lastTask.Start();                   // Execute the task
                lastTask.Wait();                    // Wait for the task to end

                if (taskQueue.Count == 0) await Task.Delay(Settings.Default.TaskDeQueue_Wait); // If the executed task was the last of the queue then the program waits few ms
                TaskDeQueue();  // Execute the next task
            }
            // Else (the queue is empty) then...
            else
            {
                //logger.Debug("Wait for new task");  // Log a debug message
                IsQueueAvailableTimer.Start();      // Start the timer which 
            }
        }

        // Method executed when the timer (which checks if the task queue isn't empty anymore) elapses
        private static void IsQueueAvailableTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // If the queue is empty then the timer is reseted
            if (taskQueue.Count == 0) IsQueueAvailableTimer.Enabled = true;
            // Else then...
            else
            {
                //logger.Debug("Start TaskDeQueue");      // Log a debug message
                IsQueueAvailableTimer.Enabled = false;  // The timer is stopped
                if (!IsConnected()) Connect();          // If the database is disconnected, it is connected
                //if(lastTask != null) lastTask.Wait();
                TaskDeQueue();                          // Execution of the next task on the queue
            }
        }

        // Method executed when the empty queue timer elapses
        private static void QueueEmptyTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // If the database is not disconnected then we log a debug message
            if (QueueEmptyCount > 0 && QueueEmptyCount < Settings.Default.QueueEmptyCount_Max + 1) logger.Debug("QueueEmptyCount: " + QueueEmptyCount.ToString());

            // If the queue is empty then...
            if (taskQueue.Count == 0 && (lastTask == null || lastTask.IsCompleted))
            {
                QueueEmptyCount++;  // Increment of the empty queue counter
                // If the counter reaches a setting value, the database is diconnected
                if (QueueEmptyCount > Settings.Default.QueueEmptyCount_Max && IsConnected()) Disconnect();
            }
        }

        //----------------------------------

        // Method executed when the scan connect timer elapses
        private static void ScanConnectTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {//logger.Debug("ScanConnect");
            // If the database isn't connected then we connect it
            if (!IsConnected())
            {
                Connect();  // Connection to the database
                logger.Info(Settings.Default.Info01 + IsConnected().ToString());    // Log an Info of the connected status
            }
            scanConnectTimer.Enabled = true;    // The timer is reseted
        }

        private static bool SendCommand_new(string commandText, object[] values = null)
        {
            //logger.Debug("SendCommand " + commandText); // Log a debug message
            bool result = false;                        // Initialize to false the return value

            // If the database is not connected then an error message is displayed, the method is stoped and returns false
            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed); // Log an error message
                return false;                                           // Returns false
            }

            Close_reader(); // The reader is closed

            MySqlCommand command = connection.CreateCommand();  // Creation of a command variable
            command.CommandText = commandText;                  // Set the text of the command variable based on the parameter

            bool isCommandOk = true;    // Creation of a boolean variable to follow 
            if (values != null)
            {
                isCommandOk = SetCommand_new(command, values);
            }

            if (!isCommandOk)
            {
                logger.Error(Settings.Default.Error02 + isCommandOk.ToString());
                MessageBoxShow(Settings.Default.Error02);
                return false;
            }

            try
            {
                reader = command.ExecuteReader();
                result = true;
            }
            catch (Exception ex)
            {
                reader = null;
                logger.Error(ex.Message);
                MessageBoxShow(ex.Message);
            }
            return result;
        }
        private static void SendCommand_Read_new(IComTabInfo tableInfo, object[] values, string orderBy = null, bool isOrderAsc = true)
        {
            logger.Debug("SendCommand_Read");

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string whereArg = " WHERE " + GetArg_new(tableInfo.Ids, values, " AND ");

            string orderArg = "";
            if (orderBy != null)
            {
                orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
            }

            SendCommand_new(@"SELECT * FROM " + tableInfo.TabName + whereArg + orderArg, values);
        }
        private static void SendCommand_Read_new(ReadInfo readInfo, object[] values = null)
        {
            logger.Debug("SendCommand_Read");

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string tableArg = values == null ? "" : GetArg_new(readInfo.TableInfo.Ids, values, " AND ");
            string whereArg = "";
            if (tableArg != "" || readInfo.CustomWhere != "")
            {
                whereArg = " WHERE " + tableArg + readInfo.CustomWhere;
            }

            string orderArg = "";
            if (readInfo.OrderBy != null)
            {
                orderArg = " ORDER BY " + readInfo.OrderBy + (readInfo.IsOrderAsc ? " ASC" : " DESC");
            }

            SendCommand_new(commandText: @"SELECT * FROM " + readInfo.TableInfo.TabName + whereArg + orderArg,
                values: values);
        }
        private static bool SendCommand_ReadAuditTrail_new(ReadInfo readInfo)
        {
            if (readInfo.DtBefore == null || readInfo.DtAfter == null)
            {
                logger.Error(Settings.Default.Error_ReadAudit_ArgIncorrect);
                MessageBoxShow(Settings.Default.Error_ReadAudit_ArgIncorrect);
                return false;
            }

            logger.Debug("SendCommand_ReadAuditTrail_new");

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            string whereDateTime = auditTrailInfo.Ids[auditTrailInfo.DateTime] + " >= @0 AND " + auditTrailInfo.Ids[auditTrailInfo.DateTime] + " <= @1";
            string eventType = " AND (";
            string orderArg = "";

            if (readInfo.EventTypes != null && readInfo.EventTypes.Length != 0)
            {
                for (int i = 0; i < readInfo.EventTypes.Length - 1; i++)
                {
                    eventType += auditTrailInfo.Ids[auditTrailInfo.EventType] + " = @" + (i + 2).ToString() + " OR ";
                }
                eventType += auditTrailInfo.Ids[auditTrailInfo.EventType] + " = @" + (readInfo.EventTypes.Length + 1).ToString() + ")";
            }
            else
            {
                eventType = "";
            }

            if (readInfo.OrderBy != null)
            {
                orderArg = " ORDER BY " + readInfo.OrderBy + (readInfo.IsOrderAsc ? " ASC" : " DESC");
            }

            List<object> values = new List<object>();

            values.Add(((DateTime)readInfo.DtBefore).ToString("yyyy-MM-dd HH:mm:ss"));
            values.Add(((DateTime)readInfo.DtAfter).ToString("yyyy-MM-dd HH:mm:ss"));

            if (readInfo.EventTypes != null)
            {
                for (int i = 0; i < readInfo.EventTypes.Length; i++)
                {
                    values.Add(readInfo.EventTypes[i]);
                }
            }

            return SendCommand_new(@"SELECT * FROM " + auditTrailInfo.TabName + " WHERE " + whereDateTime + eventType + orderArg, values.ToArray());
        }
        private static bool SendCommand_ReadAlarms_new(int firstId, int lastId, bool readAlert = false)
        {
            logger.Debug("SendCommand_ReadAlarms");

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (info.AlarmType_Alarm == null || info.AlarmType_Warning == null)
            {
                logger.Error(Settings.Default.Error12);
                MessageBoxShow(Settings.Default.Error12);
                return false;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            if (firstId > lastId)
            {
                MessageBoxShow("C'est pas bien ça");
                return false;
            }

            string whereId =
                auditTrailInfo.Ids[auditTrailInfo.Id] + " >= @0 AND " +
                auditTrailInfo.Ids[auditTrailInfo.Id] + " <= @1 AND ";
            string eventType = readAlert ?
                "(" + auditTrailInfo.Ids[auditTrailInfo.EventType] + " = '" + info.AlarmType_Alarm + "' OR " +
                auditTrailInfo.Ids[auditTrailInfo.EventType] + " = '" + info.AlarmType_Warning + "')"
                : auditTrailInfo.Ids[auditTrailInfo.EventType] + " = '" + info.AlarmType_Alarm + "'";

            object[] values = new object[2];
            values[0] = firstId;
            values[1] = lastId;

            return SendCommand_new(@"SELECT * FROM " + auditTrailInfo.TabName + " WHERE " + whereId + eventType, values);
        }
        private static void SendCommand_GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            // only prod pour la prod
            // only draft pour les tests de recette
            // only obsolete pour faire revivre une vieille recette
            // prod and draft pour modifier une recette

            //

            logger.Debug("SendCommand_GetLastRecipes");

            RecipeInfo recipeInfo = new RecipeInfo();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string statusFilter =
                status == RecipeStatus.DRAFT ? recipeInfo.Ids[recipeInfo.Status] + " = " + GetRecipeStatus(RecipeStatus.DRAFT) :
                status == RecipeStatus.OBSOLETE ? recipeInfo.Ids[recipeInfo.Status] + " = " + GetRecipeStatus(RecipeStatus.OBSOLETE) :
                status == RecipeStatus.PROD ? recipeInfo.Ids[recipeInfo.Status] + " = " + GetRecipeStatus(RecipeStatus.PROD) :
                status == RecipeStatus.PRODnDRAFT ? "(" + recipeInfo.Ids[recipeInfo.Status] + " = " + GetRecipeStatus(RecipeStatus.PROD) + " OR " +
                recipeInfo.Ids[recipeInfo.Status] + " = " + GetRecipeStatus(RecipeStatus.DRAFT) + ")" : "";

            if (statusFilter == "")
            {
                logger.Error(Settings.Default.Error03);
                MessageBoxShow(Settings.Default.Error03);
                return;
            }

            SendCommand_new("SELECT * FROM " + recipeInfo.TabName +
                " WHERE ((" + recipeInfo.Ids[recipeInfo.Name] + ", " +
                recipeInfo.Ids[recipeInfo.Version] + ") IN " +
                "(SELECT " + recipeInfo.Ids[recipeInfo.Name] +
                ", MAX(" + recipeInfo.Ids[recipeInfo.Version] + ") " +
                "FROM " + recipeInfo.TabName +
                (status == RecipeStatus.OBSOLETE ? "" : " WHERE " + statusFilter) +
                " GROUP BY " + recipeInfo.Ids[recipeInfo.Name] + "))" +
                (status == RecipeStatus.OBSOLETE ? " AND " + statusFilter : "") +
                " ORDER BY " + recipeInfo.Ids[recipeInfo.Name] + ";");
        }
        private static object[] ReadNext_new()
        {
            //logger.Debug("ReadNext new");

            object[] values = null;

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                MessageBoxShow(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            if (IsReaderNotAvailable())
            {
                logger.Error(Settings.Default.Error04);
                MessageBoxShow(Settings.Default.Error04);
                return null;
            }

            try
            {
                if (reader.Read())
                {
                    values = new object[reader.FieldCount];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values[i] = reader.GetValue(i);
                        //logger.Trace(i.ToString() + ": " + reader[i].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBoxShow(ex.Message);
                return null;
            }

            return values;
        }
        private static bool IsReaderNotAvailable() { return reader == null || reader.IsClosed; }
        private static void Close_reader() { if (!IsReaderNotAvailable()) reader.Close(); }
        private static string GetArg_new(string[] ids, object[] values, string separator, string prefix = "")
        {
            string arg = "";

            if (ids == null)
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return arg;
            }

            if (values == null)
            {
                return arg;
            }

            if (ids.Count() != values.Count())
            {
                logger.Error("On a un problème");
                MessageBoxShow("On a un problème");
                return arg;
            }

            arg = prefix;

            for (int i = 0; i < ids.Count(); i++)
            {
                if (values[i] != null) arg += (arg == prefix ? "" : separator) + ids[i] + "=@" + i.ToString();
            }

            return arg;
        }
        private static bool SetCommand_new(MySqlCommand command, object[] values)
        {
            logger.Debug("SetCommand");

            if (values.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                MessageBoxShow(Settings.Default.Error08);
                return false;
            }

            for (int i = 0; i < values.Count(); i++)
            {

                if (values[i] != null)
                {
                    try
                    {
                        logger.Trace("Value " + i.ToString() + ": " + values[i]);
                        command.Parameters.AddWithValue("@" + i.ToString(), values[i].ToString());
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                        MessageBoxShow(ex.Message);
                        return false;
                    }
                }
            }
            return true;
        }
        private static string Decrypt(string cipherText, string keyString)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = Encoding.Unicode.GetBytes(keyString);

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.KeySize = 256;
                cipher.BlockSize = 128;
                cipher.Padding = PaddingMode.PKCS7;

                using (var key = new Rfc2898DeriveBytes(keyBytes, new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8 }, 1000))
                {
                    cipher.Key = key.GetBytes(cipher.KeySize / 8);
                    cipher.IV = key.GetBytes(cipher.BlockSize / 8);
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, cipher.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                        cryptoStream.FlushFinalBlock();

                        byte[] plainBytes = memoryStream.ToArray();

                        return Encoding.Unicode.GetString(plainBytes, 0, plainBytes.Length);
                    }
                }
            }
        }
        private static void MessageBoxShow(string messageBoxText, MessageBoxButton button = MessageBoxButton.OK)
        {
            MessageBox.Show(messageBoxText, "", button);
        }
    }
}
