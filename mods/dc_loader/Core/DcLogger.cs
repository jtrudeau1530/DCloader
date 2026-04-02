using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DCLoader.Core
{
    // Simple thread-safe file logger. Must be initialized before anything else touches disk.
    public static class DcLogger
    {
        private static StreamWriter? _writer;
        private static readonly object _lock = new object();

        public static void Initialize(string logsDir)
        {
            Directory.CreateDirectory(logsDir);
            string logPath = Path.Combine(logsDir,
                $"dc_loader_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _writer = new StreamWriter(logPath, append: false) { AutoFlush = true };
            Log(LogLevel.Info, "DcLogger", $"Log started: {logPath}");
        }

        // Pops up a console window on Windows/Wine. No-op on native Linux.
        public static void OpenConsole()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AllocConsole();
                Console.Title = "dc_loader log";
                Log(LogLevel.Info, "DcLogger", "AllocConsole opened");
            }
            // native Linux users just tail the log file
        }

        public static void Info(string source, string message) =>
            Log(LogLevel.Info, source, message);

        public static void Warn(string source, string message) =>
            Log(LogLevel.Warn, source, message);

        public static void Error(string source, string message) =>
            Log(LogLevel.Error, source, message);

        private static void Log(LogLevel level, string source, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}][{level,5}][{source}] {message}";
            lock (_lock)
            {
                _writer?.WriteLine(line);
                Console.WriteLine(line); // also writes to AllocConsole window if open
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        public enum LogLevel { Info, Warn, Error }
    }
}
