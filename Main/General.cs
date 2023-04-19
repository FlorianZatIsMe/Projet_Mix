using Alarm_Management;
using Database;
using Main.Pages;
using Main.Pages.SubCycle;
using Main.Properties;
using Message;
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
        public int recipeID;
        public string OFnumber;
        public string finalWeight;
        public Frame frameMain;
        public Frame frameInfoCycle;
        public bool isTest;
        public string bowlWeight;
    }

    public class NextSeqInfo
    {
        public ISeqTabInfo recipeInfo { get; }
        public object[] recipeValues { get; }
        public Frame frameMain { get; }
        public Frame frameInfoCycle { get; }
        public int idCycle { get; }
        public int previousSeqType { get; }
        public int previousSeqId { get; }
        public bool isTest { get; }
        public string comment { get; }

        public NextSeqInfo(ISeqTabInfo recipeInfo_arg, object[] recipeValues_arg, Frame frameMain_arg, Frame frameInfoCycle_arg, int idCycle_arg, int previousSeqType_arg, int previousSeqId_arg, bool isTest_arg, string comment_arg = "")
        {
            recipeInfo = recipeInfo_arg;
            recipeValues = recipeValues_arg;
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
        public static DateTime lastActTime;

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

        public static void ResetLastActTime()
        {
            lastActTime = DateTime.Now;
        }

        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");
            ResetLastActTime();
            info = info_arg;
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
                        textBox.Text = "";
                    }
                    else if (max != -1 && decimal.Parse(textBox.Text) > max)
                    {
                        textBox.Text = "";
                    }
                    else
                    {
                        result = true;
                    }
                }
                catch (Exception)
                {
                    logger.Error(Settings.Default.General_Info_FieldNotANumber);
                    textBox.Text = "";
                    result = false;
                    goto End;
                }
            }
            else if (textBox.Text.Length > parameter)
            {
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
        public static void Update_RecipeNames(ComboBox comboBox, List<string> ProgramNames, List<int> ProgramIDs, RecipeStatus recipeStatus = RecipeStatus.PRODnDRAFT)
        {
            logger.Debug("Update_RecipeNames");

            ProgramNames.Clear();
            ProgramIDs.Clear();
            comboBox.ItemsSource = null;
            comboBox.Items.Refresh();

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetLastRecipes_new(recipeStatus); });
            List<object[]> tables = (List<object[]>)t.Result;
            RecipeInfo recipeInfo = new RecipeInfo();

            if (tables.Count > 0 && tables[0].Count() != recipeInfo.Ids.Count())
            {
                logger.Error("On a un problème");
                MyMessageBox.Show("On a un problème");
                return;
            }

            for (int i = 0; i < tables.Count; i++)
            {
                ProgramNames.Add(tables[i][recipeInfo.Name].ToString());
                ProgramIDs.Add((int)tables[i][recipeInfo.Id]);
            }

            comboBox.ItemsSource = ProgramNames;
            comboBox.Text = Settings.Default.Recipe_Request_SelectRecipe;
            comboBox.Items.Refresh();
        }

        public static void StartCycle(CycleStartInfo info)
        {
            logger.Debug("StartCycle");

            int? firstSeqType;
            int? firstSeqID;
            int? nextSeqType;
            int? nextSeqID;
            RecipeInfo recipeInfo = new RecipeInfo();
            ISeqTabInfo recipeSeqInfo;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), info.recipeID); });
            object[] recipeValues = (object[])t.Result;
            //recipeInfo = (RecipeInfo)MyDatabase.GetOneRow(typeof(RecipeInfo), recipeID);

            try
            {
                if (recipeValues == null || (int)recipeValues[recipeInfo.Id] != info.recipeID)
                {
                    logger.Error(Settings.Default.Recipe_Error_RecipeNotFound);
                    MyMessageBox.Show(Settings.Default.Recipe_Error_RecipeNotFound);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
                return;
            }
            string recipe_name;
            int recipe_version;
            try
            {
                if (recipeValues[recipeInfo.NextSeqType] == null || recipeValues[recipeInfo.NextSeqType].ToString() == "")
                {
                    firstSeqType = null;
                    firstSeqID = null;
                    nextSeqType = null;
                    nextSeqID = null;
                }
                else
                {
                    firstSeqType = (int)recipeValues[recipeInfo.NextSeqType]; //1
                    firstSeqID = (int)(recipeValues[recipeInfo.NextSeqId]); //2
                    nextSeqType = (int)recipeValues[recipeInfo.NextSeqType]; //1
                    nextSeqID = (int)recipeValues[recipeInfo.NextSeqId]; //2
                }

                recipe_name = recipeValues[recipeInfo.Name].ToString(); // 3
                recipe_version = (int)recipeValues[recipeInfo.Version]; // 4
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
                return;
            }


            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleTableValues = new object[cycleTableInfo.Ids.Count()];
            cycleTableValues[cycleTableInfo.JobNumber] = info.OFnumber;
            cycleTableValues[cycleTableInfo.BatchNumber] = info.OFnumber;
            cycleTableValues[cycleTableInfo.FinalWeight] = info.finalWeight;
            cycleTableValues[cycleTableInfo.FinalWeightUnit] = Settings.Default.CycleFinalWeight_g_Unit;
            cycleTableValues[cycleTableInfo.ItemNumber] = recipe_name;
            cycleTableValues[cycleTableInfo.RecipeName] = recipe_name;
            cycleTableValues[cycleTableInfo.RecipeVersion] = recipe_version;
            cycleTableValues[cycleTableInfo.EquipmentName] = equipement_name;
            cycleTableValues[cycleTableInfo.Username] = General.loggedUsername;
            cycleTableValues[cycleTableInfo.IsItATest] = info.isTest ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
            cycleTableValues[cycleTableInfo.bowlWeight] = info.bowlWeight;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t1 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(cycleTableInfo, cycleTableValues); });
            //MyDatabase.InsertRow(cycleTableInfo);

            CurrentCycleInfo = new CycleInfo(cycleTableInfo, cycleTableValues, info.frameInfoCycle);
            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(cycleTableInfo, cycleTableInfo.Ids[cycleTableInfo.Id]); });
            int idCycle = (int)t2.Result;

            while (nextSeqID != null)
            {
                logger.Debug("GetOneRow " + Sequence.list[(int)nextSeqType].subRecipeInfo.GetType().ToString() + " " + nextSeqID);

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(Sequence.list[(int)nextSeqType].subRecipeInfo, nextSeqID); });
                object[] recipeSeqValues = (object[])t3.Result;
                recipeSeqInfo = Sequence.list[(int)nextSeqType].subRecipeInfo;
                //Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextSeqType)].subRecipeInfo.GetType(), nextSeqID); });
                //recipeSeqInfo = (ISeqTabInfo)t3.Result;

                if (recipeSeqValues != null && recipeSeqValues[recipeSeqInfo.Id].ToString() == nextSeqID.ToString())
                {
                    CurrentCycleInfo.NewInfo(recipeSeqInfo, recipeSeqValues, decimal.Parse(info.finalWeight));

                    if (recipeSeqValues[recipeSeqInfo.NextSeqType] == null || recipeSeqValues[recipeSeqInfo.NextSeqType].ToString() == "")
                    {
                        nextSeqType = null;
                        nextSeqID = null;
                    }
                    else
                    {
                        nextSeqType = (int)recipeSeqValues[recipeSeqInfo.NextSeqType];
                        nextSeqID = (int)recipeSeqValues[recipeSeqInfo.NextSeqId];
                    }

                    logger.Trace(nextSeqType + " " + nextSeqID);
                }
                else
                {
                    MyMessageBox.Show(Settings.Default.Recipe_Error_IncorrectRecipe);
                    nextSeqID = null;
                }
            }

            CurrentCycleInfo.InitializeSequenceNumber(); //'2022-09-20 11:52:10

            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
            object[] auditTrailValues = new object[auditTrailInfo.Ids.Count()];
            auditTrailValues[auditTrailInfo.Username] = loggedUsername;
            auditTrailValues[auditTrailInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
            auditTrailValues[auditTrailInfo.Description] = Settings.Default.General_AuditTrail_StartCycle1 + info.OFnumber + Settings.Default.General_AuditTrail_StartCycle2 + recipe_name + " version " + recipe_version;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t4 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, auditTrailValues); });

            int firstAlarmId;
            if (AlarmManagement.ActiveAlarms.Count > 0) firstAlarmId = AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id;
            else
            {
                t4.Wait();
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(auditTrailInfo, auditTrailInfo.Ids[auditTrailInfo.Id]); });
                firstAlarmId = ((int)t5.Result);
            }

            cycleTableInfo = new CycleTableInfo();
            cycleTableValues = new object[cycleTableInfo.Ids.Count()];
            cycleTableValues[cycleTableInfo.DateTimeStartCycle] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleTableValues[cycleTableInfo.FirstAlarmId] = firstAlarmId;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(cycleTableInfo, cycleTableValues, idCycle); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());

            SubCycleArg subCycleArg = new SubCycleArg(
                frameMain_arg: info.frameMain, 
                frameInfoCycle_arg: info.frameInfoCycle, 
                id_arg: (int)firstSeqID, 
                idCycle_arg: idCycle, 
                idPrevious_arg: idCycle, 
                tablePrevious_arg: cycleTableInfo.TabName, 
                prevSeqInfo_arg: new CycleTableInfo(), 
                isTest_arg: info.isTest);
            info.frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[(int)(firstSeqType)].subCycPgType, new object[] { subCycleArg });

            //MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
        }
        //public static void NextSequence(ISeqTabInfo recipeParam, Frame frameMain, Frame frameInfoCycle, int idCycle, int idSubCycle, int previousSeqType, ISeqTabInfo prevSeqInfo_arg, bool isTest, string comment = "")
        public static void NextSequence(NextSeqInfo nextSeqInfo, ISeqTabInfo prevSeqInfo_arg, object[] prevSeqValues_arg = null)
        {
            //NextSeqInfo(ISeqTabInfo recipeParam_arg, Frame frameMain_arg, Frame frameInfoCycle_arg, int idCycle_arg, int previousSeqType_arg, string previousSeqId_arg, bool isTest_arg, string comment_arg = "")

            logger.Debug("NextSequence");

            if (nextSeqInfo.recipeValues[nextSeqInfo.recipeInfo.NextSeqType] == null || nextSeqInfo.recipeValues[nextSeqInfo.recipeInfo.NextSeqType].ToString() == "") // S'il n'y a pas de prochaine séquence 
            {
                nextSeqInfo.frameMain.Content = new CycleWeight(nextSeqInfo);
                //LastThingToChange(recipeParam: nextSeqInfo.recipeParam, frameMain: nextSeqInfo.frameMain, frameInfoCycle: nextSeqInfo.frameInfoCycle, idCycle: nextSeqInfo.idCycle, previousSeqType: nextSeqInfo.previousSeqType, previousSeqId: nextSeqInfo.previousSeqId.ToString(), isTest: nextSeqInfo.isTest, comment: nextSeqInfo.comment);
            }
            else
            {
                SubCycleArg subCycleArg = new SubCycleArg(
                    frameMain_arg: nextSeqInfo.frameMain, 
                    frameInfoCycle_arg: nextSeqInfo.frameInfoCycle, 
                    id_arg: int.Parse(nextSeqInfo.recipeValues[nextSeqInfo.recipeInfo.NextSeqId].ToString()),
                    idCycle_arg: nextSeqInfo.idCycle,
                    idPrevious_arg: nextSeqInfo.previousSeqId,
                    tablePrevious_arg: Sequence.list[nextSeqInfo.previousSeqType].subCycleInfo.TabName /*tableNameSubCycles[previousSeqType]*/, 
                    prevSeqInfo_arg: prevSeqInfo_arg,
                    isTest_arg: nextSeqInfo.isTest,
                    prevSeqValues_arg: new object[0]);
                nextSeqInfo.frameMain.Content = Activator.CreateInstance(Pages.Sequence.list[int.Parse(nextSeqInfo.recipeValues[nextSeqInfo.recipeInfo.NextSeqType].ToString())].subCycPgType, new object[] { subCycleArg });
            }
        }
        public static void EndCycle(NextSeqInfo nextSeqInfo, decimal bowlWeight = -1, decimal finalWeight = -1)
        {
            logger.Debug("EndCycle");
            //isCycleEnded = true;

            int nextSeqId;
            int nextRecipeSeqType;

            ICycleSeqInfo cycleSeqInfo;
            object[] cycleSeqValues;
            ICycleSeqInfo prevCycleSeqInfo;
            string row;

            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleTableValues = new object[cycleTableInfo.Ids.Count()];
            AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

            if (nextSeqInfo.previousSeqType < 0 || nextSeqInfo.previousSeqType >= Sequence.list.Count())
            {
                logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                MyMessageBox.Show(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextSeqInfo.previousSeqType.ToString());
                return;
            }

            ISeqTabInfo recipeParam = nextSeqInfo.recipeInfo;

            ISeqTabInfo recipeSeqInfo = nextSeqInfo.recipeInfo;
            object[] recipeSeqValues = nextSeqInfo.recipeValues;
            int previousSeqType = nextSeqInfo.previousSeqType;
            int previousSeqId = nextSeqInfo.previousSeqId;


            // On boucle tant qu'on est pas arrivé au bout de la recette
            while (
                recipeSeqValues[recipeSeqInfo.NextSeqType] != null &&
                recipeSeqValues[recipeSeqInfo.NextSeqType].ToString() != "")
            {
                // Note pour plus tard: ce serait bien de retirer les "1" et de les remplacer par un truc comme recipeWeightInfo.nextSeqType
                nextRecipeSeqType = int.Parse(recipeSeqValues[recipeSeqInfo.NextSeqType].ToString());

                // A CORRIGER : IF RESULT IS FALSE
                //Type nextRecipeType = Sequence.list[int.Parse(recipeSeqValues[recipeSeqInfo.NextSeqType].ToString())].subRecipeInfo.GetType();
                recipeSeqInfo = Sequence.list[int.Parse(recipeSeqValues[recipeSeqInfo.NextSeqType].ToString())].subRecipeInfo;

                Task<object> t2 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(recipeSeqInfo, (int)recipeSeqValues[recipeSeqInfo.NextSeqId]); });
                recipeSeqValues = (object[])t2.Result;

                if (nextRecipeSeqType < 0 || nextRecipeSeqType >= Sequence.list.Count())
                {
                    logger.Error(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    MyMessageBox.Show(Settings.Default.Cycle_previousSeqTypeIncorrect + " " + nextRecipeSeqType.ToString());
                    return;
                }

                //cycleSeqInfo = Sequence.list[nextRecipeSeqType].subCycleInfo;
                cycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                //cycleSeqInfo.SetRecipeParameters(recipeParam, nextSeqInfo.idCycle);
                cycleSeqValues = cycleSeqInfo.GetRecipeParameters(recipeSeqValues, nextSeqInfo.idCycle);

                // On insert les infos de recettes dans la bonne table
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t3 = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(cycleSeqInfo, cycleSeqValues); });
                //MyDatabase.InsertRow(cycleSeqInfo);

                // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t4 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(cycleSeqInfo, cycleSeqInfo.Ids[cycleSeqInfo.Id]); });
                nextSeqId = ((int)t4.Result);

                //prevCycleSeqInfo = Sequence.list[previousSeqType].subCycleInfo;
                prevCycleSeqInfo = Activator.CreateInstance(Sequence.list[nextRecipeSeqType].subCycleInfo.GetType()) as ICycleSeqInfo;
                object[] prevCyclesSeqValues = new object[prevCycleSeqInfo.Ids.Count()];
                prevCyclesSeqValues[prevCycleSeqInfo.NextSeqType] = nextRecipeSeqType.ToString();
                prevCyclesSeqValues[prevCycleSeqInfo.NextSeqId] = nextSeqId;

                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t5 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(prevCycleSeqInfo, prevCyclesSeqValues, nextSeqInfo.previousSeqId); });
                //MyDatabase.Update_Row(prevCycleSeqInfo, previousSeqId);
                t5.Wait();
                // La dernière séquence devient l'ancienne
                previousSeqType = nextRecipeSeqType;
                previousSeqId = nextSeqId;

            }

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t6 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(auditTrailInfo, auditTrailInfo.Ids[auditTrailInfo.Id]); });
            int lastAlarmId = ((int)t6.Result);
            cycleTableValues[cycleTableInfo.DateTimeEndCycle] = DateTime.Now.ToString(Settings.Default.DateTime_Format_Write);
            cycleTableValues[cycleTableInfo.LastAlarmId] = lastAlarmId;
            cycleTableValues[cycleTableInfo.lastWeightTh] = finalWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            cycleTableValues[cycleTableInfo.lastWeightEff] = bowlWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
            cycleTableValues[cycleTableInfo.Comment] = nextSeqInfo.comment;

            // A CORRIGER : IF RESULT IS FALSE
            Task<object> t7 = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(cycleTableInfo, cycleTableValues, nextSeqInfo.idCycle); });
            //MyDatabase.Update_Row(cycleTableInfo, idCycle.ToString());
            t7.Wait();

            General.CurrentCycleInfo.StopSequence();
            //Task printReportTask = Task.Factory.StartNew(() => General.PrintReport(nextSeqInfo.idCycle));

            MyMessageBox.Show(Settings.Default.Cycle_Info_CycleOver);
            //printReportTask.Wait();
            General.PrintReport(nextSeqInfo.idCycle);
            //MyMessageBox.Show(Settings.Default.Cycle_Info_ReportGenerated);

            // On cache le panneau d'information
            General.CurrentCycleInfo.SetVisibility(false);

            if (nextSeqInfo.isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> t8 = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new CycleTableInfo(), nextSeqInfo.idCycle); });
                cycleTableValues = (object[])t8.Result;
                nextSeqInfo.frameMain.Content = new Recipe(RcpAction.Modify, nextSeqInfo.frameMain, nextSeqInfo.frameInfoCycle, cycleTableValues == null ? "" : cycleTableValues[cycleTableInfo.RecipeName].ToString(), info.Window);
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
            report.GenerateCycleReport(id);
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
                    keyBoardProcess.Kill();
                }
            }
            catch (Exception)
            {

            }

        }
    }
}