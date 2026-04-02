using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Startup;

namespace DCLoader.Core
{
    // Handles interop assembly validation and loading. Safe to call before Il2CppInterop starts.
    internal static class InteropManager
    {
        private const string HashFileName = "assembly-hash.txt";

        // Checks GameAssembly.dll hash against assembly-hash.txt. False = game was updated.
        public static bool VerifyHash(string interopDir, string gameAssemblyPath)
        {
            string hashFile = Path.Combine(interopDir, HashFileName);
            if (!File.Exists(hashFile))
            {
                DcLogger.Error("Interop", $"Missing {HashFileName} in {interopDir}. " +
                    "Interop assemblies were not installed correctly.");
                return false;
            }

            string storedHash = File.ReadAllText(hashFile).Trim();
            string currentHash = ComputeSHA256(gameAssemblyPath);

            if (!string.Equals(currentHash, storedHash, StringComparison.OrdinalIgnoreCase))
            {
                DcLogger.Error("Interop",
                    $"GameAssembly.dll hash mismatch.\n" +
                    $"  Expected: {storedHash}\n" +
                    $"  Current:  {currentHash}\n" +
                    "The game has been updated. Download the updated dc_loader.");
                return false;
            }

            DcLogger.Info("Interop", $"Hash verified: {currentHash[..12]}...");
            return true;
        }

        // Hook AssemblyResolve so the runtime can find our interop and loader DLLs.
        public static void RegisterAssemblyResolver(string interopDir, string loaderDir)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string name = new AssemblyName(args.Name).Name!;
                string path = Path.Combine(interopDir, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
                path = Path.Combine(loaderDir, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
                return null;
            };
            DcLogger.Info("Interop", $"AssemblyResolve hook registered. interop={interopDir}");
        }

        public static void PreloadInteropAssemblies(string interopDir)
        {
            int count = 0;
            foreach (var dll in Directory.GetFiles(interopDir, "*.dll"))
            {
                try
                {
                    Assembly.LoadFrom(dll);
                    count++;
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("Interop", $"Could not preload {Path.GetFileName(dll)}: {ex.Message}");
                }
            }
            DcLogger.Info("Interop", $"Preloaded {count} interop assemblies from {interopDir}");
        }

        // Points Il2CppInterop's P/Invokes at the right GameAssembly.dll.
        public static void RegisterNativeLibraryResolver(string gameAssemblyPath)
        {
            NativeLibrary.SetDllImportResolver(
                typeof(Il2CppInteropRuntime).Assembly,
                (libName, assembly, searchPath) =>
                    string.Equals(libName, "GameAssembly", StringComparison.OrdinalIgnoreCase)
                        ? NativeLibrary.Load(gameAssemblyPath)
                        : IntPtr.Zero);
            DcLogger.Info("Interop", "NativeLibrary resolver registered for GameAssembly.dll");
        }

        // Only hashes size + first 64KB. Full SHA-256 of a ~60MB binary is too slow for startup.
        private static string ComputeSHA256(string filePath)
        {
            using var sha = SHA256.Create();
            var info = new FileInfo(filePath);
            sha.TransformBlock(BitConverter.GetBytes(info.Length), 0, 8, null, 0);
            using var fs = File.OpenRead(filePath);
            var buf = new byte[65536];
            int read = fs.Read(buf, 0, buf.Length);
            sha.TransformFinalBlock(buf, 0, read);
            return Convert.ToHexString(sha.Hash!);
        }
    }
}
