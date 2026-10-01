using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Janitor
{
    // Janitor, an Impostor who can clean dead bodies so they cannot be reported. Ported from
    // the original Town-Of-Us Janitor role.
    internal sealed class JanitorRole : CustomImpostorRole
    {
        public const string Id = "townofroles.Janitor";

        public override string DisplayName => "Janitor";
        public override string RoleTypeName => Id;
        public override string Description => "Clean dead bodies so they cannot be reported.";
        public override int Count => RoleConfig.Count(RoleConfig.JanitorCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.JanitorChance);
        public override string DescriptionShort => "Clean bodies";
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.55f, 0.72f, 0.95f, 1f);
    }
}
