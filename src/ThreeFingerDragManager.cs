using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace tfd
{
    public class ThreeFingerDragManager
    {
        private readonly double DragSpeedMultiplier;
        private readonly double DragStartFingersApartDistThreshold;
        private readonly double DragVelocityUpperBoundX;
        private readonly double DragVelocityUpperBoundY;
        private readonly double DragEndOnNewGestureMinDist;
        private readonly double DragEndOnNewGestureMaxDist;
        private readonly long DragEndMillisecondsThreshold;
        private readonly int DragEndConfidenceThreshold;
        private readonly int TimeSinceLast3fTouchMinMillis;
        private readonly int TrackpadCoordsDivByDenomSize;

        private readonly int ScreenWidth;
        private readonly int ScreenHeight;
        private readonly Timer monitorThreeFingersOnTrackpad = new Timer(100);
        private readonly Stopwatch timeSincePrev3fTouchWatch = new Stopwatch();
        private readonly ILogger logger;

        private bool isDragging = false;
        private int dragEndConfidence = 0;
        private int prevContactsCount = 0;
        private double prevTrackpadX = 0;
        private double prevTrackpadY = 0;

        public ThreeFingerDragManager(IContext appContext)
        {
            this.logger = appContext.GetLogger();

            this.DragSpeedMultiplier = appContext.LoadEnvVar(nameof(this.DragSpeedMultiplier), 1.5f);
            this.DragStartFingersApartDistThreshold = appContext.LoadEnvVar(nameof(this.DragStartFingersApartDistThreshold), 2.5f);
            this.DragVelocityUpperBoundX = appContext.LoadEnvVar(nameof(this.DragVelocityUpperBoundX), 5f);
            this.DragVelocityUpperBoundY = appContext.LoadEnvVar(nameof(this.DragVelocityUpperBoundY), 5f);
            this.DragEndOnNewGestureMinDist = appContext.LoadEnvVar(nameof(this.DragEndOnNewGestureMinDist), 0);
            this.DragEndOnNewGestureMaxDist = appContext.LoadEnvVar(nameof(this.DragEndOnNewGestureMaxDist), 100);
            this.DragEndMillisecondsThreshold = appContext.LoadEnvVar(nameof(this.DragEndMillisecondsThreshold), 1000);
            this.DragEndConfidenceThreshold = appContext.LoadEnvVar(nameof(this.DragEndConfidenceThreshold), 5);
            this.TimeSinceLast3fTouchMinMillis = appContext.LoadEnvVar(nameof(this.TimeSinceLast3fTouchMinMillis), 50);
            this.TrackpadCoordsDivByDenomSize = appContext.LoadEnvVar(nameof(this.TrackpadCoordsDivByDenomSize), 1);

            this.ScreenWidth = win32.GetSystemMetrics(win32.SM_CXSCREEN);
            this.ScreenHeight = win32.GetSystemMetrics(win32.SM_CYSCREEN);

            this.monitorThreeFingersOnTrackpad.Elapsed += this.CheckIfThreeFingersStillOnTrackpadHandler;
        }

        public void ProcessInput(IntPtr lParam)
        {
            TrackpadContact[] contacts = TrackpadHelper.ParseInput(lParam);
            if (contacts == null || contacts.Length == 0) return;

            // get approx. mean of all three fingers contact position
            double trackpadX = contacts[0].X;
            double trackpadY = contacts[0].Y;
            for (int i = 1; i < contacts.Length; ++i)
            {
                trackpadX += contacts[i].X;
                trackpadY += contacts[i].Y;
            }
            int divByDenomSizeClamped = Math.Max(1, contacts.Length + this.TrackpadCoordsDivByDenomSize);
            trackpadX = Math.Ceiling(trackpadX / divByDenomSizeClamped);
            trackpadY = Math.Ceiling(trackpadY / divByDenomSizeClamped);

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

                    if (distBetween3Fingers.Max() > (distBetween3Fingers.Min() * this.DragStartFingersApartDistThreshold))
                    {
                        this.logger.Debug($"3fingers too far apart=({string.Join(",", distBetween3Fingers)})");
                        return;
                    }

                    win32.GetCursorPos(out win32.MousePoint currPos);
                    win32.mouse_event(
                        win32.MOUSEEVENTF_LEFTDOWN | win32.MOUSEEVENTF_MOVE | win32.MOUSEEVENTF_ABSOLUTE,
                        Utils.TranslateCoordToAbsolute(currPos.X, this.ScreenWidth),
                        Utils.TranslateCoordToAbsolute(currPos.Y, this.ScreenHeight),
                        0,
                        0);

                    this.monitorThreeFingersOnTrackpad.Start();
                    this.isDragging = true;
                }

                /// since Tip Switch is not reported, we have to track passed time to see when touch leaves
                /// trackpad and comes back. when it comes back, we record position of first touch and move
                /// cursor when second touch happens so we have relative positioning computed correctly
                long last3fTime = this.timeSincePrev3fTouchWatch.ElapsedTicks;
                if (this.timeSincePrev3fTouchWatch.IsRunning
                    && last3fTime < this.TimeSinceLast3fTouchMinMillis * TimeSpan.TicksPerMillisecond)
                {
                    long timeClamped = Math.Max(1, last3fTime);

                    double deltaX = trackpadX - this.prevTrackpadX;
                    double deltaY = trackpadY - this.prevTrackpadY;

                    // velocity = dist / milliseconds
                    double velocityX = (deltaX * TimeSpan.TicksPerMillisecond) / timeClamped;
                    double velocityY = (deltaY * TimeSpan.TicksPerMillisecond) / timeClamped;

                    // clamp velocity & abs since deltaX/Y are signed
                    velocityX = Math.Min(Math.Max(Math.Abs(velocityX), 1f), this.DragVelocityUpperBoundX);
                    velocityY = Math.Min(Math.Max(Math.Abs(velocityY), 1f), this.DragVelocityUpperBoundY);

                    win32.GetCursorPos(out win32.MousePoint currPos);
                    currPos.X += (int)(deltaX * velocityX * this.DragSpeedMultiplier);
                    currPos.Y += (int)(deltaY * velocityY * this.DragSpeedMultiplier);

                    win32.mouse_event(
                        win32.MOUSEEVENTF_MOVE | win32.MOUSEEVENTF_ABSOLUTE,
                        Utils.TranslateCoordToAbsolute(currPos.X, this.ScreenWidth),
                        Utils.TranslateCoordToAbsolute(currPos.Y, this.ScreenHeight),
                        0,
                        0);
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
                    this.logger.Debug($"DIST={dist}");

                    if (this.DragEndOnNewGestureMinDist < dist && dist < this.DragEndOnNewGestureMaxDist)
                    {
                        if (++this.dragEndConfidence > this.DragEndConfidenceThreshold)
                        {
                            this.StopDrag();
                            this.logger.Debug($"new gesture, stopping drag");
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
            this.monitorThreeFingersOnTrackpad.Stop();

            win32.GetCursorPos(out win32.MousePoint currPos);
            win32.mouse_event(
                win32.MOUSEEVENTF_LEFTUP | win32.MOUSEEVENTF_ABSOLUTE,
                Utils.TranslateCoordToAbsolute(currPos.X, this.ScreenWidth),
                Utils.TranslateCoordToAbsolute(currPos.Y, this.ScreenHeight),
                0,
                0);

            this.dragEndConfidence = 0;
            this.prevTrackpadX = 0;
            this.prevTrackpadY = 0;
            this.prevContactsCount = 0;
            this.timeSincePrev3fTouchWatch.Reset();
        }

        private void CheckIfThreeFingersStillOnTrackpadHandler(object sender, ElapsedEventArgs e)
        {
            if (this.timeSincePrev3fTouchWatch.IsRunning
                && this.DragEndMillisecondsThreshold * TimeSpan.TicksPerMillisecond < this.timeSincePrev3fTouchWatch.ElapsedTicks)
            {
                this.StopDrag();
                this.logger.Debug("3 fingers left trackpad, stopped drag");
            }
        }
    }
}