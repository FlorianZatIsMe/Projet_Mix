using System;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections.Generic;
using Database;
using System.Configuration;
using System.ComponentModel;
using System.Collections;
using Alarm_Management.Properties;
using System.Threading.Tasks;

namespace Alarm_Management
{
    //
    //  Classes utilisées en lien avec le fichier de configuration pour détailler la liste d'alarmes
    //
    public class ConfigAlarm
    {
        public int id1 { get; set; }
        public int id2 { get; set; }
        public string description { get; set; }
        public AlarmType type { get; set; }
    }

    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ConfigAlarms
    {
        public ConfigAlarm[] configAlarms { get; set; }
    }

    public enum AlarmType
    {
        Alarm,
        Warning,
        None
    }
    public enum AlarmStatus
    {
        RAZ,
        RAZACK,
        ACTIVE,
        INACTIVE,
        ACK,
        None
    }
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

    public class AlarmManagement
    {
        public static List<Tuple<int, int>> ActiveAlarms { get; }
        public static List<int> RAZalarms;
        public static Alarm[,] alarms { get; }
        //private static MyDatabase db = new MyDatabase();

        private static bool isInitialized = false;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

        // Future interface
        private static string AuditTrail_SystemUsername;

        static AlarmManagement()
        {
            ConfigAlarm[] configAlarms = Settings.Default.Alarms.configAlarms;
            ActiveAlarms = new List<Tuple<int, int>>();
            RAZalarms = new List<int>();
            alarms = new Alarm[5, 2];

            for (int i = 0; i < configAlarms.Length; i++)
            {
                alarms[configAlarms[i].id1, configAlarms[i].id2] = new Alarm(configAlarms[i].description, configAlarms[i].type);
                //MessageBox.Show(Settings.Default.Alarms.configAlarms[i].description.ToString());
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
        public static void Initialize(IniInfo info)
        {
            AuditTrail_SystemUsername = info.AuditTrail_SystemUsername;
            isInitialized = true;
        }
        public static void NewAlarm(int id1, int id2)
        {
            if (!isInitialized) {
                logger.Error(Settings.Default.Error01);
                MessageBox.Show(Settings.Default.Error01);
            }

            int n = -1;
            int mutexID;
            bool isAlarmNotActive = alarms[id1, id2].Status != AlarmStatus.ACTIVE && 
                                    alarms[id1, id2].Status != AlarmStatus.ACK;

            // Si l'alarme est active, le programmeur est nul
            if (!isAlarmNotActive) {
                logger.Error(Settings.Default.Error02);
                MessageBox.Show(Settings.Default.Error02);
                return;
            }

            mutexID = MyDatabase.Connect(false);

            AlarmStatus statusBefore = alarms[id1, id2].Status;
            AlarmStatus statusAfter = AlarmStatus.ACTIVE;

            auditTrailInfo.columns[auditTrailInfo.username].value = AuditTrail_SystemUsername;
            auditTrailInfo.columns[auditTrailInfo.eventType].value = GetAlarmType(alarms[id1, id2].Type);
            auditTrailInfo.columns[auditTrailInfo.description].value = GetAlarmDescription(id1, id2);
            auditTrailInfo.columns[auditTrailInfo.valueBefore].value = statusBefore.ToString();
            auditTrailInfo.columns[auditTrailInfo.valueAfter].value = statusAfter.ToString();
            MyDatabase.InsertRow(auditTrailInfo, mutexID);

            alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
            alarms[id1, id2].Status = statusAfter;
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

            MyDatabase.Disconnect(mutex: mutexID);
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

            if (alarms[id1, id2].Status == AlarmStatus.INACTIVE) {
                logger.Error(Settings.Default.Error05);
                MessageBox.Show(Settings.Default.Error05);
                return;
            }

            mutexID = MyDatabase.Connect(false);
            /*
            if (!MyDatabase.IsConnected())
            {
                mutexID = MyDatabase.Connect(false);
                wasDbConnected = false;
            }
            else
            {
                mutexID = MyDatabase.Wait();
                wasDbConnected = true;
            }*/

            //if (n != -1 && alarms[id1, id2].Status != AlarmStatus.INACTIVE)

            AlarmStatus statusBefore = alarms[id1, id2].Status;
            AlarmStatus statusAfter = AlarmStatus.None;

            if (alarms[id1, id2].Type == AlarmType.Alarm)
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
            else if (alarms[id1, id2].Type == AlarmType.Warning)
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
                auditTrailInfo.columns[auditTrailInfo.username].value = AuditTrail_SystemUsername;
                auditTrailInfo.columns[auditTrailInfo.eventType].value = GetAlarmType(alarms[id1, id2].Type);
                auditTrailInfo.columns[auditTrailInfo.description].value = GetAlarmDescription(id1, id2);
                auditTrailInfo.columns[auditTrailInfo.valueBefore].value = statusBefore.ToString();
                auditTrailInfo.columns[auditTrailInfo.valueAfter].value = statusAfter.ToString();
                MyDatabase.InsertRow(auditTrailInfo, mutexID);

                alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
                alarms[id1, id2].Status = statusAfter;

                if (statusAfter == AlarmStatus.INACTIVE)
                {
                    ActiveAlarms.Add(new Tuple<int, int>(id1, id2));
                }
                else if (statusAfter == AlarmStatus.RAZ)
                {
                    RAZalarms.Add(alarms[id1, id2].id);
                }

                ActiveAlarms.RemoveAt(n);
            }
            //if (!wasDbConnected) MyDatabase.Disconnect(mutex: mutexID);
            //else MyDatabase.Signal(mutexID);
            MyDatabase.Disconnect(mutex: mutexID);
        }
        public static void AcknowledgeAlarm(int id1, int id2)
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

            AlarmStatus statusBefore = alarms[id1, id2].Status;
            AlarmStatus statusAfter = (statusBefore == AlarmStatus.ACTIVE) ? AlarmStatus.ACK : (statusBefore == AlarmStatus.INACTIVE ? AlarmStatus.RAZACK : statusBefore);

            if (statusBefore == statusAfter) {
                logger.Error(Settings.Default.Error06);
                MessageBox.Show(Settings.Default.Error06);
                return;
            }

            mutexID = MyDatabase.Connect(false);
            /*
            bool wasDbConnected;
            if (!MyDatabase.IsConnected())
            {
                mutexID = MyDatabase.Connect(false);
                wasDbConnected = false;
            }
            else
            {
                mutexID = MyDatabase.Wait();
                wasDbConnected = true;
            }*/

            auditTrailInfo.columns[auditTrailInfo.username].value = AuditTrail_SystemUsername;
            auditTrailInfo.columns[auditTrailInfo.eventType].value = GetAlarmType(alarms[id1, id2].Type);
            auditTrailInfo.columns[auditTrailInfo.description].value = GetAlarmDescription(id1, id2);
            auditTrailInfo.columns[auditTrailInfo.valueBefore].value = statusBefore.ToString();
            auditTrailInfo.columns[auditTrailInfo.valueAfter].value = statusAfter.ToString();
            MyDatabase.InsertRow(auditTrailInfo, mutexID);
            //string[] values = new string[] { "Système", GetAlarmType(alarms[id1, id2].Type), GetAlarmDescription(id1, id2), statusBefore.ToString(), statusAfter.ToString() };
            //MyDatabase.InsertRow_done_old(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values, mutex: mutexID);

            alarms[id1, id2].id = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id, mutex: mutexID);
            alarms[id1, id2].Status = statusAfter;

            if (statusAfter == AlarmStatus.ACK)
            {
                ActiveAlarms.Add(new Tuple<int, int>(id1, id2));
            }
            else if (statusAfter == AlarmStatus.RAZACK)
            {
                RAZalarms.Add(alarms[id1, id2].id);
            }
            else
            {
                logger.Error(Settings.Default.Error08);
                MessageBox.Show(Settings.Default.Error08);
            }

            ActiveAlarms.RemoveAt(n);

            //if (!wasDbConnected) MyDatabase.Disconnect(mutex: mutexID);
            //else MyDatabase.Signal(mutexID);
            MyDatabase.Disconnect(mutex: mutexID);
        }
        public static string GetAlarmType(AlarmType type)
        {
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
            return GetAlarmType(alarms[id1, id2].Type).ToUpper() + " " + id1.ToString("00") + "." + id2.ToString("00") + " " + alarms[id1, id2].Description;
        }
    }
}
