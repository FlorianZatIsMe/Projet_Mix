using Database;
using Main.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Diagnostics;
using Main.Pages.SubCycle;

namespace Main.Pages
{
    public interface ISubRecipe
    {
        //int seqType { get; }
        void SetPage(ISeqTabInfo seqInfo);
        ISeqTabInfo GetPage();
        bool IsFormatOk();
        void SetSeqNumber(int n);
    }
    public interface ISubCycle
    {
        void StopCycle();
        void EnablePage(bool enable);
        bool IsItATest();
    }
    public class SubCycleArg
    {
        public Frame frameMain { get; }
        public Frame frameInfoCycle { get; }
        public string id { get; }
        public int idCycle { get; }
        public int idPrevious { get; }
        public string tablePrevious { get; }
        public ISeqTabInfo prevSeqInfo { get; }
        public bool isTest { get; }

        public SubCycleArg(Frame frameMain_arg, Frame frameInfoCycle_arg, string id_arg, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqTabInfo prevSeqInfo_arg, bool isTest_arg = true)
        {
            frameMain = frameMain_arg;
            frameInfoCycle = frameInfoCycle_arg;
            id = id_arg;
            idCycle = idCycle_arg;
            idPrevious = idPrevious_arg;
            tablePrevious = tablePrevious_arg;
            prevSeqInfo = prevSeqInfo_arg;
            isTest = isTest_arg;
        }
    }
    public class Seq
    {
        public Type subRcpPgType { get; set; }
        public ISeqTabInfo subRecipeInfo { get; set; }
        public Type subCycPgType { get; set; }
        public ICycleSeqInfo subCycleInfo { get; set; }
    }
    public class Sequence
    {
        public static readonly List<Seq> list = new List<Seq>()
        {
            new Seq()
            {
                subRcpPgType = typeof(Pages.SubRecipe.Weight),
                subRecipeInfo = new RecipeWeightInfo(),
                subCycPgType = typeof(Pages.SubCycle.WeightBowl),
                //subCycPgType = typeof(Pages.SubCycle.CycleWeight),
                subCycleInfo = new CycleWeightInfo()
            },

            new Seq()
            {
                subRcpPgType = typeof(Pages.SubRecipe.SpeedMixer),
                subRecipeInfo = new RecipeSpeedMixerInfo(),
                subCycPgType =  typeof(Pages.SubCycle.CycleSpeedMixer),
                subCycleInfo = new CycleSpeedMixerInfo()
            }
        };
    }

    //
    // Classe utilisée pour lister les contrôles des sous-recette (ex: Weight, Speedmixer)
    //
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class IdDBControls
    {
        public List<int> list { get; set; }
    }

    public partial class Recipe : Page
    {
        private readonly RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
        private readonly RecipeInfo recipeInfo = new RecipeInfo();
        private int nRow;
        private readonly StringCollection status = Settings.Default.Recipe_Status_DescList;
        private bool isFrameLoaded;
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<string> ProgramIDs = new List<string>();
        private readonly List<string> ProgramVersions = new List<string>();
        private readonly bool isCbxToModifAvailable = false;
        private bool isCbxToCopyAvailable = false;
        private bool isCbxVersionCpyAvailable = false;
        private readonly bool isCbxToDeleteAvailable = false;
        private string currentRecipeVersion;
        private string currentRecipeStatus;
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private bool curMethodDoneOnGoing;
        private string finalWeightMin = "";
        private string finalWeightMax = "";
        private Process keyBoardProcess;
        private MainWindow mainWindow;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Recipe(RcpAction action, Frame frameMain_arg = null, Frame frameInfoCycle_arg = null, string recipeName = "", MainWindow window = null)
        {
            logger.Debug("Start");

            nRow = 1;
            frameMain = frameMain_arg;
            isFrameLoaded = false;
            mainWindow = window;

            InitializeComponent();

            switch (action)
            {
                case RcpAction.New:
                    gridNewRecipe.Visibility = Visibility.Visible;
                    Create_NewSequence(recipeWeightInfo.SeqType.ToString());// MySettings["SubRecipeWeight_SeqType"]);
                    break;
                case RcpAction.Modify: // pour ça je pense qu'une comboBox est suffisant, on puet imaginer une fenêtre intermédiaire avec une liste et une champ pour filtrer mais ça me semble pas applicable à notre besoin
                    frameInfoCycle = frameInfoCycle_arg;
                    if(mainWindow == null)
                    {
                        General.ShowMessageBox("La fenêtre principale n'a pas été définie");
                        logger.Error("La fenêtre principale n'a pas été définie");
                        return;
                    }
                    gridModify_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToModify, ProgramNames, ProgramIDs, Database.RecipeStatus.PRODnDRAFT);
                 
                    isCbxToModifAvailable = true;

                    if (recipeName != "")
                    {
                        for (int i = 0; i < ProgramNames.Count; i++)
                        {
                            if (ProgramNames[i] == recipeName)
                            {
                                cbxPgmToModify.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    break;
                case RcpAction.Copy:
                    gridCopy_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                    isCbxToCopyAvailable = true;
                    break;
                case RcpAction.Delete:
                    gridDelete_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                    isCbxToDeleteAvailable = true;
                    break;
                default:
                    break;
            }

            // Si on a testé une recette et qu'on revient ici alors on ne se déconnecte pas
            //if (action != Action.Modify || recipeName == "") //MyDatabase.Disconnect();
        }

        /* Create_NewRecipe()
         * 
         * Crée une nouvelle recette à partir des valeurs de l'interface graphique
         * S'assure que le format des paramètres est correct
         * 
         * Remarque pour celui qui utilise cette méthode: assure toi que le programme est connecté à la base de données et déconnecte toi après
         * 
         */
        private bool Create_NewRecipe(string recipeName, int new_version, RecipeStatus status, bool isRecipeCreated = true)
        {
            logger.Debug("Create_NewRecipe");

            int i = 1;
            int n;
            string[] values = new string[1];
            List<string[]> allValues = new List<string[]>();
            bool isFormatOk = true;
            string row;
            List<ISeqTabInfo> seqInfoList = new List<ISeqTabInfo>();
            ISubRecipe recipeSeq;
            Task<object> t;
            /*
            if (!MyDatabase.IsConnected()) {
                General.ShowMessageBox(DatabaseSettings.Error01);
                return false;
            }*/

            if (recipeName == "") {
                General.ShowMessageBox(Settings.Default.Recipe_Request_FillRecipeName);
                return false;
            }

            if (finalWeightMin == "" || finalWeightMax == "")
            {
                General.ShowMessageBox("Gamme de la masse final incorrecte");
                return false;
            }

            if (new_version <= 0)
            {
                logger.Error(Settings.Default.Recipe_Error_IncorrectVersion);
                General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectVersion);
                return false;
            }

            // Si on créer une nouvelle recette (version = 1)
            // On contrôle si la recette n'existe pas déjà
            RecipeInfo recipeInfo = new RecipeInfo();
            recipeInfo.Columns[recipeInfo.Name].Value = recipeName;


            if (new_version == 1 && 
                isRecipeCreated &&
                (int)(MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo, recipeInfo.Columns[recipeInfo.Version].Id); }).Result) != 0) {
                General.ShowMessageBox(Settings.Default.Recipe_Info_ExistingRecipe);
                return false;
            }
            /*
            if (new_version == 1 && 
                isRecipeCreated &&
                MyDatabase.GetMax(recipeInfo, recipeInfo.columns[recipeInfo.version].id) != 0) {
                General.ShowMessageBox(Settings.Default.Recipe_Info_ExistingRecipe);
                return false;
            }
             */

            // pourquoi faire ça, je ne sais plus, à voir...
            //using (RecipeInfo recipeInfo = new RecipeInfo()) {}

            recipeInfo = new RecipeInfo();
            recipeInfo.Columns[recipeInfo.Name].Value = recipeName;
            recipeInfo.Columns[recipeInfo.Version].Value = new_version.ToString();
            recipeInfo.Columns[recipeInfo.FinaleWeightMin].Value = int.Parse(finalWeightMin, NumberStyles.AllowThousands).ToString();
            recipeInfo.Columns[recipeInfo.FinaleWeightMax].Value = int.Parse(finalWeightMax, NumberStyles.AllowThousands).ToString();

            recipeInfo.Columns[recipeInfo.Status].Value = MyDatabase.GetRecipeStatus(status).ToString();
            //recipeInfo.columns[recipeInfo.status].value = MyDatabase.GetRecipeStatus(status).ToString();

            seqInfoList.Add(recipeInfo);

            foreach (UIElement element in gridMain.Children) // Pour chaque element de la grille principale (gridMain)...
            {
                if (element.GetType().Equals(typeof(Frame))) // Si c'est une frame...
                {
                    Frame frame = element as Frame;

                    if (frame.Content.GetType().GetInterface(typeof(ISubRecipe).Name) == null)
                    {
                        logger.Error(frame.Content.GetType().ToString());
                        continue; // je crois que c'est juste à tester
                    }

                    recipeSeq = frame.Content as ISubRecipe;

                    isFormatOk = recipeSeq.IsFormatOk();
                    if (isFormatOk) seqInfoList.Add(recipeSeq.GetPage());
                    else break;
                }
            }

            // Si toutes les séquences ne sont pas correctement renseignées, on sort de là
            if (!isFormatOk) {
                General.ShowMessageBox(Settings.Default.Recipe_Info_IncorrectFormat);
                return false;
            }

            bool isRecordOk;
            n = seqInfoList.Count();
            isRecordOk = true;

            for (i = n - 1; i > 0; i--)
            {
                if (isRecordOk)
                {
                    row = "InsertRow " + i.ToString() + " - ";
                    for (int j = 0; j < seqInfoList[i].Columns.Count(); j++)
                    {
                        row = row + seqInfoList[i].Columns[j].Id + ": " + seqInfoList[i].Columns[j].Value + " ";
                    }
                    logger.Trace(row);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[i]); });
                    isRecordOk = (bool)t.Result;
                    //isRecordOk = MyDatabase.InsertRow(seqInfoList[i]);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(seqInfoList[i].TabName, seqInfoList[i].Columns[seqInfoList[i].Id].Id); });
                    seqInfoList[i - 1].Columns[seqInfoList[i - 1].NextSeqId].Value = ((int)t.Result).ToString();
                    //seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqId].value = MyDatabase.GetMax(seqInfoList[i].name, seqInfoList[i].columns[seqInfoList[i].id].id).ToString();
                    seqInfoList[i - 1].Columns[seqInfoList[i - 1].NextSeqType].Value = seqInfoList[i].SeqType.ToString();
                }
                else break;
            }
            if (isRecordOk) isRecordOk = (bool)MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[0]); }).Result;
            //if (isRecordOk) isRecordOk = MyDatabase.InsertRow(seqInfoList[0]);

            if (isRecordOk) {
                if (new_version == 1) General.ShowMessageBox(Settings.Default.Recipe_Info_RecipeCreated);
                else General.ShowMessageBox(Settings.Default.Recipe_Info_RecipeModified);
                return true;
            }
            // S'il y a eu une erreur, on supprime les lignes qui ont été créés.
            else if (i != n - 2)
            {
                i++;

                do
                {
                    logger.Trace("DeleteRow " + i.ToString() + ": " + seqInfoList[i + 1].TabName + " " + 
                        seqInfoList[i].Columns[seqInfoList[i].NextSeqId].Id + " " +
                        seqInfoList[i].Columns[seqInfoList[i].NextSeqId].Value);

                    string id = seqInfoList[i].Columns[seqInfoList[i].NextSeqId].Value;
                    ISeqTabInfo seqTabInfo = seqInfoList[i + 1];

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(seqTabInfo, id); });
                    //MyDatabase.DeleteRow(seqInfoList[i + 1], seqInfoList[i].columns[seqInfoList[i].nextSeqId].value);

                    i++;
                } while (seqInfoList[i].Columns[seqInfoList[i].NextSeqId].Value != null);
            }

            General.ShowMessageBox(Settings.Default.Recipe_Info_RecipeNotCreated);
            return false;
        }
        private async void Display_Recipe(string id)
        {
            logger.Debug("Display_Recipe");
            Task<object> t;

            string nextSeqType;
            string nextSeqID;
            Frame currentFrame;
            List<Frame> framesToDelete = new List<Frame>();
            RecipeInfo recipeInfo = new RecipeInfo();
            ISubRecipe currentPage;
            ISeqTabInfo currentRecipeSeq;
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                General.ShowMessageBox(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), id); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), id);

            if (recipeInfo.Columns == null) // Si la requête envoyer ne contient qu'une seule ligne
            {
                General.ShowMessageBox(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            nextSeqType = recipeInfo.Columns[recipeInfo.NextSeqType].Value;
            nextSeqID = recipeInfo.Columns[recipeInfo.NextSeqId].Value;
            currentRecipeVersion = recipeInfo.Columns[recipeInfo.Version].Value;
            currentRecipeStatus = status[int.Parse(recipeInfo.Columns[recipeInfo.Status].Value)];
            tbWeightMinModif.Text = recipeInfo.Columns[recipeInfo.FinaleWeightMin].Value;
            tbWeightMaxModif.Text = recipeInfo.Columns[recipeInfo.FinaleWeightMax].Value;
            finalWeightMin = tbWeightMinModif.Text;
            finalWeightMax = tbWeightMaxModif.Text;

            foreach (UIElement element in gridMain.Children) // Pour chaque element de la grille principale (gridMain)...
            {
                if (element.GetType().Equals(typeof(Frame))) // Si c'est une frame...
                {
                    framesToDelete.Add(element as Frame); // On l'ajoute dans la liste des frame à supprimer
                }
            }

            // On supprime toutes les frame existantes
            foreach (Frame frame in framesToDelete)
            {
                isFrameLoaded = false;
                frame.Content = null;
                while (!isFrameLoaded) await Task.Delay(Settings.Default.Recipe_WaitFrameLoaded_Delay); // On attend que la frame ai bien été supprimée
            }

            do // On remplie les frames ici
            {
                isFrameLoaded = false;
                Create_NewSequence(nextSeqType);

                while (!isFrameLoaded) await Task.Delay(Settings.Default.Recipe_WaitFrameLoaded_Delay); // On attend que la frame créée (New_Sequence(nextSeqType == "1");) a été chargé
                isFrameLoaded = false;

                if (gridMain.Children[gridMain.Children.Count - 1].GetType().Equals(typeof(Frame))) // Si le dernier élément de la grille gridMain est une frame on continue
                {
                    //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID); });
                    currentRecipeSeq = (ISeqTabInfo)t.Result;
                    //currentRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID);
                    currentFrame = gridMain.Children[gridMain.Children.Count - 1] as Frame;

                    currentPage = currentFrame.Content as ISubRecipe;

                    if (currentRecipeSeq != null)
                    {
                        currentPage.SetPage(currentRecipeSeq);
                        nextSeqType = currentRecipeSeq.Columns[currentRecipeSeq.NextSeqType].Value;
                        nextSeqID = currentRecipeSeq.Columns[currentRecipeSeq.NextSeqId].Value;
                    }
                    else
                    {
                        General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectRecipe);
                        nextSeqID = "";
                    }
                }
                else
                {
                    General.ShowMessageBox(Settings.Default.Recipe_Error_FrameNotSeen);
                }
            } while (nextSeqID != "");

            curMethodDoneOnGoing = false;
        }
        private void Create_NewSequence(string seqType)
        {
            logger.Debug("Create_NewSequence");

            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            frame.ContentRendered += SubRecipeFrame_ContentRendered;
            //frame.PreviewMouseDoubleClick += FrameTest;

            frame.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(seqType)].subRcpPgType, new object[] { frame, nRow.ToString() });

            Grid.SetRow(frame, gridMain.RowDefinitions.Count() - 1);
            gridMain.Children.Add(frame);
            nRow++;
        }
        private void FrameTest(object sender, MouseButtonEventArgs e)
        {
            logger.Debug("FrameTest");

            Frame frame = sender as Frame;

            if (frame.Content.GetType().GetInterface(typeof(ISubRecipe).Name) != null)
            {
                ISubRecipe recipeSeq = frame.Content as ISubRecipe;
                ISeqTabInfo seqInfo = recipeSeq.GetPage();

                string row = "";
                for (int j = 0; j < seqInfo.Columns.Count(); j++)
                {
                    row = row + seqInfo.Columns[j].Value + " ";
                }

                General.ShowMessageBox(row);
            }
            else
            {
                General.ShowMessageBox(frame.Content.GetType().ToString());
            }

        }
        private void Update_SequenceNumbers()
        {
            logger.Debug("Update_SequenceNumbers");

            int i = 1;
            ISubRecipe subRecipe;

            foreach (UIElement element in gridMain.Children)
            {
                if (element.GetType().Equals(typeof(Frame)))
                {
                    Frame frame = element as Frame;

                    if (frame.Content.GetType().Equals(typeof(ISubRecipe)))
                    {
                        subRecipe = frame.Content as ISubRecipe;
                        subRecipe.SetSeqNumber(i);
                        i++;
                    }
                }
            }
        }
        private void Delete_Recipe(string id)
        {
            logger.Debug("Delete_Recipe");

            ISeqTabInfo subRecipeSeq;
            string nextSeqType;
            string nextSeqId;
            Task<object> t;

            /*
            if (!MyDatabase.IsConnected()) // while loop is better
            {
                General.ShowMessageBox(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(new RecipeInfo().GetType(), id); });
            RecipeInfo recipeInfo = (RecipeInfo)t.Result;
            //RecipeInfo recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(new RecipeInfo().GetType(), id);

            if (recipeInfo == null)
            {
                General.ShowMessageBox(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            nextSeqType = recipeInfo.Columns[recipeInfo.NextSeqType].Value;
            nextSeqId = recipeInfo.Columns[recipeInfo.NextSeqId].Value;
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(new RecipeInfo(), id); });
            //MyDatabase.DeleteRow(new RecipeInfo(), id);

            while (nextSeqType != "" && nextSeqType != null)
            {
                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqId); });
                subRecipeSeq = (ISeqTabInfo)t.Result;
                //subRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSetId);

                if (subRecipeSeq != null)
                {
                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(subRecipeSeq, nextSeqId); });
                    t.Wait();
                    //MyDatabase.DeleteRow(subRecipeSeq, nextSetId);
                    nextSeqType = subRecipeSeq.Columns[subRecipeSeq.NextSeqType].Value;
                    nextSeqId = subRecipeSeq.Columns[subRecipeSeq.NextSeqId].Value;
                }
                else
                {
                    nextSeqType = "";
                    General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectRecipe);
                }
            }
        }

        // EVENTS

        private void SubRecipeFrame_ContentRendered(object sender, EventArgs e)
        {
            logger.Debug("SubRecipeFrame_ContentRendered");

            Frame frame = sender as Frame;

            if (frame.Content == null)
            {
                Grid grid = frame.Parent as Grid;
                grid.Children.Remove(frame);
                Update_SequenceNumbers();
                nRow--;
            }
            isFrameLoaded = true;
        }
        private void New_Sequence_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("New_Sequence_Click");

            Create_NewSequence(recipeWeightInfo.SeqType.ToString());
        }
        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonCreate_Click");

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            Create_NewRecipe(tbRecipeNameNew.Text, 1, RecipeStatus.DRAFT);
            //MyDatabase.Disconnect();
        }
        private async void CbxPgmToModify_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxPgmToModify_SelectionChanged");

            if (isCbxToModifAvailable)
            {
                logger.Trace("CbxPgmToModify_SelectionChanged");

                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                labelVersion.Text = currentRecipeVersion;
                labelStatus.Text = currentRecipeStatus;

                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay); 

                panelInfoRecipe.Visibility = Visibility.Visible;
                panelTestRecipe.Visibility = Visibility.Visible;
                //MyDatabase.Disconnect();
            }
        }
        private void ButtonModify_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonModify_Click");

            int currentIndex = cbxPgmToModify.SelectedIndex;
            Task<object> t;

            // A CORRIGER : IF RESULT IS FALSE
            //if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)])
            if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)])
            {
                if (General.ShowMessageBox(Settings.Default.Recipe_Request_UpdateProdRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Création d'une nouvelle recette, l'ancienne version sera obsolète
                    if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text) + 1, RecipeStatus.DRAFT, false))
                    {
                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.TabName, recipeInfo.Columns[recipeInfo.Id].Id); });
                        ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                        //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id).ToString();

                        labelStatus.Text = status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)];
                        //labelStatus.Text = status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)];
                        labelVersion.Text = (int.Parse(labelVersion.Text) + 1).ToString();
                    }
                }
            }
            //else if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)])
            else if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)])
            {
                bool isStillDraft = false;

                if (General.ShowMessageBox(Settings.Default.Recipe_Request_UpdateDraftRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (General.ShowMessageBox(Settings.Default.Recipe_Request_UpdateDraftRecipe_YOU_SURE, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Modification de la recette puis modification du status en draft
                        if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text), RecipeStatus.PROD, false))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.TabName, recipeInfo.Columns[recipeInfo.Id].Id); });
                            ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                            //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id).ToString();

                            // Get the new recipe
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex].ToString()); });
                            RecipeInfo oldRecipe = (RecipeInfo)t.Result;

                            // Get the previous version of the recipe (Create new recipe and set recipe name and version)
                            string name = oldRecipe.Columns[oldRecipe.Name].Value;
                            int version = int.Parse(oldRecipe.Columns[oldRecipe.Version].Value);
                            oldRecipe = new RecipeInfo();
                            oldRecipe.Columns[oldRecipe.Name].Value = name;
                            oldRecipe.Columns[oldRecipe.Version].Value = version.ToString();
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(table: oldRecipe); });
                            oldRecipe = (RecipeInfo)t.Result;

                            if (oldRecipe == null)
                            {
                                labelStatus.Text = "###";
                                General.ShowMessageBox("Problème dans la création de la recette");
                                return;
                            }

                            // Update the status of the previous version of the recipe to Obsolete
                            string id = oldRecipe.Columns[oldRecipe.Id].Value;
                            oldRecipe = new RecipeInfo();
                            oldRecipe.Columns[recipeInfo.Status].Value = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();
                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(oldRecipe, id); });

                            labelStatus.Text = status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)];
                        }
                    }
                    else
                    {
                        isStillDraft = true;
                    }
                }
                else
                {
                    isStillDraft = true;
                }

                if(isStillDraft)
                {
                    // Modification de la recette draft en cours
                    if (Create_NewRecipe(ProgramNames[cbxPgmToModify.SelectedIndex], int.Parse(labelVersion.Text), RecipeStatus.DRAFT, false))
                    {
                        Delete_Recipe(ProgramIDs[currentIndex]);

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.TabName, recipeInfo.Columns[recipeInfo.Id].Id); });
                        ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                    }
                }
            }
            else
            {
                General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectStatus);
            }
        }
        private void ButtonActDel_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonActDel_Click");

            int currentIndex = cbxPgmToActDelete.SelectedIndex;
            RecipeInfo recipeInfo;
            RecipeInfo recipeToUpdate;
            string recipeId = ProgramIDs[currentIndex];
            Task<object> t;


            //MyDatabase.Connect();
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                General.ShowMessageBox(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), recipeId); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex]);

            if (recipeInfo == null)
            {
                General.ShowMessageBox(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
                //goto End;
            }


            if ((bool)rbDelete.IsChecked)
            {
                //if (recipeInfo.columns[recipeInfo.status].value == MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString())
                if (recipeInfo.Columns[recipeInfo.Status].Value == MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString())
                {
                    if (General.ShowMessageBox(Settings.Default.Recipe_Request_DelProdRecipe1 + 
                        recipeInfo.Columns[recipeInfo.Name].DisplayName + " " + recipeInfo.Columns[recipeInfo.Name].Value + " " + 
                        recipeInfo.Columns[recipeInfo.Version].DisplayName + " " + recipeInfo.Columns[recipeInfo.Version].Value + 
                        Settings.Default.Recipe_Request_DelProdRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        recipeToUpdate = new RecipeInfo();
                        recipeToUpdate.Columns[recipeToUpdate.Status].Value = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, recipeId); });
                        //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                        ProgramIDs.RemoveAt(currentIndex);
                        ProgramNames.RemoveAt(currentIndex);

                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        General.ShowMessageBox(Settings.Default.Recipe_Info_DelProdDone);
                    }
                }
                else if (recipeInfo.Columns[recipeInfo.Status].Value == MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT).ToString())
                {
                    if (General.ShowMessageBox(Settings.Default.Recipe_Request_DelDraftRecipe1 + 
                        recipeInfo.Columns[recipeInfo.Name].DisplayName + " " + recipeInfo.Columns[recipeInfo.Name].Value + " " + 
                        recipeInfo.Columns[recipeInfo.Version].DisplayName + " " + recipeInfo.Columns[recipeInfo.Version].Value + 
                        Settings.Default.Recipe_Request_DelDraftRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Delete_Recipe(recipeId);

                        if (int.Parse(recipeInfo.Columns[recipeInfo.Version].Value) > 1)
                        {
                            RecipeInfo recipe = new RecipeInfo();
                            recipe.Columns[recipe.Name].Value = recipeInfo.Columns[recipeInfo.Name].Value;

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipe, recipe.Columns[recipe.Id].Id); });
                            ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                            recipeId = ProgramIDs[currentIndex];
                            //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipe, recipe.columns[recipe.id].id).ToString();
                            /*
                            recipeToUpdate = new RecipeInfo();
                            recipeToUpdate.Columns[recipeToUpdate.Status].Value = MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString();

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, recipeId); });
                            */
                            //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);
                        }
                        else if (int.Parse(recipeInfo.Columns[recipeInfo.Version].Value) == 1)
                        {
                            ProgramIDs.RemoveAt(currentIndex);
                            ProgramNames.RemoveAt(currentIndex);
                        }
                        else
                        {
                            General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectVersion + ": " + recipeInfo.Columns[recipeInfo.Version].Value);
                        }

                        // mettre ça dans une fonction et on recommence tout
                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        General.ShowMessageBox(Settings.Default.Recipe_Info_DelDraftDone);
                    }
                }
                else
                {
                    General.ShowMessageBox(Settings.Default.Recipe_Error_IncorrectStatus + ": " + status[int.Parse(recipeInfo.Columns[recipeInfo.Status].Value)]);
                }
            }
            else if ((bool)rbActivate.IsChecked)
            {
                if (General.ShowMessageBox(Settings.Default.Recipe_Request_ActRecipe1 + 
                    recipeInfo.Columns[recipeInfo.Name].Value + 
                    " version " + recipeInfo.Columns[recipeInfo.Version].Value + 
                    Settings.Default.Recipe_Request_ActRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // using partout
                    recipeToUpdate = new RecipeInfo();
                    recipeToUpdate.Columns[recipeToUpdate.Status].Value = MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString();

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, recipeId); });
                    //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                    ProgramIDs.RemoveAt(currentIndex);
                    ProgramNames.RemoveAt(currentIndex);

                    CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                    General.ShowMessageBox(Settings.Default.Recipe_Info_ActDone);
                }
            }
            else
            {
                logger.Error(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
                General.ShowMessageBox(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
            }
        //End:
            //MyDatabase.Disconnect();
        }
        private void CbxAddDefaultText(ComboBox comboBox, int index)
        {
            logger.Debug("CbxAddDefaultText");

            int index0 = (ProgramIDs.Count > 0 && index == 0) ? 1 : 0;

            if (ProgramIDs.Count == 0)
            {
                ProgramNames.Insert(0, Settings.Default.Recipe_Request_SelectRecipe);
                ProgramNames.Insert(0, Settings.Default.Recipe_Request_SelectRecipe);
                comboBox.SelectedIndex = 1;
                ProgramNames.RemoveAt(0);
                ProgramNames.RemoveAt(0);
            }
            else
            {
                ProgramNames.Insert(index0, Settings.Default.Recipe_Request_SelectRecipe);
                comboBox.SelectedIndex = index0;
                ProgramNames.RemoveAt(index0);
            }

            comboBox.Items.Refresh();
        }
        private void RbActivateDelete_Checked(object sender, RoutedEventArgs e)
        {
            logger.Debug("RbActivateDelete_Checked");

            if (isCbxToDeleteAvailable)
            {
                RadioButton radioButton = sender as RadioButton;

                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                if (radioButton == rbActivate)
                {
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, RecipeStatus.OBSOLETE);
                    btDelAct.Content = Settings.Default.Recipe_btDelAct_Activate;
                }
                else if (radioButton == rbDelete)
                {
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                    btDelAct.Content = Settings.Default.Recipe_btDelAct_Delete;
                }
                //MyDatabase.Disconnect();
            }
        }
        private void Test_Sequence_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Test_Sequence_Click");

            int finalWeight;
            int min;
            int max;

            try
            {
                finalWeight = int.Parse(tbFinaleWeight.Text);
                min = int.Parse(tbWeightMinModif.Text);
                max = int.Parse(tbWeightMaxModif.Text);

                if (finalWeight < min || finalWeight > max)
                {
                    finalWeight = -1;
                }
            }
            catch (Exception)
            {
                finalWeight = -1;
            }

            if (finalWeight == -1)
            {
                General.ShowMessageBox(Settings.Default.Cycle_Info_FinalWeightIncorrect);
                return;
            }

            if (General.ShowMessageBox(Settings.Default.Recipe_Request_TestRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string id = ProgramIDs[cbxPgmToModify.SelectedIndex];
                CycleStartInfo info;
                info.recipeID = id;
                info.OFnumber = Settings.Default.General_na;
                info.finalWeight = finalWeight.ToString();
                info.frameMain = frameMain;
                info.frameInfoCycle = frameInfoCycle;
                info.isTest = true;
                info.bowlWeight = "";
                info.frameMain.Content = new WeightBowl(info);
                mainWindow.UpdateMenuStartCycle(false);
            }
        }
        private async void CbxPgmToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxPgmToCopy_SelectionChanged");
            Task<object> t;

            if (isCbxToCopyAvailable)
            {
                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                isCbxVersionCpyAvailable = false;

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                ProgramVersions.Clear();

                //if (MyDatabase.IsConnected())
                if (true)
                {
                    RecipeInfo recipeInfo = new RecipeInfo();
                    recipeInfo.Columns[recipeInfo.Name].Value = ProgramNames[comboBox.SelectedIndex];

                    ReadInfo readInfo = new ReadInfo(
                        _tableInfo: recipeInfo,
                        _orderBy: recipeInfo.Columns[recipeInfo.Version].Id,
                        _isOrderAsc: false);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows(readInfo); });
                    List<IComTabInfo> tableInfos = (List<IComTabInfo>)t.Result;
                    //List <ITableInfo> tableInfos = MyDatabase.GetRows(readInfo);
/*                    List<ITableInfo> tableInfos = MyDatabase.GetRows(recipeInfo,
                        orderBy: recipeInfo.columns[recipeInfo.version].id, isOrderAsc: false);*/

                    for (int i = 0; i < tableInfos.Count; i++)
                    {
                        ProgramVersions.Add(tableInfos[i].Columns[recipeInfo.Version].Value);
                    }

                    cbxVersionToCopy.ItemsSource = ProgramVersions;

                    if (ProgramVersions.Count > 0)
                    {
                        cbxVersionToCopy.SelectedIndex = 1;
                        cbxVersionToCopy.Items.Refresh();
                    }

                    cbxVersionToCopy.SelectedIndex = 0;
                    cbxVersionToCopy.Items.Refresh();

                    isCbxVersionCpyAvailable = true;
                }
                else
                {
                    ProgramVersions.Add(Settings.Default.Recipe_cbx_DefaultValue);
                    ProgramVersions.Add(Settings.Default.Recipe_cbx_DefaultValue);
                    ProgramVersions.Add(Settings.Default.Recipe_cbx_DefaultValue);

                    cbxVersionToCopy.ItemsSource = ProgramVersions;
                    cbxVersionToCopy.Items.Refresh();
                    cbxVersionToCopy.SelectedIndex = 0;

                    General.ShowMessageBox(DatabaseSettings.Error_connectToDbFailed);
                }

                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay);

                panelVersionRecipe.Visibility = Visibility.Visible;
                //MyDatabase.Disconnect();
            }
        }
        private async void CbxVersionToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxVersionToCopy_SelectionChanged");
            Task<object> t;

            if (isCbxVersionCpyAvailable)
            {
                logger.Trace("CbxVersionToCopy_SelectionChanged");

                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                ComboBox comboBox = sender as ComboBox;

                RecipeInfo recipeInfo = new RecipeInfo();
                recipeInfo.Columns[recipeInfo.Name].Value = cbxPgmToCopy.Text;
                recipeInfo.Columns[recipeInfo.Version].Value = ProgramVersions[comboBox.SelectedIndex];

                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(table: recipeInfo); });
                recipeInfo = (RecipeInfo)t.Result;
                //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(table: recipeInfo);

                curMethodDoneOnGoing = true;
                Display_Recipe(recipeInfo.Columns[recipeInfo.Id].Value);
                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay);

                //MyDatabase.Disconnect();
            }
        }
        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonCopy_Click");

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (Create_NewRecipe(tbRecipeNameCpy.Text, 1, RecipeStatus.DRAFT, false))
            {
                isCbxToCopyAvailable = false;
                General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                panelVersionRecipe.Visibility = Visibility.Collapsed;
                isCbxToCopyAvailable = true;
            }

            //MyDatabase.Disconnect();
        }

        private void tbWeightMinNew_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0,
                min: 0,
                max: tbWeightMaxNew.Text == "" ? -1 : int.Parse(tbWeightMaxNew.Text, NumberStyles.AllowThousands));
            finalWeightMin = tbWeightMinNew.Text;

            General.HideKeyBoard();
        }

        private void tbWeightMaxNew_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0, 
                min: tbWeightMinNew.Text == "" ? 0 : int.Parse(tbWeightMinNew.Text, NumberStyles.AllowThousands), 
                max: -1);
            finalWeightMax = tbWeightMaxNew.Text;

            General.HideKeyBoard();
        }

        private void tbWeightMinModif_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0,
                min: 0,
                max: tbWeightMaxModif.Text == "" ? -1 : int.Parse(tbWeightMaxModif.Text, NumberStyles.AllowThousands));
            finalWeightMin = tbWeightMinModif.Text;

            General.HideKeyBoard();
        }

        private void tbWeightMaxModif_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            General.Verify_Format(textBox, isNotNull: true, isNumber: true, parameter: 0,
                min: tbWeightMinModif.Text == "" ? 0 : int.Parse(tbWeightMinModif.Text, NumberStyles.AllowThousands),
                max: -1);
            finalWeightMax = tbWeightMaxModif.Text;

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
                General.HideKeyBoard();
            }
        }
    }
}