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
                    WeightStableDelay = 500, // 1.5 วินาที  
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
            return config;
        }


        public static void SaveConfig(ConfigModel config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
            Console.WriteLine("Config file saved successfully.");
        }
    }
}
