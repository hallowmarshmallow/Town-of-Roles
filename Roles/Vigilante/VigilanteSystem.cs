using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using TownOfRoles.Core;
using UnityEngine;

namespace TownOfRoles.Roles.Vigilante
{
    internal static class VigilanteSystem
    {
        private const string ShotRpc = "townofroles.VigilanteShot";
        private const string RequestShotRpc = "townofroles.VigilanteRequestShot";
        private static readonly Dictionary<byte, int> ShotsRemaining = new();
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();

        public static bool IsVigilante(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, VigilanteRole.Id);

        internal static bool CanShootNow(PlayerControl vigilante) =>
            CanShoot(vigilante) && ClosestPlayerFinder.GetClosestTarget(vigilante, out _);

        // Shots left for a player, for the HUD counter. The same value CanShoot gates on, so
        // the counter cannot disagree with whether the button fires.
        internal static int RemainingUses(PlayerControl vigilante) =>
            vigilante == null || vigilante.Data == null ? 0 : GetShots(vigilante.PlayerId);

        // Seconds until the next shot, for the HUD digits, the same clock CanShoot gates on.
        internal static float SecondsRemaining(PlayerControl vigilante)
        {
            if (vigilante == null || vigilante.Data == null) return 0f;
            var until = GetCooldown(vigilante.PlayerId);
            return Mathf.Max(0f, (float)(until - DateTime.UtcNow).TotalSeconds);
        }

        public static void TryShoot(PlayerControl vigilante)
        {
            var client = AmongUsClient.Instance;
            if (client == null || vigilante == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestShotRpc, vigilante.PlayerId);
                return;
            }
            if (!CanShoot(vigilante)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(vigilante, out var target)) return;

            var remaining = Math.Max(0, GetShots(vigilante.PlayerId) - 1);
            ShotsRemaining[vigilante.PlayerId] = remaining;
            Cooldowns[vigilante.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.VigilanteCooldown));
            var enemy = target.Data?.myRole?.RoleTeamType == RoleTeamTypes.Impostor;
            KillManager.Kill(vigilante, enemy ? target : vigilante);
            TownOfRolesRpcMux.Send(ShotRpc, vigilante.PlayerId, target.PlayerId, enemy, remaining);
        }

        private static bool CanShoot(PlayerControl vigilante) =>
            IsVigilante(vigilante) && !vigilante.Data.IsDead && GetShots(vigilante.PlayerId) > 0 &&
            DateTime.UtcNow >= GetCooldown(vigilante.PlayerId);

        private static int GetShots(byte id)
        {
            if (!ShotsRemaining.TryGetValue(id, out var value))
            {
                value = RoleConfig.Count(RoleConfig.VigilanteShots);
                ShotsRemaining[id] = value;
            }
            return value;
        }

        private static DateTime GetCooldown(byte id) =>
            Cooldowns.TryGetValue(id, out var value) ? value : DateTime.MinValue;

        public static void Reset()
        {
            ShotsRemaining.Clear();
            Cooldowns.Clear();
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();

        [ReactorRpc(RequestShotRpc)]
        private static void OnRequestShotRpc(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryShoot(player);
                    return;
                }
            }
        }

        [ReactorRpc(ShotRpc)]
        private static void OnShotRpc(byte senderId, byte vigilanteId, byte playerId, bool enemy, int shotsRemaining)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ShotsRemaining[vigilanteId] = Math.Max(0, shotsRemaining);
            Cooldowns[vigilanteId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.VigilanteCooldown));
        }
    }
}
