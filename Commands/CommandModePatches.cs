using HarmonyLib;
using TownOfRoles.Core;

namespace TownOfRoles.Commands
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    internal static class ShipStatus_CheckEndCriteria_CommandPatch
    {
        private static bool Prefix()
        {
            return !CommandState.NoGameEnd;
        }
    }
}
