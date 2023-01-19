using Alarm_Management;
using Database;
using MixingApplication.Properties;
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

namespace Main.Pages.SubCycle
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
        //private bool isCheckAlarms_onGoing = true;

        private AuditTrailInfo ATInfo = new AuditTrailInfo();
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private int firstAlarmId;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public CycleInfo(CycleTableInfo cycleTableInfo, Frame frame)
        {
            logger.Debug("Start");

            seqNumber = -1;
            firstAlarmId = AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id - 1;

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
                General.ShowMessageBox(Settings.Default.CycleInfo_Error01);
                return;
            }*/

            labelOFNumber.Text = cycleTableInfo.Columns[cycleTableInfo.BatchNumber].Value;
            labelRecipeName.Text = cycleTableInfo.Columns[cycleTableInfo.RecipeName].Value;
            labelRecipeVersion.Text = cycleTableInfo.Columns[cycleTableInfo.RecipeVersion].Value;
            labelFinalWeight.Text = cycleTableInfo.Columns[cycleTableInfo.FinalWeight].Value;

            SetVisibility(true);
        }
        public void SetVisibility(bool visibility)
        {
            logger.Debug("SetVisibility");

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

        private void ScanConnectTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            int lastAlarmId;
            Task<object> t;
            List<AuditTrailInfo> auditTrailInfos = new List<AuditTrailInfo>();

            t = MyDatabase.TaskEnQueue(() => { return 
                MyDatabase.GetMax(auditTrailInfo, auditTrailInfo.Columns[auditTrailInfo.Id].Id); });
            lastAlarmId = (int)t.Result;

            if (firstAlarmId != lastAlarmId)
            {
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetAlarms(firstAlarmId + 1, lastAlarmId, true); });
                auditTrailInfos = (List<AuditTrailInfo>)t.Result;
                firstAlarmId = lastAlarmId;

                foreach (AuditTrailInfo info in auditTrailInfos)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        AddRow(info.Columns[info.DateTime].Value + " - " +
                            info.Columns[info.Description].Value + " - " +
                            info.Columns[info.ValueAfter].Value);
                    });
                }
            }
            checkAlarmsTimer.Enabled = true;
        }

        /* checkAlarmsTimer_OnTimedEvent
         * 
         * Description: affiche dans le panneau d'informations tous les évènements d'alarmes (ACTIVE, ACK, INACTIVE et RAZ)
         * 
         * Version: 1.0
         
        private void ScanConnectTimer_OnTimedEvent_old(Object source, System.Timers.ElapsedEventArgs e)
        {
            //logger.Debug("ScanConnectTimer_OnTimedEvent");
            Task<object> t;

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
                        ATInfo = new AuditTrailInfo();
                        // A CORRIGER : IF RESULT IS FALSE
                        t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(AuditTrailInfo), AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[i].Item1, AlarmManagement.ActiveAlarms[i].Item2].id.ToString()); });
                        ATInfo = (AuditTrailInfo)t.Result;
                        //auditTrailInfo = (AuditTrailInfo)MyDatabase.GetOneRow(typeof(AuditTrailInfo), AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[i].Item1, AlarmManagement.ActiveAlarms[i].Item2].id.ToString());

                        // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                        if (ATInfo.Columns.Count() != 0)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                AddRow(ATInfo.Columns[ATInfo.DateTime].Value + " - " +
                                    ATInfo.Columns[ATInfo.Description].Value + " - " +
                                    ATInfo.Columns[ATInfo.ValueAfter].Value);
                            });
                        }
                        else
                        {
                            logger.Error(Settings.Default.CycleInfo_Error02);
                            General.ShowMessageBox(Settings.Default.CycleInfo_Error02);
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
                    ATInfo = new AuditTrailInfo();
                    // A CORRIGER : IF RESULT IS FALSE
                    t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow(typeof(AuditTrailInfo), AlarmManagement.RAZalarms[i].ToString()); });
                    ATInfo = (AuditTrailInfo)t.Result;
                    //auditTrailInfo = (AuditTrailInfo)MyDatabase.GetOneRow(typeof(AuditTrailInfo), AlarmManagement.RAZalarms[i].ToString());

                    // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                    if (ATInfo.Columns.Count() != 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            AddRow(ATInfo.Columns[ATInfo.DateTime].Value + " - " +
                                ATInfo.Columns[ATInfo.Description].Value + " - " +
                                ATInfo.Columns[ATInfo.ValueAfter].Value);
                        });
                    }
                    else
                    {
                        logger.Error(Settings.Default.CycleInfo_Error02);
                        General.ShowMessageBox(Settings.Default.CycleInfo_Error02);
                    }
                }
                AlarmManagement.RAZalarms.Clear();
            }
            checkAlarmsTimer.Enabled = true;
        }*/
        public void NewInfo(ISeqTabInfo cycleSeqInfo, decimal finalWeight = -1)
        {
            logger.Debug("NewInfo(ISeqInfo cycleSeqInfo)");

            if (cycleSeqInfo.GetType().Equals(typeof(RecipeWeightInfo)))
            {
                NewInfo(cycleSeqInfo as RecipeWeightInfo, finalWeight);
            }
            else if (cycleSeqInfo.GetType().Equals(typeof(RecipeSpeedMixerInfo)))
            {
                NewInfo(cycleSeqInfo as RecipeSpeedMixerInfo);
            }
            else
            {
                logger.Error(Settings.Default.CycleInfo_Error03);
                General.ShowMessageBox(Settings.Default.CycleInfo_Error03);
            }
        }
        public void NewInfo(RecipeWeightInfo recipeWeightInfo, decimal finalWeight)
        {
            logger.Debug("NewInfo(RecipeWeightInfo recipeWeightInfo)");

            if (finalWeight == -1)
            {
                logger.Error("Pas bien");
                General.ShowMessageBox("Pas bien");
            }

            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock productName = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Text = cycleWeightInfo.Columns[cycleWeightInfo.Product].DisplayName + ": " + 
                recipeWeightInfo.Columns[recipeWeightInfo.Name].Value
            };

            TextBlock min = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Columns[cycleWeightInfo.Min].DisplayName + ": " + 
                Math.Round(cycleWeightInfo.GetMin(recipeWeightInfo, finalWeight), 
                int.Parse(recipeWeightInfo.Columns[recipeWeightInfo.DecimalNumber].Value))
                .ToString("N" + recipeWeightInfo.Columns[recipeWeightInfo.DecimalNumber].Value).ToString()
            };

            TextBlock max = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Columns[cycleWeightInfo.Max].DisplayName + ": " +
                Math.Round(cycleWeightInfo.GetMax(recipeWeightInfo, finalWeight),
                int.Parse(recipeWeightInfo.Columns[recipeWeightInfo.DecimalNumber].Value))
                .ToString("N" + recipeWeightInfo.Columns[recipeWeightInfo.DecimalNumber].Value).ToString()
            };

            TextBlock actualWeight = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Columns[cycleWeightInfo.WeightedValue].DisplayName + ": -"
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
            logger.Debug("NewInfo(RecipeSpeedMixerInfo recipeSpeedMixerInfo)");

            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            // General.CurrentCycleInfo.NewInfoSpeedMixer(new string[] { array[3] });

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock programName = new TextBlock
            {
                Foreground = Brushes.Wheat,
                Text = cycleSpeedMixerInfo.Columns[cycleSpeedMixerInfo.Name].DisplayName + ": " + recipeSpeedMixerInfo.Columns[recipeSpeedMixerInfo.Name].Value
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
            logger.Debug("UpdateCurrentWeightInfo");

            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            (wrapPanels[seqNumber].Children[3] as TextBlock).Text = 
                cycleWeightInfo.Columns[cycleWeightInfo.WeightedValue].DisplayName + ": " + 
                info[0];
        }
        public void UpdateCurrentSpeedMixerInfo(string[] info)
        {
            logger.Debug("UpdateCurrentSpeedMixerInfo");

            (wrapPanels[seqNumber].Children[1] as TextBlock).Text = Settings.Default.CycleInfo_Mix_StatusField + ": " + info[0];
        }
        public void AddRow(string rowText)
        {
            logger.Debug("AddRow");

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
            logger.Debug("UpdateSequenceNumber");

            // Il sert à quelque chose ce code ?
            if (wrapPanels == null) wrapPanels.Clear();

            seqNumber++;
            if (seqNumber == 0) checkAlarmsTimer.Start();
        }
        public void InitializeSequenceNumber()
        {
            logger.Debug("InitializeSequenceNumber");

            seqNumber = -1;
            activeAlarms.Clear();
            AlarmManagement.RAZalarms.Clear();
        }
        public void StopSequence()
        {
            logger.Debug("StopSequence");

            //isCheckAlarms_onGoing = false;
            checkAlarmsTimer.Stop();
            InitializeSequenceNumber();
        }
    }
}
