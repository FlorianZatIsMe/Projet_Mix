using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using MySqlConnector;
using System.Collections;
using System.Collections.Specialized;
using Driver.MODBUS;
using Database;
using Driver.ColdTrap;
using System.Globalization;
using DRIVER.RS232.Weight;
using Driver.RS232.Pump;
using System.Threading.Tasks;
using Alarm_Management;
using System.Security.Principal;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

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
        private readonly MyDatabase db;
        private readonly NameValueCollection AuditTrailSettings = ConfigurationManager.GetSection("Database/Audit_Trail") as NameValueCollection;

        bool IsInGroup(string user, string group)
        {
            //using (var identity = new WindowsIdentity(user))
            {
                //WindowsIdentity identity = new WindowsIdentity(WindowsIdentity.GetCurrent().Name);
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return principal.IsInRole(group);
            }
        }

        private void pdfGenerator()
        {
            // Check http://www.pdfsharp.net/wiki/PDFsharpSamples.ashx
            string filename = String.Format("{0}_tempfile.pdf", Guid.NewGuid().ToString("D").ToUpper());

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            PdfDocument s_document = new PdfDocument();

            s_document = new PdfDocument();/*
            s_document.Info.Title = "This is the title";
            s_document.Info.Author = WindowsIdentity.GetCurrent().Name;
            s_document.Info.Subject = "this is the subject";
            s_document.Info.Keywords = "What do you want, me?";*/

            PdfPage page = s_document.AddPage();
            //MessageBox.Show(filename);
            s_document.Save(@"C:\Temp\Sample_1.pdf");
        }

        private void pdfGenerator1()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            PdfDocument document = new PdfDocument();

            PdfPage page = document.AddPage();

            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont font = new XFont("Arial", 20);

            gfx.DrawString("First line of text", font, XBrushes.Black,
                new XRect(0, 0, page.Width, page.Height),
                XStringFormats.Center);

            gfx.DrawString("Second line of text", font, XBrushes.BlueViolet,
                new XRect(0, 0, page.Width, page.Height),
                XStringFormats.BottomLeft);

            gfx.DrawString("Third line of text", font, XBrushes.Red, new XPoint(100, 300));

            document.Save(@"C:\Temp\C'est moi qui la fait.pdf");
        }

        public MainWindow()
        {
            db = new MyDatabase();
            pdfGenerator();
            //MessageBox.Show(IsInGroup("", @"BUILTIN\Users").ToString());

            string[] values = new string[] { WindowsIdentity.GetCurrent().Name, "Démarrage de l'application"};
            db.InsertRow(AuditTrailSettings["Table_Name"], AuditTrailSettings["Insert_UserDesc"], values);

            InitializeComponent();

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
            frameMain.Content = new Pages.Recipe(Action.Modify);
        }
        private void FxProgramCopy(object sender, RoutedEventArgs e)
        {

        }
        private void FxProgramDelete(object sender, RoutedEventArgs e)
        {

        }
        private async void FxAuditTrail(object sender, RoutedEventArgs e)
        {
            //while(frameInfoCycle != null) await Task.Delay(25);
            frameMain.Content = new Pages.AuditTrail();
        }
        private void FxAlarms(object sender, RoutedEventArgs e)
        {

        }
        private void FxUserLogInOut(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Opérateur (Oui), Superviseur (Non) ou Administareur (Annuler)", "Qui-es tu ?", button: MessageBoxButton.YesNoCancel);

            switch (result.ToString())
            {
                case "Yes":
                    General.Role = "Operator";
                    break;
                case "No":
                    General.Role = "Supervisor";
                    break;
                case "Cancel":
                    General.Role = "Administrator";
                    break;
                default:
                    break;
            }

            MessageBox.Show(General.Role);
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