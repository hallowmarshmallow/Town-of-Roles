using System;
using MarshAPI;
using TownOfRoles.Commands;

namespace TownOfRoles.Core
{
    // The mod's own tab on the game's options menu (the cogwheel).
    internal static class TorPage
    {
        // Registration key for the tab, and the namespace its click ids come from.
        internal const string Owner = "TownOfRoles.Settings";

        // Text on the tab.
        internal const string TabLabel = "TOR";

        // True while the tab is on a live menu.
        internal static bool Attached => OptionsTabs.IsAttached(Owner);

        // Registers the tab. Called once from Load, before the first menu is built.
        public static void Register()
        {
            try
            {
                // Stays attached while the mod is switched off: it is the surface the switch
                // lives on, and a tab that removed itself when it was used would leave no way
                // back except a restart.
                OptionsTabs.Register(new OptionsTab(Owner, TabLabel, AddRows));
            }
            catch (Exception e)
            {
                Log("register failed: " + e.Message);
            }
        }

        // Takes the tab off the menu and forgets it.
        internal static void Reset()
        {
            try { OptionsTabs.Unregister(Owner); }
            catch (Exception e) { Log("reset: " + e.Message); }
        }

        private static void AddRows(UiTabPage page)
        {
            if (page == null) return;

            // Session state, not config: it clears on the next launch (see SessionDisable).
            // What it does *not* do is un-deal a role that is already assigned, clearing a
            // player's role mid-round is a gameplay event, not a settings toggle, so a round
            // in progress keeps its roles until it ends. It stops the next one.
            page.AddToggle("Disable Mod (until restart)",
                () => SessionDisable.IsOff, value => SessionDisable.Set(value));

            // Host-side: stops the round from ending by its own win conditions.
            page.AddToggle("No Game End", () => CommandState.NoGameEnd, CommandState.SetNoGameEnd);

            // Starts the round now, instead of waiting for the lobby to fill. Host only in
            // practice, the check is the game's, not ours, and it reports the refusal.
            page.AddButton("Force Start", () => Say(GameActions.TryForceStart(), "Force-start requested."));
        }

        // Reports an action's outcome on the surface a button has, which is chat.
        private static void Say(string error, string success)
        {
            try { ChatCommands.Write(error ?? success ?? "Done."); }
            catch (Exception e) { Log("report: " + e.Message); }
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("TOR tab: " + message);
    }
}
