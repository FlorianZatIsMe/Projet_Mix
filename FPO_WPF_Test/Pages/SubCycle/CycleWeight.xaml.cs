using Database;
using DRIVER_RS232_Weight;
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
    public partial class CycleWeight : Page, IDisposable, ISubCycle
    {
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private readonly int idCycle;
        private readonly int idPrevious;
        private readonly bool isTest;
        private readonly string tablePrevious;
        private ISeqInfo prevSeqInfo;
        private readonly int idSubCycle;
        //private MyDatabase db = new MyDatabase();
        //private readonly string[] currentPhaseParameters;
        private readonly RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();
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

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //public CycleWeight(Frame frameMain_arg, Frame frameInfoCycle_arg, string id_arg, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqInfo prevSeqInfo_arg, bool isTest_arg = true)
        public CycleWeight(SubCycleArg subCycleArg)
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
            /*
            frameMain = frameMain_arg;
            frameInfoCycle = frameInfoCycle_arg;
            string id = id_arg;
            idCycle = idCycle_arg;
            idPrevious = idPrevious_arg;
            tablePrevious = tablePrevious_arg;
            prevSeqInfo = prevSeqInfo_arg;
            isTest = isTest_arg;
            */
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
                recipeWeightInfo = (RecipeWeightInfo)MyDatabase.GetOneRow(recipeWeightInfo.GetType(), id);
                //currentPhaseParameters = MyDatabase.GetOneRow("recipe_weight", whereColumns: new string[] { "id" }, whereValues: new string[] { id });

                if (recipeWeightInfo.columns.Count() != 0) // Si la commande a renvoyée une ligne
                //if (currentPhaseParameters.Count() != 0) // Si la commande a renvoyée une ligne
                {
                    // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi
                    CycleTableInfo cycleTableInfo = (CycleTableInfo)MyDatabase.GetOneRow(new CycleTableInfo().GetType(), idCycle.ToString());
                    setpoint = decimal.Parse(recipeWeightInfo.columns[8].value) * decimal.Parse(cycleTableInfo.columns[cycleTableInfo.quantityValue].value);
                    min = decimal.Parse(recipeWeightInfo.columns[9].value) * decimal.Parse(cycleTableInfo.columns[cycleTableInfo.quantityValue].value);
                    max = decimal.Parse(recipeWeightInfo.columns[10].value) * decimal.Parse(cycleTableInfo.columns[cycleTableInfo.quantityValue].value);

                    //string[] infoPreCycle = MyDatabase.GetOneRow("cycle", selectColumns: "quantity_value, quantity_unit", whereColumns: new string[] { "id" }, whereValues: new string[] { idCycle.ToString() });
                    //setpoint = decimal.Parse(recipeWeightInfo.columns[8].value) * decimal.Parse(infoPreCycle[0]);
                    //min = decimal.Parse(recipeWeightInfo.columns[9].value) * decimal.Parse(infoPreCycle[0]);
                    //max = decimal.Parse(recipeWeightInfo.columns[10].value) * decimal.Parse(infoPreCycle[0]);

                    tbPhaseName.Text = recipeWeightInfo.columns[3].value;
                    labelWeight.Text = "Masse (cible: " + setpoint.ToString("N" + recipeWeightInfo.columns[7].value) + recipeWeightInfo.columns[6].value + ")";
                    labelWeightLimits.Text = "[ " + min.ToString("N" + recipeWeightInfo.columns[7].value) + "; " + max.ToString("N" + recipeWeightInfo.columns[7].value) + " ]";

                    //tbPhaseName.Text = currentPhaseParameters[3];
                    //labelWeight.Text = "Masse (cible: " + setpoint.ToString("N" + currentPhaseParameters[7]) + currentPhaseParameters[6] + ")";
                    //labelWeightLimits.Text = "[ " + min.ToString("N" + currentPhaseParameters[7]) + "; " + max.ToString("N" + currentPhaseParameters[7]) + " ]";
                    MyDatabase.Close_reader(); // On ferme le reader de la db pour pouvoir lancer une autre requête

                    CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
                    cycleWeightInfo.SetRecipeParameters(recipeWeightInfo);
                    MyDatabase.InsertRow(cycleWeightInfo);
                    /*
                    string columns = "product, setpoint, minimum, maximum, unit, decimal_number";
                    string[] values = new string[] { currentPhaseParameters[3], setpoint.ToString(), min.ToString(), max.ToString(), currentPhaseParameters[6], currentPhaseParameters[7] };
                    MyDatabase.InsertRow_done_old("cycle_weight", columns, values);
                    */
                    idSubCycle = MyDatabase.GetMax_old(cycleWeightInfo.name, cycleWeightInfo.columns[cycleWeightInfo.id].id);

                    prevSeqInfo.columns[prevSeqInfo.nextSeqType].value = cycleWeightInfo.seqType.ToString();
                    prevSeqInfo.columns[prevSeqInfo.nextSeqId].value = idSubCycle.ToString();
                    MyDatabase.Update_Row(prevSeqInfo, idPrevious.ToString());

                    //MyDatabase.Update_Row(tablePrevious, new string[] { "next_seq_type", "next_seq_id" }, new string[] { "0", idSubCycle.ToString() }, idPrevious.ToString());

                    if (recipeWeightInfo.columns[4].value == "True") // Si le contrôle du code barre est activé, on affiche les bonnes infos
                    //if (currentPhaseParameters[4] == "True") // Si le contrôle du code barre est activé, on affiche les bonnes infos
                    {
                        isScanningStep = true;
                        tbScan.Focus();
                        labelScan.Text = "Veuillez scanner le produit " + recipeWeightInfo.columns[3].value;
                        //labelScan.Text = "Veuillez scanner le produit " + currentPhaseParameters[3];
                    }
                    else if (recipeWeightInfo.columns[4].value == "False") // Si on ne contrôle pas le code barre, on affiche les bonnes infos
                    //else if (currentPhaseParameters[4] == "False") // Si on ne contrôle pas le code barre, on affiche les bonnes infos
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
                    if (RS232Weight.rs232.IsOpen() && RS232Weight.rs232.IsFree())
                    {
                        RS232Weight.rs232.BlockUse();
                        RS232Weight.rs232.SetCommand("SIR");
                        wasBalanceFreeOnce = true;
                    }
                }
                else
                {
                    isBalanceFree = RS232Weight.rs232.IsOpen();

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
                    if (recipeWeightInfo.columns[1].value != "0") RS232Weight.rs232.SetCommand("@");
                    //if (currentPhaseParameters[1] != "0") RS232Weight.rs232.SetCommand("@");
                    taskGetWeight.Wait();
                    RS232Weight.rs232.FreeUse();
                    isBalanceFree = false;
                }

                General.CurrentCycleInfo.UpdateCurrentWeightInfo(new string[] { tbWeight.Text });

                CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
                cycleWeightInfo.columns[cycleWeightInfo.wasWeightManual].value = isBalanceFree ? "0" : "1";
                cycleWeightInfo.columns[cycleWeightInfo.dateTime].value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cycleWeightInfo.columns[cycleWeightInfo.actualValue].value = tbWeight.Text;
                MyDatabase.Update_Row(cycleWeightInfo, idSubCycle.ToString());
                //MyDatabase.Update_Row("cycle_weight", new string[] { "was_weight_manual", "date_time", "actual_value" }, 
                //    new string[] { isBalanceFree ? "0" : "1", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), tbWeight.Text }, idSubCycle.ToString());

                if (recipeWeightInfo.columns[1].value == "1") // Si la prochaine séquence est une séquence speedmixer
                //if (currentPhaseParameters[1] == "1") // Si la prochaine séquence est une séquence speedmixer
                {
                    MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                }

                General.NextSequence(new string[] { "currentPhaseParameters" }, recipeWeightInfo, frameMain, frameInfoCycle, idCycle, idSubCycle, 0, new CycleWeightInfo(), isTest);
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
                if (recipeWeightInfo.columns[5].value == textbox.Text)
                //if (currentPhaseParameters[5] == textbox.Text)
                {
                    isScanningStep = false;
                    labelScan.Visibility = Visibility.Collapsed;
                    tbScan.Visibility = Visibility.Collapsed;
                    gridMain.Visibility = Visibility.Visible;
                }
                else
                {
                    textbox.Text = "";
                    MessageBox.Show("Pas bien " + recipeWeightInfo.columns[5].value);
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

                    General.EndSequence(recipeParameters: new string[] { "currentPhaseParameters" }, recipeWeightInfo, frameMain: frameMain, frameInfoCycle: frameInfoCycle, idCycle: idCycle, previousSeqType: 0, previousSeqId: idSubCycle.ToString(), isTest: isTest, comment: "Cycle interrompu");

                    Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
                }
            }

        }

        public ISubCycle NewInstance(SubCycleArg subCycleArg)
        {
            return new CycleWeight(subCycleArg);
        }
    }
}
