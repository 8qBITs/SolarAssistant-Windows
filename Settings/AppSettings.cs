using System;
using System.Globalization;
using System.IO;

namespace SolarTray.Settings
{
    public static class AppSettings
    {
        public static string FilePath { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        // MQTT
        public static string BrokerAddress { get; set; } = "192.168.0.32";
        public static int BrokerPort { get; set; } = 1883;

        // Overlay
        public static bool ShowOverlay { get; set; } = true;

        // Battery alerts
        public static bool BatteryHighAlertEnabled { get; set; } = false;
        public static bool BatteryLowAlertEnabled { get; set; } = false;

        public static double BatteryHighThreshold { get; set; } = 95.0;
        public static double BatteryLowThreshold { get; set; } = 20.0;

        public static string BatteryHighMessage { get; set; } = "Battery is full";
        public static string BatteryLowMessage { get; set; } = "Battery is low";

        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                Save();
                return;
            }

            foreach (var line in File.ReadAllLines(FilePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim().ToLowerInvariant();
                var val = parts[1].Trim();

                switch (key)
                {
                    case "address":
                        BrokerAddress = val;
                        break;

                    case "port":
                        if (int.TryParse(val, out var p)) BrokerPort = p;
                        break;

                    case "show_overlay":
                        ShowOverlay = ParseBoolLoose(val, ShowOverlay);
                        break;

                    case "battery_high_enabled":
                        BatteryHighAlertEnabled = ParseBoolLoose(val, BatteryHighAlertEnabled);
                        break;

                    case "battery_low_enabled":
                        BatteryLowAlertEnabled = ParseBoolLoose(val, BatteryLowAlertEnabled);
                        break;

                    case "battery_high_threshold":
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var high))
                            BatteryHighThreshold = high;
                        break;

                    case "battery_low_threshold":
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var low))
                            BatteryLowThreshold = low;
                        break;

                    case "battery_high_message":
                        BatteryHighMessage = val;
                        break;

                    case "battery_low_message":
                        BatteryLowMessage = val;
                        break;
                }
            }
        }

        public static void Save()
        {
            string highTh = BatteryHighThreshold.ToString(CultureInfo.InvariantCulture);
            string lowTh = BatteryLowThreshold.ToString(CultureInfo.InvariantCulture);

            File.WriteAllLines(FilePath, new[]
            {
                "# SolarTray Configuration",
                "",
                $"address = {BrokerAddress}",
                $"port = {BrokerPort}",
                "",
                $"show_overlay = {ShowOverlay}",
                "",
                $"battery_high_enabled = {BatteryHighAlertEnabled}",
                $"battery_low_enabled = {BatteryLowAlertEnabled}",
                "",
                $"battery_high_threshold = {highTh}",
                $"battery_low_threshold = {lowTh}",
                "",
                $"battery_high_message = {BatteryHighMessage}",
                $"battery_low_message = {BatteryLowMessage}"
            });
        }

        private static bool ParseBoolLoose(string s, bool fallback)
        {
            if (bool.TryParse(s, out var b)) return b;
            if (s == "1") return true;
            if (s == "0") return false;
            return fallback;
        }
    }
}
