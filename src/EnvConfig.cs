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
            EnvConfig.tfd_EnableDebugMode = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_EnableDebugMode), false);
            EnvConfig.tfd_IsProcessDPIAware = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_IsProcessDPIAware), false);
            EnvConfig.tfd_EnableThreeFingerDrag = EnvConfig.LoadEnvVar(nameof(tfd_EnableThreeFingerDrag), true);
            EnvConfig.tfd_DragSpeedMultiplier = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragSpeedMultiplier), 1f);
            EnvConfig.tfd_DragVelocityLowerBoundX = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragVelocityLowerBoundX), 1f);
            EnvConfig.tfd_DragVelocityUpperBoundX = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragVelocityUpperBoundX), 1f);
            EnvConfig.tfd_DragVelocityLowerBoundY = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragVelocityLowerBoundY), 1f);
            EnvConfig.tfd_DragVelocityUpperBoundY = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragVelocityUpperBoundY), 1f);
            EnvConfig.tfd_DragStartFingersDistThresholdMultiplier = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragStartFingersDistThresholdMultiplier), 3f);
            EnvConfig.tfd_DragEndOnNewGestureMinDist = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragEndOnNewGestureMinDist), 0f);
            EnvConfig.tfd_DragEndOnNewGestureMaxDist = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragEndOnNewGestureMaxDist), 100f);
            EnvConfig.tfd_DragEndMillisecondsThreshold = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragEndMillisecondsThreshold), 1000);
            EnvConfig.tfd_DragEndConfidenceThreshold = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_DragEndConfidenceThreshold), 5);
            EnvConfig.tfd_TrackpadCoordsScaleFactor = EnvConfig.LoadEnvVar<int?>(nameof(EnvConfig.tfd_TrackpadCoordsScaleFactor), null);
            EnvConfig.tfd_TimeSinceLast3fTouchMinMillis = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_TimeSinceLast3fTouchMinMillis), 50);
            EnvConfig.tfd_Monitor3fOnTrackpadInterval = EnvConfig.LoadEnvVar(nameof(EnvConfig.tfd_Monitor3fOnTrackpadInterval), 100);
        }

        private static T LoadEnvVar<T>(string varName, T defaultValue)
        {
            string rawValue = Environment.GetEnvironmentVariable(varName);
            if (rawValue == null)
            {
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
