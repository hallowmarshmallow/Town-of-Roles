using System;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;
using TownOfRoles.Roles.Engineer;
using TownOfRoles.Roles.Sheriff;

namespace TownOfRoles.Roles.Jester
{
    internal static class JesterSystem
    {
        private const string JesterWinRpc = "townofroles.JesterWin";

        private static readonly Color WinColor = new(0.86f, 0.35f, 0.95f, 1f);

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            ModdedWin.Clear();
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        public static void OnPlayerExiled(PlayerEventArgs args)
        {
            if (args?.Player == null || !IsJester(args.Player)) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || ShipStatus.Instance == null) return;

            ModdedWin.Declare(args.Player);
            ModdedGameOver.Claim("Jester Wins", WinColor);
            TownOfRolesRpcMux.Send(JesterWinRpc, args.Player.PlayerId);
            ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
        }

        public static void ConvertToJester(PlayerControl player)
        {
            if (player == null || player.Data == null) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            if (!RoleRegistry.AssignVirtualAndSync(player, JesterRole.Id)) return;
            NotifyConverted(player);
        }

        private static void NotifyConverted(PlayerControl player)
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || player == null || local.PlayerId != player.PlayerId) return;
            try
            {
                SystemChat.Show("You became the Jester — get yourself voted out to win!");
            }
            catch { }
        }

        [AtomicRpc(JesterWinRpc)]
        private static void OnJesterWinRpc(byte senderId, byte winnerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;

            ModdedWin.Declare(FindPlayer(winnerId));
            ModdedGameOver.Claim("Jester Wins", WinColor);
        }

        public static bool IsJester(PlayerControl player) => RoleRegistry.IsAssigned(player, JesterRole.Id);

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.Data != null && player.PlayerId == playerId) return player;
            return null;
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ExileController_Begin_JesterPatch
    {
        private static void Prefix(ExileController __instance, GameData.PlayerInfo exiled, bool tie)
        {
            if (__instance == null || exiled == null || tie) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.PlayerId != exiled.PlayerId) continue;
                if (!RoleRegistry.IsAssigned(player, JesterRole.Id)) return;
                var text = exiled.PlayerName + " was the Jester.";
                if (__instance.Text != null) __instance.Text.Text = text;

                GameReflection.SetCompleteString(__instance, text);
                return;
            }
        }
    }
}
