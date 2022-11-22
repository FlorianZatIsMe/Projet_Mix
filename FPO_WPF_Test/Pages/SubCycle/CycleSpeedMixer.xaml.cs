using Database;
using Driver.ColdTrap;
using Driver_MODBUS;
using Driver_RS232_Pump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Timers;
using System.Reflection;
using Alarm_Management;
using FPO_WPF_Test.Properties;

namespace FPO_WPF_Test.Pages.SubCycle
{
    /* class CycleSpeedMixer
     * 
     * Description: Classe contrôlant une séquence de SpeedMixer
     * Appelé par: CycleSpeedMixer, Cycle Weight, PreCycle
     * 
     * Classes de références: TBD
     * 
     * Version: 1.0
     */

    public partial class CycleSpeedMixer : Page, IDisposable, ISubCycle
    {
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        //private MyDatabase db = new MyDatabase();
        //private readonly string[] currentPhaseParameters;
        private readonly RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();
        //private List<string[]> thisCycleInfo;
        private readonly int idCycle;
        private readonly int idPrevious;
        private readonly string tablePrevious;
        private ISeqInfo prevSeqInfo;
        private readonly int idSubCycle;
        private readonly bool isTest;

        private bool hasSequenceStarted;
        private bool isSequenceOver;
        private bool isCycleStopped;

        private readonly Task taskSeqController;
        private readonly int timeSeqController = Settings.Default.CycleMix_timeSeqController;
        private readonly Task taskCheckAlarms;
        private readonly int timeCheckAlarms = Settings.Default.CycleMix_timeCheckAlarms_Interval;

        private readonly System.Timers.Timer sequenceTimer;
        private int currentPhaseTime;
        private int currentPhaseNumber;
        private int currentSeqTime;

        private readonly System.Timers.Timer pumpNotFreeTimer;
        private bool isPumpFree;
        private bool wasPumpActivated;
        private int pumpNotFreeSince;

        private readonly System.Timers.Timer tempControlTimer;
        private bool isTempOK;
        private int tempTooHotSince;
        
        private readonly int timeoutPumpNotFree = Settings.Default.CycleMix_timeoutPumpNotFree; // 30s, si la pompe n'est pas disponible pendant ce temps, on arrête la séquence
        private readonly int timeoutTempTooHotDuringCycle = Settings.Default.CycleMix_timeoutTempTooHotDuringCycle;
        private readonly int timeoutTempTooHotBeforeCycle = Settings.Default.CycleMix_timeoutTempTooHotBeforeCycle;
        private readonly int timeoutSequenceTooLong = Settings.Default.CycleMix_timeoutSequenceTooLong;
        private readonly int timeoutSequenceBlocked = Settings.Default.CycleMix_timeoutSequenceBlocked;

        private readonly static int nAlarms = 2;
        private readonly static bool[] areAlarmActive = new bool[nAlarms]; // 0: 3,1 Alarme température trop haute ; 1: 1,1 Erreur du speedmixer pendant un cycle

        private int currentSpeed;
        private decimal currentPressure;

        private bool disposedValue;

        private bool[] status = new bool[8];

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //private AlarmManagement alarmManagement;

        /* public CycleSpeedMixer(Frame mainFrame_arg, string id, List<string[]> cycleInfo)
         * 
         * Description: Constructeur de la classe 
         * 
         * Arguments:
         *      - Frame mainFrame_arg: Frame qui doit contenir ce page (CycleSpeedMixer.xaml)
         *      - string id: valeur de la colonne "id" de la table "recipe_speedmixer". Utilisé pour avoir les paramètres de la séquence en cours
         *      - List<string[]> cycleInfo (ça va sûrement changer): Variable qui se transmet tout au long du cycle. Contient les infos du cycle, utilisé pour générer le rapport
         * 
         * Version: 1.0
         */
        //public CycleSpeedMixer(Frame frameMain_arg, Frame frameInfoCycle_arg, string id, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqInfo prevSeqInfo_arg, bool isTest_arg = true)
        public CycleSpeedMixer(SubCycleArg subCycleArg)
        {
            logger.Debug("Start");

            frameMain = subCycleArg.frameMain;
            frameInfoCycle = subCycleArg.frameInfoCycle;
            string id = subCycleArg.id;
            idCycle = subCycleArg.idCycle;
            idPrevious = subCycleArg.idPrevious;
            tablePrevious = subCycleArg.tablePrevious;
            prevSeqInfo = subCycleArg.prevSeqInfo;
            isTest = subCycleArg.isTest;
            frameMain.ContentRendered += new EventHandler(ThisFrame_ContentRendered);
            //thisCycleInfo = cycleInfo;
            isSequenceOver = false;     // la séquence n'est pas terminée
            hasSequenceStarted = false; // la séquence n'a pas démarré
            isCycleStopped = false;     // ne cycle n'a pas été arrêté

            // Initialisation des timers
            sequenceTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleMix_sequenceTimer_Interval,
                AutoReset = true
            };
            sequenceTimer.Elapsed += SeqTimer_OnTimedEvent;
            currentPhaseNumber = 1;     // la phase en cours est la première
            currentSeqTime = 0;

            pumpNotFreeTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleMix_pumpNotFreeTimer_Interval,
                AutoReset = true
            };
            pumpNotFreeTimer.Elapsed += PumpNotFreeTimer_OnTimedEvent;
            isPumpFree = false;         // la pompe n'est pas disponible
            wasPumpActivated = false;   // la pompe n'a pas encore été commandée
            pumpNotFreeSince = 0;       // Initialisation de la valeur du timer

            tempControlTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleMix_tempControlTimer_Interval,
                AutoReset = true
            };
            tempControlTimer.Elapsed += TempTooHotTimer_OnTimedEvent;
            tempTooHotSince = 0;        // Initialisation de la valeur du timer

            currentSpeed = 0;
            currentPressure = 0;

            // Mise à jour du numéro de séquence (seqNumber de la classe CycleInfo) + démarrage du scan des alarmes si on est sur la première séquence du cycle (voir checkAlarmsTimer_OnTimedEvent de la classe CycleInfo)
            General.CurrentCycleInfo.UpdateSequenceNumber();

            InitializeComponent();

            // On affiche sur le panneau d'information que la séquence est en cours
            General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { Settings.Default.CycleInfo_Mix_StatusOnGoing });


            if (!MyDatabase.IsConnected()) // Si l'on est connecté à la base de données
            {
                logger.Error(DatabaseSettings.Error01);
                return;
            }

            // Penser à changer ça
            MyDatabase.CreateTempTable("speed DECIMAL(5,1) NOT NULL, pressure DECIMAL(5,1) NOT NULL");

            // currentPhaseParameters =  liste des paramètres pour notre séquence
            //this.currentPhaseParameters = MyDatabase.GetOneRow("recipe_speedmixer", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

            recipeSpeedMixerInfo = (RecipeSpeedMixerInfo)MyDatabase.GetOneRow(recipeSpeedMixerInfo.GetType(), id);

            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            //cycleSpeedMixerInfo.SetRecipeParameters(currentPhaseParameters);
            cycleSpeedMixerInfo.SetRecipeParameters(recipeSpeedMixerInfo);

            MyDatabase.InsertRow(cycleSpeedMixerInfo);

            idSubCycle = MyDatabase.GetMax_old(cycleSpeedMixerInfo.name, cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.id].id);

            prevSeqInfo.columns[prevSeqInfo.nextSeqType].value = cycleSpeedMixerInfo.seqType.ToString();
            prevSeqInfo.columns[prevSeqInfo.nextSeqId].value = idSubCycle.ToString();
            MyDatabase.Update_Row(prevSeqInfo, idPrevious.ToString());
            //MyDatabase.Update_Row(tablePrevious, new string[] { "next_seq_type", "next_seq_id" }, new string[] { "1", idSubCycle.ToString() }, idPrevious.ToString());

            //MyDatabase.Disconnect();

            //if (this.currentPhaseParameters.Count() != 0) // S'il n'y a pas eu d'erreur...
            if (recipeSpeedMixerInfo == null) // S'il n'y a pas eu d'erreur...
            {
                logger.Error(Settings.Default.CycleMix_Erro01);
                MessageBox.Show(Settings.Default.CycleMix_Erro01);
                return;
            }

            //tbPhaseName.Text = this.currentPhaseParameters[3];
            tbPhaseName.Text = recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.seqName].value;

            pumpNotFreeTimer.Start();
            if (recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.coldtrap].value == DatabaseSettings.General_TrueValue) tempControlTimer.Start(); // On lance le timer de contrôle de la température
              
            SpeedMixerModbus.SetProgram(recipeSpeedMixerInfo);
            //SpeedMixerModbus.SetProgram(this.currentPhaseParameters); // On met à jour tout les paramètres dans le speedmixer
            taskSeqController = Task.Factory.StartNew(() => SequenceController()); // On lance la tâche de vérification du status et d'autre choses sûrement
            taskCheckAlarms = Task.Factory.StartNew(() => CheckAlarms());
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                    if (taskSeqController != null)
                    {
                        taskSeqController.Wait();
                        taskSeqController.Dispose();
                    }
                    if (taskCheckAlarms != null)
                    {
                        taskCheckAlarms.Wait();
                        taskCheckAlarms.Dispose();
                    }
                    if (sequenceTimer != null)
                    {
                        sequenceTimer.Stop();
                        sequenceTimer.Dispose();
                    }
                    if (pumpNotFreeTimer != null)
                    {
                        pumpNotFreeTimer.Stop();
                        pumpNotFreeTimer.Dispose(); 
                    }
                    if (tempControlTimer != null)
                    {
                        tempControlTimer.Stop();
                        tempControlTimer.Dispose();
                    }
                }

                // TODO: libérer les ressources non managées (objets non managés) et substituer le finaliseur
                // TODO: affecter aux grands champs une valeur null
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        ~CycleSpeedMixer()
        {
            Dispose(disposing: false);
            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Disconnection done");
        }
        private async void SequenceController()
        {
            while (!isSequenceOver) // tant que le cycle est en cours
            {
                logger.Debug("SequenceController");
                status = SpeedMixerModbus.GetStatus();

                currentPressure = SpeedMixerModbus.GetPressure();
                currentSpeed = SpeedMixerModbus.GetSpeed();
                
                this.Dispatcher.Invoke(() =>
                {
                    tbReadyToRun.Text = status[SpeedMixerSettings.MixerStatusId_ReadyToRun] ? "Ready to run " + hasSequenceStarted.ToString() : "Not ready to run";
                    tbMixerRunning.Text = status[SpeedMixerSettings.MixerStatusId_MixerRunning] ? "Mixer running" : "Mixer not running";
                    tbMixerError.Text = status[SpeedMixerSettings.MixerStatusId_MixerError] ? "Mixer Error" : "No error";
                    tbLidOpen.Text = status[SpeedMixerSettings.MixerStatusId_LidOpen] ? "Lid Open" : "Lid not open";
                    tbLidClosed.Text = status[SpeedMixerSettings.MixerStatusId_LidClosed] ? "Lid closed" : "Lid not closed";
                    tbSafetyOK.Text = status[SpeedMixerSettings.MixerStatusId_SafetyOK] ? "Safety OK" : "Safety not OK";
                    tbRobotAtHome.Text = status[SpeedMixerSettings.MixerStatusId_RobotAtHome] ? "Robot at home" : "Robot not at home";
                    tbPressure.Text = "Pression : " + currentPressure.ToString();
                    tbSpeed.Text = "Vitesse : " + currentSpeed.ToString();
                });

                // Si on n'a pas encore démarré mais que le capot n'est pas fermé (Safety not OK)
                if (!hasSequenceStarted && !status[SpeedMixerSettings.MixerStatusId_SafetyOK])
                {
                    MessageBox.Show(Settings.Default.CycleMix_Request_CloseLid);
                }
                // Si on n'a pas encore démarré et que le capot est fermé alors on démarre le cycle
                else if (!hasSequenceStarted && status[SpeedMixerSettings.MixerStatusId_SafetyOK] && !status[SpeedMixerSettings.MixerStatusId_MixerRunning])
                {
                    if ((recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.coldtrap].value == DatabaseSettings.General_FalseValue || isTempOK) && 
                        (wasPumpActivated || recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.vaccum_control].value == DatabaseSettings.General_FalseValue)) // Si on ne conttrôle pas la température ou qu'elle est bonne et si la pompe a été commandée ou qu'on en a pas besoin, on démarre le cycle
                    //if ((currentPhaseParameters[11] == "False" || isTempOK) && (wasPumpActivated || currentPhaseParameters[6] == "False")) // Si on ne conttrôle pas la température ou qu'elle est bonne et si la pompe a été commandée ou qu'on en a pas besoin, on démarre le cycle
                    {
                        if (!status[SpeedMixerSettings.MixerStatusId_MixerError])
                        {
                            MessageBox.Show(Settings.Default.CycleMix_Request_StartMix); // Peut-être retirer ça s'il y a plusieurs cycle
                            SpeedMixerModbus.RunProgram();

                            this.Dispatcher.Invoke(() =>
                            {
                                tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * (currentPhaseNumber-1)].value + "s";
                                currentPhaseTime = int.Parse(recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * (currentPhaseNumber - 1)].value);
                                //tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentPhaseNumber] + "s";
                                //currentPhaseTime = int.Parse(currentPhaseParameters[10 + 3 * currentPhaseNumber]);
                                tbPhaseTime.Text = currentPhaseTime.ToString();
                            });

                            hasSequenceStarted = true; // le programme a démarré
                            sequenceTimer.Start();
                        }
                        else
                        {
                            SpeedMixerModbus.ResetErrorProgram();
                            logger.Debug("ResetErrorProgram");
                        }
                    }
                }
                // Si le programme est en cours
                else if (hasSequenceStarted && status[SpeedMixerSettings.MixerStatusId_MixerRunning])
                {/*
                    // Si on a besoin de la pompe mais qu'elle n'est pas disponible et que le timer n'a pas été lancer
                    if (currentPhaseParameters[6] == "True" && !RS232Pump.IsOpen() && !pumpNotFreeTimer.Enabled && pumpNotFreeSince == 0)
                    {
                        // On lance le timer
                        pumpNotFreeTimer.Start();

                        // Il faut vite fermer cette message box sinon le cycle va s'arrêter, 
                        MessageBox.Show("Démarrage du timer_2: LA POMPE N'EST PAS DISPO, VITE ! RENDS LA DISPONIBLE OU LE CYCLE VA S'ARRETER POUR TOUJOURS !!!");
                    }
                    else if (pumpNotFreeSince != 0 && RS232Pump.IsOpen())
                    {
                        pumpNotFreeSince = 0;
                    }*/
                }
                // si le cycle a démarré mais qu'il ne tourne plus (si c'est la fin du programme Speedmixer)
                else if (hasSequenceStarted && !status[SpeedMixerSettings.MixerStatusId_MixerRunning] && status[SpeedMixerSettings.MixerStatusId_RobotAtHome])
                {
                    isSequenceOver = true; // le cycle est fini, du coup on arrête de le contrôler
                }

                await Task.Delay(timeSeqController);
                //MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - GO");
            }

            if (currentPhaseTime >= 0) // Si la séquence s'est pas arrêtée avant la fin, on arrête le cycle
            {
                isCycleStopped = true;
            }

            // un fois que le cycle est terminé, on commence la séquence final
            this.Dispatcher.Invoke(() =>
            {
                EndSequence();
            });
        }
        private void TempTooHotTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //
            // Il faudrait montrer la valeur du timer et un message qui informe l'utilisateur de la déconnexion de la pompe
            // 

            isTempOK = ColdTrap.IsTempOK();

            this.Dispatcher.Invoke(() =>
            {
                tbTemperatureOK.Text = isTempOK ? "Bonne" : "Trop chaude";
            });

            // Si la température est bonne, on réinitialise le timer
            if (isTempOK)
            {
                tempTooHotSince = 0;
                //tempControlTimer.Stop();
            }
            else // Sinon on l'incrémente et on se demande si on est en timeout
            {
                tempTooHotSince++;

                // On gère différemment la situation si le cycle a démarré ou pas
                if (hasSequenceStarted)
                {
                    if (tempTooHotSince > timeoutTempTooHotDuringCycle)
                    {
                        tempControlTimer.Stop();
                        logger.Error(Settings.Default.CycleMix_Error_TempTooHot);
                        MessageBox.Show(Settings.Default.CycleMix_Error_TempTooHot);
                        StopCycle();
                    }
                }
                else
                {
                    if (tempTooHotSince > timeoutTempTooHotBeforeCycle)
                    {
                        tempControlTimer.Stop();

                        logger.Error(Settings.Default.CycleMix_Error_TempTooHot);
                        MessageBox.Show(Settings.Default.CycleMix_Error_TempTooHot);
                        StopCycle();
                    }
                }
            }

            // Gestion de l'alarme
            if (hasSequenceStarted)
            {
                if (!isTempOK && !areAlarmActive[0])
                {
                    areAlarmActive[0] = true;
                    AlarmManagement.NewAlarm(3, 1); // Alarme température trop haute
                }
                else if (isTempOK && areAlarmActive[0])
                {
                    areAlarmActive[0] = false;
                    AlarmManagement.InactivateAlarm(3, 1); // Alarme température trop haute
                }
            }
        }
        private void PumpNotFreeTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //
            // Il faudrait montrer la valeur du timer et un message qui informe l'utilisateur de la déconnexion de la pompe                        // Ici on va afficher un ruban qui dit qu'on démarre le timer est en cours
            // Ici on va afficher un ruban qui dit qu'on démarre le timer est en cours
            // 

            if (!isPumpFree && RS232Pump.rs232.IsFree())
            {
                RS232Pump.rs232.BlockUse();
                isPumpFree = true;
            }

            // Si la connection avec la pompe est ouverte et qu'on a le droit de lui parler...
            if (RS232Pump.rs232.IsOpen() && isPumpFree)
            {
                pumpNotFreeSince = 0; // On réinitialise la valeur du timeout

                // Si la séquence n'a pas démarré, on démarre ou éteint la pompe
                if (!wasPumpActivated && !hasSequenceStarted)
                {
                    if (recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.vaccum_control].value == DatabaseSettings.General_TrueValue)
                    //if (currentPhaseParameters[6] == "True")
                    {
                        RS232Pump.rs232.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe

                        //Task.Delay(25); // C'est sale il faut changer ça

                        //RS232Pump.SetCommand("!C803 0");   // On pompe à fond
                    }
                    else RS232Pump.rs232.SetCommand("!C802 0"); // Sinon on arrête la pompe 
                    wasPumpActivated = true;
                }
            }
            else
            {
                pumpNotFreeSince++;

                if (pumpNotFreeSince > timeoutPumpNotFree)
                {
                    if (hasSequenceStarted)
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - TIMEOUT !!! Le cycle est en cours, je ne l'arrête pas, ALARME (WARNING) !");
                    }
                    else
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - TIMEOUT !!! Il faut arrêter le cycle maintenant, ALARME !!!");
                        StopCycle();
                    }
                }
            }
        }
        private void SeqTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            currentSeqTime++; // On met à jour le temps total du mix, quand est-ce qu'il s'arrête ?

            //MyDatabase.InsertRow("temp2", "description", new string[] { "InsertRow - seqTimer_OnTimedEvent" });
            MyDatabase.InsertRow("temp", "speed, pressure", new string[] { currentSpeed.ToString(), currentPressure.ToString() });

            currentPhaseTime--;
            
            if (currentPhaseTime >= 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    tbPhaseTime.Text = currentPhaseTime.ToString();
                });
            }
            else if (currentPhaseTime == -timeoutSequenceTooLong)
            {
                logger.Error(Settings.Default.CycleMix_Error_MixTooLong);
                MessageBox.Show(Settings.Default.CycleMix_Error_MixTooLong);
                StopCycle();
            }
            else if (currentPhaseTime == -timeoutSequenceBlocked)
            {
                logger.Error(Settings.Default.CycleMix_Error_MixerBlocked);
                MessageBox.Show(Settings.Default.CycleMix_Error_MixerBlocked);
                isSequenceOver = true;
            }

            // on devrait commencer le timeout quand le robot est à home
            // Penser à ajouter des tests : vérifier que la vitesse et la pression du speedmixer est à peu près celle du paramètre

            if (currentPhaseTime <= 0)
            {
                if (recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * currentPhaseNumber].value != null && recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * currentPhaseNumber].value != "")
                //if (currentPhaseParameters[10 + 3 * (currentPhaseNumber + 1)] != null && currentPhaseParameters[10 + 3 * (currentPhaseNumber + 1)] != "")
                {
                    currentPhaseNumber++;
                    currentPhaseTime = int.Parse(recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * (currentPhaseNumber - 1)].value);
                    //currentPhaseTime = int.Parse(currentPhaseParameters[10 + 3 * currentPhaseNumber]);

                    this.Dispatcher.Invoke(() =>
                    {
                        tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.time00 + 3 * (currentPhaseNumber - 1)].value + "s";
                        //tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentPhaseNumber] + "s";
                        tbPhaseTime.Text = currentPhaseTime.ToString();
                    });
                }
            }
        }
        private async void CheckAlarms()
        {
            //Not good, to change to a timer
            while (!hasSequenceStarted) await Task.Delay(timeCheckAlarms);

            while (hasSequenceStarted && !isSequenceOver)
            {
                if (!areAlarmActive[1] && status[SpeedMixerSettings.MixerStatusId_MixerError])
                {
                    AlarmManagement.NewAlarm(1,1);
                    areAlarmActive[1] = true;
                }
                else if (areAlarmActive[1] && !status[SpeedMixerSettings.MixerStatusId_MixerError])
                {
                    AlarmManagement.InactivateAlarm(1, 1);
                    areAlarmActive[1] = false;
                }

                await Task.Delay(timeCheckAlarms);
            }
        }
        private void EndSequence()
        {
            // On arrête les timers (celle qui gère le temps de la séquence, la température du cold trap et celle qui gère la dispo de la pompe)
            sequenceTimer.Stop();
            tempControlTimer.Stop();
            pumpNotFreeTimer.Stop();

            // Peut-être pas, à voir
            while (sequenceTimer.Enabled) ;
            while (tempControlTimer.Enabled) ;
            while (pumpNotFreeTimer.Enabled) ;
            //MessageBox.Show("2");

            General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { Settings.Default.CycleInfo_Mix_StatusEnded });
            //thisCycleInfo.Add(info);

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect(); // Il va falloir supprimer ça

            MyDatabase.SelectFromTemp("AVG(speed), AVG(pressure), STD(speed), STD(pressure)");
            string[] array = MyDatabase.ReadNext();
            string comment = "";
            MyDatabase.Close_reader();

            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.dateTimeEnd].value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.timeMixEff].value = TimeSpan.FromSeconds(currentSeqTime).ToString();
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.speedMean].value = array[0];
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.pressureMean].value = array[1];
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.speedStd].value = array[2];
            cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.pressureStd].value = array[3];
            MyDatabase.Update_Row(cycleSpeedMixerInfo, idSubCycle.ToString());

//            MyDatabase.Update_Row("cycle_speedmixer",
//                new string[] { "date_time_end", "time_mix_eff", "speed_mean", "pressure_mean", "speed_std", "pressure_std" },
//                new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), TimeSpan.FromSeconds(currentSeqTime).ToString(), array[0], array[1], array[2], array[3] }, idSubCycle.ToString());

            if (areAlarmActive[0]) // Si l'alarme est toujours active alors on l'a désactive
            {
                AlarmManagement.InactivateAlarm(3, 1); // Alarme température trop haute
                areAlarmActive[0] = false;
            }
            if (areAlarmActive[1])
            {
                AlarmManagement.InactivateAlarm(1, 1);
                areAlarmActive[1] = false;
            }

            //*
            if (!isCycleStopped && recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.nextSeqType].value != recipeSpeedMixerInfo.seqType.ToString()) // Si la prochaine séquence est une séquence de poids et que le cycle n'est pas arrêté
            //if (!isCycleStopped && currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids et que le cycle n'est pas arrêté
            {
                if (RS232Pump.rs232.IsOpen() && isPumpFree) RS232Pump.rs232.SetCommand("!C802 0");
                RS232Pump.rs232.FreeUse();
                isPumpFree = false;
            }
            else if (!isCycleStopped && recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.nextSeqType].value == recipeSpeedMixerInfo.seqType.ToString()) // Si la prochaine séquence est une séquence speedmixer et que le cycle n'est pas arrêté
            //else if (!isCycleStopped && currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer et que le cycle n'est pas arrêté
            {
                RS232Pump.rs232.FreeUse();
                isPumpFree = false;
            }
            else if (recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.nextSeqType].value == null || 
                recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.nextSeqType].value == "" || isCycleStopped) // Si c'est fini
            //else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "" || isCycleStopped) // Si c'est fini
            {
                if (RS232Pump.rs232.IsOpen() && isPumpFree) RS232Pump.rs232.SetCommand("!C802 0");
                RS232Pump.rs232.FreeUse();
                isPumpFree = false;

                comment = isCycleStopped ? "Cycle interrompu" : "";
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Je ne sais pas, je ne sais plus, je suis perdu");
            }

            if (isCycleStopped)
            {
                General.EndSequence(recipeParameters: new string[] { "currentPhaseParameters" }, recipeSpeedMixerInfo, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: 1, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: comment);
            }
            else
            {
                General.NextSequence(new string[] { "currentPhaseParameters" }, recipeSpeedMixerInfo, frameMain, frameInfoCycle, idCycle, idSubCycle, 1, new CycleSpeedMixerInfo(), isTest, comment);

            }

            Dispose();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StopCycle();
        }
        private void ThisFrame_ContentRendered(object sender, EventArgs e)
        {
            if (frameMain.Content != this)
            {
                frameMain.ContentRendered -= ThisFrame_ContentRendered;
                if(!isSequenceOver) StopCycle();
            }

        } 
        private void StopCycle()
        {
            //
            // Fait quelque chose pour le rapport
            // 

            SpeedMixerModbus.StopProgram();
            isCycleStopped = true;
            if (!hasSequenceStarted) isSequenceOver = true; // Si la séquence n'a pas démarré on l'arrête
            // On attend que le cycle se termine, plutôt que de faire ça: isSequenceOver = true; // ou directement EndSequence(), à voir
        }

        public ISubCycle NewInstance(SubCycleArg subCycleArg)
        {
            return new CycleSpeedMixer(subCycleArg);
        }
    }
}
