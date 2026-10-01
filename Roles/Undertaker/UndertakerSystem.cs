using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Undertaker
{
    // Undertaker gameplay logic (ported from Town-Of-Us' Undertaker.cs).
    internal static class UndertakerSystem
    {
        private const string DragRpc = "townofroles.UndertakerDrag";
        private const string DropRpc = "townofroles.UndertakerDrop";
        private const string RequestDragRpc = "townofroles.UndertakerRequestDrag";

        private static readonly Dictionary<byte, byte> Dragged = new(); // draggerId -> bodyParentId (all clients)
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();

        public static bool IsUndertaker(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, UndertakerRole.Id);

        public static bool IsDragging(byte playerId) => Dragged.ContainsKey(playerId);

        internal static bool CanDragNow(PlayerControl undertaker)
        {
            if (!IsUndertaker(undertaker) || undertaker.Data == null || undertaker.Data.IsDead) return false;
            if (IsDragging(undertaker.PlayerId)) return true; // pressing again drops
            return DateTime.UtcNow >= GetCooldown(undertaker.PlayerId) && FindClosestBody(undertaker) != null;
        }

        public static void TryDrag(PlayerControl undertaker)
        {
            var client = AmongUsClient.Instance;
            if (client == null || undertaker == null || undertaker.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestDragRpc, undertaker.PlayerId);
                return;
            }
            if (!CanDragNow(undertaker)) return;

            if (IsDragging(undertaker.PlayerId))
            {
                Drop(undertaker);
                return;
            }
            var body = FindClosestBody(undertaker);
            if (body == null) return;

            Dragged[undertaker.PlayerId] = body.ParentId;
            Cooldowns[undertaker.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.UndertakerDragCooldown, 10f));
            TownOfRolesRpcMux.Send(DragRpc, undertaker.PlayerId, body.ParentId);
        }

        private static void Drop(PlayerControl undertaker)
        {
            Dragged.Remove(undertaker.PlayerId);
            TownOfRolesRpcMux.Send(DropRpc, undertaker.PlayerId);
        }

        // Runs every frame on every client: keep dragged bodies following the Undertaker.
        public static void Tick()
        {
            if (Dragged.Count == 0) return;
            foreach (var pair in new Dictionary<byte, byte>(Dragged))
            {
                var undertaker = FindPlayer(pair.Key);
                var body = FindBody(pair.Value);
                if (undertaker == null || undertaker.Data == null || undertaker.Data.IsDead || body == null || body.Reported)
                {
                    Dragged.Remove(pair.Key);
                    continue;
                }
                try
                {
                    body.transform.position = undertaker.transform.position + new Vector3(0f, -0.7f, -2f);
                }
                catch { }
            }
        }

        private static DeadBody FindClosestBody(PlayerControl player)
        {
            if (player == null || player.Data == null) return null;
            if (PlayerControl.GameOptions == null) return null;
            var origin = player.GetTruePosition();
            var killDistance = GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance];
            DeadBody best = null;
            var bestDistance = float.MaxValue;

            foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                if (body == null || body.Reported) continue;
                var distance = Vector2.Distance(origin, body.TruePosition);
                if (distance <= killDistance && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = body;
                }
            }
            return best;
        }

        [ReactorRpc(RequestDragRpc)]
        private static void OnRequestDrag(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryDrag(player);
                    return;
                }
            }
        }

        [ReactorRpc(DragRpc)]
        private static void OnDrag(byte senderId, byte draggerId, byte bodyParentId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            Dragged[draggerId] = bodyParentId;
        }

        [ReactorRpc(DropRpc)]
        private static void OnDrop(byte senderId, byte draggerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            Dragged.Remove(draggerId);
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        private static DeadBody FindBody(byte parentId)
        {
            foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
                if (body != null && body.ParentId == parentId) return body;
            return null;
        }

        private static DateTime GetCooldown(byte playerId) =>
            Cooldowns.TryGetValue(playerId, out var value) ? value : DateTime.MinValue;

        public static void Reset()
        {
            Dragged.Clear();
            Cooldowns.Clear();
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
        public static void OnMeetingStarted(MeetingEventArgs _) => Reset();

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
