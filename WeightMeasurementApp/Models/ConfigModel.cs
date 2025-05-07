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
        public int WeightStartPosition { get; set; } = 5;  // ตำแหน่งเริ่มต้นของน้ำหนัก
        public int WeightEndPosition { get; set; } = 13;    // ตำแหน่งสิ้นสุดของน้ำหนัก
        public int WeightDigits { get; set; } = 6;         // จำนวนตัวเลขของน้ำหนัก
        public int WeightStableDelay { get; set; } = 500; // ระยะเวลาในการตรวจสอบน้ำหนักนิ่ง (ms)
        public double WeightMinValue { get; set; } = 5.0;  // น้ำหนักต่ำสุดที่ใช้งานได้
        public double WeightMaxValue { get; set; } = 80000.0; // น้ำหนักสูงสุดที่ใช้งานได้

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
