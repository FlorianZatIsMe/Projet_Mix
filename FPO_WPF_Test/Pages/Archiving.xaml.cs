using Alarm_Management;
using Database;
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
        private static readonly string dbName = "db1";
        private static readonly string archivingPath = @"C:\Temp\Archives\";
        private static readonly string archiveExtFile = ".sql";
        private static readonly int maxArchiveCount = 22;
        private static string lastArchiveFileName;
        private static int nLines;

        public Archiving()
        {
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
            if(dpLastRecord.SelectedDate != null)
            {
                //string lastRecordDate = ((DateTime)dpLastRecord.SelectedDate).AddDays(1).ToString("yyyy-MM-dd HH:mm:ss");
                DateTime lastRecordDate = ((DateTime)dpLastRecord.SelectedDate).AddDays(1);

                Task<bool> task = Task<bool>.Factory.StartNew(() => ExecuteArchive(General.loggedUsername, lastRecordDate));

                wpStatus.Visibility = Visibility.Visible;

                while (!task.IsCompleted)
                {
                    progressBar.Value = (double)(100 * General.count / maxArchiveCount);
                    await Task.Delay(10);
                }

                if (task.Result) lbArchives.Items.Insert(0, new ListBoxItem() { Content = lastArchiveFileName });
                lastArchiveFileName = "";
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une date");
            }
        }
        public static bool ExecuteArchive(string username, DateTime lastRecordDate)
        {
            bool isArchiveSucceeded = false;
            string lastRecordDate_s = lastRecordDate.ToString("yyyy-MM-dd HH:mm:ss");

            lastArchiveFileName = DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + "_archive_" + dbName + (username == "système" ? "_auto" : "_man") + archiveExtFile;

            string batchFile = @".\Resources\DB_backup_part_table";
            string arg1 = "\"" + @"C:\Program Files\MariaDB 10.9\bin" + "\"";
            string arg2 = "root";
            string arg3 = "Integra2022/";
            string arg4 = dbName;
            string arg5 = "\"" + "date_time<'" + lastRecordDate_s + "'" + "\"";
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
                if (!MyDatabase.IsConnected()) MyDatabase.Connect();
                MyDatabase.DeleteRows("audit_trail", lastRecordDate);
                MyDatabase.InsertRow_old("audit_trail", "event_type, username, description", new string[] { "Evènement", username, General.auditTrail_ArchiveDesc });
                MyDatabase.Disconnect();

                General.count = maxArchiveCount;

                MessageBox.Show("Archive réussi");
                isArchiveSucceeded = true;
            }
            else
            {
                if (File.Exists(archivingPath + lastArchiveFileName)) File.Delete(archivingPath + lastArchiveFileName);
                MessageBox.Show("Archive échoué");
            }
            General.count = 0;

            process.Close();

            return isArchiveSucceeded;
        }
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (lbArchives.SelectedItem != null)
            {
                string restoreFileName = (lbArchives.SelectedItem as ListBoxItem).Content.ToString();
                nLines = File.ReadAllLines(archivingPath + restoreFileName).Length + 900;

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
        public static void ExecuteRestore(string username, string restoreFileName)
        {
            if (File.Exists(archivingPath + restoreFileName))
            {
                string batchFile = @".\Resources\DB_restore";
                string arg1 = "\"" + @"C:\Program Files\MariaDB 10.9\bin" + "\"";
                string arg2 = "root";
                string arg3 = "Integra2022/";
                string arg4 = dbName;
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
                    if (!MyDatabase.IsConnected()) MyDatabase.Connect();
                    MyDatabase.InsertRow_old("audit_trail", "event_type, username, description", new string[] { "Evènement", username, General.auditTrail_RestArchDesc });
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
                MessageBox.Show("Le fichier " + archivingPath + restoreFileName + " est introuvable");
            }
            /*

                        //*/
        }
        private void DpLastRecord_LayoutUpdated(object sender, EventArgs e)
        {

        }

        private void progressBar_Loaded(object sender, RoutedEventArgs e)
        {
            progressBar.Width = this.ActualWidth - labelStatus.ActualWidth - 20;
        }
    }
}
