using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Configuration;
using System.Collections.Specialized;
using Database;
using System.Globalization;
using DRIVER_RS232_Weight;
using Driver_RS232_Pump;
using System.Security.Principal;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using User_Management;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Driver_MODBUS;
using System.Diagnostics;
using Alarm_Management;
using System.Linq;
using NLog;
using NLog.Common;
using System.Threading;
using FPO_WPF_Test.Properties;

namespace FPO_WPF_Test
{
    public enum Action
    {
        New,
        Modify,
        Copy,
        Delete
    }
    /*
    public static class GeneralSettings
    {
        public static string General_SystemUsername { get; }

        static GeneralSettings()
        {
            General_SystemUsername = Settings.Default.General_SystemUsername;
        }
    }*/

    public partial class MainWindow : Window
    {
        //private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private readonly System.Timers.Timer currentTimeTimer;
        private bool isWindowLoaded = false;
        private bool wasAutoBackupStarted = false;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public MainWindow()
        {
#if DEBUG
            LogManager.Configuration.Variables["myLevel"] = "Trace";
            LogManager.Configuration.Variables["isRelease"] = "true";
#else
            LogManager.Configuration.Variables["myLevel"] = "Error";
#endif
            LogManager.ReconfigExistingLoggers(); // Explicit refresh of Layouts and updates active Logger-objects

            AlarmManagement.ActiveAlarmEvent += ActiveAlarmEvent;
            AlarmManagement.InactiveAlarmEvent += InactiveAlarmEvent;
            /*
            ReadInfo readInfo = new ReadInfo(
                _dtBefore: DateTime.Now.AddDays(-10),
                _dtAfter: DateTime.Now);

            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername });

            List<AuditTrailInfo> tableInfos = MyDatabase.GetAlarms(2000, 2100);
            string row;
            for (int i = 0; i < tableInfos.Count; i++)
            {
                row = "";
                for (int j = 0; j < tableInfos[i].columns.Count; j++)
                {
                    row += tableInfos[i].columns[j].value;
                }
                logger.Trace(row);
            }
            /

            AuditTrailInfo auditTrail1 = new AuditTrailInfo();
            auditTrail1.columns[auditTrail1.username].value = "Test user";
            auditTrail1.columns[auditTrail1.eventType].value = "Event";
            auditTrail1.columns[auditTrail1.description].value = "Quesu<Task> test 1";
            Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrail1); });

            AuditTrailInfo auditTrail2 = new AuditTrailInfo();
            auditTrail2.columns[auditTrail2.username].value = "Test user";
            auditTrail2.columns[auditTrail2.eventType].value = "Event";
            auditTrail2.columns[auditTrail2.description].value = "Quesu<Task> test 2";
            Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrail2); });

            AuditTrailInfo auditTrail3 = new AuditTrailInfo();
            auditTrail3.columns[auditTrail3.username].value = "Test user";
            auditTrail3.columns[auditTrail3.eventType].value = "Event";
            auditTrail3.columns[auditTrail3.description].value = "Quesu<Task> test 3";
            Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrail3); });

            logger.Fatal(t1.Result.ToString());
            logger.Fatal(t2.Result.ToString());
            logger.Fatal(t3.Result.ToString());

            MessageBox.Show("Fini je crois");
            Environment.Exit(1);
            //*/

            InitializeComponent();

            UpdateUser(username: UserPrincipal.Current.DisplayName.ToLower(),
                role: UserManagement.UpdateAccessTable(UserPrincipal.Current.DisplayName));
            labelSoftwareName.Text = General.application_name + " version " + General.application_version;

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.columns[auditTrailInfo.username].value = General.loggedUsername;
            auditTrailInfo.columns[auditTrailInfo.eventType].value = Settings.Default.General_AuditTrailEvent_Event;
            auditTrailInfo.columns[auditTrailInfo.description].value = Settings.Default.General_auditTrail_StartApp;
            //MyDatabase.InsertRow(auditTrailInfo);

            // A corriger if insert didn't work
            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });

            frameMain.Content = new Pages.Status();
            frameInfoCycle.Content = null;

            // Initialisation des timers
            currentTimeTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.Main_currentTimeTimer_Interval,
                AutoReset = true
            };
            currentTimeTimer.Elapsed += CurrentTimer_OnTimedEvent;
            currentTimeTimer.Start();
            Initialize();
        }
        private async void Initialize()
        {
            while (!isWindowLoaded) await Task.Delay(Settings.Default.Main_WaitPageLoadedDelay);

            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername });
            
            // 
            //
            //
            // ICI : PENSER A INITIALISER LA BALANCE ET LA POMPE ET PEUT-ÊTRE LE COLD TRAP
            //
            //
            //
            //*
            RS232Weight.rs232.Initialize();
            RS232Pump.rs232.Initialize();
            SpeedMixerModbus.Initialize();
            if (RS232Pump.rs232.IsOpen())
            {
                RS232Pump.rs232.BlockUse();
                RS232Pump.rs232.SetCommand("!C802 0");
                RS232Pump.rs232.FreeUse();
            }
            //*/

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.columns[auditTrailInfo.description].value = General.auditTrail_BackupDesc;
            // easy to make mutex better

            ReadInfo readInfo = new ReadInfo(
                _tableInfo: auditTrailInfo,
                _orderBy: auditTrailInfo.columns[auditTrailInfo.id].id,
                _isOrderAsc: false
                );

            //List<ITableInfo> tableInfos = MyDatabase.GetRows(readInfo, 1);
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows(readInfo, 1); });
            List<ITableInfo> tableInfos = (List<ITableInfo>)t.Result;

            // check if result is null

            if (tableInfos == null)
            {
                MessageBox.Show("C'est pas bien de ne pas se connecter à la base de données");
                logger.Error("C'est pas bien de ne pas se connecter à la base de données");
                return;
            }

            auditTrailInfo = (AuditTrailInfo)tableInfos[0];

            DateTime dtLastBackup = Convert.ToDateTime(auditTrailInfo.columns[auditTrailInfo.dateTime].value);
            if (dtLastBackup.CompareTo(General.NextBackupTime.AddDays(-1)) < 0)
            {
                logger.Debug("ExecuteBackupAuto at start");
                ExecuteBackupAuto();
            }
            else
            {
                //MyDatabase.Disconnect();
            }
        }
        private bool ExecuteBackupAuto()
        {
            bool wasBackupSucceeded = false;
            int nBackupAttempt = 0;
            int mutexID;
            // mutex à retirer
            mutexID = -1;
            //mutexID = MyDatabase.Connect(false);

            while (!wasBackupSucceeded && nBackupAttempt < 3)
            {
                wasBackupSucceeded = Pages.Backup.ExecuteBackup(Settings.Default.General_SystemUsername, mutexID);

                nBackupAttempt++;
                if (!wasBackupSucceeded)
                {
                    AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                    auditTrailInfo.columns[auditTrailInfo.username].value = Settings.Default.General_SystemUsername;
                    auditTrailInfo.columns[auditTrailInfo.eventType].value = Settings.Default.General_AuditTrailEvent_Event;
                    auditTrailInfo.columns[auditTrailInfo.description].value = Settings.Default.General_auditTrail_BackupFailedDesc + nBackupAttempt.ToString();

                    // A CORRIGER: CHECK IF RESULT FALSE
                    MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });
                    //MyDatabase.InsertRow(auditTrailInfo, mutexID);
                }
            }

            //MyDatabase.Disconnect(mutexID);

            if (!wasBackupSucceeded) AlarmManagement.NewAlarm(4, 0);

            return wasBackupSucceeded;
        }
        ~MainWindow()
        {
            //MessageBox.Show("Au revoir");
        }
        private void CurrentTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                labelDateTime.Text = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
            });

            if (!wasAutoBackupStarted && General.NextBackupTime.CompareTo(DateTime.Now) < 0)
            {
                wasAutoBackupStarted = true;
                logger.Debug("ExecuteBackupAuto on time");
                if (ExecuteBackupAuto()) General.NextBackupTime = General.NextBackupTime.AddDays(1);
            }
        }
        public void UpdateUser(string username, string role)
        {
            General.loggedUsername = username;
            General.currentRole = role;
            labelUser.Text = username + ", " + role;
        }
        private void ActiveAlarmEvent()
        {
            this.Dispatcher.Invoke(() =>
            {
                menuItemAlarm.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@".\Resources\img_alarm_act.png", UriKind.Relative))
                };
            });
        }
        private void InactiveAlarmEvent()
        {
            this.Dispatcher.Invoke(() =>
            {
                menuItemAlarm.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(@".\Resources\img_alarm_en.png", UriKind.Relative))
                };
            });
        }
        private void FxCycleStart(object sender, RoutedEventArgs e)
        {
            //menuItemStart.IsEnabled = false;

            menuItemStart.Icon = new Image
            {
                Source = new BitmapImage(new Uri(Settings.Default.Main_StartIconDis, UriKind.Relative))
            };

            frameMain.Content = new Pages.SubCycle.PreCycle(frameMain, frameInfoCycle);

            //Il faudra penser à bloquer ce qu'il faut
        }
        private void FxCycleStop(object sender, RoutedEventArgs e)
        {

        }
        private void FxSystemStatus(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Status();
        }
        private void FxProgramNew(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.New);
        }
        private void FxProgramModify(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.Modify, frameMain, frameInfoCycle);
        }
        private void FxProgramCopy(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.Copy);
        }
        private void FxProgramDelete(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(Action.Delete);
        }
        private void FxAuditTrail(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.AuditTrail();
        }
        private void FxAlarms(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.ActiveAlarms(frameMain);
        }
        private void FxUserLogInOut(object sender, RoutedEventArgs e)
        {
            LogIn w = new LogIn(this);
            w.Show();
        }
        private void FxUserNew(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserModify(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserDelete(object sender, RoutedEventArgs e)
        {

        }
        private void Close_App_Click(object sender, RoutedEventArgs e)
        {
            currentTimeTimer.Stop();
            Close();
        }
        private void FxBackup(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Backup();
        }
        private void FxArchiving(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Archiving();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isWindowLoaded = true;
        }
    }
}