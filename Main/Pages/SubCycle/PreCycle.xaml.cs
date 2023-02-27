using Alarm_Management;
using Database;
using Driver_RS232_Pump;
using DRIVER_RS232_Weight;
using Main.Pages.SubCycle;
using Main.Properties;
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
        private readonly List<string> ProgramIDs = new List<string>();
        private bool isCbxRecipeAvailable = false;
        private int finalWeightMin = 0;
        private int finalWeightMax = 0;
        private MainWindow mainWindow;

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
            isCbxRecipeAvailable = true;
            //MyDatabase.Disconnect();
        }
        private void FxOK(object sender, RoutedEventArgs e)
        {
            logger.Debug("FxOK");

            try
            {
                if (int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) < finalWeightMin || int.Parse(tbFinalWeight.Text, NumberStyles.AllowThousands) > finalWeightMax)
                {
                    General.ShowMessageBox("C'est pas bien ce que tu fais. Min: " + finalWeightMin.ToString() + ", Max: " + finalWeightMax.ToString());
                    return;
                }
            }
            catch (Exception ex)
            {
                General.ShowMessageBox("C'est pas bien ce que tu fais. Min: " + finalWeightMin.ToString() + ", Max: " + finalWeightMax.ToString());
                return;
            }

            CycleStartInfo info;
            info.recipeID = ProgramIDs[cbxRecipeName.SelectedIndex];
            info.OFnumber = tbOFnumber.Text;
            info.finalWeight = tbFinalWeight.Text;
            info.frameMain = frameMain;
            info.frameInfoCycle = frameInfoCycle;
            info.isTest = false;
            info.bowlWeight = "";

            if (General.ShowMessageBox(Settings.Default.PreCycle_Request_StartCycle, Settings.Default.PreCycle_Request_StartCycle_Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                info.frameMain.Content = new WeightBowl(info);
            }
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

            if (cbxRecipeName.SelectedIndex != -1 && isCbxRecipeAvailable)
            {
                RecipeInfo recipeInfo;
                int index = cbxRecipeName.SelectedIndex;

                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[index]); });
                recipeInfo = (RecipeInfo)t.Result;

                try
                {
                    finalWeightMin = int.Parse(recipeInfo.Columns[recipeInfo.FinaleWeightMin].Value);
                    finalWeightMax = int.Parse(recipeInfo.Columns[recipeInfo.FinaleWeightMax].Value);
                }
                catch (Exception ex)
                {
                    General.ShowMessageBox("La recette ne précise pas de min et max pour la masse du produit" + recipeInfo.Columns[recipeInfo.FinaleWeightMin].Value + " " + recipeInfo.Columns[recipeInfo.FinaleWeightMax].Value);
                    finalWeightMin = 0;
                    finalWeightMax = 0;
                }
                lbFinalWeight.Text = "Masse cible (g) [" + finalWeightMin + " ; " + finalWeightMax + "]";
            }

        }
    }
}
