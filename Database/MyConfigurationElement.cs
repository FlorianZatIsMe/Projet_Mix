/*using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Configuration_old
{
    /// <summary>
    /// 
    /// </summary>
    public class DB_Features : ConfigurationElement
    {
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("Server", DefaultValue = "", IsRequired = true)]
        public string Server
        {
            get
            {
                return (string)this["Server"];
            }
            set
            {
                value = (string)this["Server"];
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

        [ConfigurationProperty("Password", IsRequired = false)]
        public string Password
        {
            get
            {
                return (string)this["Password"];
            }
            set
            {
                value = (string)this["Password"];
            }
        }
        [ConfigurationProperty("Database", DefaultValue = "", IsRequired = false)]
        public string Database
        {
            get
            {
                return (string)this["Database"];
            }
            set
            {
                value = (string)this["Database"];
            }
        }
    }
}*/