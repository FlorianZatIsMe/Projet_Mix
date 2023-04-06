using System;
using System.Collections.Generic;
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

namespace Message
{
    /// <summary>
    /// Logique d'interaction pour MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
    {
        private MessageBoxResult resultButton = MessageBoxResult.None;
        private static Window parentWindow = null;
        private static EventHandler windowDeactivatedEvent;

        private static List<MyMessageBox> myMessageBoxes = new List<MyMessageBox>();
        private int myMessageBoxId;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private MyMessageBox()
        {
            InitializeComponent();
        }

        public static void SetParentWindow(Window parent, EventHandler windowDeactivatedEvent_arg = null)
        {
            parentWindow = parent;
            windowDeactivatedEvent = windowDeactivatedEvent_arg;
        }

        public static MessageBoxResult Show(string messageBoxText, MessageBoxButton button = MessageBoxButton.OK)
        {
            MessageBoxResult result = MessageBoxResult.None;

            if (parentWindow == null)
            {
                result = Display(messageBoxText + " C'est bizarre tout ça, vraiment bizarre", button);
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
            messageBox.myMessageBoxId = myMessageBoxes.Count;
            myMessageBoxes.Add(messageBox);

            logger.Trace("Display: " + messageBoxText + ", myMessageBoxId = " + messageBox.myMessageBoxId.ToString());
            if (parentWindow != null && messageBox.myMessageBoxId == 0)
            {
                logger.Trace("On retire le parent: " + parentWindow.ToString());
                parentWindow.Deactivated -= windowDeactivatedEvent;
            }

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

        private async void Window_Deactivated(object sender, EventArgs e)
        {
            await Task.Delay(100);
            if (myMessageBoxes.Count == this.myMessageBoxId + 1)
            {
                logger.Trace("Window_Deactivated " + this.myMessageBoxId.ToString());
                this.Activate();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            logger.Trace("Closing");
            this.Deactivated -= Window_Deactivated;
            myMessageBoxes.RemoveAt(this.myMessageBoxId);

            for (int i = 0; i < myMessageBoxes.Count; i++)
            {
                logger.Trace(i.ToString() + ": " + myMessageBoxes[i].myMessageBoxId.ToString());
            }

            if (parentWindow != null && myMessageBoxes.Count == 0)
            {
                logger.Trace("On active papa");
                parentWindow.Activate();
                parentWindow.Deactivated += windowDeactivatedEvent;
            }
            else if (myMessageBoxes.Count > 0)
            {
                logger.Trace("On active la boîte " + (myMessageBoxes.Count - 1).ToString());
                myMessageBoxes[myMessageBoxes.Count - 1].Activate();
            }
        }
    }
}
