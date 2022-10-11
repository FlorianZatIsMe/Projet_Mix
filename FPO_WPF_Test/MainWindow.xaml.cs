using System;
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

        public MainWindow()
        {/*
            ReportGeneration report = new ReportGeneration();
            report.pdfGenerator("194");
            MessageBox.Show("Fini");
            Close();//*/

            string[] values = new string[] { WindowsIdentity.GetCurrent().Name, "Evènement", "Démarrage de l'application"};
            MyDatabase.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

            InitializeComponent();

            UpdateUser(username: UserPrincipal.Current.DisplayName.ToLower(), 
                role: UserManagement.UpdateAccessTable(UserPrincipal.Current.DisplayName));
            labelSoftwareName.Text = General.application_name + " version " + General.application_version;

            if (true) frameMain.Content = new Pages.Status();
            frameInfoCycle.Content = null;

            // 
            //
            //
            // ICI : PENSER A INITIALISER LA BALANCE ET LA POMPE ET PEUT-ÊTRE LE COLD TRAP
            //
            //
            // 

            RS232Weight.Open();
            RS232Pump.Open();

            if (RS232Pump.IsOpen())
            {
                RS232Pump.BlockUse();
                RS232Pump.SetCommand("!C802 0");
                RS232Pump.FreeUse();
            }

            //SpeedMixerModbus.Connect();
        }
        ~MainWindow()
        {
            //MessageBox.Show("Au revoir");
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
            frameMain.Content = new Pages.ActiveAlarms();
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
            Close();
        }
    }
}