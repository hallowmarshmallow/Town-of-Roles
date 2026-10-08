using System;
using HarmonyLib;
using MarshAPI;

namespace TownOfRoles.Core
{
    internal static class ExileTextFix
    {
        private static byte _exiledPlayerId = byte.MaxValue;
        private static string _cachedText;

        public static void Tick()
        {
            var controller = ExileController.Instance;
            if (controller == null)
            {
                _exiledPlayerId = byte.MaxValue;
                _cachedText = null;
                return;
            }

            var exiled = GameReflection.GetExileExiled(controller);
            if (exiled == null)
            {
                _cachedText = null;
                return;
            }

            if (_exiledPlayerId != exiled.PlayerId || _cachedText == null)
            {
                _cachedText = RoleRegistry.ResolveEjectionText(exiled);
                _exiledPlayerId = exiled.PlayerId;
            }
            var text = _cachedText;
            if (text == null) return;
            try
            {
                GameReflection.SetCompleteString(controller, text);
                if (controller.Text != null) controller.Text.Text = text;
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Exile text: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    internal static class HudManager_Update_ExileTextFixPatch
    {
        private static void Postfix()
        {
            try
            {
                ExileTextFix.Tick();
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("ExileTextFix: " + e.Message);
            }
        }
    }
}
