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
        private static readonly string dbName = "db1";
        public static readonly string backupPath = @"C:\Temp\Backups\";
        private static readonly string backupExtFile = ".sql";
        private static readonly int nDaysBefDelBackup = 10;
        private static string lastBackupFileName;
        private static int nLines;
        private static readonly AlarmManagement alarmManagement;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

        public Backup()
        {
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
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            Task<bool> task = Task<bool>.Factory.StartNew(() => ExecuteBackup(General.loggedUsername));

            wpStatus.Visibility = Visibility.Visible;

            while (!task.IsCompleted)
            {
                progressBar.Value = (double)(100 * General.count / 40);
                await Task.Delay(10);
            }
            MyDatabase.Disconnect();

            if (task.Result)
            {
                MessageBox.Show("Backup réussi");
                lbBackups.Items.Insert(0, new ListBoxItem() { Content = lastBackupFileName });
            }
            else
            {
                MessageBox.Show("Backup échoué");
            }
            lastBackupFileName = "";
        }
        public static bool ExecuteBackup(string username, int mutex = -1)
        {
            bool isBackupSucceeded = false;

            lastBackupFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + "_backup_" + dbName + (username == "système" ? "_auto" : "_man") + backupExtFile;

            //string batchFile = @".\Resources\DB_restore";
            string batchFile = @".\Resources\DB_backup";
            string arg1 = "\"" + @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = "root";
            string arg3 = "Integra2022/";
            string arg4 = dbName;
            //string arg5 = @"C:\Temp\Backups\" + "2022.10.17_08.54.30_backup_db1";
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
                if (AlarmManagement.alarms[4, 0].Status == AlarmStatus.ACTIVE || AlarmManagement.alarms[4, 0].Status == AlarmStatus.ACK) AlarmManagement.InactivateAlarm(4, 0);

                // c'est peut-être nul ça (je veux dire la gestion du mutex), il faut ajouter un wait non ?
                logger.Warn("c'est peut-être nul ça (je veux dire la gestion du mutex), il faut ajouter un wait non ?");

                auditTrailInfo.columns[auditTrailInfo.username].value = Settings.Default.SystemUsername;
                auditTrailInfo.columns[auditTrailInfo.eventType].value = "Evènement";
                auditTrailInfo.columns[auditTrailInfo.description].value = General.auditTrail_BackupDesc;
                MyDatabase.InsertRow(auditTrailInfo, mutex);
                //MyDatabase.InsertRow_done_old("audit_trail", "event_type, username, description", new string[] { "Evènement", username, General.auditTrail_BackupDesc }, mutex: mutex);

                //MyDatabase.Disconnect();

                General.count = 40;
                logger.Info("Backup réussi");
                //MessageBox.Show("Backup réussi");
                isBackupSucceeded = true;

                DirectoryInfo backupDirInfo = new DirectoryInfo(backupPath);
                logger.Trace(backupDirInfo.FullName);
                //FileInfo[] files = backupDirInfo.GetFiles("*" + backupExtFile);
                FileSystemInfo[] files = backupDirInfo.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                logger.Trace(files.Length);
                List<FileSystemInfo> orderedFiles = files.OrderBy(f => f.CreationTime).ToList();
                // files.Where(f => f.Extension.Equals(".sql"))
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
                            //MessageBox.Show(orderedFiles[i].CreationTime.ToString() + " deleted");
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
                logger.Error("Backup échoué");
                //MessageBox.Show("Backup échoué");
            }
            General.count = 0;
            General.text = "";

            process.Close();
            return isBackupSucceeded;
        }
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (lbBackups.SelectedItem != null)
            {
                string restoreFileName = (lbBackups.SelectedItem as ListBoxItem).Content.ToString();
                nLines = File.ReadAllLines(backupPath + restoreFileName).Length + 900;

                wpStatus.Visibility = Visibility.Visible;

                Task task = Task.Factory.StartNew(() => ExecuteRestore(General.loggedUsername, restoreFileName));

                while (!task.IsCompleted)
                {
                    progressBar.Value = (double)(100 * General.count / nLines);
                    await Task.Delay(500);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un fichier à restorer");
            }
        }
        private void ExecuteRestore(string username, string restoreFileName)
        {
            if (File.Exists(backupPath + restoreFileName))
            {
                string batchFile = @".\Resources\DB_restore";
                string arg1 = "\"" + @"C:\Program Files\MariaDB 10.9\bin" + "\"";
                string arg2 = "root";
                string arg3 = "Integra2022/";
                string arg4 = dbName;
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
                    
                    auditTrailInfo.columns[auditTrailInfo.username].value = username;
                    auditTrailInfo.columns[auditTrailInfo.eventType].value = "Evènement";
                    auditTrailInfo.columns[auditTrailInfo.description].value = General.auditTrail_RestoreDesc;
                    MyDatabase.InsertRow(auditTrailInfo);
                    //MyDatabase.InsertRow_done_old("audit_trail", "event_type, username, description", new string[] { "Evènement", username, General.auditTrail_RestoreDesc });
                    MyDatabase.Disconnect();

                    General.count = nLines;
                    MessageBox.Show("Restore réussi");
                }
                else
                {
                    MessageBox.Show("Restore échoué");
                }
                General.count = 0;
                General.text = "";

                process.Close();
            }
            else
            {
                MessageBox.Show("Le fichier " + backupPath + restoreFileName + " est introuvable");
            }
/*

            //*/
        }
        private void progressBar_Loaded(object sender, RoutedEventArgs e)
        {
            progressBar.Width = this.ActualWidth - labelStatus.ActualWidth - 20;
        }
    }
}
