using System;
using DCLoader.Config;
using HarmonyLib;

namespace DCLoader.Core
{
    internal class ModEntry
    {
        public ModInfoAttribute Info { get; }
        public ModDependencyAttribute[] Dependencies { get; }
        public Type ModType { get; }
        public Mod Instance { get; set; }
        public Harmony Harmony { get; set; }
        public Action<string>? LogSource { get; set; }
        public ConfigFile ConfigFile { get; set; }
        public bool Enabled { get; set; }
        public int ConsecutiveErrorCount { get; set; }

        public ModEntry(Type modType, ModInfoAttribute info, ModDependencyAttribute[] dependencies)
        {
            ModType = modType;
            Info = info;
            Dependencies = dependencies;
            Enabled = true;
        }
    }
}
