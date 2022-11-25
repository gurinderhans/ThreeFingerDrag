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
            win32.SetProcessDPIAware();

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