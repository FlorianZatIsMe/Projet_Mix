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

        public Recipe()
        {
            nRow = 1;
            db = new MyDatabase();
            status = MySettings["Status"].Split(',');
            isFrameLoaded = false;
            InitializeComponent();


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
            New_Sequence(MySettings["SubRecipeWeight_SeqType"]);
        }

        private void WriteDB_Click(object sender, RoutedEventArgs e)
        {
            int i = 1;
            int j = 0;
            string recipeName = tbProgramName.Text;
            string[] whereColumns = new string[] { "name" };
            string[] whereValues = new string[] { recipeName };
            int new_version;
            string nextSeqType = "";
            int nextSeqID = -1;
            string[] values = new string[1];
            string columnsRecipe = "name, version, status, first_seq_type, first_seq_id";
            string columnsWeight = "next_seq_type, next_seq_id, name, is_barcode_used, barcode, unit, decimal_number, setpoint, min, max";
            string columnsSpeedMixer = "next_seq_type, next_seq_id, name, acceleration, deceleration, vaccum_control, is_ventgas_air, monitor_type, pressure_unit, scurve, coldtrap, speed00, time00, pressure00, speed01, time01, pressure01, speed02, time02, pressure02, speed03, time03, pressure03, speed04, time04, pressure04, speed05, time05, pressure05, speed06, time06, pressure06, speed07, time07, pressure07, speed08, time08, pressure08, speed09, time09, pressure09";

            var currentPage = new List<Page>
                    {
                        new SubRecipe.Weight(),
                        new SubRecipe.SpeedMixer()
                    };
            int currentType = -1;
            string subRecipeName ="";

            if (recipeName != "")
            {
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected())
                {
                    new_version = db.GetMax("recipe", "version", whereColumns, whereValues) + 1;

                    if (new_version != 0)
                    {
                        foreach (UIElement element in gridMain.Children)
                        {
                            if (element.GetType().Equals(typeof(Frame)))
                            {
                                Frame frame = element as Frame;
                                if (frame.Content.GetType().Equals(typeof(SubRecipe.Weight)))
                                {
                                    MessageBox.Show("Weight - i = " + $"{i}");
                                    currentType = 0;
                                    currentPage[currentType] = frame.Content as SubRecipe.Weight;
                                    subRecipeName = "recipe_weight";
                                }
                                else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer)))
                                {
                                    MessageBox.Show("Speed Mixer - i = " + $"{i}");
                                    currentType = 1;
                                    currentPage[currentType] = frame.Content as SubRecipe.SpeedMixer;
                                    subRecipeName = "recipe_speedmixer";
                                }
                                else
                                {
                                    MessageBox.Show("-1");
                                    currentType = -1;
                                }

                                if (currentType != -1)
                                {
                                    if (i == 1)
                                    {
                                        nextSeqType = $"{currentType}";
                                        nextSeqID = db.GetMax(subRecipeName, "id") + 1;
                                        values = new string[] { recipeName, new_version.ToString(), $"{currentType}", nextSeqType, nextSeqID.ToString() };
                                        db.SendCommand_insertRecord("recipe", columnsRecipe, values);
                                        /*
                                         * 
                                         * 
                                         *      Problème: si l'utilisateur crée la recette mais qu'elle contient une erreur (ex: champ vide), une ou plusieurs lignes de seront créé ce qui va créer un mix.
                                         *      Une idée : commencer par tout contrôler avant d'écrire quoi que ce soit ET tout écrire à la fin s'il n'y a pas eu d'erreur (dit autrement, plutôt que d'écrire, stocker les infos) -> s'il y a une seule erreur, on pourra supprimer les lignes créées
                                         * 
                                         */
                                    }
                                    else
                                    {
                                        nextSeqID = db.GetMax(subRecipeName, "id") + (nextSeqType == $"{currentType}" ? 2 : 1);
                                        nextSeqType = $"{currentType}";
                                        values[0] = nextSeqType;
                                        values[1] = nextSeqID.ToString();

                                        if (values.Count() == 11 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was Weight
                                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                                        }
                                        else if (values.Count() == 42 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was SpeedMixer
                                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                                        }
                                        else
                                        {
                                            MessageBox.Show("SpeedMixer: très bizarre tout ça...");
                                        }
                                    }

                                    if (currentType == 0)
                                    {
                                        var tempPage = currentPage[currentType] as SubRecipe.Weight;
                                        values = tempPage.GetPage();
                                    }
                                    else if (currentType == 1)
                                    {
                                        var tempPage = currentPage[currentType] as SubRecipe.SpeedMixer;
                                        values = tempPage.GetPage();
                                    }
                                    else
                                    {
                                        MessageBox.Show("Oups");
                                        values = null;
                                    }

                                    i++;
                                }
                                else
                                {
                                    MessageBox.Show("Etrange... TRES étrange...");
                                }


/*                                }
                                else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer)))
                                {
                                    //MessageBox.Show("Speed Mixer - i = " + $"{i}");
                                    currentPage = frame.Content as SubRecipe.SpeedMixer;
                                    if (i == 1)
                                    {
                                        nextSeqType = "1";
                                        nextSeqID = db.GetMax("recipe_speedmixer", "id") + 1;
                                        values = new string[] { recipeName, new_version.ToString(), "0", nextSeqType, nextSeqID.ToString() };
                                        db.SendCommand_insertRecord("recipe", "name, version, status, first_seq_type, first_seq_id", values);
                                    }
                                    else
                                    {
                                        nextSeqID = db.GetMax("recipe_speedmixer", "id") + (nextSeqType == "1" ? 2 : 1);
                                        nextSeqType = "1";
                                        values[0] = nextSeqType;
                                        values[1] = nextSeqID.ToString();

                                        if (values.Count() == 11-1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was Weight
                                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                                        }
                                        else if(values.Count() == 42-1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was SpeedMixer
                                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                                        }
                                        else
                                        {
                                            MessageBox.Show("SpeedMixer: très bizarre tout ça...");
                                        }
                                    }
                                    values = currentPage.GetPage();
                                    i++;
                                }
                                else
                                {
                                    MessageBox.Show("Vraiment très bizarre ct'histoire...");
                                }*/
                            }
                        }
                        //MessageBox.Show("Last");
                        if (values.Count() == 11 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                        {
                            // The previous sequence was Weight
                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                        }
                        else if (values.Count() == 42 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                        {
                            // The previous sequence was SpeedMixer
                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                        }
                        else
                        {
                            MessageBox.Show("J'y crois pas, tu n'as pas fait de séquence !!!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Not good, not good at all");
                    }

                    db.Disconnect();
                    MessageBox.Show("Ouf, ça c'est fait");
                }
                else
                {
                    MessageBox.Show("Not good brotha");
                    db.ConnectAsync();
                }
            }
            else
            {
                MessageBox.Show("You should know by now that a recipe name can't be empty...");
            }

        }

        /*
        private void WriteDB_Click(object sender, RoutedEventArgs e)
        {
            int i = 1;
            int j = 0;
            string recipeName = tbProgramName.Text;
            string[] whereColumns = new string[] { "name" };
            string[] whereValues = new string[] { recipeName };
            int new_version;
            string nextSeqType = "";
            int nextSeqID = -1;
            string[] values = new string[1];
            SubRecipe.Weight currentpageWeight;
            SubRecipe.SpeedMixer currentpageSpeedMixer;
            string columnsRecipe = "name, version, status, first_seq_type, first_seq_id";
            string columnsWeight = "next_seq_type, next_seq_id, name, is_barcode_used, barcode, unit, decimal_number, setpoint, min, max";
            string columnsSpeedMixer = "next_seq_type, next_seq_id, name, acceleration, deceleration, vaccum_control, is_ventgas_air, monitor_type, pressure_unit, scurve, coldtrap, speed00, time00, pressure00, speed01, time01, pressure01, speed02, time02, pressure02, speed03, time03, pressure03, speed04, time04, pressure04, speed05, time05, pressure05, speed06, time06, pressure06, speed07, time07, pressure07, speed08, time08, pressure08, speed09, time09, pressure09";

            if (recipeName != "")
            {
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected())
                {
                    new_version = db.GetMax("recipe", "version", whereColumns, whereValues) + 1;

                    if (new_version != 0)
                    {
                        foreach (UIElement element in gridMain.Children)
                        {
                            if (element.GetType().Equals(typeof(Frame)))
                            {
                                Frame frame = element as Frame;
                                if (frame.Content.GetType().Equals(typeof(SubRecipe.Weight)))
                                {
                                    //MessageBox.Show("Weight - i = " + $"{i}");
                                    currentpageWeight = frame.Content as SubRecipe.Weight;

                                    if (i == 1)
                                    {
                                        nextSeqType = "0";
                                        nextSeqID = db.GetMax("recipe_weight", "id") + 1;
                                        values = new string[] { recipeName, new_version.ToString(), "0", nextSeqType, nextSeqID.ToString() };
                                        db.SendCommand_insertRecord("recipe", columnsRecipe, values);
                                    }
                                    else
                                    {
                                        nextSeqID = db.GetMax("recipe_weight", "id") + (nextSeqType == "0" ? 2 : 1);
                                        nextSeqType = "0";
                                        values[0] = nextSeqType;
                                        values[1] = nextSeqID.ToString();

                                        if (values.Count() == 11 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was Weight
                                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                                        }
                                        else if (values.Count() == 42 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was SpeedMixer
                                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                                        }
                                        else
                                        {
                                            MessageBox.Show("SpeedMixer: très bizarre tout ça...");
                                        }
                                    }
                                    values = currentpageWeight.GetPage();
                                    i++;
                                }
                                else if (frame.Content.GetType().Equals(typeof(SubRecipe.SpeedMixer)))
                                {
                                    //MessageBox.Show("Speed Mixer - i = " + $"{i}");
                                    currentpageSpeedMixer = frame.Content as SubRecipe.SpeedMixer;
                                    if (i == 1)
                                    {
                                        nextSeqType = "1";
                                        nextSeqID = db.GetMax("recipe_speedmixer", "id") + 1;
                                        values = new string[] { recipeName, new_version.ToString(), "0", nextSeqType, nextSeqID.ToString() };
                                        db.SendCommand_insertRecord("recipe", "name, version, status, first_seq_type, first_seq_id", values);
                                    }
                                    else
                                    {
                                        nextSeqID = db.GetMax("recipe_speedmixer", "id") + (nextSeqType == "1" ? 2 : 1);
                                        nextSeqType = "1";
                                        values[0] = nextSeqType;
                                        values[1] = nextSeqID.ToString();

                                        if (values.Count() == 11-1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was Weight
                                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                                        }
                                        else if(values.Count() == 42-1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                                        {
                                            // The previous sequence was SpeedMixer
                                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                                        }
                                        else
                                        {
                                            MessageBox.Show("SpeedMixer: très bizarre tout ça...");
                                        }
                                    }
                                    values = currentpageSpeedMixer.GetPage();
                                    i++;
                                }
                                else
                                {
                                    MessageBox.Show("Vraiment très bizarre ct'histoire...");
                                }
                            }
                        }
                        //MessageBox.Show("Last");
                        if (values.Count() == 11 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                        {
                            // The previous sequence was Weight
                            db.SendCommand_insertRecord("recipe_weight", columnsWeight, values);

                        }
                        else if (values.Count() == 42 - 1) // because it should be something like MySettings["SubRecipes_n_Columns"] - 1
                        {
                            // The previous sequence was SpeedMixer
                            db.SendCommand_insertRecord("recipe_speedmixer", columnsSpeedMixer, values);
                        }
                        else
                        {
                            MessageBox.Show("SpeedMixer: très bizarre tout ça...");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Not good, not good at all");
                    }

                    db.Disconnect();
                    MessageBox.Show("Ouf, ça c'est fait");
                }
                else
                {
                    MessageBox.Show("Not good brotha");
                    db.ConnectAsync();
                }
            }
            else
            {
                MessageBox.Show("You should know by now that a recipe name can't be empty...");
            }

        }
         */

        public static class Util
        {
            public static T Foo<T>(object obj)
            {
                // Do actual stuff here
                return default(T);
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

        private async void ReadDB_Click(object sender, RoutedEventArgs e)
        {
            /*  VARIABLE TO USED IN ARGUMENT OF THE FOLLOWING METHOD  */string[] whereValues = new string[] { tbProgramNameRead.Text, tbVersionRead.Text };

            string[] array;
            string nextSeqType;
            string nextSeqID;
            //int seqNumber = 1;

            if (!db.IsConnected()) db.Connect();

                if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_readAllRecipe(MySettings["Table_Name"].ToString(), MySettings["Where_Columns"].Split(','), whereValues);

                array = db.ReadNext();

                if (array.Count() != 0 && db.ReadNext().Count() == 0) // Si la requête envoyer ne contient qu'une seule ligne
                {
                    tbProgramName.Text = array[1];
                    tbVersion.Text = array[2];
                    labelStatus.Text = status[int.Parse(array[3])];
                    nextSeqType = array[4];
                    nextSeqID = array[5];

                    Frame frame;

                    var currentPage = new List<Page>
                    {
                        new SubRecipe.Weight(),
                        new SubRecipe.SpeedMixer()
                    };
                    
                    string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
                    int[] dbSubRecipeNColumns = Array.ConvertAll(MySettings["SubRecipes_N_Columns"].Split(','), int.Parse);

                    do // On remplie les frames ici
                    {
                        isFrameLoaded = false;
                        New_Sequence(nextSeqType);
                        while (!isFrameLoaded) // On attend que la frame créée (New_Sequence(nextSeqType == "1");) a été chargé
                        {
                            await Task.Delay(25);
                        };
                        isFrameLoaded = false;

                        if (gridMain.Children[gridMain.Children.Count - 1].GetType().Equals(typeof(Frame))) // Si le dernier élément de la grille gridMain est une frame on continue
                        {
                            db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                                db.SendCommand_readAllRecipe(dbSubRecipeName[int.Parse(nextSeqType)], MySettings["SubRecipes_Where_Columns"].Split(','), new string[] { nextSeqID });
                                frame = gridMain.Children[gridMain.Children.Count - 1] as Frame;
                            
                            if (nextSeqType == MySettings["SubRecipeWeight_SeqType"]) {
                                    currentPage[int.Parse(nextSeqType)] = frame.Content as SubRecipe.Weight;
                                }
                                else if(nextSeqType == MySettings["SubRecipeSpeedMixer_SeqType"]) {
                                    currentPage[int.Parse(nextSeqType)] = frame.Content as SubRecipe.SpeedMixer;
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

                    MessageBox.Show("Ouf c'est fini");
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

        private void New_Sequence(string seqType)
        {
            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            frame.MouseDoubleClick += new MouseButtonEventHandler(Frame_test);
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

    }
}
