using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using DCLoader;
using DCLoader.Core;

namespace DCCheater
{
    [ModInfo("dc_cheater", "DC Cheater", "1.0.0", "dc-modding",
             "Cheat mod: modify money, reputation, and XP via GUI, console commands, and hotkey.")]
    public class CheaterMod : Mod
    {
        private DCLoader.Config.ConfigEntry<string> _toggleKey;
        private GameObject _cheaterGo;

        public override void OnLoad()
        {
            Logger("DC Cheater loading...");

            _toggleKey = Config.Bind<string>("toggle_key", "F9",
                "Hotkey to open/close the cheat panel (e.g. F9, F8)");

            ClassInjector.RegisterTypeInIl2Cpp<CheaterBehaviour>();

            _cheaterGo = new GameObject("DCCheater");
            UnityEngine.Object.DontDestroyOnLoad(_cheaterGo);
            var behaviour = _cheaterGo.AddComponent<CheaterBehaviour>();
            behaviour.Initialize(_toggleKey);

            ConsoleManager.RegisterCommand("money", "Set player money: money <amount>", args =>
            {
                if (args.Length < 1 || !float.TryParse(args[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float amount))
                {
                    ConsoleManager.PrintError("Usage: money <amount>");
                    return;
                }
                var player = GameAPI.Player;
                if (player == null) { ConsoleManager.PrintError("Player not ready — load a save first."); return; }
                player.money = amount;
                ConsoleManager.Print($"Money set to {amount}");
            });

            ConsoleManager.RegisterCommand("rep", "Set player reputation: rep <amount>", args =>
            {
                if (args.Length < 1 || !float.TryParse(args[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float amount))
                {
                    ConsoleManager.PrintError("Usage: rep <amount>");
                    return;
                }
                var player = GameAPI.Player;
                if (player == null) { ConsoleManager.PrintError("Player not ready — load a save first."); return; }
                player.reputation = amount;
                ConsoleManager.Print($"Reputation set to {amount}");
            });

            ConsoleManager.RegisterCommand("xp", "Set player XP: xp <amount>", args =>
            {
                if (args.Length < 1 || !float.TryParse(args[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float amount))
                {
                    ConsoleManager.PrintError("Usage: xp <amount>");
                    return;
                }
                var player = GameAPI.Player;
                if (player == null) { ConsoleManager.PrintError("Player not ready — load a save first."); return; }
                player.xp = amount;
                ConsoleManager.Print($"XP set to {amount}");
            });

            GameAPI.OnGameLoaded += () => Logger("Game loaded — DC Cheater ready.");
            GameAPI.OnGameSaved  += () => Logger("Game saved.");

            Logger("DC Cheater loaded. Press " + _toggleKey.Value + " to open cheat panel.");
        }

        public override void OnUnload()
        {
            if (_cheaterGo != null)
                UnityEngine.Object.Destroy(_cheaterGo);
            Logger("DC Cheater unloaded.");
        }
    }
}
