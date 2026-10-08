using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Swapper
{
    internal sealed class SwapperRole : CustomCrewmateRole
    {
        public const string Id = "townofroles.Swapper";

        public override string DisplayName => "Swapper";
        public override string RoleTypeName => Id;
        public override string Description => "During meetings, swap the votes of two players.";
        public override int Count => RoleConfig.Count(RoleConfig.SwapperCount);
        public override float RoleChancePercent => RoleConfig.Chance(RoleConfig.SwapperChance);
        public override string DescriptionShort => "Use the Swap buttons in a meeting to pick two players; their votes are swapped.";

        public override string KillAbilityName => string.Empty;
        public override Color TeamColor => new(0.45f, 0.8f, 0.3f, 1f);
    }
}
