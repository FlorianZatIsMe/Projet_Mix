using Database;
using FPO_WPF_Test.Properties;
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

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour Recipe.xaml
    /// </summary>

    public interface ISubRecipe
    {
        //int seqType { get; }
        void SetPage(ISeqInfo seqInfo);
        ISeqInfo GetPage();
        bool IsFormatOk();
        void SetSeqNumber(int n);
    }
    public interface ISubCycle
    {
    }
    public class SubCycleArg
    {
        public Frame frameMain { get; }
        public Frame frameInfoCycle { get; }
        public string id { get; }
        public int idCycle { get; }
        public int idPrevious { get; }
        public string tablePrevious { get; }
        public ISeqInfo prevSeqInfo { get; }
        public bool isTest { get; }

        public SubCycleArg(Frame frameMain_arg, Frame frameInfoCycle_arg, string id_arg, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqInfo prevSeqInfo_arg, bool isTest_arg = true)
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
        public ISeqInfo subRecipeInfo { get; set; }
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
                subCycPgType = typeof(Pages.SubCycle.CycleWeight),
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

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Recipe(Action action, Frame frameMain_arg = null, Frame frameInfoCycle_arg = null, string recipeName = "")
        {
            logger.Debug("Start");

            nRow = 1;

            frameMain = frameMain_arg;
            isFrameLoaded = false;

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            InitializeComponent();

            switch (action)
            {
                case Action.New:
                    gridNewRecipe.Visibility = Visibility.Visible;
                    Create_NewSequence(recipeWeightInfo.seqType.ToString());// MySettings["SubRecipeWeight_SeqType"]);
                    break;
                case Action.Modify: // pour ça je pense qu'une comboBox est suffisant, on puet imaginer une fenêtre intermédiaire avec une liste et une champ pour filtrer mais ça me semble pas applicable à notre besoin
                    frameInfoCycle = frameInfoCycle_arg;
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
                case Action.Copy:
                    gridCopy_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                    isCbxToCopyAvailable = true;
                    break;
                case Action.Delete:
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
            List<ISeqInfo> seqInfoList = new List<ISeqInfo>();
            ISubRecipe recipeSeq;
            Task<object> t;
            /*
            if (!MyDatabase.IsConnected()) {
                MessageBox.Show(DatabaseSettings.Error01);
                return false;
            }*/

            if (recipeName == "") {
                MessageBox.Show(Settings.Default.Recipe_Request_FillRecipeName);
                return false;
            }

            if (new_version <= 0)
            {
                logger.Error(Settings.Default.Recipe_Error_IncorrectVersion);
                MessageBox.Show(Settings.Default.Recipe_Error_IncorrectVersion);
                return false;
            }

            // Si on créer une nouvelle recette (version = 1)
            // On contrôle si la recette n'existe pas déjà
            RecipeInfo recipeInfo = new RecipeInfo();
            recipeInfo.columns[recipeInfo.recipeName].value = recipeName;


            if (new_version == 1 && 
                isRecipeCreated &&
                (int)(MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo, recipeInfo.columns[recipeInfo.version].id); }).Result) != 0) {
                MessageBox.Show(Settings.Default.Recipe_Info_ExistingRecipe);
                return false;
            }
            /*
            if (new_version == 1 && 
                isRecipeCreated &&
                MyDatabase.GetMax(recipeInfo, recipeInfo.columns[recipeInfo.version].id) != 0) {
                MessageBox.Show(Settings.Default.Recipe_Info_ExistingRecipe);
                return false;
            }
             */

            // pourquoi faire ça, à voir...
            //using (RecipeInfo recipeInfo = new RecipeInfo()) {}

            recipeInfo = new RecipeInfo();
            recipeInfo.columns[recipeInfo.recipeName].value = recipeName;
            recipeInfo.columns[recipeInfo.version].value = new_version.ToString();

            recipeInfo.columns[recipeInfo.status].value = MyDatabase.GetRecipeStatus(status).ToString();
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
                MessageBox.Show(Settings.Default.Recipe_Info_IncorrectFormat);
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
                    for (int j = 0; j < seqInfoList[i].columns.Count(); j++)
                    {
                        row = row + seqInfoList[i].columns[j].id + ": " + seqInfoList[i].columns[j].value + " ";
                    }
                    logger.Trace(row);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[i]); });
                    isRecordOk = (bool)t.Result;
                    //isRecordOk = MyDatabase.InsertRow(seqInfoList[i]);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(seqInfoList[i].name, seqInfoList[i].columns[seqInfoList[i].id].id); });
                    seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqId].value = ((int)t.Result).ToString();
                    //seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqId].value = MyDatabase.GetMax(seqInfoList[i].name, seqInfoList[i].columns[seqInfoList[i].id].id).ToString();
                    seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqType].value = seqInfoList[i].seqType.ToString();
                }
                else break;
            }
            if (isRecordOk) isRecordOk = (bool)MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[0]); }).Result;
            //if (isRecordOk) isRecordOk = MyDatabase.InsertRow(seqInfoList[0]);

            if (isRecordOk) {
                if (new_version == 1) MessageBox.Show(Settings.Default.Recipe_Info_RecipeCreated);
                else MessageBox.Show(Settings.Default.Recipe_Info_RecipeModified);
                return true;
            }
            // S'il y a eu une erreur, on supprime les lignes qui ont été créés.
            else if (i != n - 2)
            {
                i++;

                do
                {
                    logger.Trace("DeleteRow " + i.ToString() + ": " + seqInfoList[i + 1].name + " " + 
                        seqInfoList[i].columns[seqInfoList[i].nextSeqId].id + " " +
                        seqInfoList[i].columns[seqInfoList[i].nextSeqId].value);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(seqInfoList[i + 1], seqInfoList[i].columns[seqInfoList[i].nextSeqId].value); });
                    //MyDatabase.DeleteRow(seqInfoList[i + 1], seqInfoList[i].columns[seqInfoList[i].nextSeqId].value);

                    i++;
                } while (seqInfoList[i].columns[seqInfoList[i].nextSeqId].value != null);
            }

            MessageBox.Show(Settings.Default.Recipe_Info_RecipeNotCreated);
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
            ISeqInfo currentRecipeSeq;
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                MessageBox.Show(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), id); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), id);

            if (recipeInfo.columns == null) // Si la requête envoyer ne contient qu'une seule ligne
            {
                MessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            nextSeqType = recipeInfo.columns[recipeInfo.nextSeqType].value;
            nextSeqID = recipeInfo.columns[recipeInfo.nextSeqId].value;
            currentRecipeVersion = recipeInfo.columns[recipeInfo.version].value;
            currentRecipeStatus = status[int.Parse(recipeInfo.columns[recipeInfo.status].value)];

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
                    currentRecipeSeq = (ISeqInfo)t.Result;
                    //currentRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID);
                    currentFrame = gridMain.Children[gridMain.Children.Count - 1] as Frame;

                    currentPage = currentFrame.Content as ISubRecipe;

                    if (currentRecipeSeq != null)
                    {
                        currentPage.SetPage(currentRecipeSeq);
                        nextSeqType = currentRecipeSeq.columns[currentRecipeSeq.nextSeqType].value;
                        nextSeqID = currentRecipeSeq.columns[currentRecipeSeq.nextSeqId].value;
                    }
                    else
                    {
                        MessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
                        nextSeqID = "";
                    }
                }
                else
                {
                    MessageBox.Show(Settings.Default.Recipe_Error_FrameNotSeen);
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
                ISeqInfo seqInfo = recipeSeq.GetPage();

                string row = "";
                for (int j = 0; j < seqInfo.columns.Count(); j++)
                {
                    row = row + seqInfo.columns[j].value + " ";
                }

                MessageBox.Show(row);
            }
            else
            {
                MessageBox.Show(frame.Content.GetType().ToString());
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

            ISeqInfo subRecipeSeq;
            string nextSeqType;
            string nextSeqId;
            Task<object> t;

            /*
            if (!MyDatabase.IsConnected()) // while loop is better
            {
                MessageBox.Show(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(new RecipeInfo().GetType(), id); });
            RecipeInfo recipeInfo = (RecipeInfo)t.Result;
            //RecipeInfo recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(new RecipeInfo().GetType(), id);

            if (recipeInfo == null)
            {
                MessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            nextSeqType = recipeInfo.columns[recipeInfo.nextSeqType].value;
            nextSeqId = recipeInfo.columns[recipeInfo.nextSeqId].value;
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(new RecipeInfo(), id); });
            //MyDatabase.DeleteRow(new RecipeInfo(), id);

            while (nextSeqType != "" && nextSeqType != null)
            {
                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqId); });
                subRecipeSeq = (ISeqInfo)t.Result;
                //subRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSetId);

                if (subRecipeSeq != null)
                {
                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(subRecipeSeq, nextSeqId); });
                    t.Wait();
                    //MyDatabase.DeleteRow(subRecipeSeq, nextSetId);
                    nextSeqType = subRecipeSeq.columns[subRecipeSeq.nextSeqType].value;
                    nextSeqId = subRecipeSeq.columns[subRecipeSeq.nextSeqId].value;
                }
                else
                {
                    nextSeqType = "";
                    MessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
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

            Create_NewSequence(recipeWeightInfo.seqType.ToString());
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
                //MyDatabase.Disconnect();
            }
        }
        private void ButtonModify_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonModify_Click");

            int currentIndex = cbxPgmToModify.SelectedIndex;
            RecipeInfo recipeInfo = new RecipeInfo();
            Task<object> t;

            //MyDatabase.Connect();
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                MessageBox.Show(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            //if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)])
            if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)])
            {
                if (MessageBox.Show(Settings.Default.Recipe_Request_UpdateProdRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Création d'une nouvelle recette, l'ancienne version sera obsolète
                    if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text) + 1, RecipeStatus.DRAFT, false))
                    {
                        recipeInfo.columns[recipeInfo.status].value = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();
                        //recipeInfo.columns[recipeInfo.status].value = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeInfo, ProgramIDs[currentIndex]); });
                        //MyDatabase.Update_Row(recipeInfo, ProgramIDs[currentIndex]);

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id); });
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
                if (MessageBox.Show(Settings.Default.Recipe_Request_UpdateDraftRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Modification de la recette puis modification du status en draft
                    if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text), RecipeStatus.PROD, false))
                    {
                        Delete_Recipe(ProgramIDs[currentIndex]);
                        logger.Error("HERE");
                        MessageBox.Show("This is not happening");

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id); });
                        ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                        //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id).ToString();

                        labelStatus.Text = status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)];
                    }
                }
                else
                {
                    // Modification de la recette draft en cours
                    if (Create_NewRecipe(ProgramNames[cbxPgmToModify.SelectedIndex], int.Parse(labelVersion.Text), RecipeStatus.DRAFT, false))
                    {
                        Delete_Recipe(ProgramIDs[currentIndex]);

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id); });
                        ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                        //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipeInfo.name, recipeInfo.columns[recipeInfo.id].id).ToString();
                    }
                }
            }
            else
            {
                MessageBox.Show(Settings.Default.Recipe_Error_IncorrectStatus);
            }

            //MyDatabase.Disconnect();
        }
        private void ButtonActDel_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonActDel_Click");

            int currentIndex = cbxPgmToActDelete.SelectedIndex;
            RecipeInfo recipeInfo;
            RecipeInfo recipeToUpdate;
            Task<object> t;

            //MyDatabase.Connect();
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                MessageBox.Show(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex]); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex]);

            if (recipeInfo == null)
            {
                MessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
                //goto End;
            }


            if ((bool)rbDelete.IsChecked)
            {
                //if (recipeInfo.columns[recipeInfo.status].value == MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString())
                if (recipeInfo.columns[recipeInfo.status].value == ((int)MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex]); }).Result).ToString())
                {
                    if (MessageBox.Show(Settings.Default.Recipe_Request_DelProdRecipe1 + 
                        recipeInfo.columns[recipeInfo.recipeName].displayName + " " + recipeInfo.columns[recipeInfo.recipeName].value + " " + 
                        recipeInfo.columns[recipeInfo.version].displayName + " " + recipeInfo.columns[recipeInfo.version].value + 
                        Settings.Default.Recipe_Request_DelProdRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        recipeToUpdate = new RecipeInfo();
                        recipeToUpdate.columns[recipeToUpdate.status].value = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]); });
                        //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                        ProgramIDs.RemoveAt(currentIndex);
                        ProgramNames.RemoveAt(currentIndex);

                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        MessageBox.Show(Settings.Default.Recipe_Info_DelProdDone);
                    }
                }
                else if (recipeInfo.columns[recipeInfo.status].value == MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT).ToString())
                {
                    if (MessageBox.Show(Settings.Default.Recipe_Request_DelDraftRecipe1 + 
                        recipeInfo.columns[recipeInfo.recipeName].displayName + " " + recipeInfo.columns[recipeInfo.recipeName].value + " " + 
                        recipeInfo.columns[recipeInfo.version].displayName + " " + recipeInfo.columns[recipeInfo.version].value + 
                        Settings.Default.Recipe_Request_DelDraftRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Delete_Recipe(ProgramIDs[currentIndex]);

                        if (int.Parse(recipeInfo.columns[recipeInfo.version].value) > 1)
                        {
                            RecipeInfo recipe = new RecipeInfo();
                            recipe.columns[recipe.recipeName].value = recipeInfo.columns[recipeInfo.recipeName].value;

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(recipe, recipe.columns[recipe.id].id); });
                            ProgramIDs[currentIndex] = ((int)t.Result).ToString();
                            //ProgramIDs[currentIndex] = MyDatabase.GetMax(recipe, recipe.columns[recipe.id].id).ToString();

                            recipeToUpdate = new RecipeInfo();
                            recipeToUpdate.columns[recipeToUpdate.status].value = MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString();

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]); });
                            //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);
                        }
                        else if (int.Parse(recipeInfo.columns[recipeInfo.version].value) == 1)
                        {
                            ProgramIDs.RemoveAt(currentIndex);
                            ProgramNames.RemoveAt(currentIndex);
                        }
                        else
                        {
                            MessageBox.Show(Settings.Default.Recipe_Error_IncorrectVersion + ": " + recipeInfo.columns[recipeInfo.version].value);
                        }

                        // mettre ça dans une fonction et on recommence tout
                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        MessageBox.Show(Settings.Default.Recipe_Info_DelDraftDone);
                    }
                }
                else
                {
                    MessageBox.Show(Settings.Default.Recipe_Error_IncorrectStatus + ": " + status[int.Parse(recipeInfo.columns[recipeInfo.status].value)]);
                }
            }
            else if ((bool)rbActivate.IsChecked)
            {
                if (MessageBox.Show(Settings.Default.Recipe_Request_ActRecipe1 + 
                    recipeInfo.columns[recipeInfo.recipeName].value + 
                    " version " + recipeInfo.columns[recipeInfo.version].value + 
                    Settings.Default.Recipe_Request_ActRecipe2, Settings.Default.General_Request_ConfirmationTitle, 
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // using partout
                    recipeToUpdate = new RecipeInfo();
                    recipeToUpdate.columns[recipeToUpdate.status].value = MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString();

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]); });
                    //MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                    ProgramIDs.RemoveAt(currentIndex);
                    ProgramNames.RemoveAt(currentIndex);

                    CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                    MessageBox.Show(Settings.Default.Recipe_Info_ActDone);
                }
            }
            else
            {
                logger.Error(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
                MessageBox.Show(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
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

            float finalWeight;

            try
            {
                finalWeight = float.Parse(tbFinaleWeight.Text);
            }
            catch (Exception)
            {
                finalWeight = -1;
            }

            if (finalWeight > 0)
            {
                if (MessageBox.Show(Settings.Default.Recipe_Request_TestRecipe, Settings.Default.General_Request_ConfirmationTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    string id = ProgramIDs[cbxPgmToModify.SelectedIndex];
                    General.StartCycle(id, Settings.Default.General_na, finalWeight.ToString(), frameMain, frameInfoCycle, true);
                }
            }
            else
            {
                MessageBox.Show(Settings.Default.Cycle_Info_FinalWeightIncorrect);
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
                    recipeInfo.columns[recipeInfo.recipeName].value = ProgramNames[comboBox.SelectedIndex];

                    ReadInfo readInfo = new ReadInfo(
                        _tableInfo: recipeInfo,
                        _orderBy: recipeInfo.columns[recipeInfo.version].id,
                        _isOrderAsc: false);

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows(readInfo); });
                    List<ITableInfo> tableInfos = (List<ITableInfo>)t.Result;
                    //List <ITableInfo> tableInfos = MyDatabase.GetRows(readInfo);
/*                    List<ITableInfo> tableInfos = MyDatabase.GetRows(recipeInfo,
                        orderBy: recipeInfo.columns[recipeInfo.version].id, isOrderAsc: false);*/

                    for (int i = 0; i < tableInfos.Count; i++)
                    {
                        ProgramVersions.Add(tableInfos[i].columns[recipeInfo.version].value);
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

                    MessageBox.Show(DatabaseSettings.Error01);
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
                recipeInfo.columns[recipeInfo.recipeName].value = cbxPgmToCopy.Text;
                recipeInfo.columns[recipeInfo.version].value = ProgramVersions[comboBox.SelectedIndex];

                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(table: recipeInfo); });
                recipeInfo = (RecipeInfo)t.Result;
                //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(table: recipeInfo);

                curMethodDoneOnGoing = true;
                Display_Recipe(recipeInfo.columns[recipeInfo.id].value);
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

        // VERIFIE LA CONNECTION DE LA BASE DE DONNéES PARTOUT !!!!! MOTHER FUCKER ¨¨
        // ...
        // ou pas... ;-P
    }
}