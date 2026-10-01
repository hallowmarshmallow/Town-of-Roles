using System;
using UnityEngine;

namespace TownOfRoles.Roles.Sheriff
{
    internal static class SheriffAbilityHolder
    {
        private static DateTime _cooldownUntil;

        public static bool IsCoolingDown => DateTime.UtcNow < _cooldownUntil;

        // Seconds until the shot is ready, for the HUD's cooldown digits, the same clock
        // IsCoolingDown gates on, so the digits cannot say ready while the gate still says no.
        public static float SecondsRemaining =>
            Mathf.Max(0f, (float)(_cooldownUntil - DateTime.UtcNow).TotalSeconds);

        public static bool TryStartCooldown()
        {
            if (IsCoolingDown) return false;
            _cooldownUntil = DateTime.UtcNow.AddSeconds(Options.KillCooldown);
            return true;
        }

        public static void Reset() => _cooldownUntil = DateTime.MinValue;
    }
}
