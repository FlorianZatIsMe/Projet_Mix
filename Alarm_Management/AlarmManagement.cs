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
        public static List<Tuple<int, int>> ActiveAlarms { get; }
        //public static List<Alarm> activeAlarms { get; }
        public static List<int> RAZalarms;
        public static Alarm[,] alarms;
        //private static MyDatabase db = new MyDatabase();
        private readonly static NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        //private readonly static Configuration.List MySettings = ConfigurationManager.GetSection("Alarm_Info/List") as Configuration.List;
        //private readonly static Database.Configuration.Connection_Info HisSettings = ConfigurationManager.GetSection("Database/Connection_Info") as Database.Configuration.Connection_Info;

        public struct Alarm
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
        static AlarmManagement()
        {
            ActiveAlarms = new List<Tuple<int, int>>();
            //RAZalarms = new List<Alarm>();
            RAZalarms = new List<int>();
            alarms = new Alarm[4, 2];

            alarms[0, 0] = new Alarm("Connexion à la balance échouée", AlarmType.Alarm);
            alarms[1, 0] = new Alarm("Connexion au SpeedMixer échouée", AlarmType.Alarm);
            alarms[2, 0] = new Alarm("Connexion à la pompe à vide échouée", AlarmType.Alarm);
            alarms[3, 0] = new Alarm("Connexion au piège froid échouée", AlarmType.Alarm);
            alarms[3, 1] = new Alarm("Température trop haute pendant le cycle", AlarmType.Alarm);
        }
        public static void NewAlarm(int id1, int id2)
        {
            //MessageBox.Show((MySettings.Alarm_Features.ID_List+1).ToString());

            int n = -1;
            bool isAlarmNotActive = alarms[id1, id2].Status != AlarmStatus.ACTIVE && 
                                    alarms[id1, id2].Status != AlarmStatus.ACK;
            /*
            for (int i = 0; i < activeAlarms.Count; i++)
            {
                if (activeAlarms[i].Description == alarms[id1, id2].Description)
                {
                    n = i;
                    break;
                }
            }*/

            if (isAlarmNotActive) // Si l'alarme n'est pas active, on peut la créer
            //if (n == -1 || (activeAlarms[n].Status != AlarmStatus.ACTIVE && activeAlarms[n].Status != AlarmStatus.ACK))
            {
                AlarmStatus statusBefore = alarms[id1, id2].Status;
                AlarmStatus statusAfter = AlarmStatus.ACTIVE;

                string[] values = new string[] { "Système", GetAlarmType(alarms[id1, id2].Type), GetAlarmDescription(id1, id2), statusBefore.ToString(), statusAfter.ToString() };

                //MyDatabase.InsertRow("temp2", "description", new string[] { "InsertRow - NewAlarm" });
                MyDatabase.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                alarms[id1, id2].id = MyDatabase.GetMax(AuditTrailSettings["Table_Name"], "id");
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
                    else MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Très bizarre...");
                }


                for (int i = 0; i < ActiveAlarms.Count(); i++)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
                }
                MessageBox.Show(GetAlarmDescription(id1, id2)); // Peut-être afficher la liste des alarmes actives à la place
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Ce n'est pas bien ce que vous faite Monsieur, on ne crée pas d'alarme si elle est déjà active...");
            }
        }
        public static void InactivateAlarm(int id1, int id2)
        {
            int n = -1;

            for (int i = 0; i < ActiveAlarms.Count; i++)
            {
                if (ActiveAlarms[i].Item1 == id1 && ActiveAlarms[i].Item2 == id2)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1 && alarms[id1, id2].Status != AlarmStatus.INACTIVE)
            {
                AlarmStatus statusBefore = alarms[id1, id2].Status;
                AlarmStatus statusAfter = AlarmStatus.None;// = (activeAlarms[n].Status == AlarmStatus.ACTIVE) ? AlarmStatus.INACTIVE : AlarmStatus.RAZ;

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
                        MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Non mais WHAT !!! T'es sérieux là ???");
                    }
                }
                else if (alarms[id1, id2].Type == AlarmType.Warning)
                {
                    statusAfter = AlarmStatus.RAZ;
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Roger, on a un problème, aucune alarm n'a été désactivée");
                }

                if (statusAfter != AlarmStatus.None)
                {
                    string[] values = new string[] { "Système", GetAlarmType(alarms[id1, id2].Type), GetAlarmDescription(id1, id2), statusBefore.ToString(), statusAfter.ToString() };

                    //MyDatabase.InsertRow("temp2", "description", new string[] { "InsertRow - InactivateAlarm" });
                    MyDatabase.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                    alarms[id1, id2].id = MyDatabase.GetMax(AuditTrailSettings["Table_Name"], "id");
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
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Tu sais pas ce que tu fais c'est pas vrai !");
            }

            for (int i = 0; i < ActiveAlarms.Count(); i++)
            {
                //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
            }
        }
        public static void AcknowledgeAlarm(int id1, int id2)
        {
            int n = -1;

            for (int i = 0; i < ActiveAlarms.Count; i++)
            {
                if (ActiveAlarms[i].Item1 == id1 && ActiveAlarms[i].Item2 == id2)
                {
                    n = i;
                    break;
                }
            }

            if (n != -1)
            {
                AlarmStatus statusBefore = alarms[id1, id2].Status;
                AlarmStatus statusAfter = statusBefore == AlarmStatus.ACTIVE ? AlarmStatus.ACK : (statusBefore == AlarmStatus.INACTIVE ? AlarmStatus.RAZACK : statusBefore);

                if (statusBefore != statusAfter)
                {
                    string[] values = new string[] { "Système", GetAlarmType(alarms[id1, id2].Type), GetAlarmDescription(id1, id2), statusBefore.ToString(), statusAfter.ToString() };

                    //MyDatabase.InsertRow("temp2", "description", new string[] { "InsertRow - AcknowledgeAlarm" });
                    MyDatabase.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"] + AuditTrailSettings["Insert_ValModif"], values);

                    alarms[id1, id2].id = MyDatabase.GetMax(AuditTrailSettings["Table_Name"], "id");
                    alarms[id1, id2].Status = statusAfter;

                    if (statusAfter == AlarmStatus.ACK)
                    {
                        ActiveAlarms.Add(new Tuple<int, int>(id1, id2));
                    }
                    else if (statusAfter == AlarmStatus.RAZACK)
                    {
                        RAZalarms.Add(alarms[id1, id2].id);
                    }

                    ActiveAlarms.RemoveAt(n);
                }

                for (int i = 0; i < ActiveAlarms.Count(); i++)
                {
                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - " + i.ToString() + ", " + activeAlarms[i].id.ToString() + ", " + activeAlarms[i].Description + ", " + activeAlarms[i].Status.ToString());
                }

            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Tu sais pas ce que tu fais c'est pas vrai !");
            }
        }
        public static string GetAlarmType(AlarmType type)
        {
            switch (type)
            {
                case AlarmType.Alarm:
                    return "Alarme";
                case AlarmType.Warning:
                    return "Alerte";
                case AlarmType.None:
                default:
                    return "";
            }
        }
        public static string GetAlarmDescription(int id1, int id2)
        {
            return GetAlarmType(alarms[id1, id2].Type).ToUpper() + " " + id1.ToString("00") + "." + id2.ToString("00") + " " + alarms[id1, id2].Description;
        }
    }
}
