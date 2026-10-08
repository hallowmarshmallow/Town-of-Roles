using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TownOfRoles.Core
{
    internal static class BootTrace
    {
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("TOR-LOGGER");

        private const string StageFileName = "TownOfRoles.bootstage";
        private const string SkipFileName = "TownOfRoles.crashskip";

        private static string StagePath => Path.Combine(Paths.ConfigPath, StageFileName);
        private static string SkipPath => Path.Combine(Paths.ConfigPath, SkipFileName);

        private static readonly HashSet<string> _crashSkips = new();

        public static void Mark(string label) => Log.LogInfo(label);

        public static string BuildStamp()
        {
            try
            {
                string path = typeof(BootTrace).Assembly.Location;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "in-memory";

                var file = new FileInfo(path);
                return file.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss") + "Z, " + file.Length + " bytes";
            }
            catch
            {
                return "unknown";
            }
        }

        public static void Run(string stage, Action body)
        {
            if (body == null) return;

            if (_crashSkips.Contains(stage))
            {
                Log.LogWarning($"[{stage}] left out: it killed an earlier launch of this build. " +
                               $"Delete BepInEx/config/{SkipFileName} to try it again.");
                return;
            }

            try { File.WriteAllText(StagePath, stage); } catch { }

            body();
        }

        public static void RecoverCrashes()
        {
            try
            {
                LoadSkips();

                if (!File.Exists(StagePath)) return;

                string stage = File.ReadAllText(StagePath).Trim();
                TryDelete(StagePath);
                if (stage.Length == 0) return;

                Log.LogError($"The previous launch died during [{stage}] " +
                             $"(marker: BepInEx/config/{StageFileName}). That step is the crash, " +
                             "not the step that would have followed it.");

                if (!_crashSkips.Add(stage))
                {
                    Log.LogWarning($"[{stage}] had already killed an earlier launch and is still " +
                                   $"failing; the crash is not confined to it. See " +
                                   $"BepInEx/config/{SkipFileName}.");
                    return;
                }

                SaveSkips();
                Log.LogWarning($"[{stage}] is left out from now on so the game can start. Fix it and " +
                               $"delete BepInEx/config/{SkipFileName}; the list is keyed to this " +
                               "build, so a rebuild re-tests everything.");
            }
            catch
            {
            }
        }

        public static void Complete()
        {
            TryDelete(StagePath);
        }

        private static void LoadSkips()
        {
            if (!File.Exists(SkipPath)) return;

            var lines = File.ReadAllLines(SkipPath);
            if (lines.Length == 0 || lines[0].Trim() != BuildStamp()) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string stage = lines[i].Trim();
                if (stage.Length > 0) _crashSkips.Add(stage);
            }

            if (_crashSkips.Count > 0)
                Log.LogWarning($"{_crashSkips.Count} boot step(s) that killed an earlier launch are " +
                               $"left out: {string.Join(", ", _crashSkips)} " +
                               $"(BepInEx/config/{SkipFileName}).");
        }

        private static void SaveSkips()
        {
            try
            {
                var text = new StringBuilder();
                text.Append(BuildStamp()).Append('\n');
                foreach (var stage in _crashSkips.OrderBy(s => s, StringComparer.Ordinal))
                    text.Append(stage).Append('\n');

                File.WriteAllText(SkipPath, text.ToString());
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
        internal static class M2_AmongUsClient_Awake
        {
            private static void Postfix() => BootTrace.Mark("AmongUsClient.Awake");
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        internal static class M3_MainMenuManager_Start
        {
            private static void Postfix() => BootTrace.Mark("MainMenuManager.Start");
        }

        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        internal static class M4_VersionShower_Start
        {
            private static void Postfix() => BootTrace.Mark("VersionShower.Start");
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        internal static class M5_GameStartManager_Start
        {
            private static void Postfix() => BootTrace.Mark("GameStartManager.Start");
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        internal static class M6_HudManager_Start
        {
            private static void Postfix() => BootTrace.Mark("HudManager.Start");
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        internal static class M7_MeetingHud_Start
        {
            private static void Postfix() => BootTrace.Mark("MeetingHud.Start");
        }
    }
}
