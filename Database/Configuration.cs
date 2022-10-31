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
        string FileWriterDestination { get; }
        int MaxRandomInt { get; }
        int RandomIntCount { get; }
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
