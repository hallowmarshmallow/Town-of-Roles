using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Atomic;
using MarshAPI;
using HarmonyLib;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class CreatorColor
    {
        private const string CreatorClaimRpc = "townofroles.CreatorClaim";
        private const float ClaimIntervalSeconds = 15f;

        private static readonly Color Blue = new(0.30f, 0.62f, 1f, 1f);
        private static readonly Color Pink = new(1f, 0.45f, 0.62f, 1f);

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Name { get; private set; }
        public static ConfigEntry<string> Secret { get; private set; }
        public static ConfigEntry<float> Speed { get; private set; }

        private static int _claimedPlayerId = -1;
        private static DateTime _nextClaim = DateTime.MinValue;

        private static float _settleUntil = float.MinValue;

        private static readonly HashSet<byte> TintedBodies = new();

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                "CreatorColor", "Enabled", true,
                "Give the mod creator's in-game name a smooth cycling blue/pink color.");
            Name = config.Bind(
                "CreatorColor", "Name", "hallowmarsh",
                "Legacy (no-secret) name match. When Secret is empty, players with this name are colored — spoofable by renaming. Set Secret to turn on the verified handshake instead.");
            Secret = config.Bind(
                "CreatorColor", "Secret", "",
                "Handshake secret. When set, this client claims creator status (broadcast every 15s) and only colors players whose claim matches THIS secret. Share it only with trusted lobbies.");
            Speed = config.Bind(
                "CreatorColor", "Speed", 2.5f,
                "Cycling speed in radians per second (higher = faster blue/pink cycle).");
        }

        public static void Reset()
        {
            RestoreTintedBodies();
            _claimedPlayerId = -1;
            _nextClaim = DateTime.MinValue;
        }

        public static void OnGameStarted(GameStartedEventArgs _)
        {
            _settleUntil = Time.unscaledTime + 1.5f;
        }

        public static void OnGameEnded(GameEndedEventArgs _)
        {
            RestoreTintedBodies();
            _claimedPlayerId = -1;
        }

        [AtomicRpc(CreatorClaimRpc)]
        private static void OnCreatorClaimRpc(byte senderId, string secret)
        {
            var expected = Secret?.Value;
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(secret)) return;
            if (!string.Equals(expected, secret, StringComparison.Ordinal)) return;

            // senderId is a client id, and every other use of this field compares it against
            // a player id, so storing it raw tinted the wrong player, or nobody.
            var owner = FindByClientId(senderId);
            if (owner == null || owner.Data == null) return;
            _claimedPlayerId = owner.PlayerId;
        }

        // The player a client id belongs to, or null while that client has no player yet. The
        // claim is broadcast every 15s, so a miss here is retried rather than lost.
        private static PlayerControl FindByClientId(byte clientId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                var client = player.GetClient();
                if (client != null && client.Id == clientId) return player;
            }

            return null;
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]

        [HarmonyPriority(Priority.Last)]
        internal static class HudManager_Update_CreatorColorPatch
        {
            private static void Postfix()
            {
                if (Enabled?.Value != true)
                {
                    RestoreTintedBodies();
                    return;
                }
                try
                {
                    if (Time.unscaledTime < _settleUntil) return;
                    var speed = Mathf.Max(0.1f, Speed?.Value ?? 2.5f);
                    var t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) / 2f;
                    var color = Color.Lerp(Blue, Pink, t);

                    if (PlayerControl.LocalPlayer != null && PlayerControl.AllPlayerControls.Count > 0)
                        MaybeClaim();

                    PruneClaim();
                    ApplyColor(color);
                }
                catch
                {
                }
            }
        }

        private static void MaybeClaim()
        {
            var secret = Secret?.Value;
            if (string.IsNullOrEmpty(secret)) return;

            var now = DateTime.UtcNow;
            if (now < _nextClaim) return;
            _nextClaim = now.AddSeconds(ClaimIntervalSeconds);
            try
            {
                TownOfRolesRpcMux.Send(CreatorClaimRpc, secret);
            }
            catch
            {
            }
        }

        private static void PruneClaim()
        {
            if (_claimedPlayerId < 0) return;
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == _claimedPlayerId) return;
            _claimedPlayerId = -1;
        }

        private static void ApplyColor(Color color)
        {
            var secret = Secret?.Value;
            var legacyName = (Name?.Value ?? string.Empty).Trim();
            var localId = PlayerControl.LocalPlayer?.PlayerId ?? -1;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.nameText == null) continue;
                if (!ShouldColor(player.PlayerId, player.Data.PlayerName, localId, secret, legacyName)) continue;
                SetColor(player.nameText, color);
                TintBody(player, color);
            }

            var meeting = MeetingHud.Instance;

            var states = meeting == null ? null : GameReflection.GetPlayerStates(meeting);
            if (states != null)
            {
                foreach (var area in states)
                {
                    if (area == null || area.NameText == null) continue;
                    var target = FindPlayer(area.TargetPlayerId);
                    if (target == null || target.Data == null) continue;
                    if (!ShouldColor(target.PlayerId, target.Data.PlayerName, localId, secret, legacyName)) continue;
                    SetColor(area.NameText, color);
                }
            }

            RestoreUntinted();
        }

        private static void TintBody(PlayerControl player, Color color)
        {
            if (player == null || player.gameObject == null) return;
            if (player.Data != null && player.Data.IsDead) return;
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                try { PlayerMaterial.SetColors(color, renderer); } catch { }
            }
            TintedBodies.Add(player.PlayerId);
        }

        private static void RestoreUntinted()
        {
            if (TintedBodies.Count == 0) return;
            var secret = Secret?.Value;
            var legacyName = (Name?.Value ?? string.Empty).Trim();
            var localId = PlayerControl.LocalPlayer?.PlayerId ?? -1;

            foreach (var id in new List<byte>(TintedBodies))
            {
                var player = FindPlayer(id);
                if (player == null || player.Data == null) { TintedBodies.Remove(id); continue; }

                if (player.Data.IsDead) { RestoreBody(player); TintedBodies.Remove(id); continue; }
                if (ShouldColor(id, player.Data.PlayerName, localId, secret, legacyName)) continue;
                RestoreBody(player);
                TintedBodies.Remove(id);
            }
        }

        private static void RestoreTintedBodies()
        {
            if (TintedBodies.Count == 0) return;
            foreach (var id in new List<byte>(TintedBodies))
            {
                var player = FindPlayer(id);
                if (player != null) RestoreBody(player);
            }
            TintedBodies.Clear();
        }

        private static void RestoreBody(PlayerControl player)
        {
            if (player == null || player.Data == null || player.gameObject == null) return;
            var colorId = player.Data.ColorId;
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                try { PlayerControl.SetPlayerMaterialColors(colorId, renderer); } catch { }
            }
        }

        private static bool ShouldColor(int playerId, string playerName, int localId, string secret, string legacyName)
        {
            var isSelf = playerId == localId;
            if (isSelf)
            {
                return !string.IsNullOrEmpty(secret)
                    || (legacyName.Length > 0
                        && string.Equals(playerName, legacyName, StringComparison.OrdinalIgnoreCase));
            }

            if (playerId == _claimedPlayerId && !string.IsNullOrEmpty(secret)) return true;
            return string.IsNullOrEmpty(secret)
                && legacyName.Length > 0
                && string.Equals(playerName, legacyName, StringComparison.OrdinalIgnoreCase);
        }

        private static PlayerControl FindPlayer(byte id)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == id) return player;
            return null;
        }

        private static void SetColor(object renderer, Color value)
        {
            if (renderer == null) return;
            var type = renderer.GetType();
            var property = type.GetProperty("color") ?? type.GetProperty("Color");
            if (property != null && property.CanWrite)
            {
                try { property.SetValue(renderer, value, null); } catch { }
            }
        }
    }
}
