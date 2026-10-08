using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Atomic;
using HarmonyLib;
using MarshAPI;
using TMPro;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class VersionBadge
    {
        private const string BadgeName = "TownOfRolesVersionBadge";

        private const string StackBadgeName = "TownOfRolesStackBadge";

        internal const string Product = "Town Of Roles";

        internal static string Badge() => Product + " " + TownOfRolesPlugin.Version + " (Beta)";

        internal static string StackBadge() =>
            "Atomic " + AtomicPlugin.Version + " | MarshAPI " + MarshAPIPlugin.Version;

        // Everything BepInEx.Core says about itself, like "6.0.0-be.788+5b766a3b7f6c".
        private static string BepInExProductVersion()
        {
            try
            {
                var assembly = typeof(TownOfRolesPlugin).BaseType?.Assembly;
                if (assembly == null) return null;

                var info = FileVersionInfo.GetVersionInfo(assembly.Location);
                if (!string.IsNullOrEmpty(info.ProductVersion)) return info.ProductVersion;
                if (!string.IsNullOrEmpty(info.FileVersion)) return info.FileVersion;

                var version = assembly.GetName().Version;
                return version == null ? null : version.ToString();
            }
            catch
            {
                return null;
            }
        }

        // "6.0.0-be.788" becomes "6+788".
        internal static string BepInExVersion()
        {
            string raw = BepInExProductVersion();
            if (string.IsNullOrEmpty(raw)) return "?";

            int plus = raw.IndexOf('+');
            string version = plus > 0 ? raw.Substring(0, plus) : raw;

            int be = version.IndexOf("-be.");
            int dot = version.IndexOf('.');
            if (be > 0 && dot > 0) return version.Substring(0, dot) + "+" + version.Substring(be + 4);

            return version;
        }

        // The part after the '+', cut down to 7 characters.
        private static string BepInExCommit()
        {
            string raw = BepInExProductVersion();
            if (string.IsNullOrEmpty(raw)) return null;

            int plus = raw.IndexOf('+');
            if (plus < 0 || plus + 1 >= raw.Length) return null;

            string commit = raw.Substring(plus + 1).Trim();
            if (commit.Length == 0) return null;
            return commit.Length > 7 ? commit.Substring(0, 7) : commit;
        }

        internal static string BepInExLine()
        {
            string commit = BepInExCommit();
            return "BepInEx " + BepInExVersion() + (string.IsNullOrEmpty(commit) ? "" : " (" + commit + ")");
        }

        // The game's own line is "v2026.9.20". Put the BepInEx build right after it.
        private static void SitNextToGameVersion(TextMeshPro text)
        {
            if (text == null) return;

            string raw;
            try { raw = text.text; }
            catch { return; }

            if (string.IsNullOrEmpty(raw) || raw.Contains("BepInEx ")) return;

            string tag = "<color=#8C93A8>" + BepInExLine() + "</color>";
            string[] lines = raw.Split('\n');

            int at = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (IsVersionLine(lines[i])) { at = i; break; }
            }

            if (at >= 0)
            {
                lines[at] = lines[at].TrimEnd('\r') + "  " + tag;
            }
            else
            {
                string[] bigger = new string[lines.Length + 1];
                bigger[0] = tag;
                Array.Copy(lines, 0, bigger, 1, lines.Length);
                lines = bigger;
            }

            text.text = string.Join("\n", lines);
            text.ForceMeshUpdate(false, false);
        }

        // A line holding nothing but a version, like "v2026.9.20", tags stripped.
        private static bool IsVersionLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string plain = StripTags(line).Trim();
            if (plain.Length == 0) return false;

            int dots = 0;
            int digits = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                char c = plain[i];
                if (c >= '0' && c <= '9') { digits++; continue; }
                if (c == '.' || c == 'v') { dots++; continue; }
                if (c == ' ') continue;
                return false;
            }

            return digits >= 3 && dots >= 2;
        }

        private static string StripTags(string line)
        {
            var sb = new StringBuilder(line.Length);
            bool inside = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '<') { inside = true; continue; }
                if (c == '>') { inside = false; continue; }
                if (!inside) sb.Append(c);
            }

            return sb.ToString();
        }

        public static void Ensure(VersionShower shower)
        {
            var text = shower == null ? null : shower.text;

            SitNextToGameVersion(text);

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
