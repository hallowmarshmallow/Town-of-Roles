using System.Collections.Generic;
using MarshAPI;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class ModdedWin
    {
        private static readonly List<byte> Winners = new();

        public static void Clear() => Winners.Clear();

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

                    TeamColor = RoleColour(data),

                    IsImpostor = false,
                    IsYou = PlayerControl.LocalPlayer != null &&
                            PlayerControl.LocalPlayer.PlayerId == data.PlayerId,
                };

                list.Add(entry);
            }

            return list.Count > 0 ? list : null;
        }

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
