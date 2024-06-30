using tfd.Properties;
using System;
using System.Windows.Forms;

namespace tfd
{
    public static class Program
    {
        private static Context appContext = new Context();
        private static NotifyIcon trayIcon;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            /// corrects absolute coordinate translation when diff. scaling than 1x is applied
            /// makes `win32.GetSystemMetrics(win32.SM_CXSCREEN)` correct correct resolution
            bool IsProcessDPIAware = Program.appContext.LoadEnvVar(nameof(IsProcessDPIAware), false);
            if (IsProcessDPIAware) win32.SetProcessDPIAware();

            Program.trayIcon = new NotifyIcon()
            {
                Visible = true,
                Icon = Resources.tray_icon,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Clear Logs", (s,e) => Program.appContext.Logger.Clear()),
                    new MenuItem("Copy Logs", (s,e) => Clipboard.SetText(Program.appContext.Logger.GetLogsAsString())),
                    new MenuItem("Exit", (s,e) => Application.Exit()),
                }),
            };

            Application.Run(new AppForm(Program.appContext));
            Program.trayIcon.Dispose();
        }
    }
}