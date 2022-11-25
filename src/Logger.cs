using System;
using System.Collections.Generic;

namespace tfd
{
    public interface ILogger
    {
        void Info(string text);
        void Debug(string text);
    }

    public class Logger : ILogger
    {
        public bool RecordDebugLogs { get; set; }

        protected List<string> Logs = new List<string>();

        public void Clear() => this.Logs.Clear();

        public void Info(string text)
        {
            string logText = $"[{DateTime.UtcNow.ToString("hh:mm:ss.fff tt")}] {text}";
            this.Logs.Add(logText);
            System.Diagnostics.Debug.WriteLine(logText);
        }

        public void Debug(string text)
        {
            if (this.RecordDebugLogs)
                this.Info(text);
        }

        public string GetLogsAsString()
        {
            if (this.Logs.Count <= 0) return "<no logs>";
            return string.Join(Environment.NewLine, this.Logs);
        }
    }
}
