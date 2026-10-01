using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Shifter
{
    // Shifter, a Neutral with no win condition who swaps roles and tasks with other players.
    // Ported from the original Town-Of-Us Shifter role.
    internal sealed class ShifterRole : CustomRole
    {
        public const string Id = "townofroles.Shifter";

        public override string DisplayName => "Shifter";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Swap roles and tasks with other players.";
        public override int Count => RoleConfig.Count(RoleConfig.ShifterCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.ShifterChance);
        public override string DescriptionShort => "Shift with a nearby player to take their role and tasks. Shifting an Impostor kills you.";
        public override Color TeamColor => new(0.75f, 0.55f, 0.95f, 1f);
    }
}
