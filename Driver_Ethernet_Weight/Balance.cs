using Driver_Ethernet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;

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
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //
        // CONSTRUCTOR
        //
        static Balance()
        {
            eth = new Ethernet("10.10.1.3", 8001, "\r\n",0, 0);
            standbyMessage = "EL" + eth.endLine;
            /*
            // Initialisation des timers
            getCurrentWeightTimer = new System.Timers.Timer
            {
                Interval = 100,
                AutoReset = false
            };
            getCurrentWeightTimer.Elapsed += GetCurrentWeightTimer_Elapsed;*/
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

        public static void BlockUse() { isFree = false; }
        public static void FreeUse() { isFree = true; }
        public static bool IsFree() { return isFree; }

        public static int TareBalance()
        {
            int zeroResult = SendZeroCommand();
            logger.Debug("Bonjour vous");
            if (zeroResult == 0 || zeroResult == 1)
            {
                return SendTareCommand();
            }

            logger.Error("Zéro failed: " + zeroResult.ToString());
            return -1;
        }

        public static Weight GetOneWeight()
        {
            string receivedData = SendCommand("SI");
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
            return eth.ReadData("@") != standbyMessage;
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

            logger.Trace("dataToSend - " + dataToSend);
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
            logger.Error("receivedData - " + test);

            sendingCommand = false;
            return receivedData;
        }
        private static int SendZeroCommand()
        {
            string receivedData = SendCommand("ZI", 20);
            if (receivedData == null) return -1;                    // Erreur commande (impossible en théorie)
            else if (receivedData == "ZI D" + eth.endLine) return 0; // Zéro réussi instable
            else if (receivedData == "ZI S" + eth.endLine) return 1; // Zéro réussi stabl
            else if (receivedData == "ZI I" + eth.endLine) return 2; // Zéro pas fait, autre tâche en cours
            else if (receivedData == "ZI +" + eth.endLine) return 3; // Zéro pas fait, poids de base trop lourd
            else if (receivedData == "ZI -" + eth.endLine) return 4; // Zéro pas fait, poids de base trop léger
            return -2;                                              // Erreur, bizzare, c'est sensé être impossible
        }
        private static int SendTareCommand()
        {
            string receivedData = SendCommand("T", 20000);
            if (receivedData == null) return -1;                        // Erreur commande (impossible en théorie)
            else if (receivedData.Substring(0, 3) == "T S") return 0;   // Tare réussi
            else if (receivedData == "T I" + eth.endLine) return 1;     // Tare pas faite, autre tâche en cours
            else if (receivedData == "T L" + eth.endLine) return 2;     // Tare pas faite, paramètre incorrect
            else if (receivedData == "T +" + eth.endLine) return 3;     // Tare pas faite, poids de base trop lourd
            else if (receivedData == "T -" + eth.endLine) return 4;     // Tare pas faite, poids de base trop léger
            return -2;                                                  // Erreur, bizzare, c'est sensé être impossible
        }
        private static Weight GetWeightFromData(string receivedData)
        {
            Weight weight = new Weight();
            if (receivedData == null) return null;

            try
            {
                int n = receivedData.IndexOf('g');
                if (n == -1) return null;
                string processedData = receivedData.Substring(3, n - 3).Trim();
                weight.value = decimal.Parse(processedData, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
                logger.Trace(weight.value.ToString());
                if (receivedData.StartsWith("S S"))
                {
                    weight.isStable = true;
                }
                else if (receivedData.StartsWith("S D"))
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
/*        private static void GetCurrentWeightTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            currentWeight = GetWeightFromData(eth.ReadData(500));
            getCurrentWeightTimer.Enabled = isGetCurWeightTimerOn;
        }*/
    }
}
