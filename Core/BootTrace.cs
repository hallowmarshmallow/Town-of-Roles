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
    // Startup crash diagnostics.
    //
    // A boot that dies natively writes no log line on its way out, so the log ends wherever the
    // previous step happened to get to and names nothing. What it can still leave behind is a
    // file, written before the step that kills it: Run(stage, body) records the stage, and the
    // next launch names it and leaves it out so the game starts.
    //
    // The stages wrap the steps that install Harmony patches or register roles - resolving a patch
    // target forces the game type's class init, which is where the process dies.
    internal static class BootTrace
    {
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("TOR-LOGGER");

        private const string StageFileName = "TownOfRoles.bootstage";
        private const string SkipFileName = "TownOfRoles.crashskip";

        // Where the step in progress is recorded, and where the steps that killed a launch are
        // remembered. BepInEx's config folder is always writable.
        private static string StagePath => Path.Combine(Paths.ConfigPath, StageFileName);
        private static string SkipPath => Path.Combine(Paths.ConfigPath, SkipFileName);

        // Stages that killed a launch of THIS build, left out from then on. A set, not one name:
        // two crashers in a row would otherwise trade places on every boot.
        private static readonly HashSet<string> _crashSkips = new();

        public static void Mark(string label) => Log.LogInfo(label);

        // The DLL's own identity - last-write time and size - read from the file on disk.
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

        // Runs one boot step under the marker. Returns without running it when a previous launch
        // of this build died inside it.
        public static void Run(string stage, Action body)
        {
            if (body == null) return;

            if (_crashSkips.Contains(stage))
            {
                Log.LogWarning($"[{stage}] left out: it killed an earlier launch of this build. " +
                               $"Delete BepInEx/config/{SkipFileName} to try it again.");
                return;
            }

            // Written and closed before the body runs: a native death flushes nothing, so this
            // file is the only thing left that can say where it happened.
            try { File.WriteAllText(StagePath, stage); } catch { }

            body();
        }

        // Called at the very start of Load(). A marker present at startup means the previous
        // process never reached Complete(), and names the step it died in.
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
                    // The skip did not save the launch: the death is either earlier than the step
                    // named here or in the boot path itself.
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
                // Recovery is best-effort: never let it stop the mod from loading.
            }
        }

        // Cleared by the last statement of Load(). A marker that outlives a launch is the crash
        // report, which is why nothing may run after this on the boot path.
        public static void Complete()
        {
            TryDelete(StagePath);
        }

        // The crasher list, valid only for the build that wrote it: the stamp is the first line,
        // so a rebuilt DLL is re-tested rather than inheriting the previous build's verdict.
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

        // M1 is logged from the end of Load(), so there is a marker even if every
        // scene patch below fails to resolve.

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

        // "Start" is private in the 2026.8.9 interop; string form matches the
        // role systems' MeetingHud patches.
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        internal static class M7_MeetingHud_Start
        {
            private static void Postfix() => BootTrace.Mark("MeetingHud.Start");
        }
    }
}
