using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Camouflager
{
    internal sealed class CamouflagerRole : CustomImpostorRole
    {
        public const string Id = "townofroles.Camouflager";

        public override string DisplayName => "Camouflager";
        public override string RoleTypeName => Id;
        public override string Description => "Turn everyone grey so identities are hidden.";
        public override int Count => RoleConfig.Count(RoleConfig.CamouflagerCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.CamouflagerChance);
        public override string DescriptionShort => "Use Camouflage to grey everyone out for a while.";
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.55f, 0.6f, 0.95f, 1f);
    }
}
