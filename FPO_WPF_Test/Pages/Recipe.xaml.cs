using Database;
using System;
using System.Collections.Generic;
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

        public Recipe()
        {
            nRow = 1;
            db = new MyDatabase();
            status = new string[] { "Draft", "Production", "Obsolète" };
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            New_Sequence(false);
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

        private void Button1_Click(object sender, RoutedEventArgs e)
        {

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

        private async void ReadDB_Click(object sender, RoutedEventArgs e)
        {
            string[] array;
            string nextSeqType;
            string nextSeqID;
            //int seqNumber = 1;
            int i;

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_readAllRecipe("recipe", new string[] { "name", "version" }, new string[] { "Old Fashioned", "1" });

                array = db.ReadNext();

                if (array.Count() != 0 && db.ReadNext().Count() == 0) // Si la requête envoyer ne contient qu'une seule ligne
                {
                    tbProgramName.Text = array[1];
                    tbVersion.Text = array[2];
                    labelStatus.Text = status[int.Parse(array[3])];
                    nextSeqType = array[4];
                    nextSeqID = array[5];

                    Frame frame;
                    SubRecipe.Weight currentPage_Weight;
                    SubRecipe.SpeedMixer currentPage_SpeedMixer;
                    SequenceWeight seqWeight = new SequenceWeight();
                    SequenceSpeedMixer seqSpeedMixer = new SequenceSpeedMixer();

                    var currentPage = new List<Page>
                    {
                        new SubRecipe.Weight(),
                        new SubRecipe.SpeedMixer()
                    };

                    string[] dbSubrecipeName = { "recipe_weight", "recipe_speedmixer" };
                    int[] dbSubrecipeColumnNumber = { 11, 42 };

                    do // On remplie les frames ici
                    {

                        New_Sequence(nextSeqType == "1");

                        if (gridMain.Children[gridMain.Children.Count - 1].GetType().Equals(typeof(Frame))) // Si le dernier élément de la grille gridMain est une frame on continue
                        {
                            while (!isFrameLoaded) // On attend que la frame créée (New_Sequence(nextSeqType == "1");) a été chargé
                            {
                                await Task.Delay(25);
                            };
                            isFrameLoaded = false;

                            db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

//                            if (nextSeqType == "0") // Si la prochaine séquence est Weight
//                            {
                                db.SendCommand_readAllRecipe(dbSubrecipeName[int.Parse(nextSeqType)], new string[] { "id" }, new string[] { nextSeqID });
                                //db.SendCommand_readAllRecipe("recipe_weight", new string[] { "id" }, new string[] { nextSeqID });
                                frame = gridMain.Children[gridMain.Children.Count - 1] as Frame;
                                //currentPage_Weight = frame.Content as SubRecipe.Weight;

                                if (nextSeqType == "0")
                                {
                                    currentPage[int.Parse(nextSeqType)] = frame.Content as SubRecipe.Weight;
                                }
                                else if(nextSeqType == "1")
                                {
                                    currentPage[int.Parse(nextSeqType)] = frame.Content as SubRecipe.SpeedMixer;
                                }

                                array = db.ReadNext();

                                if (array.Count() == dbSubrecipeColumnNumber[int.Parse(nextSeqType)] && db.ReadNext().Count() == 0)
                                {
                                    //currentPage_Weight.FillPage(seqWeight);

                                    if (nextSeqType == "0")
                                    {
                                        var test = currentPage[int.Parse(nextSeqType)] as SubRecipe.Weight;
                                        test.FillPage(array);
                                    }
                                    else if (nextSeqType == "1")
                                    {
                                        var test = currentPage[int.Parse(nextSeqType)] as SubRecipe.SpeedMixer;
                                        test.FillPage(array);
                                    }

                                    //var test = currentPage[0] as SubRecipe.Weight;

                                    nextSeqType = array[1];
                                    nextSeqID = array[2];
                                }
                                else
                                {
                                    MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                    nextSeqID = "";
                                }
//                            }
                            /*else if (nextSeqType == "1")
                            {
                                db.SendCommand_readAllRecipe("recipe_speedmixer", new string[] { "id" }, new string[] { nextSeqID });
                                frame = gridMain.Children[gridMain.Children.Count - 1] as Frame;
                                currentPage_SpeedMixer = frame.Content as SubRecipe.SpeedMixer;

                                array = db.ReadNext();

                                if (array.Count() == 42 && db.ReadNext().Count() == 0)
                                {
                                    seqSpeedMixer.Name = array[3];
                                    seqSpeedMixer.Acceleration = int.Parse(array[4]);
                                    seqSpeedMixer.Deceleration = int.Parse(array[5]);
                                    seqSpeedMixer.Vaccum_control = array[6] == "True";
                                    seqSpeedMixer.Is_ventgas_air = array[7] == "True";
                                    seqSpeedMixer.Monitor_type = array[8] == "True";
                                    seqSpeedMixer.Pressure_unit = array[9];
                                    seqSpeedMixer.Scurve = array[10];
                                    seqSpeedMixer.Coldtrap = array[11] == "True";

                                    i = 0;
                                    while (i != 10 && array[12 + 3 * i] != "")
                                    {
                                        seqSpeedMixer.Speed[i] = int.Parse(array[12 + 3*i]);
                                        seqSpeedMixer.Time[i] = int.Parse(array[13 + 3*i]);
                                        seqSpeedMixer.Pressure[i] = int.Parse(array[14 + 3*i]);
                                        i++;
                                    }
                                    seqSpeedMixer.Nphases = i;

                                    currentPage_SpeedMixer.FillPage(seqSpeedMixer);

                                    nextSeqType = array[1];
                                    nextSeqID = array[2];
                                }
                                else
                                {
                                    MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                    nextSeqID = "";
                                }
                            }*/
                        }
                        else
                        {
                            MessageBox.Show("Je ne comprends pas... Pourquoi je ne vois pas de frame...");
                        }
                    } while (nextSeqID != "");

                    MessageBox.Show("Ouf c'est fini");
                }
                else
                {
                    if (array.Count() == 0)
                    {
                        MessageBox.Show("La recette demandée n'existe pas");
                    }
                    else
                    {
                        MessageBox.Show("C'est quoi ça ? La version demandée existe en plusieurs exemplaires !");
                    }
                }

                db.Disconnect();
            }
            else
            {
                MessageBox.Show("Not good brotha");
                db.ConnectAsync();
            }

        }

        private void New_Sequence(bool isSpeedMixer)
        {
            gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Frame frame = new Frame();
            frame.MouseDoubleClick += new MouseButtonEventHandler(Frame_test);
            frame.ContentRendered += Frame_ContentRendered;

            if (isSpeedMixer)
            {
                frame.Content = new SubRecipe.SpeedMixer(frame, nRow.ToString());
            }
            else
            {
                frame.Content = new SubRecipe.Weight(frame, nRow.ToString());
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
        
        public class SequenceWeight
        {
            public string Name { get; set; }
            public bool Is_barcode_used { get; set; }
            public string Barcode { get; set; }
            public string Unit { get; set; }
            public int Decimal_number { get; set; }
            public float Setpoint { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }
        }

        public class SequenceSpeedMixer
        {
            public string Name { get; set; }
            public int Acceleration { get; set; }
            public int Deceleration { get; set; }
            public bool Vaccum_control { get; set; }
            public bool Is_ventgas_air { get; set; }
            public bool Monitor_type { get; set; }
            public string Pressure_unit { get; set; }
            public string Scurve { get; set; }
            public bool Coldtrap { get; set; }
            public int[] Speed { get; set; }
            public int[] Time { get; set; }
            public int[] Pressure { get; set; }
            public int Nphases { get; set; }

            public SequenceSpeedMixer()
            {
                Speed = new int[10];
                Time = new int[10];
                Pressure = new int[10];
            }
        }

        public class CurrentPageClass
        {
            Page[] CurrentPage { get; set; }
            SubRecipe.Weight currentPage_Weight;
            SubRecipe.SpeedMixer currentPage_SpeedMixer;

            public CurrentPageClass()
            {
                CurrentPage = new Page[2];
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
