using System;
using System.Windows.Forms;

namespace SolarTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new UI.SolarTrayForm());
        }
    }
}
