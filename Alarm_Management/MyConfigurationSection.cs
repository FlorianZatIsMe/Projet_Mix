using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alarm_Management.Configuration
{
    public class List : ConfigurationSection
    {
        [ConfigurationProperty("Alarm")]
        public Alarm_Features Alarm_Features
        {
            get
            {
                return (Alarm_Features)this["Alarm"];
            }
            set
            {
                value = (Alarm_Features)this["Alarm"];
            }
        }
    }
}