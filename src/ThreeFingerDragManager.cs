namespace tfd
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Timers;

    public class ThreeFingerDragManager
    {
        private readonly Timer monitor3fOnTrackpad;
        private readonly Stopwatch timeSincePrev3fTouchWatch = new Stopwatch();

        private bool isDragging = false;
        private int dragEndConfidence = 0;
        private int prevContactsCount = 0;
        private double prevTrackpadX = 0;
        private double prevTrackpadY = 0;

        public ThreeFingerDragManager()
        {
            this.monitor3fOnTrackpad = new Timer(EnvConfig.tfd_Monitor3fOnTrackpadInterval);
            this.monitor3fOnTrackpad.Elapsed += this.CheckIf3fOnTrackpadHandler;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0) return;

            double trackpadX = 0;
            double trackpadY = 0;
            foreach (TrackpadContact contact in contacts)
            {
                trackpadX += contact.X;
                trackpadY += contact.Y;
            }

            int trackpadScaleFactor = EnvConfig.tfd_TrackpadCoordsScaleFactor ?? contacts.Length;
            trackpadX = Math.Ceiling(trackpadX / trackpadScaleFactor);
            trackpadY = Math.Ceiling(trackpadY / trackpadScaleFactor);
            Logger.Instance.Debug($"trackpad coords, x={trackpadX}, y={trackpadY}, scaleFactor={trackpadScaleFactor}");

            double deltaX = trackpadX - this.prevTrackpadX;
            double deltaY = trackpadY - this.prevTrackpadY;

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

                    //'LG Gram 17' trackpad drivers report 3rd finger location same as 2nd finger location,
                    //ie. min dist = 0, we ensure min dist = 1 to allow high threshold value to skip this check
                    double minDistClamped = Math.Max(1, distBetween3Fingers.Min());

                    //make comparison of finger distances relative to other fingers
                    //this takes into account users natural finger distance preference
                    if (distBetween3Fingers.Max() > (minDistClamped * EnvConfig.tfd_DragStartFingersDistThresholdMultiplier))
                    {
                        Logger.Instance.Debug($"3 fingers too far apart=({string.Join(",", distBetween3Fingers)})");
                        return;
                    }

                    this.StartDrag();
                    Logger.Instance.Debug($"start 3f drag");
                }

                //`TipSwitch` isn't reported, so we track elapsed time to check when touch leaves & returns.
                //when it returns, we record position of first touch and only move cursor when second touch
                //occurs so relative positioning is computed correctly
                long last3fTouchTime = this.timeSincePrev3fTouchWatch.ElapsedTicks;
                if (this.timeSincePrev3fTouchWatch.IsRunning
                    && last3fTouchTime < EnvConfig.tfd_TimeSinceLast3fTouchMinMillis * TimeSpan.TicksPerMillisecond)
                {
                    long timeClamped = Math.Max(1, last3fTouchTime / TimeSpan.TicksPerMillisecond);

                    double velocityX =
                        Utils.ClampValue(
                            Math.Abs(deltaX / timeClamped),
                            EnvConfig.tfd_DragVelocityLowerBoundX,
                            EnvConfig.tfd_DragVelocityUpperBoundX);
                    double velocityY =
                        Utils.ClampValue(
                            Math.Abs(deltaY / timeClamped),
                            EnvConfig.tfd_DragVelocityLowerBoundY,
                            EnvConfig.tfd_DragVelocityUpperBoundY);

                    //if dX or dY are less than 1.0, casting to int rounds it down to 0
                    int dX = (int)(deltaX * velocityX * EnvConfig.tfd_DragSpeedMultiplier);
                    int dY = (int)(deltaY * velocityY * EnvConfig.tfd_DragSpeedMultiplier);

                    Logger.Instance.Debug($"timeClamped={timeClamped}, vx={velocityX}, vy={velocityY}, dx={dX}, dy={dY}");

                    win32.mouse_event(win32.MOUSEEVENTF_MOVE, dX, dY, 0, 0);
                }

                this.dragEndConfidence = 0;
                this.timeSincePrev3fTouchWatch.Restart();
            }
            else if (this.isDragging)
            {
                if (this.prevContactsCount == contacts.Length)
                {
                    double dist = Utils.CalculateHypotenuse(deltaX, deltaY);
                    Logger.Instance.Debug($"moved dist={dist}");

                    if (EnvConfig.tfd_DragEndOnNewGestureMinDist < dist && dist < EnvConfig.tfd_DragEndOnNewGestureMaxDist)
                    {
                        if (++this.dragEndConfidence > EnvConfig.tfd_DragEndConfidenceThreshold)
                        {
                            this.StopDrag();
                            Logger.Instance.Debug($"new gesture, stop drag");
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

        private void StartDrag()
        {
            win32.mouse_event(win32.MOUSEEVENTF_LEFTDOWN | win32.MOUSEEVENTF_MOVE, 0, 0, 0, 0);
            this.isDragging = true;
            this.monitor3fOnTrackpad.Start();
        }

        private void StopDrag()
        {
            win32.mouse_event(win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            this.isDragging = false;
            this.monitor3fOnTrackpad.Stop();

            this.dragEndConfidence = 0;
            this.prevTrackpadX = 0;
            this.prevTrackpadY = 0;
            this.prevContactsCount = 0;
            this.timeSincePrev3fTouchWatch.Reset();
        }

        private void CheckIf3fOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev3fTouchWatch.IsRunning
                && this.timeSincePrev3fTouchWatch.ElapsedTicks > EnvConfig.tfd_DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.StopDrag();
                Logger.Instance.Debug("3 fingers left trackpad, stop drag");
            }
        }
    }
}