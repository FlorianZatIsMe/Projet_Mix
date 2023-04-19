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
using Message;

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
        private readonly object[] recipeWeightValues;
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
        private readonly object[] cycleWeightValues;

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
            CycleTableInfo cycleTableInfo = new CycleTableInfo();
            object[] cycleValues;
            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            object[] cycleWeightValues;
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

                    if (seqTabValues[seqTabInfo.NextSeqType] == null || seqTabValues[seqTabInfo.NextSeqType].ToString() =="") nextType = null;
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

            //isSequenceOver = false;
            isWeightCorrect = false;
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
            setpointText2 = "g [ " + cycleWeightValues[cycleWeightInfo.Min].ToString() + "; " + cycleWeightValues[cycleWeightInfo.Max].ToString() + " ]";

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
                frameMain_arg: subCycle.frameMain,
                frameInfoCycle_arg: subCycle.frameInfoCycle,
                idCycle_arg: subCycle.idCycle,
                previousSeqType_arg: recipeWeightInfo.SeqType, // done
                previousSeqId_arg: previousSeqId,
                isTest_arg: subCycle.isTest) ;
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
                            labelWeight.Foreground = Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())) <= decimal.Parse(recipeWeightValues[recipeWeightInfo.Criteria].ToString()) ? Brushes.AliceBlue : Brushes.Red;
                            logger.Error((Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())) <= decimal.Parse(recipeWeightValues[recipeWeightInfo.Criteria].ToString())).ToString() + weight.value.ToString() + recipeWeightValues[recipeWeightInfo.Setpoint].ToString() + Math.Abs(weight.value - decimal.Parse(recipeWeightValues[recipeWeightInfo.Setpoint].ToString())).ToString() + recipeWeightValues[recipeWeightInfo.Criteria].ToString());
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
                        Stop();
                        return;
                    }
                }

                // Connect the balance
                //Balance.Connect();

                // While the balance is not connected or user give up
                while (!Balance.IsConnected() && !isManual)
                {
                    // We ask the user if he wants to try to connect to the balance again
                    if (MyMessageBox.Show("La balance n'est pas connecté, voulez-vous attendre ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        int count = 0;
                        while (!Balance.IsConnected() && count < 10)
                        {
                            await Task.Delay(1000);
                            count++;
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
                if (isCycle && recipeWeightValues[recipeWeightInfo.IsBarcodeUsed].ToString() == DatabaseSettings.General_TrueValue_Read)
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
                if (isManual)
                {
                    MyMessageBox.Show("Veuillez retirer tout object sur la balance puis faites une tare de la balance manuellement. Cliquer sur le ok une fois terminé");
                }
                else
                {
                    MyMessageBox.Show("Veuillez retirer tout object sur la balance. Cliquer sur le bouton une fois fait");
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
                        if (MyMessageBox.Show("Tare de la balance échouée, voulez-vous réessayer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
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
                if (General.Verify_Format(textBox: tbWeight, isNotNull: true, isNumber: true, parameter: Settings.Default.RecipeWeight_NbDecimal, min: 0))
                {
                    validWeight = decimal.Parse(tbWeight.Text);
                }
                else
                {
                    MyMessageBox.Show("Format de la masse incorrect");
                    goto End;
                }
            }

            if (isFinalWeight)
            {
                if (!IsFinalWeightCorrect(validWeight, currentSetpoint))  //if (!IsWeightCorrect(validWeight))
                {
                    if (MyMessageBox.Show("La masse du produit est incorrecte, voulez-vous continuer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        goto End;
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
                    if (MyMessageBox.Show("La masse du poids étalon est incorrecte, voulez-vous continuer ?", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        StartTimer();
                        goto End;
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
                    object[] dailyTestValues = new object[dailyTestInfo.Ids.Count()];
                    dailyTestValues[dailyTestInfo.Username] = General.loggedUsername;
                    dailyTestValues[dailyTestInfo.EquipmentName] = General.equipement_name;
                    for (int i = 0; i < referenceMasses.Length; i++)
                    {
                        dailyTestValues[dailyTestInfo.Setpoint1 + i] = referenceMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                        dailyTestValues[dailyTestInfo.Measure1 + i] = measuredMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal);
                    }
                    dailyTestValues[dailyTestInfo.Status] = isSamplingPass ? DatabaseSettings.General_TrueValue_Write : DatabaseSettings.General_FalseValue_Write;
                    Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow_new(dailyTestInfo, dailyTestValues); });
                    bool isSamplingRecorded = (bool)t.Result;

                    // Même chose dans l'audit trail
                    string description = "Test journalier " + (isSamplingPass ? "réussi" : "échoué");
                    for (int i = 0; i < referenceMasses.Length; i++)
                    {
                        description += "\n" + dailyTestInfo.Descriptions[dailyTestInfo.Setpoint1 + i] + ": " + referenceMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal) + "g - ";
                        description += dailyTestInfo.Descriptions[dailyTestInfo.Measure1 + i] + ": " + measuredMasses[i].ToString("N" + Settings.Default.RecipeWeight_NbDecimal) + "g";
                    }

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
                        MyMessageBox.Show("Test journalier " + (isSamplingPass ? "réussi" : "échoué"));
                        Task printReportTask = Task.Factory.StartNew(() => report.GenerateSamplingReport(id));
                        //printReportTask.Wait();
                        //report.GenerateSamplingReport(id);
                        //MyMessageBox.Show("Rapport généré");
                    }
                    else
                    {
                        MyMessageBox.Show("L'étalonnage n'a pas être enregistrer");
                    }
                    frameMain.Content = new Pages.Status();
                }
            }
            else if(isCycle)
            {
                Task<object> t;

                isWeightCorrect =
                    validWeight >= decimal.Parse(cycleWeightValues[cycleWeightInfo.Min].ToString()) &&
                    validWeight <= decimal.Parse(cycleWeightValues[cycleWeightInfo.Max].ToString());

                if (!isWeightCorrect)
                {
                    MyMessageBox.Show(Settings.Default.CycleWeight_IncorrectWeight);
                    goto End;
                }
                else
                {
                    if (!Balance.IsFree()) Balance.FreeUse();

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
                        idCycle_arg: subCycle.idCycle,
                        previousSeqType_arg: 0,
                        previousSeqId_arg: previousSeqId,
                        isTest_arg: subCycle.isTest);
                    General.NextSequence(nextSeqInfo, new CycleWeightInfo());
                }
            }
            else
            {
                info.bowlWeight = validWeight.ToString("N" + Settings.Default.RecipeWeight_NbDecimal.ToString());
                General.StartCycle(info);
            }
        End:
            button.IsEnabled = true;
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
                RecipeInfo recipeInfo = new RecipeInfo();
                Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(new RecipeInfo(), info.recipeID); });
                object[] recipeValues = (object[])t.Result;

                info.frameMain.Content = new Recipe(RcpAction.Modify, info.frameMain, info.frameInfoCycle, recipeValues == null ? "" : recipeValues[recipeInfo.Name].ToString(), General.info.Window);
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

                    while (!frameMain.IsFocused)
                    {
                        frameMain.Focus();
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
    }
}
