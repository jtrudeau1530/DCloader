using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DCLoader.Core;
using Xunit;

namespace DCLoader.Tests
{
    public class DcLoggerTests : IDisposable
    {
        private readonly string _tempDir;

        public DcLoggerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"dc_loader_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            // Clean up temp directory
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Fact]
        public void Initialize_CreatesLogFileInLogsDir()
        {
            // DcLogger.Initialize() should create a log file in the specified directory
            DcLogger.Initialize(_tempDir);

            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.Single(files);
        }

        [Fact]
        public void Initialize_LogFileNameMatchesPattern()
        {
            // Log file name must match dc_loader_yyyyMMdd_HHmmss.log
            DcLogger.Initialize(_tempDir);

            var files = Directory.GetFiles(_tempDir, "*.log");
            var fileName = Path.GetFileName(files[0]);
            var pattern = new Regex(@"^dc_loader_\d{8}_\d{6}\.log$");
            Assert.Matches(pattern, fileName);
        }

        [Fact]
        public void Info_WritesTimestampedEntryWithLevelAndSource()
        {
            DcLogger.Initialize(_tempDir);
            DcLogger.Info("Bootstrap", "test");

            var logPath = Directory.GetFiles(_tempDir, "*.log")[0];
            var lines = File.ReadAllLines(logPath);
            // Find the Info line (skip the "Log started" entry from Initialize)
            var infoLine = lines.FirstOrDefault(l => l.Contains("[Bootstrap]") && l.Contains("test"));
            Assert.NotNull(infoLine);
            // Format: [HH:mm:ss.fff][ Info][Bootstrap] test
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]\[ Info\]\[Bootstrap\] test", infoLine);
        }

        [Fact]
        public void Warn_WritesEntryWithWarnLevel()
        {
            DcLogger.Initialize(_tempDir);
            DcLogger.Warn("Source", "msg");

            var logPath = Directory.GetFiles(_tempDir, "*.log")[0];
            var content = File.ReadAllText(logPath);
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]\[ Warn\]\[Source\] msg", content);
        }

        [Fact]
        public void Error_WritesEntryWithErrorLevel()
        {
            DcLogger.Initialize(_tempDir);
            DcLogger.Error("Source", "msg");

            var logPath = Directory.GetFiles(_tempDir, "*.log")[0];
            var content = File.ReadAllText(logPath);
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\.\d{3}\]\[Error\]\[Source\] msg", content);
        }

        [Fact]
        public void ConcurrentWrites_DoNotInterleave()
        {
            DcLogger.Initialize(_tempDir);

            // Fire 100 concurrent writes from 10 threads — lines must not be interleaved
            const int threadCount = 10;
            const int writesPerThread = 10;
            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < writesPerThread; i++)
                        DcLogger.Info($"Thread{threadId}", $"message {i}");
                });
            }
            Task.WaitAll(tasks);

            var logPath = Directory.GetFiles(_tempDir, "*.log")[0];
            var lines = File.ReadAllLines(logPath);
            // Each line must be a complete log entry (starts with '[')
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                Assert.StartsWith("[", line);
                // Must match the full pattern — no partial writes
                Assert.Matches(@"^\[\d{2}:\d{2}:\d{2}\.\d{3}\]", line);
            }
        }

        [Fact]
        public void OpenConsole_DoesNotThrowOnNonWindowsPlatform()
        {
            DcLogger.Initialize(_tempDir);
            // On Linux (non-Wine), this must be a no-op and not throw
            var exception = Record.Exception(() => DcLogger.OpenConsole());
            Assert.Null(exception);
        }
    }
}
