using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Swooper
{
    // Swooper, an Impostor who can become temporarily invisible. Ported from the original
    // Town-Of-Us Swooper role.
    internal sealed class SwooperRole : CustomImpostorRole
    {
        public const string Id = "townofroles.Swooper";

        public override string DisplayName => "Swooper";
        public override string RoleTypeName => Id;
        public override string Description => "Become invisible for a short time.";
        public override int Count => RoleConfig.Count(RoleConfig.SwooperCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.SwooperChance);
        public override string DescriptionShort => "Use Swoop to vanish for a few seconds.";
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.4f, 0.45f, 0.6f, 1f);
    }
}
