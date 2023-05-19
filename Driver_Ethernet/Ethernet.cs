using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Timers;
using Alarm_Management;

namespace Driver_Ethernet
{
    public class Ethernet
    {
        private Socket client;
        private readonly string ipAddress;
        private readonly int port;
        public readonly string endLine;
        private readonly int alarmConnectId1;
        private readonly int alarmConnectId2;

        //private bool isActive = false;
        private Timer scanAlarmTimer;
        private bool[] areAlarmActive;
        private readonly int nAlarms = 1;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //
        // CONSTRUCTOR
        //
        public Ethernet(string ipAddress_arg, int port_arg, string endLine_arg = "", int alarmConnectId1_arg = -1, int alarmConnectId2_arg = -1)
        {
            ipAddress = ipAddress_arg;
            port = port_arg;
            endLine = endLine_arg;

            alarmConnectId1 = alarmConnectId1_arg;
            alarmConnectId2 = alarmConnectId2_arg;
            areAlarmActive = new bool[nAlarms];

            // Initialisation des timers
            //InitializeScanAlarmTimer();

            Connect();
            //scanAlarmTimer.Start();
        }

        //
        // PUBLIC METHODS
        //
        public void Connect()
        {
            logger.Debug("Connect");
            // Créez un objet socket pour la connexion
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = 500
            };

            try
            {
                // Connectez le socket à l'adresse IP et au port spécifiés
                client.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            //isActive = true;

            if (scanAlarmTimer == null || !scanAlarmTimer.Enabled)
            {
                InitializeScanAlarmTimer();
                scanAlarmTimer.Start();
            }
        }
        public void Disconnect()
        {
            if (client != null)
            {
                scanAlarmTimer.Dispose();
                // Fermez la connexion
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                //isActive = false;
            }
        }
        public bool IsConnected()
        {
            return client != null && client.Connected;
        }
        public bool WriteData(string dataToSend)
        {
            //logger.Debug("WriteData");
            if (!IsConnected()) Connect();
            if (!IsConnected()) return false;

            byte[] buffer = Encoding.ASCII.GetBytes(dataToSend + endLine);
            client.Send(buffer);
            return true;
        }
        public string ReadData(int msWaitTime = 1000)
        {
            if (!IsConnected()) return null;

            // Envoyez et recevez les données via le socket
            byte[] data = new byte[32];
            //*
            int receivedDataLength = client.Receive(data);
            Console.WriteLine("Received data: " + Encoding.ASCII.GetString(data, 0, receivedDataLength));
            return Encoding.ASCII.GetString(data, 0, receivedDataLength);
        }
        public string ReadData(string dataToSend, int msWaitTime = -1)
        {
            if (WriteData(dataToSend))
            {
                return ReadData(msWaitTime);
            }
            return "";
        }

        //
        // PRIVATE METHODS
        //
        private void InitializeScanAlarmTimer()
        {
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
        }
        private void ScanAlarmTimer_OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            //if (isActive)
            {
                //logger.Debug("ScanAlarmTimer_OnTimedEvent");
                if (!IsConnected() && !areAlarmActive[0])
                {
                    AlarmManagement.NewAlarm(alarmConnectId1, alarmConnectId2);
                    areAlarmActive[0] = true;
                }
                else if (IsConnected() && areAlarmActive[0])
                {
                    AlarmManagement.InactivateAlarm(alarmConnectId1, alarmConnectId2);
                    areAlarmActive[0] = false;
                }

                if (!IsConnected()) Connect();
            }
            scanAlarmTimer.Enabled = true;
        }
    }
}
