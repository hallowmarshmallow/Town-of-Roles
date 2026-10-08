using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Mayor
{
    internal sealed class MayorRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Mayor";

        public override string DisplayName => "Mayor";
        public override string RoleTypeName => Id;
        public override string Description => "Your vote counts double in meetings.";
        public override int Count => RoleConfig.Count(RoleConfig.MayorCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.MayorChance);
        public override string DescriptionShort => "Your votes count double.";

        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.35f, 0.6f, 1f, 1f);
    }
}
