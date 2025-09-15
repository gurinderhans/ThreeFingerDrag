namespace tfd
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Timers;

    public class ThreeFingerDragManager
    {
        private readonly double tfd_DragSpeedMultiplier;

        private readonly double tfd_DragVelocityLowerBoundX;
        private readonly double tfd_DragVelocityUpperBoundX;
        private readonly double tfd_DragVelocityLowerBoundY;
        private readonly double tfd_DragVelocityUpperBoundY;

        private readonly double tfd_DragStartFingersDistThresholdMultiplier;
        private readonly double tfd_DragEndOnNewGestureMinDist;
        private readonly double tfd_DragEndOnNewGestureMaxDist;
        private readonly long tfd_DragEndMillisecondsThreshold;
        private readonly int tfd_DragEndConfidenceThreshold;

        private readonly int tfd_TrackpadCoordsDivByDenomSize;
        private readonly int tfd_TimeSinceLast3fTouchMinMillis;

        private readonly int tfd_Monitor3fOnTrackpadInterval;
        private readonly Timer monitor3fOnTrackpad;

        private readonly ILogger Logger;
        private readonly Stopwatch timeSincePrev3fTouchWatch = new Stopwatch();

        private bool isDragging = false;
        private int dragEndConfidence = 0;
        private int prevContactsCount = 0;
        private double prevTrackpadX = 0;
        private double prevTrackpadY = 0;

        public ThreeFingerDragManager(IContext appContext)
        {
            this.Logger = appContext.GetLogger();

            this.tfd_DragSpeedMultiplier = appContext.LoadEnvVar(nameof(this.tfd_DragSpeedMultiplier), 1f);

            this.tfd_DragVelocityLowerBoundX = appContext.LoadEnvVar(nameof(this.tfd_DragVelocityLowerBoundX), 1f);
            this.tfd_DragVelocityUpperBoundX = appContext.LoadEnvVar(nameof(this.tfd_DragVelocityUpperBoundX), 1f);
            this.tfd_DragVelocityLowerBoundY = appContext.LoadEnvVar(nameof(this.tfd_DragVelocityLowerBoundY), 1f);
            this.tfd_DragVelocityUpperBoundY = appContext.LoadEnvVar(nameof(this.tfd_DragVelocityUpperBoundY), 1f);

            this.tfd_DragStartFingersDistThresholdMultiplier = appContext.LoadEnvVar(nameof(this.tfd_DragStartFingersDistThresholdMultiplier), 3f);
            this.tfd_DragEndOnNewGestureMinDist = appContext.LoadEnvVar(nameof(this.tfd_DragEndOnNewGestureMinDist), 0);
            this.tfd_DragEndOnNewGestureMaxDist = appContext.LoadEnvVar(nameof(this.tfd_DragEndOnNewGestureMaxDist), 100);
            this.tfd_DragEndMillisecondsThreshold = appContext.LoadEnvVar(nameof(this.tfd_DragEndMillisecondsThreshold), 1000);
            this.tfd_DragEndConfidenceThreshold = appContext.LoadEnvVar(nameof(this.tfd_DragEndConfidenceThreshold), 5);

            this.tfd_TrackpadCoordsDivByDenomSize = appContext.LoadEnvVar(nameof(this.tfd_TrackpadCoordsDivByDenomSize), 0);
            this.tfd_TimeSinceLast3fTouchMinMillis = appContext.LoadEnvVar(nameof(this.tfd_TimeSinceLast3fTouchMinMillis), 50);

            this.tfd_Monitor3fOnTrackpadInterval = appContext.LoadEnvVar(nameof(this.tfd_Monitor3fOnTrackpadInterval), 100);
            this.monitor3fOnTrackpad = new Timer(this.tfd_Monitor3fOnTrackpadInterval);
            this.monitor3fOnTrackpad.Elapsed += this.CheckIf3fOnTrackpadHandler;
        }

        public void ProcessTouch(TrackpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0) return;

            //calculate avg. location of all three contacts
            double trackpadX = 0, trackpadY = 0;
            for (int i = 0; i < contacts.Length; ++i)
            {
                trackpadX += contacts[i].X;
                trackpadY += contacts[i].Y;
            }

            int contactsDivBy = contacts.Length + this.tfd_TrackpadCoordsDivByDenomSize;
            trackpadX = Math.Ceiling(trackpadX / contactsDivBy);
            trackpadY = Math.Ceiling(trackpadY / contactsDivBy);

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
                    if (distBetween3Fingers.Max() > (minDistClamped * this.tfd_DragStartFingersDistThresholdMultiplier))
                    {
                        this.Logger.Debug($"3 fingers too far apart=({string.Join(",", distBetween3Fingers)})");
                        return;
                    }

                    this.StartDrag();
                }

                //TipSwitch isn't reported, so we track elapsed time to check when touch leaves & returns.
                //when it returns, we record position of first touch and only move cursor when second touch
                //occurs so relative positioning is computed correctly
                long last3fTouchTime = this.timeSincePrev3fTouchWatch.ElapsedTicks;
                if (this.timeSincePrev3fTouchWatch.IsRunning
                    && last3fTouchTime < this.tfd_TimeSinceLast3fTouchMinMillis * TimeSpan.TicksPerMillisecond)
                {
                    //ensure time passed is > 0
                    long timeClamped = Math.Max(1, last3fTouchTime / TimeSpan.TicksPerMillisecond);

                    //velocity = dist / time
                    double velocityX =
                        Utils.ClampValue(
                            Math.Abs(deltaX / timeClamped),
                            this.tfd_DragVelocityLowerBoundX,
                            this.tfd_DragVelocityUpperBoundX);
                    double velocityY =
                        Utils.ClampValue(
                            Math.Abs(deltaY / timeClamped),
                            this.tfd_DragVelocityLowerBoundY,
                            this.tfd_DragVelocityUpperBoundY);

                    int dX = (int)Utils.DirectionalCeil(deltaX * velocityX * this.tfd_DragSpeedMultiplier);
                    int dY = (int)Utils.DirectionalCeil(deltaY * velocityY * this.tfd_DragSpeedMultiplier);
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
                    this.Logger.Debug($"moved dist={dist}");

                    if (this.tfd_DragEndOnNewGestureMinDist < dist && dist < this.tfd_DragEndOnNewGestureMaxDist)
                    {
                        if (++this.dragEndConfidence > this.tfd_DragEndConfidenceThreshold)
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
                && this.timeSincePrev3fTouchWatch.ElapsedTicks > this.tfd_DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond)
            {
                this.StopDrag();
                this.Logger.Debug("3 fingers left trackpad, stop drag");
            }
        }
    }
}