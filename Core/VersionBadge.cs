using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ClassicUs.Reactor;
using HarmonyLib;
using MarshAPI;
using TMPro;
using UnityEngine;

namespace TownOfRoles.Core
{
    // Shows the mod's version line directly below the game's version text in the top-left
    // corner (the "2026.9.10" readout on the main menu), the same line plus the author credit
    // above the ping/fps readout in game, and a second line naming the versions of everything
    // the mod is actually running on.
    internal static class VersionBadge
    {
        private const string BadgeName = "TownOfRolesVersionBadge";

        private const string StackBadgeName = "TownOfRolesStackBadge";

        // The mod's name, spelled once.
        internal const string Product = "Town Of Roles";

        // The badge line: name, version, release tag. Named rather than inlined so the menu
        // badge and the in-game credit cannot disagree about the wording, which is why it is
        // reachable from both.
        internal static string Badge() => Product + " " + TownOfRolesPlugin.Version + " (Beta)";

        // The stack line: the version of everything the mod actually runs on, plus the build
        // revision, the second badge row on the main menu.
        internal static string StackBadge() =>
            "BepInEx " + BepInExVersion() +
            " | Reactor " + ReactorPlugin.Version +
            " | MarshAPI " + MarshAPIPlugin.Version +
            " | " + Revision();

        // The build revision, as r&lt;8 hex&gt;, or runknown for a build the SDK could not
        // stamp one onto (no git in PATH, or a source archive with no repository behind it).
        internal static string Revision()
        {
            try
            {
                var informational = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .OfType<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault()
                    ?.InformationalVersion;

                int plus = informational == null ? -1 : informational.IndexOf('+');
                if (plus < 0 || plus + 1 >= informational.Length) return "runknown";

                var commit = informational.Substring(plus + 1).Trim();
                if (commit.Length > 8) commit = commit.Substring(0, 8);

                return commit.Length == 0 ? "runknown" : "r" + commit;
            }
            catch
            {
                return "runknown";
            }
        }

        // BepInEx's own version.
        internal static string BepInExVersion()
        {
            try
            {
                var assembly = typeof(TownOfRolesPlugin).BaseType?.Assembly;
                if (assembly == null) return "?";

                var info = FileVersionInfo.GetVersionInfo(assembly.Location);
                string raw = !string.IsNullOrEmpty(info.ProductVersion)
                    ? info.ProductVersion
                    : !string.IsNullOrEmpty(info.FileVersion) ? info.FileVersion : null;

                if (raw != null)
                {
                    int plus = raw.IndexOf('+');
                    return plus > 0 ? raw.Substring(0, plus) : raw;
                }

                var version = assembly.GetName().Version;
                return version == null ? "?" : version.ToString();
            }
            catch
            {
                return "?";
            }
        }

        public static void Ensure(VersionShower shower)
        {
            // AddVersionLine is idempotent by name, which is what the old "already placed"
            // check was for, so a second line is a second name rather than a second check.
            var text = shower == null ? null : shower.text;

            MarshAPI.ModBadgeAPI.AddVersionLine(text, BadgeName, Badge(), new Color(0.45f, 0.85f, 1f));
            MarshAPI.ModBadgeAPI.AddVersionLine(text, StackBadgeName, StackBadge(), new Color(0.62f, 0.66f, 0.74f));
        }
    }

    [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
    internal static class VersionShower_Start_VersionBadgePatch
    {
        private static void Postfix(VersionShower __instance)
        {
            try
            {
                VersionBadge.Ensure(__instance);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("Town of Roles").LogError("Version badge: " + e.Message);
            }
        }
    }

    // In-game credit line. Each frame the PingTracker rewrites the ping/fps text, so this
    // postfix prepends the mod stack ("Town Of Roles" + "by hallowmarsh") on top of it,
    // directly above the ping and fps labels in the lobby/HUD.
    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    internal static class PingTracker_Update_CreditPatch
    {
        private static void Postfix(PingTracker __instance)
        {
            try
            {
                var renderer = __instance == null ? null : __instance.text;
                if (renderer == null) return;
                var tmp = renderer.TextData;
                if (tmp == null) return;

                string current = tmp.text;
                if (string.IsNullOrEmpty(current) || current.StartsWith(VersionBadge.Product)) return;

                tmp.text = VersionBadge.Badge() + "\nby <color=#6BFF77>hallowmarsh</color>\n" + current;
                renderer.RefreshMesh();
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("Town of Roles").LogError("PingTracker credit: " + e.Message);
            }
        }
    }
}
