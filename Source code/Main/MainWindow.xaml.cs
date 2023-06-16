using Driver_RS232_Pump;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Configuration;
using Database;
using System.Globalization;
using User_Management;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using Driver_MODBUS;
using Alarm_Management;
using System.Linq;
using NLog;
using System.Threading;
using Main.Properties;
using Driver_Ethernet_Balance;
using System.Windows.Media;
using Message;
using System.Timers;

namespace Main
{
    public enum RcpAction
    {
        New,
        Modify,
        Copy,
        Delete
    }

    public partial class MainWindow : Window
    {
        private readonly System.Timers.Timer currentTimeTimer;
        private bool isWindowLoaded = false;
        private bool wasAutoBackupStarted = false;
        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private bool isCycleStarted = false;
        private bool isAlarmActive = false;
        private bool wasActTimeUpdated = false;
        private int archiveCount = 0;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        private System.Timers.Timer scanAlarmTimer;

        public MainWindow()
        {
#if DEBUG
            LogManager.Configuration.Variables["myLevel"] = "Trace";
            LogManager.Configuration.Variables["isDebug"] = "true";
#else
            LogManager.Configuration.Variables["myLevel"] = "Error";
            LogManager.Configuration.Variables["isDebug"] = "false";
#endif
            LogManager.ReconfigExistingLoggers(); // Explicit refresh of Layouts and updates active Logger-objects
            General.Initialize(new IniInfo() { Window = this });

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
            {
                CultureInfo culture = new CultureInfo("fr-CH");
                NumberFormatInfo numberformat = culture.NumberFormat;
                numberformat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername, Window = this });
            
            InitializeComponent();

            bool isFirstSampleWeightCorrect = false;
            try
            {
                decimal.Parse(config.AppSettings.Settings["DailyTest_Weight0"].Value);
                isFirstSampleWeightCorrect = true;

            }
            catch (Exception)
            {
                isFirstSampleWeightCorrect = false;
            }

            if (isFirstSampleWeightCorrect && config.AppSettings.Settings["DailyTest_WeightID0"].Value == "") isFirstSampleWeightCorrect = false;

            if (!isFirstSampleWeightCorrect)
            {
                menuItemHome.Visibility = Visibility.Collapsed;
                menuItemStart.Visibility = Visibility.Collapsed;
                menuItemRecipes.Visibility = Visibility.Collapsed;
                menuItemAuditTrail.Visibility = Visibility.Collapsed;
                menuItemAlarm.Visibility = Visibility.Collapsed;
                menuItemBackup.Visibility = Visibility.Collapsed;
                menuItemParameters.Visibility = Visibility.Collapsed;
                menuItemDailyTest.Visibility = Visibility.Collapsed;
                menuConnexion.Visibility = Visibility.Collapsed;
                Close_App.Visibility = Visibility.Collapsed;
                MyMessageBox.Show("La première masse étalon est incorrectement définie, veuiller contacter un responsable");
                return;
            }



            AlarmManagement.ActiveAlarmEvent += ActiveAlarmEvent;
            AlarmManagement.InactiveAlarmEvent += InactiveAlarmEvent;

            

            /*
            SpeechSynthesizer synth = new SpeechSynthesizer();
            //synth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Senior);
            //synth.Speak("Oh... You're sweet, thank you. I don't love you, but it's nice to know that someone loves me. Who wouldn't anyway ?");

            General.PrintReport(1);

            MyMessageBox.Show("Fini je crois");
            Environment.Exit(1);
            //*/

            //MyMessageBox.SetParentWindow(this);

            MyMessageBox.SetParentWindow(this, this.Window_Deactivated);

            MyDatabase.Initialize(new Database.IniInfo()
            {
                CycleFinalWeight_g_Unit = Settings.Default.Cycle_FinalWeight_g_Unit,
                //CycleFinalWeight_g_Conversion = Settings.Default.Cycle_FinalWeight_g_Conversion,
                RecipeWeight_mgG_Unit = Settings.Default.RecipeWeight_mgG_Unit,
                RecipeWeight_mgG_Conversion = Settings.Default.RecipeWeight_mgG_Conversion,
                RecipeWeight_gG_Unit = Settings.Default.RecipeWeight_gG_Unit,
                RecipeWeight_gG_Conversion = Settings.Default.RecipeWeight_gG_Conversion,
                Window = this
            });

            AlarmManagement.ActiveAlarmEvent += ActiveAlarmEvent;
            AlarmManagement.InactiveAlarmEvent += InactiveAlarmEvent;

            try
            {
                UpdateUser(username: UserPrincipal.Current.DisplayName.ToLower(),
                    role: UserManagement.UpdateAccessTable());//UserPrincipal.Current.DisplayName););
            }
            catch (Exception)
            {
                logger.Error("Problème de connexion avec l'active directory");
                UserManagement.SetNoneAccess();
                UpdateUser(username: "aucun utilisateur",
                    role: AccessTableInfo.NoneRole);
            }

            labelSoftwareName.Text = General.application_name + " version " + General.application_version;

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            object[] values = new object[auditTrailInfo.Ids.Count()];
            values[auditTrailInfo.Username] = General.loggedUsername;
            values[auditTrailInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
            values[auditTrailInfo.Description] = Settings.Default.Main_AuditTrail_StartAppDesc;

            // A corriger if insert didn't work
            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, values); });

            //frameMain_Old.Content = new Pages.StatusOld();
            //frameInfoCycle.Content = null;

            // Initialisation des timers
            currentTimeTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.Main_currentTimeTimer_Interval,
                AutoReset = false
            };
            currentTimeTimer.Elapsed += CurrentTimer_OnTimedEvent;
            currentTimeTimer.Start();

            for (int i = 0; i < Pages.Sequence.list.Count; i++)
            {
                if (Pages.Sequence.list[i].subRecipeInfo.SeqType != i || Pages.Sequence.list[i].subCycleInfo.SeqType != i)
                {
                    MyMessageBox.Show(Settings.Default.Recipe_Error_listIncorrect);
                    logger.Error(Settings.Default.Recipe_Error_listIncorrect);
                    Environment.Exit(1);
                }
            }

            Initialize();

            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 2000,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            //scanAlarmTimer.Start();            
        }
        private void ScanAlarmTimer_OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                //*
                if (contentControlMain.Content.GetType() == typeof(Pages.ActiveAlarms))
                {
                    contentControlMain.Content = new Pages.Archiving(contentControlMain);
                }
                else
                {
                    contentControlMain.Content = new Pages.ActiveAlarms(contentControlMain);
                }
                //*/
            });

            scanAlarmTimer.Enabled = true;
        }
        private async void Initialize()
        {
            while (!isWindowLoaded) await Task.Delay(Settings.Default.Main_WaitPageLoadedDelay);

            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername, Window = this });

            // We connect to the balance through a task to avoid a freeze of the application and allow the user to access the OS
            Task connectBalanceTask = new Task(() => { Balance.Connect(); });
            connectBalanceTask.Start();

            //Driver_ColdTrap.ColdTrap.Initialize(new Driver_ColdTrap.IniInfo() { Window = this });
            //UserManagement.Initialize(new User_Management.IniInfo() { Window = this });
            RS232Pump.Initialize();
            SpeedMixerModbus.Initialize();

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            object[] values = new object[auditTrailInfo.Ids.Count()];
            values[auditTrailInfo.Description] = Settings.Default.Backup_AuditTrail_SuccessDesc;

            ReadInfo readInfo = new ReadInfo(
                _tableInfo: auditTrailInfo,
                _orderBy: auditTrailInfo.Ids[auditTrailInfo.Id],
                _isOrderAsc: false
                );

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows_new(readInfo, values, 1); });
            List<object[]> tableInfos = (List<object[]>)t.Result;
                        
            if (tableInfos == null)
            {
                MyMessageBox.Show("C'est pas bien de ne pas se connecter à la base de données");
                logger.Error("C'est pas bien de ne pas se connecter à la base de données");
                return;
            }
            else if (tableInfos.Count == 0)
            {
                logger.Info("First ExecuteBackupAuto");
                ExecuteBackupAuto();
            }
            else
            {
                object[] auditTrailRow = tableInfos[0];

                for (int i = 0; i < auditTrailRow.Count(); i++)
                {
                    logger.Trace(auditTrailRow[i].ToString());
                }

                try
                {
                    DateTime dtLastBackup = Convert.ToDateTime(auditTrailRow[auditTrailInfo.DateTime]);
                    if (dtLastBackup.CompareTo(General.NextBackupTime.AddDays(-1)) < 0)
                    {
                        logger.Info("ExecuteBackupAuto at start");
                        ExecuteBackupAuto();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    MyMessageBox.Show(ex.Message);
                }
            }

            //contentControlMain.Content = new Pages.Status();
            //contentControlInfoCycle.Content = null;
        }
        private bool ExecuteBackupAuto()
        {
            bool wasBackupSucceeded = false;
            int nBackupAttempt = 0;

            while (!wasBackupSucceeded && nBackupAttempt < 3)
            {
                wasBackupSucceeded = Pages.Backup.ExecuteBackup(Settings.Default.General_SystemUsername);

                nBackupAttempt++;
                if (!wasBackupSucceeded)
                {
                    AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                    object[] values = new object[auditTrailInfo.Ids.Count()];
                    values[auditTrailInfo.Username] = Settings.Default.General_SystemUsername;
                    values[auditTrailInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
                    values[auditTrailInfo.Description] = Settings.Default.Backup_AuditTrail_FailedDesc + nBackupAttempt.ToString();

                    MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, values); });
                }
            }

            if (!wasBackupSucceeded) AlarmManagement.NewAlarm(4, 0);

            return wasBackupSucceeded;
        }
        ~MainWindow()
        {
            //MyMessageBox.Show("Au revoir");
        }

        private void CurrentTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // We show the current date and time
            this.Dispatcher.Invoke(() =>
            {
                labelDateTime.Text = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
            });

            // We see if we perform an automatic backup
            if (!wasAutoBackupStarted && General.NextBackupTime.CompareTo(DateTime.Now) < 0)
            {
                wasAutoBackupStarted = true;
                logger.Debug("ExecuteBackupAuto on time");
                if (ExecuteBackupAuto()) General.NextBackupTime = General.NextBackupTime.AddDays(1);
            }

            // We see if we perform an archiving
            if (archiveCount == 0)
            {
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRowCount((new AuditTrailInfo()).TabName); });
                int nAuditTailRows = (int)t.Result;

                if (nAuditTailRows > Settings.Default.Archiving_RowNumberTrigger)
                {
                    logger.Debug(nAuditTailRows.ToString());
                    if (!Pages.Archiving.ExecuteFullArchive()) MyMessageBox.Show("Archivage échoué, merci de contacter un administrateur du système");
                }
            }
            archiveCount = (archiveCount + 1) % 3600;

            // We see if we perform an auto log off
            if (General.currentRole != AccessTableInfo.NoneRole && DateTime.Now.AddMinutes(-1 * Settings.Default.AutoLogOff_min).CompareTo(General.lastActTime) > 0)
            {
                UserManagement.SetNoneAccess();
                logger.Info("Auto log off at " + DateTime.Now.ToString());

                this.Dispatcher.Invoke(() => {
                    UpdateUser("aucun utilisateur", AccessTableInfo.NoneRole);
                });
            }

            // We see if we inform the user of the calibration state
            bool isCalibMonitored = ConfigurationManager.AppSettings["Main_IsCalibMonitored"] == (true).ToString();

            if (isCalibMonitored)//Settings.Default.Main_IsCalibMonitored)
            {
                try
                {
                    DateTime nextCalibDate = Convert.ToDateTime(ConfigurationManager.AppSettings["NextCalibDate"]);
                    this.Dispatcher.Invoke(() => {
                        if (nextCalibDate.CompareTo(DateTime.Now) < 0)
                        {
                            if (labelCalibration.Text != "Calibration de La balance expirée depuis le " + nextCalibDate.ToString(Settings.Default.Date_Format_Read))
                            {
                                labelCalibration.Text = "Calibration de La balance expirée depuis le " + nextCalibDate.ToString(Settings.Default.Date_Format_Read);
                                labelCalibration.Foreground = (SolidColorBrush)Application.Current.FindResource("FontColor_Alarm");// Brushes.Orange;
                            }
                        }
                        else if (nextCalibDate.CompareTo(DateTime.Now.AddDays(15)) < 0)
                        {
                            if (labelCalibration.Text != "La balance devra être calibrée avant le " + nextCalibDate.ToString(Settings.Default.Date_Format_Read))
                            {
                                labelCalibration.Text = "La balance devra être calibrée avant le " + nextCalibDate.ToString(Settings.Default.Date_Format_Read);
                                labelCalibration.Foreground = (SolidColorBrush)Application.Current.FindResource("FontColor_Warning"); //Brushes.Yellow;
                            }
                        }
                        else
                        {
                            if (labelCalibration.Text != "")
                            {
                                labelCalibration.Text = "";
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    logger.Fatal(ConfigurationManager.AppSettings["NextCalibDate"]);
                }
            }

            currentTimeTimer.Enabled = true;
        }
        public void UpdateUser(string username, string role)
        {
            General.loggedUsername = username;
            General.currentRole = role;
            labelUser.Text = username + ", " + role;
            bool[] currentAccess = UserManagement.GetCurrentAccessTable();

            bool isATest;

            // Si le cycle à démarrer et que la frame principale est un sous cycle alors on rend le bouton STOP enable si le test en cours est un test
            if (isCycleStarted && contentControlMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
            {
                Pages.ISubCycle subCycle = contentControlMain.Content as Pages.ISubCycle;
                subCycle.EnablePage(false);
                isATest = subCycle.IsItATest();
            }
            else
            {
                isATest = false;
                contentControlMain.Content = new Pages.Status();
            }

            menuItemStart.Visibility = 
                currentAccess[AccessTableInfo.CycleStart] ? Visibility.Visible : 
                currentAccess[AccessTableInfo.RecipeUpdate] ? Visibility.Visible : Visibility.Collapsed;

            // Si le nouvel utilisateur peut modifier les recettes mais ne peut pas démarrer un cycle
            if (!currentAccess[AccessTableInfo.CycleStart] && currentAccess[AccessTableInfo.RecipeUpdate])
            {
                menuItemStart.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(isATest ? Settings.Default.Main_StopIconEn : Settings.Default.Main_StopIconDis, UriKind.Relative))
                };
                menuItemStart.Header = "STOP";
                menuItemStart.IsEnabled = isATest;
            }
            // Sinon si le nouvel utilisateur peut démarrer un cycle
            else if ((!isATest && currentAccess[AccessTableInfo.CycleStart]) || (isATest && currentAccess[AccessTableInfo.RecipeUpdate]))
            {
                menuItemStart.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(isCycleStarted ? Settings.Default.Main_StopIconEn : Settings.Default.Main_StartIconEn, UriKind.Relative))
                };
                menuItemStart.Header = isCycleStarted ? "STOP" : "DEMARRER";
                menuItemStart.IsEnabled = true;
            }
            else
            {
                menuItemStart.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(isCycleStarted ? Settings.Default.Main_StopIconDis : Settings.Default.Main_StartIconDis, UriKind.Relative))
                };
                menuItemStart.Header = isCycleStarted ? "STOP" : "DEMARRER";
                menuItemStart.IsEnabled = false;
            }

            menuItemRecipes.Visibility = currentAccess[AccessTableInfo.RecipeUpdate] ? Visibility.Visible : Visibility.Collapsed;
            menuItemBackup.Visibility = currentAccess[AccessTableInfo.Backup] ? Visibility.Visible : Visibility.Collapsed;
            menuItemParameters.Visibility = currentAccess[AccessTableInfo.Parameters] ? Visibility.Visible : Visibility.Collapsed;
            menuItemDailyTest.Visibility = currentAccess[AccessTableInfo.DailyTest] ? Visibility.Visible : Visibility.Collapsed;
            Close_App.Visibility = currentAccess[AccessTableInfo.ApplicationStop] ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateMenuStartCycle(bool start)
        {
            isCycleStarted = !start;
            bool[] currentAccess = UserManagement.GetCurrentAccessTable();


            if (!currentAccess[AccessTableInfo.CycleStart] && currentAccess[AccessTableInfo.RecipeUpdate])
            {
                menuItemStart.IsEnabled = !start;
                menuItemStart.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(
                        start ? Settings.Default.Main_StopIconDis : Settings.Default.Main_StopIconEn, UriKind.Relative))
                };
            }
            else
            {
                menuItemStart.Header = start ? "DEMARRER" : "STOP";
                menuItemStart.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(
                        start ? (currentAccess[AccessTableInfo.CycleStart] ? Settings.Default.Main_StartIconEn : Settings.Default.Main_StartIconDis) : Settings.Default.Main_StopIconEn, UriKind.Relative))
                };
            }

            menuItemHome.IsEnabled = start;
            menuItemHome.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_HomeIconEn : Settings.Default.Main_HomeIconDis, UriKind.Relative))
            };

            menuItemRecipes.IsEnabled = start;
            menuItemRecipes.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_RecipeIconEn : Settings.Default.Main_RecipeIconDis, UriKind.Relative))
            };

            menuItemAuditTrail.IsEnabled = start;
            menuItemAuditTrail.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_AuditTrailIconEn : Settings.Default.Main_AuditTrailIconDis, UriKind.Relative))
            };

            menuItemAlarm.IsEnabled = start;
            menuItemAlarm.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    isAlarmActive ? Settings.Default.Main_AlarmIconAct : (start ? Settings.Default.Main_AlarmIconEn : Settings.Default.Main_AlarmIconDis), UriKind.Relative))
            };

            menuItemBackup.IsEnabled = start;
            menuItemBackup.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_BackupArchiveIconEn : Settings.Default.Main_BackupArchiveIconDis, UriKind.Relative))
            };

            menuItemParameters.IsEnabled = start;
            menuItemParameters.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_ParametersIconEn : Settings.Default.Main_ParametersIconDis, UriKind.Relative))
            };

            menuItemDailyTest.IsEnabled = start;
            menuItemDailyTest.Icon = new Image
            {
                Source = new BitmapImage(new Uri(
                    start ? Settings.Default.Main_DailyTestIconEn : Settings.Default.Main_DailyTestIconDis, UriKind.Relative))
            };
        }
        private void ActiveAlarmEvent()
        {
            isAlarmActive = true;
            this.Dispatcher.Invoke(() =>
            {
                menuItemAlarm.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(Settings.Default.Main_AlarmIconAct, UriKind.Relative))
                };
            });
        }
        private void InactiveAlarmEvent()
        {
            isAlarmActive = false;
            this.Dispatcher.Invoke(() =>
            {
                menuItemAlarm.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(isCycleStarted ? Settings.Default.Main_AlarmIconDis : Settings.Default.Main_AlarmIconEn, UriKind.Relative))
                };
            });
        }
        private void FxCycleStart(object sender, RoutedEventArgs e)
        {
            if (!isCycleStarted)
            {
                DailyTestInfo dailyTestInfo = new DailyTestInfo();
                object[] dailyTestValues = new object[dailyTestInfo.Ids.Count()];
                dailyTestValues[dailyTestInfo.Status] = DatabaseSettings.General_TrueValue_Write;
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(dailyTestInfo, dailyTestInfo.Ids[dailyTestInfo.Id], dailyTestValues); });
                int id = (int)t.Result;

                dailyTestInfo = new DailyTestInfo();
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(dailyTestInfo, id); });
                dailyTestValues = (object[])t.Result;

                DateTime lastDailyTest;
                try
                {
                    lastDailyTest = Convert.ToDateTime(dailyTestValues[dailyTestInfo.DateTime]);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    MyMessageBox.Show(ex.Message);
                    return;
                }

                DateTime lastAllowedDailyTest = DateTime.Now.AddDays(-Settings.Default.LastDailyTest_Days).AddHours(-Settings.Default.LastDailyTest_Hours);
                if (lastDailyTest.CompareTo(lastAllowedDailyTest) < 0)
                {
                    bool[] accessTable = UserManagement.GetCurrentAccessTable();
                    if (accessTable[AccessTableInfo.DailyTest])
                    {
                        if (MyMessageBox.Show("Le dernier test journalier de la balance a été fait le " + lastDailyTest.ToString(Settings.Default.Date_Format_Read) + " à " + lastDailyTest.ToString(Settings.Default.Time_Format) + ", voulez-vous faire le test journalier ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            if (!General.IsFolderAccessible(Settings.Default.DailyTest_ReportPath))
                            {
                                if (MyMessageBox.Show("Le rapport ne pourra être généré, voulez-vous continuer ?",
                                    MessageBoxButton.YesNo) == MessageBoxResult.No) return;
                            }

                            contentControlMain.Content = new Pages.SubCycle.CycleWeight(Pages.SubCycle.CurrentPhase.DailyTest, contentControlMain);
                            //contentControlMain.Content = new Pages.Status();
                            return;
                        }
                    }
                    else
                    {
                        MyMessageBox.Show("Le dernier test journalier de la balance a été fait le " + lastDailyTest.ToString(Settings.Default.Date_Format_Read) + " à " + lastDailyTest.ToString(Settings.Default.Time_Format) + " veuillez contacter une personne compétente pour faire le test journalier");
                        return;
                    }

                }

                if (!General.IsFolderAccessible(Settings.Default.Report_Path))
                {
                    if (MyMessageBox.Show("Le rapport ne pourra être généré, voulez-vous continuer ?", 
                        MessageBoxButton.YesNo) == MessageBoxResult.No) return;
                }

                //frameMain_Old.Content = new Pages.SubCycle.PreCycleOld(frameMain_Old, frameInfoCycle, this);
                contentControlMain.Content = new Pages.SubCycle.PreCycle(contentControlMain, contentControlInfoCycle, this);
                UpdateMenuStartCycle(false);
            }
            else
            {
                if (contentControlMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
                {
                    Pages.ISubCycle subCycle = contentControlMain.Content as Pages.ISubCycle;
                    subCycle.StopCycle();
                }
                else
                {
                    contentControlMain.Content = new Pages.Status();
                    //frameMain_Old.Content = new Pages.StatusOld();
                }

                UpdateMenuStartCycle(true);
            }
        }

        private async void FxSystemStatus(object sender, RoutedEventArgs e)
        {/*
            using (Pages.Status page = new Pages.Status())
            {
                frameMain.Content = page;
                while (!frameMain.Content.Equals(page)) await Task.Delay(100);
                while (frameMain.Content.Equals(page)) await Task.Delay(100);
            }*/
            contentControlMain.Content = new Pages.Status();

            //frameMain_Old.Content = new Pages.StatusOld();
        }
        private void FxProgramNew(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.RecipeOld(RcpAction.New);
            contentControlMain.Content = new Pages.Recipe(RcpAction.New);
        }
        private void FxProgramModify(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.RecipeOld(RcpAction.Modify, frameMain_Old, frameInfoCycle, window: this);
            contentControlMain.Content = new Pages.Recipe(RcpAction.Modify, contentControlMain, contentControlInfoCycle, window: this);
        }
        private void FxProgramCopy(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.RecipeOld(RcpAction.Copy);
            contentControlMain.Content = new Pages.Recipe(RcpAction.Copy);
        }
        private void FxProgramDelete(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.RecipeOld(RcpAction.Delete);
            contentControlMain.Content = new Pages.Recipe(RcpAction.Delete);
        }
        private async void FxAuditTrail(object sender, RoutedEventArgs e)
        {/*
            using (Pages.AuditTrail page = new Pages.AuditTrail())
            {
                frameMain.Content = page;
                while (!frameMain.Content.Equals(page)) await Task.Delay(100);
                while (frameMain.Content.Equals(page)) await Task.Delay(100);
            }//*/

            contentControlMain.Content = new Pages.AuditTrail();
            //frameMain_Old.Navigate(new Pages.AuditTrailOld());
        }
        private void FxAlarms(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.ActiveAlarmsOld(frameMain_Old);
            contentControlMain.Content = new Pages.ActiveAlarms(contentControlMain);
        }
        private void FxUserLogInOut(object sender, RoutedEventArgs e)
        {
            LogIn w = new LogIn(this, this.Window_Deactivated);
            w.ShowDialog();
        }
        private void Close_App_Click(object sender, RoutedEventArgs e)
        {
            currentTimeTimer.Stop();
            Close();
        }
        private void FxBackup(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.BackupOld();
            contentControlMain.Content = new Pages.Backup(contentControlMain);
        }
        private void FxArchiving(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.ArchivingOld();
            contentControlMain.Content = new Pages.Archiving(contentControlMain);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isWindowLoaded = true;
        }

        private void FxParameters(object sender, RoutedEventArgs e)
        {
            //frameMain_Old.Content = new Pages.ParametersOld();
            contentControlMain.Content = new Pages.Parameters();
        }

        private void FxDailyTest(object sender, RoutedEventArgs e)
        {
            if (!General.IsFolderAccessible(Settings.Default.DailyTest_ReportPath))
            {
                if (MyMessageBox.Show("Le rapport ne pourra être généré, voulez-vous continuer ?",
                    MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            }

            //frameMain_Old.Content = new Pages.SubCycle.CycleWeightOld(frameMain_Old);
            contentControlMain.Content = new Pages.SubCycle.CycleWeight(Pages.SubCycle.CurrentPhase.DailyTest, contentControlMain);
        }

        private void frameMain_ContentRendered(object sender, EventArgs e)
        {
        }

        private async void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!wasActTimeUpdated)
            {
                wasActTimeUpdated = true;
                General.ResetLastActTime();
                await Task.Delay(2000);
                wasActTimeUpdated = false;
            }
        }

        public async void Window_Deactivated(object sender, EventArgs e)
        {
            await Task.Delay(100);
            logger.Debug("Window_Deactivated");

            //this.Activate();
            //await Task.Delay(100);

            /*
Oui, il existe une autre méthode pour verrouiller un ordinateur et empêcher l'accès à d'autres programmes. Cette méthode consiste à créer un "shell personnalisé" pour Windows. En d'autres termes, vous pouvez remplacer l'interface graphique habituelle de Windows (explorer.exe) par votre propre application. Voici les étapes à suivre :

    Ouvrez l'éditeur de registre en appuyant sur la touche Windows + R, tapez "regedit" et appuyez sur Entrée.

    Accédez à la clé de registre suivante : HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon.

    Créez une nouvelle chaîne de valeur nommée "Shell" (si elle n'existe pas déjà).

    Définissez la valeur de la chaîne Shell sur le chemin complet de votre programme (par exemple, "C:\MonProgramme.exe").

    Redémarrez l'ordinateur.

Lorsque l'ordinateur redémarre, votre application s'exécutera automatiquement et remplacera l'interface graphique habituelle de Windows. Cela signifie que les utilisateurs ne pourront pas accéder à d'autres programmes ou au bureau, car ils n'auront pas accès à l'interface graphique habituelle de Windows.

Il est important de noter que cette méthode est assez radicale et peut rendre l'ordinateur inutilisable si votre application plante ou ne fonctionne pas correctement. Il est donc recommandé de tester soigneusement votre application avant de la déployer sur un ordinateur de production.
            */
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool[] currentAccessTable = UserManagement.GetCurrentAccessTable();

            if (!currentAccessTable[AccessTableInfo.ApplicationStop])
            {
                MyMessageBox.Show("C'est pas bien ça");
                e.Cancel = true;
                return;
            }

            /*
            if (frameMain_Old.Content.GetType().GetInterface(typeof(IDisposable).Name) == typeof(IDisposable))
            {
                IDisposable disposablePage = frameMain_Old.Content as IDisposable;
                disposablePage.Dispose();
            }*/
        }

        private IDisposable previousDisposablePage = null;
        private bool pageToDisposed = false;
        private Page previousPage = null;
        private object frameContent;

        private void frameMain_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Frame frame = sender as Frame;
            /*
            logger.Debug("frameMain_Navigated " + frame.Content.GetType().ToString());

            if (pageToDisposed)
            {
                logger.Debug("previousDisposablePage.Dispose");
                pageToDisposed = false;
                previousDisposablePage.Dispose();
                //previousDisposablePage = null;
            }

            if (frame.Content.GetType().GetInterface(typeof(IDisposable).Name) == typeof(IDisposable))
            {
                previousDisposablePage = frame.Content as IDisposable;
                pageToDisposed = true;
            }//*/
        }

        private void contentControlMain_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            MyMessageBox.Show("Target updated ?");
        }
    }
}