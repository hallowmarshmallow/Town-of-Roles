using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Arsonist
{
    // Arsonist, a Neutral role that douses players and ignites them to win. Ported from the
    // original Town-Of-Us Arsonist role.
    internal sealed class ArsonistRole : CustomRole
    {
        public const string Id = "townofroles.Arsonist";

        public override string DisplayName => "Arsonist";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Douse players, then ignite them all to win.";
        public override int Count => RoleConfig.Count(RoleConfig.ArsonistCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.ArsonistChance);
        public override string DescriptionShort => "FIRE IN THE HOLE";
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(1f, 0.45f, 0.15f, 1f);
    }
}
