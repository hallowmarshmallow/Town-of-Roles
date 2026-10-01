using System;
using System.Collections.Generic;
using HarmonyLib;
using MarshAPI;

namespace TownOfRoles.Core
{
    // Bridges the mod's role settings onto the game's own role-option page.
    internal static class RoleNativeOptions
    {
        // Declares role's native options.
        public static void Declare(CustomRole role, RoleOptionBuilder options)
        {
            if (role == null || options == null) return;

            try
            {
                var key = RoleCatalog.KeyOfId(role.RoleTypeName);
                if (key == null)
                {
                    // Not a catalog role: an external mod's descriptor. It has no
                    // channels, so it declares its own options in its own override.
                    return;
                }

                if (!RoleOptionSpecs.TryGet(key, out var specs)) return;

                for (int i = 0; i < specs.Length; i++)
                    DeclareOne(key, in specs[i], options);
            }
            catch (Exception e)
            {
                // A declaration failure must not stop the role from registering; the
                // builder throws on a malformed option precisely so it is caught here
                // rather than inside the game's menu build.
                Log("declare for " + role.RoleTypeName + ": " + e.Message);
            }
        }

        private static void DeclareOne(string roleKey, in (string Field, string Label, string Kind) spec,
                                       RoleOptionBuilder options)
        {
            var channelName = roleKey + "." + spec.Field;

            // False means "no native equivalent": either there is no such channel, or
            // it is a string setting. Either way it stays where it is.
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

        // The native row's step per click.
        private static float StepFor(float min, float max) => (max - min) < 10f ? 0.1f : 1f;

        // mod -> native

        // Writes one channel's value into the game's option store, so a change made on the
        // mod's own row is visible to the native page too.
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

        // native -> mod

        private static int _pullFailures;

        // Copies every registered role's native option values back into the mod's channels.
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

    // The host's option set has just been applied to the local role objects: mirror it into the
    // mod's channels, which is what gameplay actually reads.
    [HarmonyPatch(typeof(RoleOptionsManager), nameof(RoleOptionsManager.ReadJson))]
    internal static class RoleOptionsManager_ReadJson_NativeOptionsPatch
    {
        private static void Postfix()
        {
            try { RoleNativeOptions.PullFromNative(); }
            catch (Exception e) { BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("ReadJson bridge: " + e.Message); }
        }
    }

    // A native role-option row was clicked on this client.
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
