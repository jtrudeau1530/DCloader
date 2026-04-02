using System;
using System.Diagnostics;
using System.IO;

namespace DCLoader
{
    // All the paths dc_loader cares about. Call Initialize() once at startup.
    public static class DcPaths
    {
        public static string GameRoot { get; private set; } = null!;
        public static string LoaderDir { get; private set; } = null!;
        public static string InteropDir { get; private set; } = null!;
        public static string LogsDir { get; private set; } = null!;
        public static string ModsDir { get; private set; } = null!;
        public static string ReferencesDir { get; private set; } = null!;
        public static string ConfigDir { get; private set; } = null!;
        public static string GameAssemblyPath { get; private set; } = null!;

        public static void Initialize()
        {
            // Doorstop sets this env var; fall back to MainModule for standalone debugging
            string exePath = Environment.GetEnvironmentVariable("DOORSTOP_PROCESS_PATH")
                          ?? Process.GetCurrentProcess().MainModule!.FileName;
            GameRoot = Path.GetDirectoryName(exePath)!;
            LoaderDir = Path.Combine(GameRoot, "loader");
            InteropDir = Path.Combine(LoaderDir, "interop");
            LogsDir = Path.Combine(LoaderDir, "logs");
            ModsDir = Path.Combine(GameRoot, "Mods");
            ReferencesDir = Path.Combine(ModsDir, "References");
            ConfigDir = Path.Combine(LoaderDir, "config");
            GameAssemblyPath = Path.Combine(GameRoot, "GameAssembly.dll");
        }
    }
}
