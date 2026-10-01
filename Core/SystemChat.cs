using System;

namespace TownOfRoles.Core
{
    // Shows the mod's system messages (role notifications, command feedback, and host
    // broadcasts) through the game's native "SYSTEM ALERT" popup
    // (HudManager.ChatPopup.ShowWarning). Every call site routes through Show.
    internal static class SystemChat
    {
        // Shows a system message through the game's native "SYSTEM ALERT" popup
        // (HudManager.ChatPopup.ShowWarning). Falls back to a log line when the HUD/popup is
        // not available (lobby, main menu).
        public static void Show(string message)
        {
            try
            {
                var popup = HudManager.Instance?.ChatPopup;
                if (popup == null)
                {
                    BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogInfo(message);
                    return;
                }
                popup.ShowWarning(message);
            }
            catch
            {
                // Non-fatal: the popup must never crash gameplay code.
            }
        }
    }
}
