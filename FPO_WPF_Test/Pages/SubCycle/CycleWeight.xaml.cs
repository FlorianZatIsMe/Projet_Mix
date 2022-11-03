using Database;
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
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private readonly int idCycle;
        private readonly int idPrevious;
        private readonly bool isTest;
        private readonly string tablePrevious;
        private readonly int idSubCycle;
        //private MyDatabase db = new MyDatabase();
        private readonly string[] currentPhaseParameters;
        //private List<string[]> thisCycleInfo;
        private readonly decimal setpoint;
        private readonly decimal min;
        private readonly decimal max;
        //private readonly string unit; to use later maybe
        private bool isScanningStep;
        private bool isSequenceOver;
        private readonly Task taskGetWeight;
        private bool isBalanceFree;
        private bool wasBalanceFreeOnce;
        private bool isWeightCorrect;
        private bool disposedValue;

        public CycleWeight(Frame frameMain_arg, Frame frameInfoCycle_arg, string id, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, bool isTest_arg = true)
        {
            frameMain = frameMain_arg;
            frameInfoCycle = frameInfoCycle_arg;
            idCycle = idCycle_arg;
            idPrevious = idPrevious_arg;
            tablePrevious = tablePrevious_arg;
            isTest = isTest_arg;
            frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);
            //thisCycleInfo = cycleInfo;
            isSequenceOver = false;
            isWeightCorrect = false;
            wasBalanceFreeOnce = false;
            General.CurrentCycleInfo.UpdateSequenceNumber();

            InitializeComponent();

            //if (!MyDatabase.IsConnected()) MyDatabase.Connect();

            if (MyDatabase.IsConnected()) // while loop is better
            {
                currentPhaseParameters = MyDatabase.GetOneRow("recipe_weight", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                if (currentPhaseParameters.Count() != 0) // Si la commande a renvoyée une ligne
                {
                    // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi
                    string[] infoPreCycle = MyDatabase.GetOneRow("cycle", selectColumns: "quantity_value, quantity_unit", whereColumns: new string[] { "id" }, whereValues: new string[] { idCycle.ToString() });

                    setpoint = decimal.Parse(currentPhaseParameters[8]) * decimal.Parse(infoPreCycle[0]);
                    min = decimal.Parse(currentPhaseParameters[9]) * decimal.Parse(infoPreCycle[0]);
                    max = decimal.Parse(currentPhaseParameters[10]) * decimal.Parse(infoPreCycle[0]);

                    tbPhaseName.Text = currentPhaseParameters[3];
                    labelWeight.Text = "Masse (cible: " + setpoint.ToString("N" + currentPhaseParameters[7]) + currentPhaseParameters[6] + ")";
                    labelWeightLimits.Text = "[ " + min.ToString("N" + currentPhaseParameters[7]) + "; " + max.ToString("N" + currentPhaseParameters[7]) + " ]";
                    MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                    string columns = "product, setpoint, minimum, maximum, unit, decimal_number";
                    string[] values = new string[] { currentPhaseParameters[3], setpoint.ToString(), min.ToString(), max.ToString(), currentPhaseParameters[6], currentPhaseParameters[7] };

                    MyDatabase.InsertRow_old("cycle_weight", columns, values);
                    idSubCycle = MyDatabase.GetMax("cycle_weight", "id");

                    MyDatabase.Update_Row(tablePrevious, new string[] { "next_seq_type", "next_seq_id" }, new string[] { "0", idSubCycle.ToString() }, idPrevious.ToString());

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
                    MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Méthode suivante ne renvoie pas une seule ligne: MyDatabase.SendCommand_Read(recipe_weight, whereColumns: new string[] { id }, whereValues: new string[] { id })");
                }
                //MyDatabase.Disconnect();
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - La base de données n'est pas connecté");
                MyDatabase.ConnectAsync();
            }

            taskGetWeight = Task.Factory.StartNew(() => GetWeight());
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
        private async void GetWeight()
        {
            decimal currentWeight;

            while (!isSequenceOver)
            {
                if (!wasBalanceFreeOnce)
                {
                    if (RS232Weight.IsOpen() && RS232Weight.IsFree())
                    {
                        RS232Weight.BlockUse();
                        RS232Weight.SetCommand("SIR");
                        wasBalanceFreeOnce = true;
                    }
                }
                else
                {
                    isBalanceFree = RS232Weight.IsOpen();

                    if (isBalanceFree)
                    {
                        //MessageBox.Show("2");
                        try
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                currentWeight = RS232Weight.GetWeight();
                                tbWeight.Text = currentWeight.ToString();
                                if (!RS232Weight.IsWeightStable())
                                {
                                    tbWeight.Background = Brushes.Red;
                                    isWeightCorrect = false;
                                }
                                else if (currentWeight >= min && currentWeight <= max)
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
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }

                await Task.Delay(500);
            }



        }
        private void Button_Click(object sender, RoutedEventArgs e)
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

                MyDatabase.Update_Row("cycle_weight", new string[] { "was_weight_manual", "date_time", "actual_value" }, new string[] { isBalanceFree ? "0" : "1", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), tbWeight.Text }, idSubCycle.ToString());

                if (currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer
                {
                    MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                }

                General.NextSequence(currentPhaseParameters, frameMain, frameInfoCycle, idCycle, idSubCycle, 0, isTest);

                /*
                if (currentPhaseParameters[1] == "0") // Si la prochaine séquence est une séquence de poids
                {
                    frameMain.Content = new Pages.SubCycle.CycleWeight(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, "cycle_weight", isTest);
                }
                else if (currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer
                {
                    MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                    frameMain.Content = new Pages.SubCycle.CycleSpeedMixer(frameMain, frameInfoCycle, currentPhaseParameters[2], idCycle, idSubCycle, "cycle_weight", isTest);
                }
                else if (currentPhaseParameters[1] == null || currentPhaseParameters[1] == "") // S'il n'y a pas de prochaine séquence 
                {
                    string lastAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();
                    MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "last_alarm_id" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lastAlarmId }, idCycle.ToString());

                    MessageBox.Show("C'est fini, merci d'être passé");
                    General.CurrentCycleInfo.StopSequence();
                    General.PrintReport(idCycle);
                    MessageBox.Show("Rapport généré");

                    // On cache le panneau d'information
                    General.CurrentCycleInfo.SetVisibility(false);

                    if (isTest)
                    {
                        string[] recipeName = MyDatabase.GetOneRow("cycle", "recipe_name", new string[] { "id" }, new string[] { idCycle.ToString() });
                        frameMain.Content = new Pages.Recipe(Action.Modify, frameMain, frameInfoCycle, recipeName.Length == 0 ? "" : recipeName[0]);
                    }
                    else
                    {
                        frameMain.Content = new Pages.Status();
                    }
                }
                else
                {
                    MessageBox.Show("Je ne sais pas, je ne sais plus, je suis perdu");
                }
                */
            }
            else
            {
                MessageBox.Show("La masse du produit n'est pas correcte");
            }

            Dispose(disposing: true);
        }
        private async void TbScan_LostFocusAsync(object sender, RoutedEventArgs e)
        {
            if (isScanningStep)
            {
                TextBox textbox = sender as TextBox;

                await Task.Delay(500);
                textbox.Text = "";
                textbox.Focus();
            }
        }
        private void TbScan_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                if (currentPhaseParameters[5] == textbox.Text)
                {
                    isScanningStep = false;
                    labelScan.Visibility = Visibility.Collapsed;
                    tbScan.Visibility = Visibility.Collapsed;
                    gridMain.Visibility = Visibility.Visible;
                }
                else
                {
                    textbox.Text = "";
                    MessageBox.Show("Pas bien " + currentPhaseParameters[5]);
                }
            }
        }
        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            if (frameMain.Content != this)
            {
                if (!isWeightCorrect)
                {
                    frameMain.ContentRendered -= FrameMain_ContentRendered;

                    General.EndSequence(recipeParameters: currentPhaseParameters, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: 0, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: "Cycle interrompu");

                    /*
                    string[] recipeParameters = currentPhaseParameters;
                    int previousSeqType = 0;
                    string previousSeqId = idSubCycle.ToString();
                    string comment = "Cycle interrompu";

                    MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "comment" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), comment}, idCycle.ToString());

                    string nextSeqId;
                    int nextRecipeSeqType;
                    bool isNextRcpSeqTpOK;

                    string[] tableNameSubCycles = new string[] { "cycle_weight", "cycle_speedmixer" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
                    string[] tableNameSubRecipes = new string[] { "recipe_weight", "recipe_speedmixer" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
                    string[] columnNamesSubCycles = new string[] { "product, setpoint, minimum, maximum, unit, decimal_number", "time_mix_th, pressure_unit, speed_min, speed_max, pressure_min, pressure_max" }; // Pas bien ça, il faut faire référence au fichier de config et même ça devrait une constante globale. Non ?
                    string[] valuesSubCycle = new string[0];
                    int timeTh_seconds = 0;
                    //TimeSpan timeTh;
                    int i;

                    // On boucle tant qu'on est pas arrivé au bout de la recette
                    while (recipeParameters[1] != "" && recipeParameters[1] != null)
                    {
                        nextRecipeSeqType = int.Parse(recipeParameters[1]);
                        recipeParameters = MyDatabase.GetOneRow(tableNameSubRecipes[int.Parse(recipeParameters[1])], whereColumns: new string[] { "id" }, whereValues: new string[] { recipeParameters[2] });

                        // Si la prochaine étape est un pesage on prépare les données à mettre dans la table cycle_weight (valuesSubCycle)
                        if (nextRecipeSeqType == 0)
                        {
                            valuesSubCycle = new string[] { recipeParameters[3], recipeParameters[8], recipeParameters[9], recipeParameters[10], recipeParameters[6], recipeParameters[7] };
                            isNextRcpSeqTpOK = true;
                        }
                        // Sinon si c'est un mix on prépare les données à mettre dans la table cycle_speedmixer (valuesSubCycle)
                        else if (nextRecipeSeqType == 1)
                        {
                            i = 0;
                            while (i != 10 && recipeParameters[12 + 3 * i] != "")
                            {
                                timeTh_seconds += int.Parse(recipeParameters[13 + 3 * i]);
                                i++;
                            }

                            valuesSubCycle = new string[] { TimeSpan.FromSeconds(timeTh_seconds).ToString(), recipeParameters[9], recipeParameters[42], recipeParameters[43], recipeParameters[44], recipeParameters[45] };
                            isNextRcpSeqTpOK = true;
                            timeTh_seconds = 0;
                        }
                        // Sinon il y a un problème
                        else
                        {
                            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - Mais non, tu déconnes !");
                            isNextRcpSeqTpOK = false;
                        }

                        if (isNextRcpSeqTpOK) // S'il n'y a pas eu de problème
                        {
                            // On insert les infos de recettes dans la bonne table
                            MyDatabase.InsertRow(tableNameSubCycles[nextRecipeSeqType],
                                columnNamesSubCycles[nextRecipeSeqType],
                                valuesSubCycle);

                            // On met à jour les infos "type" et "id" de la séquence qu'on vient de renseigner dans la précédente séquence
                            nextSeqId = MyDatabase.GetMax(tableNameSubCycles[nextRecipeSeqType], "id").ToString();
                            MyDatabase.Update_Row(tableNameSubCycles[previousSeqType],
                                new string[] { "next_seq_type", "next_seq_id" },
                                new string[] { nextRecipeSeqType.ToString(), nextSeqId }, previousSeqId);

                            // La dernière séquence devient l'ancienne
                            previousSeqType = nextRecipeSeqType;
                            previousSeqId = nextSeqId;
                        }
                    }

                    string lastAlarmId = MyDatabase.GetMax("audit_trail", "id").ToString();
                    MyDatabase.Update_Row("cycle", new string[] { "date_time_end_cycle", "last_alarm_id" }, new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), lastAlarmId }, idCycle.ToString());

                    MessageBox.Show("C'est fini, merci d'être passé");
                    General.CurrentCycleInfo.StopSequence();
                    General.PrintReport(idCycle);
                    MessageBox.Show("Rapport généré");

                    // On cache le panneau d'information
                    General.CurrentCycleInfo.SetVisibility(false);

                    if (isTest)
                    {
                        string[] recipeName = MyDatabase.GetOneRow("cycle", "recipe_name", new string[] { "id" }, new string[] { idCycle.ToString() });
                        frameMain.Content = new Pages.Recipe(Action.Modify, frameMain, frameInfoCycle, recipeName.Length == 0 ? "" : recipeName[0]);
                    }
                    else
                    {
                        frameMain.Content = new Pages.Status();
                    }*/

                    Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
                }
            }

        }
    }
}
