using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Phantom
{
    internal sealed class PhantomRole : CustomRole
    {
        public const string Id = "townofroles.Phantom";

        public override string DisplayName => "Phantom";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Complete all your tasks after death to win.";
        public override int Count => RoleConfig.Count(RoleConfig.PhantomCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.PhantomChance);
        public override string DescriptionShort => "When you die, keep doing tasks as a phantom. Finish them all to win.";
        public override Color TeamColor => new(0.75f, 0.75f, 0.85f, 1f);
    }
}
