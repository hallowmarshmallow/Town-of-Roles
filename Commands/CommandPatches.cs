using HarmonyLib;
using TownOfRoles.Core;

namespace TownOfRoles.Commands
{
    // The dispatch patch that used to live here moved to MarshAPI: intercepting
    // PlayerControl.RpcSendChat is what makes "a command never reaches the lobby" true,
    // so it cannot belong to whichever mod happens to register one, two of them would
    // be two owners of the same swallow. See MarshAPI's ChatCommands.

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.CoClose))]
    internal static class ChatController_Close_KeepChatOpenPatch
    {
        [HarmonyPriority(Priority.High)]
        private static bool Prefix()
        {
            // Keeps the native chat panel open when explicitly requested; the command backend
            // works either way. Deliberately not ANDed with CommandConfig.Enabled: this switch
            // has a row on the Debug tab and must not be silently conjoined with another.
            return CommandConfig.AlwaysCommandChat?.Value != true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    internal static class PlayerControl_FixedUpdate_VisualEffectsPatch
    {
        private static void Postfix(PlayerControl __instance)
        {
            if (__instance == null || __instance != PlayerControl.LocalPlayer) return;
            if (CommandConfig.Enabled?.Value != true) return;
            CommandSystem.TickLocalEffects();
        }
    }
}
