namespace tfd
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

        private readonly ILogger Logger;
        private readonly ThreeFingerDragManager tfDragManager;

        public AppForm(IContext context)
        {
            this.InitializeComponent();
            this.Load += (s, e) => this.Size = Size.Empty;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.Logger = context.GetLogger();

            bool tfd_EnableThreeFingerDrag = context.LoadEnvVar(nameof(tfd_EnableThreeFingerDrag), true);
            if (tfd_EnableThreeFingerDrag)
            {
                this.tfDragManager = new ThreeFingerDragManager(context);
                bool registeredTrackpad = TrackpadHelper.RegisterTrackpad(this.Handle);
                this.Logger.Info($"registered trackpad={registeredTrackpad}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == win32.WM_INPUT)
            {
                TrackpadContact[] contacts = TrackpadHelper.ParseInput(m.LParam);
                this.tfDragManager?.ProcessTouch(contacts);
            }

            base.WndProc(ref m);
        }
    }
}