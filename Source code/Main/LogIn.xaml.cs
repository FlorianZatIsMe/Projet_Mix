
using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;

using Database;
using Message;
using User_Management;
using Main.Properties;

namespace Main
{
    /// <summary>
    /// Logique d'interaction pour LogIn.xaml
    /// </summary>
    public partial class LogIn : Window
    {
        private MainWindow mainWindow = null;
        private EventHandler windowDeactivatedEvent;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public LogIn(MainWindow parent, EventHandler windowDeactivatedEvent_arg)
        {
            logger.Debug("Start");/*
            mainWindow = parent;
            windowDeactivatedEvent = windowDeactivatedEvent_arg;
            mainWindow.Deactivated -= windowDeactivatedEvent;
            MyMessageBox.SetParentWindow(this, this.Window_Deactivated);
            InitializeComponent();
            Left = (System.Windows.SystemParameters.WorkArea.Width - Width) / 2;
            Top = 100;
            username.Focus();*/


            mainWindow = parent;
            windowDeactivatedEvent = windowDeactivatedEvent_arg;
            mainWindow.Deactivated -= windowDeactivatedEvent;
            //MyMessageBox.SetParentWindow(this, this.Window_Deactivated);
            InitializeComponent();
            Left = (System.Windows.SystemParameters.WorkArea.Width - Width) / 2;
            Top = 100;
        }
        private void Click(string user = null)
        {
            logger.Debug("Click");
            string role = "";
            btConnect.IsEnabled = false;
            Impersonator.ResetPassword();
            //Impersonator.password = Settings.Default.Default_Password;

            if (user == null)
            {
                UserManagement.SetNoneAccess();
                mainWindow.UpdateUser(Settings.Default.General_NoneUsername, AccessTableInfo.NoneRole);
            }
            else
            {
                role = UserManagement.UpdateAccessTable(user, password.Password);

                if (role == AccessTableInfo.NoneRole)
                {
                    //UserManagement.SetNoneAccess();
                    mainWindow.UpdateUser(Settings.Default.General_NoneUsername, AccessTableInfo.NoneRole);
                }
                else
                {
                    mainWindow.UpdateUser(user, UserManagement.UpdateAccessTable(user, password.Password));
                    Impersonator.password = password.Password;
                }
            }

            if (mainWindow.contentControlMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
            {
                Pages.ISubCycle subCycle = mainWindow.contentControlMain.Content as Pages.ISubCycle;
                bool[] accessTable = UserManagement.GetCurrentAccessTable();
                subCycle.EnablePage(accessTable[subCycle.IsItATest() ? AccessTableInfo.RecipeUpdate : AccessTableInfo.CycleStart]);
            }
            else
            {
                mainWindow.UpdateMenuStartCycle(true);
                mainWindow.contentControlMain.Content = new Pages.Status();
            }

            if (role != AccessTableInfo.NoneRole) this.Close();
            else MyMessageBox.Show("L'utilisateur n'a pas accès à l'application");
            btConnect.IsEnabled = true;
            //this.PreviewKeyDown += Window_PreviewKeyDown;
        }
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonOk_Click");
            Click(username.Text);
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonCancel_Click");

            this.Close();
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("Window_PreviewKeyDown");

            if (e.Key == Key.Enter)
            {
                General.HideKeyBoard();
                Click(username.Text);
            }
            else if (e.Key == Key.Escape)
            {
                General.HideKeyBoard();
                this.Close();
            }
        }

        private void ButtonLogOff_Click(object sender, RoutedEventArgs e)
        {
            Click();

            // Terminer l'impersonation
            //_context?.Undo();
        }

        public async void Window_Deactivated(object sender, EventArgs e)
        {
            await Task.Delay(100);
            logger.Trace("Window_Deactivated");
            this.Activate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Deactivated -= Window_Deactivated;
            mainWindow.Deactivated += windowDeactivatedEvent;
            MyMessageBox.SetParentWindow(mainWindow, windowDeactivatedEvent);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyMessageBox.SetParentWindow(this, this.Window_Deactivated);
            this.username.Focus();
        }

        private void ShowKeyBoard(object sender, RoutedEventArgs e)
        {
            General.ShowKeyBoard();
        }

        private void HideKeyBoard(object sender, RoutedEventArgs e)
        {
            General.HideKeyBoard();
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword,
            int dwLogonType, int dwLogonProvider, out IntPtr phToken);
    }
}
