using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SolarTray.Services
{
    public sealed class WindowVisibilityService
    {
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOWMINIMIZED = 2;

        public bool ForegroundIsMaximizedOrFullscreen(IntPtr ourWindowHandle)
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            var shell = GetShellWindow();
            if (fg == shell) return false;
            if (fg == ourWindowHandle) return false;

            var sb = new StringBuilder(256);
            GetClassName(fg, sb, sb.Capacity);
            var cls = sb.ToString();
            if (cls is "Progman" or "WorkerW") return false;

            var placement = new WINDOWPLACEMENT { length = Marshal.SizeOf(typeof(WINDOWPLACEMENT)) };
            if (!GetWindowPlacement(fg, ref placement)) return false;
            if (placement.showCmd == SW_SHOWMINIMIZED) return false;
            if (placement.showCmd == SW_SHOWMAXIMIZED) return true;

            if (!GetWindowRect(fg, out var r)) return false;

            var rect = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            var screen = Screen.FromHandle(fg).Bounds;

            const int tolerance = 5;
            var coversScreen =
                rect.Left <= screen.Left + tolerance &&
                rect.Top <= screen.Top + tolerance &&
                rect.Right >= screen.Right - tolerance &&
                rect.Bottom >= screen.Bottom - tolerance;

            return coversScreen;
        }

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll", SetLastError = true)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length, flags, showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }
    }
}
