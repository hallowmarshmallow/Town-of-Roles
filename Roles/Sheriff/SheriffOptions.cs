using TownOfRoles.Core;

namespace TownOfRoles.Roles.Sheriff
{
    internal static class SheriffOptions
    {
        public static float KillCooldown => RoleConfig.Seconds(RoleConfig.SheriffKillCooldown, 10f);
        public static bool KillOther => RoleConfig.SheriffKillOther?.Value ?? true;

        public static bool KillsNeutrals => RoleConfig.SheriffKillsNeutrals?.Value ?? true;
        public static bool BodyReport => RoleConfig.SheriffBodyReport?.Value ?? false;
    }
}
