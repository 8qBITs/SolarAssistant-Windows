using System;
using System.Drawing;
using System.Windows.Forms;
using SolarTray.Settings;

namespace SolarTray.UI
{
    public static class BatteryAlertSettingsDialog
    {
        public static void Show(IWin32Window owner)
        {
            using var dlg = new Form
            {
                Text = "Battery alerts",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(400, 260),
                ShowInTaskbar = false
            };

            var lblHigh = new Label { Text = "High (full) alert:", AutoSize = true, Location = new Point(12, 15) };
            var chkHigh = new CheckBox { Text = "Enabled", AutoSize = true, Location = new Point(30, 35), Checked = AppSettings.BatteryHighAlertEnabled };
            var lblHighTh = new Label { Text = "Threshold %:", AutoSize = true, Location = new Point(30, 60) };
            var numHigh = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Increment = 1, Value = (decimal)AppSettings.BatteryHighThreshold, Location = new Point(120, 58), Width = 60 };
            var lblHighMsg = new Label { Text = "Message:", AutoSize = true, Location = new Point(30, 85) };
            var txtHigh = new TextBox { Text = AppSettings.BatteryHighMessage, Location = new Point(100, 82), Width = 270 };

            var lblLow = new Label { Text = "Low alert:", AutoSize = true, Location = new Point(12, 120) };
            var chkLow = new CheckBox { Text = "Enabled", AutoSize = true, Location = new Point(30, 140), Checked = AppSettings.BatteryLowAlertEnabled };
            var lblLowTh = new Label { Text = "Threshold %:", AutoSize = true, Location = new Point(30, 165) };
            var numLow = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 1, Increment = 1, Value = (decimal)AppSettings.BatteryLowThreshold, Location = new Point(120, 163), Width = 60 };
            var lblLowMsg = new Label { Text = "Message:", AutoSize = true, Location = new Point(30, 190) };
            var txtLow = new TextBox { Text = AppSettings.BatteryLowMessage, Location = new Point(100, 187), Width = 270 };

            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new Point(dlg.ClientSize.Width - 170, dlg.ClientSize.Height - 35), Width = 75 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Location = new Point(dlg.ClientSize.Width - 90, dlg.ClientSize.Height - 35), Width = 75 };

            dlg.Controls.AddRange(new Control[]
            {
                lblHigh, chkHigh, lblHighTh, numHigh, lblHighMsg, txtHigh,
                lblLow, chkLow, lblLowTh, numLow, lblLowMsg, txtLow,
                btnOk, btnCancel
            });

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(owner) != DialogResult.OK) return;

            AppSettings.BatteryHighAlertEnabled = chkHigh.Checked;
            AppSettings.BatteryHighThreshold = (double)numHigh.Value;
            AppSettings.BatteryHighMessage = txtHigh.Text;

            AppSettings.BatteryLowAlertEnabled = chkLow.Checked;
            AppSettings.BatteryLowThreshold = (double)numLow.Value;
            AppSettings.BatteryLowMessage = txtLow.Text;

            AppSettings.Save();
        }
    }
}
