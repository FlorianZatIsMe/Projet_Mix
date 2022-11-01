﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Configuration;
using System.Collections.Specialized;
using Database;
using System.Globalization;
using DRIVER.RS232.Weight;
using Driver.RS232.Pump;
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
using Driver.MODBUS;
using System.Diagnostics;
using Alarm_Management;
using System.Linq;
using NLog;
using NLog.Common;
using System.Threading;
using FPO_WPF_Test.Properties;

namespace FPO_WPF_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum Action
    {
        New,
        Modify,
        Copy,
        Delete
    }

    public partial class MainWindow : Window
    {
        //private readonly MyDatabase db;
        private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;
        private readonly System.Timers.Timer currentTimeTimer;
        private bool isWindowLoaded = false;
        private bool wasAutoBackupStarted = false;
        private readonly AlarmManagement alarmManagement;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
#if DEBUG
            LogManager.Configuration.Variables["myLevel"] = "Debug";
            LogManager.Configuration.Variables["isRelease"] = "true";
#else
            LogManager.Configuration.Variables["myLevel"] = "Error";
#endif

            LogManager.ReconfigExistingLoggers(); // Explicit refresh of Layouts and updates active Logger-objects
            /*
            logger.Trace("Trace");
            logger.Debug("Debug");
            logger.Info("Info");
            logger.Warn("Warning");
            logger.Error("Error");
            logger.Fatal("Fatal");
            //*/
            /*
            ReportGeneration report = new ReportGeneration();
            report.PdfGenerator("194");
            MessageBox.Show("Fini");

            Environment.Exit(1);
            //*/

            InitializeComponent();

            UpdateUser(username: UserPrincipal.Current.DisplayName.ToLower(),
                role: UserManagement.UpdateAccessTable(UserPrincipal.Current.DisplayName));
            labelSoftwareName.Text = General.application_name + " version " + General.application_version;

            string[] values = new string[] { General.loggedUsername, "Evènement", "Démarrage de l'application" };
            MyDatabase.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

            frameMain.Content = new Pages.Status();
            frameInfoCycle.Content = null;


            // Initialisation des timers
            currentTimeTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = true
            };
            currentTimeTimer.Elapsed += CurrentTimer_OnTimedEvent;
            currentTimeTimer.Start();
            Initialize();
        }
        private async void Initialize()
        {
            while (!isWindowLoaded) await Task.Delay(25);

            AlarmManagement.Initialize(new IniInfo() { AuditTrail_SystemUsername = Settings.Default.AuditTrail_SystemUsername });

            // 
            //
            //
            // ICI : PENSER A INITIALISER LA BALANCE ET LA POMPE ET PEUT-ÊTRE LE COLD TRAP
            //
            //
            //
            RS232Weight.Initialize();
            /*
            RS232Pump.Initialize();
            SpeedMixerModbus.Initialize();
            if (RS232Pump.IsOpen())
            {
                RS232Pump.BlockUse();
                RS232Pump.SetCommand("!C802 0");
                RS232Pump.FreeUse();
            }
            //*/

            int mutexID = MyDatabase.SendCommand_Read("audit_trail", "date_time", new string[] { "description" }, new string[] { General.auditTrail_BackupDesc }, "id", false, isMutexReleased: false);
            string[] array = MyDatabase.ReadNext(mutexID);
            MyDatabase.Signal(mutexID);

            DateTime dtLastBackup = Convert.ToDateTime(array[0]);
            //MessageBox.Show(dtLastBackup.ToString() + " - " + General.NextBackupTime.AddDays(-1).ToString());
            if (dtLastBackup.CompareTo(General.NextBackupTime.AddDays(-1)) < 0)
            {
                ExecuteBackupAuto();
            }
            else
            {
                MyDatabase.Disconnect();
            }
        }
        private bool ExecuteBackupAuto()
        {
            bool wasBackupSucceeded = false;
            int nBackupAttempt = 0;
            int mutexID;

            while (!wasBackupSucceeded && nBackupAttempt < 3)
            {
                if (!MyDatabase.IsConnected()) mutexID = MyDatabase.Connect();

                //Task<bool> task = Task<bool>.Factory.StartNew(() => Pages.Backup.ExecuteBackup("système"));
                //wasBackupSucceeded = task.Result;

                wasBackupSucceeded = Pages.Backup.ExecuteBackup("système");

                nBackupAttempt++;
                if (!wasBackupSucceeded)
                {
                    MyDatabase.InsertRow("audit_trail", "event_type, username, description", new string[] { "Evènement", "système", "Backup complet de la base de donnée échoué, essai " + nBackupAttempt.ToString() });
                }
                MyDatabase.Disconnect();
            }

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
                if (ExecuteBackupAuto()) General.NextBackupTime = General.NextBackupTime.AddDays(1);
            }
        }
        public void UpdateUser(string username, string role)
        {
            General.loggedUsername = username;
            General.currentRole = role;
            labelUser.Text = username + ", " + role;
        }
        private void FxCycleStart(object sender, RoutedEventArgs e)
        {
            //menuItemStart.IsEnabled = false;

            menuItemStart.Icon = new Image
            {
                Source = new BitmapImage(new Uri("Resources/img_start_dis.png", UriKind.Relative))
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