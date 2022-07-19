using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Configuration
{
    public class DB_Connection_Info : ConfigurationSection
    {
        [ConfigurationProperty("DB_Settings")]
        public DB_Features DB_Features
        {
            get
            {
                return (DB_Features)this["DB_Settings"];
            }
            set
            {
                value = (DB_Features)this["DB_Settings"];
            }
        }
    }
}