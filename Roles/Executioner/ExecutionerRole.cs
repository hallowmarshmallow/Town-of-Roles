using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Executioner
{
    internal sealed class ExecutionerRole : CustomRole
    {
        public const string Id = "townofroles.Executioner";

        public override string DisplayName => "Executioner";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Get your target voted out to win.";
        public override int Count => RoleConfig.Count(RoleConfig.ExecutionerCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.ExecutionerChance);
        public override string DescriptionShort => "Get your target voted out";
        public override Color TeamColor => new(0.45f, 0.9f, 0.85f, 1f);
    }
}
