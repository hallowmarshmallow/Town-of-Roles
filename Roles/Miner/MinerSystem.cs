using System;
using System.Collections.Generic;
using Atomic;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Miner
{
    internal static class MinerSystem
    {
        private const string RequestMineRpc = "townofroles.MinerRequestMine";
        private const string MineRpc = "townofroles.MinerMine";
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();

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

        private static void CreateVent(int id, Vector2 position, bool host)
        {
            try
            {
                var ship = ShipStatus.Instance;
                if (ship == null || ship.AllVents == null || ship.AllVents.Length == 0) return;
                var template = ship.AllVents[0];
                if (template == null || template.gameObject == null) return;

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

                vent.Left = null;
                vent.Right = null;
                vent.Center = null;

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

                GameReflection.SetAllVents(ship, list.ToArray());
            }
        }

        [AtomicRpc(RequestMineRpc)]
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

        [AtomicRpc(MineRpc)]
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
