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

        private static readonly string[] tableNameSubCycles = new string[] { "cycle_weight", "cycle_speedmixer" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
        private static readonly string[] tableNameSubRecipes = new string[] { "recipe_weight", "recipe_speedmixer" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
        private static readonly string[] columnNamesSubCycles = new string[] { "product, setpoint, minimum, maximum, unit, decimal_number", "time_mix_th, pressure_unit, speed_min, speed_max, pressure_min, pressure_max" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
        public static readonly string auditTrail_BackupDesc = "Backup complet de la base de donnée réussi";
        public static readonly string auditTrail_RestoreDesc = "Restauration complète de la base de donnée réussi";
        public static readonly string auditTrail_ArchiveDesc = "Archivage de la base de donnée réussi";
        public static readonly string auditTrail_RestArchDesc = "Restauration de l'archivage de la base de donnée réussi";
        public static int count = 0;
        public static string text;
        public static DateTime NextBackupTime;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private static CycleTableInfo cycleTableInfo = new CycleTableInfo();

        static General()
        {
            NextBackupTime = Convert.ToDateTime("12:00");
            if (General.NextBackupTime.CompareTo(DateTime.Now) < 0)
            {
                General.NextBackupTime = General.NextBackupTime.AddDays(1);
            }
        }
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

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

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
                    //string recipe_status = MySettings["Status"].Split(',')[int.Parse(array[5])];

                    //cycleTableInfo.columns[cycleTableInfo.id].value = "";
                    //cycleTableInfo.columns[cycleTableInfo.nextSeqType].value = "1";
                    //cycleTableInfo.columns[cycleTableInfo.nextSeqId].value = "2";
                    cycleTableInfo.columns[cycleTableInfo.jobNumber].value = OFnumber;
                    cycleTableInfo.columns[cycleTableInfo.batchNumber].value = OFnumber;
                    cycleTableInfo.columns[cycleTableInfo.quantityValue].value = finalWeight;
                    cycleTableInfo.columns[cycleTableInfo.quantityUnit].value = "g";
                    cycleTableInfo.columns[cycleTableInfo.itemNumber].value = recipe_name;
                    cycleTableInfo.columns[cycleTableInfo.recipeName].value = recipe_name;
                    cycleTableInfo.columns[cycleTableInfo.recipeVersion].value = recipe_version;
                    cycleTableInfo.columns[cycleTableInfo.equipmentName].value = equipement_name;
                    //cycleTableInfo.columns[cycleTableInfo.dateTimeStartCycle].value = "2022-11-03 11:33:25";
                    //cycleTableInfo.columns[cycleTableInfo.dateTimeEndCycle].value = "2022-11-03 12:33:25";
                    cycleTableInfo.columns[cycleTableInfo.username].value = General.loggedUsername;
                    //cycleTableInfo.columns[cycleTableInfo.firstAlarmId].value = "12";
                    //cycleTableInfo.columns[cycleTableInfo.lastAlarmId].value = "13";
                    //cycleTableInfo.columns[cycleTableInfo.comment].value = "14";
                    cycleTableInfo.columns[cycleTableInfo.isItATest].value = isTest ? "1" : "0";
                    MyDatabase.InsertRow(cycleTableInfo);

                    //string columns = "job_number, batch_number, quantity_value, quantity_unit, item_number, recipe_name, recipe_version, equipment_name, username, is_it_a_test";
                    //string[] values = new string[] { OFnumber, OFnumber, finalWeight, "g", recipe_name, recipe_name, recipe_version, General.equipement_name, General.loggedUsername, isTest ? "1" : "0" };
                    //MyDatabase.InsertRow_done_old("cycle", columns, values);
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

                    auditTrailInfo.columns[auditTrailInfo.username].value = General.loggedUsername;
                    auditTrailInfo.columns[auditTrailInfo.eventType].value = "Evènement";
                    auditTrailInfo.columns[auditTrailInfo.description].value = "Départ cycle. Lot: " + OFnumber + ", Recette: " + recipe_name + " version " + recipe_version;
                    MyDatabase.InsertRow(auditTrailInfo);
                    //MyDatabase.InsertRow_done_old("audit_trail", "username, event_type, description", new string[] { General.loggedUsername, "Evènement", "Départ cycle. Lot: " + OFnumber + ", Recette: " + recipe_name + " version " + recipe_version });

                    string firstAlarmId;
                    if (AlarmManagement.ActiveAlarms.Count > 0) firstAlarmId = AlarmManagement.alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id.ToString();
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
        //public static void NextSequence(string[] currentPhaseParameters, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, string tablePrevious, int previousSeqType, bool isTest, string comment = "")
        public static void NextSequence(string[] currentPhaseParameters, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, int previousSeqType, bool isTest, string comment = "")
        {
            if (currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids
            {
                frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, tableNameSubCycles[previousSeqType], isTest);
                //frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, tablePrevious, isTest);
            }
            else if (currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer
            {
                frameMain.Content = new Pages.SubCycle.CycleSpeedMixer(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, tableNameSubCycles[previousSeqType], isTest);
                //frameMain.Content = new Pages.SubCycle.CycleSpeedMixer(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, tablePrevious, isTest);
            }
            else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "") // S'il n'y a pas de prochaine séquence 
            {
                EndSequence(recipeParameters: currentPhaseParameters, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: previousSeqType, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: comment);
            }
            else
            {
                MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
            }
        }
        public static void EndSequence(string[] recipeParameters, Frame frameMain, Frame frameInfoCycle, int idCycle, int previousSeqType, string previousSeqId, bool isTest, string comment = "")
        {
            MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "comment" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), comment }, idCycle.ToString());

            string nextSeqId;
            int nextRecipeSeqType;
            bool isNextRcpSeqTpOK;

            string[] valuesSubCycle = new string[0];
            int timeTh_seconds = 0;
            //TimeSpan timeTh;
            int i;


            ICycleSeqInfo cycleSeqInfo;
            string row;

            List<ICycleSeqInfo> cycleSeqTypes = new List<ICycleSeqInfo>()
            {
                new CycleWeightInfo(),
                new CycleSpeedMixerInfo()
            };

            for (int j = 0; j < cycleSeqTypes.Count(); j++)
            {
                if (cycleSeqTypes[j].seqType != j) {
                    logger.Error("Je veux pas être méchant mais tu ne sais pas coder");
                    return;
                }
            }

            if (previousSeqType < 0 || previousSeqType >= cycleSeqTypes.Count())
            {
                logger.Error("Mais non, tu déconnes ! " + previousSeqType.ToString());
                MessageBox.Show("Mais non, tu déconnes ! " + previousSeqType.ToString());
                return;
            }

            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (recipeParameters[1] != "" && recipeParameters[1] != null)
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeParameters[1]);
                recipeParameters = MyDatabase.GetOneRow(tableNameSubRecipes[int.Parse(recipeParameters[1])], whereColumns: new string[] { "id" }, whereValues: new string[] { recipeParameters[2] });

                row = "recipeParameters ";
                for (int j = 0; j < recipeParameters.Count(); j++)
                {
                    row = row + j.ToString() + "-" + recipeParameters[j] + " ";
                }
                logger.Trace(row);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= cycleSeqTypes.Count()) {
                    logger.Error("Mais non, tu déconnes ! " + nextRecipeSeqType.ToString());
                    MessageBox.Show("Mais non, tu déconnes ! " + nextRecipeSeqType.ToString());
                    return;
                }

                cycleSeqInfo = cycleSeqTypes[nextRecipeSeqType];
                cycleSeqInfo.SetRecipeParameters(recipeParameters);

                row = cycleSeqInfo.GetType().ToString() + " ";
                for (int j = 0; j < cycleSeqInfo.columns.Count(); j++)
                {
                    row = row + cycleSeqInfo.columns[j].id + ": " + cycleSeqInfo.columns[j].value + " ";
                }
                logger.Trace(row);

                // On insert les infos de recettes dans la bonne table
                MyDatabase.InsertRow(cycleSeqInfo);

                // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence
                nextSeqId = MyDatabase.GetMax(cycleSeqInfo.name, "id").ToString();
                //nextSeqId = MyDatabase.GetMax(tableNameSubCycles[nextRecipeSeqType], "id").ToString();
                MyDatabase.Update_Row(cycleSeqTypes[previousSeqType].name,
                    new string[] { "next_seq_type", "next_seq_id" },
                    new string[] { nextRecipeSeqType.ToString(), nextSeqId }, previousSeqId);

                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

                /*

                // Si la prochaine étape est un pesage on prépare les données à mettre dans la table cycle_weight (valuesSubCycle)
                if (nextRecipeSeqType == 0)
                {
                    valuesSubCycle = new string[] { recipeParameters[3], recipeParameters[8], recipeParameters[9], recipeParameters[10], recipeParameters[6], recipeParameters[7] };
                    isNextRcpSeqTpOK = true;
                }
                // Sinon si c'est un mix on prépare les données à mettre dans la table cycle_speedmixer (valuesSubCycle)
                else if (nextRecipeSeqType == 1)
                {
                    i = 0;
                    while (i != 10 && recipeParameters[12 + 3 * i] != "")
                    {
                        timeTh_seconds += int.Parse(recipeParameters[13 + 3 * i]);
                        i++;
                    }

                    valuesSubCycle = new string[] { TimeSpan.FromSeconds(timeTh_seconds).ToString(), recipeParameters[9], recipeParameters[42], recipeParameters[43], recipeParameters[44], recipeParameters[45] };
                    isNextRcpSeqTpOK = true;
                    timeTh_seconds = 0;
                }
                // Sinon il y a un problème
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Mais non, tu déconnes !");
                    isNextRcpSeqTpOK = false;
                }

                if (isNextRcpSeqTpOK) // S'il n'y a pas eu de problème
                {/*
                    // On insert les infos de recettes dans la bonne table
                    MyDatabase.InsertRow_done_old(tableNameSubCycles[nextRecipeSeqType],
                        columnNamesSubCycles[nextRecipeSeqType],
                        valuesSubCycle);

                    // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence
                    nextSeqId = MyDatabase.GetMax(tableNameSubCycles[nextRecipeSeqType], "id").ToString();
                    MyDatabase.Update_Row(tableNameSubCycles[previousSeqType],
                        new string[] { "next_seq_type", "next_seq_id" },
                        new string[] { nextRecipeSeqType.ToString(), nextSeqId }, previousSeqId);

                    // La dernière séquence devient l'ancienne
                    previousSeqType = nextRecipeSeqType;
                    previousSeqId = nextSeqId;
                }
            */
            }

            string lastAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();
            MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "last_alarm_id" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lastAlarmId }, idCycle.ToString());

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
                MyDatabase.Disconnect();
            }
        }
        public static void PrintReport(int id)
        {
            ReportGeneration report = new ReportGeneration();
            report.PdfGenerator(id.ToString());
        }
    }
}
