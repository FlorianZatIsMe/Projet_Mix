using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Main
{
    /// <summary>
    /// Logique d'interaction pour MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
    {
        private MessageBoxResult resultButton = MessageBoxResult.None;
        private static Window parentWindow = null;

        private MyMessageBox()
        {
            InitializeComponent();
        }

        public static void SetParentWindow(Window parent)
        {
            parentWindow = parent;
        }

        public static MessageBoxResult Show(string messageBoxText, MessageBoxButton button = MessageBoxButton.OK)
        {
            MessageBoxResult result = MessageBoxResult.None;

            if (parentWindow == null)
            {
                result = Display(messageBoxText, button);
            }
            else
            {
                parentWindow.Dispatcher.Invoke(() =>
                {
                    result = Display(messageBoxText, button);
                });
            }
            return result;
        }
        private static MessageBoxResult Display(string messageBoxText, MessageBoxButton button = MessageBoxButton.OK)
        {
            MyMessageBox messageBox = new MyMessageBox();
            messageBox.labelMessage.Text = messageBoxText;
            messageBox.btOk.Visibility = button == MessageBoxButton.OK ? Visibility.Visible : Visibility.Collapsed;
            messageBox.btYes.Visibility = button == MessageBoxButton.YesNo ? Visibility.Visible : Visibility.Collapsed;
            messageBox.btNo.Visibility = button == MessageBoxButton.YesNo ? Visibility.Visible : Visibility.Collapsed;
            messageBox.ShowDialog();
            return messageBox.resultButton;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            resultButton = MessageBoxResult.OK;
            this.Close();
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            resultButton = MessageBoxResult.Yes;
            this.Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            resultButton = MessageBoxResult.No;
            this.Close();
        }
    }
}
