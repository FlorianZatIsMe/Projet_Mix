using Alarm_Management;
using Database;
using Main.Pages;
using Main.Pages.SubCycle;
using Main.Pages.SubCycle;
using Main.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
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
        public MainWindow Window;
    }

    public struct CycleStartInfo
    {
        public string recipeID;
        public string OFnumber;
        public string finalWeight;
        public Frame frameMain;
        public Frame frameInfoCycle;
        public bool isTest;
        public string bowlWeight;
    }

    public class NextSeqInfo
    {
        public ISeqTabInfo recipeParam { get; }
        public Frame frameMain { get; }
        public Frame frameInfoCycle { get; }
        public int idCycle { get; }
        public int previousSeqType { get; }
        public string previousSeqId { get; }
        public bool isTest { get; }
        public string comment { get; }

        public NextSeqInfo(ISeqTabInfo recipeParam_arg, Frame frameMain_arg, Frame frameInfoCycle_arg, int idCycle_arg, int previousSeqType_arg, string previousSeqId_arg, bool isTest_arg, string comment_arg = "")
        {
            recipeParam = recipeParam_arg;
            frameMain = frameMain_arg;
            frameInfoCycle = frameInfoCycle_arg;
            idCycle = idCycle_arg;
            previousSeqType = previousSeqType_arg;
            previousSeqId = previousSeqId_arg;
            isTest = isTest_arg;
            comment = comment_arg;
        }
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
        private static Process keyBoardProcess;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static IniInfo info;

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
                    result = false;

                    if (min != -1 && max != -1 && (decimal.Parse(textBox.Text) < min || decimal.Parse(textBox.Text) > max))
                    {
                        textBox.Text = "";
                    }
                    else if (min != -1 && decimal.Parse(textBox.Text) < min)
                    {
                        //ShowMessageBox(Settings.Default.General_Info_FieldBelowMin + min.ToString());
                        textBox.Text = "";
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        //ShowMessageBox(Settings.Default.General_Info_FieldAboveMax + max.ToString());
                        textBox.Text = "";
                    }
                    else
                    {
                        result = true;
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
        //public static void StartCycle(string recipeID, string OFnumber, string finalWeight, Frame frameMain, Frame frameInfoCycle, bool isTest = true)
        public static void StartCycle(CycleStartInfo info)
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
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), info.recipeID); });
            recipeInfo = (RecipeInfo)t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), recipeID);

            if (recipeInfo == null || recipeInfo.Columns[recipeInfo.Id].Value != info.recipeID)
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
            cycleTableInfo.Columns[cycleTableInfo.JobNumber].Value = info.OFnumber;
            cycleTableInfo.Columns[cycleTableInfo.BatchNumber].Value = info.OFnumber;
            cycleTableInfo.Columns[cycleTableInfo.FinalWeight].Value = info.finalWeight;
            cycleTableInfo.Columns[cycleTableInfo.FinalWeightUnit].Value = Settings.Default.CycleFinalWeight_g_Unit;
            cycleTableInfo.Columns[cycleTableInfo.ItemNumber].Value = recipe_name;
            cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value = recipe_name;
            cycleTableInfo.Columns[cycleTableInfo.RecipeVersion].Value = recipe_version;
            cycleTableInfo.Columns[cycleTableInfo.EquipmentName].Value = equipement_name;
            cycleTableInfo.Columns[cycleTableInfo.Username].Value = General.loggedUsername;
            cycleTableInfo.Columns[cycleTableInfo.IsItATest].Value = info.isTest ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
            cycleTableInfo.Columns[cycleTableInfo.bowlWeight].Value = info.bowlWeight;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(cycleTableInfo); });
            //MyDatabase.InsertRow(cycleTableInfo);

            CurrentCycleInfo = new CycleInfo(cycleTableInfo, info.frameInfoCycle);
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
                    CurrentCycleInfo.NewInfo(recipeSeqInfo, decimal.Parse(info.finalWeight));

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
            auditTrailInfo.Columns[auditTrailInfo.Description].Value = Settings.Default.General_AuditTrail_StartCycle1 + info.OFnumber + Settings.Default.General_AuditTrail_StartCycle2 + recipe_name + " version " + recipe_version;

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

            SubCycleArg subCycleArg = new SubCycleArg(info.frameMain, info.frameInfoCycle, firstSeqID, idCycle, idCycle, cycleTableInfo.TabName, new CycleTableInfo(), info.isTest);
            info.frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(firstSeqType)].subCycPgType, new object[] { subCycleArg });

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
        }
        //public static void NextSequence(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, int previousSeqType, ISeqTabInfo prevSeqInfo_arg, bool isTest, string comment = "")
        public static void NextSequence(NextSeqInfo nextSeqInfo, ISeqTabInfo prevSeqInfo_arg)
        {
            //NextSeqInfo(ISeqTabInfo recipeParam_arg, Frame frameMain_arg, Frame frameInfoCycle_arg, int idCycle_arg, int previousSeqType_arg, string previousSeqId_arg, bool isTest_arg, string comment_arg = "")

            logger.Debug("NextSequence");

            if (nextSeqInfo.recipeParam.Columns[nextSeqInfo.recipeParam.NextSeqType].Value == null || nextSeqInfo.recipeParam.Columns[nextSeqInfo.recipeParam.NextSeqType].Value == "") // S'il n'y a pas de prochaine séquence 
            {
                nextSeqInfo.frameMain.Content = new WeightBowl(nextSeqInfo);
                //LastThingToChange(recipeParam: nextSeqInfo.recipeParam, frameMain: nextSeqInfo.frameMain, frameInfoCycle: nextSeqInfo.frameInfoCycle, idCycle: nextSeqInfo.idCycle, previousSeqType: nextSeqInfo.previousSeqType, previousSeqId: nextSeqInfo.previousSeqId.ToString(), isTest: nextSeqInfo.isTest, comment: nextSeqInfo.comment);
            }
            else
            {
                SubCycleArg subCycleArg = new SubCycleArg(
                    frameMain_arg: nextSeqInfo.frameMain, 
                    frameInfoCycle_arg: nextSeqInfo.frameInfoCycle, 
                    id_arg: nextSeqInfo.recipeParam.Columns[nextSeqInfo.recipeParam.NextSeqId].Value,
                    idCycle_arg: nextSeqInfo.idCycle,
                    idPrevious_arg: int.Parse(nextSeqInfo.previousSeqId),
                    tablePrevious_arg: Sequence.list[nextSeqInfo.previousSeqType].subCycleInfo.TabName /*tableNameSubCycles[previousSeqType]*/, 
                    prevSeqInfo_arg: prevSeqInfo_arg,
                    isTest_arg: nextSeqInfo.isTest);
                nextSeqInfo.frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(nextSeqInfo.recipeParam.Columns[nextSeqInfo.recipeParam.NextSeqType].Value)].subCycPgType, new object[] { subCycleArg });
            }
        }
        /*
        public static void LastThingToChange(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int previousSeqType, string previousSeqId, bool isTest, string comment = "")
        {
            logger.Debug("WeightFinalBowl");

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
                Type nextRecipeType = Sequence.list[int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value)].subRecipeInfo.GetType();
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(nextRecipeType, recipeParam.Columns[recipeParam.NextSeqId].Value); });
                //Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(recipeParam.GetType(), recipeParam.Columns[recipeParam.NextSeqId].Value); });
                recipeParam = (ISeqTabInfo)t2.Result;
                //recipeParam = (ISeqInfo)MyDatabase.GetOneRow(typeof(ISeqInfo), recipeParam.columns[recipeParam.id].value);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                //cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
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

                //prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqType].Value = nextRecipeSeqType.ToString();
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqId].Value = nextSeqId;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId); });
                //MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);
                t5.Wait();
                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

            }

            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            CycleTableInfo currentCycle = new CycleTableInfo();
            CycleWeightInfo currentWeigh = new CycleWeightInfo();
            decimal lastWeightTh = -1;
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(CycleTableInfo), idCycle.ToString()); });
            currentCycle = (CycleTableInfo)t.Result;

            try
            {
                lastWeightTh = decimal.Parse(currentCycle.Columns[currentCycle.bowlWeight].Value);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ShowMessageBox(ex.Message);
                lastWeightTh = -1;
            }

            ISeqTabInfo seqTabInfo = currentCycle;
            string nextId = currentCycle.Columns[currentCycle.NextSeqId].Value;
            string nextType = currentCycle.Columns[currentCycle.NextSeqType].Value;

            if (lastWeightTh != -1)
            {
                while (nextId != "" && nextId != null)
                {
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[seqTabInfo.SeqType].subCycleInfo.GetType(), nextId); });
                    seqTabInfo = (ISeqTabInfo)t.Result;
                    nextId = seqTabInfo.Columns[seqTabInfo.NextSeqId].Value;
                    nextType = seqTabInfo.Columns[seqTabInfo.NextSeqType].Value;

                    if (seqTabInfo.SeqType == currentWeigh.SeqType)
                    {
                        currentWeigh = (CycleWeightInfo)seqTabInfo;

                        if (currentWeigh.Columns[currentWeigh.IsSolvent].Value == DatabaseSettings.General_FalseValue_Read)
                        {
                            try
                            {
                                lastWeightTh += decimal.Parse(currentWeigh.Columns[currentWeigh.ActualValue].Value);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                                ShowMessageBox(ex.Message);
                                lastWeightTh = -1;
                                nextType = "";
                            }
                        }
                    }
                }
            }

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            string lastAlarmId = ((int)t6.Result).ToString();
            //string lastAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();
            cycleTableInfo.Columns[cycleTableInfo.DateTimeEndCycle].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableInfo.Columns[cycleTableInfo.LastAlarmId].Value = lastAlarmId;
            cycleTableInfo.Columns[cycleTableInfo.lastWeightTh].Value = lastWeightTh.ToString();
            cycleTableInfo.Columns[cycleTableInfo.Comment].Value = comment;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t7 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString()); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());
            t7.Wait();

            CurrentCycleInfo.StopSequence();
            Task printReportTask = Task.Factory.StartNew(() => PrintReport(idCycle));

            ShowMessageBox(Settings.Default.Cycle_Info_CycleOver);
            printReportTask.Wait();
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
        */
        //public static void EndCycle(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int previousSeqType, string previousSeqId, bool isTest, string comment = "")
        /*public static void EndCycle_old(NextSeqInfo nextSeqInfo)
        {
            logger.Debug("EndCycle");

            string nextSeqId;
            int nextRecipeSeqType;

            ICycleSeqInfo cycleSeqInfo;
            ICycleSeqInfo prevCycleSeqInfo;
            string row;

            if (nextSeqInfo.previousSeqType < 0 || nextSeqInfo.previousSeqType >= Sequence.list.Count())
            {
                logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                return;
            }

            ISeqTabInfo recipeParam = nextSeqInfo.recipeParam;
            int previousSeqType = nextSeqInfo.previousSeqType;
            string previousSeqId = nextSeqInfo.previousSeqId;

            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (
                recipeParam.Columns[recipeParam.NextSeqType].Value != "" &&
                recipeParam.Columns[recipeParam.NextSeqType].Value != null)
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value);

                // A CORRIGER : IF RESULT IS FALSE
                Type nextRecipeType = Sequence.list[int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value)].subRecipeInfo.GetType();
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(nextRecipeType, recipeParam.Columns[recipeParam.NextSeqId].Value); });
                //Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(recipeParam.GetType(), recipeParam.Columns[recipeParam.NextSeqId].Value); });
                recipeParam = (ISeqTabInfo)t2.Result;
                //recipeParam = (ISeqInfo)MyDatabase.GetOneRow(typeof(ISeqInfo), recipeParam.columns[recipeParam.id].value);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                //cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                cycleSeqInfo.SetRecipeParameters(recipeParam, nextSeqInfo.idCycle);

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

                //prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqType].Value = nextRecipeSeqType.ToString();
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqId].Value = nextSeqId;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(prevCycleSeqInfo, nextSeqInfo.previousSeqId); });
                //MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);
                t5.Wait();
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
            //cycleTableInfo.Columns[cycleTableInfo.lastWeightTh].Value = lastWeightTh.ToString();
            cycleTableInfo.Columns[cycleTableInfo.Comment].Value = nextSeqInfo.comment;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t7 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleTableInfo, nextSeqInfo.idCycle.ToString()); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());
            t7.Wait();

            CurrentCycleInfo.StopSequence();
            Task printReportTask = Task.Factory.StartNew(() => PrintReport(nextSeqInfo.idCycle));

            ShowMessageBox(Settings.Default.Cycle_Info_CycleOver);
            printReportTask.Wait();
            ShowMessageBox(Settings.Default.Cycle_Info_ReportGenerated);

            // On cache le panneau d'information
            CurrentCycleInfo.SetVisibility(false);

            if (nextSeqInfo.isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t8 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(CycleTableInfo), nextSeqInfo.idCycle.ToString()); });
                cycleTableInfo = (CycleTableInfo)t8.Result;
                //cycleTableInfo = (CycleTableInfo)MyDatabase.GetOneRow(typeof(CycleTableInfo), idCycle.ToString());
                MainWindow w = nextSeqInfo.frameMain.Parent as MainWindow;
                nextSeqInfo.frameMain.Content = new Recipe(RcpAction.Modify, nextSeqInfo.frameMain, nextSeqInfo.frameInfoCycle, cycleTableInfo.Columns.Count == 0 ? "" : cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value, window: w);
            }
            else
            {
                nextSeqInfo.frameMain.Content = new Status();
                //MyDatabase.Disconnect();
            }
        }*/

        public static void EndCycle(NextSeqInfo nextSeqInfo, decimal bowlWeight = -1, decimal finalWeight = -1)
        {
            logger.Debug("EndCycle");
            //isCycleEnded = true;

            string nextSeqId;
            int nextRecipeSeqType;

            ICycleSeqInfo cycleSeqInfo;
            ICycleSeqInfo prevCycleSeqInfo;
            string row;

            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (nextSeqInfo.previousSeqType < 0 || nextSeqInfo.previousSeqType >= Sequence.list.Count())
            {
                logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                General.ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                return;
            }

            ISeqTabInfo recipeParam = nextSeqInfo.recipeParam;
            int previousSeqType = nextSeqInfo.previousSeqType;
            string previousSeqId = nextSeqInfo.previousSeqId;


            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (
                recipeParam.Columns[recipeParam.NextSeqType].Value != "" &&
                recipeParam.Columns[recipeParam.NextSeqType].Value != null)
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value);

                // A CORRIGER : IF RESULT IS FALSE
                Type nextRecipeType = Sequence.list[int.Parse(recipeParam.Columns[recipeParam.NextSeqType].Value)].subRecipeInfo.GetType();
                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(nextRecipeType, recipeParam.Columns[recipeParam.NextSeqId].Value); });
                //Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(recipeParam.GetType(), recipeParam.Columns[recipeParam.NextSeqId].Value); });
                recipeParam = (ISeqTabInfo)t2.Result;
                //recipeParam = (ISeqInfo)MyDatabase.GetOneRow(typeof(ISeqInfo), recipeParam.columns[recipeParam.id].value);

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    General.ShowMessageBox(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                //cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                cycleSeqInfo.SetRecipeParameters(recipeParam, nextSeqInfo.idCycle);

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

                //prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqType].Value = nextRecipeSeqType.ToString();
                prevCycleSeqInfo.Columns[prevCycleSeqInfo.NextSeqId].Value = nextSeqId;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(prevCycleSeqInfo, nextSeqInfo.previousSeqId); });
                //MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);
                t5.Wait();
                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

            }

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(auditTrailInfo.TabName, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            string lastAlarmId = ((int)t6.Result).ToString();
            //string lastAlarmId = MyDatabase.GetMax(auditTrailInfo.name, auditTrailInfo.columns[auditTrailInfo.id].id).ToString();
            cycleTableInfo.Columns[cycleTableInfo.DateTimeEndCycle].Value = DateTime.Now.ToString(Settings.Default.DateTime_Format_Write);
            cycleTableInfo.Columns[cycleTableInfo.LastAlarmId].Value = lastAlarmId;
            cycleTableInfo.Columns[cycleTableInfo.lastWeightTh].Value = finalWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            cycleTableInfo.Columns[cycleTableInfo.lastWeightEff].Value = bowlWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            cycleTableInfo.Columns[cycleTableInfo.Comment].Value = nextSeqInfo.comment;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t7 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleTableInfo, nextSeqInfo.idCycle.ToString()); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());
            t7.Wait();

            General.CurrentCycleInfo.StopSequence();
            Task printReportTask = Task.Factory.StartNew(() => General.PrintReport(nextSeqInfo.idCycle));

            General.ShowMessageBox(Settings.Default.Cycle_Info_CycleOver);
            printReportTask.Wait();
            General.ShowMessageBox(Settings.Default.Cycle_Info_ReportGenerated);

            // On cache le panneau d'information
            General.CurrentCycleInfo.SetVisibility(false);

            if (nextSeqInfo.isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t8 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(CycleTableInfo), nextSeqInfo.idCycle.ToString()); });
                cycleTableInfo = (CycleTableInfo)t8.Result;
                nextSeqInfo.frameMain.Content = new Recipe(RcpAction.Modify, nextSeqInfo.frameMain, nextSeqInfo.frameInfoCycle, cycleTableInfo.Columns.Count == 0 ? "" : cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value, info.Window);
            }
            else
            {
                nextSeqInfo.frameMain.Content = new Status();
                //MyDatabase.Disconnect();
            }
            info.Window.UpdateMenuStartCycle(true);
        }

        public static void PrintReport(int id)
        {
            logger.Debug("PrintReport");

            ReportGeneration report = new ReportGeneration();
            report.GenerateCycleReport(id.ToString());
        }

        public static void ShowKeyBoard()
        {
            keyBoardProcess = Process.Start("osk.exe");
        }

        public static void HideKeyBoard()
        {
            try
            {
                if (keyBoardProcess != null)
                {
                    info.Window.Activate();
                    //keyBoardProcess.Kill();
                }
            }
            catch (Exception)
            {

            }

        }
    }
}