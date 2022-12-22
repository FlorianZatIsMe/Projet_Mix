﻿using System;
using System.Windows;
using System.Collections.Generic;
using Database;
using System.Configuration;
using Alarm_Management.Properties;
using System.Threading.Tasks;

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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Future interface
        private static string AuditTrail_SystemUsername;

        public static event Action ActiveAlarmEvent = null;
        public static event Action InactiveAlarmEvent = null;

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
        /// <param name="info"></param>
        public static void Initialize(IniInfo info)
        {
            logger.Debug("Initialize");

            AuditTrail_SystemUsername = info.AuditTrail_SystemUsername;

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
                MessageBox.Show(Settings.Default.Error01);
            }

            int n = -1;
            int mutexID;
            bool isAlarmNotActive = Alarms[id1, id2].Status != AlarmStatus.ACTIVE && 
                                    Alarms[id1, id2].Status != AlarmStatus.ACK;

            // Si l'alarme est active, le programmeur est nul
            if (!isAlarmNotActive) {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                return;
            }

            mutexID = -1;
            //mutexID = MyDatabase.Connect(false);

            // Add test if database not connected, and do that everywhere

            AlarmStatus statusBefore = Alarms[id1, id2].Status;
            AlarmStatus statusAfter = AlarmStatus.ACTIVE;


            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.Columns[auditTrailInfo.Username].Value = AuditTrail_SystemUsername;
            auditTrailInfo.Columns[auditTrailInfo.EventType].Value = GetAlarmType(Alarms[id1, id2].Type);
            auditTrailInfo.Columns[auditTrailInfo.Description].Value = GetAlarmDescription(id1, id2);
            auditTrailInfo.Columns[auditTrailInfo.ValueBefore].Value = statusBefore.ToString();
            auditTrailInfo.Columns[auditTrailInfo.ValueAfter].Value = statusAfter.ToString();

            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });
            //MyDatabase.InsertRow(auditTrailInfo, mutexID);


            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            Alarms[id1, id2].id = (int)t.Result;
            //Alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
            Alarms[id1, id2].Status = statusAfter;
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
                    MessageBox.Show(Settings.Default.Error03);
                }
            }

            MessageBox.Show(GetAlarmDescription(id1, id2)); // Peut-être afficher la liste des alarmes actives à la place

            //MyDatabase.Disconnect(mutex: mutexID);
        }
        public static void InactivateAlarm(int id1, int id2)
        {
            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
            }

            int n = -1;
            int mutexID;

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
                MessageBox.Show(Settings.Default.Error04);
                return;
            }

            if (Alarms[id1, id2].Status == AlarmStatus.INACTIVE) {
                logger.Error(Settings.Default.Error05);
                MessageBox.Show(Settings.Default.Error05);
                return;
            }

            mutexID = -1;
            //mutexID = MyDatabase.Connect(false);

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
                    MessageBox.Show(Settings.Default.Error06);
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
                MessageBox.Show(Settings.Default.Error07);
                return;
            }

            if (statusAfter != AlarmStatus.None)
            {
                AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                auditTrailInfo.Columns[auditTrailInfo.Username].Value = AuditTrail_SystemUsername;
                auditTrailInfo.Columns[auditTrailInfo.EventType].Value = GetAlarmType(Alarms[id1, id2].Type);
                auditTrailInfo.Columns[auditTrailInfo.Description].Value = GetAlarmDescription(id1, id2);
                auditTrailInfo.Columns[auditTrailInfo.ValueBefore].Value = statusBefore.ToString();
                auditTrailInfo.Columns[auditTrailInfo.ValueAfter].Value = statusAfter.ToString();
                MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });
                //MyDatabase.InsertRow(auditTrailInfo, mutexID);


                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
                Alarms[id1, id2].id = (int)t.Result;
                //Alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
                Alarms[id1, id2].Status = statusAfter;

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

            //MyDatabase.Disconnect(mutex: mutexID);
        }
        public static void AcknowledgeAlarm(int id1, int id2)
        {
            logger.Debug("AcknowledgeAlarm");

            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
            }

            int n = -1;
            int mutexID;

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
                MessageBox.Show(Settings.Default.Error04);
                return;
            }

            AlarmStatus statusBefore = Alarms[id1, id2].Status;
            AlarmStatus statusAfter = (statusBefore == AlarmStatus.ACTIVE) ? AlarmStatus.ACK : (statusBefore == AlarmStatus.INACTIVE ? AlarmStatus.RAZACK : statusBefore);

            if (statusBefore == statusAfter) {
                logger.Error(Settings.Default.Error06);
                MessageBox.Show(Settings.Default.Error06);
                return;
            }

            //mutexID = MyDatabase.Connect(false);
            mutexID = -1;

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.Columns[auditTrailInfo.Username].Value = AuditTrail_SystemUsername;
            auditTrailInfo.Columns[auditTrailInfo.EventType].Value = GetAlarmType(Alarms[id1, id2].Type);
            auditTrailInfo.Columns[auditTrailInfo.Description].Value = GetAlarmDescription(id1, id2);
            auditTrailInfo.Columns[auditTrailInfo.ValueBefore].Value = statusBefore.ToString();
            auditTrailInfo.Columns[auditTrailInfo.ValueAfter].Value = statusAfter.ToString();
            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });
            //MyDatabase.InsertRow(auditTrailInfo, mutexID);

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            Alarms[id1, id2].id = (int)t.Result;
            //Alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
            Alarms[id1, id2].Status = statusAfter;

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
                MessageBox.Show(Settings.Default.Error08);
            }

            ActiveAlarms.RemoveAt(n);
            if (ActiveAlarms.Count == 0) InactiveAlarmEvent();

            //MyDatabase.Disconnect(mutex: mutexID);
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
