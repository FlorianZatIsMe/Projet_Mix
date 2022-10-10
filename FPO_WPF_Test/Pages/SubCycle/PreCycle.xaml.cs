using Alarm_Management;
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
    public partial class PreCycle : Page
    {
        private Frame frameMain = new Frame();
        private Frame frameInfoCycle = new Frame();
        private List<string> ProgramNames = new List<string>();
        private List<string> ProgramIDs = new List<string>();
        //private MyDatabase db = new MyDatabase();
        private List<string[]> thisCycleInfo = new List<string[]>();
        private readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;

        public PreCycle(Frame frameMain_arg, Frame inputInfoCycleFrame)
        {
            frameMain = frameMain_arg;
            frameInfoCycle = inputInfoCycleFrame;
            InitializeComponent();

            General.Update_RecipeNames(cbxProgramName, ProgramNames, ProgramIDs, MyDatabase.RecipeStatus.PROD);
        }
        private void fxOK(object sender, RoutedEventArgs e)
        {/*
            string[] array;
            string[] preCycleInfo = new string[] { ProgramNames[cbxProgramName.SelectedIndex], tbOFnumber.Text, tbFinalWeight.Text };
            string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
            string firstSeqType;
            string firstSeqID;
            string nextSeqType;
            string nextSeqID;*/

            if (MessageBox.Show("Voulez-vous démarrer le cycle?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                General.StartCycle(ProgramIDs[cbxProgramName.SelectedIndex], tbOFnumber.Text, tbFinalWeight.Text, frameMain, frameInfoCycle, false);

                //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
                /*
                if (MyDatabase.IsConnected()) // while loop is better
                {
                    array = MyDatabase.GetOneRow("recipe", whereColumns: new string[] { "id" }, whereValues: new string[] { ProgramIDs[cbxProgramName.SelectedIndex] });

                    if (array.Count() != 0 && array[0] == ProgramIDs[cbxProgramName.SelectedIndex])
                    {
                        thisCycleInfo.Add(preCycleInfo);

                        General.CurrentCycleInfo = new CycleInfo(new string[] { tbOFnumber.Text, array[3], array[4], tbFinalWeight.Text }, frameInfoCycle);

                        //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                        firstSeqType = array[1];
                        firstSeqID = array[2];
                        nextSeqType = array[1];
                        nextSeqID = array[2];

                        string recipe_name = array[3];
                        string recipe_version = array[4];
                        string recipe_status = MySettings["Status"].Split(',')[int.Parse(array[5])];

                        string columns = "job_number, batch_number, quantity_value, quantity_unit, item_number, recipe_name, recipe_version, equipment_name, username";
                        string[] values = new string[] { tbOFnumber.Text, tbOFnumber.Text, tbFinalWeight.Text, "g", recipe_name, recipe_name, recipe_version, General.equipement_name, General.loggedUsername };
                        MyDatabase.InsertRow("cycle", columns, values);
                        int idCycle = MyDatabase.GetMax("cycle", "id");

                        while (nextSeqID != "" && nextSeqID != null)
                        {
                            array = MyDatabase.GetOneRow(dbSubRecipeName[int.Parse(nextSeqType)], whereColumns: MySettings["Column_id"].Split(','), whereValues: new string[] { nextSeqID });

                            if (array.Count() != 0 && array[0] == nextSeqID)
                            {
                                if (nextSeqType == MySettings["SubRecipeWeight_SeqType"])
                                {
                                    // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi
                                    General.CurrentCycleInfo.NewInfoWeight(new string[] { array[3], Math.Round(decimal.Parse(array[9]), int.Parse(array[7])).ToString("N" + array[7]).ToString(), Math.Round(decimal.Parse(array[10]), int.Parse(array[7])).ToString("N" + array[7]).ToString() });
                                }
                                else if (nextSeqType == MySettings["SubRecipeSpeedMixer_SeqType"])
                                {
                                    General.CurrentCycleInfo.NewInfoSpeedMixer(new string[] { array[3] });
                                }

                                nextSeqType = array[1];
                                nextSeqID = array[2];
                            }
                            else
                            {
                                MessageBox.Show("Elle est cassée ta recette, tu me demandes une séquence qui n'existe pas è_é");
                                nextSeqID = "";
                            }

                            MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                        }

                        General.CurrentCycleInfo.InitializeSequenceNumber(); //'2022-09-20 11:52:10

                        MyDatabase.InsertRow("audit_trail", "username, event_type, description", new string[] { General.loggedUsername, "Evènement", "Départ cycle. Lot: " + tbOFnumber.Text + ", Recette: " + recipe_name + " version " + recipe_version });

                        string firstAlarmId;
                        if (AlarmManagement.activeAlarms.Count > 0) firstAlarmId = AlarmManagement.alarms[AlarmManagement.activeAlarms[0].Item1, AlarmManagement.activeAlarms[0].Item2].id.ToString();
                        else firstAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();

                        MyDatabase.Update_Row("cycle", new string[] { "date_time_start_cycle", "first_alarm_id" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), firstAlarmId }, idCycle.ToString());

                        if (firstSeqType == "0") // Si la première séquence est une séquence de poids
                        {
                            outputFrame.Content = new Pages.SubCycle.CycleWeight(outputFrame, frameInfoCycle, firstSeqID, idCycle, idCycle, "cycle", false);
                        }
                        else if (firstSeqType == "1") // Si la première séquence est une séquence speedmixer
                        {
                            outputFrame.Content = new Pages.SubCycle.CycleSpeedMixer(outputFrame, frameInfoCycle, firstSeqID, idCycle, idCycle, "cycle", false);
                        }
                        else
                        {
                            thisCycleInfo.Clear();
                            MessageBox.Show("C'est pas normal ça...");
                        }

                        MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                    }
                    else
                    {
                        MessageBox.Show("curieux");
                    }
                    //MyDatabase.Disconnect();
                }
                else
                {
                    MessageBox.Show("La base de données n'est pas connecté");
                    MyDatabase.ConnectAsync();
                }*/
            }
        }
        private void fxAnnuler(object sender, RoutedEventArgs e)
        {
            frameMain.Content = new Status();
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
