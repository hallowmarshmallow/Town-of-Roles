using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Morphling
{
    internal sealed class MorphlingRole : CustomImpostorRole
    {
        public const string Id = "townofroles.Morphling";

        public override string DisplayName => "Morphling";
        public override string RoleTypeName => Id;
        public override string Description => "Copy another player's appearance for a few seconds.";
        public override int Count => RoleConfig.Count(RoleConfig.MorphlingCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.MorphlingChance);
        public override string DescriptionShort => "Use Morph on a nearby player to copy their look; you revert when it wears off.";

        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.6f, 0.9f, 0.4f, 1f);
    }
}
