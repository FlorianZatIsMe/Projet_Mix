using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using Main.Properties;
using Message;
using System.Globalization;

namespace Main.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour PreCycle.xaml
    /// </summary>
    public partial class PreCycle : UserControl
    {
        private readonly ContentControl contentControlMain = new ContentControl();
        private readonly ContentControl contentControlInfoCycle = new ContentControl();
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<int> ProgramIDs = new List<int>();
        private bool isCbxRecipeAvailable = false;
        private int finalWeightMin = 0;
        private int finalWeightMax = 0;
        private MainWindow mainWindow;

        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PreCycle(ContentControl contentControlMain, ContentControl contentControlInfoCycle, MainWindow mainWindow)
        {
            logger.Debug("Start");

            this.contentControlMain = contentControlMain;
            this.contentControlInfoCycle = contentControlInfoCycle;
            this.mainWindow = mainWindow;
            InitializeComponent();

            General.Update_RecipeNames(cbxRecipeName, ProgramNames, ProgramIDs, RecipeStatus.PROD);
            SetFinalWeightRange(-1);
            isCbxRecipeAvailable = true;
        }
        private void FxOK(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxOK");
            tbOk.IsEnabled = false;

            if (!VerifyFormatBatchJob(tbJobNumber.Text)) goto End;
            if (!VerifyFormatBatchJob(tbBatchNumber.Text)) goto End;

            int recipeIndex = -1;
            if ((bool)tgBarcodeOption.IsChecked)
            {
                if (cbxRecipeName.SelectedIndex == -1)
                {
                    MyMessageBox.Show(Settings.Default.PreCycle_SelectProduct);
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
                    MyMessageBox.Show(Settings.Default.PreCycle_IncorrectProduct);
                    goto End;
                }
            }

            if (recipeIndex == -1)
            {
                logger.Error("Drôle d'erreur");
                MyMessageBox.Show("Drôle d'erreur");
                goto End;
            }

            try
            {
                if (int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) < finalWeightMin || int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) > finalWeightMax)
                {
                    MyMessageBox.Show(
                        Settings.Default.PreCycle_IncorrectFinalWeight1 + 
                        finalWeightMin.ToString() + 
                        Settings.Default.PreCycle_IncorrectFinalWeight2 + 
                        finalWeightMax.ToString() + 
                        Settings.Default.PreCycle_IncorrectFinalWeight3);
                    goto End;
                }
            }
            catch (Exception)
            {
                MyMessageBox.Show(
                    Settings.Default.PreCycle_IncorrectFinalWeight1 +
                    finalWeightMin.ToString() +
                    Settings.Default.PreCycle_IncorrectFinalWeight2 +
                    finalWeightMax.ToString() +
                    Settings.Default.PreCycle_IncorrectFinalWeight3);
                goto End;
            }

            CycleStartInfo info;
            info.recipeID = ProgramIDs[recipeIndex];
            info.JobNumber = tbJobNumber.Text;
            info.BatchNumber = tbBatchNumber.Text;
            info.finalWeight = tbFinalWeight.Text;
            info.frameMain = null;
            info.frameInfoCycle = null;
            info.contentControlMain = contentControlMain;
            info.contentControlInfoCycle = contentControlInfoCycle;
            info.isTest = false;
            info.bowlWeight = "";

            if (MyMessageBox.Show(Settings.Default.PreCycle_Request_StartCycle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                info.contentControlMain.Content = new CycleWeight(CurrentPhase.BowlWeight, info);
            }
        End:
            tbOk.IsEnabled = true;
        }

        private bool VerifyFormatBatchJob(string text)
        {
            string messageRequiredFormat = 
                Settings.Default.PreCycle_JobBatchNumber_IncorrectFormat1 + 
                Settings.Default.PreCycle_JobBatchNumber_NbChar.ToString() + " " + 
                (Settings.Default.PreCycle_JobBatchNumber_IsNumber ? 
                Settings.Default.PreCycle_JobBatchNumber_IncorrectFormat2_num : Settings.Default.PreCycle_JobBatchNumber_IncorrectFormat2_char) + 
                Settings.Default.PreCycle_JobBatchNumber_IncorrectFormat3 +
                text.Length.ToString() + " " + Settings.Default.PreCycle_JobBatchNumber_NbChar.ToString();

            if (text.Length != Settings.Default.PreCycle_JobBatchNumber_NbChar)
            {
                MyMessageBox.Show(messageRequiredFormat);
                return false;
            }

            if (Settings.Default.PreCycle_JobBatchNumber_IsNumber)
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
            contentControlMain.Content = new Status();
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
            if (informUser) MyMessageBox.Show(Settings.Default.PreCycle_IncorrectProduct);
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
                logger.Error(ex.Message + " " + recipeValues[recipeInfo.FinaleWeightMin].ToString() + " " + recipeValues[recipeInfo.FinaleWeightMax].ToString());
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

        private void tbJobBatchNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox.Text != "")
            {
                VerifyFormatBatchJob(textBox.Text);
                General.HideKeyBoard();
            }
        }
        private void tbRecipeName_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("tbRecipeName_LostFocus");

            if (tbRecipeName.Text != "")
            {
                SetFinalWeightRangeFromTextBox();
            }
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

        private bool isHideKeyBoardIfEnterOnGoing = false;

        private async void HideKeyBoardIfEnter(object sender, KeyEventArgs e)
        {
            if (isHideKeyBoardIfEnterOnGoing) return;

            isHideKeyBoardIfEnterOnGoing = true;

            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                TextBox textBox = sender as TextBox;
                //MyMessageBox.Show(textBox.Text);

                if (textBox == tbJobNumber)
                {
                    if (VerifyFormatBatchJob(textBox.Text))
                    {
                        tbBatchNumber.Focus();
                    }
                    else
                    {
                        goto End;
                    }
                }
                else if (textBox == tbBatchNumber)
                {
                    if (VerifyFormatBatchJob(textBox.Text))
                    {
                        tbRecipeName.Focus();
                    }
                    else
                    {
                        goto End;
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
                        goto End;
                    }
                }
                else
                {
                    General.HideKeyBoard();
                }
            }
        End:
            await Task.Delay(100);
            isHideKeyBoardIfEnterOnGoing = false;
        }
    }
}
