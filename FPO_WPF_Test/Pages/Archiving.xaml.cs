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
    /// Logique d'interaction pour Archiving.xaml
    /// </summary>
    public partial class Archiving : Page
    {
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        //private readonly string dbName = DatabaseSettings.ConnectionInfo.db;
        private readonly string archivingPath = Settings.Default.Archiving_archivingPath;// @"C:\Temp\Archives\"; 
        private readonly string archiveExtFile = Settings.Default.ArchBack_ExtFile;// ".sql";
        private readonly int maxArchiveCount = Settings.Default.Archiving_maxArchiveCount;// 22;
        private string lastArchiveFileName;
        private int nLines;

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Archiving()
        {
            logger.Debug("Start");

            if (!Directory.Exists(archivingPath))
            {
                Directory.CreateDirectory(archivingPath);
            }

            InitializeComponent();
            dpLastRecord.DisplayDateEnd = DateTime.Now;
            dpLastRecord.SelectedDateFormat = DatePickerFormat.Long;

            labelStatus.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            labelStatus.Arrange(new Rect(0, 0, labelStatus.DesiredSize.Width, labelStatus.DesiredSize.Height));

            DirectoryInfo archiveDirInfo = new DirectoryInfo(archivingPath);

            FileInfo[] files = archiveDirInfo.GetFiles("*" + archiveExtFile);

            lbArchives.Items.SortDescriptions.Clear();
            lbArchives.Items.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Descending));
            for (int i = 0; i < files.Length; i++)
            {
                lbArchives.Items.Add(new ListBoxItem() { Content = files[i].Name });
            }
            lbArchives.Items.Refresh();
        }
        private async void Archive_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Archive_Click");

            if (dpLastRecord.SelectedDate != null)
            {
                DateTime lastRecordDate = ((DateTime)dpLastRecord.SelectedDate).AddDays(1);

                Task<bool> task = Task<bool>.Factory.StartNew(() => ExecuteArchive(General.loggedUsername, lastRecordDate));

                wpStatus.Visibility = Visibility.Visible;

                while (!task.IsCompleted)
                {
                    progressBar.Value = (double)(100 * General.count / maxArchiveCount);
                    await Task.Delay(Settings.Default.ArchBack_progressBar_RefreshDelay);
                }

                if (task.Result) lbArchives.Items.Insert(0, new ListBoxItem() { Content = lastArchiveFileName });
                lastArchiveFileName = "";
            }
            else
            {
                MessageBox.Show(Settings.Default.Archiving_Request_SelectDate);
            }
        }
        public bool ExecuteArchive(string username, DateTime lastRecordDate)
        {
            logger.Debug("ExecuteArchive");

            bool isArchiveSucceeded = false;
            string lastRecordDate_s = lastRecordDate.ToString("yyyy-MM-dd HH:mm:ss");

            lastArchiveFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Archiving_fileName_archive + DatabaseSettings.ConnectionInfo.Db + (username == Settings.Default.General_SystemUsername ? Settings.Default.ArchBack_fileName_auto : Settings.Default.ArchBack_fileName_man) + archiveExtFile;

            string batchFile = Settings.Default.Archiving_Archive_batchFile;// @".\Resources\DB_backup_part_table";
            string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = DatabaseSettings.ConnectionInfo.UserID;// "root";
            string arg3 = DatabaseSettings.ConnectionInfo.Password;// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
            string arg5 = "\"" + auditTrailInfo.Columns[auditTrailInfo.DateTime].Id + "<'" + lastRecordDate_s + "'" + "\"";
            string arg6 = archivingPath + lastArchiveFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5 + " " + arg6;
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
                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows(new AuditTrailInfo(), lastRecordDate); });
                //MyDatabase.DeleteRows(new AuditTrailInfo(), lastRecordDate);

                AuditTrailInfo auditTInfo = new AuditTrailInfo();
                auditTInfo.Columns[auditTInfo.Username].Value = username;
                auditTInfo.Columns[auditTInfo.EventType].Value = Settings.Default.General_AuditTrailEvent_Event;
                auditTInfo.Columns[auditTInfo.Description].Value = General.auditTrail_ArchiveDesc;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTInfo); });
                //MyDatabase.InsertRow(auditTInfo);
                //MyDatabase.Disconnect();

                General.count = maxArchiveCount;

                MessageBox.Show(Settings.Default.Archiving_archivingSuccessfull);
                isArchiveSucceeded = true;
            }
            else
            {
                if (File.Exists(archivingPath + lastArchiveFileName)) File.Delete(archivingPath + lastArchiveFileName);
                MessageBox.Show(Settings.Default.Archiving_archivingFailed);
            }
            General.count = 0;

            process.Close();

            return isArchiveSucceeded;
        }
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Restore_Click");

            if (lbArchives.SelectedItem != null)
            {
                string restoreFileName = (lbArchives.SelectedItem as ListBoxItem).Content.ToString();
                nLines = File.ReadAllLines(archivingPath + restoreFileName).Length + Settings.Default.Archiving_nLines_offset;

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
        public void ExecuteRestore(string username, string restoreFileName)
        {
            logger.Debug("ExecuteRestore");

            if (File.Exists(archivingPath + restoreFileName))
            {
                string batchFile = Settings.Default.Archiving_Restore_batchFile;// @".\Resources\DB_restore";
                string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
                string arg2 = DatabaseSettings.ConnectionInfo.UserID;// "root";
                string arg3 = DatabaseSettings.ConnectionInfo.Password;// "Integra2022/";
                string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
                string arg5 = archivingPath + restoreFileName;
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
                    //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                    AuditTrailInfo auditTInfo = new AuditTrailInfo();
                    auditTInfo.Columns[auditTInfo.Username].Value = username;
                    auditTInfo.Columns[auditTInfo.EventType].Value = Settings.Default.General_AuditTrailEvent_Event;
                    auditTInfo.Columns[auditTInfo.Description].Value = General.auditTrail_RestArchDesc;

                    // A CORRIGER : IF RESULT IS FALSE
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTInfo); });
                    //MyDatabase.InsertRow(auditTInfo);
                    //MyDatabase.Disconnect();

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
                MessageBox.Show(Settings.Default.ArchBack_FileNotFound_1 + archivingPath + restoreFileName + Settings.Default.ArchBack_FileNotFound_2);
            }
        }
        private void progressBar_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Debug("progressBar_Loaded");

            progressBar.Width = this.ActualWidth - labelStatus.ActualWidth - Settings.Default.ArchBack_progressBar_LeftMargin;
        }
    }
}
