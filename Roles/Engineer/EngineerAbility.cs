using System;
using Atomic;
using MarshAPI;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Engineer
{
    internal static class EngineerAbility
    {
        private const string FixLightsRpc = "townofroles.EngineerFixLights";
        private static DateTime _cooldownUntil;

        internal static bool CanFixSab(PlayerControl engineer)
        {
            return engineer != null && engineer.Data != null && !engineer.Data.IsDead &&
                   EngineerSystem.IsEngineer(engineer) && DateTime.UtcNow >= _cooldownUntil && ShipStatus.Instance != null;
        }

        internal static bool IsSabotageActive()
        {
            var ship = ShipStatus.Instance;
            if (ship == null || ship.Systems == null) return false;

            try
            {
                var reactor = GetSystem<ReactorSystemType>(ship, SystemTypes.Reactor);
                var seismic = GetSystem<ReactorSystemType>(ship, SystemTypes.Laboratory);
                var oxygen = GetSystem<LifeSuppSystemType>(ship, SystemTypes.LifeSupp);
                var comms = GetSystem<HqHudSystemType>(ship, SystemTypes.Comms);
                var lights = GetSystem<SwitchSystem>(ship, SystemTypes.Electrical);

                if ((reactor != null && reactor.IsActive) || (seismic != null && seismic.IsActive)) return true;
                if (oxygen != null && oxygen.IsActive) return true;
                if (comms != null && comms.IsActive) return true;
                if (lights != null && lights.Value != lights.ExpectedSwitches) return true;
            }
            catch { }
            return false;
        }

        public static bool TryFixSab(PlayerControl engineer)
        {
            if (!CanFixSab(engineer)) return false;

            var ship = ShipStatus.Instance;
            if (ship == null || ship.Systems == null) return false;

            try
            {
                var reactor = GetSystem<ReactorSystemType>(ship, SystemTypes.Reactor);
                var seismic = GetSystem<ReactorSystemType>(ship, SystemTypes.Laboratory);
                if (reactor != null && reactor.IsActive)
                {
                    ship.RpcRepairSystem(SystemTypes.Reactor, 16);
                    Finish();
                    return true;
                }
                if (seismic != null && seismic.IsActive)
                {
                    ship.RpcRepairSystem(SystemTypes.Laboratory, 16);
                    Finish();
                    return true;
                }

                var oxygen = GetSystem<LifeSuppSystemType>(ship, SystemTypes.LifeSupp);
                if (oxygen != null && oxygen.IsActive)
                {
                    ship.RpcRepairSystem(SystemTypes.LifeSupp, 16);
                    Finish();
                    return true;
                }

                var comms = GetSystem<HqHudSystemType>(ship, SystemTypes.Comms);
                if (comms != null && comms.IsActive)
                {
                    ship.RpcRepairSystem(SystemTypes.Comms, (int)HqHudSystemType.Tags.DeactiveBit | 0);
                    ship.RpcRepairSystem(SystemTypes.Comms, (int)HqHudSystemType.Tags.DeactiveBit | 1);
                    Finish();
                    return true;
                }

                var lights = GetSystem<SwitchSystem>(ship, SystemTypes.Electrical);
                if (lights != null && lights.Value != lights.ExpectedSwitches)
                {
                    lights.ActualSwitches = lights.ExpectedSwitches;
                    lights.Value = lights.ExpectedSwitches;
                    TownOfRolesRpcMux.Send(FixLightsRpc);
                    Finish();
                    return true;
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Engineer fix: " + e.Message);
                return false;
            }

            return false;

            void Finish() =>
                _cooldownUntil = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.EngineerFixCooldown, 30f));
        }

        private static T GetSystem<T>(ShipStatus ship, SystemTypes type) where T : Il2CppObjectBase
        {
            try
            {
                var system = ship.Systems[type];
                return system?.TryCast<T>();
            }
            catch { return null; }
        }

        [AtomicRpc(FixLightsRpc)]
        private static void OnFixLights(byte senderId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var ship = ShipStatus.Instance;
            var lights = ship == null ? null : GetSystem<SwitchSystem>(ship, SystemTypes.Electrical);
            if (lights == null) return;
            lights.ActualSwitches = lights.ExpectedSwitches;
            lights.Value = lights.ExpectedSwitches;
        }

        public static void Reset()
        {
            _cooldownUntil = DateTime.MinValue;
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
    }

    internal static class EngineerSystem
    {
        public static bool IsEngineer(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, EngineerRole.Id);
    }
}
