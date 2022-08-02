using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EasyModbus;


namespace Driver.MODBUS
{
    public class SpeedMixerModbus
    {
        private ModbusClient SpeedMixer;
        public bool IsConnected {get; set;}

        private readonly NameValueCollection MySettings;

        public SpeedMixerModbus()
        {
            IsConnected = false;

            MySettings = ConfigurationManager.GetSection("MODBUS_Connection_Info") as NameValueCollection;
            if (MySettings.Count == 0)        
            {
                MessageBox.Show("Post Settings are not defined");
                //Faire autre chose
            }

            Connect();
        }

        ~SpeedMixerModbus()
        {
            Disconnect();
            //MessageBox.Show("SpeedMixer: Au revoir");
        }

        public void Connect()
        {
            try
            {
                SpeedMixer = new ModbusClient(MySettings["IP_address"].ToString(), int.Parse(MySettings["port"]));    //Ip-Address and Port of Modbus-TCP-Server
                SpeedMixer.Connect();                   //Connect to Server
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                SpeedMixer.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
    }
}
