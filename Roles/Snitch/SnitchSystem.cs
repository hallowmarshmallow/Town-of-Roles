using System;
using System.Collections.Generic;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Assets;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Snitch
{
    internal static class SnitchSystem
    {
        private static readonly Dictionary<byte, ArrowBehaviour> Arrows = new();

        private static readonly HashSet<byte> NameHighlighted = new();

        public static bool IsSnitch(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, SnitchRole.Id);

        public static bool TasksComplete(PlayerControl player)
        {
            if (player == null || player.Data == null || player.Data.Tasks == null) return false;

            var tasks = player.Data.Tasks;
            if (tasks.Count == 0) return false;
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i] == null || !tasks[i].Complete) return false;
            return true;
        }

        public static void Tick()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null) { ClearAll(); return; }
            if (!IsSnitch(local) || local.Data.IsDead) { ClearAll(); return; }
            if (MeetingHud.Instance != null || ExileController.Instance != null) { ClearAll(); return; }
            if (!TasksComplete(local)) { ClearAll(); return; }

            UpdateArrows(local);
        }

        private static void UpdateArrows(PlayerControl snitch)
        {
            var targets = new HashSet<byte>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                if (player.Data.myRole == null || player.Data.myRole.RoleTeamType != RoleTeamTypes.Impostor) continue;
                targets.Add(player.PlayerId);
                if (Arrows.TryGetValue(player.PlayerId, out var arrow) && arrow != null)
                {
                    arrow.target = player.transform.position;

                    if (arrow.image != null) SizeArrow(arrow.transform, arrow.image.sprite);
                    continue;
                }
                CreateArrow(player);
            }

            if (Arrows.Count > 0)
            {
                var stale = new List<byte>();
                foreach (var id in Arrows.Keys)
                    if (!targets.Contains(id)) stale.Add(id);
                for (int i = 0; i < stale.Count; i++) DestroyArrow(stale[i]);
            }

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                bool isTarget = !Camouflager.CamouflagerSystem.IsActive && targets.Contains(player.PlayerId);
                try
                {
                    if (player.nameText == null) continue;
                    player.nameText.color = isTarget ? Palette.ImpostorRed : Color.white;
                    if (isTarget) NameHighlighted.Add(player.PlayerId);
                }
                catch { }
            }
        }

        private const float ScreenHeightShare = 0.05f;

        private static void CreateArrow(PlayerControl player)
        {
            try
            {
                var icon = RoleArt.Arrow;
                if (icon == null) return;

                var go = new GameObject("ToU_SnitchArrow_" + player.PlayerId);
                var arrow = go.AddComponent<ArrowBehaviour>();

                var sprite = new GameObject("Sprite");
                sprite.transform.SetParent(go.transform, false);
                var sr = sprite.AddComponent<SpriteRenderer>();
                sr.sprite = icon;
                sr.sortingOrder = 200;

                arrow.image = sr;
                arrow.target = player.transform.position;
                SizeArrow(go.transform, icon);
                Arrows[player.PlayerId] = arrow;
            }
            catch { }
        }

        private static void SizeArrow(Transform root, Sprite icon)
        {
            if (root == null || icon == null || root.childCount == 0) return;

            var child = root.GetChild(0);
            if (child == null) return;

            var camera = Camera.main;
            float screenHeight = camera != null ? camera.orthographicSize * 2f : 6f;
            float wanted = screenHeight * ScreenHeightShare;

            float spriteHeight = icon.rect.height / Mathf.Max(icon.pixelsPerUnit, 0.0001f);
            if (spriteHeight <= 0.0001f) return;

            child.localScale = Vector3.one * (wanted / spriteHeight);
        }

        private static void DestroyArrow(byte playerId)
        {
            if (!Arrows.TryGetValue(playerId, out var arrow)) return;
            Arrows.Remove(playerId);
            if (arrow != null && arrow.gameObject != null && arrow.gameObject)
                UnityEngine.Object.Destroy(arrow.gameObject);
        }

        private static void ClearAll()
        {
            if (NameHighlighted.Count > 0)
            {
                foreach (var id in new List<byte>(NameHighlighted))
                {
                    var player = FindPlayerLocal(id);
                    if (player != null && player.nameText != null)
                    {
                        try { player.nameText.color = Color.white; } catch { }
                    }
                }
                NameHighlighted.Clear();
            }
            if (Arrows.Count == 0) return;
            foreach (var id in new List<byte>(Arrows.Keys)) DestroyArrow(id);
        }

        private static PlayerControl FindPlayerLocal(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        public static void Reset() => ClearAll();
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
        public static void OnMeetingStarted(MeetingEventArgs _) => Reset();
    }
}
