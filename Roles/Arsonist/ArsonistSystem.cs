using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;
using TownOfRoles.Roles.Spy;

namespace TownOfRoles.Roles.Arsonist
{
    // Arsonist gameplay logic (ported from Town-Of-Us' Arsonist.cs).
    internal static class ArsonistSystem
    {
        private const string DouseRpc = "townofroles.ArsonistDouse";
        private const string RequestDouseRpc = "townofroles.ArsonistRequestDouse";
        private const string RequestIgniteRpc = "townofroles.ArsonistRequestIgnite";
        private const string IgniteRpc = "townofroles.ArsonistIgnite";
        private const string WinRpc = "townofroles.ArsonistWin";

        private static readonly Color WinColor = new(1f, 0.45f, 0.15f, 1f);

        private static readonly HashSet<byte> Doused = new();
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();

        public static bool IsArsonist(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, ArsonistRole.Id);

        public static int DousedCount => Doused.Count;

        internal static bool CanDouseNow(PlayerControl arsonist)
        {
            if (!IsArsonist(arsonist) || arsonist.Data == null || arsonist.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(arsonist.PlayerId) &&
                   ClosestPlayerFinder.GetClosestTarget(arsonist, out _);
        }

        internal static bool CanIgniteNow(PlayerControl arsonist) =>
            IsArsonist(arsonist) && arsonist.Data != null && !arsonist.Data.IsDead && Doused.Count > 0;

        public static void TryDouse(PlayerControl arsonist)
        {
            var client = AmongUsClient.Instance;
            if (client == null || arsonist == null || arsonist.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestDouseRpc, arsonist.PlayerId);
                return;
            }
            if (!CanDouseNow(arsonist)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(arsonist, out var target)) return;
            if (!Doused.Add(target.PlayerId)) return; // already doused

            Cooldowns[arsonist.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.ArsonistDouseCooldown, 10f));
            TownOfRolesRpcMux.Send(DouseRpc, target.PlayerId);
            SpySystem.OnPlayerDoused(target.PlayerId);
        }

        public static void TryIgnite(PlayerControl arsonist)
        {
            var client = AmongUsClient.Instance;
            if (client == null || arsonist == null || arsonist.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestIgniteRpc, arsonist.PlayerId);
                return;
            }
            if (!CanIgniteNow(arsonist)) return;

            var victims = new List<PlayerControl>();
            foreach (var id in Doused)
            {
                var player = FindPlayer(id);
                if (player != null && player.Data != null && !player.Data.IsDead) victims.Add(player);
            }
            if (victims.Count == 0) return;

            Doused.Clear();
            foreach (var victim in victims) KillManager.Kill(arsonist, victim);
            TownOfRolesRpcMux.Send(IgniteRpc);
        }

        // Round lifecycle / pool
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            Doused.Clear();
            Cooldowns.Clear();
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            CheckEliminationWin();
        }

        // The Arsonist wins when every other living player is dead.
        private static void CheckEliminationWin()
        {
            if (ModdedGameOver.HasClaim || ShipStatus.Instance == null) return;
            var aliveArsonists = 0;
            var aliveOthers = 0;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;
                if (RoleRegistry.IsAssigned(player, ArsonistRole.Id)) aliveArsonists++;
                else aliveOthers++;
            }
            if (aliveArsonists == 0 || aliveOthers > 0) return;

            ModdedGameOver.Claim("Arsonist Wins", WinColor);
            TownOfRolesRpcMux.Send(WinRpc);
            ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
        }

        // RPCs
        [ReactorRpc(RequestDouseRpc)]
        private static void OnRequestDouse(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryDouse(player);
                    return;
                }
            }
        }

        [ReactorRpc(RequestIgniteRpc)]
        private static void OnRequestIgnite(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryIgnite(player);
                    return;
                }
            }
        }

        [ReactorRpc(DouseRpc)]
        private static void OnDouse(byte senderId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            Doused.Add(targetId);
        }

        [ReactorRpc(IgniteRpc)]
        private static void OnIgnite(byte senderId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            Doused.Clear();
        }

        [ReactorRpc(WinRpc)]
        private static void OnWin(byte senderId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ModdedGameOver.Claim("Arsonist Wins", WinColor);
        }

        // End screen
        // Drawn by MarshAPI's central ModdedGameOver patches; the Arsonist claims its
        // title at its win sites. The per-role EndGameManager patch pair that used to
        // live here is gone, see MarshAPI/Endgame/ModdedGameOver.cs.

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        private static DateTime GetCooldown(byte arsonistId) =>
            Cooldowns.TryGetValue(arsonistId, out var value) ? value : DateTime.MinValue;

        private static void Local(string message)
        {
            try
            {
                SystemChat.Show(message);
            }
            catch { }
        }
    }

    // (The old EndGameManager Update/SetEverythingUp Arsonist patches are gone, 
    // MarshAPI's central ModdedGameOver pair draws the end screen for every role.)

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ExileController_Begin_ArsonistPatch
    {
        private static void Prefix(ExileController __instance, GameData.PlayerInfo exiled, bool tie)
        {
            if (__instance == null || exiled == null || tie) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.PlayerId != exiled.PlayerId) continue;
                if (!RoleRegistry.IsAssigned(player, ArsonistRole.Id)) return;
                var text = exiled.PlayerName + " was the Arsonist.";
                if (__instance.Text != null) __instance.Text.Text = text;
                // completeString is protected in the 2026.8.9 interop.
                GameReflection.SetCompleteString(__instance, text);
                return;
            }
        }
    }

    // Exile reveal text is re-applied every frame by Core/ExileTextFix, which polls
    // ExileController.Instance. The old patch targeted a compiler-generated coroutine type
    // that the interop no longer emits. Never patch coroutine types.

}
