using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomic;
using MarshAPI;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Assassin
{
    internal static class AssassinSystem
    {
        private const string RequestGuessRpc = "townofroles.AssassinRequestGuess";
        private const string GuessResultRpc = "townofroles.AssassinGuessResult";
        private static readonly HashSet<byte> _guessedThisMeeting = new();
        internal static readonly string[] GuessableRoles =
        {
            "Sheriff", "Engineer", "Medic", "Seer", "Vigilante", "Altruist", "Mayor", "Swapper", "Spy",
            "Assassin", "Janitor", "Morphling", "Camouflager", "Swooper", "Underdog", "Undertaker",
            "Investigator", "Time Lord", "Snitch", "Phantom",
            "Shifter", "The Glitch", "Miner",
            "Jester", "Executioner", "Arsonist", "Crewmate", "Neutral"
        };

        public static bool IsAssassin(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, AssassinRole.Id);

        public static string AvailableRoles => string.Join(", ", GuessableRoles);

        public static void Reset()
        {
            _guessedThisMeeting.Clear();
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
        public static void OnMeetingStarted(MeetingEventArgs _) => _guessedThisMeeting.Clear();
        public static void OnMeetingEnded(MeetingEventArgs _) => _guessedThisMeeting.Clear();

        public static bool TryHandleGuess(PlayerControl sender, string[] args)
        {
            if (!IsAssassin(sender)) return false;
            if (MeetingHud.Instance == null)
            {
                Local("Assassin guesses can only be used during a meeting.");
                return true;
            }
            if (args == null || args.Length < 2)
            {
                Local("Usage: /guess <player name or id> <role>");
                Local("Guessable roles: " + AvailableRoles);
                return true;
            }

            var target = ResolveTarget(args.Take(args.Length - 1).ToArray());
            var guess = CanonicalizeRole(args[args.Length - 1]);
            if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected)
            {
                Local("That player is not a valid guess target.");
                return true;
            }
            if (target == sender || (target.Data.myRole != null && target.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor))
            {
                Local("You cannot guess yourself or another Impostor.");
                return true;
            }
            if (guess == null)
            {
                Local("Unknown role. Guessable roles: " + AvailableRoles);
                return true;
            }
            if (_guessedThisMeeting.Contains(target.PlayerId))
            {
                Local("That player has already been guessed this meeting.");
                return true;
            }

            var client = AmongUsClient.Instance;
            if (client == null) return true;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestGuessRpc, sender.PlayerId, target.PlayerId, guess);
                Local("Guess sent to the host.");
                return true;
            }

            ResolveGuess(sender, target, guess);
            return true;
        }

        public static bool TryGuessTarget(PlayerControl assassin, PlayerControl target, string guess)
        {
            if (!IsAssassin(assassin) || assassin.Data.IsDead || MeetingHud.Instance == null || target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected) return false;
            if (target == assassin || (target.Data.myRole != null && target.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor)) return false;
            var canonical = CanonicalizeRole(guess);
            if (canonical == null || _guessedThisMeeting.Contains(target.PlayerId)) return false;
            var client = AmongUsClient.Instance;
            if (client == null) return false;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestGuessRpc, assassin.PlayerId, target.PlayerId, canonical);
                return true;
            }
            ResolveGuess(assassin, target, canonical);
            return true;
        }

        public static bool IsEligibleTarget(PlayerControl assassin, PlayerControl target)
        {
            return IsAssassin(assassin) && target != null && target != assassin && target.Data != null &&
                   !target.Data.IsDead && !target.Data.Disconnected &&
                   target.Data.myRole != null && target.Data.myRole.RoleTeamType != RoleTeamTypes.Impostor;
        }

        private static void ResolveGuess(PlayerControl assassin, PlayerControl target, string guess)
        {
            if (assassin == null || assassin.Data == null || assassin.Data.IsDead || target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected || !IsAssassin(assassin)) return;

            var meeting = MeetingHud.Instance;
            var voteState = GameReflection.GetMeetingState(meeting);
            if (meeting == null || voteState == MeetingHud.VoteStates.Discussion || voteState == MeetingHud.VoteStates.Results ||
                target.Data.myRole == null || target.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor ||
                _guessedThisMeeting.Contains(target.PlayerId) ||
                (!AssassinSettingsSync.ActiveMultiKill && _guessedThisMeeting.Contains(assassin.PlayerId))) return;
            _guessedThisMeeting.Add(target.PlayerId);

            var actual = GetRoleName(target);
            bool correct = string.Equals(actual, guess, StringComparison.OrdinalIgnoreCase);
            if (correct && !AssassinSettingsSync.ActiveMultiKill) _guessedThisMeeting.Add(assassin.PlayerId);
            var victim = correct ? target : assassin;
            KillManager.Kill(assassin, victim);
            TownOfRolesRpcMux.Send(GuessResultRpc, assassin.PlayerId, victim.PlayerId, correct, (byte)target.PlayerId);
            Local(correct
                ? $"Assassin guessed {target.Data.PlayerName} correctly: {actual}."
                : $"Wrong guess. The Assassin guessed {guess}; actual role was {actual}.");
        }

        [AtomicRpc(RequestGuessRpc)]
        private static void OnRequestGuess(byte senderId, byte assassinId, byte targetId, string guess)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost || senderId != GetClientId(assassinId)) return;
            var assassin = FindPlayer(assassinId);
            var target = FindPlayer(targetId);
            var canonical = CanonicalizeRole(guess);
            if (assassin != null && target != null && canonical != null)
                TryGuessTarget(assassin, target, canonical);
        }

        [AtomicRpc(GuessResultRpc)]
        private static void OnGuessResult(byte senderId, byte assassinId, byte victimId, bool correct, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            _guessedThisMeeting.Add(targetId);
        }

        private static string GetRoleName(PlayerControl player)
        {
            if (RoleRegistry.IsAssigned(player, "townofroles.Sheriff")) return "Sheriff";
            if (RoleRegistry.IsAssigned(player, "townofroles.Engineer")) return "Engineer";
            if (RoleRegistry.IsAssigned(player, "townofroles.Jester")) return "Jester";
            if (RoleRegistry.IsAssigned(player, "townofroles.Medic")) return "Medic";
            if (RoleRegistry.IsAssigned(player, "townofroles.Seer")) return "Seer";
            if (RoleRegistry.IsAssigned(player, "townofroles.Vigilante")) return "Vigilante";
            if (RoleRegistry.IsAssigned(player, "townofroles.Assassin")) return "Assassin";
            if (RoleRegistry.IsAssigned(player, "townofroles.Janitor")) return "Janitor";
            if (RoleRegistry.IsAssigned(player, "townofroles.Altruist")) return "Altruist";
            if (RoleRegistry.IsAssigned(player, "townofroles.Mayor")) return "Mayor";
            if (RoleRegistry.IsAssigned(player, "townofroles.Executioner")) return "Executioner";
            if (RoleRegistry.IsAssigned(player, "townofroles.Arsonist")) return "Arsonist";
            if (RoleRegistry.IsAssigned(player, "townofroles.Swapper")) return "Swapper";
            if (RoleRegistry.IsAssigned(player, "townofroles.Morphling")) return "Morphling";
            if (RoleRegistry.IsAssigned(player, "townofroles.Spy")) return "Spy";
            if (RoleRegistry.IsAssigned(player, "townofroles.Camouflager")) return "Camouflager";
            if (RoleRegistry.IsAssigned(player, "townofroles.Swooper")) return "Swooper";
            if (RoleRegistry.IsAssigned(player, "townofroles.Underdog")) return "Underdog";
            if (RoleRegistry.IsAssigned(player, "townofroles.Undertaker")) return "Undertaker";
            if (RoleRegistry.IsAssigned(player, "townofroles.Investigator")) return "Investigator";
            if (RoleRegistry.IsAssigned(player, "townofroles.TimeLord")) return "Time Lord";
            if (RoleRegistry.IsAssigned(player, "townofroles.Snitch")) return "Snitch";
            if (RoleRegistry.IsAssigned(player, "townofroles.Phantom")) return "Phantom";
            if (RoleRegistry.IsAssigned(player, "townofroles.Shifter")) return "Shifter";
            if (RoleRegistry.IsAssigned(player, "townofroles.Glitch")) return "The Glitch";
            if (RoleRegistry.IsAssigned(player, "townofroles.Miner")) return "Miner";
            if (player?.Data?.myRole == null) return "Unknown";
            if (player.Data.myRole.RoleTeamType == RoleTeamTypes.Impostor) return "Impostor";
            if (player.Data.myRole.RoleTeamType == RoleTeamTypes.Neutral) return "Neutral";
            return "Crewmate";
        }

        private static string CanonicalizeRole(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var compact = string.Concat(value.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            foreach (var role in GuessableRoles)
            {
                var roleCompact = string.Concat(role.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
                if (roleCompact == compact) return role;
            }
            return null;
        }

        private static PlayerControl ResolveTarget(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            var query = string.Join(" ", args).Trim();
            if (byte.TryParse(query, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) return FindPlayer(id);
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player?.Data != null && string.Equals(player.Data.PlayerName, query, StringComparison.OrdinalIgnoreCase)) return player;
            return null;
        }

        private static PlayerControl FindPlayer(byte id)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == id) return player;
            return null;
        }

        private static byte GetClientId(byte playerId)
        {
            var player = FindPlayer(playerId);
            return player == null || player.GetClient() == null ? (byte)255 : (byte)player.GetClient().Id;
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
}
