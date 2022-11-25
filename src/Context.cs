using System;

namespace tfd
{
    public interface IContext
    {
        ILogger GetLogger();
        T LoadEnvVar<T>(string envVarName, T defaultValue);
    }

    public class Context : IContext
    {
        public readonly Logger Logger = new Logger();

        public Context()
        {
            this.Logger.RecordDebugLogs = this.LoadEnvVar(nameof(this.Logger.RecordDebugLogs), false);
        }

        public ILogger GetLogger() => this.Logger;

        public T LoadEnvVar<T>(string varName, T defaultValue)
        {
            string rawValue = Environment.GetEnvironmentVariable(varName);
            if (rawValue == null)
            {
                this.Logger.Info($"env set default, {varName}={defaultValue}");
                return defaultValue;
            }

            T loadedValue = (T)Convert.ChangeType(rawValue, typeof(T));
            this.Logger.Info($"env loaded, {varName}={loadedValue}");
            return loadedValue;
        }
    }
}
