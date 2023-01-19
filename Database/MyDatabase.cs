using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
//using System.Threading;
using Database.Properties;
//using System.Windows.Threading;

// CONFIURE "yyyy-MM-dd HH:mm:ss" SOME DAY, PLEASE

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
        public decimal? RecipeWeight_gG_Conversion;
        ///<value>Unit g to put in the cycle database table (used by the class CycleWeightInfo)</value>
        public string CycleFinalWeight_g_Unit;
        ///<value>Conversion value of the unit g. Used to calculate the conversion ratio in the class CycleWeightInfo</value>
        public decimal? CycleFinalWeight_g_Conversion;
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

        //private static Window mainWindow;   // Main window of the application (used to attach the MessageBoxes to it)

        /// <value>
        /// Variable initialized by other projects
        /// </value>
        public static IniInfo info;

        //
        // MAYBE TO DELETE
        //

        //private readonly static List<int> mutexIDs = new List<int>();
        //private static ManualResetEvent signal = new ManualResetEvent(true);
        //private static int lastMutexID = 0;
        //public static List<int> AlarmListID = new List<int>();
        //public static List<string> AlarmListDescription = new List<string>();
        //public static List<string> AlarmListStatus = new List<string>();

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

        // Method executed when the empty queue timer elapses
        private static void QueueEmptyTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // If the database is not disconnected then we log a debug message
            if(QueueEmptyCount > 0 && QueueEmptyCount < Settings.Default.QueueEmptyCount_Max+1) logger.Debug("QueueEmptyCount: " + QueueEmptyCount.ToString());

            // If the queue is empty then...
            if (taskQueue.Count == 0 && (lastTask == null || lastTask.IsCompleted))
            {
                QueueEmptyCount++;  // Increment of the empty queue counter
                // If the counter reaches a setting value, the database is diconnected
                if (QueueEmptyCount > Settings.Default.QueueEmptyCount_Max && IsConnected()) Disconnect();
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
                logger.Debug("Start TaskDeQueue");      // Log a debug message
                IsQueueAvailableTimer.Enabled = false;  // The timer is stopped
                if (!IsConnected()) Connect();          // If the database is disconnected, it is connected
                //if(lastTask != null) lastTask.Wait();
                TaskDeQueue();                          // Execution of the next task on the queue
            }
        }

        // Method which executes the next task from the queue
        private async static void TaskDeQueue()
        {
            // If the task queue isn't empty then the next task is executed
            if (taskQueue.Count > 0)
            {
                logger.Debug("Dequeue on going");   // Log a debug message
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
                logger.Debug("Wait for new task");  // Log a debug message
                IsQueueAvailableTimer.Start();      // Start the timer which 
            }
        }

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
            if (info.RecipeWeight_gG_Conversion == null && info_arg.RecipeWeight_gG_Conversion != null) info.RecipeWeight_gG_Conversion = info_arg.RecipeWeight_gG_Conversion;              // if conversion of the unit g/g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.CycleFinalWeight_g_Unit == null && info_arg.CycleFinalWeight_g_Unit != null) info.CycleFinalWeight_g_Unit = info_arg.CycleFinalWeight_g_Unit;                          // if unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.CycleFinalWeight_g_Conversion == null && info_arg.CycleFinalWeight_g_Conversion != null) info.CycleFinalWeight_g_Conversion = info_arg.CycleFinalWeight_g_Conversion;  // if conversion of the unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
            if (info.Window == null && info_arg.Window != null) info.Window = info_arg.Window;                                                                                              // if conversion of the unit g from info wasn't already updated and if this value of the parameter isn't empty then we update info
        }

        /// <summary>
        /// Connection to the database
        /// </summary>
        public static void Connect()
        {
            // If the database is already connected then the method is stopped
            if (IsConnected())
            {
                logger.Debug("No Connect"); // Log a debug message
                return;                     // End of the method
            }

            logger.Debug("Connect");    // Log a debug message
            isConnecting = true;        // Activate the connecting flag to inform that connection is on going

            // Creation of the connection builer
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
            {
                Server = Settings.Default.ConnectionInfo.Server,        // Setting of the server to connect to
                UserID = Settings.Default.ConnectionInfo.UserID,        // Setting of the user ID
                Password = Settings.Default.ConnectionInfo.Password,    // Setting of the password of the used user ID
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
        public async static void Disconnect()
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
        public static bool IsConnected()
        {
            if (connection == null) return false;                           // If the connection variable is null then the method is stopped and return false
            return connection.State == System.Data.ConnectionState.Open;    // Returns True if the connection status is open, false otherwise
        }

        /// <summary>
        ///  Base method to interact with the database. This method allows to execute any SQL command to the open connection.
        /// </summary>
        /// <param name="commandText">SQL command to send</param>
        /// <param name="columns">List of columns, the non-empty values can be used in the command. Default value: null</param>
        /// <returns>True if the command was correctly executed, false otherwise</returns>
        public static bool SendCommand(string commandText, List<Column> columns = null)
        {
            logger.Debug("SendCommand " + commandText); // Log a debug message
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
            if (columns != null)
            {
                isCommandOk = SetCommand(command, columns);
            }

            if (!isCommandOk)
            {
                logger.Error(Settings.Default.Error02 + isCommandOk.ToString());
                ShowMessageBox(Settings.Default.Error02);
                goto End;
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
                ShowMessageBox(ex.Message);
            }

        End:
            return result;
        }

        public static void SendCommand_Read(IComTabInfo tableInfo, string orderBy = null, bool isOrderAsc = true, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("SendCommand_Read " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string whereArg = " WHERE " + GetArg(tableInfo.Columns, " AND ");

            string orderArg = "";
            if (orderBy != null)
            {
                orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
            }

            SendCommand(@"SELECT * FROM " + tableInfo.TabName + whereArg + orderArg, tableInfo.Columns);
        }
        public static void SendCommand_Read(ReadInfo readInfo, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("SendCommand_Read " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string tableArg = GetArg(readInfo.TableInfo.Columns, " AND ");
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

            SendCommand(commandText: @"SELECT * FROM " + readInfo.TableInfo.TabName + whereArg + orderArg,
                columns: readInfo.TableInfo.Columns);
        }
        public static int SendCommand_ReadAuditTrail(DateTime dtBefore, DateTime dtAfter, string[] eventTypes = null, string orderBy = null, bool isOrderAsc = true, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("SendCommand_ReadAuditTrail " + GetMutexIDs());

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            string whereDateTime = auditTrailInfo.Columns[auditTrailInfo.DateTime].Id + " >= @0 AND " + auditTrailInfo.Columns[auditTrailInfo.DateTime].Id + " <= @1";
            string eventType = " AND (";
            string orderArg = "";

            if (eventTypes != null && eventTypes.Length != 0)
            {
                for (int i = 0; i < eventTypes.Length - 1; i++)
                {
                    eventType += auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = @" + (i + 2).ToString() + " OR ";
                }
                eventType += auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = @" + (eventTypes.Length + 1).ToString() + ")";
            }
            else
            {
                eventType = "";
            }

            if (orderBy != null)
            {
                orderArg = " ORDER BY " + orderBy + (isOrderAsc ? " ASC" : " DESC");
            }

            List<Column> columns = new List<Column>();

            columns.Add(new Column()
            {
                Value = dtBefore.ToString("yyyy-MM-dd HH:mm:ss")
            });

            columns.Add(new Column()
            {
                Value = dtAfter.ToString("yyyy-MM-dd HH:mm:ss")
            });

            if (eventTypes != null)
            {
                for (int i = 0; i < eventTypes.Length; i++)
                {
                    columns.Add(new Column()
                    {
                        Value = eventTypes[i]
                    });
                }
            }

            SendCommand(@"SELECT * FROM " + auditTrailInfo.TabName + " WHERE " + whereDateTime + eventType + orderArg, columns);

        End:
            //if (isMutexReleased) Signal(mutexID);
            return mutex;
        }
        public static int SendCommand_ReadAuditTrail(ReadInfo readInfo, bool isMutexReleased = true, int mutex = -1)
        {
            int mutexID = Wait(mutex);

            if (readInfo.DtBefore == null || readInfo.DtAfter == null)
            {
                logger.Error(Settings.Default.Error_ReadAudit_ArgIncorrect);
                ShowMessageBox(Settings.Default.Error_ReadAudit_ArgIncorrect);
                return mutexID;
            }

            logger.Debug("SendCommand_ReadAuditTrail " + GetMutexIDs());

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            string whereDateTime = auditTrailInfo.Columns[auditTrailInfo.DateTime].Id + " >= @0 AND " + auditTrailInfo.Columns[auditTrailInfo.DateTime].Id + " <= @1";
            string eventType = " AND (";
            string orderArg = "";

            if (readInfo.EventTypes != null && readInfo.EventTypes.Length != 0)
            {
                for (int i = 0; i < readInfo.EventTypes.Length - 1; i++)
                {
                    eventType += auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = @" + (i + 2).ToString() + " OR ";
                }
                eventType += auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = @" + (readInfo.EventTypes.Length + 1).ToString() + ")";
            }
            else
            {
                eventType = "";
            }

            if (readInfo.OrderBy != null)
            {
                orderArg = " ORDER BY " + readInfo.OrderBy + (readInfo.IsOrderAsc ? " ASC" : " DESC");
            }

            List<Column> columns = new List<Column>();

            columns.Add(new Column()
            {
                Value = ((DateTime)readInfo.DtBefore).ToString("yyyy-MM-dd HH:mm:ss")
            });

            columns.Add(new Column()
            {
                Value = ((DateTime)readInfo.DtAfter).ToString("yyyy-MM-dd HH:mm:ss")
            });

            if (readInfo.EventTypes != null)
            {
                for (int i = 0; i < readInfo.EventTypes.Length; i++)
                {
                    columns.Add(new Column()
                    {
                        Value = readInfo.EventTypes[i]
                    });
                }
            }

            SendCommand(@"SELECT * FROM " + auditTrailInfo.TabName + " WHERE " + whereDateTime + eventType + orderArg, columns);

        End:
            //if (isMutexReleased) Signal(mutexID);
            return mutexID;
        }
        public static int SendCommand_ReadAlarms(int firstId, int lastId, bool readAlert = false)
        {
            int mutexID = Wait();

            logger.Debug("SendCommand_ReadAlarms " + GetMutexIDs());

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (info.AlarmType_Alarm == null || info.AlarmType_Warning == null)
            {
                logger.Error(Settings.Default.Error12);
                ShowMessageBox(Settings.Default.Error12);
                goto End;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            if (firstId > lastId)
            {
                ShowMessageBox("C'est pas bien ça");
                goto End;
            }

            string whereId =
                auditTrailInfo.Columns[auditTrailInfo.Id].Id + " >= @0 AND " +
                auditTrailInfo.Columns[auditTrailInfo.Id].Id + " <= @1 AND ";
            string eventType = readAlert ?
                "(" + auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = '" + info.AlarmType_Alarm + "' OR " +
                auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = '" + info.AlarmType_Warning + "')"
                : auditTrailInfo.Columns[auditTrailInfo.EventType].Id + " = '" + info.AlarmType_Alarm + "'";

            List<Column> columns = new List<Column>();
            columns.Add(new Column() { Value = firstId.ToString() });
            columns.Add(new Column() { Value = lastId.ToString() });

            SendCommand(@"SELECT * FROM " + auditTrailInfo.TabName + " WHERE " + whereId + eventType, columns);

        End:
            //Signal(mutexID);
            return mutexID;
        }
        public static void SendCommand_GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            // only prod pour la prod
            // only draft pour les tests de recette
            // only obsolete pour faire revivre une vieille recette
            // prod and draft pour modifier une recette

            //int mutexID = Wait();

            logger.Debug("SendCommand_GetLastRecipes" + GetMutexIDs());

            RecipeInfo recipeInfo = new RecipeInfo();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return;
            }

            string statusFilter =
                status == RecipeStatus.DRAFT ? recipeInfo.Columns[recipeInfo.Status].Id + " = " + GetRecipeStatus(RecipeStatus.DRAFT) :
                status == RecipeStatus.OBSOLETE ? recipeInfo.Columns[recipeInfo.Status].Id + " = " + GetRecipeStatus(RecipeStatus.OBSOLETE) :
                status == RecipeStatus.PROD ? recipeInfo.Columns[recipeInfo.Status].Id + " = " + GetRecipeStatus(RecipeStatus.PROD) :
                status == RecipeStatus.PRODnDRAFT ? "(" + recipeInfo.Columns[recipeInfo.Status].Id + " = " + GetRecipeStatus(RecipeStatus.PROD) + " OR " +
                recipeInfo.Columns[recipeInfo.Status].Id + " = " + GetRecipeStatus(RecipeStatus.DRAFT) + ")" : "";

            if (statusFilter == "")
            {
                logger.Error(Settings.Default.Error03);
                ShowMessageBox(Settings.Default.Error03);
                return;
            }

            SendCommand("SELECT * FROM " + recipeInfo.TabName +
                " WHERE ((" + recipeInfo.Columns[recipeInfo.Name].Id + ", " +
                recipeInfo.Columns[recipeInfo.Version].Id + ") IN " +
                "(SELECT " + recipeInfo.Columns[recipeInfo.Name].Id +
                ", MAX(" + recipeInfo.Columns[recipeInfo.Version].Id + ") " +
                "FROM " + recipeInfo.TabName +
                (status == RecipeStatus.OBSOLETE ? "" : " WHERE " + statusFilter) +
                " GROUP BY " + recipeInfo.Columns[recipeInfo.Name].Id + "))" +
                (status == RecipeStatus.OBSOLETE ? " AND " + statusFilter : "") +
                " ORDER BY " + recipeInfo.Columns[recipeInfo.Name].Id + ";");

            /*
            SendCommand("SELECT * FROM " + recipeInfo.TabName +
                " WHERE ((" + recipeInfo.Columns[recipeInfo.Name].Id + ", " +
                recipeInfo.Columns[recipeInfo.Version].Id + ") IN " +
                "(SELECT " + recipeInfo.Columns[recipeInfo.Name].Id +
                ", MAX(" + recipeInfo.Columns[recipeInfo.Version].Id + ") " +
                "FROM " + recipeInfo.TabName +
                " GROUP BY " + recipeInfo.Columns[recipeInfo.Name].Id + ")) AND " +
                statusFilter + " ORDER BY " + recipeInfo.Columns[recipeInfo.Name].Id + ";");*/

            /*
            SendCommand("SELECT " +
                recipeInfo.columns[recipeInfo.recipeName].id + ", " +
                recipeInfo.columns[recipeInfo.id].id +
                " FROM " + recipeInfo.name +
                " WHERE ((" + recipeInfo.columns[recipeInfo.recipeName].id + ", " +
                recipeInfo.columns[recipeInfo.version].id + ") IN " +
                "(SELECT " + recipeInfo.columns[recipeInfo.recipeName].id +
                ", MAX(" + recipeInfo.columns[recipeInfo.version].id + ") " +
                "FROM " + recipeInfo.name +
                " GROUP BY " + recipeInfo.columns[recipeInfo.recipeName].id + ")) AND " +
                statusFilter + " ORDER BY " + recipeInfo.columns[recipeInfo.recipeName].id + ";");
             */
        }

        public static IBasTabInfo ReadNext(Type tableType, int mutex = -1)
        {
            int mutexID = Wait(mutex);

            IBasTabInfo tableInfo = Activator.CreateInstance(tableType) as IBasTabInfo;

            logger.Debug("ReadNext ITableInfo" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                //return null;
                tableInfo = null;
                goto End;
            }

            if (IsReaderNotAvailable())
            {
                logger.Error(Settings.Default.Error04);
                ShowMessageBox(Settings.Default.Error04);
                //return null;
                tableInfo = null;
                goto End;
            }

            try
            {
                if (reader.Read() && tableInfo.Columns.Count == reader.FieldCount)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        tableInfo.Columns[i].Value = reader[i].ToString();
                        logger.Trace(tableInfo.Columns[i].Id + ": " + tableInfo.Columns[i].Value);
                    }
                }
                else
                {
                    if (tableInfo.Columns.Count != reader.FieldCount)
                    {
                        logger.Error(Settings.Default.Error14 + tableInfo.Columns.Count.ToString() + ", " + reader.FieldCount.ToString() + ", " + tableInfo.GetType().ToString());
                        ShowMessageBox(Settings.Default.Error14);
                    }
                    //return null;
                    tableInfo = null;
                    goto End;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ShowMessageBox(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            return tableInfo;
        }
        public static string[] ReadNext(int mutex = -1)
        {
            int mutexID = Wait(mutex);

            logger.Debug("ReadNext" + GetMutexIDs());

            string[] array = null;

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            if (IsReaderNotAvailable())
            {
                logger.Error(Settings.Default.Error04);
                ShowMessageBox(Settings.Default.Error04);
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
                ShowMessageBox(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            //logger.Trace(array.Length.ToString());
            return array;
        }
        public static bool[] ReadNextBool(int mutex = -1)
        {
            int mutexID = Wait(mutex);
            bool[] array = null;

            logger.Debug("ReadNextBool " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            if (IsReaderNotAvailable())
            {
                logger.Error(Settings.Default.Error04);
                ShowMessageBox(Settings.Default.Error04);
                goto End;
            }

            try
            {
                if (reader.Read())
                {
                    array = new bool[reader.FieldCount - 2];

                    for (int i = 0; i < reader.FieldCount - 2; i++)
                    {
                        array[i] = reader.GetBoolean(i + 2);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

        End:
            if (mutex == -1) Signal(mutexID);
            return array;
        }

        public static bool InsertRow(IBasTabInfo tableInfo, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            bool result = false;

            logger.Debug("InsertRow ITableInfo" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            if (tableInfo.Columns == null || tableInfo.Columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
                goto End;
            }

            //MySqlDataReader reader;
            string valueFields = "";
            string columnFields = "";

            //
            // Get arg, c'est nul !!! GET ARG je te dis
            //
            for (int i = 0; i < tableInfo.Columns.Count(); i++)
            {
                if (tableInfo.Columns[i].Value != "" && tableInfo.Columns[i].Value != null)
                {
                    columnFields = columnFields + tableInfo.Columns[i].Id + ", ";
                    valueFields = valueFields + "@" + i.ToString() + ", ";
                    logger.Trace(i.ToString() + ": " + columnFields + " - " + valueFields);
                }
            }

            if (columnFields.Length > 0)
            {
                columnFields = columnFields.Remove(columnFields.Length - 2);
                valueFields = valueFields.Remove(valueFields.Length - 2);
            }
            logger.Fatal("CORRIGE MOI çA !!!");
            MySqlCommand command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO " + tableInfo.TabName + " (" + columnFields + ") VALUES (" + valueFields + ");";
            logger.Trace("Insert command: " + command.CommandText);
            SetCommand(command, tableInfo.Columns);

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
                ShowMessageBox(ex.Message);
            }

        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static bool InsertRow_new(object obj)
        {
            //bool result = false;

            IBasTabInfo tableInfo = obj as IBasTabInfo;

            logger.Debug("InsertRow ITableInfo" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return false;
            }

            if (tableInfo.Columns == null || tableInfo.Columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
                return false;
            }

            //MySqlDataReader reader;
            string valueFields = "";
            string columnFields = "";

            //
            // Get arg, c'est nul !!! GET ARG je te dis
            //
            for (int i = 0; i < tableInfo.Columns.Count(); i++)
            {
                if (tableInfo.Columns[i].Value != "" && tableInfo.Columns[i].Value != null)
                {
                    columnFields = columnFields + tableInfo.Columns[i].Id + ", ";
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
            command.CommandText = @"INSERT INTO " + tableInfo.TabName + " (" + columnFields + ") VALUES (" + valueFields + ");";
            logger.Trace("Insert command: " + command.CommandText);
            SetCommand(command, tableInfo.Columns);

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
                ShowMessageBox(ex.Message);
            }
            return false;
        }

        public static bool Update_Row(IComTabInfo tableInfo, string id, int mutex = -1)
        {
            //int mutexID = Wait(mutex);
            bool result = false;

            logger.Debug("Update_Row ITableInfo " + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            if (tableInfo.Columns == null || tableInfo.Columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
                goto End;
            }

            Close_reader();

            string whereArg = " WHERE " + tableInfo.Columns[tableInfo.Id].Id + " = @" + tableInfo.Id.ToString();
            string setArg = " SET " + GetArg(tableInfo.Columns, ", ");
            //bool isCommandOk = true;

            tableInfo.Columns[tableInfo.Id].Value = id;
            SendCommand(@"UPDATE " + tableInfo.TabName + setArg + whereArg, tableInfo.Columns);
        /*
        MySqlCommand command = connection.CreateCommand();
        command.CommandText = @"UPDATE " + tableInfo.name + setArg + whereArg;
        logger.Trace(command.CommandText);
        isCommandOk = SetCommand(command, tableInfo.columns);
        command.Parameters.AddWithValue("@id", id);

        if (!isCommandOk)
        {
            logger.Error(Settings.Default.Error02);
            ShowMessageBox(Settings.Default.Error02);
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
            ShowMessageBox(ex.Message);
        }*/

        End:
            //if (mutex == -1) Signal(mutexID);
            return result;
        }

        public static bool DeleteRow(IComTabInfo tableInfo, string id)
        {
            bool result = false;

            logger.Debug("DeleteRow " + tableInfo.TabName + " " + id + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return result;
            }

            string whereArg = " WHERE " + tableInfo.Columns[tableInfo.Id].Id + " = @0 ";

            List<Column> columns = new List<Column>();
            columns.Add(new Column() { Value = id });

            SendCommand(@"DELETE FROM " + tableInfo.TabName + whereArg, columns);

            return result;
        }
        public static bool DeleteRows(IDtTabInfo tableInfo, DateTime lastRecordDate)
        {
            bool result = false;

            logger.Debug("DeleteRows " + tableInfo.TabName + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return result;
            }

            SendCommand(@"DELETE FROM " + tableInfo.TabName + " WHERE " + tableInfo.Columns[tableInfo.DateTime].Id + " < \"" + lastRecordDate.ToString("yyyy-MM-dd HH:mm:ss") + "\"");

            Close_reader();
            return result;
        }

        public static IComTabInfo GetOneRow(Type tableType = null, string id = null, IComTabInfo table = null)
        {
            int mutexID = Wait();

            logger.Debug("GetOneRow" + GetMutexIDs());

            IComTabInfo tableInfo;

            if (tableType == null && id == null && table == null)
            {
                logger.Error(Settings.Default.Error16);
                ShowMessageBox(Settings.Default.Error16);
                //return null;
                tableInfo = null;
                goto End;
            }

            if (table == null)
            {
                tableInfo = Activator.CreateInstance(tableType) as IComTabInfo;
            }
            else
            {
                tableInfo = Activator.CreateInstance(table.GetType()) as IComTabInfo;
                tableInfo.Columns = table.Columns;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                //return null;
                tableInfo = null;
                goto End;
            }

            if (table == null) tableInfo.Columns[tableInfo.Id].Value = id;
            SendCommand_Read(tableInfo, isMutexReleased: false, mutex: mutexID);
            tableInfo = (IComTabInfo)ReadNext(tableInfo.GetType(), mutexID);

            if (tableInfo == null)
            {
                logger.Error(Settings.Default.Error17);
                ShowMessageBox(Settings.Default.Error17);
                //return null;
                goto End;
            }

            if (ReadNext(tableInfo.GetType(), mutexID) != null)
            {
                logger.Error(Settings.Default.Error15);
                ShowMessageBox(Settings.Default.Error15);
                //return null;
                tableInfo = null;
                goto End;
            }
        End:
            //Close_reader();
            Signal(mutexID);
            return tableInfo;
        }
        public static string[] GetOneArrayRow(IComTabInfo tableInfo, string id)
        {
            int mutexID = Wait();
            string[] array;

            logger.Debug("GetOneRow" + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                //return null;
                array = null;
                goto End;
            }

            tableInfo.Columns[tableInfo.Id].Value = id;
            SendCommand_Read(tableInfo, isMutexReleased: false, mutex: mutexID);
            array = ReadNext(mutexID);

            if (ReadNext(mutexID) != null)
            {
                logger.Error(Settings.Default.Error15);
                ShowMessageBox(Settings.Default.Error15);
                //return null;
                array = null;
                goto End;
            }
        End:
            //Close_reader();
            Signal(mutexID);
            return array;
        }
        public static bool[] GetOneBoolRow(IComTabInfo table)
        {
            int mutexID = Wait();

            logger.Debug("GetOneBoolRow " + GetMutexIDs());

            //ITableInfo tableInfo;
            bool[] result = null;

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                //return null;
                goto End;
            }

            ReadInfo readInfo = new ReadInfo(
                _tableInfo: table);
            SendCommand_Read(readInfo, isMutexReleased: false, mutex: mutexID);
            result = ReadNextBool(mutexID);

            if (result == null)
            {
                logger.Error(Settings.Default.Error17);
                ShowMessageBox(Settings.Default.Error17);
                //return null;
                goto End;
            }

            if (ReadNextBool(mutexID) != null)
            {
                logger.Error(Settings.Default.Error15);
                ShowMessageBox(Settings.Default.Error15);
                //return null;
                goto End;
            }
        End:
            //Close_reader();
            Signal(mutexID);
            return result;
        }
        public static void GetOneBoolRow_new(object obj)
        {
            logger.Debug("GetOneBoolRow " + GetMutexIDs());

            IComTabInfo table = obj as IComTabInfo;

            //ITableInfo tableInfo;
            bool[] result = null;

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                //return null;
                goto End;
            }

            ReadInfo readInfo = new ReadInfo(
                _tableInfo: table);
            SendCommand_Read(readInfo, isMutexReleased: false);
            result = ReadNextBool();

            if (result == null)
            {
                logger.Error(Settings.Default.Error17);
                ShowMessageBox(Settings.Default.Error17);
                //return null;
                goto End;
            }

            if (ReadNextBool() != null)
            {
                logger.Error(Settings.Default.Error15);
                ShowMessageBox(Settings.Default.Error15);
                //return null;
                goto End;
            }
        End:
            //Close_reader();
            return;
        }
        public static List<IComTabInfo> GetRows(IComTabInfo tableInfo, int nRows = 0, string orderBy = null, bool isOrderAsc = true, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("GetRows " + GetMutexIDs());

            if (nRows > Settings.Default.MaxNumbRows || nRows < 0)
            {
                logger.Error(Settings.Default.Error_NumbRowsIncorrect);
                ShowMessageBox(Settings.Default.Error_NumbRowsIncorrect);
                return null;
            }

            int mutexID = Wait(mutex);
            SendCommand_Read(tableInfo: tableInfo, orderBy: orderBy, isOrderAsc: isOrderAsc, isMutexReleased: false, mutex: mutexID);

            List<IComTabInfo> tables = new List<IComTabInfo>();
            IComTabInfo table;
            int i = 1;
            int n = nRows == 0 ? Settings.Default.MaxNumbRows : nRows;
            /*
            table = (ITableInfo)ReadNext(tableInfo.GetType(), mutexID);

            while (table != null && i < n)
            {
                tables.Add(table);
                table = (ITableInfo)ReadNext(tableInfo.GetType(), mutexID);
                i++;
            }*/

            do
            {
                table = (IComTabInfo)ReadNext(tableInfo.GetType(), mutexID);
                if (table != null) tables.Add(table);
                i++;
                logger.Fatal(i.ToString() + ", " + n.ToString());
            } while (table != null && i < n);

            if (nRows == 0 && i == n)
            {
                logger.Error(Settings.Default.Error_IDidntReadItAll);
                ShowMessageBox(Settings.Default.Error_IDidntReadItAll);
            }

            if (isMutexReleased) Signal(mutexID);
            return tables;
        }
        public static List<IComTabInfo> GetRows(ReadInfo readInfo, int nRows = 0, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("GetRows " + GetMutexIDs());

            if (nRows > Settings.Default.MaxNumbRows || nRows < 0)
            {
                logger.Error(Settings.Default.Error_NumbRowsIncorrect);
                ShowMessageBox(Settings.Default.Error_NumbRowsIncorrect);
                return null;
            }

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                return null;
            }

            int mutexID = Wait(mutex);
            SendCommand_Read(readInfo: readInfo, isMutexReleased: false, mutex: mutexID);

            List<IComTabInfo> tables = new List<IComTabInfo>();
            IComTabInfo table;
            int i = 0;
            int n = nRows == 0 ? Settings.Default.MaxNumbRows : nRows;
            /*
                        table = (ITableInfo)ReadNext(readInfo.tableInfo.GetType(), mutexID);
                        while (table != null && i < n)
                        {
                            tables.Add(table);
                            table = (ITableInfo)ReadNext(readInfo.tableInfo.GetType(), mutexID);
                            i++;
                        }
            */
            do
            {
                table = (IComTabInfo)ReadNext(readInfo.TableInfo.GetType(), mutexID);
                if (table != null) tables.Add(table);
                i++;
            } while (table != null && i < n);

            if (nRows == 0 && i == n)
            {
                logger.Error(Settings.Default.Error_IDidntReadItAll);
                ShowMessageBox(Settings.Default.Error_IDidntReadItAll);
            }

            if (isMutexReleased) Signal(mutexID);
            return tables;
        }
        public static List<string[]> GetAuditTrailRows(ReadInfo _readInfo, int nRows = 0, bool isMutexReleased = true, int mutex = -1)
        {
            logger.Debug("GetAuditTrailRows " + GetMutexIDs());
            ReadInfo readInfo = new ReadInfo(_readInfo, new AuditTrailInfo());

            if (nRows > Settings.Default.MaxNumbRows || nRows < 0)
            {
                logger.Error(Settings.Default.Error_NumbRowsIncorrect);
                ShowMessageBox(Settings.Default.Error_NumbRowsIncorrect);
                return null;
            }

            if (readInfo.DtBefore == null || readInfo.DtAfter == null)
            {
                logger.Error(Settings.Default.Error_ReadAudit_ArgIncorrect);
                ShowMessageBox(Settings.Default.Error_ReadAudit_ArgIncorrect);
                return null;
            }

            int mutexID = Wait(mutex);
            SendCommand_ReadAuditTrail(readInfo: readInfo, isMutexReleased: false, mutex: mutexID);

            List<string[]> tables = new List<string[]>();
            string[] table;
            int i = 0;
            int n = nRows == 0 ? Settings.Default.MaxNumbRows : nRows;

            table = ReadNext(mutexID);

            while (table != null && i < n)
            {
                tables.Add(table);
                table = ReadNext(mutexID);
                i++;
            }

            if (nRows == 0 && i == n)
            {
                logger.Error(Settings.Default.Error_IDidntReadItAll);
                ShowMessageBox(Settings.Default.Error_IDidntReadItAll);
            }

            if (isMutexReleased) Signal(mutexID);
            return tables;
        }
        public static List<AuditTrailInfo> GetAlarms(int firstId, int lastId, bool readAlert = false)
        {
            logger.Debug("GetAlarms " + GetMutexIDs());

            int mutexID = SendCommand_ReadAlarms(firstId, lastId, readAlert);

            List<AuditTrailInfo> tables = new List<AuditTrailInfo>();
            AuditTrailInfo table;

            table = (AuditTrailInfo)ReadNext(typeof(AuditTrailInfo), mutexID);

            while (table != null)
            {
                tables.Add(table);
                table = (AuditTrailInfo)ReadNext(typeof(AuditTrailInfo), mutexID);
            }

            Signal(mutexID);
            return tables;
        }
        public static List<RecipeInfo> GetLastRecipes(RecipeStatus status = RecipeStatus.PRODnDRAFT)
        {
            logger.Debug("GetLastRecipes " + GetMutexIDs());

            SendCommand_GetLastRecipes(status);

            List<RecipeInfo> tables = new List<RecipeInfo>();
            RecipeInfo table;

            table = (RecipeInfo)ReadNext(typeof(RecipeInfo));

            while (table != null)
            {
                tables.Add(table);
                table = (RecipeInfo)ReadNext(typeof(RecipeInfo));
            }

            return tables;
        }

        public static int GetMax(IComTabInfo tableInfo, string column, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            int result = -1;

            logger.Debug("GetMax " + tableInfo.TabName + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            int n;
            string whereArg = " WHERE " + GetArg(tableInfo.Columns, " AND ");
            if (whereArg == " WHERE ")
            {
                whereArg = "";
            }
            SendCommand(@"SELECT MAX(" + column + ") FROM " + tableInfo.TabName + whereArg, tableInfo.Columns);

            try
            {
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
                ShowMessageBox(ex.Message);
            }
        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }
        public static int GetMax(string tableName, string column, int mutex = -1)
        {
            int mutexID = Wait(mutex);
            int result = -1;

            logger.Debug("GetMax " + tableName + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            int n;
            SendCommand(@"SELECT MAX(" + column + ") FROM " + tableName);

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

        End:
            if (mutex == -1) Signal(mutexID);
            return result;
        }

        public static bool CreateTempTable()
        {
            int mutexID = Wait();

            TempInfo tempInfo = new TempInfo();
            string fields = tempInfo.Columns[0].Id + " DECIMAL(5,1) NOT NULL, " + tempInfo.Columns[1].Id + " DECIMAL(5,1) NOT NULL";
            bool result = false;

            logger.Debug("CreateTempTable " + fields + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            result = SendCommand(@"DROP TABLE IF EXISTS " + Settings.Default.Temp_TableName);
            if(result) result = SendCommand(@"CREATE TABLE " + Settings.Default.Temp_TableName + " (" +
                    "id  INT NOT NULL auto_increment PRIMARY KEY," +
                    fields + ")");
        /*
        Close_reader();

        // On supprimer la table temp si jamais elle existe
        MySqlCommand command = connection.CreateCommand();

        try
        {
            command.CommandText = @"DROP TABLE IF EXISTS " + Settings.Default.Temp_TableName;
            reader = command.ExecuteReader();
            Close_reader();
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            ShowMessageBox(ex.Message);
        }

        try
        {
            command.CommandText = @"CREATE TABLE " + Settings.Default.Temp_TableName + " (" +
                "id  INT NOT NULL auto_increment PRIMARY KEY," +
                fields + ")";
            reader = command.ExecuteReader();
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            ShowMessageBox(ex.Message);
        }*/
        End:
            Close_reader();
            Signal(mutexID);
            //dbReady = true;
            return result;
        }
        public static TempResultInfo GetResultRowTemp()
        {
            int mutexID = Wait();

            TempInfo tempInfo = new TempInfo();
            string select = "AVG(" + tempInfo.Columns[tempInfo.Speed].Id + "), AVG(" + tempInfo.Columns[tempInfo.Pressure].Id +
                "), STD(" + tempInfo.Columns[tempInfo.Speed].Id + "), STD(" + tempInfo.Columns[tempInfo.Pressure].Id + ")";

            logger.Debug("SelectFromTemp " + select + GetMutexIDs());

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error_connectToDbFailed);
                ShowMessageBox(Settings.Default.Error_connectToDbFailed);
                goto End;
            }

            SendCommand(@"SELECT " + select + " FROM " + tempInfo.TabName + ";");
        /*
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
            ShowMessageBox(ex.Message);
        }
        */
        End:
            Signal(mutexID);
            return (TempResultInfo)ReadNext(typeof(TempResultInfo));
        }

        public static void Close_reader() { if (!IsReaderNotAvailable()) reader.Close(); }
        public static bool IsReaderNotAvailable() { return reader == null || reader.IsClosed; }

        // Méthode outils
        private static string GetArg(List<Column> columns, string separator, string prefix = "")
        {
            string arg = "";

            if (columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
                return arg;
            }

            arg = prefix;

            for (int i = 0; i < columns.Count(); i++)
            {
                if (columns[i].Value != null && columns[i].Value != "") arg += (arg == prefix ? "" : separator) + columns[i].Id + "=@" + i.ToString();
            }

            return arg;
        }

        private static bool SetCommand(MySqlCommand command, List<Column> columns)
        {
            logger.Debug("SetCommand");

            if (columns.Count() == 0)
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
                return false;
            }

            for (int i = 0; i < columns.Count(); i++)
            {

                if (columns[i].Value != "" && columns[i].Value != null)
                {
                    try
                    {
                        logger.Trace("Value " + i.ToString() + ": " + columns[i].Value);
                        command.Parameters.AddWithValue("@" + i.ToString(), columns[i].Value);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex.Message);
                        ShowMessageBox(ex.Message);
                        return false;
                    }
                }
            }

            /*
            if (columns.Count() < indexes.Count()) {
                logger.Error(Settings.Default.Error10);
                ShowMessageBox(Settings.Default.Error10);
                return;
            }

            for (int i = 0; i < indexes.Count(); i++)
            {
                logger.Trace("Value " + i.ToString() + ": " + columns[indexes[i]].value);
                command.Parameters.AddWithValue("@" + i.ToString(), columns[indexes[i]].value);
            }             
             */
            return true;
        }

        public static int Wait(int mutex = -1)
        {
            //logger.Debug("Wait " + GetMutexIDs());
            /*
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

            logger.Trace("Wait " + GetMutexIDs());

            return mutexID;*/
            return 0;
        }
        public static void Signal(int mutex)
        {/*
            if (mutex != mutexIDs[0])
            {
                logger.Error(Settings.Default.Error11);
                ShowMessageBox(Settings.Default.Error11);
            }

            mutexIDs.RemoveAt(0);
            signal.Set();*/
            //logger.Debug("Signal " + GetMutexIDs());
        }
        private static string GetMutexIDs()
        {/*
            string text = " ";
            for (int i = 0; i < mutexIDs.Count; i++)
            {
                text = text + mutexIDs[i].ToString() + "_";
            }

            return text.TrimEnd('_');*/
            return "";
        }

        // Interface à implémenter
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

        public static void ShowMessageBox(string message)
        {
            if (info.Window != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(info.Window, message);
                }));
            }
            else
            {
                MessageBox.Show(message);
            }
        }
    }
}
