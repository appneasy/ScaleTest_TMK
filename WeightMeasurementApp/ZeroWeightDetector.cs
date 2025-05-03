using System;
using System.Text.RegularExpressions;
// <summary>
/// คลาสสำหรับการตรวจจับและยืนยันค่า 0
/// </summary>
namespace WeightMeasurementApp
{
    public class ZeroWeightDetector
    {
        private ConsoleLogger logger;
        private int zeroConfirmCount = 0;
        private const int ZERO_CONFIRM_THRESHOLD = 2; // จำนวนครั้งที่ต้องพบค่า 0 ต่อเนื่องเพื่อยืนยัน
        private const double ZERO_WEIGHT_THRESHOLD = 5.0; // น้ำหนักที่ต่ำกว่านี้จะถือว่าเป็น 0
        private DateTime? firstZeroTime = null; // เวลาที่พบ 0 ครั้งแรก
        private const double ZERO_CONFIRM_TIME_SECONDS = 0.5; // เวลาในการยืนยันค่า 0 (วินาที)

        /// <summary>
        /// สร้างอินสแตนซ์ใหม่ของ ZeroWeightDetector
        /// </summary>
        /// <param name="logger">Logger สำหรับบันทึกข้อมูล</param>
        public ZeroWeightDetector(ConsoleLogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// ตรวจสอบว่าน้ำหนักควรเป็น 0 หรือไม่ โดยพิจารณาจากข้อมูลดิบและค่าน้ำหนัก
        /// </summary>
        /// <param name="rawData">ข้อมูลดิบที่ได้รับ</param>
        /// <param name="weight">น้ำหนักที่สกัดได้</param>
        /// <returns>true ถ้าควรแสดงเป็น 0, false ถ้าไม่ใช่</returns>
        public bool IsZeroWeight(string rawData, double weight)
        {
            // พิจารณาจากน้ำหนัก - ถ้าต่ำกว่าเกณฑ์ ให้ถือว่าเป็น 0
            bool isLowWeight = weight <= ZERO_WEIGHT_THRESHOLD;

            // พิจารณาจากข้อมูลดิบ - ตรวจสอบรูปแบบที่บ่งชี้ว่าเป็น 0
            bool hasZeroPattern = ContainsExplicitZero(rawData);

            // เพิ่มตัวนับเมื่อพบค่า 0 (ไม่ว่าจากน้ำหนักหรือรูปแบบ)
            if (isLowWeight || hasZeroPattern)
            {
                // เริ่มนับเวลาเมื่อพบ 0 ครั้งแรก
                if (firstZeroTime == null)
                {
                    firstZeroTime = DateTime.Now;
                    zeroConfirmCount = 1;
                    logger?.Log("เริ่มตรวจจับค่า 0 - ครั้งที่ 1");
                }
                else
                {
                    // เพิ่มตัวนับเมื่อพบค่า 0 ต่อเนื่อง
                    zeroConfirmCount++;
                    logger?.Log($"ตรวจพบค่า 0 ต่อเนื่อง - ครั้งที่ {zeroConfirmCount}");

                    // ตรวจสอบว่าเวลาผ่านไปนานพอหรือไม่
                    TimeSpan elapsedTime = DateTime.Now - firstZeroTime.Value;
                    bool timeConfirmed = elapsedTime.TotalSeconds >= ZERO_CONFIRM_TIME_SECONDS;

                    // ตรวจสอบทั้งจำนวนครั้งและเวลา
                    if (zeroConfirmCount >= ZERO_CONFIRM_THRESHOLD || timeConfirmed)
                    {
                        logger?.Log($"ยืนยันค่า 0 - ตรวจพบ {zeroConfirmCount} ครั้ง ใช้เวลา {elapsedTime.TotalSeconds:F2} วินาที");
                        return true;
                    }
                }

                // ถ้ารูปแบบชัดเจนมาก ให้ยืนยันเลยไม่ต้องรอ
                if (hasZeroPattern && IsStrongZeroPattern(rawData))
                {
                    logger?.Log("ยืนยันค่า 0 ทันที - พบรูปแบบที่ชัดเจนมาก");
                    return true;
                }

                // ถ้าค่าน้ำหนักเป็น 0 อย่างชัดเจน
                if (weight == 0)
                {
                    // ตรวจสอบเพิ่มเติมจากรูปแบบข้อมูล
                    if (hasZeroPattern)
                    {
                        logger?.Log("ยืนยันค่า 0 ทันที - น้ำหนักเป็น 0 และมีรูปแบบที่ชัดเจน");
                        return true;
                    }
                }

                return false; // รอการยืนยันต่อไป
            }
            else
            {
                // รีเซ็ตตัวนับเมื่อไม่พบค่า 0
                ResetZeroDetection();
                return false;
            }
        }

        /// <summary>
        /// รีเซ็ตการตรวจจับค่า 0
        /// </summary>
        public void ResetZeroDetection()
        {
            zeroConfirmCount = 0;
            firstZeroTime = null;
        }

        /// <summary>
        /// ตรวจสอบว่ามีการระบุค่า 0 อย่างชัดเจนในข้อมูลหรือไม่
        /// </summary>
        /// <param name="rawData">ข้อมูลดิบที่ต้องการตรวจสอบ</param>
        /// <returns>true ถ้ามีการระบุค่า 0 อย่างชัดเจน, false ถ้าไม่มี</returns>
        public bool ContainsExplicitZero(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
            {
                return false;
            }

            // กรณีที่มีการระบุ "0" อย่างชัดเจน
            if (rawData.Contains("      0") ||
                rawData.Contains("    0") ||
                rawData.Contains(" 0 ") ||
                rawData.EndsWith(" 0") ||
                rawData.Contains("p      0"))
            {
                logger?.Log("พบการระบุค่า 0 อย่างชัดเจนในข้อมูล");
                return true;
            }

            // กรณีพิเศษสำหรับรูปแบบ "(0      0     0"
            if (rawData.Contains("(0") && rawData.Contains("      0") && !HasNonZeroNumber(rawData))
            {
                logger?.Log("พบรูปแบบพิเศษที่มีแต่ค่า 0");
                return true;
            }

            // กรณีพิเศษสำหรับรูปแบบ Toledo "(X      0     Z"
            if (rawData.Contains("      ") && rawData.Length > 8)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(rawData, @"\d+");
                if (matches.Count >= 2 && matches[1].Value == "0")
                {
                    logger?.Log("พบค่า 0 ในตำแหน่งตรงกลางของรูปแบบ Toledo");
                    return true;
                }
            }

            // กรณีที่มีเฉพาะตัวเลข 0 และไม่มีตัวเลขอื่น
            var allMatches = System.Text.RegularExpressions.Regex.Matches(rawData, @"\d+");
            bool onlyZeros = true;

            foreach (System.Text.RegularExpressions.Match match in allMatches)
            {
                if (double.TryParse(match.Value, out double value) && value > 0)
                {
                    onlyZeros = false;
                    break;
                }
            }

            if (onlyZeros && allMatches.Count > 0)
            {
                logger?.Log("พบเฉพาะตัวเลข 0 ในข้อมูลดิบ ไม่มีตัวเลขอื่น");
                return true;
            }

            return false;
        }

        /// <summary>
        /// ตรวจสอบว่ามีตัวเลขที่มากกว่า 0 ในข้อมูลหรือไม่
        /// </summary>
        /// <param name="text">ข้อความที่ต้องการตรวจสอบ</param>
        /// <returns>true ถ้ามีตัวเลขที่มากกว่า 0, false ถ้าไม่มี</returns>
        private bool HasNonZeroNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\d+");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (double.TryParse(match.Value, out double value) && value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าเป็นรูปแบบค่า 0 ที่ชัดเจนมากหรือไม่
        /// </summary>
        /// <param name="rawData">ข้อมูลดิบที่ต้องการตรวจสอบ</param>
        /// <returns>true ถ้าเป็นรูปแบบค่า 0 ที่ชัดเจนมาก, false ถ้าไม่ใช่</returns>
        private bool IsStrongZeroPattern(string rawData)
        {
            // รูปแบบที่ชัดเจนมาก เช่น "(0      0     0" หรือ "p      0"
            if ((rawData.Contains("(0") && rawData.Contains("      0") && !HasNonZeroNumber(rawData)) ||
                rawData.Contains("p      0") ||
                (rawData.Contains("      0") && !HasNonZeroNumber(rawData)))
            {
                return true;
            }

            // กรณี Toledo ที่มีค่า 0 ในตำแหน่งกลาง
            if (rawData.Contains("      ") && rawData.Length > 8)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(rawData, @"\d+");
                if (matches.Count >= 2 && matches[1].Value == "0" && !HasNonZeroNumber(rawData))
                {
                    return true;
                }
            }

            return false;
        }
    }
}