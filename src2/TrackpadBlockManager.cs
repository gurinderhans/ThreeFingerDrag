namespace tpb
{
    using System;
    using System.Collections.Generic;
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
        private List<TrackpadContact> initialTouches = new List<TrackpadContact>();

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

        /* touch block rules:
         * > initial outside && curr inside => allow + update initial to inside
         * > initial inside && curr inside => allow
         * > initial inside && curr outside => allow
         * > initial outside && curr outside => block
         */
        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length > 2)
            {
                this.shouldBlockTrackpad = false;
                return;
            }

            //todo: log second finger
            //if (EnvConfig.tpb_EnableDetailedTrackpadLogging)
            //    Logger.Instance.Debug($"trackpad curr pos= x:{currPos.X}, y:{currPos.Y}");
            //if first and second touches are on opposite sides of trackpad and outside bounds, assume no scroll gesture and block trackpad
            //remaining logic stays same if only single touch detected

            if (this.initialTouches.Count != contacts.Length)
            {
                this.initialTouches = new List<TrackpadContact>(contacts);
                if (!this.monitor1fOnTrackpad.Enabled)
                {
                    this.monitor1fOnTrackpad.Start();
                }

                return;
            }

            // todo: simplify by combining below duplicated logic
            TrackpadContact currFirstTouch = contacts[0];
            if (contacts.Length == 1)
            {
                bool initialOutside = !Utils.IsPointInPolygon(this.initialTouches[0].X, this.initialTouches[0].Y, EnvConfig.tpb_TouchBoundsPolygon);
                bool currOutside = !Utils.IsPointInPolygon(currFirstTouch.X, currFirstTouch.Y, EnvConfig.tpb_TouchBoundsPolygon);
                if (initialOutside && !currOutside)
                {
                    this.initialTouches = new List<TrackpadContact>(contacts);
                }

                this.shouldBlockTrackpad = initialOutside && currOutside;
            }
            else if (contacts.Length == 2)
            {
                TrackpadContact currSecondTouch = contacts[1];

                bool initialFirstOutside = !Utils.IsPointInPolygon(this.initialTouches[0].X, this.initialTouches[0].Y, EnvConfig.tpb_TouchBoundsPolygon);
                bool initialSecondOutside = !Utils.IsPointInPolygon(this.initialTouches[1].X, this.initialTouches[1].Y, EnvConfig.tpb_TouchBoundsPolygon);
                bool initialOutside = initialFirstOutside && initialSecondOutside;

                bool currFirstOutside = !Utils.IsPointInPolygon(currFirstTouch.X, currFirstTouch.Y, EnvConfig.tpb_TouchBoundsPolygon);
                bool currSecondOutside = !Utils.IsPointInPolygon(currSecondTouch.X, currSecondTouch.Y, EnvConfig.tpb_TouchBoundsPolygon);
                bool currOutside = currFirstOutside && currSecondOutside;

                if (initialOutside && !currOutside)
                {
                    this.initialTouches = new List<TrackpadContact>(contacts);
                }

                this.shouldBlockTrackpad = initialOutside && currOutside;
            }

            //todo: setup devbox laptop to work from it and test apps better
            //todo: can likely decrease tpb_DragEndMillisecondsThreshold so we're not tracking prev. touch for too long, since don't need to keep drag down/etc like tfd
            //      making it less should help track latest touches and better switching b/w single & double touches
            if (!this.shouldBlockTrackpad) Console.WriteLine($"trackpad blocked ?= {this.shouldBlockTrackpad}");

            this.timeSincePrev1fTouchWatch.Restart();
        }

        private void CheckIf1fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev1fTouchWatch.IsRunning
                && this.timeSincePrev1fTouchWatch.ElapsedTicks > EnvConfig.tpb_DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.initialTouches.Clear();
                this.timeSincePrev1fTouchWatch.Reset();
                this.monitor1fOnTrackpad.Stop();
                Logger.Instance.Debug("1 finger left trackpad, stop");
            }
        }
    }
}