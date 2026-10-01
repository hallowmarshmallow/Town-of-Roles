using System;
using HarmonyLib;
using UnityEngine;

namespace TownOfRoles.Core
{
    // The mod's *fallback* wheel handling for the native game-config menu.
    internal static class SettingsScroll
    {
        // Visible list height in world units (~YStart down to screen bottom).
        private const float ViewportHeight = 9.0f;

        // One wheel notch (~3) moves the list by roughly one row (0.45).
        private const float WheelFactor = 0.15f;

        private static float _offset;
        private static Transform _trackedFirstChild;

        // The wheel reader used to live here: UnityEngine.Input is not in the shipped
        // GameLibs reference, so it has to be reached by reflection, and that reflection
        // is not specific to this page. It is MarshAPI.UiWheel's now, the same reader
        // drives the horizontal pan, so the module-name fallbacks and the once-only
        // warning exist in one place.

        // Called when the menu (re)builds its rows, so scrolling starts from the top.
        public static void Reset() => _offset = 0f;

        internal static GameOptionsMenu _trackedMenu;

        // Remembers the page the fallback drives. Called from the OnEnable hook, because
        // SettingMenu has no Update on 9.10 to tick from directly, the tick instead rides the
        // HudManager patch below, which is a detour both platforms have proven safe.
        public static void Track(GameOptionsMenu menu) => _trackedMenu = menu;

        public static void Tick(GameOptionsMenu menu)
        {
            if (RoleConfig.NativeMenuRows?.Value != true) return;
            if (menu == null || menu.transform == null) return;

            // Rows are rebuilt from scratch on every menu open and on internal
            // role rebuilds (GameOptionsMenu.Update re-runs SettingMenu.OnEnable
            // via _pendingRoleRebuild). Detect a new root child and reset the
            // accumulated offset so a stale value never applies to freshly
            // positioned rows.
            var firstChild = menu.transform.childCount > 0 ? menu.transform.GetChild(0) : null;
            if (firstChild != _trackedFirstChild)
            {
                _offset = 0f;
                _trackedFirstChild = firstChild;
            }

            // 9.10 owns the wheel through its own Scroller on both settings
            // pages; translating the rows here as well would double every notch.
            if (MarshAPI.UiScroll.FindNativeScroller(menu.transform) != null) return;

            if (!MarshAPI.UiWheel.Available) return;

            float wheel = MarshAPI.UiWheel.Scroll.y;
            if (Mathf.Abs(wheel) < 0.01f) return;

            // Content height is offset-invariant, so it can be measured from
            // the current (already scrolled) positions.
            float first = float.MinValue, last = float.MaxValue;
            for (int i = 0; i < menu.transform.childCount; i++)
            {
                var child = menu.transform.GetChild(i);
                if (child == null || !child.gameObject.activeSelf) continue;
                float y = child.localPosition.y;
                if (y > first) first = y;
                if (y < last) last = y;
            }
            if (first == float.MinValue || last == float.MaxValue) return;

            float contentHeight = first - last;
            float maxOffset = Mathf.Max(0f, contentHeight - ViewportHeight);
            float newOffset = Mathf.Clamp(_offset - wheel * WheelFactor, 0f, maxOffset);
            float shift = newOffset - _offset;
            if (Mathf.Abs(shift) < 0.0001f) return;
            _offset = newOffset;

            for (int i = 0; i < menu.transform.childCount; i++)
            {
                var child = menu.transform.GetChild(i);
                if (child == null || !child.gameObject.activeSelf) continue;
                var p = child.localPosition;
                p.y += shift;
                child.localPosition = p;
            }
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogWarning("Settings scroll: " + message);
    }

    // Targeting GameOptionsMenu directly terminates the 2026.9.10 load (class init
    // inside the resolution, see crash-reports/), so the fallback ticks from the
    // HUD instead: the menu instance is captured at OnEnable (Track) and driven
    // per frame here, a detour both platforms have proven safe. The tick is a
    // no-op while no menu is tracked, which is every frame outside settings.
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    internal static class HudManager_Update_SettingsScrollPatch
    {
        private static void Postfix()
        {
            try
            {
                SettingsScroll.Tick(SettingsScroll._trackedMenu);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Settings scroll: " + e.Message);
            }
        }
    }
}
