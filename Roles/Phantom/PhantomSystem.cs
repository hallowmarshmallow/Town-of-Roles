using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Phantom
{
    // Phantom gameplay logic (ported from Town-Of-Us' Phantom.cs).
    internal static class PhantomSystem
    {
        private const string DeathRpc = "townofroles.PhantomDeath";
        private const string WinRpc = "townofroles.PhantomWin";

        private static readonly Color WinColor = new(0.75f, 0.75f, 0.85f, 1f);

        private static readonly HashSet<byte> PhantomDead = new(); // all clients: faded rendering

        public static bool IsPhantom(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, PhantomRole.Id);

        // True once the phantom has died (win condition is now live).
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

            // Every client (host included): keep dead phantoms faded. The game
            // restores material colors on cosmetic refreshes, so re-apply
            // idempotently each tick instead of relying on the one-shot RPC.
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
                    if (player.Data.Tasks.get_Item(i) == null || !player.Data.Tasks.get_Item(i).Complete) { allDone = false; break; }
                if (!allDone) continue;

                // The Phantom whose tasks are all done is this round's winner.
                ModdedWin.Declare(player);
                ModdedGameOver.Claim("Phantom Wins", WinColor);
                TownOfRolesRpcMux.Send(WinRpc, player.PlayerId);
                ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
                return;
            }
        }

        // Semi-transparent body so the phantom can move unseen (host + clients).
        private static void Fade(PlayerControl player)
        {
            if (player == null) return;
            try
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    // The 2026.9.20 player has Renderer.material only; the materials array is not
                    // in its metadata the way it was in 2026.9.10, so one material per renderer is
                    // faded rather than every slot. material (not sharedMaterial) is what keeps the
                    // tint on this player's body alone.
                    var mat = renderer.material;
                    if (mat == null) continue;
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.15f);
                }
            }
            catch { }
        }

        [ReactorRpc(DeathRpc)]
        private static void OnDeath(byte senderId, byte phantomId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            PhantomDead.Add(phantomId);
            Fade(FindPlayer(phantomId));
        }

        [ReactorRpc(WinRpc)]
        private static void OnWin(byte senderId, byte winnerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            // The id travels with the win: each client draws its own end screen, and a
            // client that only knew "the Phantom won" could not name the player on it.
            ModdedWin.Declare(FindPlayer(winnerId));
            ModdedGameOver.Claim("Phantom Wins", WinColor);
        }

        public static void Reset()
        {
            PhantomDead.Clear();
            // The winner list and the title claim belong to the round that just ended;
            // the result screen that read them is gone with it.
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

    // The end screen is drawn by MarshAPI's central ModdedGameOver patches; the
    // Phantom claims its title at its win sites. The per-role EndGameManager patch
    // pair that used to live here is gone, see MarshAPI/Endgame/ModdedGameOver.cs.

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
