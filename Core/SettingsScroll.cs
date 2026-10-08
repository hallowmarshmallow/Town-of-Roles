using System;
using HarmonyLib;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class SettingsScroll
    {
        private const float ViewportHeight = 9.0f;

        private const float WheelFactor = 0.15f;

        private static float _offset;
        private static Transform _trackedFirstChild;

        public static void Reset() => _offset = 0f;

        internal static GameOptionsMenu _trackedMenu;

        public static void Track(GameOptionsMenu menu) => _trackedMenu = menu;

        public static void Tick(GameOptionsMenu menu)
        {
            if (RoleConfig.NativeMenuRows?.Value != true) return;
            if (menu == null || menu.transform == null) return;

            var firstChild = menu.transform.childCount > 0 ? menu.transform.GetChild(0) : null;
            if (firstChild != _trackedFirstChild)
            {
                _offset = 0f;
                _trackedFirstChild = firstChild;
            }

            if (MarshAPI.UiScroll.FindNativeScroller(menu.transform) != null) return;

            if (!MarshAPI.UiWheel.Available) return;

            float wheel = MarshAPI.UiWheel.Scroll.y;
            if (Mathf.Abs(wheel) < 0.01f) return;

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
