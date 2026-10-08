using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class ClosestPlayerFinder
    {
        public static bool GetClosestTarget(PlayerControl player, out PlayerControl target)
        {
            target = null;
            if (player == null || player.Data == null) return false;

            var origin = player.GetTruePosition();
            if (PlayerControl.GameOptions == null) return false;
            var killDistance = GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance];
            var best = float.MaxValue;

            foreach (var other in PlayerControl.AllPlayerControls)
            {
                if (other == null || other == player || other.Data == null) continue;
                if (other.Data.IsDead || other.Data.Disconnected) continue;

                var distance = Vector2.Distance(origin, other.GetTruePosition());
                if (distance <= killDistance && distance < best)
                {
                    best = distance;
                    target = other;
                }
            }

            return target != null;
        }
    }
}
