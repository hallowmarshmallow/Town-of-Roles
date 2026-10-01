using System;
using System.Collections.Generic;
using ClassicUs.Reactor;
using MarshAPI;
using TownOfRoles.Core;

namespace TownOfRoles.Core
{
    // Cross-client log of every murder this round: (victim, killer, time).
    internal static class KillLog
    {
        public struct KillEntry
        {
            public byte Victim;
            public byte Killer;
            public DateTime Time;
        }

        private const string RpcKey = "townofroles.KillLogRecord";
        private static readonly List<KillEntry> Entries = new();

        public static IReadOnlyList<KillEntry> All => Entries;

        // Most recent record for a victim, if any.
        public static bool TryGetLatest(byte victimId, out KillEntry entry)
        {
            entry = default;
            var found = false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Victim != victimId) continue;
                if (!found || Entries[i].Time > entry.Time) { entry = Entries[i]; found = true; }
            }
            return found;
        }

        // GameEvents.AfterMurder hook, subscribed in TownOfRolesPlugin.Load().
        public static void OnAfterMurder(MurderEventArgs args)
        {
            if (args?.Target == null || args.Killer == null) return;
            Add(args.Target.PlayerId, args.Killer.PlayerId);
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost)
                TownOfRolesRpcMux.Send(RpcKey, args.Target.PlayerId, args.Killer.PlayerId);
        }

        private static void Add(byte victim, byte killer) =>
            Entries.Add(new KillEntry { Victim = victim, Killer = killer, Time = DateTime.UtcNow });

        [ReactorRpc(RpcKey)]
        private static void OnRecord(byte senderId, byte victim, byte killer)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost || senderId != client.HostId) return;
            // Skip if the local AfterMurder already recorded this exact kill.
            if (TryGetLatest(victim, out var existing) &&
                existing.Killer == killer &&
                (DateTime.UtcNow - existing.Time).TotalSeconds < 2.0) return;
            Add(victim, killer);
        }

        public static void Reset() => Entries.Clear();
        public static void OnGameStarted(GameStartedEventArgs _) => Reset();
        public static void OnGameEnded(GameEndedEventArgs _) => Reset();
    }
}
