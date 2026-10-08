using System;

namespace TownOfRoles.Core
{
    internal static class SystemChat
    {
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
            }
        }
    }
}
