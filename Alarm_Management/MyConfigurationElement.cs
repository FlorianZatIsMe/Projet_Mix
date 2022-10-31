using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alarm_Management.Configuration_old
{
    public class Alarm_Features : ConfigurationElement
    {
        [ConfigurationProperty("ID_List", DefaultValue = -1, IsRequired = true)]
        public int ID_List
        {
            get
            {
                return (int)this["ID_List"];
            }
            set
            {
                value = (int)this["ID_List"];
            }
        }

        [ConfigurationProperty("UserID", DefaultValue = "", IsRequired = true)]
        public string UserID
        {
            get
            {
                return (string)this["UserID"];
            }
            set
            {
                value = (string)this["UserID"];
            }
        }
    }
}