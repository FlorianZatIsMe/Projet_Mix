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
    /// <summary>
    /// Logique d'interaction pour CycleSpeedMixer.xaml
    /// </summary>
    public partial class CycleSpeedMixer : Page, IDisposable
    {
        private Frame mainFrame;
        private Frame frameInfoCycle;
        private MyDatabase db = new MyDatabase();
        private readonly string[] currentPhaseParameters;
        private List<string[]> thisCycleInfo;
        private bool sequenceHasStarted;
        private bool isSequenceOver;
        private bool isTempOK;
        private Task taskGetStatus;
        private int timeGetStatus = 500;
        private System.Timers.Timer sequenceTimer;
        private System.Timers.Timer pumpNotFreeTimer;
        private System.Timers.Timer tempTooHotTimer;
        private int currentSeqTimer;
        private int currentSeqNumber;
        private bool isPumpFree;
        private int pumpNotFreeSince;
        private int tempTooHotSince;
        private bool isCycleStopped;
        private bool disposedValue;
        private readonly int timeoutPumpNotFree = 30; // 30s, si la pompe n'est pas disponible pendant ce temps, on arrête la séquence
        private readonly int timeoutTempTooHot = 20; // 30s, si la pompe n'est pas disponible pendant ce temps, on arrête la séquence
        private static int nAlarms = 1;
        private static bool[] areAlarmActive = new bool[nAlarms];

        public CycleSpeedMixer(Frame mainFrame_arg, Frame frameInfoCycle_arg, string id, List<string[]> cycleInfo)
        {
            mainFrame = mainFrame_arg;
            frameInfoCycle = frameInfoCycle_arg;
            mainFrame.ContentRendered += new EventHandler(thisFrame_ContentRendered);
            thisCycleInfo = cycleInfo;
            isSequenceOver = false;
            sequenceHasStarted = false;
            currentSeqNumber = 1;
            isCycleStopped = false;

            sequenceTimer = new System.Timers.Timer();
            sequenceTimer.Interval = 1000;
            sequenceTimer.Elapsed += seqTimer_OnTimedEvent;
            sequenceTimer.AutoReset = true;

            pumpNotFreeTimer = new System.Timers.Timer();
            pumpNotFreeTimer.Interval = 1000;
            pumpNotFreeTimer.Elapsed += pumpNotFreeTimer_OnTimedEvent;
            pumpNotFreeTimer.AutoReset = true;

            tempTooHotTimer = new System.Timers.Timer();
            tempTooHotTimer.Interval = 1000;
            tempTooHotTimer.Elapsed += tempTooHotTimer_OnTimedEvent;
            tempTooHotTimer.AutoReset = true;

            isPumpFree = false;
            pumpNotFreeSince = 0;

            General.CurrentCycleInfo.UpdateSequenceNumber();

            InitializeComponent();

            General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { "En cours" });

            // On bloque l'utilisation de la pompe par quelqu'un d'autre
            // On vérifie aussi que personne n'est en train d'utiliser la pompe
            if (RS232Pump.IsOpen() && RS232Pump.IsFree())
            {
                RS232Pump.BlockUse();
                isPumpFree = true;
            }
            else
            {
                isPumpFree = false;
            }

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_Read("recipe_speedmixer", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                currentPhaseParameters = db.ReadNext();

                if (currentPhaseParameters.Count() != 0 && db.ReadNext().Count() == 0)
                {
                    db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                    tbPhaseName.Text = currentPhaseParameters[3];

                    // Pump initializsation
                    if (isPumpFree)
                    {
                        if (currentPhaseParameters[6] == "True") RS232Pump.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe
                        else RS232Pump.SetCommand("!C802 0"); // Sinon on arrête la pompe 
                    }
                    else if (RS232Pump.IsOpen())
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la pompe déjà en cours, tu vois");
                    }
                    else
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la pompe impossible");
                    }

                    SpeedMixerModbus.SetProgram(currentPhaseParameters); // On met à jour tout les paramètres dans le speedmixer

                    isTempOK = currentPhaseParameters[11] != "True"; // Si on n'utilise pas le piège froid, on va zapper l'étape où l'on attend que la température du piège froid soit bonne

                    taskGetStatus = Task.Factory.StartNew(() => getStatus()); // On lance la tâche de vérification du status et d'autre choses sûrement
                }
                db.Disconnect();
            }
            else
            {
                MessageBox.Show("La base de données n'est pas connecté");
                db.ConnectAsync();
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                }

                // TODO: libérer les ressources non managées (objets non managés) et substituer le finaliseur
                // TODO: affecter aux grands champs une valeur null
                disposedValue = true;
                if (taskGetStatus != null) taskGetStatus.Dispose();
                if (sequenceTimer != null) sequenceTimer.Dispose();
                if (pumpNotFreeTimer != null) pumpNotFreeTimer.Dispose();
                if (tempTooHotTimer != null) tempTooHotTimer.Dispose();
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
            MessageBox.Show("Disconnection done");
        }
        private void pumpNotFreeTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            pumpNotFreeSince++;

            //
            // Il faudrait montrer la valeur du timer et un message qui informe l'utilisateur de la déconnexion de la pompe
            // 

            // On vérifie si tout va bien en fait
            if (RS232Pump.IsOpen() && (RS232Pump.IsFree() || sequenceHasStarted || isSequenceOver))
            {
                RS232Pump.BlockUse();
                isPumpFree = true;
                pumpNotFreeSince = 0;
                pumpNotFreeTimer.AutoReset = false;
                pumpNotFreeTimer.Enabled = false;
            }

            if (pumpNotFreeSince > timeoutPumpNotFree)
            {
                pumpNotFreeTimer.AutoReset = false; // c'est un peu sale tout ça !
                pumpNotFreeTimer.Stop();

                if (sequenceHasStarted)
                {
                    MessageBox.Show("TIMEOUT !!! Le cycle est en cours, je ne l'arrête pas, ALARME (WARNING) !");
                    
                }
                else
                {
                    MessageBox.Show("TIMEOUT !!! Il faut arrêter le cycle maintenant, ALARME !!!");
                    stopCycle();
                    isSequenceOver = true;
                }

                pumpNotFreeTimer.Enabled = false;
            }
        }
        private void tempTooHotTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            tempTooHotSince++;

            //
            // Il faudrait montrer la valeur du timer et un message qui informe l'utilisateur de la déconnexion de la pompe
            // 

            // On vérifie si tout va bien en fait
            if (isTempOK)
            {
                tempTooHotSince = 0;
                tempTooHotTimer.Stop();
            }

            if (tempTooHotSince > timeoutTempTooHot)
            {
                tempTooHotTimer.Stop();

                MessageBox.Show("TIMEOUT !!! C'est fini, il fait beaucoup trop chaud, génération d'alarme");
                stopCycle();
                if (!sequenceHasStarted) isSequenceOver = true;
            }

            if (sequenceHasStarted)
            {
                if (!isTempOK && !areAlarmActive[0])
                {
                    areAlarmActive[0] = true;
                    MessageBox.Show(currentPhaseParameters[11]);
                    AlarmManagement.NewAlarm(AlarmManagement.alarms[3, 1]); // Warning température trop haute
                }
                else if (isTempOK && areAlarmActive[0])
                {
                    areAlarmActive[0] = false;
                    AlarmManagement.InactivateAlarm(AlarmManagement.alarms[3, 1]); // Warning température trop haute
                }
            }


        }
        private void seqTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            currentSeqTimer--;

            if (currentSeqTimer >= 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    tbPhaseTime.Text = currentSeqTimer.ToString();
                });
            }
            else if (currentSeqTimer == -60)
            {
                //sequenceTimer.Enabled = false;
                MessageBox.Show("C'est normal que ça traîne comme ça ? Attention je vais arrêter le timer");
                stopCycle();
            }
            else if (currentSeqTimer == -90)
            {
                MessageBox.Show("C'est fini, il faut se faire une raison");
                isSequenceOver = true;
            }

                // on devrait commencer le timeout quand le robot est à home
                // Penser à ajouter des tests : vérifier que la vitesse et la pression du speedmixer est à peu près celle du paramètre

                if (currentSeqTimer <= 0)
            {
                if (currentPhaseParameters[10 + 3 * (currentSeqNumber+1)] != null  && currentPhaseParameters[10 + 3 * (currentSeqNumber+1)] != "")
                {
                    currentSeqNumber++;
                    currentSeqTimer = int.Parse(currentPhaseParameters[10 + 3 * currentSeqNumber]);

                    this.Dispatcher.Invoke(() =>
                    {
                        tbPhaseNumber.Text = currentSeqNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentSeqNumber] + "s";
                        tbPhaseTime.Text = currentSeqTimer.ToString();
                    });
                }
            }
        }
        private async void getStatus()
        {
            bool[] status = new bool[8];

            while (!isSequenceOver) // tant que le cycle est en cours
            {
                status = SpeedMixerModbus.GetStatus();

                //                if (true) // Si la température est bonne ou si on ne vérifie pas température alors on contrôle le cycle speedmixer
                //                {
                // Contrôler la température
                if (currentPhaseParameters[11] == "True")
                {
                    isTempOK = temperatureControl();
                } 

                    // A voir si on supprimer la ligne au dessus de if (isTempOK)
                    //status = SpeedMixerModbus.GetStatus();

                    this.Dispatcher.Invoke(() =>
                    {
                        tbReadyToRun.Text = status[0] ? "Ready to run " + sequenceHasStarted.ToString() : "Not ready to run";
                        tbMixerRunning.Text = status[1] ? "Mixer running" : "Mixer not running";
                        tbMixerError.Text = status[2] ? "Mixer Error" : "No error";
                        tbLidOpen.Text = status[3] ? "Lid Open" : "Lid not open";
                        tbLidClosed.Text = status[4] ? "Lid closed" : "Lid not closed";
                        tbSafetyOK.Text = status[5] ? "Safety OK" : "Safety not OK";
                        tbRobotAtHome.Text = status[7] ? "Robot at home" : "Robot not at home";
                        tbPressure.Text = "Pression : " + (SpeedMixerModbus.GetPressure() / 10).ToString();
                        tbSpeed.Text = "Vitesse : " + (SpeedMixerModbus.GetSpeed()).ToString();
                    });

                    // Si on n'a pas encore démarré mais que le capot n'est pas fermé (Safety not OK)
                    if (!sequenceHasStarted && !status[5]) 
                    {
                        MessageBox.Show("Veuillez fermer le capot avant de démarrer le cycle");
                    }
                    // Si on n'a pas encore démarré et que le capot est fermé alors on démarre le cycle
                    else if (!sequenceHasStarted && status[5] && !status[1]) 
                    {
                        if (currentPhaseParameters[11] == "False" || isTempOK)
                        {
                            if (isPumpFree || currentPhaseParameters[6] == "False") // Si la pompe est dispo ou qu'on en a pas besoin, on démarre le cycle
                            {
                                // Pump initializsation
                                if (isPumpFree)
                                {
                                    if (currentPhaseParameters[6] == "True") RS232Pump.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe
                                    else RS232Pump.SetCommand("!C802 0"); // Sinon on arrête la pompe 
                                }

                                MessageBox.Show("Cliquez sur OK pour démarrer le speedmixer"); // Peut-être retirer ça s'il y a plusieurs cycle
                                SpeedMixerModbus.RunProgram();

                                this.Dispatcher.Invoke(() =>
                                {
                                    tbPhaseNumber.Text = currentSeqNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentSeqNumber] + "s";
                                    currentSeqTimer = int.Parse(currentPhaseParameters[10 + 3 * currentSeqNumber]);
                                    tbPhaseTime.Text = currentSeqTimer.ToString();
                                });

                                sequenceHasStarted = true; // le programme a démarré
                                sequenceTimer.Enabled = true;
                            }
                            else if (!isPumpFree && currentPhaseParameters[6] == "True") // Si la pompe n'est pas dispo mais qu'on en a besoin, on démarre le timer
                            {
                                if (!pumpNotFreeTimer.Enabled && pumpNotFreeSince == 0)
                                {
                                    pumpNotFreeTimer.AutoReset = true;
                                    pumpNotFreeTimer.Enabled = true;
                                    //MessageBox.Show("Démarrage du timer: LA POMPE N'EST PAS DISPO, VITE ! RENDS LA DISPONIBLE OU LE CYCLE VA S'ARRETER POUR TOUJOURS !!!");
                                }
                            }
                            else
                            {
                                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Je ne comprends pas comment c'est possible");
                            }
                        }
                    }
                    // Si le programme est en cours
                    else if (sequenceHasStarted && status[1]) 
                    {
                        if (currentPhaseParameters[6] == "True" && !RS232Pump.IsOpen() && !pumpNotFreeTimer.Enabled && pumpNotFreeSince == 0)
                        {
                            isPumpFree = false;
                            pumpNotFreeTimer.AutoReset = true;
                            pumpNotFreeTimer.Enabled = true;
                            // Il faut vite fermer cette message box sinon le cycle va s'arrêter, 
                            MessageBox.Show("Démarrage du timer_2: LA POMPE N'EST PAS DISPO, VITE ! RENDS LA DISPONIBLE OU LE CYCLE VA S'ARRETER POUR TOUJOURS !!!");
                        }
                        else if (pumpNotFreeSince != 0 && RS232Pump.IsOpen())
                        {
                            pumpNotFreeSince = 0;
                        }
                    }
                    // si le cycle a démarré mais qu'il ne tourne plus (si c'est la fin du programme Speedmixer)
                    else if (sequenceHasStarted && !status[1] && status[7])
                    {
                        isSequenceOver = true; // le cycle est fini, du coup on arrête de le contrôler
                    }
/*                }
                else if(!sequenceHasStarted)
                {
                    isTempOK = temperatureControl();
                    /*
                    isTempOK = ColdTrap.IsTempOK();

                    this.Dispatcher.Invoke(() =>
                    {
                        tbTemperatureOK.Text = isTempOK ? "Bonne" : "Trop chaude";
                    });

                    if (!tempTooHotTimer.Enabled && tempTooHotSince == 0)
                    {
                        tempTooHotTimer.AutoReset = true;
                        tempTooHotTimer.Enabled = true;
                    }*//*
                }*/

                await Task.Delay(timeGetStatus);
                //MessageBox.Show("GO");
            }

            // un fois que le cycle est terminé, on commence la séquence final
            this.Dispatcher.Invoke(() =>
            {
                EndSequence();
            });
        }
        private bool temperatureControl()
        {
            bool check = ColdTrap.IsTempOK();

            this.Dispatcher.Invoke(() =>
            {
                tbTemperatureOK.Text = isTempOK ? "Bonne" : "Trop chaude";
            });

            if (!check && !tempTooHotTimer.Enabled && tempTooHotSince == 0)
            {
                //MessageBox.Show("On démarre le timer");
                tempTooHotTimer.AutoReset = true;
                tempTooHotTimer.Enabled = true;
            }

            return check;
        }
        private void EndSequence()
        {
            try
            {
                string[] info = new string[] { "1", currentPhaseParameters[3] };

                // On arrête les 2 timers (celle qui gère le temps de la séquence et celle qui gère la dispo de la pompe)
                pumpNotFreeTimer.Stop();
                sequenceTimer.Stop();

                // Peut-être pas, à voir
                while (pumpNotFreeTimer.Enabled) ;
                while (sequenceTimer.Enabled) ;

                General.CurrentCycleInfo.UpdateCurrentSpeedMixerInfo(new string[] { "Terminé" });
                thisCycleInfo.Add(info);

                if (areAlarmActive[0]) // Si l'alarme est toujours active alors on l'a désactive
                {
                    //MessageBox.Show("INIBITION");
                    AlarmManagement.InactivateAlarm(AlarmManagement.alarms[3, 1]); // Alarme température trop haute
                    areAlarmActive[0] = false;
                }

                if (!isCycleStopped && currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids et que le cycle n'est pas arrêté
                {
                    if (RS232Pump.IsOpen())
                    {
                        RS232Pump.BlockUse();
                        RS232Pump.SetCommand("!C802 0");
                        isPumpFree = false;
                    }
                    RS232Pump.FreeUse();
                    mainFrame.Content = new Pages.SubCycle.CycleWeight(mainFrame, frameInfoCycle, currentPhaseParameters[2], thisCycleInfo);
                }
                else if (!isCycleStopped && currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer et que le cycle n'est pas arrêté
                {
                    RS232Pump.FreeUse();
                    isPumpFree = false;

                    mainFrame.Content = new Pages.SubCycle.CycleSpeedMixer(mainFrame, frameInfoCycle, currentPhaseParameters[2], thisCycleInfo);
                }
                else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "" || isCycleStopped) // Si c'est fini
                {
                    if (RS232Pump.IsOpen())
                    {
                        RS232Pump.BlockUse();
                        RS232Pump.SetCommand("!C802 0");
                        isPumpFree = false;
                    }
                    RS232Pump.FreeUse();


                    MessageBox.Show("C'est fini, merci d'être passé");
                    General.PrintReport(thisCycleInfo);

                    // On cache le panneau d'information
                    frameInfoCycle.Content = null;
                    frameInfoCycle.Visibility = Visibility.Collapsed;
                    mainFrame.Content = new Pages.Status();
                }
                else
                {
                    MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            if (!sequenceHasStarted) isSequenceOver = true; // Si la séquence n'a pas démarré on l'arrête
            // On attend que le cycle se termine, plutôt que de faire ça: isSequenceOver = true; // ou directement EndSequence(), à voir
        }
    }
}
