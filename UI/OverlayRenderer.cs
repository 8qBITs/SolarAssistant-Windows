using System;
using System.Drawing;

namespace SolarTray.UI
{
    public sealed class OverlayRenderer
    {
        public Bitmap RenderStatus(string pv, string load, string soc, string grid)
        {
            using var textFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            using var emojiFont = CreateEmojiFontOrFallback(textFont);

            var segments = new (string Text, Color Color, bool IsEmoji)[]
            {
                ("☀ ", Color.Gold, true),
                ($"PV {pv} | ", Color.Lime, false),

                ("🏠 ", Color.DeepSkyBlue, true),
                ($"Load {load} | ", Color.Lime, false),

                ("🔋 ", Color.LimeGreen, true),
                ($"{soc} | ", Color.Lime, false),

                ("⚡ ", Color.Orange, true),
                ($"Grid {grid}", Color.Lime, false)
            };

            SizeF totalSize = SizeF.Empty;
            using (var tmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmp))
            {
                foreach (var seg in segments)
                {
                    var font = seg.IsEmoji ? emojiFont : textFont;
                    var s = g.MeasureString(seg.Text, font);
                    totalSize.Width += s.Width;
                    totalSize.Height = Math.Max(totalSize.Height, s.Height);
                }
            }

            const int paddingX = 8;
            const int paddingY = 4;
            var width = (int)Math.Ceiling(totalSize.Width) + paddingX * 2;
            var height = (int)Math.Ceiling(totalSize.Height) + paddingY * 2;

            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Black);

                float x = paddingX;
                float y = paddingY;

                foreach (var seg in segments)
                {
                    using var brush = new SolidBrush(seg.Color);
                    var font = seg.IsEmoji ? emojiFont : textFont;
                    g.DrawString(seg.Text, font, brush, new PointF(x, y));
                    x += g.MeasureString(seg.Text, font).Width;
                }
            }

            return bmp;
        }

        public Bitmap RenderPlain(string text)
        {
            using var font = new Font("Segoe UI", 9f, FontStyle.Regular);

            Size textSize;
            using (var tmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(tmp))
                textSize = g.MeasureString(text, font).ToSize();

            const int paddingX = 8;
            const int paddingY = 4;

            var bmp = new Bitmap(textSize.Width + paddingX * 2, textSize.Height + paddingY * 2);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Black);
                using var brush = new SolidBrush(Color.Lime);
                g.DrawString(text, font, brush, new PointF(paddingX, paddingY));
            }

            return bmp;
        }

        private static Font CreateEmojiFontOrFallback(Font fallback)
        {
            try { return new Font("Segoe UI Emoji", 10f, FontStyle.Regular); }
            catch { return (Font)fallback.Clone(); }
        }
    }
}
