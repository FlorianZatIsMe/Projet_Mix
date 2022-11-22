using Database;
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
    }

    public interface ISubCycle
    {
        ISubCycle NewInstance(SubCycleArg subCycleArg);
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
        public ISeqInfo subCycleInfo { get; set; }
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

    public partial class Recipe : Page
    {
        private int nRow;
        //private readonly MyDatabase db;
        private readonly string[] status;
        private bool isFrameLoaded;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;
        //private readonly Action CurrentAction;
        private readonly List<string> ProgramNames = new List<string>();
        private readonly List<string> ProgramIDs = new List<string>();
        private readonly List<string> ProgramVersions = new List<string>();
        private readonly bool isCbxToModifAvailable = false;
        private bool isCbxToCopyAvailable = false;
        private bool isCbxVersionCpyAvailable = false;
        private readonly bool isCbxToDeleteAvailable = false;
        //private string currentRecipeName;
        private string currentRecipeVersion;
        private string currentRecipeStatus;
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private bool curMethodDoneOnGoing;

        private static readonly string exportFolder = @"C:\Temp\Exports\";
        private static readonly string fileExtension_table = ".tbl";

        public readonly List<ISubRecipe> subRecipePages = new List<ISubRecipe>
        {
            new SubRecipe.Weight(),
            new SubRecipe.SpeedMixer()
        };

        public readonly List<ISeqInfo> subRecipesSeqs = new List<ISeqInfo>
        {
            new RecipeWeightInfo(),
            new RecipeSpeedMixerInfo()
        };

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Recipe(Action action, Frame frameMain_arg = null, Frame frameInfoCycle_arg = null, string recipeName = "")
        {
            nRow = 1;
            status = MySettings["Status"].Split(',');
            frameMain = frameMain_arg;
            isFrameLoaded = false;

            MyDatabase.Initialize(new Database.IniInfo()
            {
                //AlarmType_Alarm = Settings.Default.AlarmType_Alarm,
                //AlarmType_Warning = Settings.Default.AlarmType_Warning
            });

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            InitializeComponent();

            switch (action)
            {
                case Action.New:
                    gridNewRecipe.Visibility = Visibility.Visible;
                    Create_NewSequence(MySettings["SubRecipeWeight_SeqType"]);
                    break;
                case Action.Modify: // pour ça je pense qu'une comboBox est suffisant, on puet imaginer une fenêtre intermédiaire avec une liste et une champ pour filtrer mais ça me semble pas applicable à notre besoin
                    frameInfoCycle = frameInfoCycle_arg;
                    gridModify_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToModify, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PRODnDRAFT);

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
                    General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PRODnDRAFT);
                    isCbxToCopyAvailable = true;
                    break;
                case Action.Delete:
                    gridDelete_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PRODnDRAFT);
                    isCbxToDeleteAvailable = true;
                    break;
                default:
                    break;
            }
            //MessageBox.Show("Recipe");
            // Si on a testé une recette et qu'on revient ici alors on ne se déconnecte pas
            if (action != Action.Modify || recipeName == "") MyDatabase.Disconnect();
        }
        ~Recipe()
        {

        }

        /* Create_NewRecipe()
         * 
         * Crée une nouvelle recette à partir des valeurs de l'interface graphique
         * S'assure que le format des paramètres est correct
         * 
         * Remarque pour celui qui utilise cette méthode: assure toi que le programme est connecté à la base de données et déconnecte toi après
         * 
         */
        private bool Create_NewRecipe(string recipeName, int new_version, string status, bool isRecipeCreated = true)
        {
            int i = 1;
            int n;
            string[] values = new string[1];
            List<string[]> allValues = new List<string[]>();
            string[] tableNameSubRecipes = MySettings["SubRecipes_Table_Name"].Split(',');

            bool isFormatOk = true;
            bool result = false;

            string row;

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            List<ISeqInfo> seqInfoList = new List<ISeqInfo>();
            ISubRecipe recipeSeq;

            if (!MyDatabase.IsConnected()) {
                MessageBox.Show("La base de données n'est pas connecté");
                return false;
            }

            if (recipeName == "") {
                MessageBox.Show("You should know by now that a recipe name can't be empty...");
                return false;
            }

            if (new_version <= 0) {
                MessageBox.Show("Not good, not good at all");
                return false;
            }

            // Si on créer une nouvelle recette (version = 1)
            // On contrôle si la recette n'existe pas déjà
            if (new_version == 1 && 
                isRecipeCreated && 
                MyDatabase.GetMax_old(MySettings["Table_Name"], MySettings["Column_Recipe_version"], new string[] { MySettings["Column_Recipe_name"] }, new string[] { recipeName }) != 0) {
                MessageBox.Show("Le nom de la recette existe déjà");
                return false;
            }

            // pourquoi faire ça, à voir...
            //using (RecipeInfo recipeInfo = new RecipeInfo()) {}

            RecipeInfo recipeInfo = new RecipeInfo();
            recipeInfo.columns[recipeInfo.recipeName].value = recipeName;
            recipeInfo.columns[recipeInfo.version].value = new_version.ToString();
            recipeInfo.columns[recipeInfo.status].value = status;

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
                MessageBox.Show("Les calculs sont pas bons");
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

                    isRecordOk = MyDatabase.InsertRow(seqInfoList[i]);
                    seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqId].value = MyDatabase.GetMax_old(seqInfoList[i].name, "id").ToString();
                    seqInfoList[i - 1].columns[seqInfoList[i - 1].nextSeqType].value = seqInfoList[i].seqType.ToString();
                }
                else break;
            }
            if (isRecordOk) isRecordOk = MyDatabase.InsertRow(seqInfoList[0]);

            if(isRecordOk) {
                MessageBox.Show("Jusqu'ici tout va bien");
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

                    MyDatabase.DeleteRow(seqInfoList[i + 1], seqInfoList[i].columns[seqInfoList[i].nextSeqId].value);

                    //MyDatabase.DeleteRow_old(seqInfoList[i + 1].name,
                        //new string[] { seqInfoList[i].columns[seqInfoList[i].id].id },
                        //new string[] { seqInfoList[i].columns[seqInfoList[i].nextSeqId].value });
                    i++;
                } while (seqInfoList[i].columns[seqInfoList[i].nextSeqId].value != null);
            }

            MessageBox.Show("La recette n'a pas pu être créé");
            return false;
        }
        private async void Display_Recipe(string id)
        {
            string nextSeqType;
            string nextSeqID;
            Frame currentFrame;
            List<Frame> framesToDelete = new List<Frame>();
            RecipeInfo recipeInfo = new RecipeInfo();
            ISubRecipe currentPage;
            ISeqInfo currentRecipeSeq;
            /*
            List<ISeqInfo> recipes = new List<ISeqInfo>
            {
                        new RecipeWeightInfo(),
                        new RecipeSpeedMixerInfo()
            };*/


            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected()) // while loop is better
            {
                recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(recipeInfo.GetType(), id);
                //array = MyDatabase.GetOneRow(MySettings["Table_Name"].ToString(), whereColumns: new string[] { MySettings["Column_id"] }, whereValues: new string[] { id });

                if (recipeInfo.columns != null) // Si la requête envoyer ne contient qu'une seule ligne
                {
                    nextSeqType = recipeInfo.columns[recipeInfo.nextSeqType].value;
                    nextSeqID = recipeInfo.columns[recipeInfo.nextSeqId].value;
                    currentRecipeVersion = recipeInfo.columns[recipeInfo.version].value;
                    currentRecipeStatus = status[int.Parse(recipeInfo.columns[recipeInfo.status].value)];

                    string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
                    int[] dbSubRecipeNColumns = Array.ConvertAll(MySettings["SubRecipes_N_Columns"].Split(','), int.Parse);

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
                        while (!isFrameLoaded) await Task.Delay(25); // On attend que la frame ai bien été supprimée
                    }

                    do // On remplie les frames ici
                    {
                        isFrameLoaded = false;
                        Create_NewSequence(nextSeqType);

                        while (!isFrameLoaded) await Task.Delay(25); // On attend que la frame créée (New_Sequence(nextSeqType == "1");) a été chargé
                        isFrameLoaded = false;

                        if (gridMain.Children[gridMain.Children.Count - 1].GetType().Equals(typeof(Frame))) // Si le dernier élément de la grille gridMain est une frame on continue
                        {
                            MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                            currentRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(subRecipesSeqs[int.Parse(nextSeqType)].GetType(), nextSeqID);
                            //int mutexID = MyDatabase.SendCommand_Read(tableName: dbSubRecipeName[int.Parse(nextSeqType)], whereColumns: MySettings["Column_id"].Split(','), whereValues: new string[] { nextSeqID }, isMutexReleased: false);
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
                                MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                nextSeqID = "";
                            }
                            //MyDatabase.Signal(mutexID);
                        }
                        else
                        {
                            MessageBox.Show("Je ne comprends pas... Pourquoi je ne vois pas de frame...");
                        }
                    } while (nextSeqID != "");
                }
                else
                {
                    MessageBox.Show("C'est quoi ça ? La version demandée existe en plusieurs exemplaires !");
                }
            }
            else
            {
                MessageBox.Show("Not good brotha");
                MyDatabase.ConnectAsync();
            }
            curMethodDoneOnGoing = false;
        }
        private void Create_NewSequence(string seqType)
        {
            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            frame.ContentRendered += SubRecipeFrame_ContentRendered;
            //frame.PreviewMouseDoubleClick += FrameTest;

            if (seqType == MySettings["SubRecipeWeight_SeqType"])
            {
                frame.Content = new SubRecipe.Weight(frame, nRow.ToString());
            }
            else if (seqType == MySettings["SubRecipeSpeedMixer_SeqType"])
            {
                frame.Content = new SubRecipe.SpeedMixer(frame, nRow.ToString());
            }

            Grid.SetRow(frame, gridMain.RowDefinitions.Count() - 1);
            gridMain.Children.Add(frame);
            nRow++;
        }

        private void FrameTest(object sender, MouseButtonEventArgs e)
        {
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
            int i = 1;

            foreach (UIElement element in gridMain.Children)
            {
                if (element.GetType().Equals(typeof(Frame)))
                {
                    Frame frame = element as Frame;
                    if (frame.Content.GetType().Equals(typeof(SubRecipe.Weight)))
                    {
                        SubRecipe.Weight currentpage = frame.Content as SubRecipe.Weight;
                        currentpage.SetSeqNumber(i.ToString());
                        i++;
                    }
                    else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer)))
                    {
                        SubRecipe.SpeedMixer currentpage = frame.Content as SubRecipe.SpeedMixer;
                        currentpage.SetSeqNumber(i.ToString());
                        i++;
                    }
                }
            }
        }
        private void Delete_Recipe(string id)
        {
            ISeqInfo subRecipeSeq;
            string nextSetType;
            string nextSetId;
            string[] tableNameSubRecipes = MySettings["SubRecipes_Table_Name"].Split(',');

            if (!MyDatabase.IsConnected()) // while loop is better
            {
                MessageBox.Show("La base de données n'est pas connecté");
                return;
            }

            RecipeInfo recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(new RecipeInfo().GetType(), id);

            if (recipeInfo == null)
            {
                MessageBox.Show("Delete_Recipe - La recette demandée n'existe pas");
                return;
            }

            nextSetType = recipeInfo.columns[recipeInfo.nextSeqType].value;
            nextSetId = recipeInfo.columns[recipeInfo.nextSeqId].value;

            MyDatabase.DeleteRow(new RecipeInfo(), id);

            while (nextSetType != "" && nextSetType != null)
            {
                subRecipeSeq = (ISeqInfo)MyDatabase.GetOneRow(subRecipesSeqs[int.Parse(nextSetType)].GetType(), nextSetId);

                if (subRecipeSeq != null)
                {
                    MyDatabase.DeleteRow(subRecipeSeq, nextSetId);
                    nextSetType = subRecipeSeq.columns[subRecipeSeq.nextSeqType].value;
                    nextSetId = subRecipeSeq.columns[subRecipeSeq.nextSeqId].value;
                }
                else
                {
                    nextSetType = "";
                    MessageBox.Show("Delete_Recipe - Bizarre");
                }
            }
        }

        // EVENTS

        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            if (frameMain.Content != this)
            {
                //MessageBox.Show("FrameMain_ContentRendered");
                //MyDatabase.Disconnect();
            }
        }
        private void SubRecipeFrame_ContentRendered(object sender, EventArgs e)
        {
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
            Create_NewSequence(MySettings["SubRecipeWeight_SeqType"]);
        }
        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            Create_NewRecipe(tbRecipeNameNew.Text, 1, MySettings["Recipe_Status_Draft"]);
            //MessageBox.Show("ButtonCreate_Click");
            MyDatabase.Disconnect();
        }
        private async void CbxPgmToModify_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isCbxToModifAvailable)
            {
                if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                labelVersion.Text = currentRecipeVersion;
                labelStatus.Text = currentRecipeStatus;

                while (curMethodDoneOnGoing) await Task.Delay(25); 

                panelInfoRecipe.Visibility = Visibility.Visible;
                //MessageBox.Show("CbxPgmToModify_SelectionChanged");
                MyDatabase.Disconnect();
            }
        }
        private void ButtonModify_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = cbxPgmToModify.SelectedIndex;

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected()) // while loop is better
            {
                if (labelStatus.Text == status[int.Parse(MySettings["Recipe_Status_Production"])])
                {
                    if (MessageBox.Show("Vous vous apprêtez à modifier une recette en production, une nouvelle version sera créée. Voulez-vous continuer?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Création d'une nouvelle recette, l'ancienne version sera obsolète
                        if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text)+1, MySettings["Recipe_Status_Draft"], false))
                        {
                            RecipeInfo recipeInfo = new RecipeInfo();
                            recipeInfo.columns[recipeInfo.status].value = MySettings["Recipe_Status_Obsolete"];
                            MyDatabase.Update_Row(recipeInfo, ProgramIDs[currentIndex]);

                            //MyDatabase.Update_Row("recipe", new string[] { "status" }, new string[] { MySettings["Recipe_Status_Obsolete"] }, ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = MyDatabase.GetMax_old("recipe", "id").ToString();
                            labelStatus.Text = status[int.Parse(MySettings["Recipe_Status_Draft"])];
                            labelVersion.Text = (int.Parse(labelVersion.Text) + 1).ToString();
                        }
                    }
                }
                else if (labelStatus.Text == status[int.Parse(MySettings["Recipe_Status_Draft"])])
                {
                    if (MessageBox.Show("Voulez vous libérer cette recette en production?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Modification de la recette puis modification du status en draft
                        if(Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text), MySettings["Recipe_Status_Production"], false))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = MyDatabase.GetMax_old("recipe", "id").ToString();
                            labelStatus.Text = status[int.Parse(MySettings["Recipe_Status_Production"])];
                        }
                    }
                    else
                    {
                        // Modification de la recette draft en cours
                        if(Create_NewRecipe(ProgramNames[cbxPgmToModify.SelectedIndex], int.Parse(labelVersion.Text), MySettings["Recipe_Status_Draft"], false))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = MyDatabase.GetMax_old("recipe", "id").ToString();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Tu ne devrais pas pouvoir faire ça");
                }
            }/*
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                MyDatabase.ConnectAsync();
            }*/
            //MessageBox.Show("ButtonModify_Click");
            MyDatabase.Disconnect();
        }
        private void ButtonActDel_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = cbxPgmToActDelete.SelectedIndex;
            RecipeInfo recipeInfo;
            RecipeInfo recipeToUpdate;
            //int index0;
            //string[] array;

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected()) // while loop is better
            {
                recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(new RecipeInfo().GetType(), ProgramIDs[currentIndex]);
                /*
                try
                {
                    array = MyDatabase.GetOneRow("recipe", whereColumns: new string[] { "id" }, whereValues: new string[] { ProgramIDs[currentIndex] });
                }
                catch (Exception)
                {
                    array = new string[0];
                }*/

                if (recipeInfo == null)
                {
                    MessageBox.Show("La recette sélectionné n'existe pas");
                }
                else
                {
                    if ((bool)rbDelete.IsChecked)
                    {
                        if (recipeInfo.columns[recipeInfo.status].value == MySettings["Recipe_Status_Production"])
                        {
                            if (MessageBox.Show("Vous vous apprêtez à rendre obsolète la recette " + recipeInfo.columns[recipeInfo.recipeName].value + " version " + recipeInfo.columns[recipeInfo.version].value + " actuellement en production. Voulez-vous continuer?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                recipeToUpdate = new RecipeInfo();
                                recipeToUpdate.columns[recipeToUpdate.status].value = MySettings["Recipe_Status_Obsolete"];
                                MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                                //MyDatabase.Update_Row("recipe", new string[] { "status" }, new string[] { MySettings["Recipe_Status_Obsolete"] }, ProgramIDs[currentIndex]);

                                ProgramIDs.RemoveAt(currentIndex);
                                ProgramNames.RemoveAt(currentIndex);

                                CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                                MessageBox.Show("D'accord, faisons comme ça");
                            }
                        }
                        else if (recipeInfo.columns[5].value == MySettings["Recipe_Status_Draft"])
                        {
                            if (MessageBox.Show("Vous vous apprêtez à supprimer la recette " + recipeInfo.columns[recipeInfo.recipeName].value + " version " + recipeInfo.columns[recipeInfo.version].value + " actuellement en draft. Voulez-vous continuer?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                Delete_Recipe(ProgramIDs[currentIndex]);

                                if (int.Parse(recipeInfo.columns[4].value) > 1)
                                {
                                    ProgramIDs[currentIndex] = MyDatabase.GetMax_old("recipe", "id", whereColumns: new string[] { "name" }, whereValues: new string[] { recipeInfo.columns[recipeInfo.recipeName].value }).ToString();

                                    recipeToUpdate = new RecipeInfo();
                                    recipeToUpdate.columns[recipeToUpdate.status].value = MySettings["Recipe_Status_Production"];
                                    MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                                    //MyDatabase.Update_Row("recipe", new string[] { "status" }, new string[] { MySettings["Recipe_Status_Production"] }, ProgramIDs[currentIndex]);
                                }
                                else if (int.Parse(recipeInfo.columns[recipeInfo.version].value) == 1)
                                {
                                    ProgramIDs.RemoveAt(currentIndex);
                                    ProgramNames.RemoveAt(currentIndex);
                                }
                                else
                                {
                                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ButtonDelete_Click - version: " + recipeInfo.columns[recipeInfo.version].value);
                                }

                                // mettre ça dans une fonction et on recommence tout
                                CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                                MessageBox.Show("Si tu insistes");
                            }
                        }
                        else
                        {
                            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ButtonDelete_Click - Status: " + status[int.Parse(recipeInfo.columns[recipeInfo.status].value)]);
                        }
                    }
                    else if ((bool)rbActivate.IsChecked)
                    {
                        if (MessageBox.Show("Vous vous apprêtez à faire revenir la recette " + recipeInfo.columns[recipeInfo.recipeName].value + " version " + recipeInfo.columns[recipeInfo.version].value + " en production. Voulez-vous continuer?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            // using partout
                            recipeToUpdate = new RecipeInfo();
                            recipeToUpdate.columns[recipeToUpdate.status].value = MySettings["Recipe_Status_Production"];
                            MyDatabase.Update_Row(recipeToUpdate, ProgramIDs[currentIndex]);

                            //MyDatabase.Update_Row("recipe", new string[] { "status" }, new string[] { MySettings["Recipe_Status_Production"] }, ProgramIDs[currentIndex]);
                            ProgramIDs.RemoveAt(currentIndex);
                            ProgramNames.RemoveAt(currentIndex);

                            CbxAddDefaultText(cbxPgmToActDelete, currentIndex);
                            /*
                            index0 = (ProgramIDs.Count > 1 && currentIndex == 0) ? 1 : 0;
                            ProgramNames.Insert(index0, "Veuillez sélectionner une recette");
                            cbxPgmToActDelete.SelectedIndex = index0;
                            ProgramNames.RemoveAt(index0);
                            cbxPgmToActDelete.Items.Refresh();
                            */
                            MessageBox.Show("Ben voilà !");
                        }
                    }
                    else
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - ButtonActDel_Click - Comment t'as fait ça ?");
                    }
                }
            }/*
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                MyDatabase.ConnectAsync();
            }*/
            //MessageBox.Show("ButtonActDel_Click");
            MyDatabase.Disconnect();
        }
        private void CbxAddDefaultText(ComboBox comboBox, int index)
        {
            //MessageBox.Show(ProgramIDs.Count.ToString());
            int index0 = (ProgramIDs.Count > 0 && index == 0) ? 1 : 0;

            if (ProgramIDs.Count == 0)
            {
                ProgramNames.Insert(0, "Veuillez sélectionner une recette");
                ProgramNames.Insert(0, "Veuillez sélectionner une recette");
                comboBox.SelectedIndex = 1;
                ProgramNames.RemoveAt(0);
                ProgramNames.RemoveAt(0);
            }
            else
            {
                ProgramNames.Insert(index0, "Veuillez sélectionner une recette");
                comboBox.SelectedIndex = index0;
                ProgramNames.RemoveAt(index0);
            }

            comboBox.Items.Refresh();
        }
        private void RbActivateDelete_Checked(object sender, RoutedEventArgs e)
        {

            if (isCbxToDeleteAvailable)
            {
                RadioButton radioButton = sender as RadioButton;

                if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                if (radioButton == rbActivate)
                {
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.OBSOLETE);
                    btDelAct.Content = "Activer recette";
                }
                else if (radioButton == rbDelete)
                {
                    General.Update_RecipeNames(cbxPgmToActDelete, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PRODnDRAFT);
                    btDelAct.Content = "Supprimer recette";
                }
                //MessageBox.Show("RbActivateDelete_Checked");
                MyDatabase.Disconnect();
            }
        }
        private void Test_Sequence_Click(object sender, RoutedEventArgs e)
        {
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
                if (MessageBox.Show("Voulez-vous démarrer le cycle? Assurez-vous d'avoir sauvegarder la recette avant de la tester", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    string id = ProgramIDs[cbxPgmToModify.SelectedIndex];
                    General.StartCycle(id, "NA", finalWeight.ToString(), frameMain, frameInfoCycle, true);
                }
            }
            else
            {
                MessageBox.Show("La masse final n'est pas correcte");
            }
        }
        private async void CbxPgmToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isCbxToCopyAvailable)
            {
                if (!MyDatabase.IsConnected()) MyDatabase.Connect();

                isCbxVersionCpyAvailable = false;

                ComboBox comboBox = sender as ComboBox;
                curMethodDoneOnGoing = true;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                ProgramVersions.Clear();

                if (MyDatabase.IsConnected())
                {
                    int mutexID = MyDatabase.SendCommand_Read_old(tableName: "recipe", selectColumns: "version",
                        whereColumns: new string[] { "name" },
                        whereValues: new string[] { ProgramNames[comboBox.SelectedIndex] }, orderBy: "version", isOrderAsc: false, 
                        isMutexReleased: false);

                    string[] array = MyDatabase.ReadNext(mutexID);

                    while (array.Length != 0)
                    {
                        ProgramVersions.Add(array[0]);
                        array = MyDatabase.ReadNext(mutexID);
                    }
                    MyDatabase.Signal(mutexID);

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
                    ProgramVersions.Add("###");
                    ProgramVersions.Add("###");
                    ProgramVersions.Add("###");

                    cbxVersionToCopy.ItemsSource = ProgramVersions;
                    cbxVersionToCopy.Items.Refresh();
                    cbxVersionToCopy.SelectedIndex = 0;

                    MessageBox.Show("Not good brotha");
                }

                while (curMethodDoneOnGoing) await Task.Delay(25);

                panelVersionRecipe.Visibility = Visibility.Visible;
                //MessageBox.Show("CbxPgmToCopy_SelectionChanged");
                MyDatabase.Disconnect();
            }
        }
        private void CbxVersionToCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isCbxVersionCpyAvailable)
            {
                ComboBox comboBox = sender as ComboBox;

                RecipeInfo recipeInfo = new RecipeInfo();
                recipeInfo.columns[recipeInfo.recipeName].value = cbxPgmToCopy.Text;
                recipeInfo.columns[recipeInfo.version].value = ProgramVersions[comboBox.SelectedIndex];

                recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(table: recipeInfo);
                Display_Recipe(recipeInfo.columns[recipeInfo.id].value);
            }
        }
        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (Create_NewRecipe(tbRecipeNameCpy.Text, 1, MySettings["Recipe_Status_Draft"], false))
            {
                isCbxToCopyAvailable = false;
                General.Update_RecipeNames(cbxPgmToCopy, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PRODnDRAFT);
                panelVersionRecipe.Visibility = Visibility.Collapsed;
                isCbxToCopyAvailable = true;
            }
            //MessageBox.Show("ButtonCopy_Click");
            MyDatabase.Disconnect();
        }

        // TOOLS
        private static void CreateTextFile(string fileName, string folderPath)
        {
            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
            }
        }
        private static void WriteInTextFile(string fileName, string text)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = File.Open(fileName, FileMode.Append))
                    {
                        byte[] text_b = new UTF8Encoding(true).GetBytes(text + "\n");
                        fs.Write(text_b, 0, text_b.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Tu ne sais pas ce que tu fais avoue");
            }
        }
        private static string[] ReadTextFile(string fileName)
        {
            string[] lines = new string[0];

            if (File.Exists(fileName))
            {
                try
                {
                    lines = File.ReadLines(fileName).ToArray();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Tu t'es perdu non ?");
            }

            return lines;
        }
        public static void ExportTable(string tableName, string folderPath)
        {
            int start = 0;
            int range = 500;
            int end = 1 + range;
            int mutexID = MyDatabase.SendCommand_ReadPart(tableName: tableName, start: start, end: end, isMutexReleased: false);
            //int mutexID = MyDatabase.SendCommand_Read(tableName: tableName, isMutexReleased: false);

            if (!MyDatabase.IsReaderNotAvailable())
            {
                string fileName = folderPath + @"\" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + "_" + tableName + fileExtension_table;
                CreateTextFile(fileName, folderPath);
                ReadOnlyCollection<DbColumn> columns = MyDatabase.GetColumnCollection();

                string[] array;
                string command1 = "INSERT INTO " + tableName + " (";
                string command2;
                int n = columns.Count;

                for (int i = 1; i < n - 1; i++)
                {
                    command1 += columns[i].ColumnName + ", ";
                }
                command1 += columns[n - 1].ColumnName + ") VALUES (\"";

                array = MyDatabase.ReadNext(mutexID);

                while (array.Length != 0)
                {
                    while (array.Length != 0)
                    {
                        command2 = "";

                        for (int i = 1; i < n - 1; i++)
                        {
                            command2 += array[i] + "\", \"";
                        }
                        command2 += array[n - 1] + "\");";

                        WriteInTextFile(fileName, command1 + command2);
                        array = MyDatabase.ReadNext(mutexID);
                    }
                    MyDatabase.Signal(mutexID);

                    start += range;
                    end += range;

                    //MessageBox.Show("1");

                    mutexID = MyDatabase.SendCommand_ReadPart(tableName: tableName, start: start, end: end, isMutexReleased: false);
                    array = MyDatabase.ReadNext(mutexID);
                }
                MyDatabase.Signal(mutexID);
                //MessageBox.Show("2");
            }

        }
        public static void ExportAllTables()
        {
            string folderPath = exportFolder + @"\" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + "_" + "ExportAllTables";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(exportFolder);
            }

            if (MyDatabase.IsConnected())
            {
                List<string> tablesName = MyDatabase.GetTablesName();

                for (int i = 0; i < tablesName.Count; i++)
                {
                    ExportTable(tablesName[i], folderPath);
                }

            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Base de données pas connectée");
            }
        }
        public static void ImportTable(string fileName)
        {
            string[] lines = ReadTextFile(fileName);

            MessageBox.Show(lines.Length.ToString());

            /*
            string command = "";
            int k = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                command += lines[i];

                if (lines[i].EndsWith(";"))
                {
                    MyDatabase.SendCommand(command);
                    command = "";
                }
            }*/
        }

        // VERIFIE LA CONNECTION DE LA BASE DE DONNéES PARTOUT !!!!! MOTHER FUCKER ¨¨
    }
}