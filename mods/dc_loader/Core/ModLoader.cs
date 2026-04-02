using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DCLoader.Core
{
    // Scans Mods/ for DLLs, finds Mod subclasses, sorts by dependencies.
    internal static class ModLoader
    {
        public static List<ModEntry> DiscoverMods(string modsFolder)
        {
            var entries = new List<ModEntry>();

            if (!Directory.Exists(modsFolder))
            {
                DcLogger.Warn("ModLoader", $"[ModLoader] Mods folder not found: {modsFolder}");
                return entries;
            }

            var dllFiles = Directory.GetFiles(modsFolder, "*.dll");
            DcLogger.Info("ModLoader", $"[ModLoader] Scanning {dllFiles.Length} DLL(s) in {modsFolder}");

            foreach (var dllPath in dllFiles)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(dllPath);
                }
                catch (Exception ex)
                {
                    DcLogger.Error("ModLoader",
                        $"[ModLoader] Failed to load assembly '{Path.GetFileName(dllPath)}': {ex.Message}");
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    DcLogger.Warn("ModLoader",
                        $"[ModLoader] Partial type load for '{Path.GetFileName(dllPath)}' — some types could not be loaded");
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || !typeof(Mod).IsAssignableFrom(type))
                        continue;

                    var info = type.GetCustomAttribute<ModInfoAttribute>();
                    if (info == null)
                    {
                        DcLogger.Warn("ModLoader",
                            $"[ModLoader] Type {type.FullName} extends Mod but has no [ModInfo] — skipping");
                        continue;
                    }

                    var deps = type.GetCustomAttributes<ModDependencyAttribute>().ToArray();
                    entries.Add(new ModEntry(type, info, deps));

                    DcLogger.Info("ModLoader",
                        $"[ModLoader] Discovered: {info.Name} v{info.Version} ({info.ID}) from {Path.GetFileName(dllPath)}");
                }
            }

            return entries;
        }

        // Kahn's algorithm -- topo sort by dependencies. Skips mods with missing/circular deps.
        public static List<ModEntry> TopologicalSort(List<ModEntry> mods)
        {
            var byId = new Dictionary<string, ModEntry>();
            foreach (var mod in mods)
            {
                if (byId.ContainsKey(mod.Info.ID))
                {
                    DcLogger.Warn("ModLoader",
                        $"[ModLoader] Duplicate mod ID '{mod.Info.ID}' — keeping first, skipping duplicate");
                    continue;
                }
                byId[mod.Info.ID] = mod;
            }

            var inDegree = new Dictionary<string, int>();
            var graph = new Dictionary<string, List<string>>(); // dep -> dependents
            var skipped = new HashSet<string>();

            foreach (var id in byId.Keys)
            {
                inDegree[id] = 0;
                graph[id] = new List<string>();
            }

            foreach (var mod in byId.Values)
            {
                foreach (var dep in mod.Dependencies)
                {
                    if (!byId.ContainsKey(dep.ModID))
                    {
                        DcLogger.Error("ModLoader",
                            $"[ModLoader] Mod '{mod.Info.ID}' requires '{dep.ModID}' which is not installed — skipping");
                        skipped.Add(mod.Info.ID);
                        break;
                    }

                    graph[dep.ModID].Add(mod.Info.ID);
                    inDegree[mod.Info.ID]++;
                }
            }

            // cascade -- if your dependency got skipped, you're skipped too
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var mod in byId.Values)
                {
                    if (skipped.Contains(mod.Info.ID)) continue;

                    foreach (var dep in mod.Dependencies)
                    {
                        if (skipped.Contains(dep.ModID))
                        {
                            DcLogger.Error("ModLoader",
                                $"[ModLoader] Mod '{mod.Info.ID}' skipped — depends on skipped mod '{dep.ModID}'");
                            skipped.Add(mod.Info.ID);
                            changed = true;
                            break;
                        }
                    }
                }
            }

            var queue = new Queue<string>();
            foreach (var id in byId.Keys)
            {
                if (!skipped.Contains(id) && inDegree[id] == 0)
                    queue.Enqueue(id);
            }

            var sorted = new List<ModEntry>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                sorted.Add(byId[current]);

                foreach (var dependent in graph[current])
                {
                    if (skipped.Contains(dependent)) continue;

                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                        queue.Enqueue(dependent);
                }
            }

            int expectedCount = byId.Count - skipped.Count;
            if (sorted.Count < expectedCount)
            {
                DcLogger.Error("ModLoader",
                    $"[ModLoader] Circular dependency detected! Loaded {sorted.Count}/{expectedCount} non-skipped mods");
            }

            DcLogger.Info("ModLoader", $"[ModLoader] Load order resolved: {sorted.Count} mod(s)");
            return sorted;
        }
    }
}
