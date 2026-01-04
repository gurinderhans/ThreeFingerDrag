namespace tpb
{
    using System;
    using System.Diagnostics;
    using System.Timers;

    public class TrackpadBlockManager
    {
        private readonly Timer monitor1fOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev1fTouchWatch = new Stopwatch();

        //keep reference to hook and delegate to prevent garbage collection
        private readonly win32.WindowHookDelegate trackpadHookDelegate;
        private readonly IntPtr trackpadHookHandle = IntPtr.Zero;

        private bool shouldBlockTrackpad;
        private TrackpadContact? firstTrackpadContact;

        public TrackpadBlockManager()
        {
            this.trackpadHookDelegate = (c, w, l) => this.TrackpadHookDelegateHandler(c, w, l) ? 1 : win32.CallNextHookEx(this.trackpadHookHandle, c, w, l);
            this.trackpadHookHandle = win32.SetWindowsHookEx(win32.WH_MOUSE_LL, this.trackpadHookDelegate, IntPtr.Zero, 0);
            this.monitor1fOnTrackpad.Elapsed += this.CheckIf1fOnTrackpadHandler;
        }

        ~TrackpadBlockManager()
        {
            if (this.trackpadHookHandle != IntPtr.Zero)
            {
                win32.UnhookWindowsHookEx(this.trackpadHookHandle);
            }

            Logger.Instance.Info($"{nameof(TrackpadBlockManager)} got gc'd");
        }

        private bool TrackpadHookDelegateHandler(int code, int wParam, IntPtr lParam)
        {
            if (EnvConfig.tpb_EnableDetailedTrackpadLogging)
            {
                Logger.Instance.Debug($"mouse event blocked={this.shouldBlockTrackpad}");
            }

            return this.shouldBlockTrackpad;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length > 2)
            {
                this.shouldBlockTrackpad = false;
                return;
            }

            //todo: log second finger
            //if (EnvConfig.tpb_EnableDetailedTrackpadLogging)
            //{
            //    Logger.Instance.Debug($"trackpad curr pos= x:{currPos.X}, y:{currPos.Y}");
            //}

            //if (first and second are on opposite sides of trackpad and outside bounds, assume no scroll gesture and block)
            //remainng logic should stay same if only one touch detected

            TrackpadContact firstPos = contacts[0];
            if (contacts.Length == 2)
            {
                TrackpadContact secondPos = contacts[1];
            }

            //if (this.firstTrackpadContact == null)
            //{
            //    this.firstTrackpadContact = firstPos;
            //    this.monitor1fOnTrackpad.Start();
            //    return;
            //}

            ////rules:
            ////- first outside && curr inside => allow + update first to inside
            ////- first inside && curr inside => allow
            ////- first inside && curr outside => allow
            ////- first outside && curr outside => disallow
            //bool firstInside = Utils.IsPointInPolygon(this.firstTrackpadContact.Value.X, this.firstTrackpadContact.Value.Y, EnvConfig.tpb_TouchBoundsPolygon);
            //bool currInside = Utils.IsPointInPolygon(firstPos.X, firstPos.Y, EnvConfig.tpb_TouchBoundsPolygon);
            //if (!firstInside && currInside)
            //{
            //    this.firstTrackpadContact = firstPos;
            //}

            //this.shouldBlockTrackpad = !firstInside && !currInside;
            //Logger.Instance.Debug($"trackpad, first={firstInside}, curr={currInside}, shouldBlock={this.shouldBlockTrackpad}");
            //this.timeSincePrev1fTouchWatch.Restart();
        }

        private void CheckIf1fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev1fTouchWatch.IsRunning
                && this.timeSincePrev1fTouchWatch.ElapsedTicks > EnvConfig.tpb_DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.firstTrackpadContact = null;
                this.timeSincePrev1fTouchWatch.Reset();
                this.monitor1fOnTrackpad.Stop();
                Logger.Instance.Debug("1 finger left trackpad, stop");
            }
        }
    }
}