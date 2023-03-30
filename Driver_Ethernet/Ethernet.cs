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
    public class Config
    {

    }


    public class Ethernet
    {
        private Socket client;
        private readonly string ipAddress;
        private readonly int port;
        public readonly string endLine;
        private readonly int alarmConnectId1;
        private readonly int alarmConnectId2;
        private bool isActive = false;
        private readonly System.Timers.Timer scanAlarmTimer;
        private bool[] areAlarmActive;
        private readonly int nAlarms = 1;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Ethernet(string ipAddress_arg, int port_arg, string endLine_arg = "", int alarmConnectId1_arg = -1, int alarmConnectId2_arg = -1)
        {
            ipAddress = ipAddress_arg;
            port = port_arg;
            endLine = endLine_arg;

            alarmConnectId1 = alarmConnectId1_arg;
            alarmConnectId2 = alarmConnectId2_arg;
            areAlarmActive = new bool[nAlarms];
            /*
            // Créez un objet socket pour la connexion
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = 500
            };*/

            // Initialisation des timers
            scanAlarmTimer = new System.Timers.Timer
            {
                Interval = 1000,
                AutoReset = false
            };
            scanAlarmTimer.Elapsed += ScanAlarmTimer_OnTimedEvent;
            scanAlarmTimer.Start();
        }

        private void ScanAlarmTimer_OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (isActive)
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
                //client.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));
                //client.BeginConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), null, client);
            }
            scanAlarmTimer.Enabled = true;
        }

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
            isActive = true;
        }

        public bool IsConnected()
        {
            return client != null && client.Connected;
        }

        public void Disconnect()
        {
            if (client != null)
            {
                // Fermez la connexion
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                isActive = false;
            }
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
            //logger.Debug("ReadData(int msWaitTime = 1000)");
            //if (!IsConnected()) Connect();
            if (!IsConnected()) return null;

            // Envoyez et recevez les données via le socket
            byte[] data = new byte[32];
            //*
            int receivedDataLength = client.Receive(data);
            Console.WriteLine("Received data: " + Encoding.ASCII.GetString(data, 0, receivedDataLength));
            return Encoding.ASCII.GetString(data, 0, receivedDataLength);
            //*/
            /*
            ArraySegment<byte> buffer = new ArraySegment<byte>(data);
            Task<int> task = client.ReceiveAsync(buffer, SocketFlags.None);
            task.Wait(msWaitTime);
            
            //Message.MyMessageBox.Show("Alors");

            int receivedDataLength;
            if (task.IsCompleted)
            {
                receivedDataLength = task.Result;
                Console.WriteLine("Received data: " + Encoding.ASCII.GetString(data, 0, receivedDataLength));
                return Encoding.ASCII.GetString(data, 0, receivedDataLength);
            }
            return null;
            //*/
        }

        public string ReadData(string dataToSend, int msWaitTime = -1)
        {
            if (WriteData(dataToSend))
            {
                return ReadData(msWaitTime);
            }
            return "";
        }
    }
}
