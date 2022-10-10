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

namespace Graphics
{
    /// <summary>
    /// The base class with some helper functions.
    /// </summary>
    public class Base
    {
        public XColor BackColor;
        public XColor BackColor2;
        public XColor ShadowColor;
        public double BorderWidth;
        public XPen BorderPen;

        public Base()
        {
            BackColor = XColors.Ivory;
            BackColor2 = XColors.WhiteSmoke;

            BackColor = XColor.FromArgb(212, 224, 240);
            BackColor2 = XColor.FromArgb(253, 254, 254);

            ShadowColor = XColors.Gainsboro;
            BorderWidth = 4.5;
            BorderPen = new XPen(XColor.FromArgb(94, 118, 151), BorderWidth);
        }

        /// <summary>
        /// Draws the page title and footer.
        /// </summary>
        public void DrawTitle(PdfPage page, XGraphics gfx, string title, PdfDocument Document)
        {
            var rect = new XRect(new XPoint(), gfx.PageSize);
            rect.Inflate(-10, -15);
            var font = new XFont("Verdana", 14, XFontStyle.Bold);
            gfx.DrawString(title, font, XBrushes.MidnightBlue, rect, XStringFormats.TopCenter);

            rect.Offset(0, 5);
            font = new XFont("Verdana", 8, XFontStyle.Italic);
            var format = new XStringFormat();
            format.Alignment = XStringAlignment.Near;
            format.LineAlignment = XLineAlignment.Far;
            gfx.DrawString("Created with " + PdfSharp.ProductVersionInfo.Producer, font, XBrushes.DarkOrchid, rect, format);

            font = new XFont("Verdana", 8);
            format.Alignment = XStringAlignment.Center;
            gfx.DrawString(Document.PageCount.ToString(CultureInfo.InvariantCulture), font, XBrushes.DarkOrchid, rect, format);

            Document.Outlines.Add(title, page, true);
        }

        /// <summary>
        /// Draws a sample box.
        /// </summary>
        public void BeginBox(XGraphics gfx, int number, string title)
        {
            const int dEllipse = 15;
            var rect = new XRect(0, 20, 300, 200);
            if (number % 2 == 0)
                rect.X = 300 - 5;
            rect.Y = 40 + ((number - 1) / 2) * (200 - 5);
            rect.Inflate(-10, -10);
            var rect2 = rect;
            rect2.Offset(BorderWidth, BorderWidth);
            gfx.DrawRoundedRectangle(new XSolidBrush(ShadowColor), rect2, new XSize(dEllipse + 8, dEllipse + 8));
            var brush = new XLinearGradientBrush(rect, BackColor, BackColor2, XLinearGradientMode.Vertical);
            gfx.DrawRoundedRectangle(BorderPen, brush, rect, new XSize(dEllipse, dEllipse));
            rect.Inflate(-5, -5);

            var font = new XFont("Verdana", 12, XFontStyle.Regular);
            gfx.DrawString(title, font, XBrushes.Navy, rect, XStringFormats.TopCenter);

            rect.Inflate(-10, -5);
            rect.Y += 20;
            rect.Height -= 20;

            _state = gfx.Save();
            gfx.TranslateTransform(rect.X, rect.Y);
        }

        public void EndBox(XGraphics gfx)
        {
            gfx.Restore(_state);
        }

        /// <summary>
        /// Gets a five-pointed star with the specified size and center.
        /// </summary>
        public static XPoint[] GetPentagram(double size, XPoint center)
        {
            var points = (XPoint[])Pentagram.Clone();
            for (var idx = 0; idx < 5; idx++)
            {
                points[idx].X = points[idx].X * size + center.X;
                points[idx].Y = points[idx].Y * size + center.Y;
            }
            return points;
        }

        /// <summary>
        /// Gets a normalized five-pointed star.
        /// </summary>
        static XPoint[] Pentagram
        {
            get
            {
                if (_pentagram == null)
                {
                    var order = new[] { 0, 3, 1, 4, 2 };
                    _pentagram = new XPoint[5];
                    for (var idx = 0; idx < 5; idx++)
                    {
                        var rad = order[idx] * 2 * Math.PI / 5 - Math.PI / 10;
                        _pentagram[idx].X = Math.Cos(rad);
                        _pentagram[idx].Y = Math.Sin(rad);
                    }
                }
                return _pentagram;
            }
        }
        static XPoint[] _pentagram;

        public void DrawMessage(XGraphics gfx, string message)
        {
            var font = new XFont("PlatformDefault", 12, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

            gfx.DrawString(message, font, XBrushes.DarkSlateGray, 10, 10);
        }

        XGraphicsState _state;
    }
}



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
            report.pdfGenerator("191");
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
        private async void FxAuditTrail(object sender, RoutedEventArgs e)
        {
            //while(frameInfoCycle != null) await Task.Delay(25);
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