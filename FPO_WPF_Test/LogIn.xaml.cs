using FPO_WPF_Test.Properties;
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

namespace FPO_WPF_Test
{
    /// <summary>
    /// Logique d'interaction pour LogIn.xaml
    /// </summary>
    public partial class LogIn : Window
    {
        readonly MainWindow mainWindow;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public LogIn(MainWindow window)
        {
            logger.Debug("Start");

            mainWindow = window;
            InitializeComponent();
        }
        private void Click()
        {
            logger.Debug("Click");

            PrincipalContext pc = new PrincipalContext(ContextType.Domain);
            bool isCredentialValid = pc.ValidateCredentials(username.Text, password.Password);

            if (isCredentialValid)
            {
                if (username.Text.ToLower() == "julien.aquilon") General.ShowMessageBox("Salut Chef");

                string role = UserManagement.UpdateAccessTable(username.Text);
                mainWindow.UpdateUser(username.Text, role);

                this.Close();
            }
            else
            {
                General.ShowMessageBox(Settings.Default.LogIn_Info_PswIncorrect);
            }
        }
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonOk_Click");

            Click();
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
                Click();
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
