using System;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Underdog
{
    internal static class UnderdogSystem
    {
        public static bool IsUnderdog(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, UnderdogRole.Id);

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            if (PlayerControl.GameOptions == null) return;
            if (RoleConfig.Underdog?.Value != true) return;

            int aliveImpostors = 0;
            int aliveOthers = 0;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                if (player.Data.myRole != null && player.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor)
                    aliveImpostors++;
                else
                    aliveOthers++;
            }
            if (aliveImpostors >= aliveOthers) return;

            var multiplier = RoleConfig.UnderdogCooldownMultiplier?.Value ?? 0.5f;
            if (multiplier <= 0f || multiplier >= 1f) return;
            var reduced = PlayerControl.GameOptions.KillCooldown * multiplier;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead) continue;
                if (!IsUnderdog(player)) continue;

                if (player.killTimer > reduced + 0.05f)
                {
                    try { player.RpcSetKillTimer(reduced); } catch { }
                }
            }
        }

        public static void Reset() { }
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
    }
}
