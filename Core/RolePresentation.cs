using System;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Roles.Assassin;
using TownOfRoles.Roles.Executioner;
using TownOfRoles.Roles.Engineer;
using TownOfRoles.Roles.Jester;
using TownOfRoles.Roles.Lovers;
using TownOfRoles.Roles.Medic;
using TownOfRoles.Roles.Seer;
using TownOfRoles.Roles.Sheriff;
using TownOfRoles.Roles.Vigilante;

namespace TownOfRoles.Core
{
    internal static class RolePresentation
    {
        public static bool TryGet(PlayerControl player, out string name, out Color color)
        {
            name = null;
            color = Color.white;
            if (player == null || player.Data == null) return false;

            foreach (var def in RoleCatalog.All)
            {
                if (!RoleRegistry.IsAssigned(player, def.Id)) continue;

                if (def.Id == "townofroles.Executioner" && ExecutionerSystem.IsConverted(player))
                    return false;
                name = def.Name;
                color = def.Color;
                return true;
            }
            return false;
        }

        public static bool CanSee(PlayerControl viewer, PlayerControl target)
        {
            if (viewer == null || target == null || target.Data == null) return false;
            if (viewer == target) return true;

            if (LoverSystem.ArePartners(viewer, target)) return true;
            if (viewer.Data.IsDead) return RoleConfig.DeadSeeRoles?.Value == true;
            return RoleConfig.ImpostorSeeRoles?.Value == true && viewer.Data.myRole != null && target.Data.myRole != null &&
                   viewer.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor &&
                   target.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor;
        }

        public static string WithRole(string playerName, string roleName) =>
            string.IsNullOrEmpty(roleName)
                ? playerName
                : playerName + "\n" + roleName;
    }
}
