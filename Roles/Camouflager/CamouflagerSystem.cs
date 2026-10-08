using System;
using System.Collections.Generic;
using Atomic;
using HarmonyLib;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Camouflager
{
    internal static class CamouflagerSystem
    {
        private const string StartRpc = "townofroles.CamouflageStart";
        private const string RequestRpc = "townofroles.CamouflageRequest";

        private static readonly Dictionary<byte, DateTime> Cooldowns = new();
        private static DateTime _camoUntil;
        private static bool _camoActive;

        public static bool IsCamouflager(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, CamouflagerRole.Id);

        internal static bool IsActive => _camoActive && DateTime.UtcNow < _camoUntil;

        internal static bool CanCamouflageNow(PlayerControl camouflager)
        {
            if (!IsCamouflager(camouflager) || camouflager.Data == null || camouflager.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(camouflager.PlayerId);
        }

        public static void TryCamouflage(PlayerControl camouflager)
        {
            var client = AmongUsClient.Instance;
            if (client == null || camouflager == null || camouflager.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestRpc, camouflager.PlayerId);
                return;
            }
            if (!CanCamouflageNow(camouflager)) return;

            var duration = RoleConfig.Seconds(RoleConfig.CamouflageDuration, 10f);
            Cooldowns[camouflager.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.CamouflageCooldown, 30f));
            _camoUntil = DateTime.UtcNow.AddSeconds(duration);
            _camoActive = true;
            ApplyCamo();
            TownOfRolesRpcMux.Send(StartRpc, duration);
        }

        public static void Tick()
        {
            if (!_camoActive) return;
            if (DateTime.UtcNow < _camoUntil)
            {
                ApplyCamo();
                return;
            }
            RestoreAll();
            _camoActive = false;
        }

        private static void ApplyCamo()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;

                try { if (player.nameText != null) player.nameText.text = string.Empty; } catch { }
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    try { PlayerMaterial.SetColors(Palette.DisabledGrey, renderer); } catch { }
                }

                HideOutfit(player);
            }
        }

        private static void RestoreAll()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                var colorId = player.Data.ColorId;

                try
                {
                    if (player.nameText != null && !string.IsNullOrEmpty(player.Data.PlayerName))
                        player.nameText.text = player.Data.PlayerName;
                }
                catch { }
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    try { PlayerControl.SetPlayerMaterialColors(colorId, renderer); } catch { }
                }

                RestoreOutfit(player);
            }
        }

        private static void HideOutfit(PlayerControl player)
        {
            try { player.HatRenderer?.SetEnabled(false); } catch { }

            try
            {
                var skinRend = player.XMLSkin?.SkinRend;
                if (skinRend != null) skinRend.enabled = false;
            }
            catch { }

            try { if (player.CurrentPet != null) player.CurrentPet.gameObject.SetActive(false); } catch { }
        }

        private static void RestoreOutfit(PlayerControl player)
        {
            try { player.HatRenderer?.SetEnabled(true); } catch { }
            try
            {
                var skinRend = player.XMLSkin?.SkinRend;
                if (skinRend != null) skinRend.enabled = true;
            }
            catch { }
            try { if (player.CurrentPet != null) player.CurrentPet.gameObject.SetActive(true); } catch { }
        }

        [HarmonyPatch(typeof(HatParent), nameof(HatParent.LateUpdate))]
        internal static class HatParent_LateUpdate_CamouflagePatch
        {
            private static bool Prefix() => !IsActive;
        }

        [AtomicRpc(RequestRpc)]
        private static void OnRequest(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryCamouflage(player);
                    return;
                }
            }
        }

        [AtomicRpc(StartRpc)]
        private static void OnStart(byte senderId, float duration)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            _camoUntil = DateTime.UtcNow.AddSeconds(duration);
            _camoActive = true;
            ApplyCamo();
        }

        public static float SecondsRemaining(PlayerControl camouflager)
        {
            if (camouflager == null || camouflager.Data == null) return 0f;
            var until = GetCooldown(camouflager.PlayerId);
            var left = (until - DateTime.UtcNow).TotalSeconds;
            return left > 0 ? (float)left : 0f;
        }

        public static float ActiveSecondsRemaining
        {
            get
            {
                if (!_camoActive) return 0f;
                var left = (_camoUntil - DateTime.UtcNow).TotalSeconds;
                return left > 0 ? (float)left : 0f;
            }
        }

        private static DateTime GetCooldown(byte playerId) =>
            Cooldowns.TryGetValue(playerId, out var value) ? value : DateTime.MinValue;

        public static void Reset()
        {
            if (_camoActive) RestoreAll();
            Cooldowns.Clear();
            _camoActive = false;
            _camoUntil = DateTime.MinValue;
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
        public static void OnMeetingStarted(MeetingEventArgs _) => Reset();

    }
}
