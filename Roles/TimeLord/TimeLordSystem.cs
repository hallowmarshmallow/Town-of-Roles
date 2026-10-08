using System;
using System.Collections.Generic;
using Atomic;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.TimeLord
{
    internal static class TimeLordSystem
    {
        private const string RequestRewindRpc = "townofroles.TimeLordRequestRewind";
        private const string ReviveRpc = "townofroles.TimeLordRewindRevive";
        private const int SampleCount = 90;
        private const int SamplesPerTick = 5;

        private static readonly Dictionary<byte, Vector2[]> History = new();

        private static readonly Dictionary<byte, float[]> HistoryTimes = new();
        private static readonly Dictionary<byte, int> HistoryWrite = new();

        private static readonly Dictionary<byte, float> DeathTimes = new();
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();
        private static int _tickCount;

        public static bool IsTimeLord(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, TimeLordRole.Id);

        internal static bool CanRewindNow(PlayerControl timeLord)
        {
            if (!IsTimeLord(timeLord) || timeLord.Data == null || timeLord.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(timeLord.PlayerId);
        }

        public static void TryRewind(PlayerControl timeLord)
        {
            var client = AmongUsClient.Instance;
            if (client == null || timeLord == null || timeLord.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestRewindRpc, timeLord.PlayerId);
                return;
            }
            if (!CanRewindNow(timeLord)) return;

            var seconds = RoleConfig.Seconds(RoleConfig.RewindSeconds, 5f);
            Cooldowns[timeLord.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.RewindCooldown, 30f));
            ApplyRewind(seconds);
            if (RoleConfig.RewindRevive?.Value != false) ApplyRevive(seconds);
        }

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            if (PlayerControl.GameOptions == null) return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected) continue;
                var id = player.PlayerId;
                if (player.Data.IsDead)
                {
                    if (!DeathTimes.ContainsKey(id)) DeathTimes[id] = Time.unscaledTime;
                }
                else if (DeathTimes.ContainsKey(id))
                {
                    DeathTimes.Remove(id);
                }
            }

            _tickCount++;
            if (_tickCount % SamplesPerTick != 0) return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected) continue;
                var id = player.PlayerId;
                if (!History.TryGetValue(id, out var ring))
                {
                    ring = new Vector2[SampleCount];
                    History[id] = ring;
                    HistoryWrite[id] = 0;
                }
                var index = HistoryWrite[id];
                ring[index] = player.GetTruePosition();
                if (!HistoryTimes.TryGetValue(id, out var times))
                {
                    times = new float[SampleCount];
                    HistoryTimes[id] = times;
                }
                times[index] = Time.unscaledTime;
                HistoryWrite[id] = (index + 1) % SampleCount;
            }
        }

        private static void ApplyRewind(float seconds)
        {
            if (seconds <= 0f) return;
            var cutoff = Time.unscaledTime - seconds;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                if (player.NetTransform == null) continue;
                if (!History.TryGetValue(player.PlayerId, out var ring)) continue;
                if (!HistoryTimes.TryGetValue(player.PlayerId, out var times)) continue;

                var write = HistoryWrite[player.PlayerId];

                Vector2 target = ring[write];
                var found = false;
                for (int back = 1; back <= SampleCount; back++)
                {
                    int i = (write - back + SampleCount) % SampleCount;
                    if (times[i] <= 0f) break;
                    target = ring[i];
                    found = true;
                    if (times[i] <= cutoff) break;
                }
                if (!found || target == Vector2.zero) continue;
                try { player.NetTransform.RpcSnapTo(target); } catch { }
            }
        }

        private static void ApplyRevive(float seconds)
        {
            var cutoff = Time.unscaledTime - seconds;
            foreach (var pair in new Dictionary<byte, float>(DeathTimes))
            {
                if (pair.Value < cutoff) continue;
                DeathTimes.Remove(pair.Key);

                var victim = FindPlayer(pair.Key);
                if (victim == null || victim.Data == null || !victim.Data.IsDead || victim.Data.Disconnected) continue;

                try { victim.Revive(); } catch { continue; }
                RemoveBody(pair.Key);
                TownOfRolesRpcMux.Send(ReviveRpc, pair.Key);
                if (pair.Key == PlayerControl.LocalPlayer?.PlayerId)
                    Local("The timeline healed — you are alive again!");
            }
        }

        private static void RemoveBody(byte victimId)
        {
            foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                if (body != null && body.ParentId == victimId)
                    Janitor.JanitorSystem.RemoveBody(body);
            }
        }

        [AtomicRpc(ReviveRpc)]
        private static void OnReviveRpc(byte senderId, byte victimId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var victim = FindPlayer(victimId);
            if (victim == null || victim.Data == null || !victim.Data.IsDead) return;
            try { victim.Revive(); } catch { return; }
            RemoveBody(victimId);
        }

        [AtomicRpc(RequestRewindRpc)]
        private static void OnRequestRewind(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryRewind(player);
                    return;
                }
            }
        }

        private static DateTime GetCooldown(byte playerId) =>
            Cooldowns.TryGetValue(playerId, out var value) ? value : DateTime.MinValue;

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        public static void Reset()
        {
            History.Clear();
            HistoryTimes.Clear();
            DeathTimes.Clear();
            Cooldowns.Clear();
            _tickCount = 0;
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();

        private static void Local(string message)
        {
            try
            {
                SystemChat.Show(message);
            }
            catch { }
        }
    }
}
