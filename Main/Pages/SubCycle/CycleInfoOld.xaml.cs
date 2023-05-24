using Alarm_Management;
using Database;
using Main.Properties;
using Message;
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
    public partial class CycleInfoOld : Page // Les pages ne peuvent pas être static
    {
        private readonly List<WrapPanel> wrapPanels = new List<WrapPanel>();
        private static int seqNumber;
        private readonly static List<Tuple<int, int>> activeAlarms = new List<Tuple<int, int>>();
        private System.Timers.Timer checkAlarmsTimer;
        private readonly Frame frameCycleInfo;

        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private int firstAlarmId;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public CycleInfoOld(CycleTableInfo cycleTableInfo, object[] cycleTableValues, Frame frame)
        {
            logger.Debug("Start");

            seqNumber = -1;

            if (AlarmManagement.ActiveAlarms.Count != 0)
            {
                firstAlarmId = AlarmManagement.Alarms[AlarmManagement.ActiveAlarms[0].Item1, AlarmManagement.ActiveAlarms[0].Item2].id - 1;
            }
            else
            {
                Task<object> t;

                t = MyDatabase.TaskEnQueue(() => {
                    return
                    MyDatabase.GetMax_new(auditTrailInfo, auditTrailInfo.Ids[auditTrailInfo.Id]);
                });
                firstAlarmId = (int)t.Result;             
            }


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
                MyMessageBox.Show(Settings.Default.CycleInfo_Error01);
                return;
            }*/

            labelOFNumber.Text = cycleTableValues[cycleTableInfo.BatchNumber].ToString();
            labelRecipeName.Text = cycleTableValues[cycleTableInfo.RecipeName].ToString();
            labelRecipeVersion.Text = cycleTableValues[cycleTableInfo.RecipeVersion].ToString();
            labelFinalWeight.Text = cycleTableValues[cycleTableInfo.FinalWeight].ToString();

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
            AuditTrailInfo auditTrailInfos = new AuditTrailInfo();

            t = MyDatabase.TaskEnQueue(() => { return 
                MyDatabase.GetMax_new(auditTrailInfo, auditTrailInfo.Ids[auditTrailInfo.Id]); });
            lastAlarmId = (int)t.Result;

            if (firstAlarmId != lastAlarmId)
            {
                t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetAlarms_new(firstAlarmId + 1, lastAlarmId, true); });
                List<object[]> rows = (List<object[]>)t.Result;
                firstAlarmId = lastAlarmId;

                foreach (object[] info in rows)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        AddRow(info[auditTrailInfos.DateTime] + " - " +
                            info[auditTrailInfos.Description] + " - " +
                            info[auditTrailInfos.ValueAfter]);
                    });
                }

                /*
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
                }*/
            }
            checkAlarmsTimer.Enabled = true;
        }

        public void NewInfo(ISeqTabInfo cycleSeqInfo, object[] recipe, decimal finalWeight = -1)
        {
            logger.Debug("NewInfo(ISeqInfo cycleSeqInfo)");

            if (cycleSeqInfo.GetType().Equals(typeof(RecipeWeightInfo)))
            {
                NewWeightInfo(recipe, finalWeight);
            }
            else if (cycleSeqInfo.GetType().Equals(typeof(RecipeSpeedMixerInfo)))
            {
                NewSpeedMixerInfo(recipe);
            }
            else
            {
                logger.Error(Settings.Default.CycleInfo_Error03);
                MyMessageBox.Show(Settings.Default.CycleInfo_Error03);
            }
        }
        public void NewWeightInfo(object[] recipeWeight, decimal finalWeight)
        {
            logger.Debug("NewInfo(RecipeWeightInfo recipeWeightInfo)");

            if (finalWeight == -1)
            {
                logger.Error("Pas bien");
                MyMessageBox.Show("Pas bien");
            }

            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            RecipeWeightInfo recipeWeightInfo = new RecipeWeightInfo();

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock productName = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.Product] + ": " +
                recipeWeight[recipeWeightInfo.Name].ToString()
            };

            TextBlock min = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.Min] + ": " + 
                Math.Round(cycleWeightInfo.GetMin(recipeWeight, finalWeight), 
                int.Parse(recipeWeight[recipeWeightInfo.DecimalNumber].ToString()))
                .ToString("N" + recipeWeight[recipeWeightInfo.DecimalNumber].ToString())
            };

            TextBlock max = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.Max] + ": " +
                Math.Round(cycleWeightInfo.GetMax(recipeWeight, finalWeight),
                int.Parse(recipeWeight[recipeWeightInfo.DecimalNumber].ToString()))
                .ToString("N" + recipeWeight[recipeWeightInfo.DecimalNumber].ToString()).ToString()
            };

            TextBlock actualWeight = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.ActualValue] + ": -"
            };

            wrapPanel.Children.Add(productName);
            wrapPanel.Children.Add(min);
            wrapPanel.Children.Add(max);
            wrapPanel.Children.Add(actualWeight);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void NewSpeedMixerInfo(object[] recipe)
        {
            logger.Debug("NewInfo(RecipeSpeedMixerInfo recipeSpeedMixerInfo)");

            CycleSpeedMixerInfo cycleSpeedMixerInfo = new CycleSpeedMixerInfo();
            RecipeSpeedMixerInfo recipeSpeedMixerInfo = new RecipeSpeedMixerInfo();
            // General.CurrentCycleInfo.NewInfoSpeedMixer(new string[] { array[3] });

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            TextBlock programName = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Text = cycleSpeedMixerInfo.Descriptions[cycleSpeedMixerInfo.Name] + ": " + recipe[recipeSpeedMixerInfo.Name].ToString()
            };

            TextBlock status = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
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
                cycleWeightInfo.Descriptions[cycleWeightInfo.ActualValue] + ": " + 
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
                Style = (Style)this.FindResource("Label1"),
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
