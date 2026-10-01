using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.TimeLord
{
    // Time Lord, a Crewmate who can rewind everyone back to where they stood. Ported from the
    // original Town-Of-Us TimeLord role.
    internal sealed class TimeLordRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.TimeLord";

        public override string DisplayName => "Time Lord";
        public override string RoleTypeName => Id;
        public override string Description => "Rewind time to undo player movement.";
        public override int Count => RoleConfig.Count(RoleConfig.TimeLordCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.TimeLordChance);
        public override string DescriptionShort => "Use Rewind to snap everyone back a few seconds.";
        public override Color TeamColor => new(0.55f, 0.65f, 0.95f, 1f);
    }
}
