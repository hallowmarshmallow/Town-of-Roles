using System;
using ClassicUs.Reactor;
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
            // The result screen is the only owner of the winner list, and it is gone by
            // now: whatever it was told belongs to the round that just ended.
            ModdedWin.Clear();
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        public static void OnPlayerExiled(PlayerEventArgs args)
        {
            if (args?.Player == null || !IsJester(args.Player)) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || ShipStatus.Instance == null) return;
            // The Jester who was voted out is the winner of this round.
            ModdedWin.Declare(args.Player);
            ModdedGameOver.Claim("Jester Wins", WinColor);
            TownOfRolesRpcMux.Send(JesterWinRpc, args.Player.PlayerId);
            ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
        }

        // Converts a player to the Jester (host-authoritative, e.g. an Executioner whose target
        // died).
        public static void ConvertToJester(PlayerControl player)
        {
            if (player == null || player.Data == null) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return; // host-authoritative
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

        [ReactorRpc(JesterWinRpc)]
        private static void OnJesterWinRpc(byte senderId, byte winnerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            // The id travels with the win: every client draws its own end screen, and a
            // client that only knew "a Jester won" could not name the player on it.
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
                // completeString is protected in the 2026.8.9 interop.
                GameReflection.SetCompleteString(__instance, text);
                return;
            }
        }
    }

    // Exile reveal text is re-applied every frame by Core/ExileTextFix, which polls
    // ExileController.Instance. The old patch targeted a compiler-generated coroutine type
    // that the interop no longer emits. Never patch coroutine types.

    // The end screen is drawn by MarshAPI's central ModdedGameOver patches; the
    // Jester claims its title at its win sites and names its winner through
    // ModdedWin. The per-role EndGameManager patch pair that used to live here is
    // gone, see MarshAPI/Endgame/ModdedGameOver.cs.
}
