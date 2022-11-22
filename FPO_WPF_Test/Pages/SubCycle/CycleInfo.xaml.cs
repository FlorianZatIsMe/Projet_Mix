using Alarm_Management;
using Database;
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
using static Alarm_Management.AlarmManagement;

namespace FPO_WPF_Test.Pages.SubCycle
{
    /// <summary>
    /// Logique d'interaction pour CycleInfo.xaml
    /// </summary>
    public partial class CycleInfo : Page // Les pages ne peuvent pas être static
    {
        private readonly List<WrapPanel> wrapPanels = new List<WrapPanel>();
        private static int seqNumber;
        private readonly static List<Tuple<int, int>> activeAlarms = new List<Tuple<int, int>>();
        private System.Timers.Timer checkAlarmsTimer;
        //private Task taskCheckAlarm;
        //private MyDatabase db = new MyDatabase();
        private readonly Frame frameCycleInfo;
        private bool isCheckAlarms_onGoing = true;

        private AuditTrailInfo auditTrailInfo = new AuditTrailInfo();

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public CycleInfo(CycleTableInfo cycleTableInfo, Frame frame)
        {
            seqNumber = -1;

            // Initialisation des timers
            checkAlarmsTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.CycleInfo_checkAlarmsTimer_Interval,
                AutoReset = false
            };
            checkAlarmsTimer.Elapsed += ScanConnectTimer_OnTimedEvent;

            frameCycleInfo = frame;

            InitializeComponent();
            /*
            if (info.Length != 4)
            {
                logger.Error(Settings.Default.CycleInfo_Error01);
                MessageBox.Show(Settings.Default.CycleInfo_Error01);
                return;
            }*/

            labelOFNumber.Text = cycleTableInfo.columns[cycleTableInfo.batchNumber].value;
            labelRecipeName.Text = cycleTableInfo.columns[cycleTableInfo.recipeName].value;
            labelRecipeVersion.Text = cycleTableInfo.columns[cycleTableInfo.recipeVersion].value;
            labelFinalWeight.Text = cycleTableInfo.columns[cycleTableInfo.quantityValue].value;

            SetVisibility(true);
        }
        public void SetVisibility(bool visibility) 
        {
            if (visibility)
            {
                frameCycleInfo.Content = this;
                frameCycleInfo.Visibility = Visibility.Visible;
            }
            else
            {
                frameCycleInfo.Content = null;
                frameCycleInfo.Visibility = Visibility.Collapsed;
            }
        }

        /* checkAlarmsTimer_OnTimedEvent
         * 
         * Description: affiche dans le panneau d'informations tous les évènements d'alarmes (ACTIVE, ACK, INACTIVE et RAZ)
         * 
         * Version: 1.0
         */
        private void ScanConnectTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // S'il y a un évènement d'alarme qui n'a pas été affiché...
            if (!activeAlarms.SequenceEqual(AlarmManagement.ActiveAlarms))
            {
                // On parcours toutes les alarmes actives
                for (int i = 0; i < AlarmManagement.ActiveAlarms.Count; i++)
                {
                    // Si l'alarme active qu'on regarde n'a pas été affichée...
                    if (!activeAlarms.Contains(AlarmManagement.ActiveAlarms[i]))
                    {
                        // On met dans la variable array, l'enregistrement d'audit trail de l'alarme en question
                        auditTrailInfo = new AuditTrailInfo();
                        auditTrailInfo = (AuditTrailInfo)MyDatabase.GetOneRow(auditTrailInfo.GetType(), AlarmManagement.alarms[AlarmManagement.ActiveAlarms[i].Item1, AlarmManagement.ActiveAlarms[i].Item2].id.ToString());

                        // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                        if (auditTrailInfo.columns.Count() != 0)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                AddRow(auditTrailInfo.columns[auditTrailInfo.dateTime].value + " - " +
                                    auditTrailInfo.columns[auditTrailInfo.description].value + " - " +
                                    auditTrailInfo.columns[auditTrailInfo.valueAfter].value);
                            });
                        }
                        else
                        {
                            logger.Error(Settings.Default.CycleInfo_Error02);
                            MessageBox.Show(Settings.Default.CycleInfo_Error02);
                        }
                    }
                }

                // activeAlarms = AlarmManagement.activeAlarms mais en plus chiant
                activeAlarms.Clear();
                for (int i = 0; i < AlarmManagement.ActiveAlarms.Count; i++) activeAlarms.Add(AlarmManagement.ActiveAlarms[i]);
            }

            // S'il y a une Remise A Zéro d'une alarme qui n'a pas été affichée...
            if (AlarmManagement.RAZalarms.Count > 0)
            {
                // On parcours toutes les alarmes RAZ
                for (int i = 0; i < AlarmManagement.RAZalarms.Count; i++)
                {
                    // On met dans la variable array l'enregistrement d'audit trail de l'alarme
                    auditTrailInfo = new AuditTrailInfo();
                    auditTrailInfo = (AuditTrailInfo)MyDatabase.GetOneRow(auditTrailInfo.GetType(), AlarmManagement.RAZalarms[i].ToString());

                    // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                    if (auditTrailInfo.columns.Count() != 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            AddRow(auditTrailInfo.columns[auditTrailInfo.dateTime].value + " - " +
                                auditTrailInfo.columns[auditTrailInfo.description].value + " - " +
                                auditTrailInfo.columns[auditTrailInfo.valueAfter].value);
                        });
                    }
                    else
                    {
                        logger.Error(Settings.Default.CycleInfo_Error02);
                        MessageBox.Show(Settings.Default.CycleInfo_Error02);
                    }
                }
                AlarmManagement.RAZalarms.Clear();
            }
            checkAlarmsTimer.Enabled = true;
        }
        public void NewInfo(ISeqInfo cycleSeqInfo)
        {
            if (cycleSeqInfo.GetType().Equals(typeof(RecipeWeightInfo)))
            {
                NewInfo(cycleSeqInfo as RecipeWeightInfo);
            }
            else if (cycleSeqInfo.GetType().Equals(typeof(RecipeSpeedMixerInfo)))
            {
                NewInfo(cycleSeqInfo as RecipeSpeedMixerInfo);
            }
            else
            {
                logger.Error(Settings.Default.CycleInfo_Error03);
                MessageBox.Show(Settings.Default.CycleInfo_Error03);
            }
        }
        public void NewInfo(RecipeWeightInfo recipeWeightInfo)
        {
            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock productName = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Text = cycleWeightInfo.columns[cycleWeightInfo.product].displayName + ": " + 
                recipeWeightInfo.columns[recipeWeightInfo.seqName].value
            };

            TextBlock min = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.columns[cycleWeightInfo.min].displayName + ": " + 
                Math.Round(decimal.Parse(recipeWeightInfo.columns[recipeWeightInfo.min].value), 
                int.Parse(recipeWeightInfo.columns[recipeWeightInfo.decimalNumber].value))
                .ToString("N" + recipeWeightInfo.columns[recipeWeightInfo.decimalNumber].value).ToString()
            };

            TextBlock max = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.columns[cycleWeightInfo.max].displayName + ": " +
                Math.Round(decimal.Parse(recipeWeightInfo.columns[recipeWeightInfo.max].value),
                int.Parse(recipeWeightInfo.columns[recipeWeightInfo.decimalNumber].value))
                .ToString("N" + recipeWeightInfo.columns[recipeWeightInfo.decimalNumber].value).ToString()
            };

            TextBlock actualWeight = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.columns[cycleWeightInfo.actualValue].displayName + ": -"
            };

            wrapPanel.Children.Add(productName);
            wrapPanel.Children.Add(min);
            wrapPanel.Children.Add(max);
            wrapPanel.Children.Add(actualWeight);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void NewInfo(RecipeSpeedMixerInfo recipeSpeedMixerInfo)
        {
            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            // General.CurrentCycleInfo.NewInfoSpeedMixer(new string[] { array[3] });

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock programName = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Text = cycleSpeedMixerInfo.columns[cycleSpeedMixerInfo.mixName].displayName + ": " + recipeSpeedMixerInfo.columns[recipeSpeedMixerInfo.seqName].value
            };

            TextBlock status = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = Settings.Default.CycleInfo_Mix_StatusField + ": " + Settings.Default.CycleInfo_Mix_StatusWaiting
            };

            wrapPanel.Children.Add(programName);
            wrapPanel.Children.Add(status);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void UpdateCurrentWeightInfo(string[] info)
        {
            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            (wrapPanels[seqNumber].Children[3] as TextBlock).Text = 
                cycleWeightInfo.columns[cycleWeightInfo.actualValue].displayName + ": " + 
                info[0];
        }
        public void UpdateCurrentSpeedMixerInfo(string[] info)
        {
            (wrapPanels[seqNumber].Children[1] as TextBlock).Text = Settings.Default.CycleInfo_Mix_StatusField + ": " + info[0];
        }
        public void AddRow(string rowText)
        {
            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock actualWeight = new TextBlock
            {
                Foreground = Brushes.Wheat,
                //            actualWeight.Margin = new Thickness(0, 10, 0, 0);
                Text = rowText
            };

            wrapPanel.Children.Add(actualWeight);
            StackMain.Children.Add(wrapPanel);
        }
        public void UpdateSequenceNumber()
        {
            // Il sert à quelque chose ce code ?
            if (wrapPanels == null) wrapPanels.Clear();

            seqNumber++;
            if (seqNumber == 0) checkAlarmsTimer.Start();
        }
        public void InitializeSequenceNumber()
        {
            seqNumber = -1;
            activeAlarms.Clear();
            AlarmManagement.RAZalarms.Clear();
        }
        public void StopSequence()
        {
            isCheckAlarms_onGoing = false;
            checkAlarmsTimer.Stop();
            InitializeSequenceNumber();
        }
    }
}
