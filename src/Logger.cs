namespace tfd
{
    using System;
    using System.Collections.Generic;

    public interface ILogger
    {
        void Info(string text);
        void Debug(string text);
        void Error(string text);
    }

    public class Logger : ILogger
    {
        private List<string> logs = new List<string>();

        private readonly bool recordDebugLogs;

        public Logger(bool recordDebugLogs) => this.recordDebugLogs = recordDebugLogs;

        public void Clear() => this.logs.Clear();
        public void Info(string text) => this.Log("Info", text);
        public void Error(string text) => this.Log("Error", text);
        public void Debug(string text) => this.Log("Debug", text);

        private void Log(string type, string text)
        {
            string logText = $"[{DateTime.UtcNow:hh:mm:ss.fff tt}][{type}][{text}]";
            if (this.recordDebugLogs)
            {
                System.Diagnostics.Debug.WriteLine(logText);
                this.logs.Add(logText);
            }
            else if (type != "Debug")
            {
                this.logs.Add(logText);
            }
        }

        public string GetLogsAsString()
        {
            return this.logs.Count == 0
                ? "<no logs>"
                : string.Join(Environment.NewLine, this.logs);
        }
    }
}
