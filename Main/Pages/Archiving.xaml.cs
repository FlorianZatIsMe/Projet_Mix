using Alarm_Management;
using Database;
using Main.Properties;
using Message;
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

namespace Main.Pages
{
    /// <summary>
    /// Logique d'interaction pour Archiving.xaml
    /// </summary>
    public partial class Archiving : Page
    {
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private readonly static string archivingPath = Settings.Default.Archiving_sqlPath;
        private readonly static string csvPath = Settings.Default.Archiving_csvPath;
        private readonly static string archiveExtFile = Settings.Default.ArchBack_ExtFile;
        private readonly static string csvExtFile = Settings.Default.CsvExtFile;
        private readonly int maxArchiveCount = Settings.Default.Archiving_maxArchiveCount;
        private string lastArchiveFileName;
        private int nLines;
        private Frame parentFrame;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Archiving(Frame parentFrame_arg)
        {
            logger.Debug("Start");
            parentFrame = parentFrame_arg;
            InitializeComponent();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Debug("Page_Loaded");
            if (!Directory.Exists(archivingPath))
            {
                try
                {
                    Directory.CreateDirectory(archivingPath);
                }
                catch (Exception ex)
                {
                    MyMessageBox.Show("Le dossier " + archivingPath + " n'est pas accessible: " + ex.Message);
                    parentFrame.Content = new Status();
                    return;
                }
            }

            dpLastRecord.DisplayDateEnd = DateTime.Now;
            dpLastRecord.SelectedDateFormat = DatePickerFormat.Long;

            labelStatus.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            labelStatus.Arrange(new Rect(0, 0, labelStatus.DesiredSize.Width, labelStatus.DesiredSize.Height));

            UpdateBackupList();
        }
        private void UpdateBackupList()
        {
            DirectoryInfo archiveDirInfo = new DirectoryInfo(archivingPath);
            FileInfo[] files = archiveDirInfo.GetFiles("*" + archiveExtFile);

            lbArchives.Items.Clear();
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
                General.count = 0;

                Task<bool> task = Task<bool>.Factory.StartNew(() => ArchiveUntilDate(lastRecordDate, true));

                wpStatus.Visibility = Visibility.Visible;

                while (!task.IsCompleted)
                {
                    progressBar.Value = (double)(100 * General.count / maxArchiveCount);
                    await Task.Delay(Settings.Default.ArchBack_progressBar_RefreshDelay);
                }
                General.count = 0;

                if (task.Result)
                {
                    UpdateBackupList();
                    MyMessageBox.Show(Settings.Default.Archiving_archivingSuccessfull);
                }
                else
                {
                    MyMessageBox.Show(Settings.Default.Archiving_archivingFailed);
                }
            }
            else
            {
                MyMessageBox.Show(Settings.Default.Archiving_Request_SelectDate);
            }
        }


        //*
        public static bool ExecuteFullArchive()
        {
            logger.Debug("ExecuteArchive");
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(tableName: auditTrailInfo.TabName, nRow: Settings.Default.Archive_RowNumberStartArchive,orderBy: auditTrailInfo.Ids[auditTrailInfo.DateTime],isOrderAsc: false); });
            object[] row = (object[])t.Result;

            if (row == null)
            {
                MyMessageBox.Show("Il y a un problème");
                logger.Error("Il y a un problème");
                return false;
            }

            if (row.Count() != auditTrailInfo.Ids.Count())
            {
                MyMessageBox.Show("Il y a un problème");
                logger.Error("Il y a un problème");
                return false;
            }

            DateTime firstRecordDate;
            string firstRecordDate_s;
            try
            {
                firstRecordDate = Convert.ToDateTime(row[auditTrailInfo.DateTime]);
                firstRecordDate_s = firstRecordDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                MyMessageBox.Show(ex.Message);
                logger.Error(ex.Message);
                return false;
            }

            bool result = ArchiveUntilDate(firstRecordDate, false);
            General.count = 0;

            return result;
        }//*/

        private static bool ArchiveUntilDate(DateTime firstRecordDate, bool isManual)
        {
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            DailyTestInfo dailyTestInfo = new DailyTestInfo();

            bool isArchiveOk = true;
            Task<object> t;

            string firstRecordDate_s = firstRecordDate.ToString(Settings.Default.DateTime_Format_Write);// "yyyy-MM-dd HH:mm:ss");

            if (ExecuteArchive(auditTrailInfo, firstRecordDate_s, isManual)
             && DbTableToCSV(auditTrailInfo, firstRecordDate_s, isManual))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(auditTrailInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive(cycleTableInfo, firstRecordDate_s, isManual)
             && DbTableToCSV(cycleTableInfo, firstRecordDate_s, isManual))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleTableInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive(cycleWeightInfo, firstRecordDate_s, isManual)
             && DbTableToCSV(cycleWeightInfo, firstRecordDate_s, isManual))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleWeightInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive(cycleSpeedMixerInfo, firstRecordDate_s, isManual)
             && DbTableToCSV(cycleSpeedMixerInfo, firstRecordDate_s, isManual))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleSpeedMixerInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive(dailyTestInfo, firstRecordDate_s, isManual)
             && DbTableToCSV(dailyTestInfo, firstRecordDate_s, isManual))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(dailyTestInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }            

            object[] auditTrailValues = new object[auditTrailInfo.Ids.Count()];
            auditTrailValues[auditTrailInfo.Username] = General.loggedUsername;
            auditTrailValues[auditTrailInfo.EventType] = Settings.Default.AuditTrail_EventType_Event;
            auditTrailValues[auditTrailInfo.Description] = "Archivage" +
                (isManual ? " manuel" : " automatique") + 
                " jusqu'au " + 
                firstRecordDate.ToString(Settings.Default.Date_Format_Read) + 
                (isArchiveOk ? " réussi" : " échoué");

            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, auditTrailValues); });

            return isArchiveOk;
        }


        private static bool DbTableToCSV(IDtTabInfo dtTabInfo, string dateTimeColumnValue, bool isManual)
        {
            bool isTransferSucceeded = true;
            string fileName = csvPath + 
                DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + 
                Settings.Default.Archiving_fileName_archive + 
                DatabaseSettings.ConnectionInfo.Db + "_" +
                dtTabInfo.TabName +
                (isManual ? Settings.Default.ArchBack_fileName_man : Settings.Default.ArchBack_fileName_auto) +
                csvExtFile;

            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
            }

            if (File.Exists(fileName))
            {
                List<object[]> rows = new List<object[]>();
                
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows_new(new ReadInfo((IComTabInfo)dtTabInfo, _customWhere: dtTabInfo.Ids[dtTabInfo.DateTime] + "<'" + dateTimeColumnValue + "'"), nRows: -1); });
                rows = (List<object[]>)t.Result;

                using (FileStream fs = File.Open(fileName, FileMode.Append))
                {
                    if (!WriteRowToCSV(fs, dtTabInfo.Ids)) isTransferSucceeded = false;

                    for (int i = 0; i < rows.Count; i++)
                    {
                        if(!WriteRowToCSV(fs, rows[i])) isTransferSucceeded = false;
                    }
                }
            }
            else
            {
                return false;
            }

            return isTransferSucceeded;
        }

        private static bool WriteRowToCSV(FileStream fs, object[] row)
        {
            string line_s = "";
            bool isWrittingSuccessful = true;

            for (int j = 0; j < row.Count(); j++)
            {
                line_s = line_s + row[j] + ",";
            }

            if (line_s.Length == 0)
            {
                isWrittingSuccessful = false;
                logger.Error("On a un problème " + fs.Name + ": " + row[0]);
                MyMessageBox.Show("On a un problème " + fs.Name + ": " + row[0]);
            }

            byte[] line_b = new UTF8Encoding(true).GetBytes(line_s + "\n");
            try
            {
                fs.Write(line_b, 0, line_b.Length);
            }
            catch (Exception ex)
            {
                isWrittingSuccessful = false;
                logger.Error(line_b + ex.Message);
                MyMessageBox.Show(line_b + ex.Message);
            }
            return isWrittingSuccessful;
        }

        private static bool ExecuteArchive(IDtTabInfo dtTabInfo, string dateTimeColumnValue, bool isManual)
        {
            bool isArchiveSucceeded = false;

            string archiveFileName = archivingPath + 
                DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + 
                Settings.Default.Archiving_fileName_archive + 
                DatabaseSettings.ConnectionInfo.Db + "_" + 
                dtTabInfo.TabName + 
                (isManual ? Settings.Default.ArchBack_fileName_man : Settings.Default.ArchBack_fileName_auto) +
                archiveExtFile;

            string batchFile = Settings.Default.Archiving_Archive_batchFile;// @".\Resources\DB_backup_part_table";
            string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = DatabaseSettings.ConnectionInfo.UserID;// "root";
            string arg3 = General.Decrypt(DatabaseSettings.ConnectionInfo.Password, General.key);// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
            string arg5 = dtTabInfo.TabName;
            string arg6 = "\"" + dtTabInfo.Ids[dtTabInfo.DateTime] + "<'" + dateTimeColumnValue + "'" + "\"";
            string arg7 = archiveFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5 + " " + arg6 + " " + arg7;
            //MyMessageBox.Show(command);
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(processInfo);//*
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginErrorReadLine();//*/
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                isArchiveSucceeded = true;

                // Il faut supprimer toutes les lignes sauvegarder
                // audit_trail


                /*
                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows(new AuditTrailInfo(), lastRecordDate); });
                //MyDatabase.DeleteRows(new AuditTrailInfo(), lastRecordDate);

                AuditTrailInfo auditTInfo = new AuditTrailInfo();
                auditTInfo.C_olumns[auditTInfo.Username].Value = username;
                auditTInfo.C_olumns[auditTInfo.EventType].Value = Settings.Default.General_AuditTrailEvent_Event;
                auditTInfo.C_olumns[auditTInfo.Description].Value = General.auditTrail_ArchiveDesc;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTInfo); });
                //MyDatabase.InsertRow(auditTInfo);
                //MyDatabase.Disconnect();

                General.count = maxArchiveCount;

                MyMessageBox.Show(Settings.Default.Archiving_archivingSuccessfull);
                isArchiveSucceeded = true;
                */
            }
            else
            {
                //if (File.Exists(archivingPath + lastArchiveFileName)) File.Delete(archivingPath + lastArchiveFileName);

                // audit trail (idem partout)

                MyMessageBox.Show(Settings.Default.Archiving_archivingFailed);
            }
            //General.count = 0;

            process.Close();

            return isArchiveSucceeded;
        }
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Restore_Click");

            if (lbArchives.SelectedItem != null)
            {
                string restoreFileName = (lbArchives.SelectedItem as ListBoxItem).Content.ToString();

                if (!File.Exists(archivingPath + restoreFileName))
                {
                    MyMessageBox.Show("Fichier " + archivingPath + restoreFileName + " n'existe pas");
                    UpdateBackupList();
                    return;
                }

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
                MyMessageBox.Show(Settings.Default.ArchBack_Request_SelectFile);
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
                string arg3 = General.Decrypt(DatabaseSettings.ConnectionInfo.Password, General.key);// "Integra2022/";
                string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
                string arg5 = archivingPath + restoreFileName;
                string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5;
                //MyMessageBox.Show(command);    
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
                    AuditTrailInfo auditTInfo = new AuditTrailInfo();
                    object[] values = new object[auditTInfo.Ids.Count()];
                    values[auditTInfo.Username] = username;
                    values[auditTInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
                    values[auditTInfo.Description] = General.auditTrail_RestArchDesc;

                    // A CORRIGER : IF RESULT IS FALSE
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTInfo, values); });

                    General.count = nLines;
                    MyMessageBox.Show(Settings.Default.ArchBack_restoreSuccessfull);
                }
                else
                {
                    MyMessageBox.Show(Settings.Default.ArchBack_restoreFailed);
                }
                General.count = 0;
                General.text = "";

                process.Close();
            }
            else
            {
                MyMessageBox.Show(Settings.Default.ArchBack_FileNotFound_1 + archivingPath + restoreFileName + Settings.Default.ArchBack_FileNotFound_2);
            }
        }
        private void progressBar_Loaded(object sender, RoutedEventArgs e)
        {
            logger.Debug("progressBar_Loaded");

            progressBar.Width = this.ActualWidth - labelStatus.ActualWidth - Settings.Default.ArchBack_progressBar_LeftMargin;
        }
    }
}
