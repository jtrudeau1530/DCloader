using System;
using Il2CppInterop.Runtime.Startup;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.HarmonySupport;
using UnityEngine;
using DCLoader;
using DCLoader.Core;
using DCLoader.Core.UI;

// Doorstop 4.5 finds this by exact namespace + class + method name. Don't rename anything here.
namespace Doorstop
{
    public static class Entrypoint
    {
        public static void Start()
        {
            try
            {
                DcPaths.Initialize();
                DcLogger.Initialize(DcPaths.LogsDir);
                DcLogger.OpenConsole();

                DcLogger.Info("Bootstrap", $"dc_loader starting. Game root: {DcPaths.GameRoot}");
                DcLogger.Info("Bootstrap", $"DOORSTOP_INITIALIZED={Environment.GetEnvironmentVariable("DOORSTOP_INITIALIZED")}");

                // bail if interop assemblies don't match the installed game version
                if (!InteropManager.VerifyHash(DcPaths.InteropDir, DcPaths.GameAssemblyPath))
                {
                    DcLogger.Error("Bootstrap", "Aborting: interop hash check failed. Mod loading skipped.");
                    return;
                }

                // resolver must be in place before we load any interop DLLs
                InteropManager.RegisterAssemblyResolver(DcPaths.InteropDir, DcPaths.LoaderDir);
                InteropManager.PreloadInteropAssemblies(DcPaths.InteropDir);

                // native resolver has to go before Il2CppInteropRuntime.Start()
                InteropManager.RegisterNativeLibraryResolver(DcPaths.GameAssemblyPath);

                // AddHarmonySupport() handles the DetourProvider registration internally
                Il2CppInteropRuntime
                    .Create(new RuntimeConfiguration
                    {
                        UnityVersion = new Version(6000, 3, 12)
                    })
                    .AddHarmonySupport()
                    .Start();

                DcLogger.Info("Bootstrap", "Il2CppInterop runtime started. HarmonyX active.");

                // IL2CPP needs these registered before AddComponent<T>() -- no BepInEx to do it for us
                ClassInjector.RegisterTypeInIl2Cpp<ModLoaderBehaviour>();
                ClassInjector.RegisterTypeInIl2Cpp<ModMenuBehaviour>();
                ClassInjector.RegisterTypeInIl2Cpp<ConsoleBehaviour>();

                DcLogger.Info("Bootstrap", "IL2CPP types registered.");

                var go = new GameObject("DCLoader");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<ModLoaderBehaviour>();
                go.AddComponent<ModMenuBehaviour>();
                go.AddComponent<ConsoleBehaviour>();

                DcLogger.Info("Bootstrap", "DCLoader GameObject attached. Handing off to ModLoaderBehaviour.Awake().");
            }
            catch (Exception ex)
            {
                // DcLogger might not be alive yet if we crashed really early
                try { DcLogger.Error("Bootstrap", $"Fatal bootstrap error: {ex}"); }
                catch { /* nothing we can do */ }
            }
        }
    }
}
