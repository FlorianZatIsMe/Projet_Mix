using Driver_Ethernet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using Driver_Ethernet_Weight.Properties;
using System.Configuration;

namespace Driver_Ethernet_Balance
{
    public class Weight
    {
        public decimal value { get; set; }
        public bool isStable { get; set; }
    }

    /*public struct IniInfo
    {
        public Window Window;
    }*/

    public static class Balance
    {
        private static readonly Ethernet eth;
        private static readonly string standbyMessage;
        //private static Weight currentWeight;
        //private static readonly System.Timers.Timer getCurrentWeightTimer;
        private static bool isGetCurWeightTimerOn;
        private static bool sendingCommand = false;
        private static bool isFree = true;
        private static Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //
        // CONSTRUCTOR
        //
        static Balance()
        {
            eth = new Ethernet(Settings.Default.IpAddress, 
                Settings.Default.Port,
                config.AppSettings.Settings["BalanceEndLine"].Value, 
                Settings.Default.Alarm_Connection_id1, 
                Settings.Default.Alarm_Connection_id2);
            standbyMessage = Settings.Default.StandByChar + eth.endLine;
        }

        //
        // PUBLIC METHODS
        //
        public static bool Connect()
        {
            eth.Connect();
            return IsConnected();
        }

        public static void Disconnect()
        {
            eth.Disconnect();
        }

        public static bool IsConnected()
        {
            return eth.IsConnected();
        }

        public static void BlockUse() { isFree = false; logger.Debug("Block"); }
        public static void FreeUse() { isFree = true; logger.Debug("Free"); }
        public static bool IsFree() { return isFree; }

        public static int TareBalance()
        {
            logger.Debug("TareBalance");
            int zeroResult = SendZeroCommand();
            if (zeroResult == 0 || zeroResult == 1)
            {
                return SendTareCommand();
            }

            logger.Error("Zéro failed: " + zeroResult.ToString());
            return -1;
        }

        public static Weight GetOneWeight()
        {
            string receivedData = SendCommand(Settings.Default.GetWeightCommand);
            if (receivedData == null)
            {
                logger.Fatal("C'est NULL");
                return null;
            }
            return GetWeightFromData(receivedData);
        }

        //
        // PRIVATE METHODS
        //
        private static bool ResetBalance()
        {
            return eth.ReadData(Settings.Default.ResetCommand) != standbyMessage;
        }
        private static string SendCommand(string dataToSend, int msWaitTime = 500)
        {
            if (isFree)
            {
                logger.Error("La balance n'est pas réservé");
                return "";
            }

            if (sendingCommand) return "";
            sendingCommand = true;

            logger.Debug("dataToSend - " + dataToSend);
            string receivedData = eth.ReadData(dataToSend, msWaitTime);

            if (receivedData == standbyMessage)
            {
                if (!ResetBalance())
                {
                    sendingCommand = false;
                    return null;
                }
                receivedData = eth.ReadData(dataToSend, msWaitTime);
            }

            string test = receivedData;
            logger.Debug("receivedData - " + test);

            sendingCommand = false;
            return receivedData;
        }
        private static int SendZeroCommand()
        {
            string receivedData = SendCommand(Settings.Default.ZeroCommand);
            if (receivedData == null) return -1;                                                            // Erreur commande (impossible en théorie)
            else if (receivedData == Settings.Default.ZeroSuccessful_Instable + eth.endLine) return 0;      // Zéro réussi instable
            else if (receivedData == Settings.Default.ZeroSuccessful_Stable + eth.endLine) return 1;        // Zéro réussi stable
            else if (receivedData == Settings.Default.ZeroFailed_OtherTaskOnGoing + eth.endLine) return 2;  // Zéro pas fait, autre tâche en cours
            else if (receivedData == Settings.Default.ZeroFailed_TooHeavy + eth.endLine) return 3;          // Zéro pas fait, poids de base trop lourd
            else if (receivedData == Settings.Default.ZeroFailed_TooLight + eth.endLine) return 4;          // Zéro pas fait, poids de base trop léger
            return -2;                                                                                      // Erreur, bizzare, c'est sensé être impossible
        }
        private static int SendTareCommand()
        {
            string receivedData = SendCommand(Settings.Default.TareCommand);
            if (receivedData == null) return -1;                                                                // Erreur commande (impossible en théorie)
            else if (receivedData.Substring(0, 3) == Settings.Default.TareSuccessful) return 0;                 // Tare réussi
            else if (receivedData == Settings.Default.TareFailed_OtherTaskOnGoing + eth.endLine) return 1;      // Tare pas faite, autre tâche en cours
            else if (receivedData == Settings.Default.TareFailed_IncorrectParameters + eth.endLine) return 2;   // Tare pas faite, paramètre incorrect
            else if (receivedData == Settings.Default.TareFailed_TooHeavy + eth.endLine) return 3;              // Tare pas faite, poids de base trop lourd
            else if (receivedData == Settings.Default.TareFailed_TooLight + eth.endLine) return 4;              // Tare pas faite, poids de base trop léger
            return -2;                                                                                          // Erreur, bizzare, c'est sensé être impossible
        }
        private static Weight GetWeightFromData(string receivedData)
        {
            Weight weight = new Weight();
            if (receivedData == null) return null;

            try
            {
                int n = receivedData.IndexOf(Settings.Default.WeightResponse_Unit);
                if (n == -1) return null;
                string processedData = receivedData.Substring(3, n - 3).Trim();
                weight.value = decimal.Parse(processedData, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
                logger.Trace(weight.value.ToString());
                if (receivedData.StartsWith(Settings.Default.WeightResponse_Prefix_Stable))
                {
                    weight.isStable = true;
                }
                else if (receivedData.StartsWith(Settings.Default.WeightResponse_Prefix_Unstable))
                {
                    weight.isStable = false;
                }
                else
                {
                    weight = null;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                weight = null;
            }

            return weight;
        }
    }
}
