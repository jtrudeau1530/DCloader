using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCLoader.Core
{
    public class ModLoaderBehaviour : MonoBehaviour
    {
        // IL2CPP needs this or it explodes with MissingMethodException
        public ModLoaderBehaviour(IntPtr ptr) : base(ptr) { }

        private GCHandle _selfHandle;
        private static ModLoaderBehaviour _instance = null!;

        private bool _modsDiscovered = false;
        private bool _modsLoaded = false;

        private void Awake()
        {
            // prevent IL2CPP's Boehm GC from collecting us
            _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            _instance = this;

            DontDestroyOnLoad(gameObject);

            Directory.CreateDirectory(DcPaths.ModsDir);
            Directory.CreateDirectory(DcPaths.ReferencesDir);
            DcLogger.Info("ModLoader", $"ModLoaderBehaviour alive. Mods folder: {DcPaths.ModsDir}");

            var registry = new ModRegistry();
            registry.Initialize();

            // shared deps need to be loaded before we scan for mods
            ReferencesLoader.LoadAll(DcPaths.ReferencesDir);

            var discovered = ModLoader.DiscoverMods(DcPaths.ModsDir);
            DcLogger.Info("ModLoader", $"[ModLoader] Discovered {discovered.Count} mod(s)");

            var sorted = ModLoader.TopologicalSort(discovered);
            DcLogger.Info("ModLoader", $"[ModLoader] {sorted.Count} mod(s) after dependency resolution");

            registry.RegisterMods(sorted);
            _modsDiscovered = true;

            // IL2CPP type needs explicit delegate cast here
            try
            {
                SaveSystem.onSavingData += (SaveSystem.OnSavingData)OnSavingDataHandler;
                DcLogger.Info("ModLoader", "[ModLoader] SaveSystem.onSavingData hooked");
            }
            catch (Exception ex)
            {
                DcLogger.Warn("ModLoader",
                    $"[ModLoader] Failed to hook SaveSystem.onSavingData: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!_modsDiscovered) return;

            // wait for the game to actually be ready before calling OnLoad
            if (!_modsLoaded)
            {
                try
                {
                    if (MainGameManager.instance != null)
                    {
                        _modsLoaded = true;
                        DcLogger.Info("ModLoader",
                            "[ModLoader] MainGameManager ready — dispatching OnLoad()");
                        ModRegistry.Instance.DispatchOnLoad();
                        GameAPI.RaiseOnGameLoaded();
                        DcLogger.Info("ModLoader", "[ModLoader] GameAPI.OnGameLoaded fired.");
                    }
                }
                catch (Exception)
                {
                    // type might not be available yet, keep polling
                }
                return;
            }

            ModRegistry.Instance.DispatchOnUpdate();
        }

        private void OnGUI()
        {
            if (!_modsLoaded) return;
            ModRegistry.Instance.DispatchOnGUI();
        }

        private static void OnSavingDataHandler()
        {
            DcLogger.Info("ModLoader", "[ModLoader] Save detected — dispatching OnSave()");
            ModRegistry.Instance?.DispatchOnSave();
            GameAPI.RaiseOnGameSaved();
        }

        private void OnDestroy()
        {
            if (_selfHandle.IsAllocated) _selfHandle.Free();

            DcLogger.Info("ModLoader", "[ModLoader] Shutting down — dispatching OnUnload()");
            ModRegistry.Instance?.DispatchOnUnloadAll();

            try
            {
                SaveSystem.onSavingData -= (SaveSystem.OnSavingData)OnSavingDataHandler;
            }
            catch (Exception) { /* best effort cleanup */ }
        }
    }
}
