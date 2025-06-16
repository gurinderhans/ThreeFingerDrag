using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace tfd
{
    //TODO: check commit before adding the left clicker blocker, but now if you're moving cursor with one finger and then while keeping first finger down,
    //bring down two more to initiate three finger drag, the mouse / window starts glitching/stuttering and dropping frames
    public class ThreeFingerDragManager
    {
        private readonly double DragSpeedMultiplier;
        private readonly double DragVelocityUpperBoundX;
        private readonly double DragVelocityUpperBoundY;
        private readonly double DragEndOnNewGestureMinDist;
        private readonly double DragEndOnNewGestureMaxDist;
        private readonly double DragStartFingersDistThresholdMultiplier;
        private readonly long DragEndMillisecondsThreshold;
        private readonly int DragEndConfidenceThreshold;
        private readonly int TimeSinceLast3fTouchMinMillis;
        private readonly int TrackpadCoordsDivByDenomSize;

        // todo: convert to config
        private readonly Timer monitor3fOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev3fTouchWatch = new Stopwatch();
        private readonly ILogger Logger;

        private bool isDragging = false;
        private int dragEndConfidence = 0;
        private int prevContactsCount = 0;
        private double prevTrackpadX = 0;
        private double prevTrackpadY = 0;


        //////
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
        private bool trackpadBlockLeftClick;

        // todo: convert to config
        private readonly Timer monitor1fOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev1fTouchWatch = new Stopwatch();
        private TrackpadContact firstContactPoint;

        public ThreeFingerDragManager(IContext appContext)
        {
            this.Logger = appContext.GetLogger();
            this.DragSpeedMultiplier = appContext.LoadEnvVar(nameof(this.DragSpeedMultiplier), 1.5f);
            this.DragVelocityUpperBoundX = appContext.LoadEnvVar(nameof(this.DragVelocityUpperBoundX), 1.3f);
            this.DragVelocityUpperBoundY = appContext.LoadEnvVar(nameof(this.DragVelocityUpperBoundY), 1.2f);
            this.DragEndOnNewGestureMinDist = appContext.LoadEnvVar(nameof(this.DragEndOnNewGestureMinDist), 0);
            this.DragEndOnNewGestureMaxDist = appContext.LoadEnvVar(nameof(this.DragEndOnNewGestureMaxDist), 100);
            this.DragStartFingersDistThresholdMultiplier = appContext.LoadEnvVar(nameof(this.DragStartFingersDistThresholdMultiplier), 2.5f);
            this.DragEndMillisecondsThreshold = appContext.LoadEnvVar(nameof(this.DragEndMillisecondsThreshold), 1000);
            this.DragEndConfidenceThreshold = appContext.LoadEnvVar(nameof(this.DragEndConfidenceThreshold), 3);
            this.TimeSinceLast3fTouchMinMillis = appContext.LoadEnvVar(nameof(this.TimeSinceLast3fTouchMinMillis), 50);
            this.TrackpadCoordsDivByDenomSize = appContext.LoadEnvVar(nameof(this.TrackpadCoordsDivByDenomSize), 0);
            this.monitor3fOnTrackpad.Elapsed += this.CheckIf3fOnTrackpadHandler;

            /////
            this.TrackpadHookDelegate = (c, w, l) => this.TrackpadHookDelegateHandler(c, w, l) ? 1 : win32.CallNextHookEx(this.TrackpadHookHandle, c, w, l);
            this.TrackpadHookHandle = win32.SetWindowsHookEx(win32.WH_MOUSE_LL, this.TrackpadHookDelegate, IntPtr.Zero, 0);

            this.EnableTrackpadDetailedLogging = appContext.LoadEnvVar(nameof(this.EnableTrackpadDetailedLogging), false);

            this.TrackpadLeftBoundary = appContext.LoadEnvVar(nameof(this.TrackpadLeftBoundary), 0);
            this.TrackpadRightBoundary = appContext.LoadEnvVar(nameof(this.TrackpadRightBoundary), 1224);
            this.TrackpadTopBoundary = appContext.LoadEnvVar(nameof(this.TrackpadTopBoundary), 0);
            this.TrackpadBottomBoundary = appContext.LoadEnvVar(nameof(this.TrackpadBottomBoundary), 804);

            this.TrackpadLeftBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadLeftBorderWidth), 200);
            this.TrackpadRightBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadRightBorderWidth), 200);
            this.TrackpadTopBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadTopBorderWidth), 200);
            this.TrackpadBottomBorderWidth = appContext.LoadEnvVar(nameof(this.TrackpadBottomBorderWidth), 200);

            this.monitor1fOnTrackpad.Elapsed += this.CheckIf1fOnTrackpadHandler;
        }

        ~ThreeFingerDragManager()
        {
            if (this.TrackpadHookHandle != IntPtr.Zero)
            {
                win32.UnhookWindowsHookEx(this.TrackpadHookHandle);
            }

            this.Logger.Info($"{nameof(ThreeFingerDragManager)} got gc'd");
        }

        private bool TrackpadHookDelegateHandler(int code, int wParam, IntPtr lParam)
        {
            if (this.EnableTrackpadDetailedLogging)
            {
                this.Logger.Debug($"trackpad click blocked={this.trackpadBlockLeftClick}");
            }

            return false;// this.trackpadBlockLeftClick && wParam == win32.WM_LBUTTONDOWN;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0) return;

            if (!this.isDragging && contacts.Length == 1)
            {
                TrackpadContact currPos = contacts[0];
                if (this.EnableTrackpadDetailedLogging)
                {
                    this.Logger.Debug($"trackpad cx:{currPos.X}, cy:{currPos.Y}");
                }

                if (!this.monitor1fOnTrackpad.Enabled)
                {
                    Console.WriteLine($"1f touched down on trackpad, 3f dragging={this.isDragging}");

                    this.firstContactPoint = currPos;
                    this.monitor1fOnTrackpad.Start();
                }

                this.timeSincePrev1fTouchWatch.Restart();

                //isFirstOutOfBounds = chk(firstContactPoint);
                //isCurrOutOfBounds = chk(currPos);
                //block or not based off above

                this.trackpadBlockLeftClick = currPos.X < this.TrackpadLeftBoundary + this.TrackpadLeftBorderWidth
                    || currPos.X > this.TrackpadRightBoundary - this.TrackpadRightBorderWidth
                    || currPos.Y < this.TrackpadTopBoundary + this.TrackpadTopBorderWidth
                    || currPos.Y > this.TrackpadBottomBoundary - this.TrackpadBottomBorderWidth;
            }

            /// calculate avg. location of all three contacts
            double trackpadX = 0, trackpadY = 0;
            for (int i = 0; i < contacts.Length; ++i)
            {
                trackpadX += contacts[i].X;
                trackpadY += contacts[i].Y;
            }
            int contactsDivBy = contacts.Length + this.TrackpadCoordsDivByDenomSize;
            trackpadX = Math.Ceiling(trackpadX / contactsDivBy);
            trackpadY = Math.Ceiling(trackpadY / contactsDivBy);

            if (contacts.Length == 3)
            {
                if (!this.isDragging)
                {
                    double[] distBetween3Fingers = new[]
                    {
                        Utils.CalculateHypotenuse(contacts[0].X - contacts[1].X, contacts[0].Y - contacts[1].Y),
                        Utils.CalculateHypotenuse(contacts[0].X - contacts[2].X, contacts[0].Y - contacts[2].Y),
                        Utils.CalculateHypotenuse(contacts[1].X - contacts[2].X, contacts[1].Y - contacts[2].Y),
                    };

                    /// 'LG Gram 17' trackpad drivers report 3rd finger location same as 2nd finger location,
                    /// ie. min dist = 0, we ensure min dist = 1 to allow high threshold value to skip this check
                    double minDistClamped = Math.Max(1, distBetween3Fingers.Min());

                    /// make comparison of finger distances relative to other fingers
                    /// this takes into account users natural finger distance preference
                    if (distBetween3Fingers.Max() > (minDistClamped * this.DragStartFingersDistThresholdMultiplier))
                    {
                        this.Logger.Debug($"3 fingers too far apart=({string.Join(",", distBetween3Fingers)})");
                        return;
                    }

                    win32.mouse_event(win32.MOUSEEVENTF_LEFTDOWN | win32.MOUSEEVENTF_MOVE, 0, 0, 0, 0);
                    this.monitor3fOnTrackpad.Start();
                    this.isDragging = true;
                    Console.WriteLine("3 fingers drag start");

                    //cancel 1f
                    this.Cancel1fMonitoring();
                }

                /// TipSwitch isn't reported, so we track elapsed time to check when touch leaves & returns.
                /// when it returns, we record position of first touch and only move cursor when second touch
                /// occurs so relative positioning is computed correctly
                long last3fTouchTime = this.timeSincePrev3fTouchWatch.ElapsedTicks;
                if (this.timeSincePrev3fTouchWatch.IsRunning
                    && last3fTouchTime < this.TimeSinceLast3fTouchMinMillis * TimeSpan.TicksPerMillisecond)
                {
                    double deltaX = trackpadX - this.prevTrackpadX;
                    double deltaY = trackpadY - this.prevTrackpadY;

                    /// velocity = dist / milliseconds; ensure time != 0
                    long timeClamped = Math.Max(1, last3fTouchTime / TimeSpan.TicksPerMillisecond);
                    double velocityX = deltaX / timeClamped;
                    double velocityY = deltaY / timeClamped;

                    /// clamp velocity b/w custom threshold
                    velocityX = Math.Min(Math.Max(Math.Abs(velocityX), 1f), this.DragVelocityUpperBoundX);
                    velocityY = Math.Min(Math.Max(Math.Abs(velocityY), 1f), this.DragVelocityUpperBoundY);

                    int dX = (int)(deltaX * velocityX * this.DragSpeedMultiplier);
                    int dY = (int)(deltaY * velocityY * this.DragSpeedMultiplier);
                    win32.mouse_event(win32.MOUSEEVENTF_MOVE, dX, dY, 0, 0);
                }

                this.dragEndConfidence = 0;
                this.timeSincePrev3fTouchWatch.Restart();
            }
            else if (this.isDragging)
            {
                if (this.prevContactsCount == contacts.Length)
                {
                    double deltaX = trackpadX - this.prevTrackpadX;
                    double deltaY = trackpadY - this.prevTrackpadY;
                    double dist = Utils.CalculateHypotenuse(deltaX, deltaY);
                    this.Logger.Debug($"moved dist={dist}");

                    if (this.DragEndOnNewGestureMinDist < dist && dist < this.DragEndOnNewGestureMaxDist)
                    {
                        if (++this.dragEndConfidence > this.DragEndConfidenceThreshold)
                        {
                            this.StopDrag();
                            this.Logger.Debug($"new gesture, stop drag");
                        }
                    }
                    else
                    {
                        this.dragEndConfidence = 0;
                    }
                }
                else
                {
                    this.dragEndConfidence = 0;
                }
            }

            this.prevTrackpadX = trackpadX;
            this.prevTrackpadY = trackpadY;
            this.prevContactsCount = contacts.Length;
        }

        private void StopDrag()
        {
            this.isDragging = false;
            this.monitor3fOnTrackpad.Stop();
            win32.mouse_event(win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

            this.dragEndConfidence = 0;
            this.prevTrackpadX = 0;
            this.prevTrackpadY = 0;
            this.prevContactsCount = 0;
            this.timeSincePrev3fTouchWatch.Reset();
            Console.WriteLine("3 fingers left trackpad, stop drag");
        }

        private void CheckIf3fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev3fTouchWatch.IsRunning
                && this.timeSincePrev3fTouchWatch.ElapsedTicks > this.DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.StopDrag();
            }
        }

        private void Cancel1fMonitoring()
        {
            this.trackpadBlockLeftClick = false;
            this.monitor1fOnTrackpad.Stop();
            this.timeSincePrev1fTouchWatch.Reset();
            Console.WriteLine("1 finger touch stopped");
        }

        private void CheckIf1fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev1fTouchWatch.IsRunning
                && this.timeSincePrev1fTouchWatch.ElapsedTicks > 400 * TimeSpan.TicksPerMillisecond)
            {
                this.Cancel1fMonitoring();
            }
        }
    }
}