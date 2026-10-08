using System;
using System.Collections.Generic;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Phantom
{
    internal static class PhantomSystem
    {
        private const string DeathRpc = "townofroles.PhantomDeath";
        private const string WinRpc = "townofroles.PhantomWin";

        private static readonly Color WinColor = new(0.75f, 0.75f, 0.85f, 1f);

        private static readonly HashSet<byte> PhantomDead = new();

        public static bool IsPhantom(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, PhantomRole.Id);

        public static bool IsPhantomDead(PlayerControl player) =>
            player != null && PhantomDead.Contains(player.PlayerId);

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost)
            {
                CheckDeath();
                CheckWin();
            }

            if (PhantomDead.Count > 0)
            {
                foreach (var id in new List<byte>(PhantomDead))
                    Fade(FindPlayer(id));
            }
        }

        private static void CheckDeath()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (!IsPhantom(player)) continue;
                if (!player.Data.IsDead || PhantomDead.Contains(player.PlayerId)) continue;
                PhantomDead.Add(player.PlayerId);
                TownOfRolesRpcMux.Send(DeathRpc, player.PlayerId);
                Fade(player);
            }
        }

        private static void CheckWin()
        {
            if (ModdedGameOver.HasClaim || ShipStatus.Instance == null) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected) continue;
                if (!IsPhantom(player) || !PhantomDead.Contains(player.PlayerId)) continue;
                if (player.Data.Tasks == null || player.Data.Tasks.Count == 0) continue;
                var allDone = true;
                for (int i = 0; i < player.Data.Tasks.Count; i++)
                    if (player.Data.Tasks[i] == null || !player.Data.Tasks[i].Complete) { allDone = false; break; }
                if (!allDone) continue;

                ModdedWin.Declare(player);
                ModdedGameOver.Claim("Phantom Wins", WinColor);
                TownOfRolesRpcMux.Send(WinRpc, player.PlayerId);
                ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
                return;
            }
        }

        private static void Fade(PlayerControl player)
        {
            if (player == null) return;
            try
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;

                    var mat = renderer.material;
                    if (mat == null) continue;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.15f);
                }
            }
            catch { }
        }

        [AtomicRpc(DeathRpc)]
        private static void OnDeath(byte senderId, byte phantomId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            PhantomDead.Add(phantomId);
            Fade(FindPlayer(phantomId));
        }

        [AtomicRpc(WinRpc)]
        private static void OnWin(byte senderId, byte winnerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;

            ModdedWin.Declare(FindPlayer(winnerId));
            ModdedGameOver.Claim("Phantom Wins", WinColor);
        }

        public static void Reset()
        {
            PhantomDead.Clear();

            ModdedWin.Clear();
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) { }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        private static void Local(string message)
        {
            try
            {
                SystemChat.Show(message);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ExileController_Begin_PhantomPatch
    {
        private static void Prefix(ExileController __instance, GameData.PlayerInfo exiled, bool tie)
        {
            if (__instance == null || exiled == null || tie) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.PlayerId != exiled.PlayerId) continue;
                if (!RoleRegistry.IsAssigned(player, PhantomRole.Id)) return;
                var text = exiled.PlayerName + " was the Phantom.";
                if (__instance.Text != null) __instance.Text.Text = text;

                GameReflection.SetCompleteString(__instance, text);
                return;
            }
        }
    }
}
