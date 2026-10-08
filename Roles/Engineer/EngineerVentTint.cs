using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace TownOfRoles.Roles.Engineer
{
    internal static class EngineerVentTint
    {
        private static readonly Color Fallback = new(0f, 1f, 1f, 1f);

        private const string OutlineProperty = "_OutlineColor";
        private const string Outline2Property = "_Outline2Color";

        private static bool EngineerCanVent =>
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            EngineerSystem.IsEngineer(PlayerControl.LocalPlayer) &&
            !PlayerControl.LocalPlayer.Data.IsDead;

        private static void TintButton(UseButtonManager manager)
        {
            if (manager == null || !EngineerCanVent) return;

            try
            {
                var button = manager.UseButton;
                if (button == null) return;
                if (AsVent(manager.CurrentTarget) == null) return;

                var want = button.enabled ? VentColor("VentEnabledColor") : VentColor("VentDisabledColor");
                if (button.color != want) button.color = want;
            }
            catch
            {
            }
        }

        private static Vent AsVent(IUsable target)
        {
            if (target is not Il2CppObjectBase obj) return null;
            try { return obj.TryCast<Vent>(); }
            catch (ArgumentException) { return null; }
        }

        private static void TintOutline(Vent vent)
        {
            if (vent == null || !EngineerCanVent) return;

            try
            {
                var renderer = vent.GetComponent<Renderer>();
                var material = renderer != null ? renderer.material : null;
                if (material == null) return;

                var cyan = VentColor("VentEnabledColor");
                material.SetColor(OutlineProperty, cyan);
                material.SetColor(Outline2Property, cyan);
            }
            catch
            {
            }
        }

        public static void Tick() => TintButton(UseButtonManager.Instance);

        private static Color VentColor(string fieldName)
        {
            var field = AccessTools.Field(typeof(UseButtonManager), fieldName);
            if (field == null) return Fallback;
            return field.GetValue(null) is Color c ? c : Fallback;
        }

        [HarmonyPatch(typeof(UseButtonManager), "Update")]
        internal static class UseButtonManager_Update_EngineerTintPatch
        {
            private static void Postfix(UseButtonManager __instance)
            {
                TintButton(__instance);
                if (__instance != null) TintOutline(AsVent(__instance.CurrentTarget));
            }
        }

        [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
        internal static class Vent_SetOutline_EngineerTintPatch
        {
            private static void Postfix(Vent __instance, bool on, bool mainTarget)
            {
                if (!on || !mainTarget) return;
                TintOutline(__instance);
            }
        }
    }
}
