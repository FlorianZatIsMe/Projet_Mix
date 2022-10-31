using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alarm_Management.AlarmManagement;

namespace Alarm_Management
{
    internal class Configuration
    {
        public interface IAlarmConfig
        {
            int id1 { get; }
            int id2 { get; }
            string Description { get; }
            AlarmType Type { get; }
        }

        public class AlarmConfig : IAlarmConfig
        {
            /*
        "id1": 0,
        "id2": 0,
        "Description": "J'ai chaud",
        "Type": "Alarm"
                */
            public int id1 => throw new NotImplementedException();

            public int id2 => throw new NotImplementedException();

            public string Description => throw new NotImplementedException();

            public AlarmType Type => throw new NotImplementedException();
        }
    }
}
