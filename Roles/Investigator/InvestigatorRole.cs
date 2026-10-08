using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Investigator
{
    internal sealed class InvestigatorRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Investigator";

        public override string DisplayName => "Investigator";
        public override string RoleTypeName => Id;
        public override string Description => "See the footprints of other players.";
        public override int Count => RoleConfig.Count(RoleConfig.InvestigatorCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.InvestigatorChance);
        public override string DescriptionShort => "Investigate feet.";
        public override Color TeamColor => new(0.35f, 0.9f, 0.75f, 1f);
    }
}
