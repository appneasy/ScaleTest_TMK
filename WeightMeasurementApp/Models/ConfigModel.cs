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

        // ✨ เพิ่มตัวแปรสำหรับการตั้งค่ารูปแบบข้อมูลน้ำหนัก
        public int WeightStartPosition { get; set; }
        public int WeightEndPosition { get; set; }
        public int WeightDigits { get; set; }
        public int WeightStableDelay { get; set; } // เวลาที่ต้องนิ่งเพื่อถือว่าเสถียร (ms)
        public double WeightMaxValue { get; set; }
        public double WeightMinValue { get; set; }

        public ConfigModel()
        {
            ComPort = "COM1";
            BaudRate = 1200;
            Parity = "Even";
            DataBits = 7;
            StopBits = "1";
            Handshake = "None";
            AutoConnect = false;

            // ✨ กำหนดค่าเริ่มต้นสำหรับการตั้งค่ารูปแบบข้อมูลน้ำหนัก
            WeightStartPosition = 5;
            WeightEndPosition = 13;
            WeightDigits = 6;
            WeightStableDelay = 500; // 1.5 วินาที  
            WeightMaxValue = 80000.0;
            WeightMinValue = 5.0; // ✨ เพิ่มค่าเริ่มต้น  
        }

    }

}
