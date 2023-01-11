using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Alarm_Management;
using Database;
using Driver_MODBUS.Properties;
using EasyModbus;

namespace Driver_MODBUS
{
    /// <summary>
    /// 
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ConnectionInfo
    {
        public string ipAddress { get; set; }
        public int port { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public static class SpeedMixerSettings
    {
        public static int MixerStatusId_ReadyToRun { get; }
        public static int MixerStatusId_MixerRunning { get; }
        public static int MixerStatusId_MixerError { get; }
        public static int MixerStatusId_LidOpen { get; }
        public static int MixerStatusId_LidClosed { get; }
        public static int MixerStatusId_SafetyOK { get; }
        public static int MixerStatusId_RobotAtHome { get; }

        static SpeedMixerSettings()
        {
            MixerStatusId_ReadyToRun = Settings.Default.MixerStatusId_ReadyToRun;
            MixerStatusId_MixerRunning = Settings.Default.MixerStatusId_MixerRunning;
            MixerStatusId_MixerError = Settings.Default.MixerStatusId_MixerError;
            MixerStatusId_LidOpen = Settings.Default.MixerStatusId_LidOpen;
            MixerStatusId_LidClosed = Settings.Default.MixerStatusId_LidClosed;
            MixerStatusId_SafetyOK = Settings.Default.MixerStatusId_SafetyOK;
            MixerStatusId_RobotAtHome = Settings.Default.MixerStatusId_RobotAtHome;
        }
    }

    public struct IniInfo
    {
        public Window Window;
    }

    /// <summary>
    /// 
    /// </summary>
    public static class SpeedMixerModbus
    {
        private static ModbusClient speedMixer;
        private static readonly NameValueCollection MySettings = ConfigurationManager.GetSection("MODBUS_Connection_Info") as NameValueCollection;
        //private static MyDatabase db = new MyDatabase();
        private readonly static int nAlarms = 1;
        private readonly static bool[] areAlarmActive = new bool[nAlarms];
        //private readonly static Task taskAlarmScan;
        private static bool isModbusActive;
        private static readonly System.Timers.Timer scanAlarmTimer;
        private static readonly AlarmManagement alarmManagement = new AlarmManagement();

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static IniInfo info;

        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize");

            info = info_arg;
        }

        private static void ShowMessageBox(string message)
        {
            if (info.Window != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(info.Window, message);
                }));
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        static SpeedMixerModbus()
        {
            logger.Debug("Start");

            // Initialisation des timers
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = Settings.Default.ScanAlarmTimer_Interval,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            scanAlarmTimer.Start();
        }
        /*
        ~SpeedMixerModbus()
        {
            Disconnect();
            ShowMessageBox(MethodBase.GetCurrentMethod().Name + " - SpeedMixer: Au revoir");
        }*/
        private static void ScanAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (isModbusActive && !IsConnected() && !areAlarmActive[0])
            {
                AlarmManagement.NewAlarm(Settings.Default.Alarm_Connection_id1, Settings.Default.Alarm_Connection_id2);
                areAlarmActive[0] = true;
            }
            else if (IsConnected() && areAlarmActive[0])
            {
                AlarmManagement.InactivateAlarm(Settings.Default.Alarm_Connection_id1, Settings.Default.Alarm_Connection_id2);
                areAlarmActive[0] = false;
            }

            if (isModbusActive && !IsConnected()) Connect();

            scanAlarmTimer.Enabled = true;
        }
        public static void Initialize()
        {
            Connect();
            isModbusActive = true;
        }
        public static void Connect()
        {
            speedMixer = new ModbusClient(Settings.Default.ConnectionInfo.ipAddress, Settings.Default.ConnectionInfo.port);    //Ip-Address and Port of Modbus-TCP-Server

            try { speedMixer.Connect(); }
            catch (Exception ex) { logger.Error(ex.Message); }
        }
        public static void Disconnect()
        {
            try
            {
                speedMixer.Disconnect();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                ShowMessageBox(ex.Message);
            }
        }
        public static bool IsConnected()
        {
            if (speedMixer == null)
            {
                return false;
            }

            return speedMixer.Available(Settings.Default.ScanAlarmTimer_Interval); // c'est nul ce truc
        }
        public static void SetProgram(string[] array)
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                int vaccumScale;
                int speedParameter;
                int timeParameter;
                int pressureParameter;
                string speedFromDB;
                string timeFromDB;
                string pressureFromDB;

                try
                {

                    speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, 0);    // Instruction to allow the modification of the parameters

                    // Name of the program

                    for (int i = 0; i < 10; i++)
                    {
                        speedMixer.WriteSingleRegister(Settings.Default.Register_MixName + i, 0);
                    }

                    speedMixer.WriteSingleRegister(Settings.Default.Register_Acceleration, int.Parse(array[4]));  // Acceleration
                    speedMixer.WriteSingleRegister(Settings.Default.Register_Deceleration, int.Parse(array[5]));  // Deceleration
                    speedMixer.WriteSingleRegister(Settings.Default.Register_VacuumInUse, array[6] == "True" ? 1 : 0);    // Vacuum in Use (0=No ; 1=Yes)

                    //                    speedMixer.WriteSingleRegister(3048, 0);    // ça ne fonctionne pas, ça devrait être le choix du vent gas
                    //                    speedMixer.WriteSingleRegister(3049, 0);    // Monitor type Je pense que ça ne fonctionne pas

                    switch (array[9])
                    {
                        case "Torr":
                            vaccumScale = 1;
                            break;
                        case "mBar":
                            vaccumScale = 2;
                            break;
                        case "inHg":
                            vaccumScale = 3;
                            break;
                        case "PSIA":
                            vaccumScale = 4;
                            break;
                        default:
                            vaccumScale = -1;
                            break;
                    }

                    if (vaccumScale != -1) speedMixer.WriteSingleRegister(Settings.Default.Register_VacuumScale, vaccumScale);    // Vacuum Scale (1=Torr ; 2=mBar ; 3=inHg ; 4=PSIA)
                    else ShowMessageBox(MethodBase.GetCurrentMethod().Name + " - Qu'est-ce que t'as fait ?");
                    //SpeedMixer.WriteSingleRegister(3052, 0);    // S Curve, pas touche

                    speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, 0);    // Numéro du programme
                    speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, 1);    // Commande pour mettre à jour tout les paramètres

                    //ShowMessageBox("Alors...");

                    //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters

                    for (int i = 0; i < 10; i++)
                    {
                        speedFromDB = array[12 + 3 * i];
                        timeFromDB = array[13 + 3 * i];
                        pressureFromDB = array[14 + 3 * i];

                        //ShowMessageBox(timeFromDB);

                        speedParameter = (speedFromDB == "" || speedFromDB == null) ? 0 : int.Parse(speedFromDB);
                        timeParameter = (timeFromDB == "" || timeFromDB == null) ? 0 : int.Parse(timeFromDB);
                        pressureParameter = (pressureFromDB == "" || pressureFromDB == null) ? 0 : int.Parse(pressureFromDB);

                        speedMixer.WriteSingleRegister(Settings.Default.Register_Speed01 + i, speedParameter);       // Vitesse des 10 phases
                        speedMixer.WriteSingleRegister(Settings.Default.Register_Time01 + i, timeParameter);        // Temps des 10 phases
                        speedMixer.WriteSingleRegister(Settings.Default.Register_Pressure01 + i, 10 * pressureParameter);    // Pression de vide des 10 phases
                    }

                    speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, 0);    // Instruction to allow the modification of the parameters
                    speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, 0);    // Numéro du programme
                    speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, 1);    // Commande pour mettre à jour tout les paramètres
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    ShowMessageBox(ex.Message);
                }
            }
            else
            {
                ShowMessageBox("Problème de connection avec le SpeedMixer");
            }
        }
        public static void SetProgram(RecipeSpeedMixerInfo recipe)
        {
            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return;
            }

            int vaccumScale;
            int speedParameter;
            int timeParameter;
            int pressureParameter;
            string speedFromDB;
            string timeFromDB;
            string pressureFromDB;

            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_Reset);    // Instruction to allow the modification of the parameters

            // Name of the program

            for (int i = 0; i < 10; i++)
            {
                speedMixer.WriteSingleRegister(Settings.Default.Register_MixName + i, 0);
            }

            speedMixer.WriteSingleRegister(Settings.Default.Register_Acceleration, int.Parse(recipe.Columns[recipe.Acceleration].Value));  // Acceleration
            speedMixer.WriteSingleRegister(Settings.Default.Register_Deceleration, int.Parse(recipe.Columns[recipe.Deceleration].Value));  // Deceleration
            speedMixer.WriteSingleRegister(Settings.Default.Register_VacuumInUse, 
                recipe.Columns[recipe.Vaccum_control].Value == DatabaseSettings.General_TrueValue_Read ? 
                Settings.Default.VacuumInUse_Yes : Settings.Default.VacuumInUse_No);    // Vacuum in Use (0=No ; 1=Yes)

            //speedMixer.WriteSingleRegister(3048, 0);    // ça ne fonctionne pas, ça devrait être le choix du vent gas
            //speedMixer.WriteSingleRegister(3049, 0);    // Monitor type Je pense que ça ne fonctionne pas
            string pUnit = recipe.Columns[recipe.PressureUnit].Value;
            vaccumScale = pUnit == recipe.PUnit_Torr ? Settings.Default.VacuumScale_Torr :
                pUnit == recipe.PUnit_mBar ? Settings.Default.VacuumScale_mBar :
                pUnit == recipe.PUnit_inHg ? Settings.Default.VacuumScale_inHg :
                pUnit == recipe.PUnit_PSIA ? Settings.Default.VacuumScale_PSIA : Settings.Default.VacuumScale_Error;

            if (vaccumScale != Settings.Default.VacuumScale_Error) speedMixer.WriteSingleRegister(Settings.Default.Register_VacuumScale, vaccumScale);    // Vacuum Scale (1=Torr ; 2=mBar ; 3=inHg ; 4=PSIA)
            else 
            {
                logger.Error(Settings.Default.Error02);
                ShowMessageBox(Settings.Default.Error02);
            }
            //SpeedMixer.WriteSingleRegister(3052, 0);    // S Curve, pas touche

            speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, Settings.Default.ProgramId_ToUse);    // Numéro du programme
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_SetDefinedMixProgram);    // Commande pour mettre à jour tout les paramètres

            //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters

            for (int i = 0; i < 10; i++)
            {
                speedFromDB = recipe.Columns[recipe.Speed00 + 3 * i].Value;
                timeFromDB = recipe.Columns[recipe.Time00 + 3 * i].Value;
                pressureFromDB = recipe.Columns[recipe.Pressure00 + 3 * i].Value;

                speedParameter = (speedFromDB == "" || speedFromDB == null) ? 0 : int.Parse(speedFromDB);
                timeParameter = (timeFromDB == "" || timeFromDB == null) ? 0 : int.Parse(timeFromDB);
                pressureParameter = (pressureFromDB == "" || pressureFromDB == null) ? 0 : int.Parse(pressureFromDB);

                speedMixer.WriteSingleRegister(Settings.Default.Register_Speed01 + i, speedParameter);       // Vitesse des 10 phases
                speedMixer.WriteSingleRegister(Settings.Default.Register_Time01 + i, timeParameter);        // Temps des 10 phases
                speedMixer.WriteSingleRegister(Settings.Default.Register_Pressure01 + i, Settings.Default.Pressure_Multiplicator * pressureParameter);    // Pression de vide des 10 phases
            }

            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_Reset);    // Instruction to allow the modification of the parameters
            speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, Settings.Default.ProgramId_ToUse);    // Numéro du programme
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_SetDefinedMixProgram);    // Commande pour mettre à jour tout les paramètres
        }
        public static void RunProgram()
        {
            //if (!IsConnected())

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return;
            }

            // A REVOIR

            speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, Settings.Default.ProgramId_ToUse);    // Numéro du programme
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_Reset);    // Instruction to allow the modification of the parameters
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_RunDefinedMixProgram);    // Commande pour lancer un programme

            //speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme
            //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
            //speedMixer.WriteSingleRegister(3053, 100);    // Commande pour lancer un programme
        }
        public static void StopProgram()
        {
            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return;
            }

            // A REVOIR

            speedMixer.WriteSingleRegister(Settings.Default.Register_ProgramId, Settings.Default.ProgramId_ToUse);    // Numéro du programme
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_Stop_Active_Mix);    // Commande pour lancer un programme
            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_Reset);    // Instruction to allow the modification of the parameters

            //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
            //speedMixer.WriteSingleRegister(3053, 200);  // Commande pour stopper un programme
            //speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme
        }
        public static void ResetErrorProgram()
        {
            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return;
            }

            // A REVOIR

            speedMixer.WriteSingleRegister(Settings.Default.Register_Instruction, Settings.Default.Instruction_ResetError);    // Instruction to allow the modification of the parameters

            //speedMixer.WriteSingleRegister(3053, 300);  // Commande pour lancer un programme
        }
        public static bool[] GetStatus()
        {
            int[] message;
            bool[] status = new bool[8];
            UInt16 mask = 0x01;

            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return status;
            }

            message = speedMixer.ReadHoldingRegisters(Settings.Default.Register_MixerStatus, 1);

            UInt16 uintMessage = Convert.ToUInt16(message[0] & 0xFF);

            for (int i = 0; i < 8; i++)
            {
                status[i] = (uintMessage & mask) == (0x01 << i);
                //ShowMessageBox(i.ToString() + " - " + (status[i]).ToString());
                mask <<= 1;
            }

            return status;
        }
        public static decimal GetPressure()
        {
            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return -1;
            }

            int[] message = speedMixer.ReadHoldingRegisters(Settings.Default.Register_MixerPressure, 1);
            return (message[0] / Settings.Default.Pressure_Multiplicator);
        }
        public static int GetSpeed()
        {
            //if (!IsConnected()) Connect();

            if (!IsConnected())
            {
                logger.Error(Settings.Default.Error01);
                //ShowMessageBox(Settings.Default.Error01);
                return -1;
            }

            int[] message = speedMixer.ReadHoldingRegisters(Settings.Default.Register_MixerSpeed, 1);
            return message[0];
        }
    }
}
