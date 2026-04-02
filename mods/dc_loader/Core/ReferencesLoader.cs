using System;
using System.IO;
using System.Reflection;

namespace DCLoader.Core
{
    // Loads shared dependency DLLs from Mods/References/ before mod scanning.
    // These are NOT scanned for Mod subclasses.
    internal static class ReferencesLoader
    {
        public static void LoadAll(string referencesDir)
        {
            if (!Directory.Exists(referencesDir))
            {
                DcLogger.Info("ReferencesLoader",
                    $"[ReferencesLoader] No References folder at {referencesDir} — skipping");
                return;
            }

            var dllFiles = Directory.GetFiles(referencesDir, "*.dll");
            DcLogger.Info("ReferencesLoader",
                $"[ReferencesLoader] Loading {dllFiles.Length} reference DLL(s) from {referencesDir}");

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    Assembly.LoadFrom(dllPath);
                    DcLogger.Info("ReferencesLoader",
                        $"[ReferencesLoader] Loaded reference: {Path.GetFileName(dllPath)}");
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("ReferencesLoader",
                        $"[ReferencesLoader] Failed to load reference '{Path.GetFileName(dllPath)}': {ex.Message}");
                }
            }
        }
    }
}
