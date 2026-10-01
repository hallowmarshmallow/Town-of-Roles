using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using HarmonyLib;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Camouflager
{
    // Camouflager gameplay logic (ported from Town-Of-Us' Camouflager.cs).
    internal static class CamouflagerSystem
    {
        private const string StartRpc = "townofroles.CamouflageStart";
        private const string RequestRpc = "townofroles.CamouflageRequest";

        private static readonly Dictionary<byte, DateTime> Cooldowns = new();
        private static DateTime _camoUntil;
        private static bool _camoActive;

        public static bool IsCamouflager(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, CamouflagerRole.Id);

        // True while the round-wide camouflage is live. Presentation layers (role name lines,
        // Snitch red plates) must check this before writing anything into nameText, they run at
        // 10 Hz and would otherwise overwrite the blanked names within a frame.
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

        // Runs every frame on every client: keep the grey while active, restore once on expiry.
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
                // Blank the overhead name too (Town-Of-Us Utils.Camouflage sets
                // nameText.text = ""), grey bodies alone don't hide identities.
                try { if (player.nameText != null) player.nameText.text = string.Empty; } catch { }
                foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    try { PlayerMaterial.SetColors(Palette.DisabledGrey, renderer); } catch { }
                }

                // The grey wash only recolors, a red hat is a grey hat, and still a
                // silhouette that identifies the wearer. Strip the cosmetics that
                // carry identity through the game's own outfit setters, and restore
                // from PlayerInfo afterwards (HatId/SkinId/PetId are the authoritative
                // current values, so a Morphling mid-morph reverts to their morph).
                HideOutfit(player);
            }
        }

        private static void RestoreAll()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                var colorId = player.Data.ColorId;
                // Restore the overhead name from GameData (also undoes a blanked
                // name; Morphling-style RpcSetName changes live in the same field,
                // so Data.PlayerName is always the authoritative current value).
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

        // Renders every player's hat, skin and pet invisible for the camouflage.
        private static void HideOutfit(PlayerControl player)
        {
            // Hat: HatParent.SetEnabled toggles its renderers (the game's own switch).
            try { player.HatRenderer?.SetEnabled(false); } catch { }

            // Skin: XMLSkinAnimator rides PlayerControl.XMLSkin and draws the outfit
            // sprite (SkinRend). Hiding the renderer is the visual-only equivalent of
            // the game's own "no skin" state.
            try
            {
                var skinRend = player.XMLSkin?.SkinRend;
                if (skinRend != null) skinRend.enabled = false;
            }
            catch { }

            // Pet: a whole GameObject with its own animator; deactivate it outright.
            try { if (player.CurrentPet != null) player.CurrentPet.gameObject.SetActive(false); } catch { }
        }

        // Undoes HideOutfit from what PlayerInfo still holds.
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

        // While the camouflage is live, keep the game's hat updater from re-enabling the hat.
        [HarmonyPatch(typeof(HatParent), nameof(HatParent.LateUpdate))]
        internal static class HatParent_LateUpdate_CamouflagePatch
        {
            private static bool Prefix() => !IsActive;
        }

        [ReactorRpc(RequestRpc)]
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

        [ReactorRpc(StartRpc)]
        private static void OnStart(byte senderId, float duration)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            _camoUntil = DateTime.UtcNow.AddSeconds(duration);
            _camoActive = true;
            ApplyCamo();
        }

        // Seconds until this Camouflager may camouflage again, zero when ready.
        public static float SecondsRemaining(PlayerControl camouflager)
        {
            if (camouflager == null || camouflager.Data == null) return 0f;
            var until = GetCooldown(camouflager.PlayerId);
            var left = (until - DateTime.UtcNow).TotalSeconds;
            return left > 0 ? (float)left : 0f;
        }

        // Seconds the running camouflage still lasts, zero when it is not up.
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
            // Never leave everyone grey: restore each player's own colors before
            // clearing state (meeting start / round end while camo is active).
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
