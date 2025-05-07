// ConfigManager.cs
using System;
using System.IO;
using System.Text.Json;
using WeightMeasurementApp.Models;

namespace WeightMeasurementApp.Helpers
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = "config.json";

        public static ConfigModel LoadConfig()
        {
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"Looking for config file at: {ConfigPath}");

            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine("Config file not found. Creating default config...");
                var defaultConfig = new ConfigModel
                {
                    ComPort = "COM7",
                    BaudRate = 1200,
                    Parity = "None",
                    DataBits = 8,
                    StopBits = "One",
                    Handshake = "None",
                    WeightStartPosition = 5,
                    WeightEndPosition = 13,
                    WeightDigits = 6,
                    WeightStableDelay = 500, // 0.5 วินาที  
                    WeightMaxValue = 80000.0,
                    WeightMinValue = 5.0 // ✨ เพิ่มค่าเริ่มต้น  
                };
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            Console.WriteLine("Config file found. Reading data...");
            string json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<ConfigModel>(json);
            if (config == null)
            {
                throw new InvalidOperationException("Failed to load configuration: JSON is invalid.");
            }
            ValidateAndSetDefaultValues(config);
            return config;
        }

        private static void ValidateAndSetDefaultValues(ConfigModel config)
        {
            // ตรวจสอบและตั้งค่าเริ่มต้นสำหรับฟิลด์ Weight Settings ถ้าไม่มีค่า
            if (config.WeightStartPosition < 0)
                config.WeightStartPosition = 0;

            if (config.WeightEndPosition <= config.WeightStartPosition)
                config.WeightEndPosition = config.WeightStartPosition + 8;

            if (config.WeightDigits <= 0)
                config.WeightDigits = 5;

            if (config.WeightStableDelay <= 0)
                config.WeightStableDelay = 1000;

            if (config.WeightMinValue < 0)
                config.WeightMinValue = 5.0;

            if (config.WeightMaxValue <= config.WeightMinValue)
                config.WeightMaxValue = 80000.0;
        }


        public static void SaveConfig(ConfigModel config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                Console.WriteLine("Config file saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}
