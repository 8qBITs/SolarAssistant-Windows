using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SolarTray.Model;
using SolarTray.Services;
using SolarTray.Settings;

namespace SolarTray.UI
{
    public sealed class SolarTrayForm : Form
    {
        // UI
        private readonly PictureBox _overlay = new() { SizeMode = PictureBoxSizeMode.AutoSize };
        private readonly System.Windows.Forms.Timer _visibilityTimer = new() { Interval = 500 };

        // Services
        private readonly OverlayRenderer _renderer = new();
        private readonly WindowVisibilityService _visibility = new();
        private readonly BatteryAlertService _batteryAlerts = new();
        private readonly MqttSolarClient _mqtt = new();

        // Tray + menu (non-nullable)
        private readonly ContextMenuStrip _menu;
        private readonly NotifyIcon _tray;
        private readonly ToolStripMenuItem _overlayMenuItem;
        private readonly ToolStripMenuItem _highAlertItem;
        private readonly ToolStripMenuItem _lowAlertItem;

        // Data
        private SolarSnapshot _latest = new();
        private double? _previousSoc;

        public SolarTrayForm()
        {
            AppSettings.Load();

            ConfigureWindow();

            // Build menu (do not capture out vars in lambdas)
            var menuFactory = new TrayMenuFactory();

            ToolStripMenuItem configureThresholdsItem; // declare first to avoid "out capture" issues

            _menu = menuFactory.Build(
                showDetails: ShowDetails,
                exitApp: ExitApp,
                setOverlayEnabled: ApplyOverlayEnabled,
                out _overlayMenuItem,
                out _highAlertItem,
                out _lowAlertItem,
                out configureThresholdsItem
            );

            // Now safe: configureThresholdsItem is a normal local variable here
            configureThresholdsItem.Click += (_, __) =>
            {
                BatteryAlertSettingsDialog.Show(this);

                // refresh checkmarks after dialog saved settings
                _highAlertItem.Checked = AppSettings.BatteryHighAlertEnabled;
                _lowAlertItem.Checked = AppSettings.BatteryLowAlertEnabled;
            };

            ContextMenuStrip = _menu;
            _overlay.ContextMenuStrip = _menu;

            // Tray icon
            _tray = new NotifyIcon
            {
                Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solar.ico")),
                Visible = true,
                Text = "Solar: starting...",
                ContextMenuStrip = _menu
            };

            _tray.DoubleClick += (_, __) => ShowDetails();
            _tray.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    _menu.Show(Cursor.Position);
            };

            // Overlay double-click
            _overlay.DoubleClick += (_, __) => ShowDetails();
            DoubleClick += (_, __) => ShowDetails();

            // Positioning
            Shown += (_, __) => PositionOverlay();
            Resize += (_, __) => PositionOverlay();

            // Visibility timer
            _visibilityTimer.Tick += (_, __) => ApplyOverlayVisibilityRule();
            _visibilityTimer.Start();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            PositionOverlay();
            ApplyOverlayEnabled(AppSettings.ShowOverlay);

            // MQTT events (marshaled onto UI thread)
            _mqtt.Connected += () => BeginInvoke(new Action(() =>
            {
                SetOverlayBitmap(_renderer.RenderPlain("Solar: connected"));
                UpdateTrayText("Solar: connected");
            }));

            _mqtt.ConnectionFailed += msg => BeginInvoke(new Action(() =>
            {
                SetOverlayBitmap(_renderer.RenderPlain(msg));
                UpdateTrayText(msg);
            }));

            _mqtt.SnapshotUpdated += snap => BeginInvoke(new Action(() => OnSnapshot(snap)));

            try
            {
                await _mqtt.StartAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    SetOverlayBitmap(_renderer.RenderPlain("Solar: error"));
                    UpdateTrayText("Solar: error");
                    MessageBox.Show(ex.Message, "MQTT error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void ConfigureWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            DoubleBuffered = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Controls.Add(_overlay);
        }

        private void OnSnapshot(SolarSnapshot snap)
        {
            _latest = snap;

            // Battery edge detection
            _batteryAlerts.Evaluate(_previousSoc, snap.SocPercent, ShowTrayNotification);
            _previousSoc = snap.SocPercent;

            // Format values
            var pv = snap.PvKw.HasValue ? $"{snap.PvKw.Value:0.0}kW" : "N/A";
            var load = snap.LoadKw.HasValue ? $"{snap.LoadKw.Value:0.0}kW" : "N/A";
            var soc = snap.SocPercent.HasValue ? $"{snap.SocPercent.Value:0}%" : "N/A";

            var grid = "N/A";
            if (snap.GridKw.HasValue)
                grid = snap.GridKw.Value.ToString("+0.0;-0.0;0.0") + "kW";

            UpdateTrayText($"☀ PV {pv} | 🏠 Load {load} | 🔋 {soc} | ⚡ {grid}");
            SetOverlayBitmap(_renderer.RenderStatus(pv, load, soc, grid));
        }

        private void ApplyOverlayEnabled(bool enabled)
        {
            if (enabled)
            {
                Show();
                PositionOverlay();
                ApplyOverlayVisibilityRule();
            }
            else
            {
                Hide();
            }
        }

        private void ApplyOverlayVisibilityRule()
        {
            if (!AppSettings.ShowOverlay)
            {
                if (Visible) Hide();
                return;
            }

            var shouldHide = _visibility.ForegroundIsMaximizedOrFullscreen(Handle);

            if (shouldHide)
            {
                if (Visible) Hide();
            }
            else
            {
                if (!Visible)
                {
                    Show();
                    PositionOverlay();
                }
            }
        }

        private void PositionOverlay()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            Location = new Point(bounds.Right - Width - 5, bounds.Bottom - Height - 5);
        }

        private void SetOverlayBitmap(Bitmap bmp)
        {
            var old = _overlay.Image;
            _overlay.Image = bmp;
            old?.Dispose();

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            PositionOverlay();
        }

        private void UpdateTrayText(string text)
        {
            // NotifyIcon.Text max length is 63
            if (text.Length > 63)
                text = text.Substring(0, 63);

            _tray.Text = text;
        }

        private void ShowTrayNotification(string text, ToolTipIcon icon)
        {
            _tray.BalloonTipTitle = "Solar battery";
            _tray.BalloonTipText = text;
            _tray.BalloonTipIcon = icon;
            _tray.ShowBalloonTip(5000);
        }

        private void ShowDetails()
        {
            string gridStatus = "N/A";
            if (_latest.GridKw.HasValue)
            {
                if (_latest.GridKw.Value > 0) gridStatus = "Importing";
                else if (_latest.GridKw.Value < 0) gridStatus = "Exporting";
                else gridStatus = "Neutral";
            }

            string msg =
                "PV Power:      " + (_latest.PvKw.HasValue ? _latest.PvKw.Value.ToString("0.00") + " kW" : "N/A") + Environment.NewLine +
                "Load Power:    " + (_latest.LoadKw.HasValue ? _latest.LoadKw.Value.ToString("0.00") + " kW" : "N/A") + Environment.NewLine +
                "Battery SOC:   " + (_latest.SocPercent.HasValue ? _latest.SocPercent.Value.ToString("0.0") + " %" : "N/A") + Environment.NewLine +
                "Battery Volt:  " + (_latest.BatteryVolts.HasValue ? _latest.BatteryVolts.Value.ToString("0.00") + " V" : "N/A") + Environment.NewLine +
                "Grid Power:    " + (_latest.GridKw.HasValue ? Math.Abs(_latest.GridKw.Value).ToString("0.00") + " kW" : "N/A")
                + " (" + gridStatus + ")";

            MessageBox.Show(msg, "Solar data", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ExitApp()
        {
            _visibilityTimer.Stop();
            _visibilityTimer.Dispose();

            _tray.Visible = false;
            _tray.Dispose();

            await _mqtt.DisposeAsync();

            Application.Exit();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _overlay.Image?.Dispose();

            _tray.Visible = false;
            _tray.Dispose();

            base.OnFormClosed(e);
        }
    }
}
