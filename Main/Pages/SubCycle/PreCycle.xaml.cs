using Alarm_Management;
using Database;
using Driver_RS232_Pump;
using DRIVER_RS232_Weight;
using Main.Pages.SubCycle;
using Main.Properties;
using Message;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
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

namespace Main.Pages.SubCycle
{
    public partial class PreCycle : Page
    {
        private readonly Frame frameMain = new Frame();
        private readonly Frame frameInfoCycle = new Frame();
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<int> ProgramIDs = new List<int>();
        private bool isCbxRecipeAvailable = false;
        private int finalWeightMin = 0;
        private int finalWeightMax = 0;
        private MainWindow mainWindow;
        private bool test;

        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PreCycle(Frame frameMain_arg, Frame inputInfoCycleFrame, MainWindow mainWindow_arg)
        {
            logger.Debug("Start");

            frameMain = frameMain_arg;
            frameInfoCycle = inputInfoCycleFrame;
            mainWindow = mainWindow_arg;
            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            InitializeComponent();
            
            General.Update_RecipeNames(cbxRecipeName, ProgramNames, ProgramIDs, RecipeStatus.PROD);
            SetFinalWeightRange(-1);
            isCbxRecipeAvailable = true;
            //MyDatabase.Disconnect();
        }
        private void FxOK(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxOK");
            tbOk.IsEnabled = false;

            if (!VerifyFormatOF()) goto End;

            try
            {
                if (int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) < finalWeightMin || int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) > finalWeightMax)
                {
                    MyMessageBox.Show("C'est pas bien ce que tu fais. Min: " + finalWeightMin.ToString() + ", Max: " + finalWeightMax.ToString());
                    goto End;
                }
            }
            catch (Exception)
            {
                MyMessageBox.Show("C'est pas bien ce que tu fais. Min: " + finalWeightMin.ToString() + ", Max: " + finalWeightMax.ToString());
                goto End;
            }

            int recipeIndex = -1;
            if ((bool)tgBarcodeOption.IsChecked)
            {
                if (cbxRecipeName.SelectedIndex == -1)
                {
                    MyMessageBox.Show("Veuillez sélectionner un code produit");
                    goto End;
                }
                recipeIndex = cbxRecipeName.SelectedIndex;
            }
            else
            {
                bool isRecipeOk = false;
                for (int i = 0; i < ProgramNames.Count; i++)
                {
                    if (tbRecipeName.Text == ProgramNames[i])
                    {
                        isRecipeOk = true;
                        recipeIndex = i;
                    }
                }
                if (!isRecipeOk)
                {
                    MyMessageBox.Show("Code produit incorrect");
                    goto End;
                }
            }

            if (recipeIndex == -1)
            {
                MyMessageBox.Show("Drôle d'erreur");
                logger.Error("Drôle d'erreur");
                goto End;
            }


            CycleStartInfo info;
            info.recipeID = ProgramIDs[recipeIndex];
            info.OFnumber = tbOFnumber.Text;
            info.finalWeight = tbFinalWeight.Text;
            info.frameMain = frameMain;
            info.frameInfoCycle = frameInfoCycle;
            info.isTest = false;
            info.bowlWeight = "";

            if (MyMessageBox.Show(Settings.Default.PreCycle_Request_StartCycle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                info.frameMain.Content = new CycleWeight(info);
            }
        End:
            tbOk.IsEnabled = true;
        }

        private bool VerifyFormatOF()
        {
            string text = tbOFnumber.Text;
            string messageRequiredFormat = "Format du numéro d'OF incorrect. " + Settings.Default.OFNumber_NbChar.ToString() + " " + (Settings.Default.OFNumber_IsNumber ? "chiffres" : "charactères") + " requis";

            if (text.Length != Settings.Default.OFNumber_NbChar)
            {
                MyMessageBox.Show(messageRequiredFormat);
                return false;
            }

            if (Settings.Default.OFNumber_IsNumber)
            {
                try
                {
                    int.Parse(text);
                }
                catch (Exception)
                {
                    MyMessageBox.Show(messageRequiredFormat);
                    return false;
                }
            }
            return true;
        }

        private void FxAnnuler(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxAnnuler");

            //MyDatabase.Disconnect();
            mainWindow.UpdateMenuStartCycle(true);
            frameMain.Content = new Status();
        }
        private void cbxProgramName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("cbxProgramName_SelectionChanged" + cbxRecipeName.SelectedIndex.ToString());
            SetFinalWeightRangeFromComboBox();
        }


        private void SetFinalWeightRangeFromComboBox()
        {
            if (cbxRecipeName.SelectedIndex != -1 && isCbxRecipeAvailable)
            {
                SetFinalWeightRange(cbxRecipeName.SelectedIndex);
            }
            else if (isCbxRecipeAvailable)
            {
                SetFinalWeightRange(-1);
            }
        }

        private bool SetFinalWeightRangeFromTextBox(bool informUser = true)
        {
            for (int i = 0; i < ProgramNames.Count; i++)
            {
                if (tbRecipeName.Text == ProgramNames[i])
                {
                    SetFinalWeightRange(i);
                    return true;
                }
            }

            SetFinalWeightRange(-1);
            if(informUser) MyMessageBox.Show("Code produit incorrect");
            return false;
        }

        private void SetFinalWeightRange(int index)
        {
            RecipeInfo recipeInfo = new RecipeInfo();

            if (index == -1)
            {
                finalWeightMin = 0;
                finalWeightMax = 0;
                lbFinalWeight.Text = Settings.Default.PreCycle_FinalWeigh_Field + finalWeightMin + " ; " + finalWeightMax + "]";
                return;
            }

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), ProgramIDs[index]); });
            object[] recipeValues = (object[])t.Result;

            try
            {
                finalWeightMin = (int)(recipeValues[recipeInfo.FinaleWeightMin]);
                finalWeightMax = (int)(recipeValues[recipeInfo.FinaleWeightMax]);
            }
            catch (Exception ex)
            {
                MyMessageBox.Show("La recette ne précise pas de min et max pour la masse du produit" + recipeValues[recipeInfo.FinaleWeightMin].ToString() + " " + recipeValues[recipeInfo.FinaleWeightMax].ToString());
                finalWeightMin = 0;
                finalWeightMax = 0;
            }
            lbFinalWeight.Text = Settings.Default.PreCycle_FinalWeigh_Field + finalWeightMin + " ; " + finalWeightMax + "]";
        }

        private void tgBarcodeOption_Click(object sender, RoutedEventArgs e)
        {
            cbxRecipeName.Visibility = (bool)tgBarcodeOption.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            tbRecipeName.Visibility = (bool)tgBarcodeOption.IsChecked ? Visibility.Collapsed : Visibility.Visible;

            if ((bool)tgBarcodeOption.IsChecked)
            {
                SetFinalWeightRangeFromComboBox();
                tgBarcodeOption.Content = Settings.Default.PreCycle_TbList_TextBox;
            }
            else
            {
                SetFinalWeightRangeFromTextBox(false);
                tgBarcodeOption.Content = Settings.Default.PreCycle_TbList_List;
            }
        }

        private void tbOFnumber_LostFocus(object sender, RoutedEventArgs e)
        {
            VerifyFormatOF();
            General.HideKeyBoard();
        }
        private void tbRecipeName_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("tbRecipeName_LostFocus");
            SetFinalWeightRangeFromTextBox();
            //if (!SetFinalWeightRangeFromTextBox()) MyMessageBox.Show("Code produit incorrect");
            General.HideKeyBoard();
        }

        private void ShowKeyBoard(object sender, RoutedEventArgs e)
        {
            General.ShowKeyBoard();
        }

        private void HideKeyBoard(object sender, RoutedEventArgs e)
        {
            General.HideKeyBoard();
        }

        private void HideKeyBoardIfEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                TextBox textBox = sender as TextBox;
                General.HideKeyBoard();

                if (textBox == tbOFnumber)
                {
                    if (VerifyFormatOF())
                    {
                        tbRecipeName.Focus();
                    }
                    else
                    {
                        General.ShowKeyBoard();
                    }
                }
                else if (textBox == tbRecipeName)
                {
                    if (SetFinalWeightRangeFromTextBox())
                    {
                        tbFinalWeight.Focus();
                    }
                    else
                    {
                        General.ShowKeyBoard();
                    }
                }
            }
        }
    }
}
