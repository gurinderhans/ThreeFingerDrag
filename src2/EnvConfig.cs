namespace tpb
{
    using System;
    using System.Drawing;
    using System.Web.Script.Serialization;

    public static class EnvConfig
    {
        public static bool tpb_EnableDebugMode { get; private set; }
        public static bool tpb_EnableDetailedTrackpadLogging { get; private set; }
        public static bool tpb_EnableTrackpadBlock { get; private set; }
        public static bool tpb_IsProcessDPIAware { get; private set; }
        public static Point[] tpb_TouchBoundsPolygon { get; private set; }
        public static long tpb_DragEndMillisecondsThreshold { get; private set; }

        public static void LoadVariables()
        {
            EnvConfig.tpb_EnableDebugMode = EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_EnableDebugMode), false);
            EnvConfig.tpb_EnableDetailedTrackpadLogging = EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_EnableDetailedTrackpadLogging), false);
            EnvConfig.tpb_EnableTrackpadBlock = EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_EnableTrackpadBlock), true);
            EnvConfig.tpb_IsProcessDPIAware = EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_EnableTrackpadBlock), false);
            EnvConfig.tpb_TouchBoundsPolygon = new JavaScriptSerializer().Deserialize<Point[]>(EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_TouchBoundsPolygon), string.Empty));
            EnvConfig.tpb_DragEndMillisecondsThreshold = EnvConfig.LoadEnvVar(nameof(EnvConfig.tpb_DragEndMillisecondsThreshold), 300);
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
