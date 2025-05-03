using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMeasurementApp.Models
{
    public class ConfigModel
    {
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public string Parity { get; set; }
        public int DataBits { get; set; }
        public string StopBits { get; set; }
        public string Handshake { get; set; }
        public bool AutoConnect { get; set; } = false;

        public ConfigModel()
        {
            ComPort = "COM1";
            BaudRate = 1200;
            Parity = "Even";
            DataBits = 7;
            StopBits = "1";
            Handshake = "None";
            AutoConnect = false;
        }

    }

}
