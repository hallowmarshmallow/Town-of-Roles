using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Engineer
{
    // Engineer, a Crewmate who can use the game's native vent system. No custom button or asset
    // is required; CanVent makes Classic Us handle the existing vent UI and networked vent
    // RPCs.
    internal sealed class EngineerRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Engineer";

        public override string DisplayName => "Engineer";
        public override string RoleTypeName => Id;
        public override string Description => "Use vents to move around the map.";
        public override int Count => RoleConfig.Count(RoleConfig.EngineerCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.EngineerChance);
        public override string DescriptionShort => "Fix sabotages and vent";
        public override bool CanVent => true;
        // Engineer uses a dedicated MarshAPI ability button, not the native Kill button.
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.2f, 0.85f, 0.95f, 1f);
    }
}
