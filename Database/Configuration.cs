using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database
{
    public interface IConfig
    {
        /// <summary>
        /// 
        /// </summary>
        string FileWriterDestination { get; }
        /// <summary>
        /// 
        /// </summary>
        int MaxRandomInt { get; }
        /// <summary>
        /// 
        /// </summary>
        int RandomIntCount { get; }
        /// <summary>
        /// 
        /// </summary>
        int ScanConnect_Timer { get; }
    }

    public class Config : IConfig
    {
        public string FileWriterDestination { get; set; }
        public int MaxRandomInt { get; set; }
        public int RandomIntCount { get; set; }
        public int ScanConnect_Timer { get; set; }

        public Config()
        {

        }
    }
}
