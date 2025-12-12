using System;
using System.Windows.Forms;
using SolarTray.Services;
using SolarTray.Settings;

namespace SolarTray.UI
{
    public sealed class TrayMenuFactory
    {
        public ContextMenuStrip Build(
            Action showDetails,
            Action exitApp,
            Action<bool> setOverlayEnabled,
            out ToolStripMenuItem overlayItem,
            out ToolStripMenuItem highAlertItem,
            out ToolStripMenuItem lowAlertItem,
            out ToolStripMenuItem configureThresholdsItem)
        {
            var menu = new ContextMenuStrip();

            // --- Start with Windows ---
            var startupItem = new ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true,
                Checked = StartupRegistry.IsEnabled()
            };
            startupItem.CheckedChanged += (_, __) => StartupRegistry.SetEnabled(startupItem.Checked);
            menu.Items.Add(startupItem);

            // --- Overlay toggle (use LOCAL variable, not out param, inside lambda) ---
            var overlayLocal = new ToolStripMenuItem("Show overlay (desktop text)")
            {
                CheckOnClick = true,
                Checked = AppSettings.ShowOverlay
            };
            overlayLocal.CheckedChanged += (_, __) =>
            {
                AppSettings.ShowOverlay = overlayLocal.Checked;
                AppSettings.Save();
                setOverlayEnabled(overlayLocal.Checked);
            };
            menu.Items.Add(overlayLocal);

            // --- Battery alerts submenu ---
            var batteryMenu = new ToolStripMenuItem("Battery alerts");

            var highLocal = new ToolStripMenuItem("High alert enabled")
            {
                CheckOnClick = true,
                Checked = AppSettings.BatteryHighAlertEnabled
            };
            highLocal.CheckedChanged += (_, __) =>
            {
                AppSettings.BatteryHighAlertEnabled = highLocal.Checked;
                AppSettings.Save();
            };

            var lowLocal = new ToolStripMenuItem("Low alert enabled")
            {
                CheckOnClick = true,
                Checked = AppSettings.BatteryLowAlertEnabled
            };
            lowLocal.CheckedChanged += (_, __) =>
            {
                AppSettings.BatteryLowAlertEnabled = lowLocal.Checked;
                AppSettings.Save();
            };

            var configureLocal = new ToolStripMenuItem("Configure thresholds...");

            batteryMenu.DropDownItems.Add(highLocal);
            batteryMenu.DropDownItems.Add(lowLocal);
            batteryMenu.DropDownItems.Add(new ToolStripSeparator());
            batteryMenu.DropDownItems.Add(configureLocal);

            menu.Items.Add(batteryMenu);

            // --- Footer actions ---
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Details", null, (_, __) => showDetails());
            menu.Items.Add("Exit", null, (_, __) => exitApp());

            // Assign OUT parameters at the very end (no lambdas touch these)
            overlayItem = overlayLocal;
            highAlertItem = highLocal;
            lowAlertItem = lowLocal;
            configureThresholdsItem = configureLocal;

            return menu;
        }
    }
}
