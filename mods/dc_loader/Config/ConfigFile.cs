using System;
using System.Collections.Generic;
using System.IO;
using DCLoader.Core;

namespace DCLoader.Config
{
    /// <summary>
    /// Per-mod TOML config file (loader/config/{modId}.toml).
    ///
    /// Usage:
    ///   var entry = config.Bind&lt;float&gt;("damage", 1.5f, "Damage multiplier");
    ///   float val = entry.Value;
    ///   entry.OnValueChanged += (old, @new) => ...;
    ///
    /// First Bind() call creates the file if it doesn't exist. ConfigWatcher handles live reload.
    /// </summary>
    public sealed class ConfigFile
    {
        private readonly string _modId;
        private readonly string _filePath;
        private readonly Dictionary<string, IConfigEntryBase> _entries
            = new Dictionary<string, IConfigEntryBase>(StringComparer.OrdinalIgnoreCase);

        public ConfigFile(string modId)
            : this(modId, DcPaths.ConfigDir) { }

        // explicit dir for tests
        internal ConfigFile(string modId, string configDir)
        {
            _modId = modId ?? throw new ArgumentNullException(nameof(modId));
            _filePath = Path.Combine(configDir, modId + ".toml");
        }

        public IReadOnlyDictionary<string, IConfigEntryBase> Entries => _entries;

        /// <summary>
        /// Declare a config key. Loads from disk if file exists, otherwise creates
        /// the file with defaults. Same key = same instance on repeat calls.
        /// </summary>
        public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description = "")
        {
            if (_entries.TryGetValue(key, out var existing))
                return (ConfigEntry<T>)existing;

            var entry = new ConfigEntry<T>(key, defaultValue, description);
            _entries[key] = entry;

            if (File.Exists(_filePath))
                Load();
            else
                Save();

            return entry;
        }

        // write-then-rename for atomic saves
        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var sb = new System.Text.StringBuilder();
                foreach (var kvp in _entries)
                {
                    IConfigEntryBase e = kvp.Value;
                    if (!string.IsNullOrEmpty(e.Description))
                        sb.AppendLine($"# {e.Description}");
                    sb.AppendLine($"{e.Key} = {e.ToTomlValueString()}");
                    sb.AppendLine();
                }

                string tmp = _filePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), System.Text.Encoding.UTF8);
                File.Move(tmp, _filePath, overwrite: true);
            }
            catch (Exception ex)
            {
                DcLogger.Error("ConfigFile", $"[ConfigFile:{_modId}] Save failed: {ex.Message}");
            }
        }

        // Loads from disk silently (no OnValueChanged). Called by Bind() automatically.
        public void Load()
        {
            if (!File.Exists(_filePath)) return;

            try
            {
                string text = File.ReadAllText(_filePath, System.Text.Encoding.UTF8);
                var table = ParseSimpleToml(text);

                foreach (var kvp in _entries)
                {
                    if (table.TryGetValue(kvp.Key, out object rawValue))
                        kvp.Value.SetFromToml(rawValue);
                    // not in file = keep default, that's fine
                }

                // let devs know if their config file has stale keys
                foreach (var fileKey in table.Keys)
                {
                    if (!_entries.ContainsKey(fileKey))
                        DcLogger.Warn("ConfigFile",
                            $"[ConfigFile:{_modId}] Unknown key in TOML: '{fileKey}'");
                }
            }
            catch (Exception ex)
            {
                DcLogger.Error("ConfigFile", $"[ConfigFile:{_modId}] Load failed: {ex.Message}");
            }
        }

        // Reloads from disk and fires OnValueChanged for anything that changed.
        // Called by ConfigWatcher on file modification.
        public void Reload()
        {
            if (!File.Exists(_filePath)) return;

            try
            {
                string text = File.ReadAllText(_filePath, System.Text.Encoding.UTF8);
                var table = ParseSimpleToml(text);

                foreach (var kvp in _entries)
                {
                    if (table.TryGetValue(kvp.Key, out object rawValue))
                        kvp.Value.SetFromTomlAndNotify(rawValue);
                }
            }
            catch (Exception ex)
            {
                DcLogger.Error("ConfigFile", $"[ConfigFile:{_modId}] Reload failed: {ex.Message}");
            }
        }

        // Minimal TOML parser -- only handles the flat key=value stuff we write.
        // No arrays, no tables, no multi-line strings. Ugly but it works.
        private static Dictionary<string, object> ParseSimpleToml(string text)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                int eqIdx = line.IndexOf('=');
                if (eqIdx < 1) continue;

                string key = line.Substring(0, eqIdx).Trim();
                string valueStr = line.Substring(eqIdx + 1).Trim();

                valueStr = StripInlineComment(valueStr);

                object parsed = ParseTomlValue(valueStr);
                if (parsed != null)
                    result[key] = parsed;
            }

            return result;
        }

        private static string StripInlineComment(string s)
        {
            if (s.StartsWith("\""))
                return s; // don't strip # inside quoted strings

            int hash = s.IndexOf('#');
            return hash >= 0 ? s.Substring(0, hash).TrimEnd() : s;
        }

        private static object ParseTomlValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;

            if (s == "true")  return (object)true;
            if (s == "false") return (object)false;

            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                return UnescapeTomlString(s.Substring(1, s.Length - 2));

            if (!s.Contains('.') && !s.Contains('e') && !s.Contains('E'))
            {
                if (long.TryParse(s, out long l)) return (object)l;
            }

            if (double.TryParse(s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double d))
                return (object)d;

            // raw string fallback (handles unquoted enum values)
            return (object)s;
        }

        private static string UnescapeTomlString(string s)
            => s.Replace("\\\"", "\"")
               .Replace("\\n", "\n")
               .Replace("\\r", "\r")
               .Replace("\\\\", "\\");
    }
}
