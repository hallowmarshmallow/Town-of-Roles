using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Glitch
{
    // The Glitch gameplay logic (ported from Town-Of-Us' Glitch.cs).
    internal static class GlitchSystem
    {
        private const string MimicRpc = "townofroles.GlitchMimic";
        private const string HackRpc = "townofroles.GlitchHack";
        private const string RequestMimicRpc = "townofroles.GlitchRequestMimic";
        private const string RequestHackRpc = "townofroles.GlitchRequestHack";
        private const string RequestKillRpc = "townofroles.GlitchRequestKill";
        private const string KillRpc = "townofroles.GlitchKill";
        private const string WinRpc = "townofroles.GlitchWin";

        private static readonly Dictionary<byte, DateTime> MimicUntil = new();
        private static readonly Dictionary<byte, DateTime> HackUntil = new();
        // Independent cooldowns per ability (the OG role has three separate
        // buttons with three separate timers).
        private static readonly Dictionary<byte, DateTime> MimicCooldowns = new();
        private static readonly Dictionary<byte, DateTime> HackCooldowns = new();
        private static readonly Dictionary<byte, DateTime> KillCooldowns = new();
        private static readonly Dictionary<byte, (string Name, int Color)> OriginalOutfit = new();

        private static readonly Color WinColor = new(0.45f, 0.95f, 0.35f, 1f);

        public static bool IsGlitch(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, GlitchRole.Id);

        public static bool IsHacked(PlayerControl player) =>
            player != null && player.Data != null && HackUntil.TryGetValue(player.PlayerId, out var until) && DateTime.UtcNow < until;

        // Mimic
        internal static bool CanMimicNow(PlayerControl glitch)
        {
            if (!IsGlitch(glitch) || glitch.Data == null || glitch.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(glitch.PlayerId, MimicCooldowns) &&
                   ClosestPlayerFinder.GetClosestTarget(glitch, out _);
        }

        public static void TryMimic(PlayerControl glitch)
        {
            var client = AmongUsClient.Instance;
            if (client == null || glitch == null || glitch.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestMimicRpc, glitch.PlayerId);
                return;
            }
            if (!CanMimicNow(glitch)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(glitch, out var target)) return;
            if (target.Data == null) return;

            var targetName = target.Data.PlayerName;
            var targetColor = target.Data.ColorId;
            var ownName = glitch.Data.PlayerName;
            var ownColor = glitch.Data.ColorId;

            OriginalOutfit[glitch.PlayerId] = (ownName, ownColor);
            MimicUntil[glitch.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchMimicDuration, 10f));
            MimicCooldowns[glitch.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchMimicCooldown, 30f));

            ApplyName(glitch, targetName);
            Recolor(glitch, targetColor);
            TownOfRolesRpcMux.Send(MimicRpc, glitch.PlayerId, targetName, targetColor);
        }

        // Hack
        internal static bool CanHackNow(PlayerControl glitch)
        {
            if (!IsGlitch(glitch) || glitch.Data == null || glitch.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(glitch.PlayerId, HackCooldowns) &&
                   ClosestPlayerFinder.GetClosestTarget(glitch, out _);
        }

        public static void TryHack(PlayerControl glitch)
        {
            var client = AmongUsClient.Instance;
            if (client == null || glitch == null || glitch.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestHackRpc, glitch.PlayerId);
                return;
            }
            if (!CanHackNow(glitch)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(glitch, out var target)) return;
            if (target == glitch || target.Data == null) return;

            HackUntil[target.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchHackDuration, 10f));
            HackCooldowns[glitch.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchHackCooldown, 30f));
            TownOfRolesRpcMux.Send(HackRpc, target.PlayerId);
        }

        // Kill
        internal static bool CanKillNow(PlayerControl glitch)
        {
            if (!IsGlitch(glitch) || glitch.Data == null || glitch.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(glitch.PlayerId, KillCooldowns) &&
                   ClosestPlayerFinder.GetClosestTarget(glitch, out _);
        }

        public static void TryKill(PlayerControl glitch)
        {
            var client = AmongUsClient.Instance;
            if (client == null || glitch == null || glitch.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestKillRpc, glitch.PlayerId);
                return;
            }
            if (!CanKillNow(glitch)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(glitch, out var target)) return;
            if (target == glitch || target.Data == null) return;

            KillCooldowns[glitch.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchKillCooldown, 30f));
            KillManager.Kill(glitch, target);
            TownOfRolesRpcMux.Send(KillRpc, target.PlayerId);
        }

        // Round lifecycle / pool
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            MimicUntil.Clear();
            HackUntil.Clear();
            MimicCooldowns.Clear();
            HackCooldowns.Clear();
            KillCooldowns.Clear();
            OriginalOutfit.Clear();
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        // Host tick: revert expired mimics, and run the elimination win check.
        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            RevertExpiredMimics();
            CheckEliminationWin();
        }

        private static void RevertExpiredMimics()
        {
            if (MimicUntil.Count == 0) return;
            var now = DateTime.UtcNow;
            foreach (var key in new List<byte>(MimicUntil.Keys))
            {
                if (now < MimicUntil[key]) continue;
                var glitch = FindPlayer(key);
                if (glitch == null || glitch.Data == null) continue;

                MimicUntil.Remove(key);
                var (ownName, ownColor) = OriginalOutfit.TryGetValue(key, out var cached)
                    ? cached
                    : (glitch.Data.PlayerName, glitch.Data.ColorId);
                OriginalOutfit.Remove(key);

                ApplyName(glitch, ownName);
                Recolor(glitch, ownColor);
                TownOfRolesRpcMux.Send("townofroles.GlitchRevert", key, ownName, ownColor);
                return; // one revert per tick is plenty
            }
        }

        // The Glitch wins when every other living player is dead.
        private static void CheckEliminationWin()
        {
            if (ModdedGameOver.HasClaim || ShipStatus.Instance == null) return;
            var aliveGlitches = 0;
            var aliveOthers = 0;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;
                if (RoleRegistry.IsAssigned(player, GlitchRole.Id)) aliveGlitches++;
                else aliveOthers++;
            }
            if (aliveGlitches == 0 || aliveOthers > 0) return;

            ModdedGameOver.Claim("The Glitch Wins", WinColor);
            TownOfRolesRpcMux.Send(WinRpc);
            ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
        }

        // RPCs
        [ReactorRpc(RequestMimicRpc)]
        private static void OnRequestMimic(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryMimic(player);
                    return;
                }
            }
        }

        [ReactorRpc(RequestHackRpc)]
        private static void OnRequestHack(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryHack(player);
                    return;
                }
            }
        }

        [ReactorRpc(RequestKillRpc)]
        private static void OnRequestKill(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryKill(player);
                    return;
                }
            }
        }

        [ReactorRpc(MimicRpc)]
        private static void OnMimic(byte senderId, byte glitchId, string targetName, int targetColor)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var glitch = FindPlayer(glitchId);
            if (glitch == null) return;
            // The name already arrived through the host's RpcSetName broadcast;
            // clients only need to recolor the body.
            Recolor(glitch, targetColor);
        }

        [ReactorRpc("townofroles.GlitchRevert")]
        private static void OnRevert(byte senderId, byte glitchId, string ownName, int ownColor)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var glitch = FindPlayer(glitchId);
            if (glitch == null) return;
            Recolor(glitch, ownColor);
        }

        [ReactorRpc(HackRpc)]
        private static void OnHack(byte senderId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var target = FindPlayer(targetId);
            if (target == null) return;
            HackUntil[target.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.GlitchHackDuration, 10f));
            if (target.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
                Local("You have been hacked! You cannot report bodies or do tasks.");
        }

        [ReactorRpc(KillRpc)]
        private static void OnKill(byte senderId, byte targetId)
        {
            // KillManager.Kill is already networked host-authoritative; the
            // companion RPC exists only to keep client-side kill tables in sync
            // for future hacking/guessing features.
        }

        [ReactorRpc(WinRpc)]
        private static void OnWin(byte senderId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ModdedGameOver.Claim("The Glitch Wins", WinColor);
        }

        // Hack suppression hooks
        // GameEvents.BeforeReport hook: a hacked player cannot report bodies.
        public static void OnBeforeReport(ReportEventArgs args)
        {
            if (args.IsEmergencyMeeting || args.Reporter == null) return;
            if (IsHacked(args.Reporter)) args.Cancelled = true;
        }

        // End screen
        // Drawn by MarshAPI's central ModdedGameOver patches; the Glitch claims its
        // title at its win sites. The per-role EndGameManager patch pair that used to
        // live here is gone, see MarshAPI/Endgame/ModdedGameOver.cs.

        // Visual helpers (shared with Morphling)
        private static void ApplyName(PlayerControl player, string name)
        {
            if (player == null || string.IsNullOrEmpty(name)) return;
            try
            {
                if (player.Data != null && player.Data.PlayerName != name)
                    player.RpcSetName(name);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Glitch name: " + e.Message);
            }
        }

        private static void Recolor(PlayerControl player, int colorId)
        {
            if (player == null) return;
            try
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    try { PlayerControl.SetPlayerMaterialColors(colorId, renderer); } catch { }
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Glitch recolor: " + e.Message);
            }
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        // Seconds this Glitch's mimic still runs for, zero when it is not up.
        public static float MimicSecondsRemaining(PlayerControl glitch) => SecondsLeft(MimicUntil, glitch);

        // Seconds this Glitch's hack still runs for, zero when it is not up.
        public static float HackSecondsRemaining(PlayerControl glitch) => SecondsLeft(HackUntil, glitch);

        private static float SecondsLeft(Dictionary<byte, DateTime> table, PlayerControl player)
        {
            if (player == null || player.Data == null) return 0f;
            if (!table.TryGetValue(player.PlayerId, out var until)) return 0f;
            var left = (until - DateTime.UtcNow).TotalSeconds;
            return left > 0 ? (float)left : 0f;
        }

        private static DateTime GetCooldown(byte glitchId, Dictionary<byte, DateTime> table) =>
            table.TryGetValue(glitchId, out var value) ? value : DateTime.MinValue;

        // Seconds until the Glitch's kill is ready, for the HUD digits, the same table
        // CanKillNow gates on.
        internal static float SecondsUntilKillReady(PlayerControl glitch)
        {
            if (glitch == null || glitch.Data == null) return 0f;
            var until = GetCooldown(glitch.PlayerId, KillCooldowns);
            return Mathf.Max(0f, (float)(until - DateTime.UtcNow).TotalSeconds);
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

    // (The old EndGameManager Update/SetEverythingUp Glitch patches are gone, 
    // MarshAPI's central ModdedGameOver pair draws the end screen for every role.)

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ExileController_Begin_GlitchPatch
    {
        private static void Prefix(ExileController __instance, GameData.PlayerInfo exiled, bool tie)
        {
            if (__instance == null || exiled == null || tie) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.PlayerId != exiled.PlayerId) continue;
                if (!RoleRegistry.IsAssigned(player, GlitchRole.Id)) return;
                var text = exiled.PlayerName + " was The Glitch.";
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
