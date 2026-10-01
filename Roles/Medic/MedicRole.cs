using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Medic
{
    internal sealed class MedicRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Medic";

        public override string DisplayName => "Medic";
        public override string RoleTypeName => Id;
        public override string Description => "Protect players from kills.";
        public override int Count => RoleConfig.Count(RoleConfig.MedicCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.MedicChance);
        public override string DescriptionShort => "Protect a player once.";
        // Medic uses a dedicated MarshAPI ability button, not the native Kill button.
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.3f, 0.95f, 0.55f, 1f);
    }
}
