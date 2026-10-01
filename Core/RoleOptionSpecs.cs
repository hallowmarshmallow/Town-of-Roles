using System;
using System.Collections.Generic;

namespace TownOfRoles.Core
{
    // The single table describing every extra role setting the mod exposes: one row per role,
    // keyed by the role's config key, listing the fields that sit under it while the role's
    // count is at least 1.
    internal static class RoleOptionSpecs
    {
        public static readonly Dictionary<string, (string Field, string Label, string Kind)[]> ByRole = new()
        {
            ["Sheriff"] = new[]
            {
                ("KillCooldown", "Kill Cooldown", "float"),
                ("KillOther", "Kill Other", "bool"),
                ("BodyReport", "Report Own Body", "bool"),
            },
            ["Engineer"] = new[] { ("FixCooldown", "Fix Cooldown", "float") },
            ["Medic"] = new[]
            {
                ("Uses", "Shields", "int"),
                ("Cooldown", "Cooldown", "float"),
                ("ShieldBreaksOnKill", "Breaks On Kill", "bool"),
            },
            ["Seer"] = new[]
            {
                ("Uses", "Investigations", "int"),
                ("Cooldown", "Cooldown", "float"),
                ("RevealMode", "Reveal", "string"),
            },
            ["Vigilante"] = new[]
            {
                ("Shots", "Shots", "int"),
                ("Cooldown", "Cooldown", "float"),
            },
            ["Assassin"] = new[]
            {
                ("MultiKill", "Multi-Kill", "bool"),
                ("MeetingUi", "Meeting Buttons", "bool"),
            },
            ["Janitor"] = new[] { ("CleanCooldown", "Clean Cooldown", "float") },
            ["Altruist"] = new[]
            {
                ("Uses", "Revives", "int"),
                ("Cooldown", "Cooldown", "float"),
            },
            ["Mayor"] = new[] { ("VoteBank", "Vote Bank", "int") },
            ["Arsonist"] = new[] { ("DouseCooldown", "Douse Cooldown", "float") },
            ["Executioner"] = new[]
            {
                ("ConvertOnTargetDeath", "Convert On Target Death", "bool"),
                ("ConvertRole", "Convert To", "string"),
            },
            ["Lovers"] = new[]
            {
                ("BothDie", "Both Lovers Die", "bool"),
                ("ImpostorLover", "May Be An Impostor", "bool"),
            },
            ["Morphling"] = new[]
            {
                ("MorphCooldown", "Morph Cooldown", "float"),
                ("MorphDuration", "Morph Duration", "float"),
            },
            ["Camouflager"] = new[]
            {
                ("CamouflageCooldown", "Camouflage Cooldown", "float"),
                ("CamouflageDuration", "Camouflage Duration", "float"),
            },
            ["Swooper"] = new[]
            {
                ("SwoopCooldown", "Swoop Cooldown", "float"),
                ("SwoopDuration", "Swoop Duration", "float"),
            },
            ["Underdog"] = new[] { ("CooldownMultiplier", "Cooldown Multiplier", "float") },
            ["Undertaker"] = new[] { ("DragCooldown", "Drag Cooldown", "float") },
            ["Investigator"] = new[]
            {
                ("FootprintInterval", "Footprint Interval", "float"),
                ("FootprintDuration", "Footprint Duration", "float"),
            },
            ["TimeLord"] = new[]
            {
                ("RewindCooldown", "Rewind Cooldown", "float"),
                ("RewindSeconds", "Rewind Seconds", "float"),
            },
            ["Shifter"] = new[] { ("ShiftCooldown", "Shift Cooldown", "float") },
            ["Glitch"] = new[]
            {
                ("MimicCooldown", "Mimic Cooldown", "float"),
                ("MimicDuration", "Mimic Duration", "float"),
                ("HackCooldown", "Hack Cooldown", "float"),
                ("HackDuration", "Hack Duration", "float"),
                ("KillCooldown", "Kill Cooldown", "float"),
            },
            ["Miner"] = new[] { ("MineCooldown", "Mine Cooldown", "float") },
        };

        public static bool TryGet(string roleKey, out (string Field, string Label, string Kind)[] specs) =>
            ByRole.TryGetValue(roleKey ?? string.Empty, out specs);
    }
}
