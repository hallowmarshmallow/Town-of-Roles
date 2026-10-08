using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Sheriff
{
    internal sealed class SheriffRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Sheriff";

        public override string DisplayName => "Sheriff";
        public override string RoleTypeName => Id;
        public override string Description => "Shoot the impostors";
        public override int Count => RoleConfig.Count(RoleConfig.SheriffCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.SheriffChance);
        public override string DescriptionShort => "Use the Kill button to shoot your target.";

        public override bool CanUseKillButton => false;
        public override string KillAbilityName => "Shoot";
        public override Color TeamColor => new(0.95f, 0.8f, 0.2f, 1f);
    }
}
