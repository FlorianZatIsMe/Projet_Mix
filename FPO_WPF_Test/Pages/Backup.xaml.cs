using Alarm_Management;
using Database;
using FPO_WPF_Test.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour Backup.xaml
    /// </summary>
    public partial class Backup : Page
    {
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private static readonly string dbName = DatabaseSettings.ConnectionInfo.db;
        public static readonly string backupPath = Settings.Default.Backup_backupPath;// @"C:\Temp\Backups\";
        private static readonly string backupExtFile = Settings.Default.ArchBack_ExtFile;
        private readonly int maxBackupCount = Settings.Default.Backup_maxBackupCount;// 22;
        private static readonly int nDaysBefDelBackup = Settings.Default.Backup_nDaysBefDelBackup;// 10;
        private static string lastBackupFileName;
        private static int nLines;
        private static readonly AlarmManagement alarmManagement;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Backup()
        {
            logger.Debug("Start");

            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            InitializeComponent();

            labelStatus.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            labelStatus.Arrange(new Rect(0, 0, labelStatus.DesiredSize.Width, labelStatus.DesiredSize.Height));

            DirectoryInfo backupDirInfo = new DirectoryInfo(backupPath);
            FileInfo[] files = backupDirInfo.GetFiles("*" + backupExtFile);

            lbBackups.Items.SortDescriptions.Clear();
            lbBackups.Items.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Descending));
            for (int i = 0; i < files.Length; i++)
            {
                lbBackups.Items.Add(new ListBoxItem() { Content = files[i].Name });
            }
            lbBackups.Items.Refresh();
        }
        private async void Backup_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Backup_Click");

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            Task<bool> task = Task<bool>.Factory.StartNew(() => ExecuteBackup(General.loggedUsername));

            wpStatus.Visibility = Visibility.Visible;

            while (!task.IsCompleted)
            {
                progressBar.Value = (double)(100 * General.count / maxBackupCount);
                await Task.Delay(10);
            }
            MyDatabase.Disconnect();

            if (task.Result)
            {
                logger.Debug(Settings.Default.Backup_BackupSuccessfull);
                MessageBox.Show(Settings.Default.Backup_BackupSuccessfull);
                lbBackups.Items.Insert(0, new ListBoxItem() { Content = lastBackupFileName });
            }
            else
            {
                logger.Error(Settings.Default.Backup_BackupFailed);
                MessageBox.Show(Settings.Default.Backup_BackupFailed);
            }
            lastBackupFileName = "";
        }
        public static bool ExecuteBackup(string username, int mutex = -1)
        {
            logger.Debug("ExecuteBackup");

            bool isBackupSucceeded = false;

            lastBackupFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Backup_fileName_backup + 
                DatabaseSettings.ConnectionInfo.db + 
                (username == Settings.Default.General_SystemUsername ? Settings.Default.ArchBack_fileName_auto : Settings.Default.ArchBack_fileName_man) + 
                backupExtFile;

            string batchFile = Settings.Default.Backup_Backup_batchFile;// @".\Resources\DB_backup";
            string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = DatabaseSettings.ConnectionInfo.userID;// "root";
            string arg3 = DatabaseSettings.ConnectionInfo.password;// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.db;// dbName;
            string arg5 = backupPath + lastBackupFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5;
            //MessageBox.Show(command);    
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(processInfo);
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
                General.text = e.Data;
            };
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
                General.text = e.Data;
            };
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
                // Si l'alarme est active, on la désactive
                if (AlarmManagement.Alarms[4, 0].Status == AlarmStatus.ACTIVE || AlarmManagement.Alarms[4, 0].Status == AlarmStatus.ACK) AlarmManagement.InactivateAlarm(4, 0);

                // c'est peut-être nul ça (je veux dire la gestion du mutex), il faut ajouter un wait non ?
                logger.Warn("c'est peut-être nul ça (je veux dire la gestion du mutex), il faut ajouter un wait non ?");

                AuditTrailInfo auditTInfo = new AuditTrailInfo();
                auditTInfo.columns[auditTInfo.username].value = Settings.Default.General_SystemUsername;
                auditTInfo.columns[auditTInfo.eventType].value = Settings.Default.General_AuditTrailEvent_Event;
                auditTInfo.columns[auditTInfo.description].value = General.auditTrail_BackupDesc;
                MyDatabase.InsertRow(auditTInfo, mutex);

                //MyDatabase.Disconnect();

                General.count = Settings.Default.Backup_maxBackupCount;
                isBackupSucceeded = true;

                DirectoryInfo backupDirInfo = new DirectoryInfo(backupPath);
                logger.Trace(backupDirInfo.FullName);
                FileSystemInfo[] files = backupDirInfo.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                logger.Trace(files.Length);
                List<FileSystemInfo> orderedFiles = files.OrderBy(f => f.CreationTime).ToList();
                DateTime dtNow = DateTime.Now.AddDays(-nDaysBefDelBackup);
                int i;
                for (i = 0; i < orderedFiles.Count; i++)
                {
                    logger.Trace(i.ToString() + " - " + orderedFiles[i].Name + " " + orderedFiles[i].Extension);

                    if (orderedFiles[i].Extension != "")
                    {
                        if (orderedFiles[i].CreationTime.CompareTo(dtNow) < 0)
                        {
                            orderedFiles[i].Delete();
                            logger.Info(orderedFiles[i].CreationTime.ToString() + " deleted");
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                if (File.Exists(backupPath + lastBackupFileName)) File.Delete(backupPath + lastBackupFileName);
            }
            General.count = 0;
            General.text = "";

            process.Close();
            return isBackupSucceeded;
        }
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Restore_Click");

            if (lbBackups.SelectedItem != null)
            {
                string restoreFileName = (lbBackups.SelectedItem as ListBoxItem).Content.ToString();
                nLines = File.ReadAllLines(backupPath + restoreFileName).Length + Settings.Default.Backup_nLines_offset;

                wpStatus.Visibility = Visibility.Visible;

                Task task = Task.Factory.StartNew(() => ExecuteRestore(General.loggedUsername, restoreFileName));

                while (!task.IsCompleted)
                {
                    progressBar.Value = (double)(100 * General.count / nLines);
                    await Task.Delay(Settings.Default.ArchBack_progressBar_RefreshDelay);
                }
            }
            else
            {
                MessageBox.Show(Settings.Default.ArchBack_Request_SelectFile);
            }
        }
        private void ExecuteRestore(string username, string restoreFileName)
        {
            logger.Debug("ExecuteRestore");

            if (File.Exists(backupPath + restoreFileName))
            {
                string batchFile = Settings.Default.Backup_Restore_batchFile;// @".\Resources\DB_restore";
                string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
                string arg2 = DatabaseSettings.ConnectionInfo.userID;// "root";
                string arg3 = DatabaseSettings.ConnectionInfo.password;// "Integra2022/";
                string arg4 = DatabaseSettings.ConnectionInfo.db;// dbName;
                string arg5 = backupPath + restoreFileName;
                string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5;
                //MessageBox.Show(command);    
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                var process = Process.Start(processInfo);
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                    General.count++;
                };
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                    General.count++;
                };
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                    AuditTrailInfo auditTInfo = new AuditTrailInfo();
                    auditTInfo.columns[auditTInfo.username].value = username;
                    auditTInfo.columns[auditTInfo.eventType].value = Settings.Default.General_AuditTrailEvent_Event;// "Evènement";
                    auditTInfo.columns[auditTInfo.description].value = General.auditTrail_RestoreDesc;
                    MyDatabase.InsertRow(auditTInfo);
                    MyDatabase.Disconnect();

                    General.count = nLines;
                    MessageBox.Show(Settings.Default.ArchBack_restoreSuccessfull);
                }
                else
                {
                    MessageBox.Show(Settings.Default.ArchBack_restoreFailed);
                }
                General.count = 0;
                General.text = "";

                process.Close();
            }
            else
            {
                MessageBox.Show(Settings.Default.ArchBack_FileNotFound_1 + backupPath + restoreFileName + Settings.Default.ArchBack_FileNotFound_2);
            }
        }
        private void progressBar_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Debug("progressBar_Loaded");

            progressBar.Width = this.ActualWidth - labelStatus.ActualWidth - Settings.Default.ArchBack_progressBar_LeftMargin;
        }
    }
}
