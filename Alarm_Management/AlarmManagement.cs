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

namespace Alarm_Management
{
    public static class AlarmManagement
    {
        public static List<Alarm> activeAlarms { get; }
        public static List<Alarm> RAZalarms;
        public readonly static Alarm[,] alarms;
        private static MyDatabase db = new MyDatabase();
        private readonly static NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;

        public struct Alarm
        {
            public int id;
            public string Description;
            public AlarmType Type;
            public AlarmStatus Status;
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
            ACTIVE,
            INACTIVE,
            ACK,
            None
        }

        static AlarmManagement()
        {
            activeAlarms = new List<Alarm>();
            RAZalarms = new List<Alarm>();
            alarms = new Alarm[4, 2];

            alarms[0, 0].Description = "ALARM 00.00 - Connexion à la balance échouée";
            alarms[0, 0].Type = AlarmType.Alarm;

            alarms[1, 0].Description = "ALARM 01.00 - Connexion au SpeedMixer échouée";
            alarms[1, 0].Type = AlarmType.Alarm;

            alarms[2, 0].Description = "ALARM 02.00 - Connexion à la pompe à vide échouée";
            alarms[2, 0].Type = AlarmType.Alarm;

            alarms[3, 0].Description = "ALARM 03.00 - Connexion au piège froid échouée";
            alarms[3, 0].Type = AlarmType.Alarm;

            alarms[3, 1].Description = "ALARM 03.01 - Température trop haute pendant le cycle";
            alarms[3, 1].Type = AlarmType.Alarm;
        }
        public static void NewAlarm(Alarm alarm_arg)
        {
            int n = -1;
            Alarm alarm = alarm_arg;

            for (int i = 0; i < activeAlarms.Count; i++)
            {
                if (activeAlarms[i].Description == alarm.Description)
                {
                    n = i;
                    break;
                }
            }

            if (n == -1 || (activeAlarms[n].Status != AlarmStatus.ACTIVE && activeAlarms[n].Status != AlarmStatus.ACK))
            {
                AlarmStatus statusBefore = (n == -1) ? AlarmStatus.RAZ : activeAlarms[n].Status;
                AlarmStatus statusAfter = AlarmStatus.ACTIVE;

                string[] values = new string[] { "Système", alarm.Description, statusBefore.ToString(), statusAfter.ToString() };
                db.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                alarm.id = db.GetMax(AuditTrailSettings["Table_Name"], "c00");
                alarm.Status = statusAfter;
                activeAlarms.Add(alarm);

                if (n != -1) activeAlarms.RemoveAt(n);

                for (int i = 0; i < activeAlarms.Count(); i++)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
                }
                MessageBox.Show(alarm.Description); // Peut-être afficher la liste des alarmes actives à la place
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Ce n'est pas bien ce que vous faite Monsieur, on ne crée pas d'alarme si elle est déjà active...");
            }
        }
        public static void InactivateAlarm(Alarm alarm_arg)
        {
            int n = -1;
            Alarm alarm = alarm_arg;

            for (int i = 0; i < activeAlarms.Count; i++)
            {
                if (activeAlarms[i].Description == alarm.Description)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1 && activeAlarms[n].Status != AlarmStatus.INACTIVE)
            {
                AlarmStatus statusBefore = activeAlarms[n].Status;
                AlarmStatus statusAfter = AlarmStatus.None;// = (activeAlarms[n].Status == AlarmStatus.ACTIVE) ? AlarmStatus.INACTIVE : AlarmStatus.RAZ;

                if (alarm.Type == AlarmType.Alarm)
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
                        MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Non mais WHAT !!! T'es sérieux là ???");
                    }
                }
                else if (alarm.Type == AlarmType.Warning)
                {
                    statusAfter = AlarmStatus.RAZ;
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Roger, on a un problème, aucune alarm n'a été désactivée");
                }

                if (statusAfter != AlarmStatus.None)
                {
                    string[] values = new string[] { "Système", alarm.Description, statusBefore.ToString(), statusAfter.ToString() };
                    db.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                    alarm.id = db.GetMax(AuditTrailSettings["Table_Name"], "c00");
                    alarm.Status = statusAfter;

                    if (statusAfter == AlarmStatus.INACTIVE)
                    {
                        activeAlarms.Add(alarm);
                    }
                    else if (statusAfter == AlarmStatus.RAZ)
                    {
                        RAZalarms.Add(alarm);
                    }

                    activeAlarms.RemoveAt(n);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Tu sais pas ce que tu fais c'est pas vrai !");
            }

            for (int i = 0; i < activeAlarms.Count(); i++)
            {
                //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
            }
        }
        public static void AcknowledgeAlarm(Alarm alarm_arg)
        {
            int n = -1;
            Alarm alarm = alarm_arg;

            for (int i = 0; i < activeAlarms.Count; i++)
            {
                if (activeAlarms[i].Description == alarm.Description)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1)
            {
                AlarmStatus statusBefore = activeAlarms[n].Status;
                AlarmStatus statusAfter = (activeAlarms[n].Status == AlarmStatus.ACTIVE) ? AlarmStatus.ACK : AlarmStatus.RAZ;

                string[] values = new string[] { "Système", alarm.Description, statusBefore.ToString(), statusAfter.ToString() };
                db.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                alarm.id = db.GetMax(AuditTrailSettings["Table_Name"], "c00");
                alarm.Status = statusAfter;

                if (statusAfter == AlarmStatus.ACK)
                {
                    activeAlarms.Add(alarm);
                }
                else if (statusAfter == AlarmStatus.RAZ)
                {
                    RAZalarms.Add(alarm);
                }

                activeAlarms.RemoveAt(n);

                for (int i = 0; i < activeAlarms.Count(); i++)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
                }

            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Tu sais pas ce que tu fais c'est pas vrai !");
            }
        }
    }
}
