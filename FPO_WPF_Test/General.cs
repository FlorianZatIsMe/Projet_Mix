using Alarm_Management;
using Database;
using FPO_WPF_Test.Pages;
using FPO_WPF_Test.Pages.SubCycle;
using FPO_WPF_Test.Properties;
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
        public static CycleInfo CurrentCycleInfo;
        public const string application_version = "1.0"; // see if we can manage that through VisualStudio
        public const string application_name = "MixingApplication";
        public readonly static string equipement_name = Settings.Default.General_equipement_name;
        public static string loggedUsername = WindowsIdentity.GetCurrent().Name;
        public static string currentRole = "";

        public static readonly string auditTrail_BackupDesc = Settings.Default.General_auditTrail_BackupDesc;
        public static readonly string auditTrail_RestoreDesc = Settings.Default.General_auditTrail_RestoreDesc;
        public static readonly string auditTrail_ArchiveDesc = Settings.Default.General_auditTrail_ArchiveDesc;
        public static readonly string auditTrail_RestArchDesc = Settings.Default.General_auditTrail_RestArchDesc;
        public static int count = 0;
        public static string text;
        public static DateTime NextBackupTime;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static General()
        {
            logger.Debug("Start");

            NextBackupTime = Convert.ToDateTime(Settings.Default.General_AutoBackupTime);
            if (General.NextBackupTime.CompareTo(DateTime.Now) < 0)
            {
                General.NextBackupTime = General.NextBackupTime.AddDays(1);
            }
        }
        public static bool Verify_Format(TextBox textBox, bool isNotNull, bool isNumber, int parameter, decimal min = -1, decimal max = -1)
        {
            logger.Debug("Verify_Format");

            /*
             * parameter:
             *              - si isNumber = false : le nombre de caractère max
             *              - si isNumber = true : le nombre de chiffre après la virgule
             */

            bool result = true;

            if (isNotNull && textBox.Text == "")
            {
                MessageBox.Show(Settings.Default.General_Info_EmptyField);
                return false;
            }

            if (isNumber)
            {
                try
                {
                    textBox.Text = Math.Round(decimal.Parse(textBox.Text), parameter).ToString("N" + parameter.ToString());

                    if (min != -1 && max != -1 && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
                    {
                        MessageBox.Show(Settings.Default.General_Info_FieldOutOfRange + " [" + min.ToString() + " ; " + max.ToString() + "]");

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
                            MessageBox.Show("ça n'est pas possible OK");
                            return false;
                        }
                    }
                    else if (min != -1 && decimal.Parse(textBox.Text) < min)
                    {
                        MessageBox.Show(Settings.Default.General_Info_FieldBelowMin + min.ToString());
                        textBox.Text = min.ToString();
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        MessageBox.Show(Settings.Default.General_Info_FieldAboveMax + max.ToString());
                        textBox.Text = max.ToString();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Settings.Default.General_Info_FieldNotANumber);
                    textBox.Text = "";
                    return false;
                }
            }
            else if (textBox.Text.Length > parameter)
            {
                MessageBox.Show(Settings.Default.General_Info_FieldTooLong1 + parameter.ToString() + Settings.Default.General_Info_FieldTooLong2);
                return false;
            }
            return result;
        }
        public static void Update_RecipeNames(ComboBox comboBox, List<string> ProgramNames, List<string> ProgramIDs, RecipeStatus recipeStatus = RecipeStatus.PRODnDRAFT)
        {
            logger.Debug("Update_RecipeNames");

            comboBox.ItemsSource = null;
            ProgramNames.Clear();
            ProgramIDs.Clear();

            ProgramNames.Add(Settings.Default.Recipe_Request_SelectRecipe);

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected())
            {
                MyDatabase.SendCommand_GetLastRecipes(recipeStatus);
                //string[] array;
                RecipeInfo recipeInfo;

                if (!MyDatabase.IsReaderNotAvailable())
                {
                    recipeInfo = (RecipeInfo)MyDatabase.ReadNext(typeof(RecipeInfo));

                    while (recipeInfo != null)
                    {
                        ProgramNames.Add(recipeInfo.columns[recipeInfo.recipeName].value);
                        ProgramIDs.Add(recipeInfo.columns[recipeInfo.id].value);

                        recipeInfo = (RecipeInfo)MyDatabase.ReadNext(typeof(RecipeInfo));
                    }
                }
                else
                {
                    logger.Error(DatabaseSettings.ReaderUnavailable);
                    MessageBox.Show(DatabaseSettings.ReaderUnavailable);
                }
                //MyDatabase.Disconnect();
            }
            else
            {
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);

                MessageBox.Show(DatabaseSettings.Error01);
            }

            comboBox.ItemsSource = ProgramNames;
            comboBox.SelectedIndex = 0;
            ProgramNames.RemoveAt(0);
            comboBox.Items.Refresh();
        }
        public static void StartCycle(string recipeID, string OFnumber, string finalWeight, Frame frameMain, Frame frameInfoCycle, bool isTest = true)
        {
            logger.Debug("StartCycle");

            //string[] array;
            //string[] dbSubRecipeName = MySettings["SubRecipes_Table_Name"].Split(',');
            string firstSeqType;
            string firstSeqID;
            string nextSeqType;
            string nextSeqID;
            CycleTableInfo cycleTableInfo;
            RecipeInfo recipeInfo;
            ISeqInfo recipeSeqInfo;

            if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                MessageBox.Show(DatabaseSettings.Error01);
                return;
            }

            recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), recipeID);

            if (recipeInfo == null || recipeInfo.columns[recipeInfo.id].value != recipeID)
            {
                logger.Error(Settings.Default.Recipe_Error_RecipeNotFound);
                MessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

            firstSeqType = recipeInfo.columns[recipeInfo.nextSeqType].value; //1
            firstSeqID = recipeInfo.columns[recipeInfo.nextSeqId].value; //2
            nextSeqType = recipeInfo.columns[recipeInfo.nextSeqType].value; //1
            nextSeqID = recipeInfo.columns[recipeInfo.nextSeqId].value; //2

            string recipe_name = recipeInfo.columns[recipeInfo.recipeName].value; // 3
            string recipe_version = recipeInfo.columns[recipeInfo.version].value; // 4

            cycleTableInfo = new CycleTableInfo();
            cycleTableInfo.columns[cycleTableInfo.jobNumber].value = OFnumber;
            cycleTableInfo.columns[cycleTableInfo.batchNumber].value = OFnumber;
            cycleTableInfo.columns[cycleTableInfo.quantityValue].value = finalWeight;
            cycleTableInfo.columns[cycleTableInfo.quantityUnit].value = "g";
            cycleTableInfo.columns[cycleTableInfo.itemNumber].value = recipe_name;
            cycleTableInfo.columns[cycleTableInfo.recipeName].value = recipe_name;
            cycleTableInfo.columns[cycleTableInfo.recipeVersion].value = recipe_version;
            cycleTableInfo.columns[cycleTableInfo.equipmentName].value = equipement_name;
            cycleTableInfo.columns[cycleTableInfo.username].value = General.loggedUsername;
            cycleTableInfo.columns[cycleTableInfo.isItATest].value = isTest ? "1" : "0";
            MyDatabase.InsertRow(cycleTableInfo);

            CurrentCycleInfo = new CycleInfo(cycleTableInfo, frameInfoCycle);
            int idCycle = MyDatabase.GetMax(cycleTableInfo.name, cycleTableInfo.columns[cycleTableInfo.id].id);

            while (nextSeqID != "" && nextSeqID != null)
            {
                logger.Debug("GetOneRow " + Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType().ToString() + " " + nextSeqID);
                recipeSeqInfo = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID);

                if (recipeSeqInfo.columns.Count() != 0 && recipeSeqInfo.columns[recipeSeqInfo.id].value == nextSeqID)
                {
                    CurrentCycleInfo.NewInfo(recipeSeqInfo);

                    nextSeqType = recipeSeqInfo.columns[recipeSeqInfo.nextSeqType].value;
                    nextSeqID = recipeSeqInfo.columns[recipeSeqInfo.nextSeqId].value;

                    logger.Trace(nextSeqType + " " + nextSeqID);
                }
                else
                {
                    MessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
                    nextSeqID = "";
                }

                //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
            }

            CurrentCycleInfo.InitializeSequenceNumber(); //'2022-09-20 11:52:10

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.columns[auditTrailInfo.username].value = loggedUsername;
            auditTrailInfo.columns[auditTrailInfo.eventType].value = Settings.Default.General_AuditTrailEvent_Event;
            auditTrailInfo.columns[auditTrailInfo.description].value = Settings.Default.General_AuditTrail_StartCycle1 + OFnumber + Settings.Default.General_AuditTrail_StartCycle2 + recipe_name + " version " + recipe_version;
            MyDatabase.InsertRow(auditTrailInfo);

            string firstAlarmId;
            if (AlarmManagement.ActiveAlarms.Count > 0) firstAlarmId = AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id.ToString();
            else firstAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();

            cycleTableInfo = new CycleTableInfo();
            cycleTableInfo.columns[cycleTableInfo.dateTimeStartCycle].value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.columns[cycleTableInfo.firstAlarmId].value = firstAlarmId;
            MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            SubCycleArg subCycleArg = new SubCycleArg(frameMain, frameInfoCycle, firstSeqID, idCycle, idCycle, cycleTableInfo.name, new CycleTableInfo(), isTest);
            frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(firstSeqType)].subCycPgType, new object[] { subCycleArg });

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
        }
        public static void NextSequence(ISeqInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, int previousSeqType, ISeqInfo prevSeqInfo_arg, bool isTest, string comment = "")
        {
            logger.Debug("NextSequence");

            if (recipeParam.columns[recipeParam.nextSeqType].value == null || recipeParam.columns[recipeParam.nextSeqType].value == "") // S'il n'y a pas de prochaine séquence 
            {
                EndSequence(recipeParam: recipeParam, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: previousSeqType, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: comment);
            }
            else
            {
                SubCycleArg subCycleArg = new SubCycleArg(frameMain, frameInfoCycle, recipeParam.columns[recipeParam.nextSeqId].value, idCycle, idSubCycle, Sequence.list[previousSeqType].subCycleInfo.name /*tableNameSubCycles[previousSeqType]*/, prevSeqInfo_arg, isTest);
                frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(recipeParam.columns[recipeParam.nextSeqType].value)].subCycPgType, new object[] { subCycleArg });
            }
        }
        public static void EndSequence(ISeqInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int previousSeqType, string previousSeqId, bool isTest, string comment = "")
        {
            logger.Debug("EndSequence");

            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            cycleTableInfo.columns[cycleTableInfo.dateTimeEndCycle].value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.columns[cycleTableInfo.comment].value = comment;
            MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            string nextSeqId;
            int nextRecipeSeqType;

            //string[] valuesSubCycle = new string[0];
            //int i;

            ICycleSeqInfo cycleSeqInfo;
            ICycleSeqInfo prevCycleSeqInfo;
            string row;

            if (previousSeqType < 0 || previousSeqType >= Sequence.list.Count())
            {
                logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + previousSeqType.ToString());
                MessageBox.Show(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + previousSeqType.ToString());
                return;
            }

            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (
                recipeParam.columns[recipeParam.nextSeqType].value != "" && 
                recipeParam.columns[recipeParam.nextSeqType].value != null)
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeParam.columns[recipeParam.nextSeqType].value);
                recipeParam = (ISeqInfo)MyDatabase.GetOneRow(typeof(ISeqInfo), recipeParam.columns[recipeParam.id].value);

                row = "recipeParameters ";
                for (int j = 0; j < recipeParam.columns.Count(); j++)
                {
                    row = row + j.ToString() + "-" + recipeParam.columns[j].value + " ";
                }
                logger.Trace(row);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    MessageBox.Show(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo.SetRecipeParameters(recipeParam);

                row = cycleSeqInfo.GetType().ToString() + " ";
                for (int j = 0; j < cycleSeqInfo.columns.Count(); j++)
                {
                    row = row + cycleSeqInfo.columns[j].id + ": " + cycleSeqInfo.columns[j].value + " ";
                }
                logger.Trace(row);

                // On insert les infos de recettes dans la bonne table
                MyDatabase.InsertRow(cycleSeqInfo);

                // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence
                nextSeqId = MyDatabase.GetMax(cycleSeqInfo.name, cycleSeqInfo.columns[cycleSeqInfo.id].id).ToString();

                prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo.columns[prevCycleSeqInfo.nextSeqType].value = nextRecipeSeqType.ToString();
                prevCycleSeqInfo.columns[prevCycleSeqInfo.nextSeqId].value = nextSeqId;
                MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);

                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

            }

            cycleTableInfo = new CycleTableInfo();
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            string lastAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();
            cycleTableInfo.columns[cycleTableInfo.dateTimeEndCycle].value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.columns[cycleTableInfo.lastAlarmId].value = lastAlarmId;
            MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            MessageBox.Show(Settings.Default.Cycle_Info_CycleOver);
            CurrentCycleInfo.StopSequence();
            PrintReport(idCycle);
            MessageBox.Show(Settings.Default.Cycle_Info_ReportGenerated);

            // On cache le panneau d'information
            CurrentCycleInfo.SetVisibility(false);

            if (isTest)
            {
                cycleTableInfo = (CycleTableInfo)MyDatabase.GetOneRow(typeof(CycleTableInfo), idCycle.ToString());
                frameMain.Content = new Recipe(Action.Modify, frameMain, frameInfoCycle, cycleTableInfo.columns.Count == 0 ? "" : cycleTableInfo.columns[cycleTableInfo.recipeName].value);
            }
            else 
            {
                frameMain.Content = new Status();
                MyDatabase.Disconnect();
            }
        }
        public static void PrintReport(int id)
        {
            logger.Debug("PrintReport");

            ReportGeneration report = new ReportGeneration();
            report.PdfGenerator(id.ToString());
        }
    }
}
