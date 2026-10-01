using System.Collections.Generic;
using MarshAPI;
using UnityEngine;

namespace TownOfRoles.Core
{
    // Names the winners of a modded round to the game's own end screen. The screen builds its
    // winner art from TempData.customWinners, so the list is published here, when the win
    // fires, rather than from the end screen: by then the game has already drawn its own
    // placeholder players, and a list handed over that late never replaces them.
    internal static class ModdedWin
    {
        private static readonly List<byte> Winners = new();

        // Forgotten at every round boundary, so one round's winners cannot leak.
        public static void Clear() => Winners.Clear();

        // Records the players who won and hands the game the list it draws. Called at the
        // moment the round is ended, on the host and on every client that hears the win.
        public static void Declare(params PlayerControl[] winners)
        {
            Winners.Clear();
            if (winners == null) return;

            for (int i = 0; i < winners.Length; i++)
            {
                var player = winners[i];
                if (player == null || player.Data == null) continue;
                if (!Winners.Contains(player.PlayerId)) Winners.Add(player.PlayerId);
            }

            var list = BuildList();
            if (list != null) ModdedGameOver.SetWinners(list);
        }

        // The recorded winners in the shape the end screen takes, or null when none is left.
        private static Il2CppSystem.Collections.Generic.List<WinningPlayerData> BuildList()
        {
            if (Winners.Count == 0) return null;

            var list = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();

            for (int i = 0; i < Winners.Count; i++)
            {
                var data = FindInfo(Winners[i]);
                if (data == null) continue;

                var entry = new WinningPlayerData
                {
                    Id = data.PlayerId,
                    Name = data.PlayerName,
                    IsDead = data.IsDead,
                    ColorId = data.ColorId,
                    HatId = data.HatId,
                    SkinId = data.SkinId,
                    PetId = data.PetId,
                    // The body colour the screen tints its player art with. The role's own
                    // colour, so a Jester wins purple rather than in whatever the game
                    // defaults a Custom result to.
                    TeamColor = RoleColour(data),
                    // A modded winner is never an impostor: IsImpostor is what picks the red
                    // ghost, and none of these roles are impostors.
                    IsImpostor = false,
                    IsYou = PlayerControl.LocalPlayer != null &&
                            PlayerControl.LocalPlayer.PlayerId == data.PlayerId,
                };

                list.Add(entry);
            }

            return list.Count > 0 ? list : null;
        }

        // The winning role's colour, or white when the player's role cannot be read.
        private static Color RoleColour(GameData.PlayerInfo data)
        {
            var player = FindPlayer(data.PlayerId);
            if (player == null) return Color.white;

            try
            {
                return RolePresentation.TryGet(player, out _, out var colour) ? colour : Color.white;
            }
            catch
            {
                return Color.white;
            }
        }

        private static GameData.PlayerInfo FindInfo(byte playerId)
        {
            var player = FindPlayer(playerId);
            return player != null ? player.Data : null;
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.Data != null && player.PlayerId == playerId) return player;
            return null;
        }
    }
}
