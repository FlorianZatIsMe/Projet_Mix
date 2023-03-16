using Main;
using Main.Properties;
using System;
using System.Collections.Generic;
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
using Driver_Ethernet_Balance;
using Database;
using Main.Pages;
using System.Timers;

namespace Main.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour WeightBowl.xaml
    /// </summary>
    public partial class CycleWeight : Page, ISubCycle
    {
        private bool isBalanceFree = false;
        private bool isManual = false;

        private bool isPageActive = false;

        private CycleStartInfo info;

        private NextSeqInfo nextSeqInfo;
        private decimal currentSetpoint = -1;
        private decimal currentRatio;
        private string message1;

        private Frame frameMain;

        private bool isFinalWeight = false;
        private bool isBowlWeight = false;
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
        private int timerInterval = 100;

        // Sampling variables
        private bool isSampling = false;
        private decimal[] referenceMasses = { (decimal)5.661, (decimal)12.764 };
        private decimal[] measuredMasses;
        private int sampleNumber;
        private bool isSamplingPass = true;

        // Cycle variables
        private bool isCycle = false;
        private bool isCycleEnded = false;
        private readonly SubCycleArg subCycle;
        private readonly int previousSeqId;
        private readonly RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
        //private readonly decimal setpoint;
        //private readonly decimal min;
        //private readonly decimal max;
        private bool isScanningStep;
        //private bool isSequenceOver;
        //private readonly System.Timers.Timer getWeightTimer;
        //private bool wasBalanceFreeOnce;
        private bool isWeightCorrect;
        //private bool disposedValue;
        //private decimal currentWeight;
        private readonly CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();


        // Constructor to measure the mass of the empty bowl at the start of a cycle
        public CycleWeight(CycleStartInfo info_arg)
        {
            isBowlWeight = true;
            info = info_arg;
            frameMain = info.frameMain;
            message1 = message1EmptyBowl;


            /*
            info.frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            getWeightTimer = new Timer
            {
                AutoReset = true,
                Interval = timerInterval
            };
            getWeightTimer.Elapsed += GetWeightTimer_Elapsed;
            //*/

            InitializeComponent();
            Initialize();
            //labelMessage.Text = tareOnGoing;
        }

        // Constructor to measure the mass of the bowl at the end of a cycle
        public CycleWeight(NextSeqInfo nextSeqInfo_arg)
        {
            isFinalWeight = true;
            nextSeqInfo = nextSeqInfo_arg;
            frameMain = nextSeqInfo.frameMain;
            currentRatio = Settings.Default.LastWeightRatio;
            message1 = message1FullBowl;

            //Calculation the theoritical total weight
            CycleTableInfo currentCycle = new CycleTableInfo();
            CycleWeightInfo currentWeigh = new CycleWeightInfo();
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(CycleTableInfo), nextSeqInfo.idCycle.ToString()); });
            currentCycle = (CycleTableInfo)t.Result;

            try
            {
                currentSetpoint = decimal.Parse(currentCycle.Columns[currentCycle.bowlWeight].Value);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                General.ShowMessageBox(ex.Message);
                currentSetpoint = -1;
            }

            ISeqTabInfo seqTabInfo = currentCycle;
            string nextId = currentCycle.Columns[currentCycle.NextSeqId].Value;
            string nextType = currentCycle.Columns[currentCycle.NextSeqType].Value;

            if (currentSetpoint != -1)
            {
                while (nextId != "" && nextId != null)
                {
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(Sequence.list[int.Parse(nextType)].subCycleInfo.GetType(), nextId); });
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
                                currentSetpoint += decimal.Parse(currentWeigh.Columns[currentWeigh.ActualValue].Value);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex.Message);
                                General.ShowMessageBox(ex.Message);
                                currentSetpoint = -1;
                                nextType = "";
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
        public CycleWeight(Frame frame)
        {
            frameMain = frame;
            isSampling = true;
            sampleNumber = 0;
            currentSetpoint = referenceMasses[sampleNumber];
            currentRatio = Settings.Default.SamplingRatio;
            measuredMasses = new decimal[referenceMasses.Length];
            message1 = message1Sampling;
            InitializeComponent();
            Initialize();
        }

        public CycleWeight(SubCycleArg subCycleArg)
        {
            logger.Debug("Start");
            isCycle = true;
            Task<object> t;

            subCycle = subCycleArg;
            frameMain = subCycle.frameMain;

            //subCycle.frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            //isSequenceOver = false;
            isWeightCorrect = false;
            //wasBalanceFreeOnce = false;
            General.CurrentCycleInfo.UpdateSequenceNumber();

            /*
            // Initialisation des timers
            getWeightTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleWeight_getWeightTimer_Interval,
                AutoReset = false
            };
            getWeightTimer.Elapsed += GetWeightTimer_OnTimedEvent;*/

            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeWeightInfo), subCycle.id); });
            recipeWeightInfo = (RecipeWeightInfo)t.Result;

            if (recipeWeightInfo == null) // Si la commande a renvoyée une ligne
            {
                logger.Error(Settings.Default.CycleWeight_Error_NoRecipe);
                General.ShowMessageBox(Settings.Default.CycleWeight_Error_NoRecipe);
                return; // ou exit carrément
            }

            cycleWeightInfo.SetRecipeParameters(recipeWeightInfo, subCycle.idCycle);

            message1 = message1Cycle1 + recipeWeightInfo.Columns[recipeWeightInfo.Name].Value + message1Cycle2;
            currentSetpoint = decimal.Parse(cycleWeightInfo.Columns[cycleWeightInfo.Setpoint].Value);
            setpointText2 = "g [ " + cycleWeightInfo.Columns[cycleWeightInfo.Min].Value + "; " + cycleWeightInfo.Columns[cycleWeightInfo.Max].Value + " ]";

            /*
            tbPhaseName.Text = recipeWeightInfo.Columns[recipeWeightInfo.Name].Value;
            labelWeight.Text = Settings.Default.CycleWeight_WeightField + " (" + Settings.Default.CycleWeight_SetpointField + ": " + cycleWeightInfo.Columns[cycleWeightInfo.Setpoint].Value + Settings.Default.CycleFinalWeight_g_Unit + ")";
            labelWeightLimits.Text = "[ " + cycleWeightInfo.Columns[cycleWeightInfo.Min].Value + "; " + cycleWeightInfo.Columns[cycleWeightInfo.Max].Value + " ]";
            */

            InitializeComponent();

            logger.Error(cycleWeightInfo.IsSolvent.ToString() + " - " + cycleWeightInfo.Columns[cycleWeightInfo.IsSolvent].Id + " - " + cycleWeightInfo.Columns[cycleWeightInfo.IsSolvent].Value);
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(cycleWeightInfo); });
            //MyDatabase.InsertRow(cycleWInfo);
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(cycleWeightInfo.TabName, cycleWeightInfo.Columns[cycleWeightInfo.Id].Id); });
            previousSeqId = (int)t.Result;
            //idSubCycle = MyDatabase.GetMax(cycleWInfo.name, cycleWInfo.columns[cycleWInfo.id].id);

            subCycle.prevSeqInfo.Columns[subCycle.prevSeqInfo.NextSeqType].Value = cycleWeightInfo.SeqType.ToString();
            subCycle.prevSeqInfo.Columns[subCycle.prevSeqInfo.NextSeqId].Value = previousSeqId.ToString();

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(subCycle.prevSeqInfo, subCycle.idPrevious.ToString()); });
            //MyDatabase.Update_Row(subCycle.prevSeqInfo, subCycle.idPrevious.ToString());
            
            nextSeqInfo = new NextSeqInfo(
                recipeParam_arg: recipeWeightInfo, // done
                frameMain_arg: subCycle.frameMain,
                frameInfoCycle_arg: subCycle.frameInfoCycle,
                idCycle_arg: subCycle.idCycle,
                previousSeqType_arg: recipeWeightInfo.SeqType, // done
                previousSeqId_arg: previousSeqId.ToString(),
                isTest_arg: subCycle.isTest);

            Initialize();
        }

        private void Initialize()
        {
            frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            //*
            getWeightTimer = new Timer
            {
                AutoReset = true,
                Interval = timerInterval
            };
            getWeightTimer.Elapsed += GetWeightTimer_Elapsed;
            //*/

            labelMessage.Text = tareOnGoing;
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

                        if (isFinalWeight || isSampling)
                        {
                            labelWeight.Foreground = IsWeightCorrect(weight.value) ? Brushes.AliceBlue : Brushes.Red;
                            //labelWeight.Foreground = (Math.Abs(weight.value - currentSetpoint) < currentSetpoint * currentRatio) ? Brushes.AliceBlue : Brushes.Red;
                        }
                        else if (isCycle)
                        {
                            labelWeight.Foreground = Math.Abs(weight.value - decimal.Parse(recipeWeightInfo.Columns[recipeWeightInfo.Setpoint].Value)) <= decimal.Parse(recipeWeightInfo.Columns[recipeWeightInfo.Criteria].Value) ? Brushes.AliceBlue : Brushes.Red;
                            logger.Error((Math.Abs(weight.value - decimal.Parse(recipeWeightInfo.Columns[recipeWeightInfo.Setpoint].Value)) <= decimal.Parse(recipeWeightInfo.Columns[recipeWeightInfo.Criteria].Value)).ToString() + weight.value.ToString() + recipeWeightInfo.Columns[recipeWeightInfo.Setpoint].Value + Math.Abs(weight.value - decimal.Parse(recipeWeightInfo.Columns[recipeWeightInfo.Setpoint].Value)).ToString() + recipeWeightInfo.Columns[recipeWeightInfo.Criteria].Value);
            }
                    }
                });
            }

            getWeightTimer.Enabled = isgetWeightTaskActive;
        }

        private async void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            Frame frame = sender as Frame;

            if (frame.Content == this)
            {
                isPageActive = true;
                bool exeTare = true;

                // If the balance is free, we block it
                if (Balance.IsFree())
                {
                    Balance.BlockUse();
                    isBalanceFree = true;
                }

                // While the balance is not free
                while (!isBalanceFree)
                {
                    // We ask the user if he wants to try to block the balance again
                    if (General.ShowMessageBox("La balance n'est pas disponible, voulez-vous attendre ?", "Balance bloquée", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                        Stop();
                        return;
                    }
                }

                // Connect the balance
                Balance.Connect();

                // While the balance is not connected or user give up
                while (!Balance.IsConnected() && !isManual)
                {
                    // We ask the user if he wants to try to connect to the balance again
                    if (General.ShowMessageBox("La balance n'est pas connecté, voulez-vous attendre ?", "Balance déconnecté", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        // If he wants to wait, we connect to the balance
                        Balance.Connect();
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
                if (isCycle && recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Value == DatabaseSettings.General_TrueValue_Read)
                {
                    tbScan.Focus();
                    labelMessage.Text = Settings.Default.CycleWeight_Request_ScanProduct + " " + recipeWeightInfo.Columns[recipeWeightInfo.Name].Value;
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
                if (isManual)
                {
                    General.ShowMessageBox("Veuillez retirer tout object sur la balance puis faites une tare de la balance manuellement. Cliquer sur le ok une fois terminé");
                }
                else
                {
                    General.ShowMessageBox("Veuillez retirer tout object sur la balance. Cliquer sur le bouton une fois fait");
                }

                labelWeight.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
                tbWeight.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;

                while (exeTare)
                {
                    int tare = isManual ? 0 : Balance.TareBalance();
                    if (tare == 0)
                    {
                        labelMessage.Text = message1;
                        btNext.Visibility = Visibility.Visible;

                        if (isFinalWeight || isSampling || isCycle)
                        {
                            labelSetpoint.Visibility = Visibility.Visible;
                            string setpoint = "#";
                            if (currentSetpoint != -1)
                            {
                                setpoint = currentSetpoint.ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                            }
                            labelSetpoint.Text = setpointText1 + setpoint + setpointText2;
                        }

                        // démarrage du timer qui lit en continue la valeur du poids et l'affiche
                        StartTimer();

                        isgetWeightTaskActive = true;
                        //getWeightTask = Task.Factory.StartNew(() => GetWeight()); ;
                        exeTare = false;
                    }
                    else
                    {
                        if (General.ShowMessageBox("Tare de la balance échouée, voulez-vous réessayer ?", "Titre", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            exeTare = false;
                            Stop();
                            return;
                        }
                        logger.Error("Tare de la balance échouée, tare: " + Balance.TareBalance().ToString());
                    }
                }
            }
            else if (isPageActive)
            {
                frame.ContentRendered -= FrameMain_ContentRendered;
                StopTimer();
                if (!Balance.IsFree()) Balance.FreeUse();
                if (!isCycleEnded && (isFinalWeight || isCycle))
                {
                    if (frameMain.Content.GetType() == typeof(Pages.Status) || frameMain.Content.GetType() == typeof(Pages.Recipe))
                    {
                        EndCycle();
                    }
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
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
                        if (General.ShowMessageBox("Le poids n'est toujours pas stable, voulez-vous continuer à attendre ?", "Problème de stabilité", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            // S'il veut continuer à attendre, on continue à attendre
                            waitingCounter = 0;
                        }
                        else
                        {
                            // Sinon on arrête d'attendre
                            keepWaiting = false;
                            Stop();
                            return;
                        }
                    }
                }

                StopTimer();

                if (!keepWaiting) return;

                // Le poids est stable, du coup on le stock dans info et on lance la séquence
                validWeight = weight.value;
            }
            else
            {
                if (General.Verify_Format(textBox: tbWeight, isNotNull: true, isNumber: true, parameter: Settings.Default.RecipeWeight_NbDecimal, min: 0))
                {
                    validWeight = decimal.Parse(tbWeight.Text);
                }
                else
                {
                    General.ShowMessageBox("Format de la masse incorrect");
                    return;
                }
            }

            if (isFinalWeight)
            {
                if (!IsFinalWeightCorrect(validWeight, currentSetpoint))  //if (!IsWeightCorrect(validWeight))
                {
                    if (General.ShowMessageBox("La masse du produit est incorrecte, voulez-vous continuer ?", "Masse incorrect", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        return;
                    }
                }

                StopTimer();
                if (!Balance.IsFree()) Balance.FreeUse();
                EndCycle(validWeight);
            }
            else if (isSampling)
            {
                // On contrôle la mesure
                // Si c'est bon, on continue
                // Sinon, message d'erreur qui propose de recommencer ou pas
                if(!IsSampWeightCorrect(validWeight, currentSetpoint)) //if (!IsWeightCorrect(validWeight))
                {
                    if (General.ShowMessageBox("La masse du poids étalon est incorrecte, voulez-vous continuer ?", "Masse incorrect", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        return;
                    }
                    else
                    {
                        // Si on ne recommence pas, le statut est fail mais on continue
                        isSamplingPass = false;
                    }
                }

                if (sampleNumber < referenceMasses.Length)
                {
                    // On stocke la measure du poids
                    measuredMasses[sampleNumber] = validWeight;
                    sampleNumber++;
                }

                if (sampleNumber < referenceMasses.Length)
                {
                    // On met à jour la cible
                    currentSetpoint = referenceMasses[sampleNumber];
                    labelSetpoint.Text = setpointText1 + currentSetpoint.ToString("N" + Settings.Default.RecipeWeight_NbDecimal);

                    tbWeight.Text = "";
                    tbWeight.Background = Brushes.White;
                    tbWeight.Foreground = Brushes.Black;

                    // On relance le timer
                    StartTimer();
                }
                else
                {
                    // Fin de l'étalonnage
                    StopTimer();
                    if (!Balance.IsFree()) Balance.FreeUse();

                    // On met dans la base de données toutes les mesures (cible et valeur) avec le status
                    DailyTestInfo dailyTestInfo = new DailyTestInfo();
                    dailyTestInfo.Columns[dailyTestInfo.Username].Value = General.loggedUsername;
                    dailyTestInfo.Columns[dailyTestInfo.EquipmentName].Value = General.equipement_name;
                    for (int i = 0; i < referenceMasses.Length; i++)
                    {
                        dailyTestInfo.Columns[dailyTestInfo.Setpoint1 + i].Value = referenceMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                        dailyTestInfo.Columns[dailyTestInfo.Measure1 + i].Value = measuredMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                    }
                    dailyTestInfo.Columns[dailyTestInfo.Status].Value = isSamplingPass ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(dailyTestInfo); });
                    bool isSamplingRecorded = (bool)t.Result;

                    // Même chose dans l'audit trail
                    string description = "Test journalier " + (isSamplingPass ? "réussi" : "échoué");
                    for (int i = 0; i < referenceMasses.Length; i++)
                    {
                        description += "\n" + dailyTestInfo.Columns[dailyTestInfo.Setpoint1 + i].DisplayName + ": " + referenceMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal) + "g - ";
                        description += dailyTestInfo.Columns[dailyTestInfo.Measure1 + i].DisplayName + ": " + measuredMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal) + "g";
                    }

                    AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
                    auditTrailInfo.Columns[auditTrailInfo.Username].Value = General.loggedUsername;
                    auditTrailInfo.Columns[auditTrailInfo.EventType].Value = Settings.Default.General_AuditTrailEvent_Event;
                    auditTrailInfo.Columns[auditTrailInfo.Description].Value = description;
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(auditTrailInfo); });

                    // On génère le rapport d'étalonnage
                    if (isSamplingRecorded)
                    {
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(new DailyTestInfo(), dailyTestInfo.Columns[dailyTestInfo.Id].Id); });
                        ReportGeneration report = new ReportGeneration();
                        string id = ((int)t.Result).ToString();
                        General.ShowMessageBox("Test journalier " + (isSamplingPass ? "réussi" : "échoué"));
                        Task printReportTask = Task.Factory.StartNew(() => report.GenerateSamplingReport(id));
                        printReportTask.Wait();
                        General.ShowMessageBox("Rapport généré");
                    }
                    else
                    {
                        General.ShowMessageBox("L'étalonnage n'a pas être enregistrer");
                    }
                    frameMain.Content = new Pages.Status();
                }
            }
            else if(isCycle)
            {
                Task<object> t;

                isWeightCorrect =
                    validWeight >= decimal.Parse(cycleWeightInfo.Columns[cycleWeightInfo.Min].Value) &&
                    validWeight <= decimal.Parse(cycleWeightInfo.Columns[cycleWeightInfo.Max].Value);

                if (!isWeightCorrect)
                {
                    General.ShowMessageBox(Settings.Default.CycleWeight_IncorrectWeight);
                    return;
                }
                else
                {
                    if (!Balance.IsFree()) Balance.FreeUse();

                    General.CurrentCycleInfo.UpdateCurrentWeightInfo(new string[] { validWeight.ToString() });

                    CycleWeightInfo cycleWInfo = new CycleWeightInfo();
                    cycleWInfo.Columns[cycleWInfo.WasWeightManual].Value = isManual ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                    cycleWInfo.Columns[cycleWInfo.DateTime].Value = DateTime.Now.ToString(Settings.Default.DateTime_Format_Write);
                    cycleWInfo.Columns[cycleWInfo.ActualValue].Value = validWeight.ToString();

                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleWInfo, previousSeqId.ToString()); });

                    SubCycleArg sub = new SubCycleArg(subCycle.frameMain, subCycle.frameInfoCycle, recipeWeightInfo.Columns[recipeWeightInfo.NextSeqId].Value, subCycle.idCycle, previousSeqId, recipeWeightInfo.TabName, new CycleWeightInfo(), subCycle.isTest);

                    NextSeqInfo nextSeqInfo = new NextSeqInfo(
                        recipeParam_arg: recipeWeightInfo,
                        frameMain_arg: subCycle.frameMain,
                        frameInfoCycle_arg: subCycle.frameInfoCycle,
                        idCycle_arg: subCycle.idCycle,
                        previousSeqType_arg: 0,
                        previousSeqId_arg: previousSeqId.ToString(),
                        isTest_arg: subCycle.isTest);
                    General.NextSequence(nextSeqInfo, new CycleWeightInfo());
                }
            }
            else
            {
                info.bowlWeight = validWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
                General.StartCycle(info);
            }

        }

        private void Stop()
        {
            StopTimer();

            if (!Balance.IsFree()) Balance.FreeUse();

            if (isSampling)
            {
                frameMain.Content = new Status();
            }
            else
            {
                StopCycle();
            }

/*            if (!isSampling && info.isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> task = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), info.recipeID); });
                RecipeInfo recipeInfo = (RecipeInfo)task.Result;
                info.frameMain.Content = new Recipe(RcpAction.Modify, info.frameMain, info.frameInfoCycle, recipeInfo.Columns.Count == 0 ? "" : recipeInfo.Columns[recipeInfo.Name].Value);
            }
            else
            {
                frameMain.Content = new Status();
            }*/
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
            if (isCycle || isFinalWeight)
            {
                logger.Debug("EndCycle");
                isCycleEnded = true;

                General.EndCycle(nextSeqInfo, bowlWeight, finalWeight);
            }
            else if (isBowlWeight && info.isTest)
            {
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), info.recipeID.ToString()); });
                RecipeInfo recipeInfo = (RecipeInfo)t.Result;
                info.frameMain.Content = new Recipe(RcpAction.Modify, info.frameMain, info.frameInfoCycle, recipeInfo.Columns.Count == 0 ? "" : recipeInfo.Columns[recipeInfo.Name].Value, General.info.Window);
            }
            else
            {
                frameMain.Content = new Pages.Status();
            }
        }

        private bool IsWeightCorrect(decimal weightValue)
        {
            return Math.Abs(weightValue - currentSetpoint) <= currentSetpoint * currentRatio;
        }

        public static bool IsFinalWeightCorrect(decimal weightValue, decimal setpoint)
        {
            return Math.Abs(weightValue - setpoint) <= setpoint * Settings.Default.LastWeightRatio;
        }

        public static bool IsSampWeightCorrect(decimal weightValue, decimal setpoint)
        {
            return Math.Abs(weightValue - setpoint) <= setpoint * Settings.Default.SamplingRatio;
        }

        public void EnablePage(bool enable)
        {
            if (isCycle || isFinalWeight || isBowlWeight)
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
            return (isBowlWeight && info.isTest) || (isCycle && subCycle.isTest);
        }

        public void StopCycle()
        {
            StopTimer();
            if (!Balance.IsFree()) Balance.FreeUse();
            EndCycle();
        }

        private async void tbScan_LostFocus(object sender, RoutedEventArgs e)
        {
            logger.Debug("TbScan_LostFocusAsync");

            if (isScanningStep)
            {
                TextBox textbox = sender as TextBox;

                await Task.Delay(500);
                textbox.Text = "";
                textbox.Focus();
            }
        }

        private void tbScan_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("TbScan_KeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                if (recipeWeightInfo.Columns[recipeWeightInfo.Barcode].Value == textbox.Text)
                {
                    isScanningStep = false;
                    tbScan.Visibility = Visibility.Collapsed;
                }
                else
                {
                    textbox.Text = "";
                    for (int i = 0; i < recipeWeightInfo.Columns.Count; i++)
                    {
                        logger.Trace(recipeWeightInfo.Columns[i].Value);
                    }
                    General.ShowMessageBox(Settings.Default.CycleWeigh_IncorrectBarcode + " " + recipeWeightInfo.Columns[recipeWeightInfo.Barcode].Value);
                }
            }
        }
    }
}
