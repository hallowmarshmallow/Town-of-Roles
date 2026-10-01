using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Miner
{
    // Miner gameplay logic (ported from Town-Of-Us' Miner.cs).
    internal static class MinerSystem
    {
        private const string RequestMineRpc = "townofroles.MinerRequestMine";
        private const string MineRpc = "townofroles.MinerMine";
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();
        // All mine vents placed this round, keyed by vent id (all clients).
        private static readonly Dictionary<int, Vent> Mines = new();
        private static int _nextVentId = 1000;

        public static bool IsMiner(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, MinerRole.Id);

        internal static bool CanMineNow(PlayerControl miner)
        {
            if (!IsMiner(miner) || miner.Data == null || miner.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(miner.PlayerId);
        }

        public static void TryMine(PlayerControl miner)
        {
            var client = AmongUsClient.Instance;
            if (client == null || miner == null || miner.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestMineRpc, miner.PlayerId);
                return;
            }
            if (!CanMineNow(miner)) return;

            var position = miner.GetTruePosition();
            int id = AllocateVentId();
            Cooldowns[miner.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.MineCooldown, 30f));

            CreateVent(id, position, host: true);
            TownOfRolesRpcMux.Send(MineRpc, id, position.x, position.y);
        }

        // Instantiates a mine vent clone, links it to the nearest existing mine, and registers
        // it in ShipStatus.AllVents. Runs on the host (authoritative) and identically on every
        // client for the RPC.
        private static void CreateVent(int id, Vector2 position, bool host)
        {
            try
            {
                var ship = ShipStatus.Instance;
                if (ship == null || ship.AllVents == null || ship.AllVents.Length == 0) return;
                var template = ship.AllVents[0];
                if (template == null || template.gameObject == null) return;

                // Deliberately NOT UiClone: this is a gameplay object, not UI. It
                // keeps every component (it has to *be* a working Vent), it must not
                // have its listeners cleared, and it must not join any GridArrange
                // ignore list. The only thing it shares with a UI clone is the word.
                var clone = UnityEngine.Object.Instantiate(template.gameObject, ship.transform);
                clone.name = "MineVent_" + id;
                var vent = clone.GetComponent<Vent>();
                if (vent == null) vent = clone.GetComponentInChildren<Vent>(true);
                if (vent == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                vent.Id = id;
                vent.transform.position = new Vector3(position.x, position.y, template.transform.position.z);

                // The clone inherits the template's serialized Left/Right/Center
                // references (Unity only remaps refs to cloned objects, and the
                // template's neighbors are NOT cloned). Null them so this mine is
                // not secretly wired into the vanilla vent network; the only link
                // is the one we set below.
                vent.Left = null;
                vent.Right = null;
                vent.Center = null;

                // Link to the nearest previously placed mine (bidirectional).
                Vent nearest = null;
                var nearestDistance = float.MaxValue;
                foreach (var existing in Mines.Values)
                {
                    if (existing == null || existing.gameObject == null) continue;
                    var distance = Vector2.Distance(existing.transform.position, vent.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = existing;
                    }
                }
                if (nearest != null)
                {
                    vent.Left = nearest;
                    nearest.Right = vent;
                }

                Mines[id] = vent;

                // Register with the ship so vent interactions treat it as real.
                // AllVents has a private setter in the 2026.8.9 interop.
                var list = new List<Vent>(ship.AllVents);
                list.Add(vent);
                GameReflection.SetAllVents(ship, list.ToArray());
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Miner vent: " + e.Message);
            }
        }

        private static int AllocateVentId()
        {
            int id = _nextVentId++;
            // Avoid colliding with any vanilla vent ids on the map.
            var ship = ShipStatus.Instance;
            if (ship != null && ship.AllVents != null)
            {
                while (true)
                {
                    bool collision = false;
                    foreach (var vent in ship.AllVents)
                    {
                        if (vent != null && vent.Id == id) { collision = true; break; }
                    }
                    if (!collision) break;
                    id = _nextVentId++;
                }
            }
            return id;
        }

        // Round lifecycle
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();

        public static void Reset()
        {
            Cooldowns.Clear();
            DestroyMines();
            Mines.Clear();
            _nextVentId = 1000;
        }

        public static void OnGameEnded(GameEndedEventArgs _) => Reset();

        private static void DestroyMines()
        {
            foreach (var vent in Mines.Values)
            {
                if (vent == null || vent.gameObject == null) continue;
                try { UnityEngine.Object.Destroy(vent.gameObject); } catch { }
            }
            // Remove the mine vents from the ship's AllVents array.
            var ship = ShipStatus.Instance;
            if (ship != null && ship.AllVents != null)
            {
                var list = new List<Vent>();
                foreach (var vent in ship.AllVents)
                {
                    if (vent == null) continue;
                    var mineId = vent.Id;
                    if (mineId >= 1000 && Mines.ContainsKey(mineId)) continue;
                    list.Add(vent);
                }
                // AllVents has a private setter in the 2026.8.9 interop.
                GameReflection.SetAllVents(ship, list.ToArray());
            }
        }

        // RPCs
        [ReactorRpc(RequestMineRpc)]
        private static void OnRequestMine(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryMine(player);
                    return;
                }
            }
        }

        [ReactorRpc(MineRpc)]
        private static void OnMine(byte senderId, int ventId, float x, float y)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            if (Mines.ContainsKey(ventId)) return;
            CreateVent(ventId, new Vector2(x, y), host: false);
        }

        private static DateTime GetCooldown(byte minerId) =>
            Cooldowns.TryGetValue(minerId, out var value) ? value : DateTime.MinValue;

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
