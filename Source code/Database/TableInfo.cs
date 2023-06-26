using Database.Properties;
using Message;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;

namespace Database
{
    //
    // INTERFACES
    //

    /// <summary>
    /// Basic interface for a database table.
    /// <para>Creation revision: 001</para> 
    /// </summary>
    public interface IBasTabInfo
    {
        /// <value>Name of the database table</value>
        string TabName { get; }
        /// <value>Columns of the database table</value>
        //List<Column> Columns { get; set; }
        /// <value>Name of the columns of the database table</value>
        string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        string[] Descriptions { get; }
    }

    /// <summary>
    /// Interface of a common database table.
    /// Based on the basic database table interface
    /// <para>Creation revision: 001</para>
    /// </summary>
    public interface IComTabInfo : IBasTabInfo
    {
        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented.</value>
        int Id { get; }
    }

    /// <summary>
    /// Interface of a basic table which contains a date and time column.
    /// Based on the basic database table interface
    /// <para>Creation revision: 001</para>
    /// </summary>
    public interface IDtTabInfo : IBasTabInfo
    {
        /// <value>Index of the date and time column</value>
        int DateTime { get; }
    }

    /// <summary>
    /// Interface of a sequential table (table whose rows can refer to another row from the same table or another sequential table).
    /// Based on the common database table interface
    /// <para>Creation revision: 001</para>
    /// </summary>
    public interface ISeqTabInfo : IComTabInfo
    {
        /// <value>Identification number of the current sequential table</value>
        int SeqType { get; }

        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table</value>
        int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table</value>
        int NextSeqId { get; }
    }

    /// <summary>
    /// Interface of a cycle sequential table. This type of table contains the information of the sequence of a cycle (results and recipe information). Recipe information is based on the applicable recipe table (which is a sequential table)
    /// Based on the sequential database table interface
    /// <para>Creation revision: 001</para>
    /// </summary>
    public interface ICycleSeqInfo : ISeqTabInfo
    {
        /// <summary>
        /// Method which sets the recipe information related to the applicable recipe table 
        /// </summary>
        /// <param name="recipe">Variable containing the recipe information</param>
        /// <param name="idCycle">The value of the id column (see Id from IComTabInfo) of the row of the first cycle sequential table</param>
        //void SetRecipeParameters(ISeqTabInfo recipe, int idCycle);

        /// <summary>
        /// Method which returns the recipe information related to the applicable recipe table 
        /// </summary>
        /// <param name="recipe">Variable containing the recipe information</param>
        /// <param name="idCycle">The value of the id column (see Id from IComTabInfo) of the row of the first cycle sequential table</param>
        object[] GetRecipeParameters(object[] recipe, int idCycle);
    }

    //
    // DATABASE TABLE CLASSES
    //

    /// <summary>
    /// Class containing the infomration of the audit trail database table. The table must contain at least the following colummns: 
    /// Id (UNIQUE INTEGER)
    /// DateTime, 
    /// username, 
    /// eventType, 
    /// description, 
    /// valueBefore, 
    /// valueAfter, 
    /// comment
    /// <para>Creation revision: 001</para>
    /// </summary>
    public class AuditTrailInfo : IComTabInfo, IDtTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public AuditTrailInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.AuditTrail_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the list of names to be displayed of the columns from the settings
            StringCollection colDesc = Settings.Default.AuditTrail_ColDesc;
            Descriptions = new string[colDesc.Count];
            colDesc.CopyTo(Descriptions, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.AuditTrail_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table and the name of the columns to be displayed
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i], colDesc[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.AuditTrail_ColN_id;
            DateTime = Settings.Default.AuditTrail_ColN_dateTime;
            Username = Settings.Default.AuditTrail_ColN_username;
            EventType = Settings.Default.AuditTrail_ColN_eventType;
            Description = Settings.Default.AuditTrail_ColN_description;
            ValueBefore = Settings.Default.AuditTrail_ColN_valueBefore;
            ValueAfter = Settings.Default.AuditTrail_ColN_valueAfter;
            Comment = Settings.Default.AuditTrail_ColN_comment;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; set; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }

        /// <value>Index of the date and time column. This column contains the date and time of the insertion the rows. From IDtTabInfo</value>
        public int DateTime { get; }

        /// <value>Index of the username column. This column contains the username of the user who performed the event to be logged in the audit trail</value>
        public int Username { get; }

        /// <value>Index of the event type column. This column contains the type the event to be logged in the audit trail</value>
        public int EventType { get; }

        /// <value>Index of the description column. This column contains the description the event to be logged in the audit trail</value>
        public int Description { get; }

        /// <value>Index of the value before column. This column contains the value before the event to be logged in the audit trail</value>
        public int ValueBefore { get; }

        /// <value>Index of the value after column. This column contains the value after the event to be logged in the audit trail</value>
        public int ValueAfter { get; }

        /// <value>Index of the comment column. This column contains the comment of the event to be logged in the audit trail</value>
        public int Comment { get; }
    }

    /// <summary>
    /// Class containing the infomration of the access database table. The table must contain at least the following colummns: 
    /// Id,
    /// Role,
    /// cyclestart... (TBD).
    /// <para>Creation revision: 001</para>
    /// </summary>
    public class AccessTableInfo : IComTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public AccessTableInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.AccessTable_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            
            // Import the name of the database table from the settings
            TabName = Settings.Default.AccessTable_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.AccessTable_ColN_id;
            Role = Settings.Default.AccessTable_ColN_role;
        }

        static AccessTableInfo()
        {
            CycleStart = Settings.Default.AccessTable_ColN_cycleStart;
            RecipeUpdate = Settings.Default.AccessTable_ColN_recipeCreate;
            Backup = Settings.Default.AccessTable_ColN_Backup;
            Parameters = Settings.Default.AccessTable_ColN_Parameters;
            DailyTest = Settings.Default.AccessTable_ColN_DailyTest;
            ApplicationStop = Settings.Default.AccessTable_ColN_applicationStop;
            AckAlarm = Settings.Default.AccessTable_ColN_AckAlarm;

            // Import the name of the following access role from the settings: operator, supervisor, administrator and guest
            OperatorRole = Settings.Default.AccessTable_Role_operator;
            SupervisorRole = Settings.Default.AccessTable_Role_supervisor;
            AdministratorRole = Settings.Default.AccessTable_Role_administrator;
            NoneRole = Settings.Default.AccessTable_Role_none;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }

        /// <value>Index of the role column. This column contains the name of the applicable access role</value>
        public int Role { get; }

        /// <value>Index of the cycle start column. This column is the access right of the action start a cycle for the applicable access role</value>
        public static int CycleStart { get; }

        /// <value>Index of the create recipe column. This column is the access the right of the action create a recipe for the applicable access role</value>
        public static int RecipeUpdate { get; }

        /// <value>Index of the backup column. This column is the access the right of the backup and archiving actions</value>
        public static int Backup { get; }

        /// <value>Index of the parameters column. This column is the access the right of the parameters screen</value>
        public static int Parameters { get; }

        /// <value>Index of the daily test column. This column is the access the right of the daily test</value>
        public static int DailyTest { get; }

        /// <value>Index of the ackowledgment of alarm column. This column is the access the right of the ackowledgment of alarm</value>
        public static int AckAlarm { get; }

        /// <value>Index of the application stop column. This column is the access the right of the action stop the application for the applicable access role</value>
        public static int ApplicationStop { get; }

        /// <value>Name of the operator access role</value>
        public static string OperatorRole { get; }

        /// <value>Name of the supervisor access role</value>
        public static string SupervisorRole { get; }

        /// <value>Name of the administrator access role</value>
        public static string AdministratorRole { get; }

        /// <value>Name of the guest access role (role without access)</value>
        public static string NoneRole { get; }
    }

    /// <summary>
    /// Class containing the infomration of the recipe database table. The table must contain at least the following colummns: 
    /// id, 
    /// firstSeqType, 
    /// firstSeqId, 
    /// recipeName, 
    /// version, 
    /// status
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>Recipes are sequences of rows of sequencetial tables based on ISeqTabInfo interface</remarks>
    public class RecipeInfo : ISeqTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public RecipeInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.Recipe_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the list of names to be displayed of the columns from the settings
            StringCollection colDesc = Settings.Default.Recipe_ColDesc;
            Descriptions = new string[colDesc.Count];
            colDesc.CopyTo(Descriptions, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.Recipe_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table and the name of the columns to be displayed
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i], colDesc[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.Recipe_ColN_id;
            NextSeqType = Settings.Default.Recipe_ColN_nextSeqType;
            NextSeqId = Settings.Default.Recipe_ColN_nextSeqId;
            Name = Settings.Default.Recipe_ColN_recipeName;
            Version = Settings.Default.Recipe_ColN_version;
            Status = Settings.Default.Recipe_ColN_status;
            FinaleWeightMin = Settings.Default.Recipe_ColN_FinalWeightMin;
            FinaleWeightMax = Settings.Default.Recipe_ColN_FinalWeightMax;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }

        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the name column. This column is the name of the recipe</value>
        public int Name { get; }

        /// <value>Index of the version column. This column is the version of the recipe</value>
        public int Version { get; }

        /// <value>Index of the status column. This column is the status of the recipe (e.g. draft, production, obsolete)</value>
        public int Status { get; }

        /// <value>Index of the final weight min column. This column contains the minimum acceptable final weight of the product</value>
        public int FinaleWeightMin { get; }

        /// <value>Index of the final weight max column. This column contains the maximum acceptable final weight of the product</value>
        public int FinaleWeightMax { get; }
    }

    /// <summary>
    /// Class containing the infomration of the recipe weight database table. The table must contain at least the following colummns: 
    /// id, 
    /// nextSeqType, 
    /// nextSeqId, 
    /// seqName, 
    /// isBarcodeUsed, 
    /// barcode, 
    /// unit, 
    /// decimalNumber, 
    /// setpoint, 
    /// min, 
    /// max
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>This table contains the required information to perform cycle weight sequences</remarks>
    public class RecipeWeightInfo : ISeqTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public RecipeWeightInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.RecipeWeight_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.RecipeWeight_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i]));

            // Import the sequential type of this class from the settings
            SeqType = Settings.Default.RecipeWeight_seqType;

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.RecipeWeight_ColN_id;
            NextSeqType = Settings.Default.RecipeWeight_ColN_nextSeqType;
            NextSeqId = Settings.Default.RecipeWeight_ColN_nextSeqId;
            Name = Settings.Default.RecipeWeight_ColN_seqName;
            IsBarcodeUsed = Settings.Default.RecipeWeight_ColN_isBarcodeUsed;
            Barcode = Settings.Default.RecipeWeight_ColN_barcode;
            Unit = Settings.Default.RecipeWeight_ColN_unit;
            DecimalNumber = Settings.Default.RecipeWeight_ColN_decimalNumber;
            Setpoint = Settings.Default.RecipeWeight_ColN_setpoint;
            Criteria = Settings.Default.RecipeWeight_ColN_criteria;
            IsSolvent = Settings.Default.RecipeWeight_ColN_isSolvent;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }
        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the name column. This column is the name of the product to be weighted</value>
        public int Name { get; }

        /// <value>Index of the is barcode column. This column informs if the barcode of the product needs to be controlled during the cycle sequence</value>
        public int IsBarcodeUsed { get; }

        /// <value>Index of the barcode column. This column contains the value of the barcode to be controlled</value>
        public int Barcode { get; }

        /// <value>Index of the unit column. This column contains the unit of the setpoint, min and max</value>
        public int Unit { get; }

        /// <value>Index of the decimal number column. This column contains the number of decimal places to be displays for the setpoint, min, max and value of the weight during the cycle sequence</value>
        public int DecimalNumber { get; }

        /// <value>Index of the setpoings column. This column contains the target weight by unit of final product</value>
        public int Setpoint { get; }

        /// <value>Index of the min column. This column contains the minimum acceptable weight by unit of final product</value>
        public int Criteria { get; }

        /// <value>Index of the is solvent column. This column if the product is a solvent (if it must be evaporated at the end of the cycle)</value>
        public int IsSolvent { get; }
    }

    /// <summary>
    /// Class containing the infomration of the recipe speedmixer database table. The table must contain at least the following colummns: 
    /// id, 
    /// nextSeqType, 
    /// nextSeqId, 
    /// seqName, 
    /// acceleration, 
    /// deceleration, 
    /// vaccum_control, 
    /// isVentgasAir, 
    /// monitorType, 
    /// pressureUnit, 
    /// scurve, 
    /// coldtrap, 
    /// speed00, 
    /// time00, 
    /// pressure00, 
    /// speed01, 
    /// time01, 
    /// pressure01, 
    /// speed02, 
    /// time02, 
    /// pressure02, 
    /// speed03, 
    /// time03, 
    /// pressure03, 
    /// speed04, 
    /// time04, 
    /// pressure04, 
    /// speed05, 
    /// time05, 
    /// pressure05, 
    /// speed06, 
    /// time06, 
    /// pressure06, 
    /// speed07, 
    /// time07, 
    /// pressure07, 
    /// speed08, 
    /// time08, 
    /// pressure08, 
    /// speed09, 
    /// time09, 
    /// pressure09, 
    /// speedMin, 
    /// speedMax, 
    /// pressureMin, 
    /// pressureMax
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>This table contains the required information to perform cycle speedmixer sequences</remarks>
    public class RecipeSpeedMixerInfo : ISeqTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public RecipeSpeedMixerInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.RecipeSpeedMixer_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.RecipeSpeedMixer_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i]));

            // Import the sequential type of this class from the settings
            SeqType = Settings.Default.RecipeSpeedMixer_seqType;

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.RecipeSpeedMixer_ColN_id;
            NextSeqType = Settings.Default.RecipeSpeedMixer_ColN_nextSeqType;
            NextSeqId = Settings.Default.RecipeSpeedMixer_ColN_nextSeqId;
            Acceleration = Settings.Default.RecipeSpeedMixer_ColN_acceleration;
            Deceleration = Settings.Default.RecipeSpeedMixer_ColN_deceleration;
            Vaccum_control = Settings.Default.RecipeSpeedMixer_ColN_vaccumControl;
            //IsVentgasAir = Settings.Default.RecipeSpeedMixer_ColN_isVentgasAir;
            //MonitorType = Settings.Default.RecipeSpeedMixer_ColN_monitorType;
            PressureUnit = Settings.Default.RecipeSpeedMixer_ColN_pressureUnit;
            Coldtrap = Settings.Default.RecipeSpeedMixer_ColN_coldtrap;
            Speed00 = Settings.Default.RecipeSpeedMixer_ColN_speed00;
            Time00 = Settings.Default.RecipeSpeedMixer_ColN_time00;
            Pressure00 = Settings.Default.RecipeSpeedMixer_ColN_pressure00;
            SpeedMin = Settings.Default.RecipeSpeedMixer_ColN_speedMin;
            SpeedMax = Settings.Default.RecipeSpeedMixer_ColN_speedMax;
            PressureMin = Settings.Default.RecipeSpeedMixer_ColN_pressureMin;
            PressureMax = Settings.Default.RecipeSpeedMixer_ColN_pressureMax;

            // Import the allowed pressure units to be put in the database table
            PUnit_Torr = Settings.Default.RecipeSpeedMixer_PressureUnit_Torr;
            PUnit_mBar = Settings.Default.RecipeSpeedMixer_PressureUnit_mBar;
            PUnit_inHg = Settings.Default.RecipeSpeedMixer_PressureUnit_inHg;
            PUnit_PSIA = Settings.Default.RecipeSpeedMixer_PressureUnit_PSIA;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        ////public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }
        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Acceleration { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Deceleration { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Vaccum_control { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int PressureUnit { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Coldtrap { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Speed00 { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Time00 { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int Pressure00 { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int SpeedMin { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int SpeedMax { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int PressureMin { get; }

        /// <value>Index of the name column. This column contains of the speedmixer sequence</value>
        public int PressureMax { get; }

        /// <value>The value of the pressure unit Torr to be put in the database table</value>
        public string PUnit_Torr { get; }

        /// <value>The value of the pressure unit mBar to be put in the database table</value>
        public string PUnit_mBar { get; }

        /// <value>The value of the pressure unit inHg to be put in the database table</value>
        public string PUnit_inHg { get; }

        /// <value>The value of the pressure unit PSIA to be put in the database table</value>
        public string PUnit_PSIA { get; }
    }

    /// <summary>
    /// Class containing the information of the cycle database table. The table must contain at least the following colummns: 
    /// id, 
    /// nextSeqType, 
    /// nextSeqId, 
    /// jobNumber, 
    /// batchNumber, 
    /// quantityValue, 
    /// quantityUnit, 
    /// itemNumber, 
    /// recipeName, 
    /// recipeVersion, 
    /// equipmentName, 
    /// dateTimeStartCycle, 
    /// dateTimeEndCycle, 
    /// username, 
    /// firstAlarmId, 
    /// lastAlarmId, 
    /// comment, 
    /// isItATest
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>The information related to a cycle is separated in different rows of sequencetial tables based on ISeqTabInfo interface. The database table related to this class is the first sequence of the cycle information</remarks>
    public class CycleTableInfo : ISeqTabInfo, IDtTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public CycleTableInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.Cycle_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.Cycle_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.Cycle_ColN_id;
            NextSeqType = Settings.Default.Cycle_ColN_nextSeqType;
            NextSeqId = Settings.Default.Cycle_ColN_nextSeqId;
            JobNumber = Settings.Default.Cycle_ColN_jobNumber;
            BatchNumber = Settings.Default.Cycle_ColN_batchNumber;
            FinalWeight = Settings.Default.Cycle_ColN_quantityValue;
            FinalWeightUnit = Settings.Default.Cycle_ColN_quantityUnit;
            ItemNumber = Settings.Default.Cycle_ColN_itemNumber;
            RecipeName = Settings.Default.Cycle_ColN_recipeName;
            RecipeVersion = Settings.Default.Cycle_ColN_recipeVersion;
            EquipmentName = Settings.Default.Cycle_ColN_equipmentName;
            DateTimeStartCycle = Settings.Default.Cycle_ColN_dateTimeStartCycle;
            DateTimeEndCycle = Settings.Default.Cycle_ColN_dateTimeEndCycle;
            Username = Settings.Default.Cycle_ColN_username;
            FirstAlarmId = Settings.Default.Cycle_ColN_firstAlarmId;
            LastAlarmId = Settings.Default.Cycle_ColN_lastAlarmId;
            Comment = Settings.Default.Cycle_ColN_comment;
            IsItATest = Settings.Default.Cycle_ColN_isItATest;
            bowlWeight = Settings.Default.Cycle_ColN_bowlWeight;
            lastWeightTh = Settings.Default.Cycle_ColN_lastWeightTh;
            lastWeightEff = Settings.Default.Cycle_ColN_lastWeightEff;

            DateTime = DateTimeStartCycle;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }
        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the job number column. This column contains the job number of the cycle</value>
        public int JobNumber { get; }

        /// <value>Index of the batch number column. This column contains the batch number of the cycle</value>
        public int BatchNumber { get; }

        /// <value>Index of the final weight column. This column contains wight of the final product at the end of the cycle</value>
        public int FinalWeight { get; }

        /// <value>Index of the final weight unit column. This column contains unit of the final weight</value>
        public int FinalWeightUnit { get; }

        /// <value>Index of the item number column. This column contains the item number of the cycle</value>
        public int ItemNumber { get; }

        /// <value>Index of the recipe name column. This column contains the name of the recipe executed during the cycle</value>
        public int RecipeName { get; }

        /// <value>Index of the recipe version column. This column contains the version of the executed recipe</value>
        public int RecipeVersion { get; }

        /// <value>Index of the equipment name column. This column contains the name of the equipment which performed the cycle</value>
        public int EquipmentName { get; }

        /// <value>Index of the start date and time column. This column contains date and time of the start of the cycle</value>
        public int DateTimeStartCycle { get; }

        /// <value>Index of the end date and time column. This column contains date and time of the end of the cycle</value>
        public int DateTimeEndCycle { get; }

        /// <value>Index of the username column. This column contains name of the user who started the cycle</value>
        public int Username { get; }

        /// <value>Index of the first alarm id column. This column contains value of the id column of the audit trail for the fist active alarm or the last audit trail event</value>
        public int FirstAlarmId { get; }

        /// <value>Index of the last alarm id column. This column contains value of the id column of the audit trail for the last audit trail event</value>
        public int LastAlarmId { get; }

        /// <value>Index of the comment column. This column contains the comment logged during the test</value>
        public int Comment { get; }

        /// <value>Index of the is a test column. This column informs if the cycle was executed during production circumpstance or not</value>
        public int IsItATest { get; }

        /// <value>Index of the bowl weight column. This column contains the weight of the empty bowl</value>
        public int bowlWeight { get; }

        /// <value>Index of the theoritical last weight column. This column contains the expected weight the empty bowl and product at the end of the mix</value>
        public int lastWeightTh { get; }

        /// <value>Index of the actual last weighcolumn. This column contains the actual weight the empty bowl and product at the end of the mix</value>
        public int lastWeightEff { get; }

        /// <value>Index of the date and time column. From IDtTabInfo</value>
        public int DateTime { get; }
    }

    /// <summary>
    /// Class containing the infomration of the cycle weight sequence database table. The table must contain at least the following colummns: 
    /// id, 
    /// nextSeqType, 
    /// nextSeqId, 
    /// product, 
    /// wasWeightManual, 
    /// dateTime, 
    /// actualValue, 
    /// setpoint, 
    /// min, 
    /// max, 
    /// unit, 
    /// decimalNumber
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>This table contains the information of the cycle weight sequences</remarks>
    public class CycleWeightInfo : ICycleSeqInfo, IDtTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public CycleWeightInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.CycleWeight_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the list of names to be displayed of the columns from the settings
            StringCollection colDesc = Settings.Default.CycleWeight_ColDesc;
            Descriptions = new string[colDesc.Count];
            colDesc.CopyTo(Descriptions, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.CycleWeight_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table and the name of the columns to be displayed
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i], colDesc[i]));

            // Import the sequential type of this class from the settings
            SeqType = Settings.Default.CycleWeight_seqType;

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.CycleWeight_ColN_id;
            NextSeqType = Settings.Default.CycleWeight_ColN_nextSeqType;
            NextSeqId = Settings.Default.CycleWeight_ColN_nextSeqId;
            Product = Settings.Default.CycleWeight_ColN_product;
            WasWeightManual = Settings.Default.CycleWeight_ColN_wasWeightManual;
            DateTime = Settings.Default.CycleWeight_ColN_dateTime;
            ActualValue = Settings.Default.CycleWeight_ColN_actualValue;
            Setpoint = Settings.Default.CycleWeight_ColN_setpoint;
            Min = Settings.Default.CycleWeight_ColN_min;
            Max = Settings.Default.CycleWeight_ColN_max;
            Unit = Settings.Default.CycleWeight_ColN_unit;
            DecimalNumber = Settings.Default.CycleWeight_ColN_decimalNumber;
            IsSolvent = Settings.Default.CycleWeight_ColN_isSolvent;
        }

        // Declaration of the logger to log errors
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }
        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }
        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the product column. This column contains the weighted product (from the name column of the weight recipe)</value>
        public int Product { get; }

        /// <value>Index of the was weight manual column. This column informs if the user entered the weighedt value manually or not</value>
        public int WasWeightManual { get; }

        /// <value>Index of the date and time column. This column contains the date and time of the weighting. From IDtTabInfo</value>
        public int DateTime { get; }

        /// <value>Index of the weighted value column. This column contains weighted value of the product</value>
        public int ActualValue { get; }

        /// <value>Index of the setpoint column. This column contains the target weight (equals basically the setpoint from the weight recipe multiplied by the final weight)</value>
        public int Setpoint { get; }

        /// <value>Index of the min column. This column contains the minimum weight (equals basically the minimum from the weight recipe multiplied by the final weight)</value>
        public int Min { get; }

        /// <value>Index of the max column. This column contains the maximum weight (equals basically the maximum from the weight recipe multiplied by the final weight)</value>
        public int Max { get; }

        /// <value>Index of the unit column. This column contains the unit of the weight</value>
        public int Unit { get; }

        /// <value>Index of the decimal number column. This column contains number of decimal places to be displayed (from the weight recipe)</value>
        public int DecimalNumber { get; }

        /// <value>Index of the is solvent column. This column informs if the product is a solvent (from the weight recipe)</value>
        public int IsSolvent { get; }

        /// <summary>
        /// Method which returns the recipe information related to the applicable recipe table 
        /// </summary>
        /// <param name="recipe">Variable containing the recipe information</param>
        /// <param name="idCycle">The value of the id column (see Id from IComTabInfo) of the row of the first cycle sequential table</param>
        public object[] GetRecipeParameters(object[] recipe, int idCycle)
        {
            object[] failReturn = null;
            object[] returnValues = new object[Ids.Length];

            // If the recipe in parameter is not a weight recipe then an error message is displayed / logged and the method is stopped
            if (recipe.Length != (new RecipeWeightInfo()).Ids.Length)
            {
                logger.Error(Settings.Default.ICycleSeqInfo_Error_RecipeIncorrect + ": " + recipe.GetType().ToString());
                MyMessageBox.Show(Settings.Default.ICycleSeqInfo_Error_RecipeIncorrect + ": " + recipe.GetType().ToString());
                return failReturn;
            }

            // Declaration of a weight recipe variable
            RecipeWeightInfo recipeWeighInfo = new RecipeWeightInfo();
            // Declaration of a cycle variable containing the values of the row whose id is the id parameter
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleTableValues = (object[])MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new CycleTableInfo(), idCycle); }).Result;
            // If the cycle variable is null, an error message is logged and the method is stopped
            if (cycleTableValues == null)
            {
                logger.Error(Settings.Default.Error_FromHere);
                return failReturn;
            }

            decimal convRatio = GetConvRatio(recipe); // Declaration of the conversion ratio variable

            // If the conversion ratio is incorrect (if the units of the final weight or of the recipe values weren't configured in the settings)
            // Then an error message is displayed / logged and the method is stopped
            if (convRatio == 0)
            {
                logger.Error(Settings.Default.ICycleSeqInfo_Error_convRatioIncorrect);
                MyMessageBox.Show(Settings.Default.ICycleSeqInfo_Error_convRatioIncorrect);
                return failReturn;
            }

            // The program tries...
            try
            {
                decimal setpoint = decimal.Parse(recipe[recipeWeighInfo.Setpoint].ToString());
                decimal criteria = decimal.Parse(recipe[recipeWeighInfo.Criteria].ToString());
                decimal finalWeight = decimal.Parse(cycleTableValues[cycleTableInfo.FinalWeight].ToString());
                //string decimalNumber = recipe[recipeWeighInfo.DecimalNumber];

                // Set of the values of the columns product, setpoint, min, max, unit and decimal number on the current weight cycle object from the recipe parameter
                returnValues[Product] = recipe[recipeWeighInfo.Name];
                returnValues[Setpoint] = (convRatio * setpoint * finalWeight);
                returnValues[Min] = (convRatio * (setpoint - criteria) * finalWeight);
                returnValues[Max] = (convRatio * (setpoint + criteria) * finalWeight);
                returnValues[Unit] = cycleTableValues[cycleTableInfo.FinalWeightUnit];
                returnValues[DecimalNumber] = recipe[recipeWeighInfo.DecimalNumber];
                returnValues[IsSolvent] = recipe[recipeWeighInfo.IsSolvent].ToString() == Settings.Default.General_TrueValue_Read ? Settings.Default.General_TrueValue_Write : Settings.Default.General_FalseValue_Write;
            }
            // If the code above generated an error then an error message is displayed / logged
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
            }
            return returnValues;
        }

        private decimal GetConvRatio(object[] recipe)
        {
            return 1;
        }

        public decimal GetMin(object[] recipe, decimal finalWeight)
        {
            RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
            decimal convRatio = GetConvRatio(recipe);
            decimal setpoint = 0;
            decimal criteria = 0;

            try
            {
                setpoint = (decimal)(recipe[recipeWeightInfo.Setpoint]);
                criteria = (decimal)(recipe[recipeWeightInfo.Criteria]);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
            }

            return convRatio * (setpoint - criteria) * finalWeight;
        }

        public decimal GetMax(object[] recipe, decimal finalWeight)
        {
            RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
            decimal convRatio = GetConvRatio(recipe);
            decimal setpoint = 0;
            decimal criteria = 0;

            try
            {
                setpoint = (decimal)(recipe[recipeWeightInfo.Setpoint]);
                criteria = (decimal)(recipe[recipeWeightInfo.Criteria]);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
            }

            return convRatio * (setpoint + criteria) * finalWeight;
        }
    }
    /// <summary>
    /// Class containing the infomration of the cycle speedmixer sequence database table. The table must contain at least the following colummns: 
    /// id, 
    /// nextSeqType, 
    /// nextSeqId, 
    /// dateTimeStart, 
    /// dateTimeEnd, 
    /// timeMixTh, 
    /// timeMixEff, 
    /// pressureUnit, 
    /// speedMin, 
    /// speedMax, 
    /// pressureMin, 
    /// pressureMax, 
    /// speedMean, 
    /// pressureMean, 
    /// speedStd, 
    /// pressureStd
    /// <para>Creation revision: 001</para>
    /// </summary>
    /// <remarks>This table contains the information of the cycle speedmixer sequences</remarks>
    public class CycleSpeedMixerInfo : ICycleSeqInfo, IDtTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public CycleSpeedMixerInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.CycleSpeedMixer_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the list of names to be displayed of the columns from the settings
            StringCollection colDesc = Settings.Default.CycleSpeedMixer_ColDesc;
            Descriptions = new string[colDesc.Count];
            colDesc.CopyTo(Descriptions, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.CycleSpeedMixer_TableName;


            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table and the name of the columns to be displayed
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i], colDesc[i]));

            // Import the sequential type of this class from the settings
            SeqType = Settings.Default.CycleSpeedMixer_seqType;

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.CycleSpeedMixer_ColN_id;
            NextSeqType = Settings.Default.CycleSpeedMixer_ColN_nextSeqType;
            NextSeqId = Settings.Default.CycleSpeedMixer_ColN_nextSeqId;
            DateTimeStart = Settings.Default.CycleSpeedMixer_ColN_dateTimeStart;
            DateTimeEnd = Settings.Default.CycleSpeedMixer_ColN_dateTimeEnd;
            TimeSeqTh = Settings.Default.CycleSpeedMixer_ColN_timeMixTh;
            TimeSeqEff = Settings.Default.CycleSpeedMixer_ColN_timeMixEff;
            PressureUnit = Settings.Default.CycleSpeedMixer_ColN_pressureUnit;
            SpeedMin = Settings.Default.CycleSpeedMixer_ColN_speedMin;
            SpeedMax = Settings.Default.CycleSpeedMixer_ColN_speedMax;
            PressureMin = Settings.Default.CycleSpeedMixer_ColN_pressureMin;
            PressureMax = Settings.Default.CycleSpeedMixer_ColN_pressureMax;
            SpeedAvg = Settings.Default.CycleSpeedMixer_ColN_speedMean;
            PressureAvg = Settings.Default.CycleSpeedMixer_ColN_pressureMean;
            SpeedStd = Settings.Default.CycleSpeedMixer_ColN_speedStd;
            PressureStd = Settings.Default.CycleSpeedMixer_ColN_pressureStd;

            DateTime = DateTimeStart;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }

        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Identification number of the current sequential table. From ISeqTabInfo interface</value>
        public int SeqType { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }
        /// <value>Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqType { get; }

        /// <value>Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</value>
        public int NextSeqId { get; }

        /// <value>Index of the start date and time column. This column contains the date and time of the start of the sequence</value>
        public int DateTimeStart { get; }

        /// <value>Index of the end date and time column. This column contains the date and time of the end of the sequence</value>
        public int DateTimeEnd { get; }

        /// <value>Index of the theoritical time of the sequence column. This column contains theoritical time of the sequence (from the recipe)</value>
        public int TimeSeqTh { get; }

        /// <value>Index of the actual time of the sequence column. This column contains actual time of the sequence</value>
        public int TimeSeqEff { get; }

        /// <value>Index of the pressure unit column. This column contains unit of the pressure from the recipe</value>
        public int PressureUnit { get; }

        /// <value>Index of the min speed column. This column contains the minimum allowed average speed from the recipe</value>
        public int SpeedMin { get; }

        /// <value>Index of the max speed column. This column contains the maximum allowed average speed from the recipe</value>
        public int SpeedMax { get; }

        /// <value>Index of the min pressure column. This column contains the minimum allowed average pressure from the recipe</value>
        public int PressureMin { get; }

        /// <value>Index of the max pressure column. This column contains the maximum allowed average pressure from the recipe</value>
        public int PressureMax { get; }

        /// <value>Index of the average speed column. This column contains average of the speed during the sequence</value>
        public int SpeedAvg { get; }

        /// <value>Index of the average pressure column. This column contains average of the pressure during the sequence</value>
        public int PressureAvg { get; }

        /// <value>Index of the standard deviation speed column. This column contains standard deviation of the speed during the sequence</value>
        public int SpeedStd { get; }

        /// <value>Index of the standard deviation pressure column. This column contains standard deviation of the pressure during the sequence</value>
        public int PressureStd { get; }

        /// <value>Index of the date and time column. From IDtTabInfo</value>
        public int DateTime { get; }

        /// <summary>
        /// Method which returns the recipe information related to the applicable recipe table 
        /// </summary>
        /// <param name="recipeValues">Variable containing the recipe information</param>
        /// <param name="idCycle">The value of the id column (see Id from IComTabInfo) of the row of the first cycle sequential table</param>
        public object[] GetRecipeParameters(object[] recipeValues, int idCycle)
        {
            RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();
            object[] failValue = null;
            object[] returnValues = new object[Ids.Length];

            // Declaration of the logger to log errors
            NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

            // If the recipe in parameter is not a speedmixer recipe then an error message is displayed / logged and the method is stopped
            if (recipeValues.Length != recipeSpeedMixerInfo.Ids.Length)
            {
                logger.Error(Settings.Default.ICycleSeqInfo_Error_RecipeIncorrect + ": " + recipeValues.Length.ToString());
                MyMessageBox.Show(Settings.Default.ICycleSeqInfo_Error_RecipeIncorrect + ": " + recipeValues.Length.ToString());
                return failValue;
            }

            int i = 0;              // Initialization of counter variable for the loop below
            int timeTh_seconds = 0; // Initialization a variable to calculate the theoritical time in seconds of the speedmixer sequence (calculated in the loop below)

            // Until the counter reaches 10 or the time of the next phase from the recipe parameter is empty...
            while (i != 10 && recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i] != null && recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() != "")
            {
                //logger.Error((i != 10 && (recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i] != null || recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() != "")).ToString());
                //logger.Error((i != 10).ToString());
                //logger.Error((recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i] != null &&  recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() != "").ToString());
                //logger.Error((recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i] != null).ToString());
                //logger.Error((recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() != "").ToString());

                // The program tries...
                try
                {
                    //logger.Fatal((recipeSpeedMixerInfo.Time00 + 3 * i).ToString() + " - " + recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() + (recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i] == null).ToString() + (recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString() == "").ToString());
                    // Theoritical time = current value + the time of the next phase from the recipe parameter
                    timeTh_seconds += int.Parse(recipeValues[recipeSpeedMixerInfo.Time00 + 3 * i].ToString());
                    // Incrementation of the counter
                    i++;
                }
                // If the code above generated an error then an error message is displayed / logged
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    MyMessageBox.Show(ex.Message);
                    return failValue;
                }
            }

            // The program tries...
            try
            {
                // Set of the values of the columns product, setpoint, min, max, unit and decimal number on the current weight cycle object from the recipe parameter
                //returnValues[Name] = recipeValues[recipeSpeedMixerInfo.Name];
                returnValues[TimeSeqTh] = TimeSpan.FromSeconds(timeTh_seconds);
                //returnValues[TimeSeqTh] = TimeSpan.FromSeconds(timeTh_seconds).ToString();
                returnValues[PressureUnit] = recipeValues[recipeSpeedMixerInfo.PressureUnit];
                returnValues[SpeedMin] = recipeValues[recipeSpeedMixerInfo.SpeedMin];
                returnValues[SpeedMax] = recipeValues[recipeSpeedMixerInfo.SpeedMax];
                returnValues[PressureMin] = recipeValues[recipeSpeedMixerInfo.PressureMin];
                returnValues[PressureMax] = recipeValues[recipeSpeedMixerInfo.PressureMax];
            }
            // If the code above generated an error then an error message is displayed / logged
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
                returnValues = failValue;
            }

            return returnValues;
        }
    }

    /// <summary>
    /// Class containing the infomration of the temp database table generated during a speedmixer sequence. The table must contain at least the following colummns: 
    /// speed, pressure
    /// <para>Creation revision: 001</para>
    /// </summary>
    public class TempInfo : IBasTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public TempInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.Temp_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.Temp_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Speed = Settings.Default.Temp_ColN_speed;
            Pressure = Settings.Default.Temp_ColN_pressure;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }

        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Index of the speed column. This column contains speed logged during the speedmixer's sequence</value>
        public int Speed { get; }

        /// <value>Index of the pressure column. This column contains pressure logged during the speedmixer's sequence</value>
        public int Pressure { get; }
    }

    /// <summary>
    /// Class containing the infomration of the temp result database table generated at the end of a speedmixer sequence. The table must contain at least the following colummns: 
    /// speed average and standard deviation, pressure average and standard deviation
    /// <para>Creation revision: 001</para>
    /// </summary>
    public class TempResultInfo : IBasTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public TempResultInfo()
        {
            // The name of the database table is empty (not used)
            TabName = "";

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new empty column to the variable Columns.
            for (int i = 0; i < Settings.Default.TempResult_ColN; i++)
            {
                //Columns.Add(new Column());
            }

            // Import the value of the indexes of the applicable variable from the settings
            SpeedAvg = Settings.Default.TempResult_ColN_speedMean;
            PressureAvg = Settings.Default.TempResult_ColN_pressureMean;
            SpeedStd = Settings.Default.TempResult_ColN_speedStd;
            PressureStd = Settings.Default.TempResult_ColN_pressureStd;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }

        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Index of the average speed column. This column contains average speed calculated at the end of the speedmixer's sequence</value>
        public int SpeedAvg { get; }

        /// <value>Index of the average pressure column. This column contains average pressure calculated at the end of the speedmixer's sequence</value>
        public int PressureAvg { get; }

        /// <value>Index of the standard deviation speed column. This column contains average standard deviation calculated at the end of the speedmixer's sequence</value>
        public int SpeedStd { get; }

        /// <value>Index of the standard deviation pressure column. This column contains standard deviation pressure calculated at the end of the speedmixer's sequence</value>
        public int PressureStd { get; }
    }

    /// <summary>
    /// Class containing the infomration of the sample database table. The table must contain at least the following colummns: 
    /// Id (UNIQUE INTEGER)
    /// DateTime, 
    /// setpoints (from 1 to 4), 
    /// measures (from 1 to 4), 
    /// status
    /// <para>Creation revision: 001</para>
    /// </summary>
    public class DailyTestInfo : IComTabInfo, IDtTabInfo
    {
        /// <summary>
        /// Sets all the variables of the class except the values of the variable Columns
        /// </summary>
        public DailyTestInfo()
        {
            // Import the list of names of the columns of the database table from the settings
            StringCollection colId = Settings.Default.DailyTest_ColIds;
            Ids = new string[colId.Count];
            colId.CopyTo(Ids, 0);
            // Import the list of names to be displayed of the columns from the settings
            StringCollection colDesc = Settings.Default.DailyTest_ColDesc;
            Descriptions = new string[colDesc.Count];
            colDesc.CopyTo(Descriptions, 0);
            // Import the name of the database table from the settings
            TabName = Settings.Default.DailyTest_TableName;

            // Initialization of the variable Columns
            //Columns = new List<Column>();
            // For each element of the list of names of the columns, add a new column to the variable Columns.
            // This new column contains the name of the column of the databse table and the name of the columns to be displayed
            //for (int i = 0; i < colId.Count; i++) Columns.Add(new Column(colId[i], colDesc[i]));

            // Import the value of the indexes of the applicable variable from the settings
            Id = Settings.Default.DailyTest_ColN_id;
            Username = Settings.Default.DailyTest_ColN_username;
            DateTime = Settings.Default.DailyTest_ColN_dateTime;
            EquipmentName = Settings.Default.DailyTest_ColN_equipmentName;
            Setpoint1 = Settings.Default.DailyTest_ColN_setpoint1;
            Setpoint2 = Settings.Default.DailyTest_ColN_setpoint1 + 1;
            Setpoint3 = Settings.Default.DailyTest_ColN_setpoint1 + 2;
            Setpoint4 = Settings.Default.DailyTest_ColN_setpoint1 + 3;
            Measure1 = Settings.Default.DailyTest_ColN_measure1;
            Measure2 = Settings.Default.DailyTest_ColN_measure1 + 1;
            Measure3 = Settings.Default.DailyTest_ColN_measure1 + 2;
            Measure4 = Settings.Default.DailyTest_ColN_measure1 + 3;
            Id1 = Settings.Default.DailyTest_ColN_id1;
            Id2 = Settings.Default.DailyTest_ColN_id1 + 1;
            Id3 = Settings.Default.DailyTest_ColN_id1 + 2;
            Id4 = Settings.Default.DailyTest_ColN_id1 + 3;
            Status = Settings.Default.DailyTest_ColN_status;

            // Set of the number of samples possible
            SamplesNumber = 4;
        }

        /// <value>Name of the database table. From IBasTabInfo interface</value>
        public string TabName { get; }

        /// <value>Columns of the database table. From IBasTabInfo interface</value>
        //public List<Column> Columns { get; set; }

        /// <value>Name of the columns of the database table. From IBasTabInfo interface</value>
        public string[] Ids { get; }

        /// <value>Description of the columns of the database table</value>
        public string[] Descriptions { get; }

        /// <value>Index of the id column (usually the first one: 0). This column <c>must be</c> an integer, usually automatically incremented. From IComTabInfo interface</value>
        public int Id { get; }

        /// <value>Index of the usernmae column. This column contains the name of the user who performed the sampling</value>
        public int Username { get; }

        /// <value>Index of the date and time column. This column contains the date and time of the insertion the rows. From IDtTabInfo</value>
        public int DateTime { get; }

        /// <value>Index of the equipment name column. This column contains the name of the equipment used for the sampling</value>
        public int EquipmentName { get; }

        /// <value>Index of the setpoint 1 column. This column contains the value of the first weight sample</value>
        public int Setpoint1 { get; }

        /// <value>Index of the setpoint 2 column. This column contains the value of the second weight sample</value>
        public int Setpoint2 { get; }

        /// <value>Index of the setpoint 3 column. This column contains the value of the third weight sample</value>
        public int Setpoint3 { get; }

        /// <value>Index of the setpoint 4 column. This column contains the value of the fourth weight sample</value>
        public int Setpoint4 { get; }

        /// <value>Index of the measure 1 column. This column contains the measure weight of the setpoint 1</value>
        public int Measure1 { get; }

        /// <value>Index of the measure 2 column. This column contains the measure weight of the setpoint 2</value>
        public int Measure2 { get; }

        /// <value>Index of the measure 3 column. This column contains the measure weight of the setpoint 3</value>
        public int Measure3 { get; }

        /// <value>Index of the measure 4 column. This column contains the measure weight of the setpoint 4</value>
        public int Measure4 { get; }

        /// <value>Index of the ID of the weight 1 column. This column contains the Id of the weight 1</value>
        public int Id1 { get; }

        /// <value>Index of the ID of the weight 2 column. This column contains the Id of the weight 2</value>
        public int Id2 { get; }

        /// <value>Index of the ID of the weight 3 column. This column contains the Id of the weight 3</value>
        public int Id3 { get; }

        /// <value>Index of the ID of the weight 4 column. This column contains the Id of the weight 4</value>
        public int Id4 { get; }

        /// <value>Index of the status column. This column contains the status of the sample</value>
        public int Status { get; }

        /// <value>Index of the sample number column. This column contains the number of samples which can be measured</value>
        public int SamplesNumber { get; }
    }
}
