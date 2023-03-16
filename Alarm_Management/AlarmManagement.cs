using System;
using System.Windows;
using System.Collections.Generic;
using Database;
using System.Configuration;
using Alarm_Management.Properties;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Alarm_Management
{
    /// <summary>
    /// class
    /// </summary>
    public class ConfigAlarm
    {
        public int id1 { get; set; }
        public int id2 { get; set; }
        public string description { get; set; }
        public AlarmType type { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ConfigAlarms
    {
        public ConfigAlarm[] configAlarms { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public enum AlarmType
    {
        Alarm,
        Warning,
        None
    }
    /// <summary>
    /// 
    /// </summary>
    public enum AlarmStatus
    {
        RAZ,
        RAZACK,
        ACTIVE,
        INACTIVE,
        ACK,
        None
    }
    /// <summary>
    /// 
    /// </summary>
    public class Alarm
    {
        public int id;
        public readonly string Description;
        public readonly AlarmType Type;
        public AlarmStatus Status;

        public Alarm(string description, AlarmType type)
        {
            id = -1;
            Description = description;
            Type = type;
            Status = AlarmStatus.RAZ;
        }
    }
    public struct IniInfo
    {
        public string AuditTrail_SystemUsername;
        public Window Window;
    }
    public static class AlarmSettings
    {
        public static string AlarmType_Alarm { get; }
        public static string AlarmType_Warning { get; }

        static AlarmSettings()
        {
            AlarmType_Alarm = Settings.Default.AlarmType_Alarm;
            AlarmType_Warning = Settings.Default.AlarmType_Warning;
        }
    }
    public class AlarmManagement
    {
        public static List<Tuple<int, int>> ActiveAlarms { get; }
        public static List<int> RAZalarms;
        public static Alarm[,] Alarms { get; }

        private static bool isInitialized = false;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Future interface
        //private static string AuditTrail_SystemUsername;

        public static event Action ActiveAlarmEvent = null;
        public static event Action InactiveAlarmEvent = null;

        public static IniInfo info;

        //private static Window mainWindow;

        private static void ShowMessageBox(string message)
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

        static AlarmManagement()
        {
            logger.Debug("Start");

            ConfigAlarm[] configAlarms = Settings.Default.Alarms.configAlarms;
            ActiveAlarms = new List<Tuple<int, int>>();
            RAZalarms = new List<int>();
            Alarms = new Alarm[5, 2];

            for (int i = 0; i < configAlarms.Length; i++)
            {
                Alarms[configAlarms[i].id1, configAlarms[i].id2] = new Alarm(configAlarms[i].description, configAlarms[i].type);
            }
            /*
            alarms[0, 0] = new Alarm("Connexion à la balance échouée", AlarmType.Alarm);
            alarms[1, 0] = new Alarm("Connexion au SpeedMixer échouée", AlarmType.Alarm);
            alarms[1, 1] = new Alarm("Erreur du speedmixer pendant un cycle", AlarmType.Alarm);
            alarms[2, 0] = new Alarm("Connexion à la pompe à vide échouée", AlarmType.Alarm);
            alarms[3, 0] = new Alarm("Connexion au piège froid échouée", AlarmType.Alarm);
            alarms[3, 1] = new Alarm("Température trop haute pendant le cycle", AlarmType.Alarm);
            alarms[4, 0] = new Alarm("Backup automatique complet de la base de données échoué aprés 3 tentatives", AlarmType.Warning);
            */
        }
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="info_arg"></param>
        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");

            info = info_arg;
            //AuditTrail_SystemUsername = info_arg.AuditTrail_SystemUsername;
            //mainWindow = info_arg.window;

            MyDatabase.Initialize(new Database.IniInfo() { 
                AlarmType_Alarm = Settings.Default.AlarmType_Alarm,
                AlarmType_Warning = Settings.Default.AlarmType_Warning });

            isInitialized = true;
        }
        public static void NewAlarm(int id1, int id2)
        {
            if(ActiveAlarms.Count == 0) ActiveAlarmEvent();
            logger.Debug("NewAlarm");

            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                ShowMessageBox(Settings.Default.Error01);
            }

            int n = -1;
            bool isAlarmNotActive = Alarms[id1, id2].Status != AlarmStatus.ACTIVE && 
                                    Alarms[id1, id2].Status != AlarmStatus.ACK;

            // Si l'alarme est active, le programmeur est nul
            if (!isAlarmNotActive) {
                logger.Error(Settings.Default.Error02);
                ShowMessageBox(Settings.Default.Error02);
                return;
            }

            // Add test if database not connected, and do that everywhere

            AlarmStatus statusBefore = Alarms[id1, id2].Status;
            AlarmStatus statusAfter = AlarmStatus.ACTIVE;

            UpdateAlarm(id1, id2, statusBefore, statusAfter);

            ActiveAlarms.Add(new Tuple<int, int>(id1, id2));

            if (statusBefore == AlarmStatus.INACTIVE)
            {
                for (int i = 0; i < ActiveAlarms.Count; i++)
                {
                    if (ActiveAlarms[i].Item1 == id1 && ActiveAlarms[i].Item2 == id2)
                    {
                        n = i;
                        break;
                    }
                }

                if (n != -1) ActiveAlarms.RemoveAt(n);
                else {
                    logger.Error(Settings.Default.Error03);
                    ShowMessageBox(Settings.Default.Error03);
                }
            }

            ShowMessageBox(GetAlarmDescription(id1, id2)); // Peut-être afficher la liste des alarmes actives à la place

            //MyDatabase.Disconnect(mutex: mutexID);
        }
        public static void InactivateAlarm(int id1, int id2)
        {
            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                ShowMessageBox(Settings.Default.Error01);
            }

            int n = -1;

            for (int i = 0; i < ActiveAlarms.Count; i++)
            {
                if (ActiveAlarms[i].Item1 == id1 && ActiveAlarms[i].Item2 == id2)
                {
                    n = i;
                    break;
                }
            }

            if (n == -1) {
                logger.Error(Settings.Default.Error04);
                ShowMessageBox(Settings.Default.Error04);
                return;
            }

            if (Alarms[id1, id2].Status == AlarmStatus.INACTIVE) {
                logger.Error(Settings.Default.Error05);
                ShowMessageBox(Settings.Default.Error05);
                return;
            }

            AlarmStatus statusBefore = Alarms[id1, id2].Status;
            AlarmStatus statusAfter = AlarmStatus.None;

            if (Alarms[id1, id2].Type == AlarmType.Alarm)
            {
                if (statusBefore == AlarmStatus.ACTIVE)
                {
                    statusAfter = AlarmStatus.INACTIVE;
                }
                else if (statusBefore == AlarmStatus.ACK)
                {
                    statusAfter = AlarmStatus.RAZ;
                }
                else
                {
                    logger.Error(Settings.Default.Error06);
                    ShowMessageBox(Settings.Default.Error06);
                    return;
                }
            }
            else if (Alarms[id1, id2].Type == AlarmType.Warning)
            {
                statusAfter = AlarmStatus.RAZ;
            }
            else
            {
                logger.Error(Settings.Default.Error07);
                ShowMessageBox(Settings.Default.Error07);
                return;
            }

            if (statusAfter != AlarmStatus.None)
            {
                UpdateAlarm(id1, id2, statusBefore, statusAfter);

                if (statusAfter == AlarmStatus.INACTIVE)
                {
                    ActiveAlarms.Add(new Tuple<int, int>(id1, id2));
                }
                else if (statusAfter == AlarmStatus.RAZ)
                {
                    RAZalarms.Add(Alarms[id1, id2].id);
                }

                ActiveAlarms.RemoveAt(n);
                if (ActiveAlarms.Count == 0) InactiveAlarmEvent();
            }
        }
        public static void AcknowledgeAlarm(int id1, int id2)
        {
            logger.Debug("AcknowledgeAlarm");

            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                ShowMessageBox(Settings.Default.Error01);
            }

            int n = -1;

            for (int i = 0; i < ActiveAlarms.Count; i++)
            {
                if (ActiveAlarms[i].Item1 == id1 && ActiveAlarms[i].Item2 == id2)
                {
                    n = i;
                    break;
                }
            }

            if (n == -1) {
                logger.Error(Settings.Default.Error04);
                ShowMessageBox(Settings.Default.Error04);
                return;
            }

            AlarmStatus statusBefore = Alarms[id1, id2].Status;
            AlarmStatus statusAfter = (statusBefore == AlarmStatus.ACTIVE) ? AlarmStatus.ACK : (statusBefore == AlarmStatus.INACTIVE ? AlarmStatus.RAZACK : statusBefore);

            if (statusBefore == statusAfter) {
                return;
            }

            UpdateAlarm(id1, id2, statusBefore, statusAfter);

            if (statusAfter == AlarmStatus.ACK)
            {
                ActiveAlarms.Add(new Tuple<int, int>(id1, id2));
            }
            else if (statusAfter == AlarmStatus.RAZACK)
            {
                RAZalarms.Add(Alarms[id1, id2].id);
            }
            else
            {
                logger.Error(Settings.Default.Error08);
                ShowMessageBox(Settings.Default.Error08);
            }

            ActiveAlarms.RemoveAt(n);
            if (ActiveAlarms.Count == 0) InactiveAlarmEvent();
        }
        private static void UpdateAlarm(int id1, int id2, AlarmStatus statusBefore, AlarmStatus statusAfter)
        {
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.Columns[auditTrailInfo.Username].Value = info.AuditTrail_SystemUsername;
            auditTrailInfo.Columns[auditTrailInfo.EventType].Value = GetAlarmType(Alarms[id1, id2].Type);
            auditTrailInfo.Columns[auditTrailInfo.Description].Value = GetAlarmDescription(id1, id2);
            auditTrailInfo.Columns[auditTrailInfo.ValueBefore].Value = statusBefore.ToString();
            auditTrailInfo.Columns[auditTrailInfo.ValueAfter].Value = statusAfter.ToString();

            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            Alarms[id1, id2].id = (int)t.Result;
            Alarms[id1, id2].Status = statusAfter;
        }
        public static void UpdateAlarms()
        {
            List<Tuple<int, int>> listId = new List<Tuple<int, int>>();
            foreach (Tuple<int, int> id in AlarmManagement.ActiveAlarms)
            {
                listId.Add(id);
            }

            foreach (Tuple<int, int> id in listId)
            {
                UpdateAlarm(id.Item1, id.Item2, AlarmStatus.RAZ, Alarms[id.Item1, id.Item2].Status);
                ShowMessageBox(GetAlarmDescription(id.Item1, id.Item2));
            }
        }
        public static string GetAlarmType(AlarmType type)
        {
            logger.Debug("GetAlarmType");

            switch (type)
            {
                case AlarmType.Alarm:
                    return Settings.Default.AlarmType_Alarm;
                case AlarmType.Warning:
                    return Settings.Default.AlarmType_Warning;
                case AlarmType.None:
                default:
                    return Settings.Default.AlarmType_None;
            }
        }
        public static string GetAlarmDescription(int id1, int id2)
        {
            logger.Debug("GetAlarmDescription");

            return GetAlarmType(Alarms[id1, id2].Type).ToUpper() + " " + id1.ToString("00") + "." + id2.ToString("00") + " " + Alarms[id1, id2].Description;
        }
    }
}
