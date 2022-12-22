using Database;
using DRIVER_RS232_Weight;
using FPO_WPF_Test.Properties;
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
        private readonly SubCycleArg subCycle;
        /*
        private readonly Frame frameMain;
        private readonly Frame frameInfoCycle;
        private readonly int idCycle;
        private readonly int idPrevious;
        private readonly bool isTest;
        private readonly string tablePrevious;
        private ISeqInfo prevSeqInfo;
        */
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
        //private readonly Task taskGetWeight;
        private readonly System.Timers.Timer getWeightTimer;
        private bool isBalanceFree = false;
        private bool wasBalanceFreeOnce;
        private bool isWeightCorrect;
        private bool disposedValue;
        private decimal currentWeight;
        private readonly CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();

        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //public CycleWeight(Frame frameMain_arg, Frame frameInfoCycle_arg, string id_arg, int idCycle_arg, int idPrevious_arg, string tablePrevious_arg, ISeqInfo prevSeqInfo_arg, bool isTest_arg = true)
        public CycleWeight(SubCycleArg subCycleArg)
        {
            logger.Debug("Start");
            Task<object> t;

            subCycle = subCycleArg;
            subCycle.frameMain.ContentRendered += new EventHandler(FrameMain_ContentRendered);

            isSequenceOver = false;
            isWeightCorrect = false;
            wasBalanceFreeOnce = false;
            General.CurrentCycleInfo.UpdateSequenceNumber();

            // Initialisation des timers
            getWeightTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleWeight_getWeightTimer_Interval,
                AutoReset = false
            };
            getWeightTimer.Elapsed += GetWeightTimer_OnTimedEvent;

            InitializeComponent();
            /*
            if (!MyDatabase.IsConnected()) // while loop is better
            {
                logger.Error(DatabaseSettings.Error01);
                MessageBox.Show(DatabaseSettings.Error01);
                return; // ou exit carrément
            }*/

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(RecipeWeightInfo), subCycle.id); });
            recipeWeightInfo = (RecipeWeightInfo)t.Result;
            //recipeWeightInfo = (RecipeWeightInfo)MyDatabase.GetOneRow(typeof(RecipeWeightInfo), subCycle.id);

            if (recipeWeightInfo == null) // Si la commande a renvoyée une ligne
            {
                logger.Error(Settings.Default.CycleWeight_Error_NoRecipe);
                MessageBox.Show(Settings.Default.CycleWeight_Error_NoRecipe);
                return; // ou exit carrément
            }

            cycleWeightInfo.SetRecipeParameters(recipeWeightInfo, subCycle.idCycle);

            // Refaire ces calculs plus sérieusement une fois que tout est clarifié, il faut arrondir aussi

            tbPhaseName.Text = recipeWeightInfo.Columns[3].Value;
            labelWeight.Text = Settings.Default.CycleWeight_WeightField + " (" + Settings.Default.CycleWeight_SetpointField + ": " + cycleWeightInfo.Columns[cycleWeightInfo.Setpoint].Value + Settings.Default.CycleFinalWeight_g_Unit + ")";
            labelWeightLimits.Text = "[ " + cycleWeightInfo.Columns[cycleWeightInfo.Min].Value + "; " + cycleWeightInfo.Columns[cycleWeightInfo.Max].Value + " ]";

            // if setpoint, min, max are incorrect
            if (false)
            {
                MessageBox.Show("Message d'erreur quelconque");
                logger.Error("Message d'erreur quelconque");
                General.EndSequence(recipeWeightInfo, frameMain: subCycle.frameMain, frameInfoCycle: subCycle.frameInfoCycle, idCycle: subCycle.idCycle, previousSeqType: this.cycleWeightInfo.SeqType, previousSeqId: idSubCycle.ToString(), isTest: subCycle.isTest, comment: Settings.Default.Report_Comment_CycleAborted);
                Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
                return;
            }

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.InsertRow(cycleWeightInfo); });
            //MyDatabase.InsertRow(cycleWInfo);
            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetMax(cycleWeightInfo.TabName, cycleWeightInfo.Columns[cycleWeightInfo.Id].Id); });
            idSubCycle = (int)t.Result;
            //idSubCycle = MyDatabase.GetMax(cycleWInfo.name, cycleWInfo.columns[cycleWInfo.id].id);

            subCycle.prevSeqInfo.Columns[subCycle.prevSeqInfo.NextSeqType].Value = cycleWeightInfo.SeqType.ToString();
            subCycle.prevSeqInfo.Columns[subCycle.prevSeqInfo.NextSeqId].Value = idSubCycle.ToString();

            // A CORRIGER : IF RESULT IS FALSE
            t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(subCycle.prevSeqInfo, subCycle.idPrevious.ToString()); });
            //MyDatabase.Update_Row(subCycle.prevSeqInfo, subCycle.idPrevious.ToString());

            if (recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Value == DatabaseSettings.General_TrueValue_Read) // Si le contrôle du code barre est activé, on affiche les bonnes infos
            {
                isScanningStep = true;
                tbScan.Focus();
                labelScan.Text = Settings.Default.CycleWeight_Request_ScanProduct + " " + recipeWeightInfo.Columns[3].Value;
            }
            else if (recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Value == DatabaseSettings.General_FalseValue_Read) // Si on ne contrôle pas le code barre, on affiche les bonnes infos
            {
                isScanningStep = false;
                labelScan.Visibility = Visibility.Collapsed;
                tbScan.Visibility = Visibility.Collapsed;
                gridMain.Visibility = Visibility.Visible;
            }
            else
            {
                logger.Error(Settings.Default.CycleWeight_Error_IncorrectValue + recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Id + " (" + recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Value + ")");
                MessageBox.Show(Settings.Default.CycleWeight_Error_IncorrectValue + recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Id + " (" + recipeWeightInfo.Columns[recipeWeightInfo.IsBarcodeUsed].Value + ")");
            }

            getWeightTimer.Start();
            //taskGetWeight = Task.Factory.StartNew(() => GetWeight());
        }
        protected virtual void Dispose(bool disposing)
        {
            logger.Debug("Dispose(bool disposing)");

            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                    if (getWeightTimer != null) getWeightTimer.Dispose();
                    //if (taskGetWeight != null) taskGetWeight.Dispose();
                }

                // TODO: libérer les ressources non managées (objets non managés) et substituer le finaliseur
                // TODO: affecter aux grands champs une valeur null
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            logger.Debug("Dispose");

            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void GetWeightTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            //logger.Debug("GetWeightTimer_OnTimedEvent");

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

            this.Dispatcher.Invoke(() =>
            {
                tbWeight.IsEnabled = Settings.Default.CycleWeight_isManualAllowed && !isBalanceFree;

                if (!Settings.Default.CycleWeight_isManualAllowed && !isBalanceFree)
                {
                    tbWeight.Text = "###";
                }
            });

            if(!isSequenceOver) getWeightTimer.Enabled = true;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("Button_Click");
            Task<object> t;

            if (!isBalanceFree)
            {
                try
                {
                    isWeightCorrect = 
                        decimal.Parse(tbWeight.Text) >= decimal.Parse(cycleWeightInfo.Columns[cycleWeightInfo.Min].Value) && 
                        decimal.Parse(tbWeight.Text) <= decimal.Parse(cycleWeightInfo.Columns[cycleWeightInfo.Max].Value);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    isWeightCorrect = false;
                }

            }

            if (!isWeightCorrect)
            {
                MessageBox.Show(Settings.Default.CycleWeight_IncorrectWeight);
            }
            else
            {
                isSequenceOver = true;

                if (isBalanceFree)
                {
                    if (recipeWeightInfo.Columns[recipeWeightInfo.NextSeqType].Value != recipeWeightInfo.SeqType.ToString()) 
                        RS232Weight.rs232.SetCommand("@");

                    //taskGetWeight.Wait();
                    RS232Weight.rs232.FreeUse();
                    isBalanceFree = false;
                }

                General.CurrentCycleInfo.UpdateCurrentWeightInfo(new string[] { tbWeight.Text });

                CycleWeightInfo cycleWInfo = new CycleWeightInfo();
                cycleWInfo.Columns[cycleWInfo.WasWeightManual].Value = isBalanceFree ? DatabaseSettings.General_FalseValue_Write : DatabaseSettings.General_TrueValue_Write;
                cycleWInfo.Columns[cycleWInfo.DateTime].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cycleWInfo.Columns[cycleWInfo.WeightedValue].Value = tbWeight.Text;

                // A CORRIGER : IF RESULT IS FALSE
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.Update_Row(cycleWInfo, idSubCycle.ToString()); });
                //MyDatabase.Update_Row(cycleWeightInfo, idSubCycle.ToString());
                /*
                if (recipeWeightInfo.columns[1].value == "1") // Si la prochaine séquence est une séquence speedmixer
                {
                    MessageBox.Show("Mettez le produit dans le speedmixer et fermer le capot");
                }
                */

                SubCycleArg sub= new SubCycleArg(subCycle.frameMain, subCycle.frameInfoCycle, recipeWeightInfo.Columns[recipeWeightInfo.NextSeqId].Value, subCycle.idCycle, idSubCycle, recipeWeightInfo.TabName, new CycleWeightInfo(), subCycle.isTest);

                General.NextSequence(recipeWeightInfo, subCycle.frameMain, subCycle.frameInfoCycle, subCycle.idCycle, idSubCycle, 0, new CycleWeightInfo(), subCycle.isTest);
            }

            Dispose(disposing: true);
        }
        private async void TbScan_LostFocusAsync(object sender, RoutedEventArgs e)
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
        private void TbScan_KeyDown(object sender, KeyEventArgs e)
        {
            logger.Debug("TbScan_KeyDown");

            TextBox textbox = sender as TextBox;

            if (e.Key == Key.Enter)
            {
                if (recipeWeightInfo.Columns[recipeWeightInfo.Barcode].Value == textbox.Text)
                {
                    isScanningStep = false;
                    labelScan.Visibility = Visibility.Collapsed;
                    tbScan.Visibility = Visibility.Collapsed;
                    gridMain.Visibility = Visibility.Visible;
                }
                else
                {
                    textbox.Text = "";
                    MessageBox.Show(Settings.Default.CycleWeigh_IncorrectBarcode + " " + recipeWeightInfo.Columns[recipeWeightInfo.Barcode].Value);
                }
            }
        }
        private void FrameMain_ContentRendered(object sender, EventArgs e)
        {
            logger.Debug("FrameMain_ContentRendered");

            if (subCycle.frameMain.Content != this)
            {
                if (!isWeightCorrect)
                {
                    subCycle.frameMain.ContentRendered -= FrameMain_ContentRendered;

                    General.EndSequence(recipeWeightInfo, frameMain: subCycle.frameMain, frameInfoCycle: subCycle.frameInfoCycle, idCycle: subCycle.idCycle, previousSeqType: cycleWeightInfo.SeqType, previousSeqId: idSubCycle.ToString(), isTest: subCycle.isTest, comment: Settings.Default.Report_Comment_CycleAborted);

                    Dispose(disposing: true); // Il va peut-être falloir sortir ça du "if"
                }
            }

        }
    }
}
