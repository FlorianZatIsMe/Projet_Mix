using Alarm_Management;
using Database;
using Main.Pages;
using Main.Pages.SubCycle;
using Main.Properties;
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
using System.Windows.Media;

namespace Main
{
    public struct IniInfo
    {
        public Window Window;
    }

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
        private static IniInfo info;

        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");
            info = info_arg;
        }

        public static MessageBoxResult ShowMessageBox(string message, string caption = "", MessageBoxButton button = MessageBoxButton.OK)
        {
            MessageBoxResult result = MessageBoxResult.None;
            if (info.Window != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    result = MessageBox.Show(owner: info.Window, messageBoxText: message, caption: caption, button: button);
                }));
            }
            else
            {
                result = MessageBox.Show(message, caption, button);
            }
            return result;
        }

        public static MessageBoxResult ShowMessageBox(string message)
        {
            MessageBoxResult result = MessageBoxResult.None;
            if (info.Window != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    result = MessageBox.Show(owner: info.Window, messageBoxText: message);
                }));
            }
            else
            {
                result = MessageBox.Show(message);
            }
            return result;
        }

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
                //ShowMessageBox(Settings.Default.General_Info_EmptyField);
                result = false;
                goto End;
            }

            if (isNumber)
            {
                try
                {
                    textBox.Text = Math.Round(decimal.Parse(textBox.Text), parameter).ToString("N" + parameter.ToString());

                    if (min != -1 && max != -1 && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
                    {
                        //ShowMessageBox(Settings.Default.General_Info_FieldOutOfRange + " [" + min.ToString() + " ; " + max.ToString() + "]");

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
                            logger.Error("ça n'est pas possible OK");
                            ShowMessageBox("ça n'est pas possible OK");
                            result = false;
                            goto End;
                        }
                    }
                    else if (min != -1 && decimal.Parse(textBox.Text) < min)
                    {
                        //ShowMessageBox(Settings.Default.General_Info_FieldBelowMin + min.ToString());
                        textBox.Text = min.ToString();
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        //ShowMessageBox(Settings.Default.General_Info_FieldAboveMax + max.ToString());
                        textBox.Text = max.ToString();
                    }
                }
                catch (Exception)
                {
                    ShowMessageBox(Settings.Default.General_Info_FieldNotANumber);
                    textBox.Text = "";
                    result = false;
                    goto End;
                }
            }
            else if (textBox.Text.Length > parameter)
            {
                //ShowMessageBox(Settings.Default.General_Info_FieldTooLong1 + parameter.ToString() + Settings.Default.General_Info_FieldTooLong2);
                result = false;
                goto End;
            }
        End:

            if (result)
            {
                textBox.Foreground = (SolidColorBrush)App.Current.Resources["TextBox.Correct.Foreground"];
                textBox.Background = (SolidColorBrush)App.Current.Resources["TextBox.Correct.Background"];
            }
            else
            {
                textBox.Foreground = (SolidColorBrush)App.Current.Resources["TextBox.Incorrect.Foreground"];
                textBox.Background = (SolidColorBrush)App.Current.Resources["TextBox.Incorrect.Background"];
            }

            return result;
        }
        public static void Update_RecipeNames(ComboBox comboBox, List<string> ProgramNames, List<string> ProgramIDs, RecipeStatus recipeStatus = RecipeStatus.PRODnDRAFT)
        {
            logger.Debug("Update_RecipeNames");

            ProgramNames.Clear();
            ProgramIDs.Clear();
            comboBox.ItemsSource = null;
            comboBox.Items.Refresh();


            //ProgramNames.Add(Settings.Default.Recipe_Request_SelectRecipe);

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            //if (MyDatabase.IsConnected())
            if(true)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetLastRecipes(recipeStatus); });
                List<RecipeInfo> tables = (List<RecipeInfo>)t.Result;
                //List<RecipeInfo> tables = MyDatabase.GetLastRecipes(recipeStatus);

                for (int i = 0; i < tables.Count; i++)
                {
                    ProgramNames.Add(tables[i].Columns[tables[i].Name].Value);
                    ProgramIDs.Add(tables[i].Columns[tables[i].Id].Value);
                }

                /*
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
                    ShowMessageBox(DatabaseSettings.ReaderUnavailable);
                }*/
                //MyDatabase.Disconnect();
            }
            else
            {
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);
                ProgramNames.Add(Settings.Default.Recipe_cbx_DefaultValue);

                ShowMessageBox(DatabaseSettings.Error_connectToDbFailed);
            }

            comboBox.ItemsSource = ProgramNames;
            comboBox.Text = Settings.Default.Recipe_Request_SelectRecipe;
            comboBox.Items.Refresh();
            //comboBox.SelectedIndex = 0;
            //ProgramNames.RemoveAt(0);
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
            ISeqTabInfo recipeSeqInfo;

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();
            /*
            if (!MyDatabase.IsConnected())
            {
                logger.Error(DatabaseSettings.Error01);
                ShowMessageBox(DatabaseSettings.Error01);
                return;
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), recipeID); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), recipeID);

            if (recipeInfo == null || recipeInfo.Columns[recipeInfo.Id].Value != recipeID)
            {
                logger.Error(Settings.Default.Recipe_Error_RecipeNotFound);
                ShowMessageBox(Settings.Default.Recipe_Error_RecipeNotFound);
                return;
            }

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

            firstSeqType = recipeInfo.Columns[recipeInfo.NextSeqType].Value; //1
            firstSeqID = recipeInfo.Columns[recipeInfo.NextSeqId].Value; //2
            nextSeqType = recipeInfo.Columns[recipeInfo.NextSeqType].Value; //1
            nextSeqID = recipeInfo.Columns[recipeInfo.NextSeqId].Value; //2

            string recipe_name = recipeInfo.Columns[recipeInfo.Name].Value; // 3
            string recipe_version = recipeInfo.Columns[recipeInfo.Version].Value; // 4

            cycleTableInfo = new CycleTableInfo();
            cycleTableInfo.Columns[cycleTableInfo.JobNumber].Value = OFnumber;
            cycleTableInfo.Columns[cycleTableInfo.BatchNumber].Value = OFnumber;
            cycleTableInfo.Columns[cycleTableInfo.FinalWeight].Value = finalWeight;
            cycleTableInfo.Columns[cycleTableInfo.FinalWeightUnit].Value = Settings.Default.CycleFinalWeight_g_Unit;
            cycleTableInfo.Columns[cycleTableInfo.ItemNumber].Value = recipe_name;
            cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value = recipe_name;
            cycleTableInfo.Columns[cycleTableInfo.RecipeVersion].Value = recipe_version;
            cycleTableInfo.Columns[cycleTableInfo.EquipmentName].Value = equipement_name;
            cycleTableInfo.Columns[cycleTableInfo.Username].Value = General.loggedUsername;
            cycleTableInfo.Columns[cycleTableInfo.IsItATest].Value = isTest ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(cycleTableInfo); });
            //MyDatabase.InsertRow(cycleTableInfo);

            CurrentCycleInfo = new CycleInfo(cycleTableInfo, frameInfoCycle);
            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(cycleTableInfo.TabName, cycleTableInfo.Columns[cycleTableInfo.Id].Id); });
            int idCycle = (int)t2.Result;
            //int idCycle = MyDatabase.GetMax(cycleTableInfo.name, cycleTableInfo.columns[cycleTableInfo.id].id);

            while (nextSeqID != "" && nextSeqID != null)
            {
                logger.Debug("GetOneRow " + Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType().ToString() + " " + nextSeqID);

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID); });
                recipeSeqInfo = (ISeqTabInfo)t3.Result;
                //recipeSeqInfo = (ISeqInfo)MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID);

                if (recipeSeqInfo.Columns.Count() != 0 && recipeSeqInfo.Columns[recipeSeqInfo.Id].Value == nextSeqID)
                {
                    CurrentCycleInfo.NewInfo(recipeSeqInfo);

                    nextSeqType = recipeSeqInfo.Columns[recipeSeqInfo.NextSeqType].Value;
                    nextSeqID = recipeSeqInfo.Columns[recipeSeqInfo.NextSeqId].Value;

                    logger.Trace(nextSeqType + " " + nextSeqID);
                }
                else
                {
                    ShowMessageBox(Settings.Default.Recipe_Error_IncorrectRecipe);
                    nextSeqID = "";
                }

                //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
            }

            CurrentCycleInfo.InitializeSequenceNumber(); //'2022-09-20 11:52:10

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            auditTrailInfo.Columns[auditTrailInfo.Username].Value = loggedUsername;
            auditTrailInfo.Columns[auditTrailInfo.EventType].Value = Settings.Default.General_AuditTrailEvent_Event;
            auditTrailInfo.Columns[auditTrailInfo.Description].Value = Settings.Default.General_AuditTrail_StartCycle1 + OFnumber + Settings.Default.General_AuditTrail_StartCycle2 + recipe_name + " version " + recipe_version;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t4 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });
            //MyDatabase.InsertRow(auditTrailInfo);

            string firstAlarmId;
            if (AlarmManagement.ActiveAlarms.Count > 0) firstAlarmId = AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id.ToString();
            else
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
                firstAlarmId = ((int)t5.Result).ToString();
                //firstAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();
            }

            cycleTableInfo = new CycleTableInfo();
            cycleTableInfo.Columns[cycleTableInfo.DateTimeStartCycle].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.Columns[cycleTableInfo.FirstAlarmId].Value = firstAlarmId;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString()); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            SubCycleArg subCycleArg = new SubCycleArg(frameMain, frameInfoCycle, firstSeqID, idCycle, idCycle, cycleTableInfo.TabName, new CycleTableInfo(), isTest);
            frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(firstSeqType)].subCycPgType, new object[] { subCycleArg });

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
        }
        public static void NextSequence(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, int previousSeqType, ISeqTabInfo prevSeqInfo_arg, bool isTest, string comment = "")
        {
            logger.Debug("NextSequence");

            if (recipeParam.Columns[recipeParam.NextSeqType].Value == null || recipeParam.Columns[recipeParam.NextSeqType].Value == "") // S'il n'y a pas de prochaine séquence 
            {
                EndSequence(recipeParam: recipeParam, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: previousSeqType, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: comment);
            }
            else
            {
                SubCycleArg subCycleArg = new SubCycleArg(frameMain, frameInfoCycle, recipeParam.Columns[recipeParam.NextSeqId].Value, idCycle, idSubCycle, Sequence.list[previousSeqType].subCycleInfo.TabName /*tableNameSubCycles[previousSeqType]*/, prevSeqInfo_arg, isTest);
                frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value)].subCycPgType, new object[] { subCycleArg });
            }
        }
        public static void EndSequence(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int previousSeqType, string previousSeqId, bool isTest, string comment = "")
        {
            logger.Debug("EndSequence");

            string nextSeqId;
            int nextRecipeSeqType;

            ICycleSeqInfo cycleSeqInfo;
            ICycleSeqInfo prevCycleSeqInfo;
            string row;

            if (previousSeqType < 0 || previousSeqType >= Sequence.list.Count())
            {
                logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + previousSeqType.ToString());
                ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + previousSeqType.ToString());
                return;
            }

            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (
                recipeParam.Columns[recipeParam.NextSeqType].Value != "" && 
                recipeParam.Columns[recipeParam.NextSeqType].Value != null)
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value);

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(ISeqTabInfo), recipeParam.Columns[recipeParam.Id].Value); });
                recipeParam = (ISeqTabInfo)t2.Result;
                //recipeParam = (ISeqInfo)MyDatabase.GetOneRow(typeof(ISeqInfo), recipeParam.columns[recipeParam.id].value);

                row = "recipeParameters ";
                for (int j = 0; j < recipeParam.Columns.Count(); j++)
                {
                    row = row + j.ToString() + "-" + recipeParam.Columns[j].Value + " ";
                }
                logger.Trace(row);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo.SetRecipeParameters(recipeParam, idCycle);

                row = cycleSeqInfo.GetType().ToString() + " ";
                for (int j = 0; j < cycleSeqInfo.Columns.Count(); j++)
                {
                    row = row + cycleSeqInfo.Columns[j].Id + ": " + cycleSeqInfo.Columns[j].Value + " ";
                }
                logger.Trace(row);

                // On insert les infos de recettes dans la bonne table
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(cycleSeqInfo); });
                //MyDatabase.InsertRow(cycleSeqInfo);

                // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t4 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(cycleSeqInfo.TabName, cycleSeqInfo.Columns[cycleSeqInfo.Id].Id); });
                nextSeqId = ((int)t4.Result).ToString();
                //nextSeqId = MyDatabase.GetMax(cycleSeqInfo.name, cycleSeqInfo.columns[cycleSeqInfo.id].id).ToString();

                prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqType].Value = nextRecipeSeqType.ToString();
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqId].Value = nextSeqId;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId); });
                //MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);

                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

            }

            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            string lastAlarmId = ((int)t6.Result).ToString();
            //string lastAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();
            cycleTableInfo.Columns[cycleTableInfo.DateTimeEndCycle].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.Columns[cycleTableInfo.LastAlarmId].Value = lastAlarmId;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t7 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString()); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            CurrentCycleInfo.StopSequence();
            Task t = Task.Factory.StartNew(() => PrintReport(idCycle));

            ShowMessageBox(Settings.Default.Cycle_Info_CycleOver);
            t.Wait();
            ShowMessageBox(Settings.Default.Cycle_Info_ReportGenerated);

            // On cache le panneau d'information
            CurrentCycleInfo.SetVisibility(false);

            if (isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t8 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(CycleTableInfo), idCycle.ToString()); });
                cycleTableInfo = (CycleTableInfo)t8.Result;
                //cycleTableInfo = (CycleTableInfo)MyDatabase.GetOneRow(typeof(CycleTableInfo), idCycle.ToString());
                frameMain.Content = new Recipe(RcpAction.Modify, frameMain, frameInfoCycle, cycleTableInfo.Columns.Count == 0 ? "" : cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value);
            }
            else 
            {
                frameMain.Content = new Status();
                //MyDatabase.Disconnect();
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
