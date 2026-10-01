using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace TownOfRoles.Roles.Engineer
{
    // The Engineer's vent button and vent outline draw in the impostor red.
    internal static class EngineerVentTint
    {
        // The game's enabled color, if the static read ever fails.
        private static readonly Color Fallback = new(0f, 1f, 1f, 1f);

        // Vent.SetOutline writes into the vent's material with these build names:
        // "_OutlineWidth" and "_Outline2Width" (SetFloat), "_OutlineColor" and
        // "_Outline2Color" (SetColor). The leading underscore is part of them, and SetColor
        // against a name the shader does not have is a silent no-op, so the underscore-less
        // spelling painted nothing.
        private const string OutlineProperty = "_OutlineColor";
        private const string Outline2Property = "_Outline2Color";

        private static bool EngineerCanVent =>
            PlayerControl.LocalPlayer != null &&
            PlayerControl.LocalPlayer.Data != null &&
            EngineerSystem.IsEngineer(PlayerControl.LocalPlayer) &&
            !PlayerControl.LocalPlayer.Data.IsDead;

        // Repaints the Use button while the local Engineer targets a vent. Runs from the
        // postfix on UseButtonManager.Update, after the game's own color write.
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
                // A static read across interop is best-effort; the game's own red
                // remains if any step throws.
            }
        }

        // The vent behind a use-button target, or null.
        private static Vent AsVent(IUsable target)
        {
            if (target is not Il2CppObjectBase obj) return null;
            try { return obj.TryCast<Vent>(); }
            catch (ArgumentException) { return null; }
        }

        // Rewrites the two outline colors of a vent with the game's vent cyan.
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

        // Backstop on the role tick clock, for a build where Harmony cannot resolve
        // UseButtonManager.Update. It is the wrong position in the frame, see the class
        // remarks, but a worse position still beats no tint.
        public static void Tick() => TintButton(UseButtonManager.Instance);

        // Reads one of UseButtonManager's private vent-color statics by name.
        private static Color VentColor(string fieldName)
        {
            var field = AccessTools.Field(typeof(UseButtonManager), fieldName);
            if (field == null) return Fallback;
            return field.GetValue(null) is Color c ? c : Fallback;
        }

        // The game's own use-button draw. Both halves of "the Engineer's vent is blue" are
        // decided here, after the game has written its color, so the two cannot disagree about
        // which frame they are in.
        [HarmonyPatch(typeof(UseButtonManager), "Update")]
        internal static class UseButtonManager_Update_EngineerTintPatch
        {
            private static void Postfix(UseButtonManager __instance)
            {
                TintButton(__instance);
                if (__instance != null) TintOutline(AsVent(__instance.CurrentTarget));
            }
        }

        // The hover frame itself, before the next use-button draw runs. Runs on the native
        // pass's own tail, so the red it just wrote is what this overwrites.
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
