using Alarm_Management;
using Database;
using FPO_WPF_Test.Pages.SubCycle;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FPO_WPF_Test
{
    internal static class General
    {
        //private static MyDatabase db = new MyDatabase();
        public static CycleInfo CurrentCycleInfo;
        public const string application_version = "1.0";
        public const string application_name = "MixingApplication";
        public const string equipement_name = "Mabcxyz";
        public static string loggedUsername = WindowsIdentity.GetCurrent().Name;
        public static string currentRole = "";
        private static readonly NameValueCollection MySettings = ConfigurationManager.GetSection("Database/Recipe") as NameValueCollection;

        public static bool Verify_Format(TextBox textBox, bool isNotNull, bool isNumber, int parameter, decimal min = -1, decimal max = -1)
        {
            /*
             * parameter:
             *              - si isNumber = false : le nombre de caractère max
             *              - si isNumber = true : le nombre de chiffre après la virgule
             */

            

            bool result = true;

            if (isNotNull && textBox.Text == "")
            {
                MessageBox.Show("Format incorrect, le champ ne peut pas être vide");
                return false;
            }

            if (isNumber)
            {
                try
                {
                    textBox.Text = Math.Round(decimal.Parse(textBox.Text), parameter).ToString("N" + parameter.ToString());

                    if (min != -1 && max != -1 && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
                    {
                        MessageBox.Show("Format incorrect, valeur en dehors de la gamme [" + min.ToString() + " ; " + max.ToString() + "]");

                        if (decimal.Parse(textBox.Text) < min)
                        {
                            textBox.Text = min.ToString();
                        }
                        else if (decimal.Parse(textBox.Text) > max)
                        {
                            textBox.Text = max.ToString();
                        }
                        else
                        {
                            MessageBox.Show("Drôle de situation");
                            return false;
                        }
                    }
                    else if (min != -1 && decimal.Parse(textBox.Text) < min)
                    {
                        MessageBox.Show("Format incorrect, valeur inférieure au minimum: " + min.ToString());
                        textBox.Text = min.ToString();
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        MessageBox.Show("Format incorrect, valeur valeur supérieur au maximum: " + max.ToString());
                        textBox.Text = max.ToString();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Format incorrect, ceci n'est pas un nombre");
                    textBox.Text = "";
                    return false;
                }
            }
            else if (textBox.Text.Length > parameter)
            {
                MessageBox.Show("Format incorrect, le champ doit contenir justqu'à " + parameter.ToString() + " caractères");
                return false;
            }
            return result;
        }
        public static void Update_RecipeNames(ComboBox comboBox, List<string> ProgramNames, List<string> ProgramIDs, MyDatabase.RecipeStatus recipeStatus = MyDatabase.RecipeStatus.PRODnDRAFT)
        {
            comboBox.ItemsSource = null;
            ProgramNames.Clear();
            ProgramIDs.Clear();

            ProgramNames.Add("Veuillez sélectionner une recette");

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected())
            {
                MyDatabase.SendCommand_GetLastRecipes(recipeStatus);
                string[] array;

                if (!MyDatabase.IsReaderNotAvailable())
                {
                    array = MyDatabase.ReadNext();

                    while (array.Length > 0)
                    {
                        ProgramNames.Add(array[0]);
                        ProgramIDs.Add(array[1]);

                        array = MyDatabase.ReadNext();
                    }
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Update_RecipeNames - Sorry");
                }
                //MyDatabase.Disconnect();
            }
            else
            {
                ProgramNames.Add("###");
                ProgramNames.Add("###");
                ProgramNames.Add("###");

                MessageBox.Show("Not good brotha");
            }

            comboBox.ItemsSource = ProgramNames;
            comboBox.SelectedIndex = 0;
            ProgramNames.RemoveAt(0);
            comboBox.Items.Refresh();
        }
        public static void StartCycle(string recipeID, string OFnumber, string finalWeight, Frame frameMain, Frame frameInfoCycle, bool isTest = true)
        {
            string[] array;
            string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
            string firstSeqType;
            string firstSeqID;
            string nextSeqType;
            string nextSeqID;

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected()) // while loop is better
            {
                array = MyDatabase.GetOneRow("recipe", whereColumns: new string[] { "id" }, whereValues: new string[] { recipeID });

                if (array.Count() != 0 && array[0] == recipeID)
                {
                    General.CurrentCycleInfo = new CycleInfo(new string[] { OFnumber, array[3], array[4], finalWeight }, frameInfoCycle);

                    //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                    firstSeqType = array[1];
                    firstSeqID = array[2];
                    nextSeqType = array[1];
                    nextSeqID = array[2];

                    string recipe_name = array[3];
                    string recipe_version = array[4];
                    string recipe_status = MySettings["Status"].Split(',')[int.Parse(array[5])];

                    string columns = "job_number, batch_number, quantity_value, quantity_unit, item_number, recipe_name, recipe_version, equipment_name, username, is_it_a_test";
                    string[] values = new string[] { OFnumber, OFnumber, finalWeight, "g", recipe_name, recipe_name, recipe_version, General.equipement_name, General.loggedUsername, isTest ? "1" : "0" };
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

                    MyDatabase.InsertRow("audit_trail", "username, event_type, description", new string[] { General.loggedUsername, "Evènement", "Départ cycle. Lot: " + OFnumber + ", Recette: " + recipe_name + " version " + recipe_version });

                    string firstAlarmId;
                    if (AlarmManagement.activeAlarms.Count > 0) firstAlarmId = AlarmManagement.alarms[AlarmManagement.activeAlarms[0].Item1, AlarmManagement.activeAlarms[0].Item2].id.ToString();
                    else firstAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();

                    MyDatabase.Update_Row("cycle", new string[] { "date_time_start_cycle", "first_alarm_id" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), firstAlarmId }, idCycle.ToString());

                    if (firstSeqType == "0") // Si la première séquence est une séquence de poids
                    {
                        frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain, frameInfoCycle, firstSeqID, idCycle, idCycle, "cycle", isTest);
                    }
                    else if (firstSeqType == "1") // Si la première séquence est une séquence speedmixer
                    {
                        frameMain.Content = new Pages.SubCycle.CycleSpeedMixer(frameMain, frameInfoCycle, firstSeqID, idCycle, idCycle, "cycle", isTest);
                    }
                    else
                    {
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
            }
        }

        public static void EndSequence(string[] currentPhaseParameters, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, bool isTest, string comment="")
        {
            if (currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids
            {
                frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, "cycle_weight", isTest);
            }
            else if (currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer
            {
                MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                frameMain.Content = new Pages.SubCycle.CycleSpeedMixer(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, "cycle_weight", isTest);
            }
            else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "") // S'il n'y a pas de prochaine séquence 
            {
                string lastAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();
                MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "last_alarm_id", "comment" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lastAlarmId, comment }, idCycle.ToString());

                MessageBox.Show("C'est fini, merci d'être passé");
                General.CurrentCycleInfo.StopSequence();
                General.PrintReport(idCycle);
                MessageBox.Show("Rapport généré");

                // On cache le panneau d'information
                General.CurrentCycleInfo.SetVisibility(false);

                if (isTest)
                {
                    string[] recipeName = MyDatabase.GetOneRow("cycle", "recipe_name", new string[] { "id" }, new string[] { idCycle.ToString() });
                    frameMain.Content = new Pages.Recipe(Action.Modify, frameMain, frameInfoCycle, recipeName.Length == 0 ? "" : recipeName[0]);
                }
                else
                {
                    frameMain.Content = new Pages.Status();
                }
            }
            else
            {
                MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
            }
        }

        public static void PrintReport(int id)
        {
            ReportGeneration report = new ReportGeneration();
            report.pdfGenerator(id.ToString());
        }
    }
}
