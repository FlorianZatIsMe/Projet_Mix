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
using EasyModbus;


namespace Driver.MODBUS
{
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

        static SpeedMixerModbus()
        {
            if (MySettings == null)
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Modbus Settings are not defined");
            }

            //taskAlarmScan = Task.Factory.StartNew(() => ScanAlarms());
            //Task.Factory.StartNew(() => ScanAlarms());

            // Initialisation des timers
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            scanAlarmTimer.Start();
        }
        /*
        ~SpeedMixerModbus()
        {
            Disconnect();
            MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - SpeedMixer: Au revoir");
        }*/
        private static void ScanAlarmTimer_OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (isModbusActive && !IsConnected() && !areAlarmActive[0])
            {
                AlarmManagement.NewAlarm(1, 0);
                areAlarmActive[0] = true;
            }
            else if (IsConnected() && areAlarmActive[0])
            {
                alarmManagement.InactivateAlarm(1, 0);
                areAlarmActive[0] = false;
            }

            if (isModbusActive && !IsConnected()) Connect();

            //MessageBox.Show("Salut");
            scanAlarmTimer.Enabled = true;
        }
        private static async void ScanAlarms()
        {
            while (true)
            {
                if (isModbusActive && !IsConnected() && !areAlarmActive[0])
                {
                    AlarmManagement.NewAlarm(1, 0);
                    areAlarmActive[0] = true;
                }
                else if (IsConnected() && areAlarmActive[0])
                {
                    alarmManagement.InactivateAlarm(1, 0);
                    areAlarmActive[0] = false;
                }

                if (isModbusActive && !IsConnected()) Connect();

                await Task.Delay(1000);
            }
        }
        public static void Initialize()
        {
            Connect();
            isModbusActive = true;
        }
        public static void Connect()
        {
            speedMixer = new ModbusClient(MySettings["IP_address"].ToString(), int.Parse(MySettings["port"]));    //Ip-Address and Port of Modbus-TCP-Server

            try { speedMixer.Connect(); }
            catch (Exception) { }
        }
        public static void Disconnect()
        {
            try
            {
                speedMixer.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
        public static bool IsConnected()
        {
            if (speedMixer == null)
            {
                return false;
            }

            return speedMixer.Available(1000); // c'est nul ce truc
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

                    speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters

                    // Name of the program
                    speedMixer.WriteSingleRegister(3000, 0);
                    speedMixer.WriteSingleRegister(3001, 0);
                    speedMixer.WriteSingleRegister(3002, 0);
                    speedMixer.WriteSingleRegister(3003, 0);
                    speedMixer.WriteSingleRegister(3004, 0);
                    speedMixer.WriteSingleRegister(3005, 0);
                    speedMixer.WriteSingleRegister(3006, 0);
                    speedMixer.WriteSingleRegister(3007, 0);
                    speedMixer.WriteSingleRegister(3008, 0);
                    speedMixer.WriteSingleRegister(3009, 0);
                    speedMixer.WriteSingleRegister(3010, 0);
                    speedMixer.WriteSingleRegister(3011, 0);
                    speedMixer.WriteSingleRegister(3012, 0);



                    speedMixer.WriteSingleRegister(3043, int.Parse(array[4]));  // Acceleration
                    speedMixer.WriteSingleRegister(3044, int.Parse(array[5]));  // Deceleration
                    speedMixer.WriteSingleRegister(3046, array[6] == "True" ? 1 : 0);    // Vacuum in Use (0=No ; 1=Yes)

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

                    if (vaccumScale != -1) speedMixer.WriteSingleRegister(3050, vaccumScale);    // Vacuum Scale (1=Torr ; 2=mBar ; 3=inHg ; 4=PSIA)
                    else MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Qu'est-ce que t'as fait ?");
                    //SpeedMixer.WriteSingleRegister(3052, 0);    // S Curve, pas touche

                    speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme
                    speedMixer.WriteSingleRegister(3053, 1);    // Commande pour mettre à jour tout les paramètres

                    //MessageBox.Show("Alors...");

                    //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters

                    for (int i = 0; i < 10; i++)
                    {
                        speedFromDB = array[12 + 3 * i];
                        timeFromDB = array[13 + 3 * i];
                        pressureFromDB = array[14 + 3 * i];

                        //MessageBox.Show(timeFromDB);

                        speedParameter = (speedFromDB == "" || speedFromDB == null) ? 0 : int.Parse(speedFromDB);
                        timeParameter = (timeFromDB == "" || timeFromDB == null) ? 0 : int.Parse(timeFromDB);
                        pressureParameter = (pressureFromDB == "" || pressureFromDB == null) ? 0 : int.Parse(pressureFromDB);

                        speedMixer.WriteSingleRegister(3013 + i, speedParameter);       // Vitesse des 10 phases
                        speedMixer.WriteSingleRegister(3023 + i, timeParameter);        // Temps des 10 phases
                        speedMixer.WriteSingleRegister(3033 + i, 10 * pressureParameter);    // Pression de vide des 10 phases
                    }

                    speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
                    speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme
                    speedMixer.WriteSingleRegister(3053, 1);    // Commande pour mettre à jour tout les paramètres
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - SpeedMixerModbus.cs, SetProgram(string[] array)" + ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
            }
        }
        public static void RunProgram()
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme
                    speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
                    speedMixer.WriteSingleRegister(3053, 100);    // Commande pour lancer un programme

                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - STOP");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
            }
        }
        public static void StopProgram()
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
                    speedMixer.WriteSingleRegister(3053, 200);  // Commande pour stopper un programme
                    speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme

                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - STOP");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
            }
        }
        public static void ResetErrorProgram()
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    //speedMixer.WriteMultipleRegisters(3053, new int[] { 300 });

                    //speedMixer.WriteSingleRegister(3053, 0);    // Instruction to allow the modification of the parameters
                    speedMixer.WriteSingleRegister(3053, 300);  // Commande pour lancer un programme
                    //speedMixer.WriteSingleRegister(3056, 0);    // Numéro du programme

                    //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - STOP");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
            }
        }
        public static bool[] GetStatus()
        {
            int[] message;
            bool[] status = new bool[8];
            UInt16 mask = 0x01;

            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    message = speedMixer.ReadHoldingRegisters(3100, 1);

                    UInt16 uintMessage = Convert.ToUInt16(message[0] & 0xFF);

                    for (int i = 0; i < 8; i++)
                    {
                        status[i] = (uintMessage & mask) == (0x01 << i);
                        //MessageBox.Show(i.ToString() + " - " + (status[i]).ToString());
                        mask <<= 1;
                    }
                }
                catch (Exception)
                {
                    // Peut-être générer une alarme à la place
                    //MessageBox.Show(ex.Message);
                }
            }
            else
            {
                //MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
            }
            return status;
        }
        public static int GetPressure()
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    int[] message = speedMixer.ReadHoldingRegisters(382, 1);
                    return message[0];
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return -1;
                }
            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
                return -1;
            }
        }
        public static int GetSpeed()
        {
            //if (!IsConnected()) Connect();

            if (IsConnected())
            {
                try
                {
                    int[] message = speedMixer.ReadHoldingRegisters(3101, 1);
                    return message[0];
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return -1;
                }

            }
            else
            {
                MessageBox.Show(MethodBase.GetCurrentMethod().Name + " - Problème de connection avec le SpeedMixer");
                return -1;
            }
        }
    }
}
