using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Executioner
{
    // Executioner gameplay logic (ported from Town-Of-Us' Executioner.cs).
    internal static class ExecutionerSystem
    {
        private const string TargetRpc = "townofroles.ExecutionerTarget";
        private const string WinRpc = "townofroles.ExecutionerWin";
        private const string ConvertRpc = "townofroles.ExecutionerConvert";
        private const int MaxRetries = 300;

        private static readonly Dictionary<byte, byte> TargetOf = new(); // executionerId -> targetId
        // Players converted away from the Executioner (target died; now Jester/Crewmate).
        private static readonly HashSet<byte> Converted = new();

        private static readonly Color WinColor = new(0.45f, 0.9f, 0.85f, 1f);

        private static byte? _pendingExecutionerId;
        private static byte? _pendingTargetId;
        private static int _pendingRetries;

        public static bool IsExecutioner(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, ExecutionerRole.Id);

        // True for an Executioner that has not converted away (still can win / show as
        // Executioner).
        public static bool IsActiveExecutioner(PlayerControl player) =>
            IsExecutioner(player) && !Converted.Contains(player.PlayerId);

        // True when the player converted to a plain Crewmate after their target died.
        public static bool IsConverted(PlayerControl player) =>
            player != null && Converted.Contains(player.PlayerId);

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            TargetOf.Clear();
            Converted.Clear();
            _pendingExecutionerId = null;
            _pendingTargetId = null;
            _pendingRetries = 0;
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null) return;

            if (client.AmHost)
            {
                // The target leaving the game also converts the Executioner
                // (no disconnect event exists, so poll the data flag).
                if (RoleConfig.ExecutionerConvertOnTargetDeath?.Value != false)
                {
                    foreach (var pair in TargetOf)
                    {
                        if (!Converted.Contains(pair.Key) && IsTargetGone(pair.Value))
                        {
                            var executioner = FindPlayer(pair.Key);
                            if (executioner != null) ConvertExecutioner(executioner);
                            break;
                        }
                    }
                }

                AssignTargetsToUnpaired();
            }

            // A client whose target RPC arrived before the registry's assignment
            // for that player: hold it until both ends exist.
            if (!_pendingExecutionerId.HasValue) return;
            if (RecordTarget(_pendingExecutionerId.Value, _pendingTargetId ?? 255))
            {
                _pendingExecutionerId = null;
                _pendingTargetId = null;
                _pendingRetries = 0;
                return;
            }
            if (++_pendingRetries >= MaxRetries)
            {
                _pendingExecutionerId = null;
                _pendingTargetId = null;
                _pendingRetries = 0;
            }
        }

        // Gives every Executioner that does not have one yet a target.
        private static void AssignTargetsToUnpaired()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (!IsActiveExecutioner(player)) continue;
                if (TargetOf.ContainsKey(player.PlayerId)) continue;

                var target = PickTarget(player);
                if (target == null) continue;
                TargetOf[player.PlayerId] = target.PlayerId;
                TownOfRolesRpcMux.Send(TargetRpc, player.PlayerId, target.PlayerId);
            }
        }

        // Records a target once both players exist. False means try again later.
        private static bool RecordTarget(byte executionerId, byte targetId)
        {
            if (FindPlayer(executionerId) == null || FindPlayer(targetId) == null) return false;
            TargetOf[executionerId] = targetId;
            return true;
        }

        // A random living, non-Impostor, unclaimed player (never the Executioner).
        private static PlayerControl PickTarget(PlayerControl executioner)
        {
            var targets = new List<PlayerControl>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player == executioner || player.Data == null) continue;
                if (player.Data.Disconnected || player.Data.IsDead) continue;
                if (player.Data.myRole == null || player.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor) continue;
                if (IsClaimedByCustomRole(player)) continue;
                targets.Add(player);
            }
            if (targets.Count == 0) return null;
            return targets[UnityEngine.Random.Range(0, targets.Count)];
        }

        // True when the player already carries one of our custom roles.
        private static bool IsClaimedByCustomRole(PlayerControl player) =>
            RoleRegistry.HasAnyCustomRole(player);

        public static void OnPlayerExiled(PlayerEventArgs args)
        {
            if (args?.Player == null) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || ShipStatus.Instance == null) return;

            // Find an Executioner whose target was just exiled.
            foreach (var pair in TargetOf)
            {
                if (pair.Value != args.Player.PlayerId) continue;
                var executioner = FindPlayer(pair.Key);
                if (executioner == null || !IsActiveExecutioner(executioner)) continue;
                ModdedGameOver.Claim("Executioner Wins", WinColor);
                TownOfRolesRpcMux.Send(WinRpc, executioner.PlayerId, pair.Value);
                ShipStatus.Instance.StartEndGame(GameOverReason.Custom, 0.5f);
                return;
            }
        }

        // Conversion (target died by non ejection means)
        // GameEvents.BeforeMurder hook (host): when an Executioner's target is killed instead
        // of voted out, the Executioner converts, becoming a Jester or a plain Crewmate (role
        // configurable, ported from Town-Of-Us' "Executioner becomes on Target Dead" option).
        public static void OnBeforeMurder(MurderEventArgs args)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || args?.Target == null) return;
            if (RoleConfig.ExecutionerConvertOnTargetDeath?.Value == false) return;

            byte? executionerId = null;
            foreach (var pair in TargetOf)
            {
                if (pair.Value == args.Target.PlayerId) { executionerId = pair.Key; break; }
            }
            if (!executionerId.HasValue) return;
            var executioner = FindPlayer(executionerId.Value);
            if (executioner == null || !IsActiveExecutioner(executioner)) return;
            ConvertExecutioner(executioner);
        }

        private static bool IsTargetGone(byte targetId)
        {
            var target = FindPlayer(targetId);
            return target == null || target.Data == null || target.Data.Disconnected;
        }

        // Converts the Executioner away from the role (host + clients).
        private static void ConvertExecutioner(PlayerControl executioner)
        {
            if (executioner == null || executioner.Data == null) return;
            Converted.Add(executioner.PlayerId);
            TargetOf.Remove(executioner.PlayerId);

            var mode = RoleConfig.ExecutionerConvertRole?.Value ?? "Jester";
            if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
            {
                // Plain crewmate: vanilla role (the virtual Executioner registry
                // assignment lingers, but presentation/win hooks ignore it via
                // Converted, so the player is effectively a normal Crewmate).
                if (RoleManager.Instance != null) RoleManager.Instance.AssignRole(executioner, "Crewmate");
            }
            else
            {
                // Jester: the existing Jester machinery (assignment + RPC +
                // win-when-voted-out) takes over the player.
                TownOfRoles.Roles.Jester.JesterSystem.ConvertToJester(executioner);
            }
            TownOfRolesRpcMux.Send(ConvertRpc, executioner.PlayerId, mode);
            // Jester mode notifies through JesterSystem.ConvertToJester already;
            // only the Crewmate path needs its own notification.
            if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
                NotifyConverted(executioner, mode);
        }

        [ReactorRpc(ConvertRpc)]
        private static void OnConvertRpc(byte senderId, byte executionerId, string mode)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            Converted.Add(executionerId);
            TargetOf.Remove(executionerId);
            if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
            {
                var player = FindPlayer(executionerId);
                if (player != null && RoleManager.Instance != null) RoleManager.Instance.AssignRole(player, "Crewmate");
                NotifyConverted(player, mode);
            }
        }

        private static void NotifyConverted(PlayerControl player, string mode)
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || player == null || local.PlayerId != player.PlayerId) return;
            try
            {
                if (HudManager.Instance?.ChatPopup == null) return;
                if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
                    SystemChat.Show("Your target died — you are now a plain Crewmate.");
                else
                    SystemChat.Show("Your target died — you became the Jester! Get yourself voted out to win.");
            }
            catch { }
        }

        [ReactorRpc(TargetRpc)]
        private static void OnTargetRpc(byte senderId, byte executionerId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;

            // The role itself arrives through the registry's own assignment RPC; this
            // carries only who to hunt.
            if (RecordTarget(executionerId, targetId)) return;
            _pendingExecutionerId = executionerId;
            _pendingTargetId = targetId;
            _pendingRetries = 0;
        }

        [ReactorRpc(WinRpc)]
        private static void OnWinRpc(byte senderId, byte executionerId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ModdedGameOver.Claim("Executioner Wins", WinColor);
            TargetOf[executionerId] = targetId;
        }

        // (The end-screen title/apply members are gone: MarshAPI's central
        // ModdedGameOver pair draws the screen from the Claim above.)

        public static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    internal static class ExileController_Begin_ExecutionerPatch
    {
        private static void Prefix(ExileController __instance, GameData.PlayerInfo exiled, bool tie)
        {
            if (__instance == null || exiled == null || tie) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.PlayerId != exiled.PlayerId) continue;
                if (!ExecutionerSystem.IsActiveExecutioner(player)) return;
                var text = exiled.PlayerName + " was the Executioner.";
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

    // (The old EndGameManager Update/SetEverythingUp Executioner patches are gone, 
    // MarshAPI's central ModdedGameOver pair draws the end screen for every role.)
}
