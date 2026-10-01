using System;
using BepInEx.Configuration;
using UnityEngine;

namespace TownOfRoles.Core
{
    // The mod's own client tweaks that are neither a role setting nor a game rule.
    internal static class HorseModeConfig
    {
        // Leave the game's seasonal horse mode alone (default: on).
        public static ConfigEntry<bool> HorseMode { get; private set; }

        // True when the horse is left alone, which is the state the player sees.
        public static bool On => HorseMode?.Value != false;

        public static void Init(ConfigFile config)
        {
            HorseMode = config.Bind("Client", "HorseMode", true,
                "Leave the game's seasonal horse mode alone. Off forces it off by setting " +
                "SeasonalSettings.Override, which is the game's own switch and is a Nullable<bool> " +
                "so it can be cleared back to 'not overridden'.");
        }
    }

    // Forces seasonal horse mode off, or clears the override to let the game decide again.
    internal static class HorseMode
    {
        private static bool _applied;
        private static bool _loggedFailure;

        // Applies the current config value. Safe to call when nothing changed.
        public static void Apply() => Set(HorseModeConfig.On);

        // Forgets what was applied, so the next Apply re-asserts it. A round boundary calls
        // this: the season is re-evaluated when the scene is rebuilt, and a remembered "already
        // applied" would leave the override cleared.
        public static void Reset() => _applied = false;

        private static void Set(bool on)
        {
            try
            {
                if (on)
                {
                    if (!_applied) return;
                    SeasonalSettings.Override = default;
                    _applied = false;
                    return;
                }

                if (_applied) return;
                SeasonalSettings.Override = new Il2CppSystem.Nullable<bool>(false);
                _applied = true;
            }
            catch (Exception e)
            {
                if (_loggedFailure) return;
                _loggedFailure = true;
                Log("horse mode: " + e.Message +
                    " (SeasonalSettings.Override may not be settable in this interop; the toggle is inert)");
            }
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Client tweaks: " + message);
    }
}
