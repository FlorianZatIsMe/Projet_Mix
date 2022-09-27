using Database;
using Driver.ColdTrap;
using Driver.MODBUS;
using Driver.RS232.Pump;
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

    public partial class CycleSpeedMixer : Page, IDisposable
    {
        private Frame mainFrame;
        private MyDatabase db = new MyDatabase();
        private readonly string[] currentPhaseParameters;
        private List<string[]> thisCycleInfo;
        private int idCycle;
        private int idPrevious;
        private string tablePrevious;
        private int idSubCycle;

        private bool hasSequenceStarted;
        private bool isSequenceOver;
        private bool isCycleStopped;

        private Task taskGetStatus;
        private int timeGetStatus = 500;

        private System.Timers.Timer sequenceTimer;
        private int currentPhaseTime;
        private int currentPhaseNumber;
        private int currentSeqTime;

        private System.Timers.Timer pumpNotFreeTimer;
        private bool isPumpFree;
        private bool wasPumpActivated;
        private int pumpNotFreeSince;

        private System.Timers.Timer tempControlTimer;
        private bool isTempOK;
        private int tempTooHotSince;

        private readonly int timeoutPumpNotFree = 30; // 30s, si la pompe n'est pas disponible pendant ce temps, on arrête la séquence
        private readonly int timeoutTempTooHotDuringCycle = 20;
        private readonly int timeoutTempTooHotBeforeCycle = 30;
        private readonly int timeoutSequenceTooLong = 60;
        private readonly int timeoutSequenceBlocked = 90;

        private static int nAlarms = 1;
        private static bool[] areAlarmActive = new bool[nAlarms];

        private int currentSpeed;
        private decimal currentPressure;
        private decimal speedMean;
        private decimal pressureMean;
        private decimal speedSTD;
        private decimal pressureSTD;

        private bool disposedValue;

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
        public CycleSpeedMixer(Frame mainFrame_arg, string id, List<string[]> cycleInfo, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg)
        {
            mainFrame = mainFrame_arg;
            idCycle = idCycle_arg;
            idPrevious = idPrevious_arg;
            tablePrevious = tablePrevious_arg;
            mainFrame.ContentRendered += new EventHandler(thisFrame_ContentRendered);
            thisCycleInfo = cycleInfo;
            isSequenceOver = false;     // la séquence n'est pas terminée
            hasSequenceStarted = false; // la séquence n'a pas démarré
            isCycleStopped = false;     // ne cycle n'a pas été arrêté

            // Initialisation des timers
            sequenceTimer = new System.Timers.Timer();
            sequenceTimer.Interval = 1000;
            sequenceTimer.Elapsed += seqTimer_OnTimedEvent;
            sequenceTimer.AutoReset = true;
            currentPhaseNumber = 1;     // la phase en cours est la première
            currentSeqTime = 0;

            pumpNotFreeTimer = new System.Timers.Timer();
            pumpNotFreeTimer.Interval = 1000;
            pumpNotFreeTimer.Elapsed += pumpNotFreeTimer_OnTimedEvent;
            pumpNotFreeTimer.AutoReset = true;
            isPumpFree = false;         // la pompe n'est pas disponible
            wasPumpActivated = false;   // la pompe n'a pas encore été commandée
            pumpNotFreeSince = 0;       // Initialisation de la valeur du timer

            tempControlTimer = new System.Timers.Timer();
            tempControlTimer.Interval = 1000;
            tempControlTimer.Elapsed += tempTooHotTimer_OnTimedEvent;
            tempControlTimer.AutoReset = true;
            tempTooHotSince = 0;        // Initialisation de la valeur du timer

            currentSpeed = 0;
            currentPressure = 0;
            speedMean = 0;
            pressureMean = 0;
            speedSTD = 0;
            pressureSTD = 0;

            // Mise à jour du numéro de séquence (seqNumber de la classe CycleInfo) + démarrage du scan des alarmes si on est sur la première séquence du cycle (voir checkAlarmsTimer_OnTimedEvent de la classe CycleInfo)
            General.CurrentCycleInfo.UpdateSequenceNumber();

            InitializeComponent();

            // On affiche sur le panneau d'information que la séquence est en cours
            General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { "En cours" });

            //if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // Si l'on est connecté à la base de données
            {
                db.CreateTempTable("speed DECIMAL(5,1) NOT NULL, pressure DECIMAL(5,1) NOT NULL");

                /*
                db.InsertRow("temp", "speed, pressure", new string[] { "10", "36" });
                db.InsertRow("temp", "speed, pressure", new string[] { "20", "92" });
                db.InsertRow("temp", "speed, pressure", new string[] { "30", "28" });
                db.InsertRow("temp", "speed, pressure", new string[] { "40", "15" });
                db.InsertRow("temp", "speed, pressure", new string[] { "50", "43" });
                */

                // currentPhaseParameters =  liste des paramètres pour notre séquence
                this.currentPhaseParameters = db.GetOneRow("recipe_speedmixer", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                int i;
                int timeTh_seconds = 0;

                i = 0;
                while (i != 10 && currentPhaseParameters[12 + 3 * i] != "")
                {
                    timeTh_seconds += int.Parse(currentPhaseParameters[13 + 3 * i]);

                    i++;
                }

                string columns = "date_time_start, time_mix_th, pressure_unit, speed_min, speed_max, pressure_min, pressure_max";
                string[] values = new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), TimeSpan.FromSeconds(timeTh_seconds).ToString(), currentPhaseParameters[9], currentPhaseParameters[42], currentPhaseParameters[43], currentPhaseParameters[44], currentPhaseParameters[45] };
                //string[] values = new string[] { this.currentPhaseParameters[3], setpoint.ToString(), min.ToString(), max.ToString(), this.currentPhaseParameters[6], this.currentPhaseParameters[7] };
                db.InsertRow("cycle_speedmixer", columns, values);
                idSubCycle = db.GetMax("cycle_speedmixer", "id");

                db.Update_Row(tablePrevious, new string[] { "next_seq_type", "next_seq_id" }, new string[] { "1", idSubCycle.ToString() }, idPrevious.ToString());

                //db.Disconnect();

                if (this.currentPhaseParameters.Count() != 0) // S'il n'y a pas eu d'erreur...
                {
                    tbPhaseName.Text = this.currentPhaseParameters[3];
                    /*
                    // Pump initializsation
                    // Si la connection RS232 avec la pompe est ouverte et que personne d'autre n'utilise la pompe
                    if (RS232Pump.IsOpen() && RS232Pump.IsFree())
                    {
                        RS232Pump.BlockUse();   // On bloque l'utilisation de la pompe par quelqu'un d'autre
                        isPumpFree = true;      // Et on dit que la pompe est dispo

                        if (currentPhaseParameters[6] == "True") RS232Pump.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe
                        else RS232Pump.SetCommand("!C802 0"); // Sinon on arrête la pompe 
                    }
                    else
                    { 
                        //isPumpFree = false;    // Sinon la pompe n'est pas dispo
                        pumpNotFreeTimer.Start();

                        if (RS232Pump.IsOpen())
                        {
                            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la pompe déjà en cours, tu vois");
                        }
                        else
                        {
                            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la pompe impossible");
                        }
                    }*/

                    pumpNotFreeTimer.Start();
                    tempControlTimer.Start(); // On lance le timer de contrôle de la température
                    SpeedMixerModbus.SetProgram(this.currentPhaseParameters); // On met à jour tout les paramètres dans le speedmixer
                    taskGetStatus = Task.Factory.StartNew(() => sequenceController()); // On lance la tâche de vérification du status et d'autre choses sûrement
                }
            }
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                //db.ConnectAsync();
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                    if (taskGetStatus != null) taskGetStatus.Dispose();
                    if (sequenceTimer != null) sequenceTimer.Dispose();
                    if (pumpNotFreeTimer != null) pumpNotFreeTimer.Dispose();
                    if (tempControlTimer != null) tempControlTimer.Dispose();
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
        private async void sequenceController()
        {
            bool[] status = new bool[8];

            while (!isSequenceOver) // tant que le cycle est en cours
            {
                status = SpeedMixerModbus.GetStatus();

                currentPressure = (decimal)SpeedMixerModbus.GetPressure() / 10;
                currentSpeed = SpeedMixerModbus.GetSpeed();

                this.Dispatcher.Invoke(() =>
                {
                    tbReadyToRun.Text = status[0] ? "Ready to run " + hasSequenceStarted.ToString() : "Not ready to run";
                    tbMixerRunning.Text = status[1] ? "Mixer running" : "Mixer not running";
                    tbMixerError.Text = status[2] ? "Mixer Error" : "No error";
                    tbLidOpen.Text = status[3] ? "Lid Open" : "Lid not open";
                    tbLidClosed.Text = status[4] ? "Lid closed" : "Lid not closed";
                    tbSafetyOK.Text = status[5] ? "Safety OK" : "Safety not OK";
                    tbRobotAtHome.Text = status[7] ? "Robot at home" : "Robot not at home";
                    tbPressure.Text = "Pression : " + currentPressure.ToString();
                    tbSpeed.Text = "Vitesse : " + currentSpeed.ToString();
                });

                // Si on n'a pas encore démarré mais que le capot n'est pas fermé (Safety not OK)
                if (!hasSequenceStarted && !status[5])
                {
                    MessageBox.Show("Veuillez fermer le capot avant de démarrer le cycle");
                }
                // Si on n'a pas encore démarré et que le capot est fermé alors on démarre le cycle
                else if (!hasSequenceStarted && status[5] && !status[1])
                {
                    if ((currentPhaseParameters[11] == "False" || isTempOK) && (wasPumpActivated || currentPhaseParameters[6] == "False")) // Si on ne conttrôle pas la température ou qu'elle est bonne et si la pompe a été commandée ou qu'on en a pas besoin, on démarre le cycle
                    {
                        MessageBox.Show("Cliquez sur OK pour démarrer le speedmixer"); // Peut-être retirer ça s'il y a plusieurs cycle
                        SpeedMixerModbus.RunProgram();

                        this.Dispatcher.Invoke(() =>
                        {
                            tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentPhaseNumber] + "s";
                            currentPhaseTime = int.Parse(currentPhaseParameters[10 + 3 * currentPhaseNumber]);
                            tbPhaseTime.Text = currentPhaseTime.ToString();
                        });

                        hasSequenceStarted = true; // le programme a démarré
                        sequenceTimer.Start();
                    }
                }
                // Si le programme est en cours
                else if (hasSequenceStarted && status[1])
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
                else if (hasSequenceStarted && !status[1] && status[7])
                {
                    isSequenceOver = true; // le cycle est fini, du coup on arrête de le contrôler
                }

                await Task.Delay(timeGetStatus);
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
        private void tempTooHotTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
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

                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - TIMEOUT pendant le cycle !!! C'est fini, il fait beaucoup trop chaud");
                        stopCycle();
                    }
                }
                else
                {
                    if (tempTooHotSince > timeoutTempTooHotBeforeCycle)
                    {
                        tempControlTimer.Stop();

                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - TIMEOUT avant le cycle !!! C'est fini, il fait beaucoup trop chaud");
                        stopCycle();
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
        private void pumpNotFreeTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //
            // Il faudrait montrer la valeur du timer et un message qui informe l'utilisateur de la déconnexion de la pompe                        // Ici on va afficher un ruban qui dit qu'on démarre le timer est en cours
            // Ici on va afficher un ruban qui dit qu'on démarre le timer est en cours
            // 

            if (!isPumpFree && RS232Pump.IsFree())
            {
                RS232Pump.BlockUse();
                isPumpFree = true;
            }

            // Si la connection avec la pompe est ouverte et qu'on a le droit de lui parler...
            if (RS232Pump.IsOpen() && isPumpFree)
            {
                pumpNotFreeSince = 0; // On réinitialise la valeur du timeout

                // Si la séquence n'a pas démarré, on démarre ou éteint la pompe
                if (!wasPumpActivated && !hasSequenceStarted)
                {
                    if (currentPhaseParameters[6] == "True") RS232Pump.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe
                    else RS232Pump.SetCommand("!C802 0"); // Sinon on arrête la pompe 
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
                        stopCycle();
                    }
                }
            }
        }
        private void seqTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            currentSeqTime++; // On met à jour le temps total du mix, quand est-ce qu'il s'arrête ?

            db.InsertRow("temp", "speed, pressure", new string[] { currentSpeed.ToString(), currentPressure.ToString() });

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
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - C'est normal que ça traîne comme ça ? Attention je vais arrêter le timer");
                stopCycle();
            }
            else if (currentPhaseTime == -timeoutSequenceBlocked)
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - C'est fini, il faut se faire une raison");
                isSequenceOver = true;
            }

            // on devrait commencer le timeout quand le robot est à home
            // Penser à ajouter des tests : vérifier que la vitesse et la pression du speedmixer est à peu près celle du paramètre

            if (currentPhaseTime <= 0)
            {
                if (currentPhaseParameters[10 + 3 * (currentPhaseNumber + 1)] != null && currentPhaseParameters[10 + 3 * (currentPhaseNumber + 1)] != "")
                {
                    currentPhaseNumber++;
                    currentPhaseTime = int.Parse(currentPhaseParameters[10 + 3 * currentPhaseNumber]);

                    this.Dispatcher.Invoke(() =>
                    {
                        tbPhaseNumber.Text = currentPhaseNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentPhaseNumber] + "s";
                        tbPhaseTime.Text = currentPhaseTime.ToString();
                    });
                }
            }
        }
        private void EndSequence()
        {
            MessageBox.Show("1");
            try
            {
                string[] info = new string[] { "1", currentPhaseParameters[3] };

                // On arrête les timers (celle qui gère le temps de la séquence, la température du cold trap et celle qui gère la dispo de la pompe)
                sequenceTimer.Stop();
                tempControlTimer.Stop();
                pumpNotFreeTimer.Stop();

                // Peut-être pas, à voir
                while (sequenceTimer.Enabled) ;
                while (tempControlTimer.Enabled) ;
                while (pumpNotFreeTimer.Enabled) ;
                MessageBox.Show("2");

                General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { "Terminé" });
                thisCycleInfo.Add(info);

                if (!db.IsConnected()) db.Connect(); // Il va falloir supprimer ça

                db.SelectFromTemp("AVG(speed), AVG(pressure), STD(speed), STD(pressure)");
                string[] array = db.ReadNext();
                db.Close_reader();
                MessageBox.Show("3");

                db.Update_Row("cycle_speedmixer", 
                    new string[] { "date_time_end", "time_mix_eff", "speed_mean", "pressure_mean", "speed_std", "pressure_std" }, 
                    new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), TimeSpan.FromSeconds(currentSeqTime).ToString(), array[0], array[1], array[2], array[3] }, idSubCycle.ToString());

                if (areAlarmActive[0]) // Si l'alarme est toujours active alors on l'a désactive
                {
                    AlarmManagement.InactivateAlarm(3, 1); // Alarme température trop haute
                    areAlarmActive[0] = false;
                }

                if (!isCycleStopped && currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids et que le cycle n'est pas arrêté
                {
                    if (RS232Pump.IsOpen() && isPumpFree) RS232Pump.SetCommand("!C802 0");
                    RS232Pump.FreeUse();
                    isPumpFree = false;
                    mainFrame.Content = new Pages.SubCycle.CycleWeight(mainFrame, currentPhaseParameters[2], thisCycleInfo, idCycle, idSubCycle, "cycle_speedmixer");
                }
                else if (!isCycleStopped && currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer et que le cycle n'est pas arrêté
                {
                    RS232Pump.FreeUse();
                    isPumpFree = false;

                    mainFrame.Content = new Pages.SubCycle.CycleSpeedMixer(mainFrame, currentPhaseParameters[2], thisCycleInfo, idCycle, idSubCycle, "cycle_speedmixer");
                }
                else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "" || isCycleStopped) // Si c'est fini
                {
                    if (RS232Pump.IsOpen() && isPumpFree) RS232Pump.SetCommand("!C802 0");
                    RS232Pump.FreeUse();
                    isPumpFree = false;

                    string lastAlarmId = db.GetMax("audit_trail", "id").ToString();
                    string comment = isCycleStopped ? "Cycle interrompu" : "";
                    db.Update_Row("cycle", new string[] { "date_time_end_cycle", "last_alarm_id", "comment" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lastAlarmId, comment }, idCycle.ToString());

                    MessageBox.Show("C'est fini, merci d'être passé");
                    General.CurrentCycleInfo.InitializeSequenceNumber();
                    General.PrintReport(idCycle);

                    // On cache le panneau d'information
                    General.CurrentCycleInfo.SetVisibility(false);
                    mainFrame.Content = new Pages.Status();
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Je ne sais pas, je ne sais plus, je suis perdu");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - EndSequence - " + ex.Message);
            }
            finally
            {
                Dispose();
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            stopCycle();
        }
        private void thisFrame_ContentRendered(object sender, EventArgs e)
        {
            if (mainFrame.Content != this)
            {
                mainFrame.ContentRendered -= thisFrame_ContentRendered;
                if(!isSequenceOver) stopCycle();
            }

        } 
        private void stopCycle()
        {
            //
            // Fait quelque chose pour le rapport
            // 

            SpeedMixerModbus.StopProgram();
            isCycleStopped = true;
            if (!hasSequenceStarted) isSequenceOver = true; // Si la séquence n'a pas démarré on l'arrête
            // On attend que le cycle se termine, plutôt que de faire ça: isSequenceOver = true; // ou directement EndSequence(), à voir
        }
    }
}
