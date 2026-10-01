using System;
using HarmonyLib;
using MarshAPI;

namespace TownOfRoles.Core
{
    // Keeps the exiled player's role reveal line (\"X was the Jester.\") applied for the whole
    // exile animation. The line comes from MarshAPI's RoleRegistry, so every registered role
    // gets its own text without a per-role entry here.
    internal static class ExileTextFix
    {
        // Cached per-exile resolution so the per-frame tick only re-assigns a string while a
        // single exile animation is running (the role is resolved once when the exiled player
        // changes, not every frame).
        private static byte _exiledPlayerId = byte.MaxValue;
        private static string _cachedText;

        // Per-frame upkeep, called from a HudManager.Update postfix. Cheap: one null check when
        // no exile is running.
        public static void Tick()
        {
            var controller = ExileController.Instance;
            if (controller == null)
            {
                _exiledPlayerId = byte.MaxValue;
                _cachedText = null;
                return;
            }
            // completeString / exiled are protected in the 2026.8.9 interop,
            // access them through the reflection adapter (see GameReflection).
            var exiled = GameReflection.GetExileExiled(controller);
            if (exiled == null)
            {
                _cachedText = null;
                return;
            }
            // Resolve the reveal text once per exiled player; re-apply it for
            // the rest of the animation (the game's Begin body overwrites
            // completeString after our Begin prefix runs, so the re-apply must
            // keep running through the animation).
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

    // Drives ExileTextFix.Tick every frame. Installed unconditionally, inert (single null
    // check) unless an exile animation is actually running.
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
