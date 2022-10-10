using Alarm_Management;
using Database;
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
        private List<WrapPanel> wrapPanels = new List<WrapPanel>();
        private static int seqNumber;
        private static List<Tuple<int, int>> activeAlarms = new List<Tuple<int, int>>();
        //private System.Timers.Timer checkAlarmsTimer;
        private Task taskCheckAlarm;
        //private MyDatabase db = new MyDatabase();
        private Frame frameCycleInfo;
        private bool isCheckAlarms_onGoing = true;

        public CycleInfo(string[] info, Frame frame)
        {
            seqNumber = -1;
            /*
            checkAlarmsTimer = new System.Timers.Timer();
            checkAlarmsTimer.Interval = 1000;
            checkAlarmsTimer.Elapsed += checkAlarmsTimer_OnTimedEvent;
            checkAlarmsTimer.AutoReset = true;*/

            frameCycleInfo = frame;

            InitializeComponent();

            if (info.Length == 4)
            {
                labelOFNumber.Text = info[0];
                labelRecipeName.Text = info[1];
                labelRecipeVersion.Text = info[2];
                labelFinalWeight.Text = info[3];

                SetVisibility(true);
            }
            else
            {
                MessageBox.Show("Il y a un problème là");
            }
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
        private async void checkAlarms_Task()
        {
            while(isCheckAlarms_onGoing)
            {
                string[] array;
                // S'il y a un évènement d'alarme qui n'a pas été affiché...
                if (!activeAlarms.SequenceEqual(AlarmManagement.activeAlarms))
                {
                    // On parcours toutes les alarmes actives
                    for (int i = 0; i < AlarmManagement.activeAlarms.Count; i++)
                    {
                        // Si l'alarme active qu'on regarde n'a pas été affichée...
                        if (!activeAlarms.Contains(AlarmManagement.activeAlarms[i]))
                        {
                            // On met dans la variable array, l'enregistrement d'audit trail de l'alarme en question
                            array = MyDatabase.GetOneRow(tableName: "audit_trail",
                                whereColumns: new string[] { "id" },
                                whereValues: new string[] { AlarmManagement.alarms[AlarmManagement.activeAlarms[i].Item1, AlarmManagement.activeAlarms[i].Item2].id.ToString() });

                            // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                            if (array.Count() != 0)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    AddRow(array[1] + " - " + array[4] + " - " + array[6]);
                                });
                            }
                            else
                            {
                                MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - checkAlarmsTimer_OnTimedEvent : Je n'aime pas ça " + AlarmManagement.alarms[AlarmManagement.activeAlarms[i].Item1, AlarmManagement.activeAlarms[i].Item2].id.ToString());
                            }
                        }
                    }

                    // activeAlarms = AlarmManagement.activeAlarms mais en plus chiant
                    activeAlarms.Clear();
                    for (int i = 0; i < AlarmManagement.activeAlarms.Count; i++) activeAlarms.Add(AlarmManagement.activeAlarms[i]);
                }

                // S'il y a une Remise A Zéro d'une alarme qui n'a pas été affichée...
                if (AlarmManagement.RAZalarms.Count > 0)
                {
                    // On parcours toutes les alarmes RAZ
                    for (int i = 0; i < AlarmManagement.RAZalarms.Count; i++)
                    {
                        // On met dans la variable array l'enregistrement d'audit trail de l'alarme
                        array = MyDatabase.GetOneRow(tableName: "audit_trail", whereColumns: new string[] { "id" }, whereValues: new string[] { AlarmManagement.RAZalarms[i].ToString() });

                        // S'il n'y a pas eu d'erreur, on affiche les infos de l'alarme
                        if (array.Count() != 0)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                AddRow(array[1] + " - " + array[4] + " - " + array[6]);
                            });
                        }
                        else
                        {
                            MessageBox.Show(MethodBase.GetCurrentMethod().DeclaringType.Name + " - checkAlarmsTimer_OnTimedEvent : Je n'aime pas ça");
                        }
                    }

                    AlarmManagement.RAZalarms.Clear();
                    MessageBox.Show("checkAlarmsTimer_OnTimedEvent - RAZ done je suis trop content !!!");
                }
                //MessageBox.Show("salut");
                await Task.Delay(1000);
            }
        }
        public void NewInfoWeight(string[] info)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock productName = new TextBlock();
            productName.Foreground = Brushes.Wheat;
            productName.Text = "Produit pesé: " + info[0];

            TextBlock min = new TextBlock();
            min.Foreground = Brushes.Wheat;
            min.Margin = new Thickness(20, 0, 0, 0);
            min.Text = "Minimum: " + info[1];

            TextBlock max = new TextBlock();
            max.Foreground = Brushes.Wheat;
            max.Margin = new Thickness(20, 0, 0, 0);
            max.Text = "Maximum: " + info[2];

            TextBlock actualWeight = new TextBlock();
            actualWeight.Foreground = Brushes.Wheat;
            actualWeight.Margin = new Thickness(20, 0, 0, 0);
            actualWeight.Text = "Masse pesée: -";

            wrapPanel.Children.Add(productName);
            wrapPanel.Children.Add(min);
            wrapPanel.Children.Add(max);
            wrapPanel.Children.Add(actualWeight);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void NewInfoSpeedMixer(string[] info)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock programName = new TextBlock();
            programName.Foreground = Brushes.Wheat;
            programName.Text = "Nom du mélange: " + info[0];

            TextBlock status = new TextBlock();
            status.Foreground = Brushes.Wheat;
            status.Margin = new Thickness(20, 0, 0, 0);
            status.Text = "Status: En attente";

            wrapPanel.Children.Add(programName);
            wrapPanel.Children.Add(status);

            StackMain.Children.Add(wrapPanel);
            wrapPanels.Add(wrapPanel);
        }
        public void UpdateCurrentWeightInfo(string[] info)
        {
            (wrapPanels[seqNumber].Children[3] as TextBlock).Text = "Masse pesée: " + info[0];
        }
        public void UpdateCurrentSpeedMixerInfo(string[] info)
        {
            (wrapPanels[seqNumber].Children[1] as TextBlock).Text = "Status: " + info[0];
        }
        public void AddRow(string rowText)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Margin = new Thickness(0, 10, 0, 0);

            TextBlock actualWeight = new TextBlock();
            actualWeight.Foreground = Brushes.Wheat;
//            actualWeight.Margin = new Thickness(0, 10, 0, 0);
            actualWeight.Text = rowText;

            wrapPanel.Children.Add(actualWeight);
            StackMain.Children.Add(wrapPanel);
        }
        public void UpdateSequenceNumber()
        {
            if (wrapPanels == null) // Il sert à quelque chose ce code ?
            {
                wrapPanels.Clear();
            }

            seqNumber++;
            if (seqNumber == 0)
            {
                //MessageBox.Show("UpdateSequenceNumber " + activeAlarms.Count.ToString() + AlarmManagement.activeAlarms.Count.ToString());
                //checkAlarmsTimer.Start();
                taskCheckAlarm = Task.Factory.StartNew(() => checkAlarms_Task());
            }
        }
        public void InitializeSequenceNumber()
        {
            //MessageBox.Show("InitializeSequenceNumber");
            seqNumber = -1;

            activeAlarms.Clear();
            AlarmManagement.RAZalarms.Clear();
        }
        public void StopSequence()
        {
            isCheckAlarms_onGoing = false;
            taskCheckAlarm.Wait();
            //while (!taskCheckAlarm.IsCompleted) await Task.Delay(25);

            InitializeSequenceNumber();
        }
    }
}
