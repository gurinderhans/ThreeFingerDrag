namespace tfd
{
    using tfd.Properties;
    using System;
    using System.Windows.Forms;

    public static class Program
    {
        private static Context appContext;
        private static NotifyIcon trayIcon;

        [STAThread]
        public static void Main()
        {
            bool tfd_EnableDebugMode = bool.TrueString == Environment.GetEnvironmentVariable(nameof(tfd_EnableDebugMode));
            Program.appContext = new Context(new Logger(tfd_EnableDebugMode));

            //corrects absolute coordinate translation when diff. scaling than 1x is applied
            //makes `win32.GetSystemMetrics(win32.SM_CXSCREEN)` correct correct resolution
            bool tfd_IsProcessDPIAware = Program.appContext.LoadEnvVar(nameof(tfd_IsProcessDPIAware), false);
            if (tfd_IsProcessDPIAware) win32.SetProcessDPIAware();

            Program.trayIcon = new NotifyIcon()
            {
                Visible = true,
                Icon = Resources.tray_icon,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Clear Logs", (s,e) => (Program.appContext.GetLogger() as Logger).Clear()),
                    new MenuItem("Copy Logs", (s,e) => Clipboard.SetText((Program.appContext.GetLogger() as Logger).GetLogsAsString())),
                    new MenuItem("Exit", (s,e) => Application.Exit()),
                }),
            };

            Application.Run(new AppForm(Program.appContext));
            Program.trayIcon.Dispose();
        }
    }
}