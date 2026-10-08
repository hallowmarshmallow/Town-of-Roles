using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using TownOfRoles.Roles.Assassin;
using TownOfRoles.Roles.Camouflager;
using TownOfRoles.Roles.Modifiers;
using TownOfRoles.Roles.Seer;

namespace TownOfRoles.Core
{
    internal static class PresentationPatches
    {
        internal static float NextUpdate { get; set; }
        internal static float NextMeetingUpdate { get; set; }
        private static bool _loggedMissingStates;

        internal static void UpdateWorld()
        {
            if (RoleConfig.PresentationEnabled?.Value != true) return;
            var viewer = PlayerControl.LocalPlayer;
            if (viewer == null || viewer.Data == null) return;

            if (SessionDisable.IsOff)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.nameText == null) continue;
                    RestoreIfInjected(player.nameText, player.Data.PlayerName);
                }
                return;
            }

            if (CamouflagerSystem.IsActive)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.nameText == null) continue;
                    SetText(player.nameText, string.Empty);
                }
                return;
            }

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.nameText == null) continue;
                if (SeerSystem.TryGetReveal(viewer, player, out var revealText, out var revealColor))
                {
                    SetText(player.nameText, RolePresentation.WithRole(player.Data.PlayerName, revealText));
                    SetColor(player.nameText, revealColor);
                }
                else if (RolePresentation.TryGet(player, out var roleName, out var roleColor) && RolePresentation.CanSee(viewer, player))
                {
                    if (player == viewer)
                    {
                        var mods = ModifierSystem.NamesFor(player.PlayerId);
                        if (mods.Length > 0)
                            roleName = roleName.Length > 0 ? roleName + " [" + mods + "]" : mods;
                    }
                    SetText(player.nameText, RolePresentation.WithRole(player.Data.PlayerName, roleName));
                    SetColor(player.nameText, roleColor);
                }
                else
                {
                    RestoreIfInjected(player.nameText, player.Data.PlayerName);
                }
            }
        }

        internal static void UpdateMeeting(MeetingHud meeting)
        {
            if (meeting == null) return;

            if (SessionDisable.IsOff)
            {
                var switchedOff = GameReflection.GetPlayerStates(meeting);
                if (switchedOff == null) return;
                foreach (var area in switchedOff)
                {
                    if (area == null || area.NameText == null) continue;
                    var target = FindPlayer(area.TargetPlayerId);
                    if (target == null || target.Data == null) continue;
                    RestoreIfInjected(area.NameText, target.Data.PlayerName);
                }
                return;
            }

            var states = GameReflection.GetPlayerStates(meeting);
            if (states == null)
            {
                if (!_loggedMissingStates)
                {
                    _loggedMissingStates = true;
                    BepInEx.Logging.Logger.CreateLogSource("TownOfRoles")
                        .LogWarning("Meeting role names unavailable: MeetingHud.playerStates field not found (interop drift).");
                }
                return;
            }
            var viewer = PlayerControl.LocalPlayer;
            if (viewer == null || viewer.Data == null) return;

            var state = GameReflection.GetMeetingState(meeting);
            var showRoles = state == MeetingHud.VoteStates.Discussion || state == MeetingHud.VoteStates.NotVoted;

            foreach (var area in states)
            {
                if (area == null || area.NameText == null) continue;
                var target = FindPlayer(area.TargetPlayerId);
                if (target == null || target.Data == null) continue;

                if (showRoles && SeerSystem.TryGetReveal(viewer, target, out var revealText, out var revealColor))
                {
                    SetText(area.NameText, RolePresentation.WithRole(target.Data.PlayerName, revealText));
                    SetColor(area.NameText, revealColor);
                    SetMeetingTypography(area.NameText, 1.18f);
                }
                else if (showRoles && RolePresentation.TryGet(target, out var roleName, out var roleColor) && RolePresentation.CanSee(viewer, target))
                {
                    if (target == viewer)
                    {
                        var mods = ModifierSystem.NamesFor(target.PlayerId);
                        if (mods.Length > 0)
                            roleName = roleName.Length > 0 ? roleName + " [" + mods + "]" : mods;
                    }
                    SetText(area.NameText, RolePresentation.WithRole(target.Data.PlayerName, roleName));
                    SetColor(area.NameText, roleColor);
                    SetMeetingTypography(area.NameText, 1.18f);
                }
                else
                {
                    RestoreIfInjected(area.NameText, target.Data.PlayerName);
                    SetMeetingTypography(area.NameText, 1f);
                }
            }
        }

        internal static void Reset()
        {
            NextUpdate = 0f;
            NextMeetingUpdate = 0f;
        }

        private static void SetText(object renderer, string value)
        {
            if (renderer == null) return;
            var type = renderer.GetType();
            var property = type.GetProperty("text") ?? type.GetProperty("Text");
            if (property != null && property.CanWrite)
            {
                try { property.SetValue(renderer, value, null); } catch { }
            }
        }

        private static void RestoreIfInjected(object renderer, string nativeName)
        {
            if (renderer == null) return;
            var type = renderer.GetType();
            var property = type.GetProperty("text") ?? type.GetProperty("Text");
            if (property == null || !property.CanRead || !property.CanWrite) return;
            try
            {
                var current = property.GetValue(renderer, null) as string;
                if (current != null && current.Contains("\n")) property.SetValue(renderer, nativeName, null);
            }
            catch { }
        }

        private static void SetMeetingTypography(object renderer, float scale)
        {
            try
            {
                var component = renderer as UnityEngine.Component;
                if (component != null) component.transform.localScale = Vector3.one * scale;

                var type = renderer?.GetType();
                var richText = type?.GetProperty("richText") ?? type?.GetProperty("RichText");
                if (richText != null && richText.CanWrite) richText.SetValue(renderer, true, null);

                var autoSize = type?.GetProperty("enableAutoSizing") ?? type?.GetProperty("EnableAutoSizing");
                if (autoSize != null && autoSize.CanWrite) autoSize.SetValue(renderer, false, null);

            }
            catch { }
        }

        private static void SetColor(object renderer, Color value)
        {
            if (renderer == null) return;
            var type = renderer.GetType();
            var property = type.GetProperty("color") ?? type.GetProperty("Color");
            if (property != null && property.CanWrite)
            {
                try { property.SetValue(renderer, value, null); } catch { }
            }
        }

        private static PlayerControl FindPlayer(byte id)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == id) return player;
            return null;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    internal static class HudManager_Update_PresentationPatch
    {
        private static void Postfix(HudManager __instance)
        {
            if (RoleConfig.PresentationEnabled?.Value != true) return;
            if (Time.unscaledTime < PresentationPatches.NextUpdate) return;
            PresentationPatches.NextUpdate = Time.unscaledTime + 0.1f;
            try
            {
                PresentationPatches.UpdateWorld();
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Role presentation update: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    internal static class MeetingHud_Update_PresentationPatch
    {
        private static void Postfix(MeetingHud __instance)
        {
            if (RoleConfig.PresentationEnabled?.Value != true) return;
            if (Time.unscaledTime < PresentationPatches.NextMeetingUpdate) return;
            PresentationPatches.NextMeetingUpdate = Time.unscaledTime + 0.1f;
            try
            {
                PresentationPatches.UpdateMeeting(__instance);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Meeting role presentation update: " + e.Message);
            }
        }
    }
}
