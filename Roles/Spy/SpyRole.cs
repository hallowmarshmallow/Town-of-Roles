using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Spy
{
    internal sealed class SpyRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Spy";

        public override string DisplayName => "Spy";
        public override string RoleTypeName => Id;
        public override string Description => "Get notified when someone vents or gets doused.";
        public override int Count => RoleConfig.Count(RoleConfig.SpyCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.SpyChance);
        public override string DescriptionShort => "You receive intel when a player vents or is doused.";

        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.85f, 0.7f, 0.35f, 1f);
    }
}
