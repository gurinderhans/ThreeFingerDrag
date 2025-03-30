using System;

namespace tfd
{
    public class TrackpadBlockManager
    {
        private readonly bool EnableTrackpadDetailedLogging;

        private readonly int TrackpadLeftBoundary;
        private readonly int TrackpadRightBoundary;
        private readonly int TrackpadTopBoundary;
        private readonly int TrackpadBottomBoundary;

        private readonly int TrackpadLeftBorderWidth;
        private readonly int TrackpadRightBorderWidth;
        private readonly int TrackpadTopBorderWidth;
        private readonly int TrackpadBottomBorderWidth;

        /// store reference to hook delegate to prevent it being garbage collected
        private readonly win32.WindowHookDelegate TrackpadHookDelegate;
        private readonly IntPtr TrackpadHookHandle = IntPtr.Zero;
        private readonly ILogger Logger;

        private bool trackpadBlockLeftClick;

        public TrackpadBlockManager(IContext appContext)
        {
            this.Logger = appContext.GetLogger();
            this.TrackpadHookDelegate = (c, w, l) => this.TrackpadHookDelegateHandler(c, w, l) ? 1 : win32.CallNextHookEx(this.TrackpadHookHandle, c, w, l);
            this.TrackpadHookHandle = win32.SetWindowsHookEx(win32.WH_MOUSE_LL, this.TrackpadHookDelegate, IntPtr.Zero, 0);

            this.EnableTrackpadDetailedLogging = appContext.LoadEnvVar(nameof(this.EnableTrackpadDetailedLogging), false);

            this.TrackpadLeftBoundary = appContext.LoadEnvVar(nameof(this.TrackpadLeftBoundary), 0);
            this.TrackpadRightBoundary = appContext.LoadEnvVar(nameof(this.TrackpadRightBoundary), int.MaxValue);
            this.TrackpadTopBoundary = appContext.LoadEnvVar(nameof(this.TrackpadTopBoundary), 0);
            this.TrackpadBottomBoundary = appContext.LoadEnvVar(nameof(this.TrackpadBottomBoundary), int.MaxValue);

            this.TrackpadLeftBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadLeftBorderWidth), 10);
            this.TrackpadRightBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadRightBorderWidth), 10);
            this.TrackpadTopBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadTopBorderWidth), 10);
            this.TrackpadBottomBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadBottomBorderWidth), 10);
        }

        ~TrackpadBlockManager()
        {
            if (this.TrackpadHookHandle != IntPtr.Zero)
            {
                win32.UnhookWindowsHookEx(this.TrackpadHookHandle);
            }

            this.Logger.Info($"{nameof(TrackpadBlockManager)} got gc'd");
        }

        private bool TrackpadHookDelegateHandler(int code, int wParam, IntPtr lParam)
        {
            if (this.EnableTrackpadDetailedLogging)
            {
                this.Logger.Debug($"trackpad click blocked={this.trackpadBlockLeftClick}");
            }

            return this.trackpadBlockLeftClick && wParam == win32.WM_LBUTTONDOWN;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0) return;

            if (contacts.Length == 1)
            {
                TrackpadContact currPos = contacts[0];
                if (this.EnableTrackpadDetailedLogging)
                {
                    this.Logger.Debug($"trackpad cx:{currPos.X}, cy:{currPos.Y}");
                }

                this.trackpadBlockLeftClick = currPos.X < this.TrackpadLeftBoundary + this.TrackpadLeftBorderWidth
                    || currPos.X > this.TrackpadRightBoundary - this.TrackpadRightBorderWidth
                    || currPos.Y < this.TrackpadTopBoundary + this.TrackpadTopBorderWidth
                    || currPos.Y > this.TrackpadBottomBoundary - this.TrackpadBottomBorderWidth;
            }
        }
    }
}
