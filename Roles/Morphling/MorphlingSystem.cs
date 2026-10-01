using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using UnityEngine;
using TownOfRoles.Core;

namespace TownOfRoles.Roles.Morphling
{
    // Morphling gameplay logic (ported from Town-Of-Us' Morphling.cs).
    internal static class MorphlingSystem
    {
        private const string MorphRpc = "townofroles.MorphlingMorph";
        private const string RequestMorphRpc = "townofroles.MorphlingRequestMorph";
        private const string RevertRpc = "townofroles.MorphlingRevert";
        private const string RequestSampleRpc = "townofroles.MorphlingRequestSample";

        private static readonly Dictionary<byte, DateTime> MorphUntil = new();
        private static readonly Dictionary<byte, DateTime> Cooldowns = new();
        private static readonly Dictionary<byte, Outfit> OriginalOutfit = new();
        private static readonly Dictionary<byte, Outfit> Samples = new();

        // Everything the morph copies, and everything the revert restores.
        private sealed class Outfit
        {
            public string Name;
            public int Color;
            public string HatId;
            public string SkinId;
            public string PetId;
        }

        // Reads a player's full current outfit from GameData (authoritative).
        private static Outfit ReadOutfit(PlayerControl player)
        {
            var data = player?.Data;
            if (data == null) return new Outfit();
            return new Outfit
            {
                Name = data.PlayerName,
                Color = data.ColorId,
                HatId = data.HatId,
                SkinId = data.SkinId,
                PetId = data.PetId,
            };
        }

        public static bool IsMorphling(PlayerControl player) =>
            player != null && player.Data != null && RoleRegistry.IsAssigned(player, MorphlingRole.Id);

        // True once this Morphling has sampled a player's DNA.
        public static bool HasSample(PlayerControl morphling) =>
            morphling != null && Samples.ContainsKey(morphling.PlayerId);

        internal static bool CanSampleNow(PlayerControl morphling)
        {
            if (!IsMorphling(morphling) || morphling.Data == null || morphling.Data.IsDead) return false;
            return DateTime.UtcNow >= GetCooldown(morphling.PlayerId) &&
                   ClosestPlayerFinder.GetClosestTarget(morphling, out _);
        }

        internal static bool CanMorphNow(PlayerControl morphling)
        {
            if (!HasSample(morphling)) return false;
            return IsMorphling(morphling) && !morphling.Data.IsDead &&
                   DateTime.UtcNow >= GetCooldown(morphling.PlayerId);
        }

        public static void TrySample(PlayerControl morphling)
        {
            var client = AmongUsClient.Instance;
            if (client == null || morphling == null || morphling.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestSampleRpc, morphling.PlayerId);
                return;
            }
            if (!CanSampleNow(morphling)) return;
            if (!ClosestPlayerFinder.GetClosestTarget(morphling, out var target)) return;
            if (target.Data == null) return;

            // DNA is the whole outfit: a morph that changes only the name and color
            // leaves the sample's hat, skin and pet behind, the tell that broke it.
            Samples[morphling.PlayerId] = ReadOutfit(target);
            // Sampling shares the morph cooldown so you cannot sample-and-morph
            // in the same instant (old TOU behavior).
            Cooldowns[morphling.PlayerId] =
                DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.MorphlingMorphCooldown, 15f));
        }

        public static void TryMorph(PlayerControl morphling)
        {
            var client = AmongUsClient.Instance;
            if (client == null || morphling == null || morphling.Data == null) return;
            if (!client.AmHost)
            {
                TownOfRolesRpcMux.Send(RequestMorphRpc, morphling.PlayerId);
                return;
            }
            if (!CanMorphNow(morphling)) return;
            if (!Samples.TryGetValue(morphling.PlayerId, out var dna)) return;

            // Cache the original outfit BEFORE anything changes (RpcSetName
            // mutates Data.PlayerName, so reading it back later is wrong).
            var own = ReadOutfit(morphling);
            OriginalOutfit[morphling.PlayerId] = own;
            MorphUntil[morphling.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.MorphlingMorphDuration, 10f));
            Cooldowns[morphling.PlayerId] = DateTime.UtcNow.AddSeconds(RoleConfig.Seconds(RoleConfig.MorphlingMorphCooldown, 15f));

            ApplyOutfit(morphling, dna);
            TownOfRolesRpcMux.Send(MorphRpc, morphling.PlayerId, dna.Name, dna.Color, dna.HatId, dna.SkinId, dna.PetId);
        }

        // Host tick: revert any morph that has expired (from the cached outfit).
        public static void Tick()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            if (MorphUntil.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var key in new List<byte>(MorphUntil.Keys))
            {
                if (now < MorphUntil[key]) continue;
                var morphling = FindPlayer(key);
                if (morphling == null || morphling.Data == null) continue;

                MorphUntil.Remove(key);
                var own = OriginalOutfit.TryGetValue(key, out var cached)
                    ? cached
                    : ReadOutfit(morphling);
                OriginalOutfit.Remove(key);

                ApplyOutfit(morphling, own);
                TownOfRolesRpcMux.Send(RevertRpc, key, own.Name, own.Color, own.HatId, own.SkinId, own.PetId);
                return; // one revert per tick is plenty
            }
        }

        // Renders a whole outfit onto one player, on this client only.
        private static void ApplyOutfit(PlayerControl player, Outfit outfit)
        {
            if (player == null || outfit == null || player.Data == null) return;

            try
            {
                var data = player.Data;
                if (!string.IsNullOrEmpty(outfit.Name)) data.PlayerName = outfit.Name;
                data.ColorId = outfit.Color;
                if (outfit.HatId != null) data.HatId = outfit.HatId;
                if (outfit.SkinId != null) data.SkinId = outfit.SkinId;
                if (outfit.PetId != null) data.PetId = outfit.PetId;

                player.RawSetOutfit(data);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles")
                    .LogError("Morphling outfit: " + e.Message);
            }
        }

        private static Outfit OutfitFrom(string name, int color, string hatId, string skinId, string petId) =>
            new() { Name = name, Color = color, HatId = hatId, SkinId = skinId, PetId = petId };

        [ReactorRpc(RequestSampleRpc)]
        private static void OnRequestSample(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TrySample(player);
                    return;
                }
            }
        }

        [ReactorRpc(RequestMorphRpc)]
        private static void OnRequestMorph(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.PlayerId != playerId) continue;
                var owner = player.GetClient();
                if (owner != null && owner.Id == senderId)
                {
                    TryMorph(player);
                    return;
                }
            }
        }

        [ReactorRpc(MorphRpc)]
        private static void OnMorph(byte senderId, byte morphlingId, string targetName, int targetColor,
            string targetHatId, string targetSkinId, string targetPetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var morphling = FindPlayer(morphlingId);
            if (morphling == null) return;
            // The payload is the outfit, not a nudge: render it here. A client that
            // missed the broadcast is covered by the same path, because the host
            // re-sends on morph and every client applies what it was sent.
            ApplyOutfit(morphling, OutfitFrom(targetName, targetColor, targetHatId, targetSkinId, targetPetId));
        }

        [ReactorRpc(RevertRpc)]
        private static void OnRevert(byte senderId, byte morphlingId, string ownName, int ownColor,
            string ownHatId, string ownSkinId, string ownPetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            var morphling = FindPlayer(morphlingId);
            if (morphling == null) return;
            // Restore from the payload, not from PlayerInfo: the fields were overwritten
            // by the morph on this client, so Data is the morph's outfit, not the player's.
            ApplyOutfit(morphling, OutfitFrom(ownName, ownColor, ownHatId, ownSkinId, ownPetId));
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (player != null && player.PlayerId == playerId) return player;
            return null;
        }

        // Seconds until this Morphling may sample or morph again, zero when ready.
        public static float SecondsRemaining(PlayerControl morphling) => SecondsLeft(Cooldowns, morphling);

        // Seconds this Morphling's shift still runs for, zero when they are themselves.
        public static float ShiftSecondsRemaining(PlayerControl morphling) => SecondsLeft(MorphUntil, morphling);

        private static float SecondsLeft(Dictionary<byte, DateTime> table, PlayerControl player)
        {
            if (player == null || player.Data == null) return 0f;
            if (!table.TryGetValue(player.PlayerId, out var until)) return 0f;
            var left = (until - DateTime.UtcNow).TotalSeconds;
            return left > 0 ? (float)left : 0f;
        }

        private static DateTime GetCooldown(byte morphlingId) =>
            Cooldowns.TryGetValue(morphlingId, out var value) ? value : DateTime.MinValue;

        public static void Reset()
        {
            MorphUntil.Clear();
            Cooldowns.Clear();
            OriginalOutfit.Clear();
            Samples.Clear();
        }

        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
        public static void OnMeetingStarted(MeetingEventArgs _) => Reset();
    }

}
