using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace tfd
{
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

        private readonly Timer monitor3fOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev3fTouchWatch = new Stopwatch();
        private readonly ILogger Logger;

        private bool isDragging = false;
        private int dragEndConfidence = 0;
        private int prevContactsCount = 0;
        private double prevTrackpadX = 0;
        private double prevTrackpadY = 0;

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
            this.DragEndConfidenceThreshold = appContext.LoadEnvVar(nameof(this.DragEndConfidenceThreshold), 5);
            this.TimeSinceLast3fTouchMinMillis = appContext.LoadEnvVar(nameof(this.TimeSinceLast3fTouchMinMillis), 50);
            this.TrackpadCoordsDivByDenomSize = appContext.LoadEnvVar(nameof(this.TrackpadCoordsDivByDenomSize), 0);
            this.monitor3fOnTrackpad.Elapsed += this.CheckIf3fOnTrackpadHandler;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0) return;

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
        }

        private void CheckIf3fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev3fTouchWatch.IsRunning
                && this.timeSincePrev3fTouchWatch.ElapsedTicks > this.DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.StopDrag();
                this.Logger.Debug("3 fingers left trackpad, stop drag");
            }
        }
    }
}