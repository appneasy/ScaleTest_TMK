using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WeightMeasurementApp.Models;

namespace WeightMeasurementApp
{
    public class SmartLogic
    {
        #region ตัวแปรสำหรับการจัดการข้อมูลและสถานะ

        // Logger และข้อมูลพื้นฐาน
        private ConsoleLogger logger;
        private const double MIN_VALID_WEIGHT = 5.0;     // น้ำหนักต่ำสุดที่ถือว่าถูกต้อง (kg)
        private const double MAX_VALID_WEIGHT = 80000.0; // น้ำหนักสูงสุดที่ถือว่าถูกต้อง (kg)

        // ข้อมูลเกี่ยวกับ noise และความเสถียร
        //private double meanNoise = 0;
        //private double stdDevNoise = 0;
        private List<double> noiseData = new List<double>();
        private double previousWeight = 0; // Yes
        private DateTime lastCheckTime = DateTime.MinValue;

        // ข้อมูลเกี่ยวกับความนิ่งและรูปแบบข้อมูล
        private bool isStable = false;  //yes
        private DateTime? lastStableTime = null; //yes
        private const double NOISE_THRESHOLD = 100.0;     // ความแตกต่างที่ยอมรับได้ในการพิจารณาว่าเป็น noise
        private const int STABLE_TIME_SECONDS = 1;       // เวลาที่ต้องนิ่งเพื่อถือว่าเสถียร (วินาที) yes

        // ตัวแปรสำหรับการเรียนรู้รูปแบบข้อมูล
        private List<DataPattern> patternHistory = new List<DataPattern>();
        private Dictionary<int, int> weightPositionFrequency = new Dictionary<int, int>();
        //private int mostLikelyWeightPosition = -1;


        // ตัวแปรสำหรับตรวจจับ noise
        //private int noiseDetectionCount = 0;

        // ใช้ฟังก์ชั่นสำหรับจำกัดการเปลี่ยนแปลงแทนค่าคงที่
        private double GetMaxRateOfChange(double currentWeight)
        {
            // น้ำหนักน้อย จำกัดการเปลี่ยนแปลงน้อย
            if (currentWeight < 100) return 150; // เพิ่มจาก 100 เป็น 150 เพื่อให้รองรับการเปลี่ยนแปลงมากขึ้น
            // น้ำหนักปานกลาง
            if (currentWeight < 1000) return 300;
            // น้ำหนักมาก
            if (currentWeight < 10000) return Math.Max(500, currentWeight * 0.2);
            // น้ำหนักมากมาก (รถบรรทุก)
            return Math.Max(2000, currentWeight * 0.3); // เพิ่มขึ้นจาก 0.5 เป็น 0.3 เพื่อรองรับการเปลี่ยนแปลงหนัก
        }

        // ประวัติการวัดน้ำหนัก
        private const int HISTORY_SIZE = 15; // เพิ่มจาก 10 เป็น 15 เพื่อให้มีข้อมูลมากขึ้นในการวิเคราะห์
        private List<WeightSample> weightHistory = new List<WeightSample>();


        private readonly ConfigModel _config; // ✨ เพิ่มฟิลด์สำหรับเก็บ ConfigModel
        private readonly Queue<double> _recentWeights = new Queue<double>();
        private readonly Queue<DateTime> _recentWeightTimes = new Queue<DateTime>();

        #endregion

        #region คลาสสำหรับจัดเก็บข้อมูล

        /// <summary>
        /// คลาสสำหรับเก็บข้อมูลรูปแบบของข้อมูลดิบและตำแหน่งของค่าน้ำหนัก
        /// </summary>
        private class DataPattern
        {
            public string RawData { get; set; }           // ข้อมูลดิบทั้งหมด
            public double Weight { get; set; }            // น้ำหนักที่พบ
            public int WeightPosition { get; set; }       // ตำแหน่งของน้ำหนักในข้อมูลดิบ
            public string PatternBeforeWeight { get; set; } = string.Empty; // รูปแบบข้อความก่อนตัวเลขน้ำหนัก
            public string PatternAfterWeight { get; set; } = string.Empty;  // รูปแบบข้อความหลังตัวเลขน้ำหนัก

            public DataPattern(string rawData, double weight, int weightPosition)
            {
                RawData = rawData;
                Weight = weight;
                WeightPosition = weightPosition;

                if (weightPosition >= 0)
                {
                    string weightString = weight.ToString();
                    int endPos = weightPosition + weightString.Length;

                    // เก็บข้อความก่อนและหลังตัวเลขน้ำหนัก เพื่อใช้ในการค้นหาตำแหน่งต่อไป
                    PatternBeforeWeight = weightPosition > 0 ? rawData.Substring(0, weightPosition) : "";
                    PatternAfterWeight = endPos < rawData.Length ? rawData.Substring(endPos) : "";
                }
            }
        }

        /// <summary>
        /// คลาสสำหรับเก็บข้อมูลน้ำหนักและเวลาที่วัด
        /// </summary>
        private class WeightSample
        {
            public double Weight { get; set; }       // ค่าน้ำหนัก
            public DateTime Timestamp { get; set; }  // เวลาที่วัด
            public bool IsFiltered { get; set; }     // เพิ่มฟิลด์สำหรับบอกว่าเป็นค่าที่ผ่านการกรองหรือยัง
            public string Source { get; set; }       // เพิ่มฟิลด์บอกแหล่งที่มาของค่าน้ำหนัก

            public WeightSample(double weight, bool isFiltered = false, string source = "unknown")
            {
                Weight = weight;
                Timestamp = DateTime.Now;
                IsFiltered = isFiltered;
                Source = source;
            }
        }

        #endregion

        #region Constructor และฟังก์ชันหลัก

        /// <summary>
        /// สร้างอินสแตนซ์ใหม่ของ SmartLogic
        /// </summary>
        public SmartLogic(ConsoleLogger logger, ConfigModel config)
        {
            this.logger = logger;
            //lastCheckTime = DateTime.Now;
            _config = config; // ✨ เก็บ ConfigModel ไว้ใช้งาน
            LogMessage("SmartLogic initialized with improved weight handling");
        }

       

        public double ProcessRawData(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData)) return 0;

            lastRawData = rawData;
            LogMessage($"Raw Data for Processing: \"{rawData}\" (Length: {rawData.Length})");

            double extractedWeight = ExtractWeightFromPattern(rawData);
            if (extractedWeight == -1)
            {
                LogMessage("❌ ไม่พบค่าน้ำหนักที่เชื่อถือได้จาก ASCII pattern");
                return previousWeight > 0 ? previousWeight : 0;
            }

            previousWeight = extractedWeight;
            return extractedWeight;
        }


        /// <summary>
        /// ตรวจสอบว่าน้ำหนักนิ่งหรือไม่
        /// </summary>
        public bool IsStable()
        {
            return isStable;
        }

        /// <summary>
        /// ตรวจสอบว่าน้ำหนักเป็น noise หรือไม่
        /// </summary>
        public double GetNoiseThreshold(double currentWeight)
        {
            if (currentWeight < 1000)
                return 100;  // น้ำหนักน้อย → ไม่ให้เปลี่ยนไว
            else if (currentWeight < 5000)
                return 300;  // ปานกลาง
            else if (currentWeight < 20000)
                return 800;  // น้ำหนักมากขึ้น → ยอมรับการพุ่งได้
            else
                return 2000; // รถบรรทุก → เปลี่ยนเร็ว ยอมรับการพุ่งระดับตัน
        }

        // แทนที่ด้วยฟังก์ชัน IsNoise ที่เรียบง่ายกว่า:
        public bool IsNoise(double weight)
        {
            // กรณีไม่มีน้ำหนัก
            if (weight <= 0)
            {
                return false;
            }

            // กรณีที่ยังไม่มีน้ำหนักอ้างอิง
            if (previousWeight <= 0)
            {
                return false;
            }

            // ให้ threshold ขึ้นอยู่กับน้ำหนักปัจจุบัน
            double dynamicThreshold = GetNoiseThreshold(previousWeight);

            // ตรวจสอบแบบพื้นฐาน: น้ำหนักเปลี่ยนแปลงเร็วเกินไป
            double change = Math.Abs(weight - previousWeight);
            if (change > dynamicThreshold)
            {
                LogMessage($"Noise detected: Weight change too rapid ({change} > {dynamicThreshold})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// จัดการน้ำหนักขนาดใหญ่ (>1000) เป็นพิเศษ
        /// </summary>
        private double HandleLargeWeight(double filteredWeight, double extractedWeight, string method)
        {
            // ตรวจสอบว่าน้ำหนักมีลักษณะของความผิดพลาดที่พบบ่อยในน้ำหนักสูง

            // 1. ตรวจสอบการแสดงผิดจากการอ่านตัวเลขที่มีทศนิยมหายไป (เช่น 1000.0 อ่านเป็น 10000)
            if (extractedWeight > 10000 && previousWeight > 0 && previousWeight < extractedWeight / 2)
            {
                // ถ้ามีความแตกต่างมากเกินไป และอาจมีปัญหาทศนิยม
                string extractedStr = extractedWeight.ToString();
                if (extractedStr.Length > 4 && !extractedStr.Contains("."))
                {
                    // ทดลองใส่จุดทศนิยมดูหลายจุด แล้วดูว่าอันไหนใกล้ค่าก่อนหน้ามากที่สุด
                    List<double> potentialValues = new List<double>();
                    for (int i = 1; i < extractedStr.Length; i++)
                    {
                        string testStr = extractedStr.Insert(i, ".");
                        if (double.TryParse(testStr, out double testValue))
                        {
                            potentialValues.Add(testValue);
                        }
                    }

                    // เลือกค่าที่ใกล้เคียงกับค่าก่อนหน้ามากที่สุด
                    if (potentialValues.Count > 0)
                    {
                        double closestValue = potentialValues
                            .OrderBy(v => Math.Abs(v - previousWeight))
                            .First();

                        if (Math.Abs(closestValue - previousWeight) < Math.Abs(extractedWeight - previousWeight) * 0.5)
                        {
                            LogMessage($"Corrected decimal point issue: {extractedWeight} -> {closestValue}");
                            return closestValue;
                        }
                    }
                }
            }

            // 2. ตรวจสอบปัญหาตัวเลขซ้ำซ้อน (เช่น 1000 อ่านเป็น 10001000)
            string weightStr = extractedWeight.ToString();
            if (weightStr.Length > 6)
            {
                // หาตัวเลขซ้ำซ้อน
                for (int i = 1; i <= weightStr.Length / 2; i++)
                {
                    string firstPart = weightStr.Substring(0, i);
                    string restPart = weightStr.Substring(i);

                    if (restPart.StartsWith(firstPart))
                    {
                        if (double.TryParse(firstPart, out double potentialValue) &&
                            potentialValue > MIN_VALID_WEIGHT)
                        {
                            LogMessage($"Detected duplicate digits: {extractedWeight} -> {potentialValue}");
                            return potentialValue;
                        }
                    }
                }
            }

            // 3. ตรวจสอบการขาดหายของตัวเลข 0 หรือสลับตำแหน่ง
            if (previousWeight > 0 && previousWeight > 1000 &&
                Math.Abs(filteredWeight - previousWeight) > previousWeight * 0.5)
            {
                // ตรวจสอบความสมเหตุสมผลของน้ำหนักโดยใช้ข้อมูลในอดีต
                if (weightHistory.Count >= 3)
                {
                    var recentWeights = weightHistory.TakeLast(3)
                        .Where(w => w.IsFiltered)
                        .Select(w => w.Weight)
                        .ToList();

                    if (recentWeights.Count > 0)
                    {
                        double avgRecent = recentWeights.Average();

                        // ถ้าความแตกต่างมากเกินไป ทดลองปรับค่า
                        if (Math.Abs(filteredWeight - avgRecent) > avgRecent * 0.5)
                        {
                            // ทดลองแก้ไขปัญหาด้วยวิธีต่างๆ
                            // 3.1 ทดลองเพิ่ม/ลบ 0 ในตำแหน่งต่างๆ
                            List<double> candidates = new List<double>();

                            string weightAsStr = filteredWeight.ToString();
                            // เพิ่ม 0 ในตำแหน่งต่างๆ
                            for (int i = 0; i <= weightAsStr.Length; i++)
                            {
                                string withZero = weightAsStr.Insert(i, "0");
                                if (double.TryParse(withZero, out double candidate))
                                {
                                    candidates.Add(candidate);
                                }
                            }

                            // ลบตัวเลขที่อาจเป็น noise
                            for (int i = 0; i < weightAsStr.Length; i++)
                            {
                                string withoutDigit = weightAsStr.Remove(i, 1);
                                if (double.TryParse(withoutDigit, out double candidate))
                                {
                                    candidates.Add(candidate);
                                }
                            }

                            // ทดลองสลับตำแหน่งตัวเลขที่อยู่ติดกัน
                            for (int i = 0; i < weightAsStr.Length - 1; i++)
                            {
                                char[] chars = weightAsStr.ToCharArray();
                                char temp = chars[i];
                                chars[i] = chars[i + 1];
                                chars[i + 1] = temp;

                                string swapped = new string(chars);
                                if (double.TryParse(swapped, out double candidate))
                                {
                                    candidates.Add(candidate);
                                }
                            }

                            // เลือกค่าที่ใกล้เคียงกับค่าเฉลี่ยล่าสุดมากที่สุด
                            if (candidates.Count > 0)
                            {
                                double bestCandidate = candidates
                                    .OrderBy(c => Math.Abs(c - avgRecent))
                                    .First();

                                if (Math.Abs(bestCandidate - avgRecent) < Math.Abs(filteredWeight - avgRecent) * 0.5)
                                {
                                    LogMessage($"Corrected large weight issue: {filteredWeight} -> {bestCandidate}");
                                    return bestCandidate;
                                }
                            }
                        }
                    }
                }
            }

    
            return filteredWeight;
        }

        #endregion

        #region ฟังก์ชันสำหรับการจัดการก้าวกระโดดและสกัดน้ำหนัก

        /// <summary>
        /// จัดการกับการเปลี่ยนแปลงน้ำหนักแบบก้าวกระโดด
        /// </summary>
        //version 4 10.22 01/05/2025
        private double lastDisplayedWeight = 0;
        private DateTime lastWeightTime = DateTime.MinValue;




        //}

        //public double ExtractWeightFromPattern(string rawData)
        //{
        //    if (string.IsNullOrEmpty(rawData))
        //    {
        //        logger?.Log("Raw data is null or empty.");
        //        return 0;
        //    }

        //    int startPos = _config.WeightStartPosition;
        //    int endPos = _config.WeightEndPosition;
        //    int expectedDigits = _config.WeightDigits;
        //    double maxWeight = _config.WeightMaxValue;
        //    double minWeight = _config.WeightMinValue;

        //    if (rawData.Length < endPos)
        //    {
        //        logger?.Log($"Raw data is too short. Expected at least {endPos} characters but got {rawData.Length}. Raw data: '{rawData}'");
        //        return 0;
        //    }

        //    try
        //    {
        //        logger?.Log($"Processing raw data: '{rawData}'");
        //        string weightStr = rawData.Substring(startPos, endPos - startPos).Trim();
        //        weightStr = Regex.Replace(weightStr, "[^0-9.]", "");

        //        if (string.IsNullOrEmpty(weightStr))
        //        {
        //            logger?.Log("Weight string is empty after cleaning.");
        //            return 0;
        //        }

        //        string pattern = $"^\\d{{1,{_config.WeightDigits}}}(\\.\\d{{0,3}})?$";
        //        if (!Regex.IsMatch(weightStr, pattern))
        //        {
        //            logger?.Log($"Weight '{weightStr}' does not match expected pattern: {pattern}");
        //            return 0;
        //        }


        //        if (double.TryParse(weightStr, out double weight))
        //        {
        //            if (weight > maxWeight || (weight < minWeight && weight != 0))
        //            {
        //                logger?.Log($"Weight {weight} out of range.");
        //                return 0;
        //            }

        //            // ✨ เพิ่ม logic ตอบสนองไวเมื่อ jump
        //            var now = DateTime.Now;
        //            double jumpThreshold = 100; // หรือกำหนดผ่าน config ก็ได้

        //            if (Math.Abs(weight - lastDisplayedWeight) >= jumpThreshold)
        //            {
        //                logger?.Log($"Jump detected → update immediately: {lastDisplayedWeight} → {weight}");
        //                lastDisplayedWeight = weight;
        //                lastWeightTime = now;
        //                return weight;
        //            }

        //            // ✨ ใช้ Delay ตามปกติ
        //            if (Math.Abs(weight - lastDisplayedWeight) < 0.01)
        //            {
        //                if ((now - lastWeightTime).TotalMilliseconds >= _config.WeightStableDelay)
        //                {
        //                    logger?.Log($"Stable delay passed → update: {weight}");
        //                    lastDisplayedWeight = weight;
        //                    return weight;
        //                }
        //            }
        //            else
        //            {
        //                lastWeightTime = now; // reset timer
        //            }

        //            return lastDisplayedWeight; // ยังไม่ถึง delay, แสดงค่าเดิม
        //        }
        //        else
        //        {
        //            logger?.Log($"Failed to parse: '{weightStr}'");
        //            return 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger?.Log($"Exception: {ex.Message}");
        //        return 0;
        //    }
        //}
        ////version 2
        //public double ExtractWeightFromPattern(string rawData)
        //{

        //    if (string.IsNullOrEmpty(rawData))
        //    {
        //        logger?.Log("Raw data is null or empty.");
        //        return 0;
        //    }

        //    int startPos = _config.WeightStartPosition;
        //    int endPos = _config.WeightEndPosition;
        //    int expectedDigits = _config.WeightDigits;
        //    double maxWeight = _config.WeightMaxValue;
        //    double minWeight = _config.WeightMinValue;

        //    // 🆕 เรียกฟังก์ชันแนะนำตำแหน่งตัวเลข
        //    LogSuggestedWeightRange(rawData);

        //    if (rawData.Length < endPos)
        //    {
        //        logger?.Log($"Raw data is too short. Expected at least {endPos} characters but got {rawData.Length}. Raw data: '{rawData}'");
        //        return 0;
        //    }

        //    try
        //    {
        //        logger?.Log($"Processing raw data: '{rawData}'");


        //        // 🧪 เพิ่ม log ตรวจสอบตำแหน่งที่ตัดและผลลัพธ์ที่ได้จาก Substring
        //        string weightRawSegment = rawData.Substring(startPos, endPos - startPos);
        //        logger?.Log($"[DEBUG] Substring({startPos}, {endPos - startPos}) = '{weightRawSegment}'");

        //        string weightStr = weightRawSegment.Trim();

        //        // 🧪 Log ก่อนและหลังการ clean ด้วย Regex
        //        logger?.Log($"[DEBUG] Before cleaning: '{weightStr}'");
        //        weightStr = Regex.Replace(weightStr, "[^0-9.]", "");
        //        logger?.Log($"[DEBUG] After cleaning: '{weightStr}'");

        //        if (string.IsNullOrEmpty(weightStr))
        //        {
        //            logger?.Log("Weight string is empty after cleaning.");
        //            return 0;
        //        }

        //        string pattern = $"^\\d{{1,{_config.WeightDigits}}}(\\.\\d{{0,3}})?$";

        //        // 🧪 Log การตรวจสอบ Regex pattern และผลลัพธ์
        //        logger?.Log($"[DEBUG] Regex pattern: '{pattern}', checking '{weightStr}'");
        //        if (!Regex.IsMatch(weightStr, pattern))
        //        {
        //            logger?.Log($"Weight '{weightStr}' does not match expected pattern: {pattern}");
        //            return 0;
        //        }

        //        if (double.TryParse(weightStr, out double weight))
        //        {
        //            if (weight > maxWeight || (weight < minWeight && weight != 0))
        //            {
        //                logger?.Log($"Weight {weight} out of range.");
        //                return 0;
        //            }

        //            // ✨ เพิ่ม logic ตอบสนองไวเมื่อ jump
        //            var now = DateTime.Now;
        //            double jumpThreshold = 100; // หรือกำหนดผ่าน config ก็ได้
        //            if (Math.Abs(weight - lastDisplayedWeight) >= jumpThreshold)
        //            {
        //                logger?.Log($"Jump detected → update immediately: {lastDisplayedWeight} → {weight}");
        //                lastDisplayedWeight = weight;
        //                lastWeightTime = now;
        //                return weight;
        //            }

        //            // ✨ ใช้ Delay ตามปกติ
        //            if (Math.Abs(weight - lastDisplayedWeight) < 0.01)
        //            {
        //                if ((now - lastWeightTime).TotalMilliseconds >= _config.WeightStableDelay)
        //                {
        //                    logger?.Log($"Stable delay passed → update: {weight}");
        //                    lastDisplayedWeight = weight;
        //                    return weight;
        //                }
        //            }
        //            else
        //            {
        //                lastWeightTime = now; // reset timer
        //            }

        //            return lastDisplayedWeight; // ยังไม่ถึง delay, แสดงค่าเดิม
        //        }
        //        else
        //        {
        //            logger?.Log($"Failed to parse: '{weightStr}'");
        //            return 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger?.Log($"Exception: {ex.Message}");
        //        return 0;
        //    }
        //}

        //version 3
        public double ExtractWeightFromPattern(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
            {
                logger?.Log("Raw data is null or empty.");
                return 0;
            }

            int startPos = _config.WeightStartPosition;
            int endPos = _config.WeightEndPosition;
            int expectedDigits = _config.WeightDigits;
            double maxWeight = _config.WeightMaxValue;
            double minWeight = _config.WeightMinValue;

            // 🆕 เรียกฟังก์ชันแนะนำตำแหน่งตัวเลข
            LogSuggestedWeightRange(rawData);

            if (rawData.Length < endPos)
            {
                logger?.Log($"Raw data is too short. Expected at least {endPos} characters but got {rawData.Length}. Raw data: '{rawData}'");
                return 0;
            }

            try
            {
                logger?.Log($"Processing raw data: '{rawData}'");
                string weightStr = rawData.Substring(startPos, endPos - startPos).Trim();

                // ✅ ล็อกก่อนทำความสะอาด
                logger?.Log($"Weight string candidate before clean: '{weightStr}'");

                weightStr = Regex.Replace(weightStr, "[^0-9.]", "");

                if (string.IsNullOrEmpty(weightStr))
                {
                    logger?.Log("Weight string is empty after cleaning.");
                    return 0;
                }

                string pattern = $"^\\d{{1,{expectedDigits}}}(\\.\\d{{0,3}})?$";
                logger?.Log($"Checking regex match: '{weightStr}' vs pattern: {pattern}");

                if (!Regex.IsMatch(weightStr, pattern))
                {
                    logger?.Log($"Weight '{weightStr}' does not match expected pattern: {pattern}");
                    return 0;
                }

                if (double.TryParse(weightStr, out double weight))
                {
                    if (weight > maxWeight || (weight < minWeight && weight != 0))
                    {
                        logger?.Log($"Weight {weight} out of range.");
                        return 0;
                    }

                    var now = DateTime.Now;
                    double jumpThreshold = 100;

                    if (Math.Abs(weight - lastDisplayedWeight) >= jumpThreshold)
                    {
                        logger?.Log($"Jump detected → update immediately: {lastDisplayedWeight} → {weight}");
                        lastDisplayedWeight = weight;
                        lastWeightTime = now;
                        return weight;
                    }

                    if (Math.Abs(weight - lastDisplayedWeight) < 0.01)
                    {
                        if ((now - lastWeightTime).TotalMilliseconds >= _config.WeightStableDelay)
                        {
                            logger?.Log($"Stable delay passed → update: {weight}");
                            lastDisplayedWeight = weight;
                            return weight;
                        }
                    }
                    else
                    {
                        lastWeightTime = now;
                    }

                    return lastDisplayedWeight;
                }
                else
                {
                    logger?.Log($"Failed to parse: '{weightStr}'");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger?.Log($"Exception: {ex.Message}");
                return 0;
            }
        }


        //สำหรับรับค่า rawData เพื่อมาระบุตำแหน่ง อักขระเริ่มต้นและสิ้นสุด version 1
        // private void LogSuggestedWeightRange(string rawData)
        //{
        //    // ค้นหาช่วงของตัวเลขต่อเนื่องที่ยาวพอสมควร (เช่น 4 หลักขึ้นไป)
        //    var matches = Regex.Matches(rawData, @"\d{4,}");

        //    if (matches.Count == 0)
        //    {
        //        logger?.Log("[DEBUG] ไม่พบลำดับตัวเลขที่มีความยาว >= 4 ใน rawData.");
        //        return;
        //    }

        //    foreach (Match match in matches)
        //    {
        //        int start = match.Index;
        //        int end = match.Index + match.Length;
        //        string val = match.Value;
        //        logger?.Log($"[SUGGESTION] พบเลข '{val}' ที่ตำแหน่ง [{start}–{end}] → แนะนำให้ใช้ startPos = {start}, endPos = {end}");
        //    }
        //}
        //สำหรับรับค่า rawData เพื่อมาระบุตำแหน่ง อักขระเริ่มต้นและสิ้นสุด version 2 ปรับให้ยืดหยุ่น ในการค้นหา digit 
        //private void LogSuggestedWeightRange(string rawData)
        //{
        //    if (string.IsNullOrEmpty(rawData)) return;

        //    int expectedDigits = _config.WeightDigits;
        //    double minWeight = _config.WeightMinValue;
        //    double maxWeight = _config.WeightMaxValue;

        //    // ค้นหาตัวเลขที่มีความยาวตั้งแต่ 2 ถึง expectedDigits (เช่น 2–5 หลัก)
        //    var matches = Regex.Matches(rawData, @"\d{2," + expectedDigits + "}");

        //    if (matches.Count == 0)
        //    {
        //        logger?.Log("📌 ไม่พบตัวเลขที่เข้าข่ายน้ำหนัก (2-" + expectedDigits + " หลัก)");
        //        return;
        //    }

        //    logger?.Log($"🔍 พบตัวเลขที่อาจเป็นน้ำหนัก {matches.Count} ตำแหน่ง:");
        //    foreach (Match match in matches)
        //    {
        //        string numStr = match.Value;
        //        int start = match.Index;
        //        int end = start + numStr.Length;

        //        if (double.TryParse(numStr, out double value))
        //        {
        //            bool inRange = value >= minWeight && value <= maxWeight;
        //            string status = inRange ? "✅" : "❌";
        //            logger?.Log($"{status} '{numStr}' ที่ตำแหน่ง [{start}–{end}] ⇒ {value} {(inRange ? "อยู่ในช่วง" : "นอกช่วง")}");
        //        }
        //        else
        //        {
        //            logger?.Log($"❌ '{numStr}' ที่ตำแหน่ง [{start}–{end}] ⇒ ไม่สามารถแปลงเป็นตัวเลขได้");
        //        }
        //    }
        //}
        //version 3
        // 🔄 ปรับปรุงฟังก์ชัน LogSuggestedWeightRange เพื่อแนะนำ startPos, endPos ที่ครอบคลุมช่วงตัวเลข
        private void LogSuggestedWeightRange(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData)) return;

            var matches = Regex.Matches(rawData, "[0-9]{2,6}"); // ✅ ค้นหาตัวเลขเรียงติดกันความยาว 2–6 หลัก
            foreach (Match match in matches)
            {
                int start = match.Index;
                int end = match.Index + match.Length - 1;
                string digits = match.Value;
                logger?.Log($"[แนะนำช่วงตำแหน่ง] พบกลุ่มตัวเลข \"{digits}\" ที่ตำแหน่ง {start} ถึง {end}");
            }

            if (matches.Count == 0)
            {
                logger?.Log("[แนะนำช่วงตำแหน่ง] ไม่พบกลุ่มตัวเลขอย่างน้อย 2 หลักในข้อมูลที่ได้รับ");
            }
        }


        public bool IsReadingStable(double newWeight)
        {
            bool isStable = false;

            // เก็บค่าน้ำหนักเข้า List สำหรับตรวจสอบความนิ่ง
            _recentWeights.Enqueue(newWeight);
            _recentWeightTimes.Enqueue(DateTime.Now);

            // เก็บค่าไว้เฉพาะช่วงเวลาตามที่กำหนด
            int stableDelay = _config.WeightStableDelay;
            while (_recentWeightTimes.Count > 0 &&
                   DateTime.Now - _recentWeightTimes.Peek() > TimeSpan.FromMilliseconds(stableDelay))
            {
                _recentWeights.Dequeue();
                _recentWeightTimes.Dequeue();
            }

            if (_recentWeights.Count == 0)
                return false;

            // ✅ ตรวจสอบว่าน้ำหนักนิ่งคงที่ ตามค่า WeightStableDelay ในการตั้งค่า  
            double minWeight = _recentWeights.Min();
            double maxWeight = _recentWeights.Max();
            double weightDiff = maxWeight - minWeight;

            isStable = weightDiff <= 0.01 * minWeight;

            if (isStable)
                logger?.Log($"Weight is stable at {newWeight}. Min: {minWeight}, Max: {maxWeight}, Diff: {weightDiff}");

            return isStable;
        }

        #endregion
        #region ฟังก์ชันสำหรับการเรียนรู้และการจัดการความเสถียร


        /// <summary>
        /// เพิ่มข้อมูลน้ำหนักลงในประวัติ
        /// </summary>
        private void UpdateWeightHistory(double weight, bool isFiltered = false, string source = "unknown")
        {
            weightHistory.Add(new WeightSample(weight, isFiltered, source));

            // รักษาขนาดประวัติไม่ให้เกินที่กำหนด
            if (weightHistory.Count > HISTORY_SIZE)
            {
                weightHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// อัพเดทสถานะความนิ่งของน้ำหนัก
        /// </summary>
        private string? lastRawData = null; // เก็บข้อมูลดิบล่าสุด

    
        private void UpdateStability(double weight)
        {
            DateTime currentTime = DateTime.Now;

            // ถ้าน้ำหนักเปลี่ยนแปลงเกินเกณฑ์ที่กำหนด
            double stabilityThreshold = Math.Max(5.0, weight * 0.01); // อย่างน้อย 5kg หรือ 1% ของน้ำหนักปัจจุบัน

            // สำหรับน้ำหนักที่มากกว่า 10000 ให้ใช้เกณฑ์ที่สูงขึ้น
            if (weight > 10000)
            {
                stabilityThreshold = Math.Max(20.0, weight * 0.01); // อย่างน้อย 20kg หรือ 1% สำหรับน้ำหนักมาก
            }

            if (previousWeight > 0 && Math.Abs(weight - previousWeight) >= stabilityThreshold)
            {
                // รีเซ็ตความนิ่ง
                lastStableTime = null;
                isStable = false;
                LogMessage($"Weight changed significantly: {previousWeight} -> {weight}, reset stability");
            }
            else
            {
                // น้ำหนักอยู่ในช่วงที่ยอมรับได้
                if (lastStableTime == null)
                {
                    // เริ่มจับเวลาความนิ่ง
                    lastStableTime = currentTime;
                    LogMessage("Started stability timer");
                }
                else
                {
                    // ตรวจสอบว่านิ่งครบเวลาที่กำหนดหรือไม่
                    TimeSpan stableTime = currentTime - lastStableTime.Value;

                    // ปรับเวลาความนิ่งตามน้ำหนัก - น้ำหนักมากต้องนิ่งนานกว่า
                    int requiredStableTime = STABLE_TIME_SECONDS;
                    if (weight > 10000) requiredStableTime = 2; // น้ำหนักมากต้องนิ่ง 2 วินาที

                    // ถ้านิ่งครบตามเวลาที่กำหนด ให้เปลี่ยนสถานะเป็นนิ่ง
                    if (stableTime.TotalSeconds >= requiredStableTime && !isStable)
                    {
                        isStable = true;
                        LogMessage($"Weight stable for {stableTime.TotalSeconds:F1} seconds, marked as stable");


                        // บังคับใช้น้ำหนักจริงล่าสุดที่นิ่งแล้ว
                        var finalWeight = Math.Round(weight, 1);  // ใช้ weight ที่ได้รับเมื่อมันนิ่ง
                        LogMessage($"🔒 น้ำหนักนิ่ง: ใช้น้ำหนักสุดท้าย = {finalWeight}");
                    }
                    else
                    {
                        LogMessage($"Weight stable for {stableTime.TotalSeconds:F1} seconds, needs {requiredStableTime} to be marked stable");
                    }
                }
            }

            // เพิ่มการรีเซ็ตความนิ่งตามเวลา - ป้องกันค้างที่ความนิ่ง
            if (isStable && lastStableTime.HasValue)
            {
                TimeSpan stableElapsedTime = currentTime - lastStableTime.Value;
                // ถ้านิ่งนานเกิน 30 วินาที ให้รีเซ็ตสถานะเพื่อบังคับตรวจสอบใหม่
                if (stableElapsedTime.TotalSeconds > 30)
                {
                    isStable = false;
                    lastStableTime = null;
                    LogMessage("รีเซ็ตสถานะความนิ่งเนื่องจากเวลาผ่านไปนาน");
                }
            }
        }
            

        #endregion

       // ฟังก์ชันสำหรับการกรองสัญญาณและฟังก์ชันสนับสนุน  
        /// <summary>
        /// บันทึกข้อความลงใน console และ log file
        /// </summary>
        private void LogMessage(string message)
        {
            logger?.Log(message);
        }      
    }
}

