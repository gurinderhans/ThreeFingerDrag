using System;
using System.Collections.Generic;

namespace tfd
{
    public interface ILogger
    {
        void Info(string text);
        void Debug(string text);
        void Error(string text);
    }

    public class Logger : ILogger
    {
        public bool RecordDebugLogs { get; set; }

        protected List<string> Logs = new List<string>();

        public void Clear() => this.Logs.Clear();
        public void Info(string text) => this.Log("Info", text);
        public void Error(string text) => this.Log("Error", text);

        public void Debug(string text)
        {
            if (this.RecordDebugLogs)
            {
                this.Log("Debug", text);
                System.Diagnostics.Debug.WriteLine(text);
            }
        }

        public string GetLogsAsString()
        {
            if (this.Logs.Count <= 0) return "<no logs>";
            return string.Join(Environment.NewLine, this.Logs);
        }

        private void Log(string type, string text)
        {
            this.Logs.Add($"[{DateTime.UtcNow:hh:mm:ss.fff tt}][{type}][{text}]");
        }
    }
}
