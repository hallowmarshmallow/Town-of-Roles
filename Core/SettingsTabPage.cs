using System;
using MarshAPI;
using TownOfRoles.Commands;

namespace TownOfRoles.Core
{
    internal static class SettingsTabPage
    {
        internal const string Owner = "TownOfRoles.Settings";

        internal const string TabLabel = "TOR";

        internal static bool Attached => OptionsTabs.IsAttached(Owner);

        public static void Register()
        {
            try
            {
                OptionsTabs.Register(new OptionsTab(Owner, TabLabel, AddRows));
            }
            catch (Exception e)
            {
                Log("register failed: " + e.Message);
            }
        }

        internal static void Reset()
        {
            try { OptionsTabs.Unregister(Owner); }
            catch (Exception e) { Log("reset: " + e.Message); }
        }

        private static void AddRows(UiTabPage page)
        {
            if (page == null) return;

            page.AddToggle("Disable Mod (until restart)",
                () => SessionDisable.IsOff, value => SessionDisable.Set(value));

            page.AddToggle("No Game End", () => CommandState.NoGameEnd, CommandState.SetNoGameEnd);

            page.AddButton("Force Start", () => Say(GameActions.TryForceStart(), "Force-start requested."));
        }

        private static void Say(string error, string success)
        {
            try { ChatCommands.Write(error ?? success ?? "Done."); }
            catch (Exception e) { Log("report: " + e.Message); }
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("TOR tab: " + message);
    }
}
