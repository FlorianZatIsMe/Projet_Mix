using Database;
using Driver.RS232.Pump;
using DRIVER.RS232.Weight;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
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

namespace FPO_WPF_Test.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour PreCycle.xaml
    /// </summary>
    public partial class PreCycle : Page
    {
        private Frame outputFrame = new Frame();
        private Frame frameInfoCycle = new Frame();
        private List<string> ProgramNames = new List<string>();
        private List<string> ProgramIDs = new List<string>();
        //private General g = new General();
        private MyDatabase db = new MyDatabase();
        private List<string[]> thisCycleInfo = new List<string[]>();
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;

        public PreCycle(Frame inputFrame, Frame inputInfoCycleFrame)
        {
            outputFrame = inputFrame;
            frameInfoCycle = inputInfoCycleFrame;
            InitializeComponent();

            General.Update_RecipeNames(cbxProgramName, ProgramNames, ProgramIDs, true);
        }

        private void fxOK(object sender, RoutedEventArgs e)
        {
            string[] array;
            string[] preCycleInfo = new string[] { ProgramNames[cbxProgramName.SelectedIndex], tbOFnumber.Text, tbFinalWeight.Text };
            string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
            string firstSeqType;
            string firstSeqID;
            string nextSeqType;
            string nextSeqID;

            if (MessageBox.Show("Voulez-vous démarrer le cycle?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected()) // while loop is better
                {
                    db.SendCommand_Read("recipe", whereColumns: new string[] { "id" }, whereValues: new string[] { ProgramIDs[cbxProgramName.SelectedIndex] });

                    array = db.ReadNext();

                    if (array.Count() != 0 && db.ReadNext().Count() == 0 && array[0] == ProgramIDs[cbxProgramName.SelectedIndex])
                    {
                        thisCycleInfo.Add(preCycleInfo);

                        General.CurrentCycleInfo = new CycleInfo(new string[] { tbOFnumber.Text, array[3], array[4], tbFinalWeight.Text });
                        //CycleInfo cycleInfo = new CycleInfo(new string[] { tbOFnumber.Text, array[3], array[4], tbFinalWeight.Text });

                        db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                        firstSeqType = array[1];
                        firstSeqID = array[2];
                        nextSeqType = array[1];
                        nextSeqID = array[2];

                        while (nextSeqID != "" && nextSeqID != null)
                        {

                            db.SendCommand_readAllRecipe(dbSubRecipeName[int.Parse(nextSeqType)], MySettings["Column_id"].Split(','), new string[] { nextSeqID });

                            array = db.ReadNext();

                            if (array[0] == nextSeqID && db.ReadNext().Count() == 0)
                            {
                                if (nextSeqType == MySettings["SubRecipeWeight_SeqType"])
                                {
                                    // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi
                                    General.CurrentCycleInfo.NewInfoWeight(new string[] { array[3], Math.Round(decimal.Parse(array[9]), int.Parse(array[7])).ToString("N" + array[7]).ToString(), Math.Round(decimal.Parse(array[10]), int.Parse(array[7])).ToString("N" + array[7]).ToString() });
                                    //cycleInfo.NewInfoWeight(new string[] { array[3], Math.Round(decimal.Parse(array[9]), int.Parse(array[7])).ToString("N" + array[7]).ToString(), Math.Round(decimal.Parse(array[10]), int.Parse(array[7])).ToString("N" + array[7]).ToString() });
                                }
                                else if (nextSeqType == MySettings["SubRecipeSpeedMixer_SeqType"])
                                {
                                    General.CurrentCycleInfo.NewInfoSpeedMixer(new string[] { array[3] });
                                    //cycleInfo.NewInfoSpeedMixer(new string[] { array[3] });
                                }

                                nextSeqType = array[1];
                                nextSeqID = array[2];
                            }
                            else
                            {
                                MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                nextSeqID = "";
                            }

                            db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                        }

                        if (firstSeqType == "0") // Si la première séquence est une séquence de poids
                        {
                            outputFrame.Content = new Pages.SubCycle.CycleWeight(outputFrame, firstSeqID, thisCycleInfo);
                        }
                        else if (firstSeqType == "1") // Si la première séquence est une séquence speedmixer
                        {
                            outputFrame.Content = new Pages.SubCycle.CycleSpeedMixer(outputFrame, firstSeqID, thisCycleInfo);
                        }
                        else
                        {
                            thisCycleInfo.Clear();
                            MessageBox.Show("C'est pas normal ça...");
                        }
                        
                        if (General.CurrentCycleInfo != null)
                        {
                            frameInfoCycle.Content = General.CurrentCycleInfo;
                            frameInfoCycle.Visibility = Visibility.Visible;
                        }

                        db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                    }
                    else
                    {
                        MessageBox.Show("curieux");
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

        private void fxAnnuler(object sender, RoutedEventArgs e)
        {
            outputFrame.Content = new Status();
        }

        private void tbOFnumber_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;   

            if (e.Key == Key.Enter)
            {
                MessageBox.Show(RS232Weight.GetData());

                if (RS232Weight.IsFree())
                {
                    RS232Weight.BlockUse();
                    if (RS232Weight.IsOpen()) RS232Weight.Open();
                    //RS232Weight.SetCommand("@");
                    RS232Weight.SetCommand(textbox.Text);
                }
            }
        }

        private void tbFinalWeight_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                //MessageBox.Show(RS232Pump.GetData());
                RS232Pump.SetCommand(textbox.Text);
            }
        }
    }
}
