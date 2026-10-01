using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Altruist
{
    // Altruist, a Crewmate who can revive a dead body, dying in the process. Ported from the
    // original Town-Of-Us Altruist role.
    internal sealed class AltruistRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Altruist";

        public override string DisplayName => "Altruist";
        public override string RoleTypeName => Id;
        public override string Description => "Revive a dead body, at the cost of your own life.";
        public override int Count => RoleConfig.Count(RoleConfig.AltruistCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.AltruistChance);
        public override string DescriptionShort => "Revive dead people at the cost of your life";
        // Altruist uses a dedicated MarshAPI ability button, not the native Kill button.
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.95f, 0.5f, 0.72f, 1f);
    }
}
