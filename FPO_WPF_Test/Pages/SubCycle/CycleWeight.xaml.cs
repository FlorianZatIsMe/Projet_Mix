﻿using Database;
using DRIVER.RS232.Weight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace FPO_WPF_Test.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour CycleWeight.xaml
    /// </summary>
    public partial class CycleWeight : Page, IDisposable
    {
        private Frame mainFrame;
        private Frame frameInfoCycle;
        private MyDatabase db = new MyDatabase();
        private readonly string[] currentPhaseParameters;
        private List<string[]> thisCycleInfo;
        private readonly decimal setpoint;
        private readonly decimal min;
        private readonly decimal max;
        private readonly string unit;
        private bool isScanningStep;
        private bool isSequenceOver;
        private Task taskGetWeight;
        private bool isBalanceFree;
        private bool isWeightCorrect;
        private bool disposedValue;

        public CycleWeight(Frame mainFrame_arg, Frame frameInfoCycle_arg, string id, List<string[]> cycleInfo)
        {
            mainFrame = mainFrame_arg;
            frameInfoCycle = frameInfoCycle_arg;
            mainFrame.ContentRendered += new EventHandler(thisFrame_ContentRendered);
            thisCycleInfo = cycleInfo;
            isSequenceOver = false;
            isWeightCorrect = false;
            General.CurrentCycleInfo.UpdateSequenceNumber();

            InitializeComponent();

            // On bloque l'utilisation de la balance par quelqu'un d'autre
            // On vérifie aussi que personne n'est en train d'utiliser la balance
            if (!RS232Weight.IsOpen()) RS232Weight.Open();

            if (RS232Weight.IsOpen())
            {
                if (RS232Weight.IsFree())
                {
                    RS232Weight.BlockUse();
                    isBalanceFree = true;
                }
                else
                {
                    isBalanceFree = false;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la balance déjà en cours");
                }
            }
            else
            {
                isBalanceFree = false;
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la balance impossible");
            }

            if (!db.IsConnected()) db.Connect();

            if (db.IsConnected()) // while loop is better
            {
                db.SendCommand_Read("recipe_weight", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                currentPhaseParameters = db.ReadNext(); // currentPhaseParameters = tous les paramètres de la séquence en cours

                if (currentPhaseParameters.Count() != 0 && db.ReadNext().Count() == 0) // Si la commande a renvoyée une ligne
                {
                    // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi

                    setpoint = decimal.Parse(currentPhaseParameters[8]) * decimal.Parse(thisCycleInfo[0][2]);
                    min = decimal.Parse(currentPhaseParameters[9]) * decimal.Parse(thisCycleInfo[0][2]);
                    max = decimal.Parse(currentPhaseParameters[10]) * decimal.Parse(thisCycleInfo[0][2]);

                    tbPhaseName.Text = currentPhaseParameters[3];
                    labelWeight.Text = "Masse (cible: " + setpoint.ToString("N" + currentPhaseParameters[7]) + currentPhaseParameters[6] + ")";
                    labelWeightLimits.Text = "[ " + min.ToString("N" + currentPhaseParameters[7]) + "; " + max.ToString("N" + currentPhaseParameters[7]) + " ]";
                    db.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                    if (currentPhaseParameters[4] == "True") // Si le contrôle du code barre est activé, on affiche les bonnes infos
                    {
                        isScanningStep = true;
                        tbScan.Focus();
                        labelScan.Text = "Veuillez scanner le produit " + currentPhaseParameters[3];
                    }
                    else if (currentPhaseParameters[4] == "False") // Si on ne contrôle pas le code barre, on affiche les bonnes infos
                    {
                        isScanningStep = false;
                        labelScan.Visibility = Visibility.Collapsed;
                        tbScan.Visibility = Visibility.Collapsed;
                        gridMain.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - valeur de currentPhaseParameters[4] incorrect");
                    }
                }
                else
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Méthode suivante ne renvoie pas une seule ligne: db.SendCommand_Read(recipe_weight, whereColumns: new string[] { id }, whereValues: new string[] { id })");
                }
                db.Disconnect();
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - La base de données n'est pas connecté");
                db.ConnectAsync();
            }

            if (isBalanceFree)
            {
                RS232Weight.SetCommand("SIR");
                taskGetWeight = Task.Factory.StartNew(() => getWeight());
            }


            /*
            if (!RS232Weight.IsOpen()) RS232Weight.Open();

            if (RS232Weight.IsOpen())
            {
                if (RS232Weight.IsFree())
                {
                    //MessageBox.Show("1");
                    RS232Weight.BlockUse();
                    isBalanceFree = true;
                    RS232Weight.SetCommand("SIR");
                    taskGetWeight = Task.Factory.StartNew(() => getWeight());
                }
                else
                {
                    isBalanceFree = false;
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la balance déjà en cours");
                }
            }
            else
            {
                isBalanceFree = false;
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Connexion avec la balance impossible");
            }*/
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                    if(taskGetWeight != null) taskGetWeight.Dispose();
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
        private async void getWeight()
        {
            if (isBalanceFree)
            {
                //MessageBox.Show("2");
                decimal currentWeight;
                while (!isSequenceOver)
                {
                    try
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            currentWeight = RS232Weight.GetWeight();
                            tbWeight.Text = currentWeight.ToString();
                            if(!RS232Weight.IsWeightStable())
                            { 
                                tbWeight.Background = Brushes.Red;
                                isWeightCorrect = false;
                            }
                            else if(currentWeight >= min && currentWeight <= max)
                            {
                                tbWeight.Background = Brushes.Green;
                                isWeightCorrect = true;
                            }
                            else
                            {
                                tbWeight.Background = Brushes.White;
                                isWeightCorrect = false;
                            }
                        });
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isBalanceFree)
                {
                    if (decimal.Parse(tbWeight.Text) >= min && decimal.Parse(tbWeight.Text) <= max)
                    {
                        isWeightCorrect = true;
                    }
                    else
                    {
                        isWeightCorrect = false;
                    }
                }

                if (isWeightCorrect)
                {
                    isSequenceOver = true;

                    if (isBalanceFree)
                    {
                        if (currentPhaseParameters[1] != "0") RS232Weight.SetCommand("@");
                        taskGetWeight.Wait();
                        RS232Weight.FreeUse();
                        isBalanceFree = false;
                    }

                    General.CurrentCycleInfo.UpdateCurrentWeightInfo(new string[] { tbWeight.Text });

                    string[] info = new string[] { "0", currentPhaseParameters[3], tbWeight.Text, min.ToString(), max.ToString() };
                    thisCycleInfo.Add(info);

                    if (currentPhaseParameters[1] == "0") // Si la première séquence est une séquence de poids
                    {
                        mainFrame.Content = new Pages.SubCycle.CycleWeight(mainFrame, frameInfoCycle, currentPhaseParameters[2], thisCycleInfo);
                    }
                    else if (currentPhaseParameters[1] == "1") // Si la première séquence est une séquence speedmixer
                    {
                        MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                        mainFrame.Content = new Pages.SubCycle.CycleSpeedMixer(mainFrame, frameInfoCycle, currentPhaseParameters[2], thisCycleInfo);
                    }
                    else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "") // Si la première séquence est une séquence speedmixer
                    {
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
                else
                {
                    MessageBox.Show("La masse du produit n'est pas correcte");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - " + ex.Message);
            }
            finally
            {
                Dispose(disposing: true);
            }
        }
        private async void tbScan_LostFocusAsync(object sender, RoutedEventArgs e)
        {
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
            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                if (currentPhaseParameters[5] == textbox.Text)
                {
                    isScanningStep = false;
                    labelScan.Visibility = Visibility.Collapsed;
                    gridMain.Visibility = Visibility.Visible;
                }
                else
                {
                    textbox.Text = "";
                    MessageBox.Show("Pas bien " + currentPhaseParameters[5]);
                }
            }
        }
        private void thisFrame_ContentRendered(object sender, EventArgs e)
        {
            if (mainFrame.Content != this)
            {
                mainFrame.ContentRendered -= thisFrame_ContentRendered;

                if (!isWeightCorrect)
                {
                    General.PrintReport(thisCycleInfo);

                    // On cache le panneau d'information
                    frameInfoCycle.Content = null;
                    frameInfoCycle.Visibility = Visibility.Collapsed;
                    mainFrame.Content = new Pages.Status();
                    Dispose(disposing: true);
                }
            }

        }
    }
}
