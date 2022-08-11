﻿using Database;
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
            int n;
            string recipeName = tbProgramName.Text;
            string[] whereColumns = new string[] { MySettings["Column_Recipe_name"] };
            int new_version;
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
            int previousType = -1;
            bool isFormatOk = true;
            //string previousStatus;

            if (recipeName != "")
            {
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected())
                {
                    new_version = db.GetMax(MySettings["Table_Name"], MySettings["Column_Recipe_version"], whereColumns, new string[] { recipeName }) + 1;
                    nextSeqID = new int[] { db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeWeight_SeqType"])], "id"), db.GetMax(MySettings["SubRecipes_Table_Name"].Split(',')[int.Parse(MySettings["SubRecipeSpeedMixer_SeqType"])], "id") };



                    /*
                     * 
                     * 
                     * 
                     *                  THIS NEEDS TO UPDATED NOW !!!
                     * 
                     * 
                     * 
                     * 
                     * 
                     * 
                     * */


                    /*
                    // On regarde la valeur du statut de la précédente version si elle existe (SPOILER: il doit être en production)
                    if (new_version > 1)
                    {
                        db.SendCommand_readAllRecipe("recipe", new string[] { "name", "version" }, new string[] { recipeName, (new_version - 1).ToString() });
                        previousStatus = db.ReadNext()[5];
                        db.Close_reader();
                    }
                    else
                    {
                        previousStatus = MySettings["Recipe_Status_Production"];
                    }*/

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
                                        values = new string[] { $"{currentType}", nextSeqID[currentType].ToString(), recipeName, new_version.ToString(), MySettings["Recipe_Status_Draft"] };

                                        allValues.Add(values);
                                    }
                                    else // Sinon on va s'occuper des tableaux recipe_weight et/ou recipe_speedmixer
                                    {
                                        nextSeqID[currentType]++; // = nextSeqID[currentType] + 1;// (previousType == currentType ? 1 : 1);
                                        values[0] = $"{currentType}";
                                        values[1] = nextSeqID[currentType].ToString();

                                        allValues.Add(values);
                                    }

                                    // Ici on lit le contenu de la frame, qu'elle représente une séquence de pesée ou de mix
                                    if (currentType == int.Parse(MySettings["SubRecipeWeight_SeqType"]))
                                    {
                                        var tempPage = currentPage[currentType] as SubRecipe.Weight;
                                        values = tempPage.GetPage();
                                        if(isFormatOk) isFormatOk = tempPage.IsFormatOk();
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
                                    }
                                    i++;
                                }
                                else
                                {
                                    MessageBox.Show("Etrange... TRES étrange...");
                                }
                            }
                        }

                        allValues.Add(values); // On enregistre le dernier set de valeurs ici
                    }
                    else
                    {
                        /*
                         * 
                         * 
                         * 
                         *                  THIS NEEDS TO UPDATED NOW !!!
                         * 
                         * 
                         * 
                         * 
                         * 
                         * 
                         * */

                        if (new_version == 0)
                        {
                            MessageBox.Show("Not good, not good at all");
                        }
                        else
                        {
                            MessageBox.Show("Not good, not good at all");
                        }
                    }

                    if (isFormatOk) // Si toutes les séquences sont correctement renseignée, on mets à jour la base de données
                    {
                        bool isRecordOk;
                        n = i;

                        isRecordOk = db.SendCommand_insertRecord(MySettings["Table_Name"], columnsRecipe, allValues[0]);

                        for (i = 1; i < n; i++)
                        {
                            if (isRecordOk) isRecordOk = db.SendCommand_insertRecord(tableNameSubRecipes[int.Parse(allValues[i - 1][0])], columnsSubRecipes[int.Parse(allValues[i - 1][0])], allValues[i]);
                            else n = i; // S'il y a une erreur, on arrête la boucle
                        }

                        // S'il y a eu une erreur, on supprime les lignes qui ont été créés.
                        if (!isRecordOk)
                        {
                            db.DeleteRow(MySettings["Table_Name"], new string[] { MySettings["Column_Recipe_name"], MySettings["Column_Recipe_version"] }, new string[] { recipeName, new_version.ToString() });

                            i = 0;
                            do
                            {
                                db.DeleteRow(tableNameSubRecipes[int.Parse(allValues[i][0])], new string[] { MySettings["Column_id"] }, new string[] { allValues[i][1] });
                                i++;
                            } while (allValues[i][0] != null && i < n-2);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Les calculs sont pas bons");
                    }

                    db.Disconnect();
                    MessageBox.Show("Ouf, ça c'est fait");
                }
                else
                {
                    MessageBox.Show("Not good brotha");
                }
            }
            else
            {
                MessageBox.Show("You should know by now that a recipe name can't be empty...");
            }

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
                db.SendCommand_readAllRecipe(MySettings["Table_Name"].ToString(), new string[] { MySettings["Column_Recipe_name"], MySettings["Column_Recipe_version"] }, whereValues);

                array = db.ReadNext();

                if (array.Count() != 0 && db.ReadNext().Count() == 0) // Si la requête envoyer ne contient qu'une seule ligne
                {
                    nextSeqType = array[1];
                    nextSeqID = array[2];
                    tbProgramName.Text = array[3];
                    tbVersion.Text = array[4];
                    labelStatus.Text = status[int.Parse(array[5])];

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

                                db.SendCommand_readAllRecipe(dbSubRecipeName[int.Parse(nextSeqType)], MySettings["Column_id"].Split(','), new string[] { nextSeqID });
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