using System;
using System.Collections.Generic;
using System.IO;
using Timer = System.Timers.Timer;

namespace DCLoader.Config
{
    // Watches config dir for .toml changes, debounces, and reloads the right ConfigFile.
    // Handles both Changed and Renamed events (editors do write-then-rename).
    internal sealed class ConfigWatcher : IDisposable
    {
        private readonly Dictionary<string, ConfigFile> _filesByModId;
        private FileSystemWatcher _watcher;

        private readonly Dictionary<string, Timer> _debounceTimers = new Dictionary<string, Timer>();
        private readonly object _lock = new object();

        private bool _disposed;

        public ConfigWatcher(Dictionary<string, ConfigFile> filesByModId)
        {
            _filesByModId = filesByModId ?? throw new ArgumentNullException(nameof(filesByModId));
        }

        public void Start(string configDir = null)
        {
            string dir = configDir ?? DcPaths.ConfigDir;

            if (!Directory.Exists(dir))
            {
                DCLoader.Core.DcLogger.Warn("ConfigWatcher",
                    $"[ConfigWatcher] ConfigDir not found ({dir}), live reload disabled.");
                return;
            }

            _watcher = new FileSystemWatcher(dir, "*.toml")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileEvent;
            _watcher.Renamed += OnRenamedEvent;

            DCLoader.Core.DcLogger.Info("ConfigWatcher",
                $"[ConfigWatcher] Watching {dir} for live reload.");
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            var modId = Path.GetFileNameWithoutExtension(e.Name);
            ScheduleReload(modId);
        }

        private void OnRenamedEvent(object sender, RenamedEventArgs e)
        {
            if (!e.Name.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)) return;
            var modId = Path.GetFileNameWithoutExtension(e.Name);
            ScheduleReload(modId);
        }

        private void ScheduleReload(string modId)
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_debounceTimers.TryGetValue(modId, out var existing))
                {
                    existing.Stop();
                    existing.Start();
                    return;
                }

                var timer = new Timer(300) { AutoReset = false };
                timer.Elapsed += (_, _) => ExecuteReload(modId);
                _debounceTimers[modId] = timer;
                timer.Start();
            }
        }

        private void ExecuteReload(string modId)
        {
            lock (_lock)
            {
                _debounceTimers.Remove(modId);
            }

            if (!_filesByModId.TryGetValue(modId, out var configFile))
            {
                DCLoader.Core.DcLogger.Warn("ConfigWatcher",
                    $"[ConfigWatcher] Reload triggered for unknown mod '{modId}' — ignoring.");
                return;
            }

            try
            {
                configFile.Reload();
                DCLoader.Core.DcLogger.Info("ConfigWatcher",
                    $"[ConfigWatcher] Reloaded config for mod '{modId}'.");
            }
            catch (Exception ex)
            {
                DCLoader.Core.DcLogger.Error("ConfigWatcher",
                    $"[ConfigWatcher] Reload failed for '{modId}': {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _watcher?.Dispose();

            lock (_lock)
            {
                foreach (var t in _debounceTimers.Values)
                    t.Dispose();
                _debounceTimers.Clear();
            }
        }
    }
}
