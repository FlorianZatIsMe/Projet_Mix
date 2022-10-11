using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
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
        public LogIn(MainWindow window)
        {
            mainWindow = window;
            InitializeComponent();
        }
        private void Click()
        {
            PrincipalContext pc = new PrincipalContext(ContextType.Domain);
            bool isCredentialValid = pc.ValidateCredentials(username.Text, password.Password);

            if (isCredentialValid)
            {
                if (username.Text.ToLower() == "julien.aquilon") MessageBox.Show("Salut Chef");

                string role = UserManagement.UpdateAccessTable(username.Text);
                mainWindow.UpdateUser(username.Text, role);
                this.Close();
            }
            else
            {
                MessageBox.Show("Nom d'utilisateur ou mot de passe incorrecte");
            }
        }
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            Click();
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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
