using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Miner
{
    // Miner, an Impostor who can place new vents that connect only to each other, forming their
    // own private passageway. Ported from the original Town-Of-Us Miner role.
    internal sealed class MinerRole : CustomImpostorRole
    {
        public const string Id = "townofroles.Miner";

        public override string DisplayName => "Miner";
        public override string RoleTypeName => Id;
        public override string Description => "Mine vents that connect only to each other.";
        public override int Count => RoleConfig.Count(RoleConfig.MinerCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.MinerChance);
        public override string DescriptionShort => "Use Mine to place a vent at your position. Your vents form a private network.";
        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.85f, 0.5f, 0.2f, 1f);
    }
}
