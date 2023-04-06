using Database;
using Main.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using Main.Pages.SubCycle;
using Message;

namespace Main.Pages
{
    public interface ISubRecipe
    {
        void SetPage(object[] seqValues);
        ISeqTabInfo GetPage();
        ISeqTabInfo GetRecipeInfo();
        object[] GetRecipeValues();

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
        public int id { get; }
        public int idCycle { get; }
        public int idPrevious { get; }
        public string tablePrevious { get; }
        public ISeqTabInfo prevSeqInfo { get; }
        public object[] prevSeqValues { get; }
        public bool isTest { get; }

        public SubCycleArg(Frame frameMain_arg, Frame frameInfoCycle_arg, int id_arg, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqTabInfo prevSeqInfo_arg, bool isTest_arg = true, object[] prevSeqValues_arg = null)
        {
            frameMain = frameMain_arg;
            frameInfoCycle = frameInfoCycle_arg;
            id = id_arg;
            idCycle = idCycle_arg;
            idPrevious = idPrevious_arg;
            tablePrevious = tablePrevious_arg;
            prevSeqInfo = prevSeqInfo_arg;
            isTest = isTest_arg;
            if (prevSeqValues_arg == null)
            {
                prevSeqValues = new object[prevSeqInfo.Ids.Count()];
            }
            else
            {
                prevSeqValues = prevSeqValues_arg;
            }
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
        private readonly RecipeInfo recipeInfo = new RecipeInfo();
        private int nRow;
        private readonly StringCollection status = Settings.Default.Recipe_Status_DescList;
        private bool isFrameLoaded;
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<int> ProgramIDs = new List<int>();
        private readonly List<string> ProgramVersions = new List<string>();
        private readonly bool isCbxToModifAvailable = false;
        private bool isCbxToCopyAvailable = false;
        private bool isCbxVersionCpyAvailable = false;
        private readonly bool isCbxToDeleteAvailable = false;
        private int currentRecipeVersion;
        private string currentRecipeStatus;
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private bool curMethodDoneOnGoing;
        private string finalWeightMin = "";
        private string finalWeightMax = "";
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
                    Create_NewSequence(recipeWeightInfo.SeqType);// MySettings["SubRecipeWeight_SeqType"]);
                    break;
                case RcpAction.Modify: // pour ça je pense qu'une comboBox est suffisant, on puet imaginer une fenêtre intermédiaire avec une liste et une champ pour filtrer mais ça me semble pas applicable à notre besoin
                    frameInfoCycle = frameInfoCycle_arg;
                    if(mainWindow == null)
                    {
                        MyMessageBox.Show("La fenêtre principale n'a pas été définie");
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

            List<ISeqTabInfo> seqInfoList = new List<ISeqTabInfo>();

            List<Tuple<ISeqTabInfo, object[]>> tSeqInfoList = new List<Tuple<ISeqTabInfo, object[]>>();

            ISubRecipe recipeSeq;
            Task<object> t;

            if (recipeName == "") {
                MyMessageBox.Show(Settings.Default.Recipe_Request_FillRecipeName);
                return false;
            }

            if (finalWeightMin == "" || finalWeightMax == "")
            {
                MyMessageBox.Show("Gamme de la masse final incorrecte");
                return false;
            }

            if (new_version <= 0)
            {
                logger.Error(Settings.Default.Recipe_Error_IncorrectVersion);
                MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectVersion);
                return false;
            }

            // Si on créer une nouvelle recette (version = 1)
            // On contrôle si la recette n'existe pas déjà
            RecipeInfo recipeInfo = new RecipeInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];
            recipeValues[recipeInfo.Name] = recipeName;


            if (new_version == 1 && 
                isRecipeCreated &&
                (int)(MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(recipeInfo, recipeInfo.Ids[recipeInfo.Version], recipeValues); }).Result) != 0) {
                MyMessageBox.Show(Settings.Default.Recipe_Info_ExistingRecipe);
                return false;
            }

            recipeInfo = new RecipeInfo();
            recipeValues = new object[recipeInfo.Ids.Count()];
            recipeValues[recipeInfo.Name] = recipeName;
            recipeValues[recipeInfo.Version] = new_version;
            recipeValues[recipeInfo.FinaleWeightMin] = finalWeightMin;
            recipeValues[recipeInfo.FinaleWeightMax] = finalWeightMax;
            recipeValues[recipeInfo.Status] = MyDatabase.GetRecipeStatus(status);

            tSeqInfoList.Add(new Tuple<ISeqTabInfo, object[]>(recipeInfo, recipeValues));
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
                    if (isFormatOk)
                    { 
                        seqInfoList.Add(recipeSeq.GetPage());
                        tSeqInfoList.Add(new Tuple<ISeqTabInfo, object[]>(recipeSeq.GetRecipeInfo(), recipeSeq.GetRecipeValues()));
                    }
                    else break;
                }
            }

            // Si toutes les séquences ne sont pas correctement renseignées, on sort de là
            if (!isFormatOk) {
                MyMessageBox.Show(Settings.Default.Recipe_Info_IncorrectFormat);
                return false;
            }

            bool isRecordOk;
            //n = seqInfoList.Count();
            n = tSeqInfoList.Count();
            isRecordOk = true;

            string row;

            for (i = n - 1; i > 0; i--)
            {
                if (isRecordOk)
                {
                    row = "InsertRow " + i.ToString() + " - ";
                    for (int j = 0; j < seqInfoList[i].Ids.Count(); j++)
                    {
                        row = row + tSeqInfoList[i].Item1.Ids[j] + ": " + (tSeqInfoList[i].Item2[j] == null ? "N/A" : tSeqInfoList[i].Item2[j].ToString()) + " ";
                        //MyMessageBox.Show((tSeqInfoList[i].Item2[j] == null).ToString());
                    }
                    logger.Trace(row);

                    // A CORRIGER : IF RESULT IS FALSE
                    //t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[i]); });
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(tSeqInfoList[i].Item1, tSeqInfoList[i].Item2); });
                    isRecordOk = (bool)t.Result;

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(tSeqInfoList[i].Item1, tSeqInfoList[i].Item1.Ids[tSeqInfoList[i].Item1.Id]); });
                    tSeqInfoList[i-1].Item2[tSeqInfoList[i-1].Item1.NextSeqId] = ((int)t.Result).ToString();
                    tSeqInfoList[i-1].Item2[tSeqInfoList[i-1].Item1.NextSeqType] = tSeqInfoList[i].Item1.SeqType.ToString();
                }
                else break;
            }
            //if (isRecordOk) isRecordOk = (bool)MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(seqInfoList[0]); }).Result;
            if (isRecordOk) isRecordOk = (bool)MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(tSeqInfoList[0].Item1, tSeqInfoList[0].Item2); }).Result;

            if (isRecordOk) {
                if (new_version == 1) MyMessageBox.Show(Settings.Default.Recipe_Info_RecipeCreated);
                else MyMessageBox.Show(Settings.Default.Recipe_Info_RecipeModified);
                return true;
            }
            // S'il y a eu une erreur, on supprime les lignes qui ont été créés.
            else if (i != n - 2)
            {
                i++;

                do
                {
                    logger.Trace("DeleteRow " + i.ToString() + ": " + tSeqInfoList[i + 1].Item1.TabName + " " +
                        tSeqInfoList[i].Item1.Ids[tSeqInfoList[i].Item1.NextSeqId] + " " +
                        tSeqInfoList[i].Item2[tSeqInfoList[i].Item1.NextSeqId].ToString());

                    int id = int.Parse(tSeqInfoList[i].Item2[tSeqInfoList[i].Item1.NextSeqId].ToString());
                    ISeqTabInfo seqTabInfo = tSeqInfoList[i + 1].Item1;

                    // A CORRIGER : IF RESULT IS FALSE
                    //t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow(seqTabInfo, id); });
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow_new(seqTabInfo, id); });

                    i++;
                } while (tSeqInfoList[i].Item2[tSeqInfoList[i].Item1.NextSeqId] != null && tSeqInfoList[i].Item2[tSeqInfoList[i].Item1.NextSeqId].ToString() != "");
            }

            MyMessageBox.Show(Settings.Default.Recipe_Info_RecipeNotCreated);
            return false;
        }
        private async void Display_Recipe(int id)
        {
            logger.Debug("Display_Recipe");
            Task<object> t;

            int? nextSeqType;
            int? nextSeqID;
            Frame currentFrame;
            List<Frame> framesToDelete = new List<Frame>();
            RecipeInfo recipeInfo = new RecipeInfo();
            object[] recipeValues = new object[recipeInfo.Ids.Count()];
            ISubRecipe currentPage;
            ISeqTabInfo currentRecipeSeq;
            object[] currentRecipeValues;

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), id); });
            recipeValues = (object[])t.Result;

            if (recipeValues == null) // Si la requête envoyer ne contient qu'une seule ligne
            {
                MyMessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            if (recipeValues[recipeInfo.NextSeqType] == null || recipeValues[recipeInfo.NextSeqType].ToString() == "")
            {
                nextSeqType = null;
                nextSeqID = null;
            }
            else
            {
                nextSeqType = (int)recipeValues[recipeInfo.NextSeqType];
                nextSeqID = (int)recipeValues[recipeInfo.NextSeqId];
            }

            currentRecipeVersion = (int)recipeValues[recipeInfo.Version];
            currentRecipeStatus = status[(int)recipeValues[recipeInfo.Status]];
            tbWeightMinModif.Text = recipeValues[recipeInfo.FinaleWeightMin].ToString();
            tbWeightMaxModif.Text = recipeValues[recipeInfo.FinaleWeightMax].ToString();
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

            while (nextSeqID != null) // On remplie les frames ici
            {
                isFrameLoaded = false;
                Create_NewSequence(nextSeqType);

                while (!isFrameLoaded) await Task.Delay(Settings.Default.Recipe_WaitFrameLoaded_Delay); // On attend que la frame créée (New_Sequence(nextSeqType == "1");) a été chargé
                isFrameLoaded = false;

                if (gridMain.Children[gridMain.Children.Count - 1].GetType().Equals(typeof(Frame))) // Si le dernier élément de la grille gridMain est une frame on continue
                {

                    // A CORRIGER : IF RESULT IS FALSE
                    currentRecipeSeq = Sequence.list[(int)(nextSeqType)].subRecipeInfo;
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(currentRecipeSeq, nextSeqID); });
                    currentRecipeValues = (object[])t.Result;
                    currentFrame = gridMain.Children[gridMain.Children.Count - 1] as Frame;

                    currentPage = currentFrame.Content as ISubRecipe;

                    if (currentRecipeValues != null)
                    {
                        currentPage.SetPage(currentRecipeValues);

                        if (currentRecipeValues[currentRecipeSeq.NextSeqType] == null || currentRecipeValues[currentRecipeSeq.NextSeqType].ToString() == "")
                        {
                            nextSeqType = null;
                            nextSeqID = null;
                        }
                        else
                        {
                            nextSeqType = (int)currentRecipeValues[currentRecipeSeq.NextSeqType];
                            nextSeqID = (int)currentRecipeValues[currentRecipeSeq.NextSeqId];
                        }

                    }
                    else
                    {
                        MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
                        nextSeqID = null;
                    }
                }
                else
                {
                    MyMessageBox.Show(Settings.Default.Recipe_Error_FrameNotSeen);
                }
            } //while (nextSeqID != null);

            curMethodDoneOnGoing = false;
        }
        private void Create_NewSequence(int? seqType)
        {
            logger.Debug("Create_NewSequence");

            if (seqType == null)
            {
                logger.Error("Le type demandé est null");
                MyMessageBox.Show("Le type demandé est null");
            }

            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            frame.ContentRendered += SubRecipeFrame_ContentRendered;

            frame.Content = Activator.CreateInstance(Pages.Sequence.list[(int)(seqType)].subRcpPgType, new object[] { frame, nRow.ToString() });

            Grid.SetRow(frame, gridMain.RowDefinitions.Count() - 1);
            gridMain.Children.Add(frame);
            nRow++;
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
        private void Delete_Recipe(int? id)
        {
            logger.Debug("Delete_Recipe");

            ISeqTabInfo subRecipeSeq;
            object[] subRecipeValues;
            int? nextSeqType;
            int? nextSeqId;
            Task<object> t;

            if (id == null)
            {
                logger.Error("On a un problème");
                MyMessageBox.Show("On a un problème");
                return;
            }

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), id); });
            object[] recipeValues = (object[])t.Result;
            RecipeInfo recipeInfo = new RecipeInfo();

            if (recipeValues == null)
            {
                MyMessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            if (recipeValues[recipeInfo.NextSeqType] == null || recipeValues[recipeInfo.NextSeqType].ToString() == "")
            {
                nextSeqType = null;
                nextSeqId = null;
            }
            else
            {
                nextSeqType = (int)recipeValues[recipeInfo.NextSeqType];
                nextSeqId = (int)recipeValues[recipeInfo.NextSeqId];
            }

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow_new(new RecipeInfo(), id); });

            while (nextSeqType != null)
            {
                // A CORRIGER : IF RESULT IS FALSE
                subRecipeSeq = Sequence.list[(int)nextSeqType].subRecipeInfo;
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(subRecipeSeq, nextSeqId); });
                subRecipeValues = (object[])t.Result;

                if (subRecipeValues != null)
                {
                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.DeleteRow_new(subRecipeSeq, nextSeqId); });
                    t.Wait();

                    if (subRecipeValues[subRecipeSeq.NextSeqType] == null || subRecipeValues[subRecipeSeq.NextSeqType].ToString() == "")
                    {
                        nextSeqType = null;
                        nextSeqId = null;
                    }
                    else
                    {
                        nextSeqType = (int)subRecipeValues[subRecipeSeq.NextSeqType];
                        nextSeqId = (int)subRecipeValues[subRecipeSeq.NextSeqId];
                    }
                }
                else
                {
                    nextSeqType = null;
                    MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
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

            Create_NewSequence(recipeWeightInfo.SeqType);
        }
        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonCreate_Click");

            Create_NewRecipe(tbRecipeNameNew.Text, 1, RecipeStatus.DRAFT);
        }
        private async void CbxPgmToModify_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxPgmToModify_SelectionChanged");

            if (isCbxToModifAvailable)
            {
                logger.Trace("CbxPgmToModify_SelectionChanged");

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                labelVersion.Text = currentRecipeVersion.ToString();
                labelStatus.Text = currentRecipeStatus;

                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay); 

                panelInfoRecipe.Visibility = Visibility.Visible;
                panelTestRecipe.Visibility = Visibility.Visible;
            }
        }
        private void ButtonModify_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonModify_Click");

            int currentIndex = cbxPgmToModify.SelectedIndex;
            Task<object> t;

            // A CORRIGER : IF RESULT IS FALSE
            if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.PROD)])
            {
                if (MyMessageBox.Show(Settings.Default.Recipe_Request_UpdateProdRecipe, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Création d'une nouvelle recette, l'ancienne version sera obsolète
                    if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text) + 1, RecipeStatus.DRAFT, false))
                    {
                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(recipeInfo, recipeInfo.Ids[recipeInfo.Id]); });
                        ProgramIDs[currentIndex] = ((int)t.Result);

                        labelStatus.Text = status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)];
                        labelVersion.Text = (int.Parse(labelVersion.Text) + 1).ToString();
                    }
                }
            }
            else if (labelStatus.Text == status[MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT)])
            {
                bool isStillDraft = false;

                if (MyMessageBox.Show(Settings.Default.Recipe_Request_UpdateDraftRecipe, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (MyMessageBox.Show(Settings.Default.Recipe_Request_UpdateDraftRecipe_YOU_SURE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Modification de la recette puis modification du status en draft
                        if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text), RecipeStatus.PROD, false))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(recipeInfo, recipeInfo.Ids[recipeInfo.Id]); });
                            ProgramIDs[currentIndex] = ((int)t.Result);

                            // Get the new recipe
                            //t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), ProgramIDs[currentIndex].ToString()); });
                            //RecipeInfo oldRecipe = (RecipeInfo)t.Result;
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), ProgramIDs[currentIndex]); });
                            object[] oldRecipeValues = (object[])t.Result;

                            string name = oldRecipeValues[recipeInfo.Name].ToString();                   // name of the recipe
                            int version = int.Parse(oldRecipeValues[recipeInfo.Version].ToString()) - 1; // version of the previous recipe

                            if (version > 0)
                            {
                                // Get the previous version of the recipe (Create new recipe and set recipe name and version)
                                //oldRecipe = new RecipeInfo();
                                oldRecipeValues = new object[recipeInfo.Ids.Count()];
                                oldRecipeValues[recipeInfo.Name] = name;
                                oldRecipeValues[recipeInfo.Version] = version.ToString();
                                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(table: recipeInfo, values: oldRecipeValues); });
                                oldRecipeValues = (object[])t.Result;

                                if (oldRecipeValues == null)
                                {
                                    labelStatus.Text = "###";
                                    MyMessageBox.Show("Problème dans la création de la recette");
                                    return;
                                }

                                // Update the status of the previous version of the recipe to Obsolete
                                int id = int.Parse(oldRecipeValues[recipeInfo.Id].ToString());
                                //oldRecipe = new RecipeInfo();
                                oldRecipeValues = new object[recipeInfo.Ids.Count()];
                                oldRecipeValues[recipeInfo.Status] = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE).ToString();
                                // A CORRIGER : IF RESULT IS FALSE
                                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(recipeInfo, oldRecipeValues, id); });
                            }

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
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(recipeInfo, recipeInfo.Ids[recipeInfo.Id]); });
                        ProgramIDs[currentIndex] = ((int)t.Result);
                    }
                }
            }
            else
            {
                MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectStatus);
            }
        }
        private void ButtonActDel_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonActDel_Click");

            int currentIndex = cbxPgmToActDelete.SelectedIndex;
            RecipeInfo recipeInfo = new RecipeInfo();
            object[] recipeValues;
            object[] recipeToUpdate = new object[recipeInfo.Ids.Count()];
            int recipeId = ProgramIDs[currentIndex];
            Task<object> t;

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), recipeId); });
            recipeValues = (object[])t.Result;

            if (recipeValues == null)
            {
                MyMessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }


            if ((bool)rbDelete.IsChecked)
            {
                if (recipeValues[recipeInfo.Status].ToString() == MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString())
                {
                    if (MyMessageBox.Show(Settings.Default.Recipe_Request_DelProdRecipe1 +
                        recipeInfo.Descriptions[recipeInfo.Name] + " " + recipeValues[recipeInfo.Name].ToString() + " " +
                        recipeInfo.Descriptions[recipeInfo.Version] + " " + recipeValues[recipeInfo.Version].ToString() + 
                        Settings.Default.Recipe_Request_DelProdRecipe2, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        //recipeToUpdate = new object[recipeInfo.Ids.Count()];
                        recipeToUpdate[recipeInfo.Status] = MyDatabase.GetRecipeStatus(RecipeStatus.OBSOLETE);

                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(new RecipeInfo(), recipeToUpdate, recipeId); });

                        ProgramIDs.RemoveAt(currentIndex);
                        ProgramNames.RemoveAt(currentIndex);

                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        MyMessageBox.Show(Settings.Default.Recipe_Info_DelProdDone);
                    }
                }
                else if (recipeValues[recipeInfo.Status].ToString() == MyDatabase.GetRecipeStatus(RecipeStatus.DRAFT).ToString())
                {
                    if (MyMessageBox.Show(Settings.Default.Recipe_Request_DelDraftRecipe1 +
                        recipeInfo.Descriptions[recipeInfo.Name] + " " + recipeValues[recipeInfo.Name].ToString() + " " +
                        recipeInfo.Descriptions[recipeInfo.Version] + " " + recipeValues[recipeInfo.Version].ToString() + 
                        Settings.Default.Recipe_Request_DelDraftRecipe2, 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Delete_Recipe(recipeId);

                        if ((int)recipeValues[recipeInfo.Version] > 1)
                        {
                            object[] recipe = new object[recipeInfo.Ids.Count()];
                            recipe[recipeInfo.Name] = recipeValues[recipeInfo.Name];

                            // A CORRIGER : IF RESULT IS FALSE
                            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(recipeInfo, recipeInfo.Ids[recipeInfo.Id]); });
                            ProgramIDs[currentIndex] = ((int)t.Result);
                            recipeId = ProgramIDs[currentIndex];
                        }
                        else if ((int)recipeValues[recipeInfo.Version] == 1)
                        {
                            ProgramIDs.RemoveAt(currentIndex);
                            ProgramNames.RemoveAt(currentIndex);
                        }
                        else
                        {
                            MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectVersion + ": " + recipeValues[recipeInfo.Version].ToString());
                        }

                        // mettre ça dans une fonction et on recommence tout
                        CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                        MyMessageBox.Show(Settings.Default.Recipe_Info_DelDraftDone);
                    }
                }
                else
                {
                    MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectStatus + ": " + status[(int)(recipeValues[recipeInfo.Status])]);
                }
            }
            else if ((bool)rbActivate.IsChecked)
            {
                if (MyMessageBox.Show(Settings.Default.Recipe_Request_ActRecipe1 + 
                    recipeValues[recipeInfo.Name].ToString() + 
                    " version " + recipeValues[recipeInfo.Version].ToString() + 
                    Settings.Default.Recipe_Request_ActRecipe2, 
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // using partout
                    recipeToUpdate = new object[recipeInfo.Ids.Count()];
                    recipeToUpdate[recipeInfo.Status] = MyDatabase.GetRecipeStatus(RecipeStatus.PROD).ToString();

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(recipeInfo, recipeToUpdate, recipeId); });

                    ProgramIDs.RemoveAt(currentIndex);
                    ProgramNames.RemoveAt(currentIndex);

                    CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                    MyMessageBox.Show(Settings.Default.Recipe_Info_ActDone);
                }
            }
            else
            {
                logger.Error(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
                MyMessageBox.Show(Settings.Default.Recipe_DelAct_Error_NoRadiobt);
            }
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
                MyMessageBox.Show(Settings.Default.Cycle_Info_FinalWeightIncorrect);
                return;
            }

            if (MyMessageBox.Show(Settings.Default.Recipe_Request_TestRecipe, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                int id = ProgramIDs[cbxPgmToModify.SelectedIndex];
                CycleStartInfo info;
                info.recipeID = id;
                info.OFnumber = Settings.Default.General_na;
                info.finalWeight = finalWeight.ToString();
                info.frameMain = frameMain;
                info.frameInfoCycle = frameInfoCycle;
                info.isTest = true;
                info.bowlWeight = "";
                info.frameMain.Content = new CycleWeight(info);
                mainWindow.UpdateMenuStartCycle(false);
            }
        }
        private async void CbxPgmToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxPgmToCopy_SelectionChanged");
            Task<object> t;

            if (isCbxToCopyAvailable)
            {
                isCbxVersionCpyAvailable = false;

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                ProgramVersions.Clear();

                RecipeInfo recipeInfo = new RecipeInfo();
                object[] recipeValues = new object[recipeInfo.Ids.Count()];
                recipeValues[recipeInfo.Name] = ProgramNames[comboBox.SelectedIndex];

                ReadInfo readInfo = new ReadInfo(
                    _tableInfo: recipeInfo,
                    _orderBy: recipeInfo.Ids[recipeInfo.Version],
                    _isOrderAsc: false);

                // A CORRIGER : IF RESULT IS FALSE
                //t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows(readInfo); });
                //List<IComTabInfo> tableInfos = (List<IComTabInfo>)t.Result;
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetRows_new(readInfo, recipeValues); });
                List<object[]> tableValues = (List<object[]>)t.Result;

                for (int i = 0; i < tableValues.Count; i++)
                {
                    ProgramVersions.Add(tableValues[i][recipeInfo.Version].ToString());
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

                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay);

                panelVersionRecipe.Visibility = Visibility.Visible;
            }
        }
        private async void CbxVersionToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("CbxVersionToCopy_SelectionChanged");
            Task<object> t;

            if (isCbxVersionCpyAvailable)
            {
                logger.Trace("CbxVersionToCopy_SelectionChanged");

                ComboBox comboBox = sender as ComboBox;

                RecipeInfo recipeInfo = new RecipeInfo();
                object[] recipeValues = new object[recipeInfo.Ids.Count()];
                recipeValues[recipeInfo.Name] = cbxPgmToCopy.Text;
                recipeValues[recipeInfo.Version] = ProgramVersions[comboBox.SelectedIndex];

                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(table: recipeInfo, values: recipeValues); });
                recipeValues = (object[])t.Result;

                curMethodDoneOnGoing = true;
                Display_Recipe((int)recipeValues[recipeInfo.Id]);
                while (curMethodDoneOnGoing) await Task.Delay(Settings.Default.Recipe_WaitRecipeDisplayedDelay);
            }
        }
        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("ButtonCopy_Click");

            if (Create_NewRecipe(tbRecipeNameCpy.Text, 1, RecipeStatus.DRAFT, true))
            {
                isCbxToCopyAvailable = false;
                General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, RecipeStatus.PRODnDRAFT);
                panelVersionRecipe.Visibility = Visibility.Collapsed;
                isCbxToCopyAvailable = true;
            }
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