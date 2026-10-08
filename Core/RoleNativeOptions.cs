using System;
using System.Collections.Generic;
using HarmonyLib;
using MarshAPI;

namespace TownOfRoles.Core
{
    internal static class RoleNativeOptions
    {
        public static void Declare(CustomRole role, RoleOptionBuilder options)
        {
            if (role == null || options == null) return;

            try
            {
                var key = RoleCatalog.KeyOfId(role.RoleTypeName);
                if (key == null)
                {
                    return;
                }

                if (!RoleOptionSpecs.TryGet(key, out var specs)) return;

                for (int i = 0; i < specs.Length; i++)
                    DeclareOne(key, in specs[i], options);
            }
            catch (Exception e)
            {
                Log("declare for " + role.RoleTypeName + ": " + e.Message);
            }
        }

        private static void DeclareOne(string roleKey, in (string Field, string Label, string Kind) spec,
                                       RoleOptionBuilder options)
        {
            var channelName = roleKey + "." + spec.Field;

            if (!RoleSettingsSync.TryDescribe(channelName, out var kind, out var min, out var max, out var current))
                return;

            switch (kind)
            {
                case "bool":
                    options.Bool(spec.Field, spec.Label, current != 0f);
                    break;
                case "int":
                    options.Int(spec.Field, spec.Label, (int)min, (int)max, (int)Math.Round(current));
                    break;
                default:
                    options.Float(spec.Field, spec.Label, min, max, StepFor(min, max), current);
                    break;
            }
        }

        private static float StepFor(float min, float max) => (max - min) < 10f ? 0.1f : 1f;

        public static void PushChannel(string channelName)
        {
            if (!NativeRoleOptions.Enabled) return;
            if (!TrySplit(channelName, out var roleKey, out var field)) return;

            if (!RoleSettingsSync.TryDescribe(channelName, out var kind, out _, out _, out var value)) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null) return;

            try
            {
                NativeRoleOptions.Sync(player, roleKey, field, value);
            }
            catch (Exception e)
            {
                Log("push " + channelName + ": " + e.Message);
            }
        }

        private static bool TrySplit(string channelName, out string roleKey, out string field)
        {
            roleKey = null;
            field = null;
            if (string.IsNullOrEmpty(channelName)) return false;

            int dot = channelName.IndexOf('.');
            if (dot <= 0 || dot == channelName.Length - 1) return false;

            roleKey = channelName.Substring(0, dot);
            field = channelName.Substring(dot + 1);
            return true;
        }

        private static int _pullFailures;

        internal static void PullFromNative()
        {
            if (!NativeRoleOptions.Enabled) return;

            try
            {
                foreach (var (descriptor, role) in RoleRegistry.RegisteredRoleBehaviours())
                {
                    var roleKey = RoleCatalog.KeyOfId(descriptor.RoleTypeName);
                    if (roleKey == null) continue;

                    var options = role.AllOptions;
                    if (options == null) continue;

                    foreach (var pair in options)
                    {
                        if (pair.Key == null) continue;
                        if (!RoleSettingsSync.TryDescribe(roleKey + "." + pair.Key, out _, out _, out _, out _)) continue;

                        var option = pair.Value;
                        if (option == null) continue;

                        RoleSettingsSync.ApplyFromNative(roleKey + "." + pair.Key, option.FloatValue);
                    }
                }
            }
            catch (Exception e)
            {
                if (_pullFailures++ < 3)
                    Log("pull from native: " + e.Message);
            }
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Role native options: " + message);
    }

    [HarmonyPatch(typeof(RoleOptionsManager), nameof(RoleOptionsManager.ReadJson))]
    internal static class RoleOptionsManager_ReadJson_NativeOptionsPatch
    {
        private static void Postfix()
        {
            try { RoleNativeOptions.PullFromNative(); }
            catch (Exception e) { BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("ReadJson bridge: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(RoleSettingGameOption), nameof(RoleSettingGameOption.ChangeRoleOption))]
    internal static class RoleSettingGameOption_ChangeRoleOption_NativeOptionsPatch
    {
        private static void Postfix()
        {
            try { RoleNativeOptions.PullFromNative(); }
            catch (Exception e) { BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Native option click bridge: " + e.Message); }
        }
    }
}
