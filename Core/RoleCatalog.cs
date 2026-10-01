using System;
using UnityEngine;

namespace TownOfRoles.Core
{
    // Single source of truth for every custom role's presentation metadata: the under-name
    // display name, its color, the flavor description, and the task-stats text shown on the
    // tasks tab ("Your Role: X, ...").
    internal readonly struct RoleDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly RoleTeamTypes Team;
        public readonly Color Color;
        public readonly string Description;
        public readonly string TaskText;

        public RoleDef(string id, string name, RoleTeamTypes team, Color color, string description, string taskText)
        {
            Id = id;
            Name = name;
            Team = team;
            Color = color;
            Description = description;
            TaskText = taskText;
        }
    }

    internal static class RoleCatalog
    {
        public static readonly RoleDef[] All =
        {
            // Crewmate
            new("townofroles.Sheriff",      "Sheriff",      RoleTeamTypes.Crewmate,
                new Color(0.95f, 0.8f, 0.2f, 1f),   "Shoot the impostors",
                "Shoot Impostors. Shooting a non-enemy may kill you."),
            new("townofroles.Engineer",     "Engineer",     RoleTeamTypes.Crewmate,
                new Color(0.2f, 0.85f, 0.95f, 1f),   "Use vents to move around the map.",
                "Fix sabotages from anywhere and use vents."),
            new("townofroles.Medic",        "Medic",        RoleTeamTypes.Crewmate,
                new Color(0.3f, 0.95f, 0.55f, 1f),   "Protect one player from a kill.",
                "Shield a player from one kill and learn the killer's clues."),
            new("townofroles.Seer",         "Seer",         RoleTeamTypes.Crewmate,
                new Color(0.65f, 0.45f, 1f, 1f),     "Investigate players to reveal their faction.",
                "Investigate other players to reveal their faction or role."),
            new("townofroles.Vigilante",    "Vigilante",    RoleTeamTypes.Crewmate,
                new Color(0.95f, 0.65f, 0.25f, 1f),  "Shoot an Impostor; shooting a Crewmate kills you.",
                "Shoot a player during meetings with limited shots."),
            new("townofroles.Altruist",     "Altruist",     RoleTeamTypes.Crewmate,
                new Color(0.95f, 0.5f, 0.72f, 1f),   "Revive a dead body — at the cost of your own life.",
                "Revive a dead body — at the cost of your own life."),
            new("townofroles.Mayor",        "Mayor",        RoleTeamTypes.Crewmate,
                new Color(0.35f, 0.6f, 1f, 1f),      "Your vote counts double in meetings.",
                "Your vote counts for the Vote Bank."),
            new("townofroles.Swapper",      "Swapper",      RoleTeamTypes.Crewmate,
                new Color(0.45f, 0.8f, 0.3f, 1f),    "During meetings, swap the votes of two players.",
                "During meetings, swap the votes of two players."),
            new("townofroles.Spy",          "Spy",          RoleTeamTypes.Crewmate,
                new Color(0.85f, 0.7f, 0.35f, 1f),   "Get notified when someone vents or gets doused.",
                "Get intel on venting and dousing around the map."),
            new("townofroles.Investigator", "Investigator", RoleTeamTypes.Crewmate,
                new Color(0.35f, 0.9f, 0.75f, 1f),   "See the footprints of other players.",
                "See the footprints of other players."),
            new("townofroles.TimeLord",     "Time Lord",    RoleTeamTypes.Crewmate,
                new Color(0.55f, 0.65f, 0.95f, 1f),  "Rewind time to undo player movement.",
                "Rewind time to undo player movement."),
            new("townofroles.Snitch",       "Snitch",       RoleTeamTypes.Crewmate,
                new Color(0.95f, 0.85f, 0.35f, 1f),  "Find the Impostors once your tasks are done.",
                "Complete all your tasks to reveal the Impostors with arrows."),

            // Impostor
            new("townofroles.Assassin",     "Assassin",     RoleTeamTypes.Impostor,
                new Color(0.95f, 0.15f, 0.18f, 1f),  "During meetings, guess another player's role. A correct guess kills them; a wrong guess kills you.",
                "Guess another player's role during a meeting. Wrong guesses kill you."),
            new("townofroles.Janitor",      "Janitor",      RoleTeamTypes.Impostor,
                new Color(0.55f, 0.72f, 0.95f, 1f),  "Clean dead bodies so they cannot be reported.",
                "Clean dead bodies so they cannot be reported."),
            new("townofroles.Morphling",    "Morphling",    RoleTeamTypes.Impostor,
                new Color(0.6f, 0.9f, 0.4f, 1f),     "Copy another player's appearance for a few seconds.",
                "Morph into another player's appearance for a short time."),
            new("townofroles.Camouflager",  "Camouflager",  RoleTeamTypes.Impostor,
                new Color(0.55f, 0.6f, 0.95f, 1f),   "Turn everyone grey so identities are hidden.",
                "Turn everyone grey so identities are hidden."),
            new("townofroles.Swooper",      "Swooper",      RoleTeamTypes.Impostor,
                new Color(0.4f, 0.45f, 0.6f, 1f),    "Become invisible for a short time.",
                "Become temporarily invisible."),
            new("townofroles.Underdog",     "Underdog",     RoleTeamTypes.Impostor,
                new Color(0.9f, 0.4f, 0.4f, 1f),     "Faster kills when the Impostors are outnumbered.",
                "Your kill cooldown is reduced while outnumbered."),
            new("townofroles.Undertaker",   "Undertaker",   RoleTeamTypes.Impostor,
                new Color(0.45f, 0.45f, 0.75f, 1f),  "Drag dead bodies away so they cannot be reported.",
                "Drag dead bodies away so they cannot be reported."),
            new("townofroles.Miner",        "Miner",        RoleTeamTypes.Impostor,
                new Color(0.85f, 0.5f, 0.2f, 1f),    "Mine vents that connect only to each other.",
                "Mine vents that connect only to each other to move around the map."),

            // Neutral
            new("townofroles.Jester",       "Jester",       RoleTeamTypes.Neutral,
                new Color(0.86f, 0.35f, 0.95f, 1f),  "Get yourself voted out to win.",
                "Get yourself voted out to win."),
            new("townofroles.Executioner",  "Executioner",  RoleTeamTypes.Neutral,
                new Color(0.45f, 0.9f, 0.85f, 1f),   "Get your target voted out to win.",
                "Get your target voted out to win. If your target dies another way, you convert."),
            new("townofroles.Arsonist",     "Arsonist",     RoleTeamTypes.Neutral,
                new Color(1f, 0.45f, 0.15f, 1f),     "Douse players, then ignite them all to win.",
                "Douse everyone, then ignite to win."),
            new("townofroles.Phantom",      "Phantom",      RoleTeamTypes.Neutral,
                new Color(0.75f, 0.75f, 0.85f, 1f),  "Complete all your tasks after death to win.",
                "Complete all your tasks after death to win."),
            new("townofroles.Shifter",      "Shifter",      RoleTeamTypes.Neutral,
                new Color(0.75f, 0.55f, 0.95f, 1f),  "Swap roles and tasks with other players.",
                "Swap roles and tasks with another player. Shifting an Impostor kills you."),
            new("townofroles.Glitch",       "The Glitch",   RoleTeamTypes.Neutral,
                new Color(0.45f, 0.95f, 0.35f, 1f),  "Mimic, hack, and kill everyone to be the last one standing.",
                "Mimic players, hack them, and kill everyone to be the last one standing."),
            // Lovers last on purpose: this table decides the under-name label, and
            // the overlay must only be shown for a player who has no role of their
            // own (TryGet returns the first match).
            new("townofroles.Lovers",       "Lover",        RoleTeamTypes.Neutral,
                new Color(1f, 0.4f, 0.8f, 1f),       "You are in love. Keep each other alive and win together.",
                "Protect your lover \u2014 you win or die together."),
        };

        // The settings/RPC key for a role: the catalog id without its "townofroles." prefix
        // (townofroles.TimeLord → TimeLord).
        public static string KeyOf(in RoleDef def)
        {
            const string prefix = "townofroles.";
            if (def.Id == null) return null;
            return def.Id.StartsWith(prefix, StringComparison.Ordinal)
                ? def.Id.Substring(prefix.Length)
                : def.Id;
        }

        // The settings key for a role *id* (townofroles.TimeLord → TimeLord), or null when the
        // id is not in the catalog.
        public static string KeyOfId(string id)
        {
            if (id == null) return null;
            foreach (var def in All)
                if (def.Id == id)
                    return KeyOf(def);
            return null;
        }

        public static string TaskTextFor(string roleName)
        {
            foreach (var def in All)
                if (def.Name == roleName)
                    return def.TaskText;
            return "Complete your tasks and survive.";
        }
    }
}
