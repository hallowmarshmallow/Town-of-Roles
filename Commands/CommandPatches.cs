using HarmonyLib;
using TownOfRoles.Core;

namespace TownOfRoles.Commands
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.CoClose))]
    internal static class ChatController_Close_KeepChatOpenPatch
    {
        [HarmonyPriority(Priority.High)]
        private static bool Prefix()
        {
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
