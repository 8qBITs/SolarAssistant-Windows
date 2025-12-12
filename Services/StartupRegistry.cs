using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SolarTray.Services
{
    public static class StartupRegistry
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "SolarTray";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false);
                var value = key?.GetValue(StartupValueName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch { return false; }
        }

        public static void SetEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = $"\"{Application.ExecutablePath}\"";
                    key.SetValue(StartupValueName, exePath);
                }
                else
                {
                    key.DeleteValue(StartupValueName, throwOnMissingValue: false);
                }
            }
            catch { /* ignore */ }
        }
    }
}
