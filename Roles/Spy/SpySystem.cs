using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Spy
{
    // Spy gameplay logic (ported from Town-Of-Us' Spy.cs).
    internal static class SpySystem
    {
        private const string IntelRpc = "townofroles.SpyIntel";
        private const byte KindVentEnter = 1;
        private const byte KindVentExit = 2;
        private const byte KindDoused = 3;

        private static readonly HashSet<byte> Venting = new();
        private static float _nextCheck;

        public static bool IsSpy(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, SpyRole.Id);

        // Called by the Arsonist when a player gets doused.
        public static void OnPlayerDoused(byte playerId)
        {
            if (!AnySpy()) return;
            SendIntel(KindDoused, playerId);
        }

        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            if (!AnySpy()) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.3f;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                var id = player.PlayerId;
                var isVenting = player.inVent;
                var known = Venting.Contains(id);
                if (isVenting && !known)
                {
                    Venting.Add(id);
                    SendIntel(KindVentEnter, id);
                }
                else if (!isVenting && known)
                {
                    Venting.Remove(id);
                    SendIntel(KindVentExit, id);
                }
            }
        }

        private static bool AnySpy()
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && IsSpy(player) && !player.Data.IsDead) return true;
            return false;
        }

        private static void SendIntel(byte kind, byte playerId)
        {
            try { TownOfRolesRpcMux.Send(IntelRpc, kind, playerId); }
            catch (Exception e) { BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Spy intel: " + e.Message); }
            // Only surface the intel locally if the host is actually a Spy.
            var local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || !IsSpy(local) || local.Data.IsDead) return;
            ShowIntel(kind);
        }

        [ReactorRpc(IntelRpc)]
        private static void OnIntel(byte senderId, byte kind, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || !IsSpy(local) || local.Data.IsDead) return;
            ShowIntel(kind);
        }

        private static void ShowIntel(byte kind)
        {
            string message;
            switch (kind)
            {
                case KindVentEnter: message = "🔍 A player entered a vent."; break;
                case KindVentExit: message = "🔍 A player left a vent."; break;
                case KindDoused: message = "🔍 A player was doused."; break;
                default: return;
            }
            try
            {
                SystemChat.Show(message);
            }
            catch { }
        }

        public static void Reset()
        {
            Venting.Clear();
            _nextCheck = 0f;
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
    }

}
