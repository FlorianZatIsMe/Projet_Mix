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

namespace FPO_WPF_Test.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour CycleSpeedMixer.xaml
    /// </summary>
    public partial class CycleSpeedMixer : Page
    {
        private Frame thisFrame;
        private MyDatabase db = new MyDatabase();
        private readonly string[] currentPhaseParameters;
        private List<string[]> thisCycleInfo;
        private bool sequenceHasStarted;
        private bool isSequenceOver;
        private bool isTempOK;
        private Task taskGetStatus;
        private System.Timers.Timer timer;
        private int currentSeqTimer;
        private int currentSeqNumber;

        public CycleSpeedMixer(Frame inputFrame, string id, List<string[]> cycleInfo)
        {
            thisFrame = inputFrame;
            thisFrame.ContentRendered += new EventHandler(thisFrame_ContentRendered);
            thisCycleInfo = cycleInfo;
            isSequenceOver = false;
            sequenceHasStarted = false;
            currentSeqNumber = 1;
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;

            InitializeComponent();

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_Read("recipe_speedmixer", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                currentPhaseParameters = db.ReadNext();

                if (currentPhaseParameters.Count() != 0 && db.ReadNext().Count() == 0)
                {
                    db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête
                    tbPhaseName.Text = currentPhaseParameters[3];

                    if (currentPhaseParameters[6] == "True") RS232Pump.SetCommand("!C802 1");   // Si on contrôle la pression, on démarre la pompe
                    else RS232Pump.SetCommand("!C802 0"); // Sinon on arrête la pompe

                    SpeedMixerModbus.SetProgram(currentPhaseParameters); // On met à jour tout les paramètres dans le speedmixer

                    isTempOK = currentPhaseParameters[11] != "True"; // Si on n'utilise pas le piège froid, on va zapper l'étape où l'on attend que la température du piège froid soit bonne
                    //if (isTempOK) RunProgram(); // On lance le speedmixer

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

        ~CycleSpeedMixer()
        {
            SpeedMixerModbus.Disconnect();
            timer.Stop();
            taskGetStatus.Dispose();  /* : 'Une tâche ne peut être supprimée que si elle est dans un état d'achèvement (RanToCompletion, Faulted ou Canceled).' */
            MessageBox.Show("Disconnection done");
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            currentSeqTimer--;

            if (currentSeqTimer >= 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    tbPhaseTime.Text = currentSeqTimer.ToString();
                });
            }
            else if (currentSeqTimer < -60)
            {
                MessageBox.Show("C'est normal que ça traîne comme ça ?");
            }

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
                if (isTempOK) // Si la température est bonne ou si on ne vérifie pas température alors on contrôle le cycle speedmixer
                {
                    status = SpeedMixerModbus.GetStatus();

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

                    if (!sequenceHasStarted && !status[5]) // Si on n'a pas encore démarré mais que le capot n'est pas fermé (Safety not OK), petit message à notre cher utilisateur
                    {
                        MessageBox.Show("Veuillez fermer le capot avant de démarrer le cycle");
                    }
                    else if (!sequenceHasStarted && status[5] && !status[1]) // Si on n'a pas encore démarré et que le capot est fermé alors on démarre le cycle
                    {
                        MessageBox.Show("Cliquez sur OK pour démarrer le speedmixer"); // Peut-être retirer ça s'il y a plusieurs cycle
                        SpeedMixerModbus.RunProgram();

                        this.Dispatcher.Invoke(() =>
                        {
                            tbPhaseNumber.Text = currentSeqNumber.ToString() + " - " + currentPhaseParameters[10 + 3 * currentSeqNumber] + "s";
                            currentSeqTimer = int.Parse(currentPhaseParameters[10 + 3 * currentSeqNumber]);
                            tbPhaseTime.Text = currentSeqTimer.ToString();
                        });

                        sequenceHasStarted = true; // le cycle a démarré
                        timer.Enabled = true;
                    }
                    else if (!sequenceHasStarted && status[1]) // Si on n'a pas encore démarré le cycle mais qu'il tourne
                    {
                    }
                    else if (sequenceHasStarted && !status[1] && status[7]) // si le cycle a démarré mais qu'il ne tourne plus
                    {
                        isSequenceOver = true; // le cycle est fini, du coup on arrête de le contrôler
                    }
                }
                else
                {
                    isTempOK = ColdTrap.IsTempOK();
                    this.Dispatcher.Invoke(() =>
                    {
                        tbReadyToRun.Text = isTempOK ? "Just like that" : "It's to hot in here";
                    });

                    //if(isTempOK) RunProgram();
                }

                await Task.Delay(500);
            }

            // un fois que le cycle est terminé, on commence la séquence final
            this.Dispatcher.Invoke(() =>
            {
                EndSequence();
            });
        }

        private void RunProgram()
        {
            MessageBox.Show("Cliquez sur OK pour démarrer le speedmixer"); // Peut-être retirer ça s'il y a plusieurs cycle
            SpeedMixerModbus.RunProgram();
        }

        private void EndSequence()
        {
            try
            {
                //string[] array;
                //SpeedMixerModbus.Disconnect();

                string[] info = new string[] { "1", currentPhaseParameters[3] };
                thisCycleInfo.Add(info);

                if (currentPhaseParameters[1] == "0") // Si la première séquence est une séquence de poids
                {
                    thisFrame.Content = new Pages.SubCycle.CycleWeight(thisFrame, currentPhaseParameters[2], thisCycleInfo);
                    RS232Pump.SetCommand("!C802 0");
                }
                else if (currentPhaseParameters[1] == "1") // Si la première séquence est une séquence speedmixer
                {
                    thisFrame.Content = new Pages.SubCycle.CycleSpeedMixer(thisFrame, currentPhaseParameters[2], thisCycleInfo);
                }
                else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "") // Si la première séquence est une séquence speedmixer
                {
                    RS232Pump.SetCommand("!C802 0");
                    MessageBox.Show("C'est fini, merci d'être passé");
                    General.PrintReport(thisCycleInfo);
                    thisFrame.Content = new Pages.Status();
                }
                else
                {
                    MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
                }


                /*
                if (!db.IsConnected()) db.Connect();

                if (db.IsConnected()) // while loop is better
                {
                    db.SendCommand_Read("recipe_speedmixer", whereColumns: new string[] { "id" }, whereValues: new string[] { currentPhaseParameters[0] });

                    array = db.ReadNext();

                    if (array.Count() != 0 && db.ReadNext().Count() == 0 && array[0] == currentPhaseParameters[0])
                    {
                        db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                        string[] info = new string[] { "1", array[3] };
                        thisCycleInfo.Add(info);

                        if (array[1] == "0") // Si la première séquence est une séquence de poids
                        {
                            thisFrame.Content = new Pages.SubCycle.CycleWeight(thisFrame, array[2], thisCycleInfo);
                            RS232Pump.SetCommand("!C802 0");
                        }
                        else if (array[1] == "1") // Si la première séquence est une séquence speedmixer
                        {
                            thisFrame.Content = new Pages.SubCycle.CycleSpeedMixer(thisFrame, array[2], thisCycleInfo);
                        }
                        else if (array[1] == null || array[1] == "") // Si la première séquence est une séquence speedmixer
                        {
                            RS232Pump.SetCommand("!C802 0");
                            MessageBox.Show("C'est fini, merci d'être passé");
                            General.PrintReport(thisCycleInfo);
                            thisFrame.Content = new Pages.Status();
                        }
                        else
                        {
                            MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Curieux, la prochaine séquence existe en plusieurs exemplaires");
                    }
                    db.Disconnect();
                }
                else
                {
                    MessageBox.Show("La base de données n'est pas connecté");
                    //isSequenceOver = false;
                    db.ConnectAsync();
                }*/
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isSequenceOver = true;
            //EndSequence();
        }
        private void thisFrame_ContentRendered(object sender, EventArgs e)
        {
            //SpeedMixerModbus.Disconnect();
            //MessageBox.Show("salut");
            
        } 
    }
}
