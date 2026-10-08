using System;
using System.Collections.Generic;
using System.Globalization;
using MarshAPI;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class NativeSettingsRows
    {
        private const string RowPrefix = "ToR_Row_";

        private readonly struct RoleRow
        {
            public readonly string Key;
            public readonly string Display;
            public readonly Color Color;

            public RoleRow(string key, string display, Color color)
            {
                Key = key;
                Display = display;
                Color = color;
            }
        }

        private static SettingMenu _menu;

        public static void Inject(SettingMenu menu)
        {
            if (RoleConfig.NativeMenuRows?.Value != true) return;
            if (menu == null || menu.menu == null || menu.menu.transform == null) return;

            try
            {
                var parent = menu.menu.transform;

                SettingRows.DestroyRows(parent, RowPrefix);
                _menu = menu;

                if (SettingRows.FindRowPrefab(menu) == null) return;

                float y = UiLayout.RowAnchor(menu);
                float step = UiLayout.RowPitch(menu);

                var roles = BuildRows();
                int added = 0;
                for (int i = 0; i < roles.Count; i++)
                {
                    var role = roles[i];
                    y -= step;
                    BuildCountRow(parent, role, y);
                    added++;

                    if (RoleSettingsSync.GetInt(role.Key + ".Count") < 1) continue;
                    if (!RoleOptionSpecs.TryGet(role.Key, out var extras)) continue;

                    for (int e = 0; e < extras.Length; e++)
                    {
                        y -= step;
                        BuildConfigRow(parent, role.Key + "." + extras[e].Field,
                            extras[e].Label, extras[e].Kind, y);
                        added++;
                    }
                }

                UiScroll.EnsureRowsReachable(parent, added, step);
            }
            catch (Exception e)
            {
                Log("inject: " + e.Message);
            }
        }

        private static List<RoleRow> BuildRows()
        {
            var rows = new List<RoleRow>();
            var all = RoleCatalog.All;
            for (int i = 0; i < all.Length; i++)
            {
                var def = all[i];
                var key = RoleCatalog.KeyOf(def);
                if (!RoleConfig.IsEnabled(key)) continue;
                rows.Add(new RoleRow(key, def.Name, def.Color));
            }
            return rows;
        }

        private static void StepCount(string key, int delta)
        {
            if (!RoleSettingsSync.CanEdit) return;
            int current = RoleSettingsSync.GetInt(key + ".Count");
            int next = Mathf.Clamp(current + delta, 0, 15);
            RoleSettingsSync.SetInt(key + ".Count", next);
            Inject(_menu);
        }

        private static void BuildCountRow(Transform parent, RoleRow role, float y)
        {
            var name = RowPrefix + "Count_" + role.Key;
            SettingRows.AddRow(_menu, new SettingRowRequest
            {
                Parent = parent,
                Name = name,
                Label = role.Display,
                LabelColor = role.Color,
                Value = RoleSettingsSync.GetInt(role.Key + ".Count").ToString(CultureInfo.InvariantCulture),
                Y = y,
                OnMinus = () => StepCount(role.Key, -1),
                OnPlus = () => StepCount(role.Key, +1),
            });
        }

        private static void BuildConfigRow(Transform parent, string channel, string labelText, string kind, float y)
        {
            var name = RowPrefix + "Cfg_" + channel.Replace(".", "_");

            GameObject row = null;
            row = SettingRows.AddRow(_menu, new SettingRowRequest
            {
                Parent = parent,
                Name = name,
                Label = labelText,
                LabelColor = new Color(0.78f, 0.78f, 0.82f, 1f),
                Value = FormatValue(channel, kind),
                Y = y,
                OnMinus = () => StepConfig(channel, kind, row, -1),
                OnPlus = () => StepConfig(channel, kind, row, +1),
            });
        }

        private static string FormatValue(string channel, string kind)
        {
            switch (kind)
            {
                case "bool": return RoleSettingsSync.GetBool(channel) ? "On" : "Off";
                case "int": return RoleSettingsSync.GetInt(channel).ToString(CultureInfo.InvariantCulture);
                case "percent": return RoleSettingsSync.GetFloat(channel).ToString("0", CultureInfo.InvariantCulture) + "%";
                case "float": return RoleSettingsSync.GetFloat(channel).ToString("0.#", CultureInfo.InvariantCulture) + "s";
                default: return RoleSettingsSync.GetString(channel, "Faction");
            }
        }

        private static void StepConfig(string channel, string kind, GameObject row, int delta)
        {
            if (!RoleSettingsSync.CanEdit) return;

            switch (kind)
            {
                case "bool":
                    RoleSettingsSync.SetBool(channel, delta > 0);
                    break;
                case "int":
                    RoleSettingsSync.SetInt(channel, Mathf.Clamp(RoleSettingsSync.GetInt(channel) + delta, 0, 15));
                    break;
                case "percent":
                    RoleSettingsSync.SetFloat(channel, Mathf.Clamp(RoleSettingsSync.GetFloat(channel) + delta * 5f, 0f, 100f));
                    break;
                case "float":
                    RoleSettingsSync.SetFloat(channel, Mathf.Max(0f, RoleSettingsSync.GetFloat(channel) + delta));
                    break;
                default:
                    var current = RoleSettingsSync.GetString(channel, "Faction");
                    RoleSettingsSync.SetString(channel, current == "Faction" ? "Role"
                        : current == "Role" ? "Faction"
                        : current == "Jester" ? "Crewmate" : "Jester");
                    break;
            }

            if (row != null && SettingRows.TryGetRowTexts(row, out _, out var valueText) && valueText != null)
                valueText.text = FormatValue(channel, kind);

            RoleNativeOptions.PushChannel(channel);
        }

        private static void Log(string message) =>
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Native settings rows: " + message);
    }

    [HarmonyPatch(typeof(SettingMenu), nameof(SettingMenu.OnEnable))]
    internal static class SettingMenu_OnEnable_NativeSettingsRowsPatch
    {
        private static void Postfix(SettingMenu __instance)
        {
            try
            {
                SettingsScroll.Reset();
                SettingsScroll.Track(__instance.menu);
                NativeSettingsRows.Inject(__instance);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles")
                    .LogError("Native settings rows hook: " + e.Message);
            }
        }
    }
}
