namespace tfd
{
    using System;

    public static class EnvConfig
    {
        public static bool tfd_EnableDebugMode { get; private set; }
        public static bool tfd_IsProcessDPIAware { get; private set; }
        public static bool tfd_EnableThreeFingerDrag { get; private set; }
        public static double tfd_DragSpeedMultiplier { get; private set; }
        public static double tfd_DragVelocityLowerBoundX { get; private set; }
        public static double tfd_DragVelocityUpperBoundX { get; private set; }
        public static double tfd_DragVelocityLowerBoundY { get; private set; }
        public static double tfd_DragVelocityUpperBoundY { get; private set; }
        public static double tfd_DragStartFingersDistThresholdMultiplier { get; private set; }
        public static double tfd_DragEndOnNewGestureMinDist { get; private set; }
        public static double tfd_DragEndOnNewGestureMaxDist { get; private set; }
        public static long tfd_DragEndMillisecondsThreshold { get; private set; }
        public static int tfd_DragEndConfidenceThreshold { get; private set; }
        public static int? tfd_TrackpadCoordsScaleFactor { get; private set; }
        public static int tfd_TimeSinceLast3fTouchMinMillis { get; private set; }
        public static int tfd_Monitor3fOnTrackpadInterval { get; private set; }

        public static void LoadVariables()
        {
            EnvConfig.tfd_EnableDebugMode = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tfd_EnableDebugMode));
            EnvConfig.tfd_IsProcessDPIAware = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tfd_IsProcessDPIAware));
            EnvConfig.tfd_EnableThreeFingerDrag = EnvConfig.LoadEnvVar<bool>(nameof(tfd_EnableThreeFingerDrag));
            EnvConfig.tfd_DragSpeedMultiplier = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragSpeedMultiplier));
            EnvConfig.tfd_DragVelocityLowerBoundX = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragVelocityLowerBoundX));
            EnvConfig.tfd_DragVelocityUpperBoundX = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragVelocityUpperBoundX));
            EnvConfig.tfd_DragVelocityLowerBoundY = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragVelocityLowerBoundY));
            EnvConfig.tfd_DragVelocityUpperBoundY = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragVelocityUpperBoundY));
            EnvConfig.tfd_DragStartFingersDistThresholdMultiplier = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragStartFingersDistThresholdMultiplier));
            EnvConfig.tfd_DragEndOnNewGestureMinDist = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragEndOnNewGestureMinDist));
            EnvConfig.tfd_DragEndOnNewGestureMaxDist = EnvConfig.LoadEnvVar<double>(nameof(EnvConfig.tfd_DragEndOnNewGestureMaxDist));
            EnvConfig.tfd_DragEndMillisecondsThreshold = EnvConfig.LoadEnvVar<long>(nameof(EnvConfig.tfd_DragEndMillisecondsThreshold));
            EnvConfig.tfd_DragEndConfidenceThreshold = EnvConfig.LoadEnvVar<int>(nameof(EnvConfig.tfd_DragEndConfidenceThreshold));
            EnvConfig.tfd_TrackpadCoordsScaleFactor = EnvConfig.LoadEnvVar<int?>(nameof(EnvConfig.tfd_TrackpadCoordsScaleFactor));
            EnvConfig.tfd_TimeSinceLast3fTouchMinMillis = EnvConfig.LoadEnvVar<int>(nameof(EnvConfig.tfd_TimeSinceLast3fTouchMinMillis));
            EnvConfig.tfd_Monitor3fOnTrackpadInterval = EnvConfig.LoadEnvVar<int>(nameof(EnvConfig.tfd_Monitor3fOnTrackpadInterval));
        }

        private static T LoadEnvVar<T>(string varName)
        {
            string rawValue = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrEmpty(rawValue))
            {
                T defaultValue = default;
                Logger.Instance.Info($"env set default, {varName}={defaultValue}");
                return defaultValue;
            }

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            T loadedValue = (T)Convert.ChangeType(rawValue, targetType);

            Logger.Instance.Info($"env loaded, {varName}={loadedValue}");
            return loadedValue;
        }
    }
}
