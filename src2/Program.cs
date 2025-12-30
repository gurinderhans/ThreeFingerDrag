namespace tpb
{
    using tpb.Properties;
    using System;
    using System.Windows.Forms;

    public static class Program
    {
        private static NotifyIcon trayIcon;

        [STAThread]
        public static void Main()
        {
            EnvConfig.LoadVariables();

            //corrects absolute coordinate translation when diff. scaling than 1x is applied
            //makes `win32.GetSystemMetrics(win32.SM_CXSCREEN)` correct correct resolution
            if (EnvConfig.tpb_IsProcessDPIAware) win32.SetProcessDPIAware();

            Program.trayIcon = new NotifyIcon()
            {
                Visible = true,
                Icon = Resources.tray_icon,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Clear Logs", (s,e) => (Logger.Instance as Logger).Clear()),
                    new MenuItem("Copy Logs", (s,e) => Clipboard.SetText((Logger.Instance as Logger).GetLogsAsString())),
                    new MenuItem("Exit", (s,e) => Application.Exit()),
                }),
            };

            Application.Run(new AppForm());
            Program.trayIcon.Dispose();
        }
    }
}