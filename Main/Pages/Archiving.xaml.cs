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
        //private readonly string dbName = DatabaseSettings.ConnectionInfo.db;
        private readonly static string archivingPath = Settings.Default.Archiving_sqlPath;// @"C:\Temp\Archives\";
        private readonly static string csvPath = Settings.Default.Archiving_csvPath;// @"C:\Temp\CSV\"; 
        private readonly static string archiveExtFile = Settings.Default.ArchBack_ExtFile;// ".sql";
        private readonly static string csvExtFile = Settings.Default.CsvExtFile;// ".csv";
        private readonly int maxArchiveCount = Settings.Default.Archiving_maxArchiveCount;// 22;
        private string lastArchiveFileName;
        private int nLines;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
                MyMessageBox.Show(Settings.Default.Archiving_Request_SelectDate);
            }
        }


        //*
        public static bool ExecuteFullArchive()
        {
            logger.Debug("ExecuteArchive");
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();

            bool isArchiveOk = true;
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

            if (ExecuteArchive_new(auditTrailInfo, firstRecordDate_s)
             && DbTableToCSV_new(auditTrailInfo, firstRecordDate_s))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(auditTrailInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive_new(cycleTableInfo, firstRecordDate_s)
             && DbTableToCSV_new(cycleTableInfo, firstRecordDate_s))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleTableInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive_new(cycleWeightInfo, firstRecordDate_s)
             && DbTableToCSV_new(cycleWeightInfo, firstRecordDate_s))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleWeightInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            if (ExecuteArchive_new(cycleSpeedMixerInfo, firstRecordDate_s)
             && DbTableToCSV_new(cycleSpeedMixerInfo, firstRecordDate_s))
            {
                // Delete rows
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(cycleSpeedMixerInfo, firstRecordDate); });
            }
            else
            {
                isArchiveOk = false;
            }

            return isArchiveOk;
        }//*/

        private static bool DbTableToCSV_new(IDtTabInfo dtTabInfo, string dateTimeColumnValue)
        {
            bool isTransferSucceeded = true;
            string fileName = csvPath + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Archiving_fileName_archive + DatabaseSettings.ConnectionInfo.Db + "_" + dtTabInfo.TabName + csvExtFile;

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
                    for (int i = 0; i < rows.Count; i++)
                    {
                        object[] row = rows[i];
                        string line_s = "";

                        for (int j = 0; j < row.Count(); j++)
                        {
                            line_s = line_s + row[j] + ",";
                        }

                        if (line_s.Length == 0)
                        {
                            isTransferSucceeded = false;
                            logger.Error("On a un problème " + fileName + ": " + row[0]);
                            MyMessageBox.Show("On a un problème " + fileName + ": " + row[0]);
                        }

                        byte[] line_b = new UTF8Encoding(true).GetBytes(line_s + "\n");
                        try
                        {
                            fs.Write(line_b, 0, line_b.Length);
                        }
                        catch (Exception ex)
                        {
                            isTransferSucceeded = false;
                            logger.Error(line_b + ex.Message);
                            MyMessageBox.Show(line_b + ex.Message);
                        }
                    }

                }
            }
            else
            {
                return false;
            }

            return isTransferSucceeded;
        }

        private static bool DbTableToCSV(IComTabInfo table, int dateTimeColumnIndex, string dateTimeColumnValue)
        {
            bool isTransferSucceeded = true;
            string fileName = csvPath + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Archiving_fileName_archive + DatabaseSettings.ConnectionInfo.Db + "_" + table.TabName + csvExtFile;

            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
            }

            if (File.Exists(fileName))
            {
                List<object[]> rows = new List<object[]>();

                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows_new(new ReadInfo(table, _customWhere: table.Ids[dateTimeColumnIndex] + "<'" + dateTimeColumnValue + "'"), nRows: -1); });
                rows = (List<object[]>)t.Result;

                using (FileStream fs = File.Open(fileName, FileMode.Append))
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        object[] row = rows[i];
                        string line_s = "";

                        for (int j = 0; j < row.Count(); j++)
                        {
                            line_s = line_s + row[j] + ",";
                        }

                        if (line_s.Length == 0)
                        {
                            isTransferSucceeded = false;
                            logger.Error("On a un problème " + fileName + ": " + row[0]);
                            MyMessageBox.Show("On a un problème " + fileName + ": " + row[0]);
                        }

                        byte[] line_b = new UTF8Encoding(true).GetBytes(line_s + "\n");
                        try
                        {
                            fs.Write(line_b, 0, line_b.Length);
                        }
                        catch (Exception ex)
                        {
                            isTransferSucceeded = false;
                            logger.Error(line_b + ex.Message);
                            MyMessageBox.Show(line_b + ex.Message);
                        }
                    }

                }
            }
            else
            {
                return false;
            }

            return isTransferSucceeded;
        }

        private static bool ExecuteArchive_new(IDtTabInfo dtTabInfo, string dateTimeColumnValue)
        {
            bool isArchiveSucceeded = false;

            string lastArchiveFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Archiving_fileName_archive + DatabaseSettings.ConnectionInfo.Db + "_" + dtTabInfo.TabName + archiveExtFile;

            string batchFile = Settings.Default.Archiving_Archive_batchFile;// @".\Resources\DB_backup_part_table";
            string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = DatabaseSettings.ConnectionInfo.UserID;// "root";
            string arg3 = General.Decrypt(DatabaseSettings.ConnectionInfo.Password, General.key);// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
            string arg5 = dtTabInfo.TabName;
            string arg6 = "\"" + dtTabInfo.Ids[dtTabInfo.DateTime] + "<'" + dateTimeColumnValue + "'" + "\"";
            string arg7 = archivingPath + lastArchiveFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5 + " " + arg6 + " " + arg7;
            //MyMessageBox.Show(command);
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(processInfo);/*
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginErrorReadLine();*/
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
            General.count = 0;

            process.Close();

            return isArchiveSucceeded;
        }
        private static bool ExecuteArchive(string tableName, string dateTimeColumnId, string dateTimeColumnValue)
        {
            bool isArchiveSucceeded = false;

            string lastArchiveFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + Settings.Default.Archiving_fileName_archive + DatabaseSettings.ConnectionInfo.Db + "_" + tableName + archiveExtFile;

            string batchFile = Settings.Default.Archiving_Archive_batchFile;// @".\Resources\DB_backup_part_table";
            string arg1 = "\"" + DatabaseSettings.DBAppFolder + "\"";// @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = DatabaseSettings.ConnectionInfo.UserID;// "root";
            string arg3 = General.Decrypt(DatabaseSettings.ConnectionInfo.Password, General.key);// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
            string arg5 = tableName;
            string arg6 = "\"" + dateTimeColumnId + "<'" + dateTimeColumnValue + "'" + "\"";
            string arg7 = archivingPath + lastArchiveFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5 + " " + arg6 + " " + arg7;
            //MyMessageBox.Show(command);
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(processInfo);/*
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                General.count++;
            };
            process.BeginErrorReadLine();*/
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
            General.count = 0;

            process.Close();

            return isArchiveSucceeded;
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
            string arg3 = General.Decrypt(DatabaseSettings.ConnectionInfo.Password, General.key);// "Integra2022/";
            string arg4 = DatabaseSettings.ConnectionInfo.Db;// dbName;
            string arg5 = "\"" + auditTrailInfo.Ids[auditTrailInfo.DateTime] + "<'" + lastRecordDate_s + "'" + "\"";
            string arg6 = archivingPath + lastArchiveFileName;
            string command = batchFile + " " + arg1 + " " + arg2 + " " + arg3 + " " + arg4 + " " + arg5 + " " + arg6;
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
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRows_new(new AuditTrailInfo(), lastRecordDate); });
                //MyDatabase.DeleteRows(new AuditTrailInfo(), lastRecordDate);

                AuditTrailInfo auditTInfo = new AuditTrailInfo();
                object[] values = new object[auditTInfo.Ids.Count()];
                values[auditTInfo.Username] = username;
                values[auditTInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
                values[auditTInfo.Description] = General.auditTrail_ArchiveDesc;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTInfo, values); });

                General.count = maxArchiveCount;

                MyMessageBox.Show(Settings.Default.Archiving_archivingSuccessfull);
                isArchiveSucceeded = true;
            }
            else
            {
                if (File.Exists(archivingPath + lastArchiveFileName)) File.Delete(archivingPath + lastArchiveFileName);
                MyMessageBox.Show(Settings.Default.Archiving_archivingFailed);
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
