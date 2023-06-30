using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Alarm_Management;
using Database;
using Message;
using Main.Properties;

namespace Main.Pages.SubCycle
{
    /// <summary>
    /// Allow the interaction with CycleInfo.xaml. 
    /// </summary>
    public partial class CycleInfo : UserControl
    {
        private readonly List<WrapPanel> wrapPanels = new List<WrapPanel>();
        private static int seqNumber;
        private readonly static List<Tuple<int, int>> activeAlarms = new List<Tuple<int, int>>();
        private System.Timers.Timer checkAlarmsTimer;
        private readonly ContentControl contentControlCycleInfo;
        private readonly AuditTrailInfo auditTrailInfo = new AuditTrailInfo();
        private int firstAlarmId;
        private Thickness margin = new Thickness(0, 20, 0, 0);
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Sets the first alarm ID, initialize the timer, puts contentControl in a variable and sets the text of the labels of the UserControl 
        /// </summary>
        /// <param name="cycleTableValues">Contains the values of the row of the database table cycle related to the current cycle</param>
        /// <param name="contentControl">ContentControl variable where is displayed this UserControl</param>
        public CycleInfo(object[] cycleTableValues, ContentControl contentControl)
        {
            logger.Debug("Start");
            CycleTableInfo cycleTableInfo = new CycleTableInfo();

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

            contentControlCycleInfo = contentControl;

            InitializeComponent();

            labelJobNumber.Text = cycleTableValues[cycleTableInfo.JobNumber].ToString();
            labelBatchNumber.Text = cycleTableValues[cycleTableInfo.BatchNumber].ToString();
            labelRecipeName.Text = cycleTableValues[cycleTableInfo.RecipeName].ToString();
            labelRecipeVersion.Text = cycleTableValues[cycleTableInfo.RecipeVersion].ToString();
            labelFinalWeight.Text = cycleTableValues[cycleTableInfo.FinalWeight].ToString();

            SetVisibility(true);
        }

        //
        // PUBLIC METHODS
        //

        /// <summary>
        /// Hides or displays this UserControl
        /// </summary>
        /// <param name="visibility">
        /// <para>True: Displays the UserControl</para>
        /// <para>False: Hides the UserControl</para>
        /// </param>
        public void SetVisibility(bool visibility)
        {
            logger.Debug("SetVisibility");

            if (visibility)
            {
                contentControlCycleInfo.Content = this;
                contentControlCycleInfo.Visibility = Visibility.Visible;
            }
            else
            {
                contentControlCycleInfo.Content = null;
                contentControlCycleInfo.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Add the information related to a cycle sequence (weight or speedmixer)
        /// </summary>
        /// <param name="cycleSeqInfo">An ISeqTabInfo object which represents the cycle sequence to display</param>
        /// <param name="recipe">The recipe parameters (from the database) of the applicable cycle sequence</param>
        /// <param name="finalWeight">Final weight of the product (set to start a cycle) to allow the calculation of weight setpoint, min and max</param>
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

        /// <summary>
        /// Updates the measured weight of the current weight sequence
        /// </summary>
        /// <param name="info">Array of string which contains the measured weight</param>
        public void UpdateCurrentWeightInfo(string[] info)
        {
            logger.Debug("UpdateCurrentWeightInfo");

            CycleWeightInfo cycleWeightInfo = new CycleWeightInfo();
            (wrapPanels[seqNumber].Children[3] as TextBlock).Text =
                cycleWeightInfo.Descriptions[cycleWeightInfo.ActualValue] + ": " +
                info[0];
        }

        /// <summary>
        /// Updates the mixing status of the current mixing sequence
        /// </summary>
        /// <param name="info">Array of string which contains the status of the sequence</param>
        public void UpdateCurrentSpeedMixerInfo(string[] info)
        {
            logger.Debug("UpdateCurrentSpeedMixerInfo");

            (wrapPanels[seqNumber].Children[0] as TextBlock).Text = Settings.Default.CycleInfo_Mix_StatusField + ": " + info[0];
        }

        /// <summary>
        /// Method to use to incremente the sequence number variable. If use for the first time, the timer which checks new alarms is started
        /// </summary>
        public void UpdateSequenceNumber()
        {
            logger.Debug("UpdateSequenceNumber");

            // Il sert à quelque chose ce code ?
            if (wrapPanels == null) wrapPanels.Clear();

            seqNumber++;
            if (seqNumber == 0) checkAlarmsTimer.Start();
        }

        /// <summary>
        /// Reset the sequence number and active alarms list. These variables must be reset before starting a cycle
        /// </summary>
        public void InitializeSequenceNumber()
        {
            logger.Debug("InitializeSequenceNumber");

            seqNumber = -1;
            activeAlarms.Clear();
            AlarmManagement.RAZalarms.Clear();
        }

        /// <summary>
        /// Method to use at the end of cycle. Stops the timer which checks new alarms and execute the method InitializeSequenceNumber
        /// </summary>
        public void StopSequence()
        {
            logger.Debug("StopSequence");

            //isCheckAlarms_onGoing = false;
            checkAlarmsTimer.Stop();
            InitializeSequenceNumber();
        }

        //
        // PRIVATE METHODS
        //
        private void ScanConnectTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            int lastAlarmId;
            Task<object> t;
            AuditTrailInfo auditTrailInfos = new AuditTrailInfo();

            t = MyDatabase.TaskEnQueue(() => {
                return
                MyDatabase.GetMax_new(auditTrailInfo, auditTrailInfo.Ids[auditTrailInfo.Id]);
            });
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
        private void NewWeightInfo(object[] recipeWeight, decimal finalWeight)
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
                Margin = this.margin
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
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.Min] + " (" + Settings.Default.General_Weight_Unit + "): " +
                Math.Round(cycleWeightInfo.GetMin(recipeWeight, finalWeight),
                int.Parse(recipeWeight[recipeWeightInfo.DecimalNumber].ToString()))
                .ToString("N" + recipeWeight[recipeWeightInfo.DecimalNumber].ToString())
            };

            TextBlock max = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.Max] + " (" + Settings.Default.General_Weight_Unit + "): " +
                Math.Round(cycleWeightInfo.GetMax(recipeWeight, finalWeight),
                int.Parse(recipeWeight[recipeWeightInfo.DecimalNumber].ToString()))
                .ToString("N" + recipeWeight[recipeWeightInfo.DecimalNumber].ToString()).ToString()
            };

            TextBlock actualWeight = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(20, 0, 0, 0),
                Text = cycleWeightInfo.Descriptions[cycleWeightInfo.ActualValue] + " (" + Settings.Default.General_Weight_Unit + "): -"
            };

            wrapPanel.Children.Add(productName);
            wrapPanel.Children.Add(min);
            wrapPanel.Children.Add(max);
            wrapPanel.Children.Add(actualWeight);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        private void NewSpeedMixerInfo(object[] recipe)
        {
            logger.Debug("NewInfo(RecipeSpeedMixerInfo recipeSpeedMixerInfo)");

            WrapPanel wrapPanel = new WrapPanel
            {
                Margin = this.margin
            };
            /*
            TextBlock programName = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Text = cycleSpeedMixerInfo.Descriptions[cycleSpeedMixerInfo.Name] + ": " + recipe[recipeSpeedMixerInfo.Name].ToString()
            };*/

            TextBlock status = new TextBlock
            {
                Style = (Style)this.FindResource("Label1"),
                Margin = new Thickness(0, 0, 0, 0),
                Text = Settings.Default.CycleInfo_Mix_StatusField + ": " + Settings.Default.CycleInfo_Mix_StatusWaiting
            };

            //wrapPanel.Children.Add(programName);
            wrapPanel.Children.Add(status);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        private void AddRow(string rowText)
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
    }
}
