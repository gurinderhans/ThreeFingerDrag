namespace tfd
{
    // todo: update namespace
    using System;
    using System.Diagnostics;
    using System.Timers;

    public class TrackpadBlockManager
    {
        private readonly bool EnableDetailedTrackpadLogging;

        private readonly int TrackpadLeftBoundary;
        private readonly int TrackpadRightBoundary;
        private readonly int TrackpadTopBoundary;
        private readonly int TrackpadBottomBoundary;

        private readonly int TrackpadLeftBorderWidth;
        private readonly int TrackpadRightBorderWidth;
        private readonly int TrackpadTopBorderWidth;
        private readonly int TrackpadBottomBorderWidth;

        private readonly long DragEndMillisecondsThreshold;

        /// store reference to hook delegate to prevent it being garbage collected
        private readonly win32.WindowHookDelegate TrackpadHookDelegate;
        private readonly IntPtr TrackpadHookHandle = IntPtr.Zero;
        private readonly ILogger Logger;

        private readonly Timer monitor1fOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev1fTouchWatch = new Stopwatch();

        private bool isFingerDown = false;
        private bool trackpadBlockLeftClick;
        private TrackpadContact firstTrackpadContact;

        public TrackpadBlockManager(IContext appContext)
        {
            this.Logger = appContext.GetLogger();
            this.TrackpadHookDelegate = (c, w, l) => this.TrackpadHookDelegateHandler(c, w, l) ? 1 : win32.CallNextHookEx(this.TrackpadHookHandle, c, w, l);
            this.TrackpadHookHandle = win32.SetWindowsHookEx(win32.WH_MOUSE_LL, this.TrackpadHookDelegate, IntPtr.Zero, 0);

            this.EnableDetailedTrackpadLogging = appContext.LoadEnvVar(nameof(this.EnableDetailedTrackpadLogging), true);//false

            this.TrackpadLeftBoundary = appContext.LoadEnvVar(nameof(this.TrackpadLeftBoundary), 0);
            this.TrackpadRightBoundary = appContext.LoadEnvVar(nameof(this.TrackpadRightBoundary), 0);
            this.TrackpadTopBoundary = appContext.LoadEnvVar(nameof(this.TrackpadTopBoundary), 0);
            this.TrackpadBottomBoundary = appContext.LoadEnvVar(nameof(this.TrackpadBottomBoundary), 0);

            this.TrackpadLeftBorderWidth = 300;// appContext.LoadEnvVar(nameof(this.TrackpadLeftBorderWidth), 0);
            this.TrackpadRightBorderWidth = 300;// appContext.LoadEnvVar(nameof(this.TrackpadRightBorderWidth), 0);
            this.TrackpadTopBorderWidth = 300;// appContext.LoadEnvVar(nameof(this.TrackpadTopBorderWidth), 0);
            this.TrackpadBottomBorderWidth = 300;// appContext.LoadEnvVar(nameof(this.TrackpadBottomBorderWidth), 0);

            this.DragEndMillisecondsThreshold = 300;// appContext.LoadEnvVar(nameof(this.DragEndMillisecondsThreshold), 1000);
            this.monitor1fOnTrackpad.Elapsed += this.CheckIf1fOnTrackpadHandler;
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
            if (this.EnableDetailedTrackpadLogging)
            {
                //this.Logger.Debug($"mouse event blocked={this.trackpadBlockLeftClick}");
            }

            return this.trackpadBlockLeftClick;// && wParam == win32.WM_LBUTTONDOWN;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length != 1)
            {
                this.trackpadBlockLeftClick = false;
                return;
            }

            TrackpadContact currPos = contacts[0];
            if (!this.isFingerDown)
            {
                this.firstTrackpadContact = currPos;
                this.monitor1fOnTrackpad.Start();
                this.isFingerDown = true;
                return;
            }

            if (this.EnableDetailedTrackpadLogging)
            {
                //this.Logger.Debug($"trackpad cx:{currPos.X}, cy:{currPos.Y}");
            }

            bool isCurrInHorizontalBounds =
                IsInRangeExclusive(
                    currPos.X,
                    this.TrackpadLeftBoundary + TrackpadLeftBorderWidth,
                    this.TrackpadRightBoundary - this.TrackpadRightBorderWidth);

            bool isCurrInVerticalBounds =
                IsInRangeExclusive(
                    currPos.Y,
                    this.TrackpadTopBoundary + this.TrackpadTopBorderWidth,
                    this.TrackpadBottomBoundary - this.TrackpadBottomBorderWidth);

            bool isFirstInHorizontalBounds =
                IsInRangeExclusive(
                    this.firstTrackpadContact.X,
                    this.TrackpadLeftBoundary + TrackpadLeftBorderWidth,
                    this.TrackpadRightBoundary - this.TrackpadRightBorderWidth);

            bool isFirstInVerticalBounds =
                IsInRangeExclusive(
                    this.firstTrackpadContact.Y,
                    this.TrackpadTopBoundary + this.TrackpadTopBorderWidth,
                    this.TrackpadBottomBoundary - this.TrackpadBottomBorderWidth);

            //first outside && curr inside => allow + update first to inside
            //first inside && curr inside => allow
            //first inside && curr outside => allow
            //first outside && curr outside => disallow
            bool firstInside = isFirstInHorizontalBounds && isFirstInVerticalBounds;
            bool currInside = isCurrInHorizontalBounds && isCurrInVerticalBounds;
            if (!firstInside && currInside)
            {
                this.firstTrackpadContact = currPos;
            }

            this.trackpadBlockLeftClick = !firstInside && !currInside;

            //this.Logger.Debug($"trackpad, first={firstInside}, curr={currInside}, block={this.trackpadBlockLeftClick}");

            //this.trackpadBlockLeftClick = !isInHorizontalBounds || !isInVerticalBounds;
            this.timeSincePrev1fTouchWatch.Restart();
        }

        //todo: move to util class
        private static bool IsInRangeExclusive(int val, int low, int high)
        {
            return val > low && val < high;
        }

        private void CheckIf1fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev1fTouchWatch.IsRunning
                && this.timeSincePrev1fTouchWatch.ElapsedTicks > this.DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.isFingerDown = false;
                this.timeSincePrev1fTouchWatch.Reset();
                this.monitor1fOnTrackpad.Stop();
                this.Logger.Debug("1 finger left trackpad, stop");
            }
        }
    }
}