using System;
using System.Collections.Generic;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Executioner
{
    internal static class ExecutionerSystem
    {
        private const string TargetRpc = "townofroles.ExecutionerTarget";
        private const string WinRpc = "townofroles.ExecutionerWin";
        private const string ConvertRpc = "townofroles.ExecutionerConvert";
        private const int MaxRetries = 300;

        private static readonly Dictionary<byte, byte> TargetOf = new();

        private static readonly HashSet<byte> Converted = new();

        private static readonly Color WinColor = new(0.45f, 0.9f, 0.85f, 1f);

        private static byte? _pendingExecutionerId;
        private static byte? _pendingTargetId;
        private static int _pendingRetries;

        public static bool IsExecutioner(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, ExecutionerRole.Id);

        public static bool IsActiveExecutioner(PlayerControl player) =>
            IsExecutioner(player) && !Converted.Contains(player.PlayerId);

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

        private static bool RecordTarget(byte executionerId, byte targetId)
        {
            if (FindPlayer(executionerId) == null || FindPlayer(targetId) == null) return false;
            TargetOf[executionerId] = targetId;
            return true;
        }

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

        private static bool IsClaimedByCustomRole(PlayerControl player) =>
            RoleRegistry.HasAnyCustomRole(player);

        public static void OnPlayerExiled(PlayerEventArgs args)
        {
            if (args?.Player == null) return;
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || ShipStatus.Instance == null) return;

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

        private static void ConvertExecutioner(PlayerControl executioner)
        {
            if (executioner == null || executioner.Data == null) return;
            Converted.Add(executioner.PlayerId);
            TargetOf.Remove(executioner.PlayerId);

            var mode = RoleConfig.ExecutionerConvertRole?.Value ?? "Jester";
            if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
            {
                if (RoleManager.Instance != null) RoleManager.Instance.AssignRole(executioner, "Crewmate");
            }
            else
            {
                TownOfRoles.Roles.Jester.JesterSystem.ConvertToJester(executioner);
            }
            TownOfRolesRpcMux.Send(ConvertRpc, executioner.PlayerId, mode);

            if (string.Equals(mode, "Crewmate", StringComparison.OrdinalIgnoreCase))
                NotifyConverted(executioner, mode);
        }

        [AtomicRpc(ConvertRpc)]
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

        [AtomicRpc(TargetRpc)]
        private static void OnTargetRpc(byte senderId, byte executionerId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;

            if (RecordTarget(executionerId, targetId)) return;
            _pendingExecutionerId = executionerId;
            _pendingTargetId = targetId;
            _pendingRetries = 0;
        }

        [AtomicRpc(WinRpc)]
        private static void OnWinRpc(byte senderId, byte executionerId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ModdedGameOver.Claim("Executioner Wins", WinColor);
            TargetOf[executionerId] = targetId;
        }

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

                GameReflection.SetCompleteString(__instance, text);
                return;
            }
        }
    }
}
