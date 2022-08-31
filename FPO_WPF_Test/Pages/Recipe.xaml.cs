using Database;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FPO_WPF_Test.Pages
{
    /// <summary>
    /// Logique d'interaction pour Recipe.xaml
    /// </summary>
    public partial class Recipe : Page
    {
        private int nRow;
        private readonly MyDatabase db;
        private readonly string[] status;
        private bool isFrameLoaded;
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;
        private readonly Action CurrentAction;
        private List<string> ProgramNames = new List<string>();
        private List<string> ProgramIDs = new List<string>();
        private bool isComboBoxAvailable;
        private string currentRecipeName;
        private string currentRecipeVersion;
        private string currentRecipeStatus;
        //private General g = new General();

        public class BaseA
        {
            public int ID = 0;
        }
        public class AA : BaseA
        {
            public int AID = 0;
        }
        public class AB : BaseA
        {
            public int BID = 1;
        }
        public class AC : BaseA
        {
            public int CID = 0;
        }
        public Recipe()
        {
            nRow = 1;
            db = new MyDatabase();
            status = MySettings["Status"].Split(',');
            isFrameLoaded = false;
            isComboBoxAvailable = false;
            InitializeComponent();

            gridNewRecipe.Visibility = Visibility.Visible;

            var list = new List<BaseA>
            {
             new AA(),
             new AB(),
             new AC(),
            };
            AA aa = list[1] as AA;
            if (aa != null)
            {
                int id = aa.AID;
                MessageBox.Show("Cool");
            }
        }
        public void DoFooWith(object blob)
        {
            // Get the containing class
            var utilType = typeof(Util);

            // Get the method we want to invoke
            var baseMethod = utilType.GetMethod("Foo", new Type[] { typeof(object) });
            // Get a "type-specific" variant
            var typedForBlob = baseMethod.MakeGenericMethod(blob.GetType());
            // And invoke it
            var res = typedForBlob.Invoke(null, new[] { blob });
        }
        public Recipe(Action action)
        {
            nRow = 1;
            db = new MyDatabase();
            status = MySettings["Status"].Split(',');
            isFrameLoaded = false;
            CurrentAction = action;
            InitializeComponent();

            switch (action)
            {
                case Action.New:
                    gridNewRecipe.Visibility = Visibility.Visible;
                    Create_NewSequence(MySettings["SubRecipeWeight_SeqType"]);
                    break;
                case Action.Modify: // pour ça je pense qu'une comboBox est suffisant, on puet imaginer une fenêtre intermédiaire avec une liste et une champ pour filtrer mais ça me semble pas applicable à notre besoin
                    gridModify_Recipe.Visibility = Visibility.Visible;
                    General.Update_RecipeNames(cbxProgramName, ProgramNames, ProgramIDs);
                    isComboBoxAvailable = true;
                    break;
                case Action.Copy:
                    break;
                case Action.Delete:
                    break;
                default:
                    break;
            }
        }
        ~Recipe()
        {

        }
        private void Frame_ContentRendered(object sender, EventArgs e)
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
        private void Frame_test(object sender, RoutedEventArgs e)
        {
            Frame frame = sender as Frame;
            Grid grid = frame.Parent as Grid;

            MessageBox.Show(gridMain.RowDefinitions.Count().ToString() + " Row: " + Grid.GetRow(frame).ToString());
        }
        private void New_Sequence_Click(object sender, RoutedEventArgs e)
        {
            Create_NewSequence(MySettings["SubRecipeWeight_SeqType"]);
        }
        private void WriteDB_Click(object sender, RoutedEventArgs e)
        {
            string recipeName = tbProgramName.Text;
            int newVersion;
            string[] whereColumns = new string[] { MySettings["Column_Recipe_name"] };

            if (recipeName != "")
            {
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected())
                {
                    newVersion = db.GetMax(MySettings["Table_Name"], MySettings["Column_Recipe_version"], whereColumns, new string[] { recipeName }) + 1;

                    if (newVersion == 1)
                    {
                        Create_NewRecipe(recipeName, newVersion, MySettings["Recipe_Status_Draft"]);
                    }
                    else
                    {
                        MessageBox.Show("Le nom de la recette existe déjà");
                    }

                    db.Disconnect();
                    //MessageBox.Show("Ouf, ça c'est fait");
                }
                else {
                    MessageBox.Show("Not good brotha");
                }
            }
            else {
                MessageBox.Show("You should know by now that a recipe name can't be empty...");
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
        private bool Create_NewRecipe(string recipeName, int new_version, string status)
        {
            int i = 1;
            int n;
            int[] nextSeqID;
            string[] values = new string[1];
            List<string[]> allValues = new List<string[]>();
            string columnsRecipe = MySettings["Columns_Recipe"];
            string columnsSubRecipeWeight = MySettings["Columns_SubRecipe_Weight"];
            string columnsSpeedMixer = MySettings["Columns_SubRecipe_SpeedMixer"];
            string[] columnsSubRecipes = new string[] { columnsSubRecipeWeight, columnsSpeedMixer };
            string[] tableNameSubRecipes = MySettings["SubRecipes_Table_Name"].Split(',');

            var currentPage = new List<Page>
                {
                    new SubRecipe.Weight(),
                    new SubRecipe.SpeedMixer()
                };
            int currentType;
            int previousType;
            bool isFormatOk = true;
            bool result;

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                nextSeqID = new int[] { db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeWeight_SeqType"])], "id"), db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeSpeedMixer_SeqType"])], "id") };

                if (new_version != 0) // && previousStatus == MySettings["Recipe_Status_Production"])
                {
                    foreach (UIElement element in gridMain.Children) // Pour chaque element de la grille principale (gridMain)...
                    {
                        if (element.GetType().Equals(typeof(Frame))) // Si c'est une frame...
                        {
                            Frame frame = element as Frame;
                            if (frame.Content.GetType().Equals(typeof(SubRecipe.Weight))) // Si la frame en question représente une séquence de pesée, on met à jour les bonnes variables pour la séquence
                            {
                                //MessageBox.Show("Weight - i = " + $"{i}");
                                currentType = 0;
                                currentPage[currentType] = frame.Content as SubRecipe.Weight;
                            }
                            else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer))) // Idem si la frame en question représente une séquence de mix
                            {
                                //MessageBox.Show("Speed Mixer - i = " + $"{i}");
                                currentType = 1;
                                currentPage[currentType] = frame.Content as SubRecipe.SpeedMixer;
                            }
                            else // Sinon il y a un problème, on met à -1 (valeur impossible) la variable currentType, voir "if (currentType != -1)" plus bas
                            {
                                MessageBox.Show("-1");
                                currentType = -1;
                            }

                            if (currentType != -1) // Si la frame actuelle est valide...
                            {
                                if (i == 1) // Si la frame actuelle est la première, on enregistre les données de la recette dans le tableau "recipe"
                                {
                                    nextSeqID[currentType]++;
                                    //values = new string[] { $"{currentType}", nextSeqID[currentType].ToString(), recipeName, new_version.ToString(), MySettings["Recipe_Status_Draft"] };
                                    values = new string[] { $"{currentType}", "#", recipeName, new_version.ToString(), status };

                                    allValues.Add(values);
                                }
                                else // Sinon on va s'occuper des tableaux recipe_weight et/ou recipe_speedmixer
                                {
                                    nextSeqID[currentType]++; // = nextSeqID[currentType] + 1;// (previousType == currentType ? 1 : 1);
                                    values[0] = $"{currentType}";
                                    values[1] = "#"; // nextSeqID[currentType].ToString();

                                    allValues.Add(values);
                                }

                                // Ici on lit le contenu de la frame, qu'elle représente une séquence de pesée ou de mix
                                if (currentType == int.Parse(MySettings["SubRecipeWeight_SeqType"]))
                                {
                                    var tempPage = currentPage[currentType] as SubRecipe.Weight;
                                    values = tempPage.GetPage();
                                    if (isFormatOk) isFormatOk = tempPage.IsFormatOk();
                                    previousType = currentType;
                                }
                                else if (currentType == int.Parse(MySettings["SubRecipeSpeedMixer_SeqType"]))
                                {
                                    var tempPage = currentPage[currentType] as SubRecipe.SpeedMixer;
                                    values = tempPage.GetPage();
                                    if (isFormatOk) isFormatOk = tempPage.IsFormatOk();
                                    previousType = currentType;
                                }
                                else
                                {
                                    MessageBox.Show("Oups");
                                    values = null;
                                    result = false;
                                }
                                i++;
                            }
                            else
                            {
                                MessageBox.Show("Etrange... TRES étrange...");
                                result = false;
                            }
                        }
                    }

                    allValues.Add(values); // On enregistre le dernier set de valeurs ici
                }
                else
                {
                    MessageBox.Show("Not good, not good at all");
                    result = false;
                }

                if (isFormatOk) // Si toutes les séquences sont correctement renseignée, on mets à jour la base de données
                {
                    bool isRecordOk;
                    n = i;
                    isRecordOk = true;

                    for (i = n - 1; i > 0; i--)
                    {
                        if (isRecordOk) isRecordOk = db.InsertRow(tableNameSubRecipes[int.Parse(allValues[i - 1][0])], columnsSubRecipes[int.Parse(allValues[i - 1][0])], allValues[i]);
                        else break; // S'il y a une erreur, on arrête la boucle

                        allValues[i - 1][1] = allValues[i - 1][0] == MySettings["SubRecipeWeight_SeqType"] ? db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeWeight_SeqType"])], "id").ToString() : db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeSpeedMixer_SeqType"])], "id").ToString();
                    }

                    if (isRecordOk) isRecordOk = db.InsertRow(MySettings["Table_Name"], columnsRecipe, allValues[0]);

                    // S'il y a eu une erreur, on supprime les lignes qui ont été créés.
                    if (!isRecordOk && i != n - 2)
                    {
                        i++;

                        do
                        {
                            db.DeleteRow(tableNameSubRecipes[int.Parse(allValues[i][0])], new string[] { MySettings["Column_id"] }, new string[] { allValues[i][1] });
                            i++;
                        } while (allValues[i][1] != null);

                        MessageBox.Show("La recette n'a pas pu être créé");
                        result = false;
                    }
                    else if (!isRecordOk) 
                    {
                        MessageBox.Show("La recette n'a pas pu être créé");
                        result = false;
                    }
                    else
                    {
                        result = true;
                        MessageBox.Show("Jusqu'ici tout va bien");
                    }
                }
                else
                {
                    MessageBox.Show("Les calculs sont pas bons");
                    result = false;
                }
                //db.Disconnect();
            }
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                db.ConnectAsync();
                result = false;
            }
            return result;
        }
        private async void Display_Recipe(string id)
        {
            string[] array;
            string nextSeqType;
            string nextSeqID;
            Frame currentFrame;
            List<Frame> framesToDelete = new List<Frame>();
            var currentPage = new List<Page>
                    {
                        new SubRecipe.Weight(),
                        new SubRecipe.SpeedMixer()
                    };

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_readAllRecipe(MySettings["Table_Name"].ToString(), new string[] { MySettings["Column_id"] }, new string[] { id });

                array = db.ReadNext();

                if (array.Count() != 0 && db.ReadNext().Count() == 0) // Si la requête envoyer ne contient qu'une seule ligne
                {
                    nextSeqType = array[1];
                    nextSeqID = array[2];
                    currentRecipeName = array[3];
                    currentRecipeVersion = array[4];
                    currentRecipeStatus = status[int.Parse(array[5])];

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
                                db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                                db.SendCommand_readAllRecipe(dbSubRecipeName[int.Parse(nextSeqType)], MySettings["Column_id"].Split(','), new string[] { nextSeqID });
                                currentFrame = gridMain.Children[gridMain.Children.Count - 1] as Frame;
                            
                            if (nextSeqType == MySettings["SubRecipeWeight_SeqType"]) {
                                currentPage[int.Parse(nextSeqType)] = currentFrame.Content as SubRecipe.Weight;
                            }
                            else if(nextSeqType == MySettings["SubRecipeSpeedMixer_SeqType"]) {
                                currentPage[int.Parse(nextSeqType)] = currentFrame.Content as SubRecipe.SpeedMixer;
                            }

                            array = db.ReadNext();
                            
                            if (array.Count() == dbSubRecipeNColumns[int.Parse(nextSeqType)] && db.ReadNext().Count() == 0) {
                                    if (nextSeqType == MySettings["SubRecipeWeight_SeqType"]) {
                                    var tempPage = currentPage[int.Parse(nextSeqType)] as SubRecipe.Weight;
                                        tempPage.SetPage(array);
                                    }
                                    else if (nextSeqType == MySettings["SubRecipeSpeedMixer_SeqType"]) {
                                    var tempPage = currentPage[int.Parse(nextSeqType)] as SubRecipe.SpeedMixer;
                                        tempPage.SetPage(array);
                                    }

                                    nextSeqType = array[1];
                                    nextSeqID = array[2];
                                }
                                else {
                                    MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                    nextSeqID = "";
                                }
                        }
                        else {
                            MessageBox.Show("Je ne comprends pas... Pourquoi je ne vois pas de frame...");
                        }
                    } while (nextSeqID != "");

                    //MessageBox.Show("Ouf c'est fini");
                }
                else {
                    if (array.Count() == 0) {
                        MessageBox.Show("La recette demandée n'existe pas");
                    }
                    else {
                        MessageBox.Show("C'est quoi ça ? La version demandée existe en plusieurs exemplaires !");
                    }
                }

                db.Disconnect();
            }
            else {
                MessageBox.Show("Not good brotha");
                db.ConnectAsync();
            }

        }
        private void Create_NewSequence(string seqType)
        {
            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            //frame.MouseDoubleClick += new MouseButtonEventHandler(Frame_test);
            frame.ContentRendered += Frame_ContentRendered;

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
                        currentpage.setSeqNumber(i.ToString());
                        i++;
                    }
                }
            }
        }
        private void Delete_Recipe(string id)
        {
            // ProgramIDs[cbxProgramName.SelectedIndex]

            string[] array;
            string nextSetType;
            string nextSetId;
            string[] tableNameSubRecipes = MySettings["SubRecipes_Table_Name"].Split(',');

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_Read("recipe", "first_seq_type, first_seq_id", new string[] { "id" }, new string[] { id });
                
                array = db.ReadNext();

                if (array.Count() != 0 && db.ReadNext().Count() == 0)
                {
                    try
                    {
                        db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                        nextSetType = array[0];
                        nextSetId = array[1];
                        db.DeleteRow("recipe", new string[] { MySettings["Column_id"] }, new string[] { id });

                        while (nextSetType != "" && nextSetType != null)
                        {
                            db.SendCommand_Read(tableNameSubRecipes[int.Parse(nextSetType)], "next_seq_type, next_seq_id", new string[] { "id" }, new string[] { nextSetId });

                            array = db.ReadNext();

                            if (array.Count() != 0 && db.ReadNext().Count() == 0)
                            {
                                db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                                db.DeleteRow(tableNameSubRecipes[int.Parse(nextSetType)], new string[] { MySettings["Column_id"] }, new string[] { nextSetId });
                                nextSetType = array[0];
                                nextSetId = array[1];
                            }
                            else
                            {
                                nextSetType = "";
                                MessageBox.Show("Delete_Recipe - Bizarre");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }
                else
                {
                    if (array.Count() == 0)
                    {
                        MessageBox.Show("Delete_Recipe - La recette demandée n'existe pas");
                    }
                    else
                    {
                        MessageBox.Show("Delete_Recipe - C'est quoi ça ? La version demandée existe en plusieurs exemplaires !");
                    }
                }

                //db.Disconnect();
            }
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                db.ConnectAsync();
            }
        }
        public static class Util
        {
            public static T Foo<T>(object obj)
            {
                // Do actual stuff here
                return default(T);
            }
        }
        private void cbxProgramName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isComboBoxAvailable)
            {
                ComboBox comboBox = sender as ComboBox;
                Display_Recipe(ProgramIDs[comboBox.SelectedIndex]);

                labelVersion.Text = currentRecipeVersion;
                labelStatus.Text = currentRecipeStatus;

                panelInfoRecipe.Visibility = Visibility.Visible;
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = cbxProgramName.SelectedIndex;

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                if (labelStatus.Text == status[int.Parse(MySettings["Recipe_Status_Production"])])
                {
                    if (MessageBox.Show("Vous vous apprêtez à modifier une recette en production, une nouvelle version sera créée. Voulez-vous continuer?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // Création d'une nouvelle recette, l'ancienne version sera obsolète
                        if (Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text)+1, MySettings["Recipe_Status_Draft"]))
                        {
                            db.Update_Row("recipe", new string[] { "status" }, new string[] { MySettings["Recipe_Status_Obsolete"] }, ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = db.GetMax("recipe", "id").ToString();
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
                        if(Create_NewRecipe(ProgramNames[currentIndex], int.Parse(labelVersion.Text), MySettings["Recipe_Status_Production"]))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = db.GetMax("recipe", "id").ToString();
                            labelStatus.Text = status[int.Parse(MySettings["Recipe_Status_Production"])];
                        }
                    }
                    else
                    {
                        // Modification de la recette draft en cours
                        if(Create_NewRecipe(ProgramNames[cbxProgramName.SelectedIndex], int.Parse(labelVersion.Text), MySettings["Recipe_Status_Draft"]))
                        {
                            Delete_Recipe(ProgramIDs[currentIndex]);
                            ProgramIDs[currentIndex] = db.GetMax("recipe", "id").ToString();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Tu ne devrais pas pouvoir faire ça");
                }
                db.Disconnect();
            }
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                db.ConnectAsync();
            }
        }
    }
}
