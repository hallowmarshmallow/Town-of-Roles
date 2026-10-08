using MarshAPI;
using UnityEngine;

namespace TownOfRoles.Roles.Lovers
{
    internal sealed class LoverRole : CustomRole
    {
        public const string Id = "townofroles.Lovers";

        internal static readonly Color LoverColor = new(1f, 0.4f, 0.8f, 1f);

        public override string DisplayName => "Lover";
        public override string RoleTypeName => Id;
        public override RoleTeamTypes TeamType => RoleTeamTypes.Neutral;
        public override bool IsOverlay => true;

        public override string Description => "You are in love. Keep each other alive and win together.";
        public override string DescriptionShort => "You have en e-girlfriend";
        public override Color TeamColor => LoverColor;
        public override string EjectionText(string playerName) => $"{playerName} was a Lover.";
    }
}
