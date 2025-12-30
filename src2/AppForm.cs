namespace tpb
{
    using System.Drawing;
    using System.Windows.Forms;

    public partial class AppForm : Form
    {
        protected override CreateParams CreateParams
        {
            //https://www.csharp411.com/hide-form-from-alttab/
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= win32.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private readonly TrackpadBlockManager tpBlockManager;

        public AppForm()
        {
            this.InitializeComponent();
            this.Load += (s, e) => this.Size = Size.Empty;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Opacity = 0;

            if (EnvConfig.tpb_EnableTrackpadBlock)
            {
                this.tpBlockManager = new TrackpadBlockManager();
                bool registeredTrackpad = TrackpadHelper.RegisterTrackpad(this.Handle);
                Logger.Instance.Info($"registered trackpad={registeredTrackpad}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == win32.WM_INPUT)
            {
                TrackpadContact[] contacts = TrackpadHelper.ParseInput(m.LParam);
                this.tpBlockManager?.ProcessTouch(contacts);
            }

            base.WndProc(ref m);
        }
    }
}