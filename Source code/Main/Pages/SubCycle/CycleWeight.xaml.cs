using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using System.Configuration;

using Database;
using Driver_Ethernet_Balance;
using Message;
using Main.Properties;

namespace Main.Pages.SubCycle
{
    public enum CurrentPhase
    {
        BowlWeight,
        Cycle,
        FinalWeight,
        DailyTest,
        None
    }

    /// <summary>
    /// Logique d'interaction pour CycleWeight.xaml
    /// </summary>
    public partial class CycleWeight : UserControl, ISubCycle
    {
        private CurrentPhase currentPhase = CurrentPhase.None;
        private static bool flag = false;

        private bool isBalanceFree = false;
        private bool isManual = false;

        //private bool isPageActive = false;

        private CycleStartInfo info;

        private NextSeqInfo nextSeqInfo;
        private decimal currentSetpoint = -1;
        //private readonly decimal currentRatio;
        private readonly string message1;

        private readonly ContentControl contentControlMain;

        //private bool isFinalWeight = false;
        //private bool isBowlWeight = false;
        private decimal finalWeight = 0;

        private Timer getWeightTimer;
        //private Task getWeightTask;
        private bool isgetWeightTaskActive = false;
        private Weight weight;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string tareOnGoing = "Tare en cours...";
        private string setpointText1 = "Masse cible: ";
        private string setpointText2 = "g";
        private string message1EmptyBowl = "Veuillez placer le contenant vide sur la balance puis appuyer sur le bouton";
        private string message1FullBowl = "Veuillez placer le contenant sur la balance puis appuyer sur le bouton";
        private string message1Sampling = "Veuillez placer le poids étalon sur la balance puis appuyer sur le bouton";
        private string message1Cycle1 = "Veuillez peser le produit ";
        private string message1Cycle2 = " sur la balance puis appuyer sur le bouton";
        //private int timerInterval;

        // Sampling variables
        //private bool isDailyTest = false;
        private decimal[] refWeights;
        private decimal[] measuredMasses;
        private string[] refIDs;
        private int sampleNumber;
        private bool isDailyTestPass = true;

        // Cycle variables
        //private bool isCycle = false;
        private bool isCycleEnded = false;
        private readonly SubCycleArg subCycle;
        private readonly int previousSeqId;
        private readonly RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
        private readonly object[] recipeWeightValues;
        private bool isScanningStep;
        //private bool isWeightCorrect;
        private readonly CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
        private readonly object[] cycleWeightValues = new object[0];

        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        // Constructor to measure the mass of the empty bowl at the start of a cycle
        public CycleWeight(CurrentPhase currentPhase, CycleStartInfo info_arg)
        {
            if (currentPhase != CurrentPhase.BowlWeight)
            {
                logger.Error("Pov naze");
                MyMessageBox.Show("Pov naze");
                return;
            }
            this.currentPhase = currentPhase;

            //isBowlWeight = true;
            info = info_arg;
            contentControlMain = info.contentControlMain;
            message1 = message1EmptyBowl;

            InitializeComponent();
            Initialize();
        }
        public CycleWeight(SubCycleArg subCycleArg)
        {
            this.currentPhase = CurrentPhase.Cycle;

            //isCycle = true;
            Task<object> t;

            subCycle = subCycleArg;
            contentControlMain = subCycle.contentControlMain;
            
            //isSequenceOver = false;
            //isWeightCorrect = false;
            //wasBalanceFreeOnce = false;
            General.CurrentCycleInfo.UpdateSequenceNumber();

            //t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeWeightInfo), subCycle.id.ToString()); });
            //recipeWeightInfo = (RecipeWeightInfo)t.Result;

            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeWeightInfo(), subCycle.id); });
            recipeWeightValues = (object[])t.Result;

            if (recipeWeightValues == null) // Si la commande a renvoyée une ligne
            {
                logger.Error(Settings.Default.CycleWeight_Error_NoRecipe);
                MyMessageBox.Show(Settings.Default.CycleWeight_Error_NoRecipe);
                return; // ou exit carrément
            }

            cycleWeightValues = cycleWeightInfo.GetRecipeParameters(recipeWeightValues, subCycle.idCycle);

            message1 = message1Cycle1 + recipeWeightValues[recipeWeightInfo.Name].ToString() + message1Cycle2;
            currentSetpoint = decimal.Parse(cycleWeightValues[cycleWeightInfo.Setpoint].ToString());
            setpointText2 = "g [ " + decimal.Parse(cycleWeightValues[cycleWeightInfo.Min].ToString()).ToString("N" + recipeWeightValues[recipeWeightInfo.DecimalNumber].ToString()) + "; " + decimal.Parse(cycleWeightValues[cycleWeightInfo.Max].ToString()).ToString("N" + recipeWeightValues[recipeWeightInfo.DecimalNumber].ToString()) + " ]";

            InitializeComponent();

            logger.Error(cycleWeightInfo.IsSolvent.ToString() + " - " + cycleWeightInfo.Ids[cycleWeightInfo.IsSolvent] + " - " + cycleWeightValues[cycleWeightInfo.IsSolvent].ToString());
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(cycleWeightInfo, cycleWeightValues); });
            //MyDatabase.InsertRow(cycleWInfo);
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(cycleWeightInfo, cycleWeightInfo.Ids[cycleWeightInfo.Id]); });
            previousSeqId = (int)t.Result;

            object[] prevSeqValues = new object[subCycle.prevSeqInfo.Ids.Count()];
            prevSeqValues[subCycle.prevSeqInfo.NextSeqType] = cycleWeightInfo.SeqType.ToString();
            prevSeqValues[subCycle.prevSeqInfo.NextSeqId] = previousSeqId.ToString();

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(subCycle.prevSeqInfo, prevSeqValues, subCycle.idPrevious); });
            //MyDatabase.Update_Row(subCycle.prevSeqInfo, subCycle.idPrevious.ToString());

            nextSeqInfo = new NextSeqInfo(
                recipeInfo_arg: recipeWeightInfo, // done
                recipeValues_arg: recipeWeightValues,
                frameMain_arg: null,
                frameInfoCycle_arg: null,
                contentControlMain_arg: subCycle.contentControlMain,
                contentControlInfoCycle_arg: subCycle.contentControlInfoCycle,
                idCycle_arg: subCycle.idCycle,
                previousSeqType_arg: recipeWeightInfo.SeqType, // done
                previousSeqId_arg: previousSeqId,
                isTest_arg: subCycle.isTest);
            Initialize();
        }

        // Constructor to measure the mass of the bowl at the end of a cycle
        public CycleWeight(CurrentPhase currentPhase, NextSeqInfo nextSeqInfo_arg)
        {
            if (currentPhase != CurrentPhase.FinalWeight)
            {
                logger.Error("Pov naze");
                MyMessageBox.Show("Pov naze");
                return;
            }
            this.currentPhase = currentPhase;

            //isFinalWeight = true;
            nextSeqInfo = nextSeqInfo_arg;
            contentControlMain = nextSeqInfo.contentControlMain;
            //currentRatio = Settings.Default.LastWeightRatio;
            message1 = message1FullBowl;

            //Calculation the theoritical total weight
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleValues;
            //CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            //object[] cycleWeightValues;
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new CycleTableInfo(), nextSeqInfo.idCycle); });
            cycleValues = (object[])t.Result;

            try
            {
                currentSetpoint = decimal.Parse(cycleValues[cycleTableInfo.bowlWeight].ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MyMessageBox.Show(ex.Message);
                currentSetpoint = -1;
            }

            ISeqTabInfo seqTabInfo = cycleTableInfo;
            object[] seqTabValues;
            int? nextId;
            if (cycleValues[cycleTableInfo.NextSeqId] == null) nextId = null;
            else nextId = (int)cycleValues[cycleTableInfo.NextSeqId];

            int? nextType;
            if (cycleValues[cycleTableInfo.NextSeqType] == null) nextType = null;
            else nextType = (int)cycleValues[cycleTableInfo.NextSeqType];

            if (currentSetpoint != -1)
            {
                while (nextId != null)
                {
                    seqTabInfo = Sequence.list[(int)nextType].subCycleInfo;
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(seqTabInfo, nextId); });
                    seqTabValues = (object[])t.Result;

                    if (seqTabValues[seqTabInfo.NextSeqId] == null || seqTabValues[seqTabInfo.NextSeqId].ToString() == "") nextId = null;
                    else nextId = int.Parse(seqTabValues[seqTabInfo.NextSeqId].ToString());

                    if (seqTabValues[seqTabInfo.NextSeqType] == null || seqTabValues[seqTabInfo.NextSeqType].ToString() == "") nextType = null;
                    else nextType = int.Parse(seqTabValues[seqTabInfo.NextSeqType].ToString());

                    if (seqTabInfo.SeqType == cycleWeightInfo.SeqType)
                    {
                        cycleWeightValues = seqTabValues;

                        if (cycleWeightValues[cycleWeightInfo.IsSolvent].ToString() == DatabaseSettings.General_FalseValue_Read)
                        {
                            try
                            {
                                currentSetpoint += decimal.Parse(cycleWeightValues[cycleWeightInfo.ActualValue].ToString());
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                                MyMessageBox.Show(ex.Message);
                                currentSetpoint = -1;
                                nextType = null;
                            }
                        }
                    }
                }
            }

            finalWeight = currentSetpoint;

            InitializeComponent();
            labelMessage.MaxWidth = 500;
            //labelMessage.Text = tareOnGoing;
            Initialize();
        }

        // Constructor to perform the daily sampling
        public CycleWeight(CurrentPhase currentPhase, ContentControl contentControlMain_arg)
        {
            if (currentPhase != CurrentPhase.DailyTest)
            {
                logger.Error("Pov naze");
                MyMessageBox.Show("Pov naze");
                return;
            }
            this.currentPhase = currentPhase;

            contentControlMain = contentControlMain_arg;
            //isDailyTest = true;
            sampleNumber = 0;
            List<decimal> refWeightsList = new List<decimal>();
            List<string> refIDsList = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    if (config.AppSettings.Settings["DailyTest_Weight" + i.ToString()].Value != "")
                    {
                        refWeightsList.Add(decimal.Parse(config.AppSettings.Settings["DailyTest_Weight" + i.ToString()].Value));
                        refIDsList.Add(config.AppSettings.Settings["DailyTest_WeightID" + i.ToString()].Value);
                    }
                    else
                    {
                        i = 4;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    i = 4;
                    break;
                }
            }
            /*
            refWeightsList.Add(Settings.Default.DailyTest_Weight1);
            if (Settings.Default.DailyTest_Weight2 != 0) refWeightsList.Add(Settings.Default.DailyTest_Weight2);
            if (Settings.Default.DailyTest_Weight3 != 0) refWeightsList.Add(Settings.Default.DailyTest_Weight3);
            if (Settings.Default.DailyTest_Weight4 != 0) refWeightsList.Add(Settings.Default.DailyTest_Weight4);*/
            refWeights = refWeightsList.ToArray();
            refIDs = refIDsList.ToArray();

            currentSetpoint = refWeights[sampleNumber];
            //currentRatio = Settings.Default.DailyTestRatio;

            measuredMasses = new decimal[refWeights.Length];
            message1 = message1Sampling;
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            //*
            getWeightTimer = new Timer
            {
                AutoReset = true,
                Interval = Settings.Default.CycleWeight_getWeightTimer_Interval
            };
            getWeightTimer.Elapsed += GetWeightTimer_Elapsed;
            //*/

            labelMessage.Text = tareOnGoing;
            InitializeCycle();
        }
        private async void InitializeCycle()
        {
            bool exeTare = true;
            // If the balance is free, we block it

            int count = 0;

            while (!Balance.IsFree() && count < 10)
            {
                await Task.Delay(500);
                count++;
            }

            if (Balance.IsFree())
            {
                Balance.BlockUse();
                isBalanceFree = true;
            }
            //MessageBox.Show("on commence " + Balance.IsFree().ToString() + ", isBalanceFree " + isBalanceFree.ToString());
            // While the balance is not free
            while (!isBalanceFree)
            {
                // We ask the user if he wants to try to block the balance again
                if (MyMessageBox.Show("La balance n'est pas disponible, voulez-vous attendre ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // If he wants to wait, we wait
                    if (Balance.IsFree())
                    {
                        Balance.BlockUse();
                        isBalanceFree = true;
                    }
                }
                else
                {
                    // Else we're out
                    while(!this.IsLoaded) await Task.Delay(100);
                    contentControlMain.Content = new Pages.Status();
                    //MessageBox.Show("alors");
                    //Stop();
                    return;
                }
                await Task.Delay(1000);
            }

            // Connect the balance
            //Balance.Connect();
            // While the balance is not connected or user give up
            while (!Balance.IsConnected() && !isManual)
            {
                // We ask the user if he wants to try to connect to the balance again
                if (MyMessageBox.Show("La balance n'est pas connecté, voulez-vous attendre ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    int n = 0;
                    while (!Balance.IsConnected() && n < 10)
                    {
                        await Task.Delay(1000);
                        n++;
                    }

                    // If he wants to wait, we connect to the balance
                    //Balance.Connect();
                }
                else
                {
                    // If he gives up then
                    // If manual weighting is allowed we set the manual weighting
                    if (Settings.Default.CycleWeight_isManualAllowed)
                    {
                        isManual = true;
                    }
                    else
                    {
                        // Else stop of cycle
                        Stop();
                        return;
                    }
                }
            }

            // On vérifie le scan du produit, si on est en train de peser un produit et que la recette demande de contrôler le code barre
            if (currentPhase == CurrentPhase.Cycle && recipeWeightValues[recipeWeightInfo.IsBarcodeUsed].ToString() == DatabaseSettings.General_TrueValue_Read)
            {
                labelMessage.Text = Settings.Default.CycleWeight_Request_ScanProduct + " " + recipeWeightValues[recipeWeightInfo.Name].ToString();
                tbScan.Visibility = Visibility.Visible;
                isScanningStep = true;

                while (isScanningStep)
                {
                    await Task.Delay(500);
                }

                labelMessage.Text = tareOnGoing;
            }
            //isManual = true;
            // if we are in manual mode, perform a manual tare
            string tareMessage1 = "";
            string tareMessage2 = "";

            if (currentPhase == CurrentPhase.BowlWeight || currentPhase == CurrentPhase.FinalWeight || currentPhase == CurrentPhase.DailyTest)
            {
                tareMessage1 = "Veuillez retirer tout object sur la balance";
            }
            else if(currentPhase == CurrentPhase.Cycle)
            {
                tareMessage1 = "Veuillez poser le contenant sur la balance";
            }
            else
            {
                logger.Error("Je ne sais pas ce que tu as fait mais c'est bizarre");
                MyMessageBox.Show("Je ne sais pas ce que tu as fait mais c'est bizarre");
                contentControlMain.Content = new Pages.Status();
                return;
            }

            if (isManual)
            {
                tareMessage2 = " puis faites une tare de la balance manuellement. Cliquer sur le ok une fois terminé";
            }
            else
            {
                tareMessage2 = ". Cliquer sur le bouton une fois fait";
            }

            MyMessageBox.Show(tareMessage1 + tareMessage2);

            labelWeight.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
            tbWeight.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;

            while (exeTare)
            {
                int tare = isManual ? 0 : Balance.TareBalance();
                if (tare == 0)
                {
                    labelMessage.Text = message1;
                    btNext.Visibility = Visibility.Visible;

                    if (currentPhase == CurrentPhase.FinalWeight || currentPhase == CurrentPhase.DailyTest || currentPhase == CurrentPhase.Cycle)
                    {
                        labelSetpoint.Visibility = Visibility.Visible;
                        string setpoint = "#";
                        if (currentSetpoint != -1)
                        {
                            setpoint = currentSetpoint.ToString("N" + Settings.Default.General_Weight_NbDecimal);
                        }
                        labelSetpoint.Text = setpointText1 + setpoint + setpointText2 + 
                            (currentPhase == CurrentPhase.DailyTest ? " (" + config.AppSettings.Settings["DailyTest_WeightID0"].Value + ")" : "");
                    }

                    // démarrage du timer qui lit en continue la valeur du poids et l'affiche
                    StartTimer();


                    isgetWeightTaskActive = true;
                    //getWeightTask = Task.Factory.StartNew(() => GetWeight()); ;
                    exeTare = false;
                }
                else
                {
                    if (MyMessageBox.Show("Tare de la balance échouée, voulez-vous réessayer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        exeTare = false;
                        Stop();
                        return;
                    }
                    logger.Error("Tare de la balance échouée, tare: " + Balance.TareBalance().ToString());
                }
            }
            btNext.Visibility = Visibility.Visible;
        }
        private void GetWeightTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            isManual = !Balance.IsConnected();
            //isManual = true;

            this.Dispatcher.Invoke(() => {
                labelWeight.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
                tbWeight.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;
            });

            if (!isManual)
            {
                weight = Balance.GetOneWeight();

                this.Dispatcher.Invoke(() => {
                    if (weight == null)
                    {
                        labelWeight.Text = "#";
                        //logger.Error(labelWeight.Text + " #");
                    }
                    else
                    {
                        labelWeight.Text = weight.value + "g";
                        //logger.Error(labelWeight.Text + weight.value);

                        if (currentPhase == CurrentPhase.FinalWeight || currentPhase == CurrentPhase.DailyTest || currentPhase == CurrentPhase.Cycle)
                        {
                            labelWeight.Foreground = IsWeightCorrect(weight.value, currentSetpoint) ? (SolidColorBrush)Application.Current.FindResource("MyFont1_Foreground") : (SolidColorBrush)Application.Current.FindResource("FontColor_Incorrect");
                            //labelWeight.Foreground = (Math.Abs(weight.value - currentSetpoint) < currentSetpoint * currentRatio) ? Brushes.AliceBlue : Brushes.Red;
                        }
                        else if (currentPhase == CurrentPhase.Cycle)
                        {
                            labelWeight.Foreground = Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())) <= decimal.Parse(recipeWeightValues[recipeWeightInfo.Criteria].ToString()) ? Brushes.AliceBlue : Brushes.Red;
                            logger.Error((Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())) <= decimal.Parse(recipeWeightValues[recipeWeightInfo.Criteria].ToString())).ToString() + weight.value.ToString() + recipeWeightValues[recipeWeightInfo.Setpoint].ToString() + Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())).ToString() + recipeWeightValues[recipeWeightInfo.Criteria].ToString());
                        }
                    }
                });
            }
            getWeightTimer.Enabled = isgetWeightTaskActive;
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.IsEnabled = false;

            bool keepWaiting = true;
            int waitingCounter = 0;

            decimal validWeight;

            if (!isManual)
            {
                // on attent que le poids se stabilise ou un timeout, on affiche le message "stabilisation en cours"
                while (keepWaiting && (weight == null || !weight.isStable))
                {
                    await Task.Delay(500);
                    waitingCounter++;

                    // Si le poids n'est pas stable de puis 5s alors...
                    if (waitingCounter > 10) // 5s
                    {
                        // On demande à l'opérateur s'il veut continuer à attendre
                        if (MyMessageBox.Show("Le poids n'est toujours pas stable, voulez-vous continuer à attendre ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            // S'il veut continuer à attendre, on continue à attendre
                            waitingCounter = 0;
                        }
                        else
                        {
                            // Sinon on arrête d'attendre
                            keepWaiting = false;
                            Stop();
                            goto End;
                        }
                    }
                }

                StopTimer();

                if (!keepWaiting) goto End;

                // Le poids est stable, du coup on le stock dans info et on lance la séquence
                validWeight = weight.value;
            }
            else
            {
                if (General.Verify_Format(textBox: tbWeight, isNotNull: true, isNumber: true, parameter: Settings.Default.General_Weight_NbDecimal, min: 0))
                {
                    validWeight = decimal.Parse(tbWeight.Text);
                }
                else
                {
                    MyMessageBox.Show("Format de la masse incorrect");
                    goto End;
                }
            }

            if (currentPhase == CurrentPhase.FinalWeight)
            {
                if (!IsFinalWeightCorrect(validWeight, currentSetpoint))  //if (!IsWeightCorrect(validWeight))
                {
                    if (MyMessageBox.Show("La masse du produit est incorrecte, voulez-vous continuer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        goto End;
                    }
                }

                isgetWeightTaskActive = false;
                getWeightTimer.Stop();
                getWeightTimer.Dispose();
                //if (!Balance.IsFree()) Balance.FreeUse();
                EndCycle(validWeight);
            }
            else if (currentPhase == CurrentPhase.DailyTest)
            {
                // On contrôle la mesure
                // Si c'est bon, on continue
                // Sinon, message d'erreur qui propose de recommencer ou pas
                if (!IsSampWeightCorrect(validWeight, currentSetpoint)) //if (!IsWeightCorrect(validWeight))
                {
                    if (MyMessageBox.Show("La masse du poids étalon est incorrecte, voulez-vous continuer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        goto End;
                    }
                    else
                    {
                        // Si on ne recommence pas, le statut est fail mais on continue
                        isDailyTestPass = false;
                    }
                }

                if (sampleNumber < refWeights.Length)
                {
                    // On stocke la measure du poids
                    measuredMasses[sampleNumber] = validWeight;
                    sampleNumber++;
                }

                if (sampleNumber < refWeights.Length)
                {
                    // On met à jour la cible
                    currentSetpoint = refWeights[sampleNumber];
                    labelSetpoint.Text = setpointText1 + currentSetpoint.ToString("N" + Settings.Default.General_Weight_NbDecimal) +
                        (currentPhase == CurrentPhase.DailyTest ? " (" + config.AppSettings.Settings["DailyTest_WeightID" + sampleNumber.ToString()].Value + ")" : "");

                    tbWeight.Text = "";
                    tbWeight.Background = Brushes.White;
                    tbWeight.Foreground = Brushes.Black;

                    // On relance le timer
                    StartTimer();
                }
                else
                {
                    // Fin de l'étalonnage
                    isgetWeightTaskActive = false;
                    getWeightTimer.Stop();
                    getWeightTimer.Dispose();
                    //if (!Balance.IsFree()) Balance.FreeUse();

                    // On met dans la base de données toutes les mesures (cible et valeur) avec le status
                    DailyTestInfo dailyTestInfo = new DailyTestInfo();
                    object[] dailyTestValues = new object[dailyTestInfo.Ids.Count()];
                    dailyTestValues[dailyTestInfo.Username] = General.loggedUsername;
                    dailyTestValues[dailyTestInfo.EquipmentName] = General.equipement_name;
                    for (int i = 0; i < refWeights.Length; i++)
                    {
                        dailyTestValues[dailyTestInfo.Setpoint1 + i] = refWeights[i].ToString("N" + Settings.Default.General_Weight_NbDecimal);
                        dailyTestValues[dailyTestInfo.Measure1 + i] = measuredMasses[i].ToString("N" + Settings.Default.General_Weight_NbDecimal);
                        dailyTestValues[dailyTestInfo.Id1 + i] = refIDs[i];
                    }
                    dailyTestValues[dailyTestInfo.Status] = isDailyTestPass ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(dailyTestInfo, dailyTestValues); });
                    bool isSamplingRecorded = (bool)t.Result;

                    // Même chose dans l'audit trail
                    string description = "Test journalier " + (isDailyTestPass ? "réussi" : "échoué");

                    AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                    object[] auditTrailValues = new object[auditTrailInfo.Ids.Count()];
                    auditTrailValues[auditTrailInfo.Username] = General.loggedUsername;
                    auditTrailValues[auditTrailInfo.EventType] = Settings.Default.General_AuditTrailEvent_Event;
                    auditTrailValues[auditTrailInfo.Description] = description;
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(auditTrailInfo, auditTrailValues); });

                    // On génère le rapport d'étalonnage
                    if (isSamplingRecorded)
                    {
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax_new(new DailyTestInfo(), dailyTestInfo.Ids[dailyTestInfo.Id]); });
                        ReportGeneration report = new ReportGeneration();
                        int id = ((int)t.Result);
                        MyMessageBox.Show("Test journalier " + (isDailyTestPass ? "réussi" : "échoué"));
                        Task printReportTask = Task.Factory.StartNew(() => report.GenerateDailyTestReport(id));
                        //printReportTask.Wait();
                        //report.GenerateSamplingReport(id);
                        //MyMessageBox.Show("Rapport généré");
                    }
                    else
                    {
                        MyMessageBox.Show("L'étalonnage n'a pas être enregistrer");
                    }
                    contentControlMain.Content = new Pages.Status();
                }
            }
            else if (currentPhase == CurrentPhase.Cycle)
            {
                Task<object> t;

                //isWeightCorrect =
                //validWeight >= decimal.Parse(cycleWeightValues[cycleWeightInfo.Min].ToString()) &&
                //validWeight <= decimal.Parse(cycleWeightValues[cycleWeightInfo.Max].ToString());

                //if (!isWeightCorrect)
                if (!IsCycleWeightCorrect(validWeight))
                {
                    MyMessageBox.Show(Settings.Default.CycleWeight_IncorrectWeight);
                    goto End;
                }
                else
                {
                    //if (!Balance.IsFree()) Balance.FreeUse();

                    General.CurrentCycleInfo.UpdateCurrentWeightInfo(new string[] { validWeight.ToString() });

                    CycleWeightInfo cycleWInfo = new CycleWeightInfo();
                    object[] cycleWValues = new object[cycleWInfo.Ids.Count()];
                    cycleWValues[cycleWInfo.WasWeightManual] = isManual ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                    cycleWValues[cycleWInfo.DateTime] = DateTime.Now.ToString(Settings.Default.DateTime_Format_Write);
                    cycleWValues[cycleWInfo.ActualValue] = validWeight.ToString();

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row_new(cycleWInfo, cycleWValues, previousSeqId); });

                    NextSeqInfo nextSeqInfo = new NextSeqInfo(
                        recipeInfo_arg: recipeWeightInfo,
                        recipeValues_arg: recipeWeightValues,
                        frameMain_arg: subCycle.frameMain,
                        frameInfoCycle_arg: subCycle.frameInfoCycle,
                        contentControlMain_arg: subCycle.contentControlMain,
                        contentControlInfoCycle_arg: subCycle.contentControlInfoCycle,
                        idCycle_arg: subCycle.idCycle,
                        previousSeqType_arg: 0,
                        previousSeqId_arg: previousSeqId,
                        isTest_arg: subCycle.isTest);
                    General.NextSequence(nextSeqInfo, new CycleWeightInfo());
                }
            }
            else if (currentPhase == CurrentPhase.BowlWeight)
            {
                //if (!Balance.IsFree()) Balance.FreeUse();
                info.bowlWeight = validWeight.ToString("N" + Settings.Default.General_Weight_NbDecimal.ToString());
                General.StartCycle(info);
            }
        End:
            button.IsEnabled = true;
        }

        private void Stop()
        {
            isgetWeightTaskActive = false;

            //if(!Balance.IsFree()) Balance.FreeUse();
            if (contentControlMain.Content != this && contentControlMain.Content.GetType() != typeof(Pages.SubCycle.CycleWeight))
            {
                //Balance.FreeUse();
            }
            //MessageBox.Show("c'est fini " + Balance.IsFree().ToString());

            getWeightTimer.Stop();
            getWeightTimer.Dispose();

            //if (!Balance.IsFree()) Balance.FreeUse();

            if (currentPhase == CurrentPhase.DailyTest)
            {
                contentControlMain.Content = new Status();
            }
            else
            {
                StopCycle();
            }
        }
        private void StopTimer()
        {
            isgetWeightTaskActive = false;
            getWeightTimer.Stop();
        }
        private void StartTimer()
        {
            isgetWeightTaskActive = true;
            getWeightTimer.Start();
        }

        private void EndCycle(decimal bowlWeight = -1)
        {
            if (currentPhase == CurrentPhase.Cycle || currentPhase == CurrentPhase.FinalWeight)
            {
                logger.Debug("EndCycle");
                isCycleEnded = true;

                General.EndCycle(nextSeqInfo, bowlWeight, finalWeight);
            }
            else if (currentPhase == CurrentPhase.BowlWeight && info.isTest)
            {
                RecipeInfo recipeInfo = new RecipeInfo();
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), info.recipeID); });
                object[] recipeValues = (object[])t.Result;

                info.contentControlMain.Content = new Recipe(RcpAction.Modify, info.contentControlMain, info.contentControlInfoCycle, recipeValues == null ? "" : recipeValues[recipeInfo.Name].ToString(), General.info.Window);
            }
            else
            {
                contentControlMain.Content = new Pages.Status();
            }
        }

        private bool IsWeightCorrect(decimal weightValue, decimal setpoint)
        {
            if (currentPhase == CurrentPhase.DailyTest)
            {
                return IsSampWeightCorrect(weightValue, setpoint);
            }
            else if (currentPhase == CurrentPhase.Cycle)
            {
                return IsCycleWeightCorrect(weightValue);
            }
            else if (currentPhase == CurrentPhase.FinalWeight)
            {
                return IsFinalWeightCorrect(weightValue, setpoint);
            }

            //return Math.Abs(weightValue - setpoint) <= setpoint * currentRatio;
            return false;
        }

        public static bool IsFinalWeightCorrect(decimal weightValue, decimal setpoint)
        {
            return Math.Abs(weightValue - setpoint) <= setpoint * Settings.Default.Cycle_FinalWeightRatio;
        }

        public static bool IsSampWeightCorrect(decimal weightValue, decimal setpoint)
        {
            return Math.Abs(weightValue - setpoint) <= setpoint * Settings.Default.DailyTestRatio;
        }

        private bool IsCycleWeightCorrect(decimal weightValue)
        {
            return weightValue >= decimal.Parse(cycleWeightValues[cycleWeightInfo.Min].ToString()) &&
                   weightValue <= decimal.Parse(cycleWeightValues[cycleWeightInfo.Max].ToString());
        }

        public void EnablePage(bool enable)
        {
            if (currentPhase == CurrentPhase.Cycle || currentPhase == CurrentPhase.FinalWeight || currentPhase == CurrentPhase.BowlWeight)
            {
                tbWeight.IsEnabled = enable;
                btNext.IsEnabled = enable;
                tbScan.IsEnabled = enable;
            }
            else
            {
                Stop();
            }
        }

        public bool IsItATest()
        {
            return (currentPhase == CurrentPhase.BowlWeight && info.isTest) || (currentPhase == CurrentPhase.Cycle && subCycle.isTest);
        }

        public void StopCycle()
        {
            isgetWeightTaskActive = false;
            getWeightTimer.Stop();
            getWeightTimer.Dispose();
            //if (!Balance.IsFree()) Balance.FreeUse();
            EndCycle();
        }

        private async void tbScan_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbScan_LostFocusAsync " + isScanningStep);

            if (isScanningStep)
            {
                tbScan.LostFocus -= tbScan_LostFocus;
                TextBox textbox = sender as TextBox;

                while (!textbox.IsFocused)
                {
                    textbox.Text = "";
                    textbox.Focus();
                    await Task.Delay(500);
                }
                tbScan.LostFocus += tbScan_LostFocus;
            }
        }

        private async void tbScan_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("TbScan_KeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Tab)
            {
                if (recipeWeightValues[recipeWeightInfo.Barcode].ToString() == textbox.Text)
                {
                    isScanningStep = false;
                    tbScan.Visibility = Visibility.Collapsed;
                }
                else
                {
                    textbox.Text = "";
                    for (int i = 0; i < recipeWeightInfo.Ids.Count(); i++)
                    {
                        logger.Trace(recipeWeightValues[i].ToString());
                    }
                    MyMessageBox.Show(Settings.Default.CycleWeigh_IncorrectBarcode + " " + recipeWeightValues[recipeWeightInfo.Barcode].ToString());

                    while (!contentControlMain.IsFocused)
                    {
                        contentControlMain.Focus();
                        await Task.Delay(100);
                    }

                    while (!this.IsFocused)
                    {
                        this.Focus();
                        await Task.Delay(100);
                    }
                }
            }
        }

        private void tbScan_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            tbScan.Focus();
        }

        private void ShowKeyBoard(object sender, RoutedEventArgs e)
        {
            General.ShowKeyBoard();
        }

        private void HideKeyBoard(object sender, RoutedEventArgs e)
        {
            General.HideKeyBoard();
        }

        private void HideKeyBoardIfEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                General.HideKeyBoard();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            isgetWeightTaskActive = false;

            if (!Balance.IsFree()) Balance.FreeUse();
            //MessageBox.Show("Unload " + Balance.IsFree().ToString());
            getWeightTimer.Stop();
            getWeightTimer.Dispose();

            //Stop();
            //if (!Balance.IsFree()) Balance.FreeUse();
            if (!isCycleEnded && (currentPhase == CurrentPhase.FinalWeight || currentPhase == CurrentPhase.Cycle))
            {
                if (this.contentControlMain.Content.GetType() == typeof(Pages.Status) || this.contentControlMain.Content.GetType() == typeof(Pages.Recipe))
                {
                    EndCycle();
                }
            }
        }
    }
}
