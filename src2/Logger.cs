namespace tpb
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public interface ILogger
    {
        void Info(string text);
        void Error(string text);
        void Debug(string text);
    }

    public class Logger : ILogger
    {
        private List<string> logs = new List<string>();

        private static Logger _instance;
        public static ILogger Instance
        {
            get
            {
                if (_instance == null) _instance = new Logger();
                return _instance;
            }
        }

        public void Info(string text) => this.Log("Info", text);

        public void Error(string text) => this.Log("Error", text);

        public void Debug(string text) => this.Log("Debug", text);

        private void Log(string type, string text)
        {
            string logText = $"[{DateTime.UtcNow:hh:mm:ss.fff tt}][{type}][{new StackFrame(2).GetMethod().Name}][{text}]";
            if (EnvConfig.tpb_EnableDebugMode)
            {
                System.Diagnostics.Debug.WriteLine(logText);
                this.logs.Add(logText);
            }
            else if (type != "Debug")
            {
                this.logs.Add(logText);
            }
        }

        public void Clear() => this.logs.Clear();

        public string GetLogsAsString()
        {
            return this.logs.Count == 0
                ? "<no logs>"
                : string.Join(Environment.NewLine, this.logs);
        }
    }
}
