using System;
using DCLoader.Config;
using HarmonyLib;

namespace DCLoader.Core
{
    /// <summary>
    /// Base class for dc_loader mods. Extend this and slap [ModInfo] on it.
    /// Don't extend MonoBehaviour -- dc_loader handles the lifecycle.
    /// </summary>
    public abstract class Mod
    {
        /// <summary>Per-mod logger. Ready before OnLoad().</summary>
        public Action<string> Logger { get; internal set; }

        /// <summary>Per-mod Harmony instance for patching. Unpatched automatically on disable.</summary>
        public Harmony Harmony { get; internal set; }

        /// <summary>
        /// Per-mod config. Use Config.Bind&lt;T&gt;() in OnLoad() to declare entries.
        /// Already wired up before OnLoad() fires.
        /// </summary>
        public ConfigFile Config { get; internal set; }

        /// <summary>Called once when the game is ready (singletons initialized).</summary>
        public virtual void OnLoad() { }

        /// <summary>Called every frame while enabled.</summary>
        public virtual void OnUpdate() { }

        /// <summary>Called every frame for UI stuff.</summary>
        public virtual void OnGUI() { }

        /// <summary>Called on game save.</summary>
        public virtual void OnSave() { }

        /// <summary>Called on disable or game exit. Clean up your stuff.</summary>
        public virtual void OnUnload() { }
    }
}
