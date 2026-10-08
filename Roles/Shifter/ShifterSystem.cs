using System;
using System.Collections.Generic;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Shifter
{
    internal static class ShifterSystem
    {
        private const string SwapRpc = "townofroles.ShifterSwap";
        private const string RequestShiftRpc = "townofroles.ShifterRequestShift";
        private const string SuicideRpc = "townofroles.ShifterSuicide";

        private static readonly Dictionary<byte, DateTime> Cooldowns = new();

        private static readonly HashSet<byte> SwappedAway = new();

        public static bool IsShifter(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, ShifterRole.Id) &&
            !SwappedAway.Contains(player.PlayerId);

        internal static bool CanShiftNow(PlayerControl shifter)
        {
            if (!IsShifter(shifter) || shifter.Data == null || shifter.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(shifter.PlayerId) &&
                   ClosestPlayerFinder.GetClosestTarget(shifter, out _);
        }

        public static void TryShift(PlayerControl shifter)
        {
            var client = AmongUsClient.Instance;
            if (client == null || shifter == null || shifter.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestShiftRpc, shifter.PlayerId);
                return;
            }
            if (!CanShiftNow(shifter)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(shifter, out var target)) return;
            if (target == shifter || target.Data == null) return;

            Cooldowns[shifter.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.ShiftCooldown, 30f));

            if (target.Data.myRole != null && target.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor)
            {
                KillManager.Kill(shifter, shifter);
                TownOfRolesRpcMux.Send(SuicideRpc, shifter.PlayerId);
                Local("You tried to shift an Impostor and died.");
                return;
            }

            var targetRoleId = FindAssignedRoleId(target);
            var shifterRoleId = FindAssignedRoleId(shifter) ?? string.Empty;

            var shifterTasks = GetTaskTypeIds(shifter);
            var targetTasks = GetTaskTypeIds(target);
            try
            {
                if (GameData.Instance != null)
                {
                    GameData.Instance.RpcSetTasks(shifter.PlayerId, targetTasks);
                    GameData.Instance.RpcSetTasks(target.PlayerId, shifterTasks);
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Shifter tasks: " + e.Message);
            }

            SwappedAway.Add(shifter.PlayerId);

            if (targetRoleId == null)
            {
                if (RoleManager.Instance != null)
                {
                    RoleManager.Instance.AssignRole(target, ShifterRole.Id);
                    RoleManager.Instance.AssignRole(shifter, "Crewmate");
                }
            }
            else
            {
                if (RoleManager.Instance != null)
                {
                    RoleManager.Instance.AssignRole(shifter, targetRoleId);
                    RoleManager.Instance.AssignRole(target, "Crewmate");
                }
            }

            TownOfRolesRpcMux.Send(SwapRpc, shifter.PlayerId, target.PlayerId, targetRoleId ?? string.Empty);
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            Cooldowns.Clear();
            SwappedAway.Clear();
        }

        public static void OnGameEnded(GameEndedEventArgs _) { }

        [AtomicRpc(RequestShiftRpc)]
        private static void OnRequestShift(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryShift(player);
                    return;
                }
            }
        }

        [AtomicRpc(SwapRpc)]
        private static void OnSwap(byte senderId, byte shifterId, byte targetId, string targetRoleId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var shifter = FindPlayer(shifterId);
            var target = FindPlayer(targetId);
            if (shifter == null || target == null || RoleManager.Instance == null) return;

            SwappedAway.Add(shifterId);
            if (string.IsNullOrEmpty(targetRoleId))
            {
                RoleManager.Instance.AssignRole(target, ShifterRole.Id);
                RoleManager.Instance.AssignRole(shifter, "Crewmate");
            }
            else
            {
                RoleManager.Instance.AssignRole(shifter, targetRoleId);
                RoleManager.Instance.AssignRole(target, "Crewmate");
            }
        }

        [AtomicRpc(SuicideRpc)]
        private static void OnSuicide(byte senderId, byte shifterId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;

            var shifter = FindPlayer(shifterId);
            if (shifter != null && shifter.PlayerId == PlayerControl.LocalPlayer?.PlayerId)
                Local("You tried to shift an Impostor and died.");
        }

        private static string FindAssignedRoleId(PlayerControl player)
        {
            if (player == null) return null;
            if (RoleRegistry.IsAssigned(player, "townofroles.Sheriff")) return "townofroles.Sheriff";
            if (RoleRegistry.IsAssigned(player, "townofroles.Engineer")) return "townofroles.Engineer";
            if (RoleRegistry.IsAssigned(player, "townofroles.Jester")) return "townofroles.Jester";
            if (RoleRegistry.IsAssigned(player, "townofroles.Medic")) return "townofroles.Medic";
            if (RoleRegistry.IsAssigned(player, "townofroles.Seer")) return "townofroles.Seer";
            if (RoleRegistry.IsAssigned(player, "townofroles.Vigilante")) return "townofroles.Vigilante";
            if (RoleRegistry.IsAssigned(player, "townofroles.Assassin")) return "townofroles.Assassin";
            if (RoleRegistry.IsAssigned(player, "townofroles.Janitor")) return "townofroles.Janitor";
            if (RoleRegistry.IsAssigned(player, "townofroles.Altruist")) return "townofroles.Altruist";
            if (RoleRegistry.IsAssigned(player, "townofroles.Mayor")) return "townofroles.Mayor";
            if (RoleRegistry.IsAssigned(player, "townofroles.Executioner")) return "townofroles.Executioner";
            if (RoleRegistry.IsAssigned(player, "townofroles.Arsonist")) return "townofroles.Arsonist";
            if (RoleRegistry.IsAssigned(player, "townofroles.Swapper")) return "townofroles.Swapper";
            if (RoleRegistry.IsAssigned(player, "townofroles.Morphling")) return "townofroles.Morphling";
            if (RoleRegistry.IsAssigned(player, "townofroles.Spy")) return "townofroles.Spy";
            if (RoleRegistry.IsAssigned(player, "townofroles.Camouflager")) return "townofroles.Camouflager";
            if (RoleRegistry.IsAssigned(player, "townofroles.Swooper")) return "townofroles.Swooper";
            if (RoleRegistry.IsAssigned(player, "townofroles.Underdog")) return "townofroles.Underdog";
            if (RoleRegistry.IsAssigned(player, "townofroles.Undertaker")) return "townofroles.Undertaker";
            if (RoleRegistry.IsAssigned(player, "townofroles.Investigator")) return "townofroles.Investigator";
            if (RoleRegistry.IsAssigned(player, "townofroles.TimeLord")) return "townofroles.TimeLord";
            if (RoleRegistry.IsAssigned(player, "townofroles.Snitch")) return "townofroles.Snitch";
            if (RoleRegistry.IsAssigned(player, "townofroles.Phantom")) return "townofroles.Phantom";
            if (RoleRegistry.IsAssigned(player, "townofroles.Glitch")) return "townofroles.Glitch";
            return null;
        }

        private static byte[] GetTaskTypeIds(PlayerControl player)
        {
            var ids = new List<byte>();
            if (player?.Data?.Tasks != null)
            {
                for (int i = 0; i < player.Data.Tasks.Count; i++)
                {
                    var task = player.Data.Tasks[i];
                    if (task == null) continue;

                    try { ids.Add((byte)task.Id); } catch { }
                }
            }
            return ids.ToArray();
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        private static DateTime GetCooldown(byte shifterId) =>
            Cooldowns.TryGetValue(shifterId, out var value) ? value : DateTime.MinValue;

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
