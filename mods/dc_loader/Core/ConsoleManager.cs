using System;
using System.Collections.Generic;
using System.Linq;
using DCLoader.Core.UI;

namespace DCLoader.Core
{
    /// <summary>
    /// Developer console command registry. Mods call RegisterCommand() to add commands,
    /// Print()/PrintError() for output.
    /// </summary>
    public static class ConsoleManager
    {
        private static readonly Dictionary<string, ConsoleCommand> _commands
            = new Dictionary<string, ConsoleCommand>(StringComparer.OrdinalIgnoreCase);

        internal static ConsoleBehaviour UI { get; set; }

        /// <summary>
        /// Register a console command (case-insensitive, no spaces in name).
        /// </summary>
        public static void RegisterCommand(string name, string description, Action<string[]> callback)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _commands[name.ToLowerInvariant()] = new ConsoleCommand(name, description, callback);
        }

        /// <summary>Print a message to the console.</summary>
        public static void Print(string message)
        {
            UI?.AppendLine(message, isError: false);
        }

        /// <summary>Print an error (shows red) to the console.</summary>
        public static void PrintError(string message)
        {
            UI?.AppendLine(message, isError: true);
        }

        internal static void Execute(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return;

            var parts = rawInput.Trim().Split(' ');
            var name = parts[0].ToLowerInvariant();

            if (_commands.TryGetValue(name, out var cmd))
            {
                var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
                try
                {
                    cmd.Callback(args);
                }
                catch (Exception ex)
                {
                    PrintError($"[{name}] error: {ex.Message}");
                }
            }
            else
            {
                PrintError($"Unknown command: '{name}'. Type 'help' for available commands.");
            }
        }

        internal static void RegisterBuiltins()
        {
            RegisterCommand("help", "List all registered commands", _ =>
            {
                foreach (var kvp in _commands.OrderBy(k => k.Key))
                    Print($"  {kvp.Value.Name,-20} {kvp.Value.Description}");
            });

            RegisterCommand("clear", "Clear the console output", _ =>
            {
                UI?.ClearOutput();
            });
        }
    }

    internal sealed class ConsoleCommand
    {
        public string Name { get; }
        public string Description { get; }
        public Action<string[]> Callback { get; }

        public ConsoleCommand(string name, string description, Action<string[]> callback)
        {
            Name = name;
            Description = description;
            Callback = callback;
        }
    }
}
