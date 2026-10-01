using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Glitch
{
    // The Glitch, a Neutral Killing role with Mimic, Hack, and Kill abilities. Wins when
    // everyone else is dead.
    internal sealed class GlitchRole : CustomRole
    {
        public const string Id = "townofroles.Glitch";

        public override string DisplayName => "The Glitch";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override string Description => "Mimic, hack, and kill everyone to be the last one standing.";
        public override int Count => RoleConfig.Count(RoleConfig.GlitchCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.GlitchChance);
        public override string DescriptionShort => "Mimic, hack and kill everyone";
        public override Color TeamColor => new(0.45f, 0.95f, 0.35f, 1f);
        public override string EjectionText(string playerName) => $"{playerName} was The Glitch.";
    }
}
