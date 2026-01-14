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
        public static long tpb_DragEndMillisecondsThreshold { get; private set; }
        public static Point[] tpb_TouchBoundsPolygon { get; private set; }

        public static void LoadVariables()
        {
            EnvConfig.tpb_EnableDebugMode = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tpb_EnableDebugMode));
            EnvConfig.tpb_EnableDetailedTrackpadLogging = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tpb_EnableDetailedTrackpadLogging));
            EnvConfig.tpb_EnableTrackpadBlock = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tpb_EnableTrackpadBlock));
            EnvConfig.tpb_IsProcessDPIAware = EnvConfig.LoadEnvVar<bool>(nameof(EnvConfig.tpb_IsProcessDPIAware));
            EnvConfig.tpb_DragEndMillisecondsThreshold = EnvConfig.LoadEnvVar<long>(nameof(EnvConfig.tpb_DragEndMillisecondsThreshold));
            EnvConfig.tpb_TouchBoundsPolygon = EnvConfig.DeserializeJson<Point[]>(EnvConfig.LoadEnvVar<string>(nameof(EnvConfig.tpb_TouchBoundsPolygon)));
        }

        private static T DeserializeJson<T>(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                return default;
            }

            return new JavaScriptSerializer().Deserialize<T>(jsonString);
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
