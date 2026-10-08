using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Core;
using TownOfRoles.Roles.Engineer;

namespace TownOfRoles.Roles.Lovers
{
    internal static class LoverSystem
    {
        private const string PairsRpc = "townofroles.LoversPairs";
        private const string EndRpc = "townofroles.LoversEnd";
        private const int MaxRetries = 300;

        private const float SettleSeconds = 2.5f;

        private static readonly Dictionary<byte, byte> Partners = new();

        private static readonly HashSet<byte> Heartbroken = new();

        private static bool _impostorLover;
        private static bool _assigned;
        private static int _attempts;
        private static float _settleUntil = float.MinValue;
        private static bool _loggedAssignment;

        public static bool Enabled => RoleConfig.Lovers?.Value == true;

        public static bool HasPair => Partners.Count > 0;

        public static bool IsLover(PlayerControl player) =>
            player != null && player.Data != null && Partners.ContainsKey(player.Data.PlayerId);

        public static bool ArePartners(PlayerControl a, PlayerControl b) =>
            a != null && b != null && a.Data != null && b.Data != null && a.PlayerId != b.PlayerId &&
            Partners.TryGetValue(a.Data.PlayerId, out var partnerId) && partnerId == b.PlayerId;

        public static PlayerControl PartnerOf(PlayerControl player) =>
            player == null || player.Data == null ? null : FindPlayer(Partners.TryGetValue(player.Data.PlayerId, out var id) ? id : (byte?)null);

        public static void OnGameStarted(GameStartedEventArgs _)
        {
            Reset();
            _settleUntil = Time.unscaledTime + SettleSeconds;
        }

        public static void OnGameEnded(GameEndedEventArgs _) => Reset();

        public static void Reset()
        {
            Partners.Clear();
            Heartbroken.Clear();
            _impostorLover = false;
            _assigned = false;
            _attempts = 0;
            _settleUntil = float.MinValue;
            _loggedAssignment = false;
        }

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            if (Enabled && !_assigned && _attempts < MaxRetries && Time.unscaledTime >= _settleUntil)
            {
                _attempts++;
                TryAssignPairs();
            }

            if (RoleConfig.LoversBothDie?.Value != false) EnforceDiesTogether();
        }

        private static void TryAssignPairs()
        {
            if (RoleManager.Instance == null) return;
            if (PlayerControl.AllPlayerControls == null || PlayerControl.AllPlayerControls.Count == 0) return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (player.Data.myRole == null) return;
            }

            var wanted = Mathf.Clamp(RoleConfig.Count(RoleConfig.LoversCount, 1), 0, 8);
            if (wanted <= 0) { _assigned = true; return; }
            if (UnityEngine.Random.Range(0f, 100f) >= RoleConfig.Chance(RoleConfig.LoversChance)) { _assigned = true; return; }

            var crewmates = new List<PlayerControl>();
            var impostors = new List<PlayerControl>();
            var spareCrewmates = new List<PlayerControl>();
            var spareImpostors = new List<PlayerControl>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.Disconnected || player.Data.IsDead) continue;
                if (RoleRegistry.HasOverlay(player, LoverRole.Id)) continue;

                var team = player.Data.myRole == null ? (RoleTeamTypes?)null : player.Data.myRole.RoleTeamType;

                var claimed = RoleRegistry.HasAnyCustomRole(player);
                if (team == RoleTeamTypes.Impostor)
                {
                    if (claimed) spareImpostors.Add(player); else impostors.Add(player);
                }
                else if (team == RoleTeamTypes.Crewmate)
                {
                    if (claimed) spareCrewmates.Add(player); else crewmates.Add(player);
                }
            }

            if (crewmates.Count < 2)
            {
                crewmates.AddRange(spareCrewmates);
                impostors.AddRange(spareImpostors);
            }

            if (crewmates.Count < 2) { _assigned = true; return; }

            Shuffle(crewmates);
            Shuffle(impostors);

            var allowImpostor = RoleConfig.LoversImpostorLover?.Value != false && impostors.Count > 0;
            var nextImpostor = 0;
            var impostorInvolved = false;
            var assigned = 0;

            for (int i = 0; i < wanted; i++)
            {
                if (crewmates.Count < 2) break;

                var first = crewmates[0];
                crewmates.RemoveAt(0);

                PlayerControl second;
                if (allowImpostor && nextImpostor < impostors.Count)
                {
                    second = impostors[nextImpostor++];
                    impostorInvolved = true;
                }
                else
                {
                    second = crewmates[0];
                    crewmates.RemoveAt(0);
                }

                if (first == null || second == null || first.PlayerId == second.PlayerId) continue;
                AssignPair(first, second);
                assigned++;
            }

            _assigned = true;
            if (assigned == 0) return;

            _impostorLover = impostorInvolved;
            TownOfRolesRpcMux.Send(PairsRpc, BuildPayload(), impostorInvolved);

            if (!_loggedAssignment)
            {
                _loggedAssignment = true;
                Log("Lovers: picked " + assigned + " pair(s)" + (impostorInvolved ? " (one Impostor lover)" : string.Empty) + ".");
            }
        }

        private static void AssignPair(PlayerControl first, PlayerControl second)
        {
            if (first == null || second == null || RoleManager.Instance == null) return;
            RoleManager.Instance.AssignRole(first, LoverRole.Id);
            RoleManager.Instance.AssignRole(second, LoverRole.Id);
            Partners[first.PlayerId] = second.PlayerId;
            Partners[second.PlayerId] = first.PlayerId;
            NotifyLocal(first, second);
            NotifyLocal(second, first);
        }

        private static void NotifyLocal(PlayerControl self, PlayerControl partner)
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || self == null || partner == null || local.PlayerId != self.PlayerId) return;
            if (partner.Data == null) return;
            try { SystemChat.Show("You are in love with " + partner.Data.PlayerName + " \u2014 stay alive together!"); }
            catch { }
        }

        [AtomicRpc(PairsRpc)]
        private static void OnPairsRpc(byte senderId, string payload, bool impostorInvolved)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            if (!ParsePayload(payload)) return;

            _impostorLover = impostorInvolved;
            _assigned = true;

            foreach (var entry in Partners)
            {
                var player = FindPlayer(entry.Key);
                if (player == null || RoleManager.Instance == null) continue;
                RoleManager.Instance.AssignRole(player, LoverRole.Id);
                NotifyLocal(player, FindPlayer(entry.Value));
            }
        }

        private static string BuildPayload()
        {
            var builder = new StringBuilder();
            var seen = new HashSet<byte>();
            foreach (var entry in Partners)
            {
                if (!seen.Add(entry.Key)) continue;
                seen.Add(entry.Value);
                if (builder.Length > 0) builder.Append(';');
                builder.Append(entry.Key.ToString(CultureInfo.InvariantCulture))
                       .Append(',')
                       .Append(entry.Value.ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static bool ParsePayload(string payload)
        {
            Partners.Clear();
            if (string.IsNullOrEmpty(payload)) return false;

            foreach (var group in payload.Split(';'))
            {
                if (group.Length == 0) continue;
                var parts = group.Split(',');
                if (parts.Length != 2) continue;
                if (!byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var first)) continue;
                if (!byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var second)) continue;
                if (first == second) continue;
                Partners[first] = second;
                Partners[second] = first;
            }

            return Partners.Count > 0;
        }

        private static void EnforceDiesTogether()
        {
            if (Partners.Count == 0) return;

            foreach (var entry in Partners)
            {
                var lover = FindPlayer(entry.Key);
                var partner = FindPlayer(entry.Value);
                if (lover == null || partner == null || lover.Data == null || partner.Data == null) continue;

                if (lover.Data.IsDead && !partner.Data.IsDead) { Heartbreak(partner); return; }
                if (partner.Data.IsDead && !lover.Data.IsDead) { Heartbreak(lover); return; }
            }
        }

        private static void Heartbreak(PlayerControl victim)
        {
            if (victim == null || victim.Data == null) return;
            if (!Heartbroken.Add(victim.PlayerId)) return;
            try
            {
                KillManager.Kill(victim, victim, new KillRequest { TeleportKiller = false });
                Log("Lovers: " + victim.Data.PlayerName + " died of heartbreak.");
            }
            catch (Exception e) { Log("heartbreak: " + e.Message); }
        }

        public static bool TryHandleEndCriteria()
        {
            if (Partners.Count == 0) return false;
            var ship = ShipStatus.Instance;
            if (ship == null) return false;

            try { if (EngineerAbility.IsSabotageActive()) return false; }
            catch { }

            var alive = AlivePlayers();
            if (alive.Count == 0) return false;

            var loversAlive = 0;
            foreach (var entry in Partners)
            {
                var lover = FindPlayer(entry.Key);
                if (lover != null && lover.Data != null && !lover.Data.IsDead) loversAlive++;
            }

            var allLoversAlive = loversAlive == Partners.Count;
            if (allLoversAlive)
            {
                if (_impostorLover && alive.Count == Partners.Count + 2) return true;

                if (alive.Count <= Partners.Count + 1)
                {
                    DeclareEnd(WinKind.Lovers);
                    return true;
                }
            }

            if (alive.Count == 2 && !IsLover(alive[0]) && !IsLover(alive[1]) &&
                IsNeutralCustomRole(alive[0]) && IsNeutralCustomRole(alive[1]))
            {
                DeclareEnd(WinKind.Nobody);
                return true;
            }

            return false;
        }

        private enum WinKind { Lovers, Nobody }

        private static void DeclareEnd(WinKind kind)
        {
            if (ModdedGameOver.HasClaim) return;
            ClaimEnd(kind);

            TownOfRolesRpcMux.Send(EndRpc, kind == WinKind.Nobody ? "nobody" : "lovers");

            var ship = ShipStatus.Instance;
            if (ship != null)
            {
                try { ship.StartEndGame(GameOverReason.Custom, 0.5f); }
                catch (Exception e) { Log("end game: " + e.Message); }
            }

            Log(kind == WinKind.Nobody
                ? "Lovers: only neutral roles were left - nobody wins."
                : "Lovers: the love couple wins.");
        }

        private static void ClaimEnd(WinKind kind)
        {
            if (kind == WinKind.Nobody)
            {
                ModdedGameOver.Claim("Only neutral roles were left", Color.white);
                return;
            }
            ModdedGameOver.Claim("Love Couple Wins", LoverRole.LoverColor);
        }

        [AtomicRpc(EndRpc)]
        private static void OnEndRpc(byte senderId, string kind)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            ClaimEnd(string.Equals(kind, "nobody", StringComparison.Ordinal) ? WinKind.Nobody : WinKind.Lovers);
        }

        private static List<PlayerControl> AlivePlayers()
        {
            var alive = new List<PlayerControl>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (player.Data.Disconnected || player.Data.IsDead) continue;
                alive.Add(player);
            }
            return alive;
        }

        private static bool IsNeutralCustomRole(PlayerControl player)
        {
            var roleId = RoleRegistry.GetAssignedRoleTypeName(player);
            if (string.IsNullOrEmpty(roleId)) return false;
            foreach (var def in RoleCatalog.All)
                if (def.Id == roleId) return def.Team == RoleTeamTypes.Neutral;
            return false;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static PlayerControl FindPlayer(byte? playerId)
        {
            if (!playerId.HasValue) return null;
            return FindPlayer(playerId.Value);
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogInfo(message);
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    [HarmonyPriority(Priority.Last)]
    internal static class ShipStatus_CheckEndCriteria_LoversPatch
    {
        private static bool Prefix()
        {
            try
            {
                var client = AmongUsClient.Instance;
                if (client == null || !client.AmHost) return true;
                return !LoverSystem.TryHandleEndCriteria();
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Lovers end criteria: " + e);
                return true;
            }
        }
    }
}
