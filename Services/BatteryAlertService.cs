using System;
using SolarTray.Settings;

namespace SolarTray.Services
{
    public sealed class BatteryAlertService
    {
        private double? _lastSoc;

        public void Evaluate(double? previousSoc, double? currentSoc, Action<string, System.Windows.Forms.ToolTipIcon> notify)
        {
            if (!currentSoc.HasValue) return;

            var soc = currentSoc.Value;
            var prev = previousSoc ?? _lastSoc;

            if (AppSettings.BatteryLowAlertEnabled)
            {
                var low = AppSettings.BatteryLowThreshold;
                if (soc <= low && (!prev.HasValue || prev.Value > low))
                {
                    var msg = string.IsNullOrWhiteSpace(AppSettings.BatteryLowMessage)
                        ? "Battery is low."
                        : AppSettings.BatteryLowMessage;

                    notify(msg, System.Windows.Forms.ToolTipIcon.Warning);
                }
            }

            if (AppSettings.BatteryHighAlertEnabled)
            {
                var high = AppSettings.BatteryHighThreshold;
                if (soc >= high && (!prev.HasValue || prev.Value < high))
                {
                    var msg = string.IsNullOrWhiteSpace(AppSettings.BatteryHighMessage)
                        ? "Battery is full."
                        : AppSettings.BatteryHighMessage;

                    notify(msg, System.Windows.Forms.ToolTipIcon.Info);
                }
            }

            _lastSoc = soc;
        }
    }
}
