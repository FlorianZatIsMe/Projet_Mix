using Main;
using MixingApplication.Properties;
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

namespace MixingApplication.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour WeightBowl.xaml
    /// </summary>
    public partial class WeightBowl : Page
    {
        private CycleStartInfo info;
        private Timer getWeightTimer;
        //private Task getWeightTask;
        private bool isgetWeightTaskActive = false;
        private Weight weight;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string tareOnGoing = "Tare en cours...";
        private int timerInterval = 50;
        public WeightBowl(CycleStartInfo info_arg)
        {
            info = info_arg;
            info.frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            //*
            getWeightTimer = new Timer
            {
                AutoReset = true,
                Interval = timerInterval
            };
            getWeightTimer.Elapsed += GetWeightTimer_Elapsed;
            //*/

            InitializeComponent();
            labelMessage.Text = tareOnGoing;
        }
        /*
        private async void GetWeight()
        {
            while(isgetWeightTaskActive)
            {
                weight = Balance.GetOneWeight();

                this.Dispatcher.Invoke(() => {
                    if (weight == null)
                    {
                        labelWeight.Text = "#";
                    }
                    else
                    {
                        labelWeight.Text = weight.value + "g";
                    }
                });
                await Task.Delay(200);
            }
        }*/
        
        private void GetWeightTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            weight = Balance.GetOneWeight();

            this.Dispatcher.Invoke(() => {
                if (weight == null)
                {
                    labelWeight.Text = "#";
                    logger.Error(labelWeight.Text + " #");
                }
                else
                {
                    labelWeight.Text = weight.value + "g";
                    logger.Error(labelWeight.Text + weight.value);
                }
            });
        }

        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            if (info.frameMain.Content == this)
            {
                General.ShowMessageBox("Veuillez retirer tout object sur la balance. Cliquer sur le bouton une fois fait");
                bool exeTare = true;

                while (exeTare)
                {
                    int tare = Balance.TareBalance();
                    logger.Fatal("salut toi : " + tare.ToString());
                    if (tare == 0)
                    {
                        labelMessage.Text = "Veuiller placer le contenant vide sur la balance puis appuyer sur le bonton";
                        // démarrage du timer qui lit en continue la valeur du poids et l'affiche
                        getWeightTimer.Start();
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
                        }
                        logger.Error("Tare de la balance échouée, tare: " + Balance.TareBalance().ToString());
                    }
                }

                info.frameMain.ContentRendered -= FrameMain_ContentRendered;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            bool keepWaiting = true;
            int waitingCounter = 0;
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
                    }
                }
            }

            getWeightTimer.Stop();

            //isgetWeightTaskActive = false;
            //getWeightTask.Wait(1000);
            /*
            if(!getWeightTask.IsCompleted)
            {
                General.ShowMessageBox("La balance ne répond plus");
                logger.Error("La balance ne répond plus");
                return;
            }*/
            if (!keepWaiting) return;

            // Le poids est stable, du coup on le stock dans info et on lance la séquence
            decimal bowlWeight = weight.value;

            info.bowlWeight = bowlWeight.ToString();
            General.StartCycle(info);
        }

        private void Stop()
        {
            if (info.isTest)
            {
                // A CORRIGER : IF RESULT IS FALSE
                Task<object> task = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeInfo), info.recipeID); });
                RecipeInfo recipeInfo = (RecipeInfo)task.Result;
                info.frameMain.Content = new Recipe(RcpAction.Modify, info.frameMain, info.frameInfoCycle, recipeInfo.Columns.Count == 0 ? "" : recipeInfo.Columns[recipeInfo.Name].Value);
            }
            else
            {
                info.frameMain.Content = new Status();
            }
        }
    }
}
