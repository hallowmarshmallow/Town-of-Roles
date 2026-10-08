using System.Collections.Generic;
using Atomic;
using MarshAPI;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Sheriff
{
    internal static class SheriffSystem
    {
        private const string KilledRpc = "townofroles.SheriffKilled";
        private const string RequestShootRpc = "townofroles.SheriffRequestShoot";

        private static readonly HashSet<(byte Victim, byte Killer)> KilledPlayers = new();
        private static readonly HashSet<byte> KilledBySheriff = new();

        public static bool IsSheriff(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, SheriffRole.Id);

        public static bool HasTarget(PlayerControl sheriff) =>
            sheriff != null && sheriff.Data != null && !sheriff.Data.IsDead &&
            ClosestPlayerFinder.GetClosestTarget(sheriff, out _);

        public static void TryShoot(PlayerControl sheriff)
        {
            if (sheriff == null || sheriff.Data == null || sheriff.Data.IsDead) return;
            var client = AmongUsClient.Instance;
            if (client == null) return;

            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestShootRpc, sheriff.PlayerId);
                return;
            }
            if (!ClosestPlayerFinder.GetClosestTarget(sheriff, out var target)) return;

            var team = target.Data?.myRole?.RoleTeamType;
            if (team == RoleTeamTypes.Impostor || (team == RoleTeamTypes.Neutral && IsKillableNeutral(target)))
            {
                PerformKill(sheriff, target);
                return;
            }

            if (team != RoleTeamTypes.Neutral && SheriffOptions.KillOther) PerformKill(sheriff, target);
            PerformKill(sheriff, sheriff);
        }

        private static bool IsKillableNeutral(PlayerControl target)
        {
            if (!SheriffOptions.KillsNeutrals) return false;

            if (RoleRegistry.IsAssigned(target, "townofroles.Jester")) return true;
            if (RoleRegistry.IsAssigned(target, "townofroles.Executioner")) return true;
            if (RoleRegistry.IsAssigned(target, "townofroles.Glitch")) return true;
            if (RoleRegistry.IsAssigned(target, "townofroles.Arsonist")) return true;
            return false;
        }

        private static void PerformKill(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null || killer.Data == null || target.Data == null) return;

            var victim = target.Data.PlayerId;
            var murderer = killer.Data.PlayerId;

            if (!KillManager.Kill(killer, target)) return;

            Record(victim, murderer);
            TownOfRolesRpcMux.Send(KilledRpc, victim, murderer);
        }

        [AtomicRpc(KilledRpc)]
        private static void OnKilled(byte senderId, byte victim, byte murderer)
        {
            var client = AmongUsClient.Instance;
            if (client == null || (!client.AmHost && senderId != client.HostId)) return;
            Record(victim, murderer);
        }

        [AtomicRpc(RequestShootRpc)]
        private static void OnRequestShoot(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId) TryShoot(player);
                return;
            }
        }

        private static void Record(byte victim, byte murderer)
        {
            KilledPlayers.Add((victim, murderer));
            if (murderer != victim) KilledBySheriff.Add(victim);
        }

        public static void OnBeforeReport(ReportEventArgs args)
        {
            if (SheriffOptions.BodyReport) return;
            if (args.IsEmergencyMeeting || args.Body == null || args.Reporter == null) return;
            if (!IsSheriff(args.Reporter)) return;

            if (KilledBySheriff.Contains(args.Body.PlayerId)) args.Cancelled = true;
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void OnGameEnded(GameEndedEventArgs _) => Reset();

        public static void Reset()
        {
            KilledPlayers.Clear();
            KilledBySheriff.Clear();
        }
    }
}
