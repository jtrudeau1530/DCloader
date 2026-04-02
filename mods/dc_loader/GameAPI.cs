using System;
using DCLoader.Core;

namespace DCLoader
{
    /// <summary>
    /// Null-safe access to game singletons and lifecycle events.
    /// Properties return null before the game is ready -- they won't throw.
    /// Subscribe to events in OnLoad().
    /// </summary>
    public static class GameAPI
    {
        /// <summary>Fires once when the game is fully loaded (MainGameManager.instance != null).</summary>
        public static event Action OnGameLoaded;

        /// <summary>Fires each time the game saves.</summary>
        public static event Action OnGameSaved;

        /// <summary>
        /// Player data (money, reputation, xp). Null before a save is loaded.
        /// Usage: GameAPI.Player?.money
        /// </summary>
        public static Player Player
        {
            get
            {
                try { return PlayerManager.instance?.playerClass; }
                catch { return null; }
            }
        }

        /// <summary>
        /// In-game time (currentTimeOfDay 0-1, day, secondsInFullDay). Null before scene load.
        /// </summary>
        public static TimeController Time
        {
            get
            {
                try { return TimeController.instance; }
                catch { return null; }
            }
        }

        internal static void RaiseOnGameLoaded()
        {
            try { OnGameLoaded?.Invoke(); }
            catch (Exception ex)
            {
                DcLogger.Error("GameAPI", $"[GameAPI] OnGameLoaded handler threw: {ex.Message}");
            }
        }

        internal static void RaiseOnGameSaved()
        {
            try { OnGameSaved?.Invoke(); }
            catch (Exception ex)
            {
                DcLogger.Error("GameAPI", $"[GameAPI] OnGameSaved handler threw: {ex.Message}");
            }
        }
    }
}
