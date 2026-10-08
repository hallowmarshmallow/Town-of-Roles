using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Seer
{
    internal sealed class SeerRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Seer";

        public override string DisplayName => "Seer";
        public override string RoleTypeName => Id;
        public override string Description => "Investigate players to reveal their faction.";
        public override int Count => RoleConfig.Count(RoleConfig.SeerCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.SeerChance);
        public override string DescriptionShort => "Investigate players.";

        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.65f, 0.45f, 1f, 1f);
    }
}
