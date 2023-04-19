using Database;
using Main.Properties;
using Message;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using User_Management;

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
            logger.Debug("Start");
            mainWindow = parent;
            windowDeactivatedEvent = windowDeactivatedEvent_arg;
            mainWindow.Deactivated -= windowDeactivatedEvent;
            InitializeComponent();
        }
        private void Click(string user = null)
        {
            logger.Debug("Click");

            if (user == null)
            {
                UserManagement.SetNoneAccess();

                mainWindow.UpdateUser("aucun utilisateur", AccessTableInfo.NoneRole);
            }
            else
            {
                try
                {
                    PrincipalContext pc = new PrincipalContext(ContextType.Domain);
                    bool isCredentialValid = pc.ValidateCredentials(user, password.Password);

                    if (isCredentialValid)
                    {
                        if (user.ToLower() == "julien.aquilon") MyMessageBox.Show("Salut Chef");

                        string role = UserManagement.UpdateAccessTable(user);
                        mainWindow.UpdateUser(user, role);
                    }
                    else
                    {
                        MyMessageBox.Show(Settings.Default.LogIn_Info_PswIncorrect);
                        return;
                    }
                }
                catch (Exception)
                {
                    logger.Error("Problème de connexion avec l'active directory");
                    MyMessageBox.Show("Problème de connexion avec l'active directory");
                    return;
                }
            }

            if (mainWindow.frameMain.Content.GetType().GetInterface(typeof(Pages.ISubCycle).Name) != null)
            {
                Pages.ISubCycle subCycle = mainWindow.frameMain.Content as Pages.ISubCycle;
                bool[] accessTable = UserManagement.GetCurrentAccessTable();
                subCycle.EnablePage(accessTable[subCycle.IsItATest() ? AccessTableInfo.RecipeUpdate : AccessTableInfo.CycleStart]);
            }
            else
            {
                mainWindow.UpdateMenuStartCycle(true);
                mainWindow.frameMain.Content = new Pages.Status();
            }

            this.Close();
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
        }

        public async void Window_Deactivated(object sender, EventArgs e)
        {
            await Task.Delay(100);
            logger.Trace("Login");
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
        }

        private void ShowKeyBoard(object sender, RoutedEventArgs e)
        {
            General.ShowKeyBoard();
        }

        private void HideKeyBoard(object sender, RoutedEventArgs e)
        {
            General.HideKeyBoard();
        }
    }
}
