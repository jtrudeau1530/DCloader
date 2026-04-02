using System;
using System.Collections.Generic;
using System.Linq;
using DCLoader.Config;
using HarmonyLib;

namespace DCLoader.Core
{
    // Central mod registry. Tracks state, dispatches lifecycle events, handles enable/disable.
    internal class ModRegistry
    {
        public static ModRegistry Instance { get; private set; }

        private const int ErrorThreshold = 30; // auto-disable after this many consecutive errors

        private readonly Dictionary<string, ModEntry> _mods = new();
        private readonly List<ModEntry> _loadOrder = new();
        private Dictionary<string, bool> _persistedState = new();
        private ConfigWatcher _configWatcher;

        public IReadOnlyList<ModEntry> LoadOrder => _loadOrder;

        public void Initialize()
        {
            ModStateStore.Initialize(DcPaths.LoaderDir);
            _persistedState = ModStateStore.Load();
            DcLogger.Info("ModRegistry", $"[ModRegistry] Loaded persisted state for {_persistedState.Count} mod(s).");
            Instance = this;
            DcLogger.Info("ModRegistry", "ModRegistry initialized.");
        }

        public void RegisterMods(List<ModEntry> sortedMods)
        {
            foreach (var entry in sortedMods)
            {
                entry.LogSource = (msg) => DcLogger.Info(entry.Info.Name, msg);
                entry.Harmony = new Harmony($"dc.mod.{entry.Info.ID}");
                entry.ConfigFile = new ConfigFile(entry.Info.ID);

                try
                {
                    entry.Instance = (Mod)Activator.CreateInstance(entry.ModType);
                    entry.Instance.Logger = entry.LogSource;
                    entry.Instance.Harmony = entry.Harmony;
                    entry.Instance.Config = entry.ConfigFile;
                }
                catch (Exception ex)
                {
                    DcLogger.Error("ModRegistry",
                        $"[ModRegistry] Failed to instantiate mod '{entry.Info.ID}': {ex.Message}");
                    continue;
                }

                // default to enabled if we haven't seen this mod before
                entry.Enabled = !_persistedState.TryGetValue(entry.Info.ID, out bool stored) || stored;

                _mods[entry.Info.ID] = entry;
                _loadOrder.Add(entry);

                DcLogger.Info("ModRegistry",
                    $"[ModRegistry] Registered: {entry.Info.Name} v{entry.Info.Version} by {entry.Info.Author} (enabled={entry.Enabled})");
            }

            // start watching config files for live reload
            var configFiles = new Dictionary<string, ConfigFile>();
            foreach (var e in _mods.Values)
            {
                if (e.ConfigFile != null)
                    configFiles[e.Info.ID] = e.ConfigFile;
            }
            _configWatcher = new ConfigWatcher(configFiles);
            _configWatcher.Start();
        }

        public void DispatchOnLoad()
        {
            foreach (var entry in _loadOrder.ToList())
            {
                if (!entry.Enabled || entry.Instance == null) continue;
                try
                {
                    entry.Instance.OnLoad();
                    entry.ConsecutiveErrorCount = 0;
                }
                catch (Exception ex)
                {
                    entry.ConsecutiveErrorCount++;
                    DcLogger.Error("ModRegistry",
                        $"[{entry.Info.Name}] OnLoad error ({entry.ConsecutiveErrorCount}/{ErrorThreshold}): {ex.Message}");
                    if (entry.ConsecutiveErrorCount >= ErrorThreshold)
                    {
                        DcLogger.Error("ModRegistry",
                            $"[ModRegistry] {entry.Info.Name} auto-disabled after {ErrorThreshold} consecutive errors.");
                        DisableMod(entry.Info.ID);
                    }
                }
            }
        }

        public void DispatchOnUpdate()
        {
            foreach (var entry in _loadOrder)
            {
                if (!entry.Enabled || entry.Instance == null) continue;
                try
                {
                    entry.Instance.OnUpdate();
                    entry.ConsecutiveErrorCount = 0;
                }
                catch (Exception ex)
                {
                    entry.ConsecutiveErrorCount++;
                    DcLogger.Error("ModRegistry",
                        $"[{entry.Info.Name}] OnUpdate error ({entry.ConsecutiveErrorCount}/{ErrorThreshold}): {ex.Message}");
                    if (entry.ConsecutiveErrorCount >= ErrorThreshold)
                    {
                        DcLogger.Error("ModRegistry",
                            $"[ModRegistry] {entry.Info.Name} auto-disabled after {ErrorThreshold} consecutive errors.");
                        DisableMod(entry.Info.ID);
                    }
                }
            }
        }

        public void DispatchOnGUI()
        {
            foreach (var entry in _loadOrder.ToList())
            {
                if (!entry.Enabled || entry.Instance == null) continue;
                try
                {
                    entry.Instance.OnGUI();
                    entry.ConsecutiveErrorCount = 0;
                }
                catch (Exception ex)
                {
                    entry.ConsecutiveErrorCount++;
                    DcLogger.Error("ModRegistry",
                        $"[{entry.Info.Name}] OnGUI error ({entry.ConsecutiveErrorCount}/{ErrorThreshold}): {ex.Message}");
                    if (entry.ConsecutiveErrorCount >= ErrorThreshold)
                    {
                        DcLogger.Error("ModRegistry",
                            $"[ModRegistry] {entry.Info.Name} auto-disabled after {ErrorThreshold} consecutive errors.");
                        DisableMod(entry.Info.ID);
                    }
                }
            }
        }

        public void DispatchOnSave()
        {
            foreach (var entry in _loadOrder.ToList())
            {
                if (!entry.Enabled || entry.Instance == null) continue;
                try
                {
                    entry.Instance.OnSave();
                    entry.ConsecutiveErrorCount = 0;
                }
                catch (Exception ex)
                {
                    entry.ConsecutiveErrorCount++;
                    DcLogger.Error("ModRegistry",
                        $"[{entry.Info.Name}] OnSave error ({entry.ConsecutiveErrorCount}/{ErrorThreshold}): {ex.Message}");
                    if (entry.ConsecutiveErrorCount >= ErrorThreshold)
                    {
                        DcLogger.Error("ModRegistry",
                            $"[ModRegistry] {entry.Info.Name} auto-disabled after {ErrorThreshold} consecutive errors.");
                        DisableMod(entry.Info.ID);
                    }
                }
            }
        }

        public void DisableMod(string modId)
        {
            if (!_mods.TryGetValue(modId, out var entry)) return;
            if (!entry.Enabled) return;

            entry.Enabled = false;

            try { entry.Instance?.OnUnload(); }
            catch (Exception ex)
            {
                DcLogger.Error("ModRegistry",
                    $"[{entry.Info.Name}] OnUnload error: {ex}");
            }

            entry.Harmony?.UnpatchSelf();

            DcLogger.Info("ModRegistry", $"[ModRegistry] Disabled: {entry.Info.Name}");
            PersistState();
        }

        public void EnableMod(string modId)
        {
            if (!_mods.TryGetValue(modId, out var entry)) return;
            if (entry.Enabled) return;

            entry.Enabled = true;

            // re-call OnLoad so it can re-apply patches
            try { entry.Instance?.OnLoad(); }
            catch (Exception ex)
            {
                DcLogger.Error("ModRegistry",
                    $"[{entry.Info.Name}] OnLoad (re-enable) error: {ex}");
            }

            DcLogger.Info("ModRegistry", $"[ModRegistry] Enabled: {entry.Info.Name}");
            PersistState();
        }

        public void DispatchOnUnloadAll()
        {
            _configWatcher?.Dispose();

            foreach (var entry in _loadOrder)
            {
                if (entry.Instance == null) continue;
                try { entry.Instance.OnUnload(); }
                catch (Exception ex)
                {
                    DcLogger.Error("ModRegistry",
                        $"[{entry.Info.Name}] OnUnload error: {ex}");
                }
                entry.Harmony?.UnpatchSelf();
            }
        }

        private void PersistState()
        {
            var state = new Dictionary<string, bool>();
            foreach (var kvp in _mods)
                state[kvp.Key] = kvp.Value.Enabled;
            ModStateStore.Save(state);
        }

        public ModEntry GetMod(string modId)
        {
            return _mods.TryGetValue(modId, out var entry) ? entry : null;
        }

        public IReadOnlyCollection<ModEntry> GetAllMods() => _mods.Values;
    }
}
