using System;
using UnityEngine;

namespace TownOfRoles.Roles.Sheriff
{
    internal static class SheriffAbilityHolder
    {
        private static DateTime _cooldownUntil;

        public static bool IsCoolingDown => DateTime.UtcNow < _cooldownUntil;

        public static float SecondsRemaining =>
            Mathf.Max(0f, (float)(_cooldownUntil - DateTime.UtcNow).TotalSeconds);

        public static bool TryStartCooldown()
        {
            if (IsCoolingDown) return false;
            _cooldownUntil = DateTime.UtcNow.AddSeconds(SheriffOptions.KillCooldown);
            return true;
        }

        public static void Reset() => _cooldownUntil = DateTime.MinValue;
    }
}
