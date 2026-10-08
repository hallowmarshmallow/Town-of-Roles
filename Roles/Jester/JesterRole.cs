using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Jester
{
    internal sealed class JesterRole : CustomRole
    {
        public const string Id = "townofroles.Jester";

        public override string DisplayName => "Jester";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Get yourself voted out to win.";
        public override int Count => RoleConfig.Count(RoleConfig.JesterCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.JesterChance);
        public override string DescriptionShort => "Get yourself voted out";
        public override Color TeamColor => new(0.86f, 0.35f, 0.95f, 1f);
    }
}
