using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DCLoader.Core
{
    // Persists mod enabled/disabled state to loader/config/mod-state.json.
    internal static class ModStateStore
    {
        private static string _path = null;

        public static void Initialize(string loaderDir)
        {
            string configDir = Path.Combine(loaderDir, "config");
            Directory.CreateDirectory(configDir);
            _path = Path.Combine(configDir, "mod-state.json");
        }

        public static Dictionary<string, bool> Load()
        {
            if (_path == null || !File.Exists(_path))
                return new Dictionary<string, bool>();

            try
            {
                string json = File.ReadAllText(_path);
                var result = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                return result ?? new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                DcLogger.Warn("ModStateStore",
                    $"[ModStateStore] Could not read mod-state.json, using defaults: {ex.Message}");
                return new Dictionary<string, bool>();
            }
        }

        // write-then-rename for atomic save
        public static void Save(Dictionary<string, bool> state)
        {
            if (_path == null) return;

            try
            {
                string tmp = _path + ".tmp";
                string json = JsonSerializer.Serialize(state,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tmp, json);
                File.Move(tmp, _path, overwrite: true);
            }
            catch (Exception ex)
            {
                DcLogger.Warn("ModStateStore",
                    $"[ModStateStore] Could not save mod-state.json: {ex.Message}");
            }
        }
    }
}
