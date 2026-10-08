using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TownOfRoles.Roles
{
    internal static class FreeplayRoleLayout
    {
        private const int Columns = 4;
        private const float ColumnSpacing = 0.82f;
        private const float RowSpacing = 0.68f;

        internal static void Reflow(TaskAdderGame game)
        {
            if (game == null || game.ActiveItems == null) return;

            var roleButtons = new List<Transform>();
            for (int i = 0; i < game.ActiveItems.Count; i++)
            {
                var item = game.ActiveItems[i];
                if (item == null) continue;
                var button = item.GetComponent<TaskAddButton>();
                if (button != null && button.IsRole) roleButtons.Add(item);
            }

            if (roleButtons.Count == 0) return;

            roleButtons.Sort((a, b) =>
            {
                var yCompare = b.localPosition.y.CompareTo(a.localPosition.y);
                return yCompare != 0 ? yCompare : a.localPosition.x.CompareTo(b.localPosition.x);
            });

            var origin = roleButtons[0].localPosition;
            origin.x -= ((Math.Min(Columns, roleButtons.Count) - 1) * ColumnSpacing) * 0.5f;

            for (int i = 0; i < roleButtons.Count; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var target = roleButtons[i];
                target.localPosition = new Vector3(
                    origin.x + column * ColumnSpacing,
                    origin.y - row * RowSpacing,
                    target.localPosition.z);
            }

            var scroller = game.scroller;
            if (scroller != null)
            {
                try
                {
                    scroller.CalculateAndSetYBounds(roleButtons.Count, Columns, 3.95f, RowSpacing);
                    scroller.SetYBoundsMin(0f);
                }
                catch (Exception e)
                {
                    BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogWarning("Freeplay role scroller resize: " + e.Message);
                }
            }
        }
    }

    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.OpenRoleFolder))]
    [HarmonyPriority(Priority.Last)]
    internal static class TaskAdderGame_OpenRoleFolder_TownOfRolesLayoutPatch
    {
        private static void Postfix(TaskAdderGame __instance)
        {
            try { FreeplayRoleLayout.Reflow(__instance); }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Freeplay role layout: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ApplyClickMask))]
    [HarmonyPriority(Priority.Last)]
    internal static class TaskAdderGame_ApplyClickMask_TownOfRolesLayoutPatch
    {
        private static void Postfix(TaskAdderGame __instance)
        {
            try { FreeplayRoleLayout.Reflow(__instance); }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Freeplay role layout refresh: " + e.Message);
            }
        }
    }
}
