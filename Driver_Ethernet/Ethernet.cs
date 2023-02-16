using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows;

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
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Ethernet(string ipAddress_arg, int port_arg, string endLine_arg = "")
        {
            ipAddress = ipAddress_arg;
            port = port_arg;
            endLine = endLine_arg;
        }

        public void Connect()
        {
            logger.Debug("Connect");
            // Créez un objet socket pour la connexion
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connectez le socket à l'adresse IP et au port spécifiés
            client.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));
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
            if (!IsConnected()) Connect();
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
            
            //MessageBox.Show("Alors");

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
