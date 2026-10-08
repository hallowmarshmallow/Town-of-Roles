using System;
using System.Diagnostics;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using TownOfRoles.Assets;
using TownOfRoles.Roles;

namespace TownOfRoles.Core
{
    internal static class BootScreenLoad
    {
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("TownOfRoles");

        private const string BadgeName = "TownOfRolesBootBadge";
        private const string GlitchBundleResource = "TownOfRoles.Assets.OriginalTownOfUs.Resources.glitchbundle";
        private const string GlitchBundleKey = "townofroles.glitchbundle";

        private static readonly Color BadgeColor = new Color(0.45f, 0.85f, 1f);

        private static bool _ran;

        public static bool Ran => _ran;

        public static void Run(string when)
        {
            if (_ran) return;

            if (RoleConfig.BootScreenPreload?.Value != true) return;

            _ran = true;

            var sw = Stopwatch.StartNew();
            try
            {
                int sprites = PreloadAssets();
                CodeInit(when);
            }
            catch (Exception e)
            {
                Log.LogError("(" + when + "): preload failed: " + e);
            }
        }

        private static int PreloadAssets()
        {
            int sprites = 0;
            try
            {
                sprites = RoleArt.Preload();
            }
            catch (Exception e)
            {
                Log.LogWarning("sprites failed to preload: " + e.Message);
            }

            try
            {
                var bundle = MarshAPI.AssetBundleManager.LoadFromEmbeddedResource(
                    typeof(RoleArt).Assembly, GlitchBundleResource, GlitchBundleKey);
            }
            catch (Exception e)
            {
                Log.LogWarning("embedded glitch bundle failed: " + e.Message);
            }

            return sprites;
        }

        private static void CodeInit(string when)
        {
            int catalog = 0;
            try
            {
                catalog = RoleCatalog.All.Length;
            }
            catch (Exception e)
            {
                Log.LogWarning("role catalog warm-up failed: " + e.Message);
            }

            int enabled = RoleConfig.EnabledRoleCount();

            MarshAPI.UiAbilityButton.CaptureTemplate(when, deferIfMissing: true);

        }

        public static void DecorateSplash(SplashManager splash)
        {
            if (RoleConfig.BootScreenBadge?.Value != true) return;

            try
            {
                var anchor = splash == null ? null : splash.loadingText;
                if (anchor == null) return;
                if (anchor.transform.Find(BadgeName) != null) return;

                anchor.ForceMeshUpdate(false, false);
                var rend = anchor.GetComponent<MeshRenderer>();
                Bounds bounds = rend != null ? rend.bounds : new Bounds(anchor.transform.position, Vector3.zero);
                float lineHeight = bounds.size.y > 0f ? bounds.size.y : 0.3f;

                var go = new GameObject(BadgeName);
                go.transform.SetParent(anchor.transform, true);
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                go.transform.position = new Vector3(
                    anchor.transform.position.x,
                    bounds.min.y - lineHeight * 0.9f,
                    anchor.transform.position.z);

                var tmp = go.AddComponent<TextMeshPro>();
                tmp.font = anchor.font;
                tmp.fontSharedMaterial = anchor.fontSharedMaterial;
                tmp.text = BadgeText();
                tmp.fontSize = anchor.fontSize;
                tmp.color = BadgeColor;
                tmp.alignment = anchor.alignment;
                tmp.enableWordWrapping = false;

            }
            catch (Exception e)
            {
                Log.LogWarning("splash badge failed: " + e.Message);
            }
        }

        public static void DecorateUpdatePopup(GithubReleaseChecker checker)
        {
            if (RoleConfig.BootScreenBadge?.Value != true) return;

            try
            {
                var popup = checker == null ? null : checker.popupText;
                if (popup == null) return;

                var line = BadgeText();
                var original = popup.text ?? string.Empty;
                if (original.Contains(line)) return;

                popup.text = original.TrimEnd('\n') +
                             (original.Length > 0 ? "\n" : string.Empty) +
                             "<size=70%><color=#73D9FF>" + line + "</color></size>";

            }
            catch (Exception e)
            {
                Log.LogWarning("update popup badge failed: " + e.Message);
            }
        }

        private static string BadgeText() =>
            "Town of Roles v" + TownOfRolesPlugin.Version + " - " +
            RoleConfig.EnabledRoleCount() + " roles";

        [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start))]
        internal static class SplashManager_Start_BootScreenPatch
        {
            private static void Postfix(SplashManager __instance)
            {
                try { DecorateSplash(__instance); }
                catch { }

                try { Run("SplashManager.Start"); }
                catch { }
            }
        }

        [HarmonyPatch(typeof(GithubReleaseChecker), nameof(GithubReleaseChecker.Awake))]
        internal static class GithubReleaseChecker_Awake_BootScreenPatch
        {
            private static void Postfix(GithubReleaseChecker __instance)
            {
                try { DecorateUpdatePopup(__instance); }
                catch { }

                try { Run("GithubReleaseChecker.Awake"); }
                catch { }
            }
        }
    }
}
