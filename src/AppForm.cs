using System.Drawing;
using System.Windows.Forms;

namespace tfd
{
    public partial class AppForm : Form
    {
        protected override CreateParams CreateParams
        {
            //https://www.csharp411.com/hide-form-from-alttab/
            get
            {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private readonly ILogger logger;
        private ThreeFingerDragManager tfDragManager;

        public AppForm(IContext context)
        {
            InitializeComponent();

            this.Load += (s, e) => this.Size = new Size(Point.Empty);
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.logger = context.GetLogger();

            bool EnableThreeFingerDrag = context.LoadEnvVar(nameof(EnableThreeFingerDrag), true);
            if (EnableThreeFingerDrag)
            {
                this.tfDragManager = new ThreeFingerDragManager(context);
                if (TrackpadHelper.RegisterTrackpad(this.Handle))
                    this.logger.Info("register trackpad success");
                else
                    this.logger.Error("error registering trackpad!");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == win32.WM_INPUT)
                this.tfDragManager.ProcessInput(m.LParam);
            base.WndProc(ref m);
        }
    }
}