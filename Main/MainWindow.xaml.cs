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
using Driver_ColdTrap;
using Main.Properties;
using Driver_Ethernet;
using Driver_Ethernet_Balance;
using System.Speech.Synthesis;
using System.Windows.Media;
using Message;

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

        private void test()
        {
            MyMessageBox.Show("test-1");
        }

        public MainWindow()
        {
#if DEBUG
            LogManager.Configuration.Variables["myLevel"] = "Trace";
            LogManager.Configuration.Variables["isRelease"] = "true";
#else
            LogManager.Configuration.Variables["myLevel"] = "Error";
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

            /*
            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername, Window = this });
            InitializeComponent();
            AlarmManagement.ActiveAlarmEvent += ActiveAlarmEvent;
            AlarmManagement.InactiveAlarmEvent += InactiveAlarmEvent;


            SpeechSynthesizer synth = new SpeechSynthesizer();
            //synth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Senior);
            //synth.Speak("Oh... You're sweet, thank you. I don't love you, but it's nice to know that someone loves me. Who wouldn't anyway ?");

            ReportGeneration report = new ReportGeneration();
            report.GenerateCycleReport(646);

            MyMessageBox.Show("Fini je crois");
            Environment.Exit(1);
            //*/

            InitializeComponent();

            MyMessageBox.SetParentWindow(this);
            MyMessageBox.SetParentWindow(this, this.Window_Deactivated);

            MyDatabase.Initialize(new Database.IniInfo()
            {
                CycleFinalWeight_g_Unit = Settings.Default.CycleFinalWeight_g_Unit,
                CycleFinalWeight_g_Conversion = Settings.Default.CycleFinalWeight_g_Conversion,
                RecipeWeight_mgG_Unit = Settings.Default.RecipeWeight_mgG_Unit,
                RecipeWeight_mgG_Conversion = Settings.Default.RecipeWeight_mgG_Conversion,
                RecipeWeight_gG_Unit = Settings.Default.RecipeWeight_gG_Unit,
                RecipeWeight_gG_Conversion = Settings.Default.RecipeWeight_gG_Conversion,
                Window = this
            });

            AlarmManagement.ActiveAlarmEvent += ActiveAlarmEvent;
            AlarmManagement.InactiveAlarmEvent += InactiveAlarmEvent;
            /*
            Thread.CurrentThread.Name = "Main";
            MyDatabase.test();
            var task = Task.Run(() => MyDatabase.test());
            //task.ContinueWith(_ => MessageBox.Show("Salut"), TaskScheduler.FromCurrentSynchronizationContext());



            //Task task = new Task(() => MyDatabase.test());
            //task.Start();
            MessageBox.Show("Salut " + Thread.CurrentThread.Name);
            
            while(true)
            {
                Thread.Sleep(500);
            }*/

            /*
            Task task = new Task(() =>
            {
                Task t = new Task(() =>
                {
                    MyMessageBox.Show("Task2");
                    test();
                });
                t.Start();
            });
            task.Start();
            MyMessageBox.Show("Salut");//*/

            try
            {
                UpdateUser(username: UserPrincipal.Current.DisplayName.ToLower(),
                    role: UserManagement.UpdateAccessTable(UserPrincipal.Current.DisplayName));
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
            values[auditTrailInfo.Description] = Settings.Default.General_auditTrail_StartApp;

            // A corriger if insert didn't work
            MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, values); });

            frameMain.Content = new Pages.Status();
            frameInfoCycle.Content = null;

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
        }
        private async void Initialize()
        {
            while (!isWindowLoaded) await Task.Delay(Settings.Default.Main_WaitPageLoadedDelay);

            AlarmManagement.Initialize(new Alarm_Management.IniInfo() { AuditTrail_SystemUsername = Settings.Default.General_SystemUsername, Window = this });

            // We connect to the balance through a task to avoid a freeze of the application and allow the user to access the OS
            Task connectBalanceTask = new Task(() => { Balance.Connect(); });
            connectBalanceTask.Start();

            RS232Pump.Initialize(/*new Driver_RS232.IniInfo() { Window = this }*/);
            Driver_ColdTrap.ColdTrap.Initialize(new Driver_ColdTrap.IniInfo() { Window = this });
            UserManagement.Initialize(new User_Management.IniInfo() { Window = this });
            //SpeedMixerModbus.Initialize();
            if (RS232Pump.IsOpen())
            {
                RS232Pump.BlockUse();
                RS232Pump.StopPump();
                RS232Pump.FreeUse();
            }

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            object[] values = new object[auditTrailInfo.Ids.Count()];
            values[auditTrailInfo.Description] = General.auditTrail_BackupDesc;

            ReadInfo readInfo = new ReadInfo(
                _tableInfo: auditTrailInfo,
                _orderBy: auditTrailInfo.Ids[auditTrailInfo.Id],
                _isOrderAsc: false
                );

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows_new(readInfo, values, 1); });
            List<object[]> tableInfos = (List<object[]>)t.Result;
                        
            if (tableInfos == null || tableInfos.Count == 0)
            {
                MyMessageBox.Show("C'est pas bien de ne pas se connecter à la base de données");
                logger.Error("C'est pas bien de ne pas se connecter à la base de données");
                return;
            }

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
                    logger.Debug("ExecuteBackupAuto at start");
                    ExecuteBackupAuto();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
                return;
            }
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
                    values[auditTrailInfo.Description] = Settings.Default.General_auditTrail_BackupFailedDesc + nBackupAttempt.ToString();

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

            if (archiveCount == 0)
            {
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRowCount((new AuditTrailInfo()).TabName); });
                int nAuditTailRows = (int)t.Result;

                if (nAuditTailRows > Settings.Default.Archive_RowNumberTrigger)
                {
                    logger.Debug(nAuditTailRows.ToString());
                    MyMessageBox.Show(nAuditTailRows.ToString());
                    if (!Pages.Archiving.ExecuteFullArchive()) MyMessageBox.Show("Archivage échoué, merci de contacter un administrateur du système");
                }
            }
            archiveCount = (archiveCount + 1) % 3600;

            if (General.currentRole != AccessTableInfo.NoneRole && DateTime.Now.AddMinutes(-1 * Settings.Default.AutoLogOff_min).CompareTo(General.lastActTime) > 0)
            {
                UserManagement.SetNoneAccess();
                logger.Debug("Auto log off at " + DateTime.Now.ToString());

                this.Dispatcher.Invoke(() => {
                    UpdateUser("aucun utilisateur", AccessTableInfo.NoneRole);
                });
            }

            if (Settings.Default.Main_IsCalibMonitored)
            {
                ConfigurationManager.RefreshSection("appSettings");

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
            if (isCycleStarted && frameMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
            {
                Pages.ISubCycle subCycle = frameMain.Content as Pages.ISubCycle;
                subCycle.EnablePage(false);
                isATest = subCycle.IsItATest();
            }
            else
            {
                isATest = false;
                frameMain.Content = new Pages.Status();
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

                DateTime lastSampling;
                try
                {
                    lastSampling = Convert.ToDateTime(dailyTestValues[dailyTestInfo.DateTime]);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    MyMessageBox.Show(ex.Message);
                    return;
                }

                if (lastSampling.CompareTo(DateTime.Now.AddDays(-1)) < 0)
                {
                    bool[] accessTable = UserManagement.GetCurrentAccessTable();
                    if (accessTable[AccessTableInfo.DailyTest])
                    {
                        if (MyMessageBox.Show("Le dernier test journalier de la balance a été fait le " + lastSampling.ToString(Settings.Default.Date_Format_Read) + " à " + lastSampling.ToString(Settings.Default.Time_Format) + ", voulez-vous faire le test journalier ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain);
                            return;
                        }
                    }
                    else
                    {
                        MyMessageBox.Show("Le dernier test journalier de la balance a été fait le " + lastSampling.ToString(Settings.Default.Date_Format_Read) + " à " + lastSampling.ToString(Settings.Default.Time_Format) + " veuillez contacter une personne compétente pour faire le test journalier");
                        return;
                    }

                }

                frameMain.Content = new Pages.SubCycle.PreCycle(frameMain, frameInfoCycle, this);
                UpdateMenuStartCycle(false);
            }
            else
            {
                if (frameMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
                {
                    Pages.ISubCycle subCycle = frameMain.Content as Pages.ISubCycle;
                    subCycle.StopCycle();
                }
                else
                {
                    frameMain.Content = new Pages.Status();
                }

                UpdateMenuStartCycle(true);
            }
        }

        private void FxSystemStatus(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Status();
        }
        private void FxProgramNew(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(RcpAction.New);
        }
        private void FxProgramModify(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(RcpAction.Modify, frameMain, frameInfoCycle, window: this);
        }
        private void FxProgramCopy(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(RcpAction.Copy);
        }
        private void FxProgramDelete(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Recipe(RcpAction.Delete);
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

        private void FxParameters(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.Parameters();
        }

        private void FxSampling(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain);
        }

        private void frameMain_ContentRendered(object sender, EventArgs e)
        {
            if (frameMain.Content.GetType() == typeof(Pages.Status))
            {
            }
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
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool[] currentAccessTable = UserManagement.GetCurrentAccessTable();

            if (!currentAccessTable[AccessTableInfo.ApplicationStop])
            {
                MyMessageBox.Show("C'est pas bien ça");
                e.Cancel = true;
            }
        }
    }
}