using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Atomic;
using MarshAPI;
using HarmonyLib;
using TownOfRoles.Core;
using TownOfRoles.Commands;
using System;
using TownOfRoles.Roles.Engineer;
using TownOfRoles.Roles;
using TownOfRoles.Roles.Jester;
using TownOfRoles.Roles.Medic;
using TownOfRoles.Roles.Seer;
using TownOfRoles.Roles.Sheriff;
using TownOfRoles.Roles.Vigilante;
using TownOfRoles.Roles.Assassin;
using TownOfRoles.Roles.Janitor;
using TownOfRoles.Roles.Altruist;
using TownOfRoles.Roles.Mayor;
using TownOfRoles.Roles.Executioner;
using TownOfRoles.Roles.Arsonist;
using TownOfRoles.Roles.Swapper;
using TownOfRoles.Roles.Morphling;
using TownOfRoles.Roles.Spy;
using TownOfRoles.Roles.Modifiers;
using TownOfRoles.Roles.Camouflager;
using TownOfRoles.Roles.Swooper;
using TownOfRoles.Roles.Underdog;
using TownOfRoles.Roles.Undertaker;
using TownOfRoles.Roles.Investigator;
using TownOfRoles.Roles.TimeLord;
using TownOfRoles.Roles.Snitch;
using TownOfRoles.Roles.Phantom;
using TownOfRoles.Roles.Shifter;
using TownOfRoles.Roles.Glitch;
using TownOfRoles.Roles.Miner;
using TownOfRoles.Roles.Lovers;

namespace TownOfRoles
{

    [BepInPlugin(Guid, "Town Of Roles", Version)]
    [BepInDependency(AtomicPlugin.Guid)]
    [BepInDependency(MarshAPIPlugin.Guid)]
    public sealed partial class TownOfRolesPlugin : BasePlugin
    {
        public const string Guid = "townofroles";

        public const string Version = "1.0";

        private static bool _harmonyHooksInstalled;
        private static bool _eventHooksInstalled;
        private static bool _engineerEventHooksInstalled;
        private static bool _jesterEventHooksInstalled;
        private static bool _jesterHarmonyInstalled;
        private static bool _batchEventHooksInstalled;
        private static bool _batchHarmonyInstalled;
        private static bool _medicEventHooksInstalled;
        private static bool _seerEventHooksInstalled;
        private static bool _vigilanteEventHooksInstalled;
        private static bool _assassinEventHooksInstalled;
        private static bool _assassinHarmonyInstalled;
        private static bool _presentationHarmonyInstalled;
        private static bool _commandHarmonyInstalled;
        private static bool _updateHarmonyInstalled;
        private static bool _versionBadgeHarmonyInstalled;
        private static bool _gameConfigHarmonyInstalled;
        private static bool _nativeRoleOptionsHarmonyInstalled;
        private static bool _settingsRowsHarmonyInstalled;
        private static bool _bootScreenHarmonyInstalled;
        private static bool _creatorColorHarmonyInstalled;
        private static bool _exileTextHarmonyInstalled;
        private const string CreatorColorHarmonyId = Guid + ".creatorcolor";
        private const string CommandHarmonyId = Guid + ".commands";
        private const string SettingsRowsHarmonyId = Guid + ".settingsrows";
        private const string VersionBadgeHarmonyId = Guid + ".versionbadge";
        private const string GameConfigHarmonyId = Guid + ".gameconfig";
        private const string NativeRoleOptionsHarmonyId = Guid + ".nativeroleoptions";
        private const string BootScreenHarmonyId = Guid + ".bootscreen";
        private const string JesterHarmonyId = Guid + ".jester";
        private const string LoversHarmonyId = Guid + ".lovers";
        private const string AssassinHarmonyId = Guid + ".assassin";

        public override void Load()
        {
            Log.LogInfo($"Town Of Roles {Version} - build {BootTrace.BuildStamp()}");
            BootTrace.RecoverCrashes();

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                try
                {
                    Log.LogError("[TOR-FATAL] Unhandled exception: " + e.ExceptionObject);
                }
                catch
                {
                }
            };

            StartUp();
            RegisterRoles();
            InstallHooks();
            FinishStartUp();
        }

        public override bool Unload()
        {
            if (_eventHooksInstalled)
            {
                GameEvents.BeforeReport -= SheriffSystem.OnBeforeReport;
                GameEvents.GameStarted -= SheriffSystem.OnGameStarted;
                GameEvents.GameEnded -= SheriffSystem.OnGameEnded;
                _eventHooksInstalled = false;
            }

            if (_engineerEventHooksInstalled)
            {
                GameEvents.GameStarted -= EngineerAbility.OnGameStarted;
                GameEvents.GameEnded -= EngineerAbility.OnGameEnded;
                _engineerEventHooksInstalled = false;
            }

            if (_jesterEventHooksInstalled)
            {
                GameEvents.PlayerExiled -= JesterSystem.OnPlayerExiled;
                GameEvents.GameStarted -= JesterSystem.OnGameStarted;
                GameEvents.GameEnded -= JesterSystem.OnGameEnded;
                _jesterEventHooksInstalled = false;
            }

            if (_assassinEventHooksInstalled)
            {
                GameEvents.GameStarted -= AssassinSystem.OnGameStarted;
                GameEvents.GameEnded -= AssassinSystem.OnGameEnded;
                GameEvents.GameStarted -= AssassinSettingsSync.OnGameStarted;
                GameEvents.GameEnded -= AssassinSettingsSync.OnGameEnded;
                GameEvents.PlayerJoined -= AssassinSettingsSync.OnPlayerJoined;
                GameEvents.AtMeeting -= AssassinSystem.OnMeetingStarted;
                GameEvents.AfterMeeting -= AssassinSystem.OnMeetingEnded;
                _assassinEventHooksInstalled = false;
            }

            if (_assassinHarmonyInstalled)
            {
                new Harmony(AssassinHarmonyId).UnpatchSelf();
                AssassinSystem.Reset();
                _assassinHarmonyInstalled = false;
            }

            if (_presentationHarmonyInstalled)
            {
                new Harmony(Guid + ".presentation").UnpatchSelf();
                PresentationPatches.Reset();
                _presentationHarmonyInstalled = false;
            }

            if (_jesterHarmonyInstalled)
            {
                new Harmony(JesterHarmonyId).UnpatchSelf();
                JesterSystem.Reset();
                _jesterHarmonyInstalled = false;
            }

            if (_batchEventHooksInstalled)
            {
                if (_medicEventHooksInstalled)
                {
                    GameEvents.BeforeMurder -= MedicSystem.OnBeforeMurder;
                    GameEvents.AfterReport -= MedicSystem.OnAfterReport;
                    GameEvents.GameStarted -= MedicSystem.OnGameStarted;
                    GameEvents.GameEnded -= MedicSystem.OnGameEnded;
                    GameEvents.AfterMurder -= Core.KillLog.OnAfterMurder;
                    GameEvents.GameStarted -= Core.KillLog.OnGameStarted;
                    GameEvents.GameEnded -= Core.KillLog.OnGameEnded;
                    _medicEventHooksInstalled = false;
                }
                if (_seerEventHooksInstalled)
                {
                    GameEvents.GameStarted -= SeerSystem.OnGameStarted;
                    GameEvents.GameEnded -= SeerSystem.OnGameEnded;
                    _seerEventHooksInstalled = false;
                }
                if (_vigilanteEventHooksInstalled)
                {
                    GameEvents.GameStarted -= VigilanteSystem.OnGameStarted;
                    GameEvents.GameEnded -= VigilanteSystem.OnGameEnded;
                    _vigilanteEventHooksInstalled = false;
                }
                _batchEventHooksInstalled = false;
            }

            if (_batchHarmonyInstalled)
            {
                UiAbilityButtons.ResetAll();
                new Harmony(Guid + ".abilities").UnpatchSelf();
                MedicSystem.Reset();
                SeerSystem.Reset();
                VigilanteSystem.Reset();
                _batchHarmonyInstalled = false;
            }

            if (_commandHarmonyInstalled)
            {
                new Harmony(CommandHarmonyId).UnpatchSelf();
                _commandHarmonyInstalled = false;
                VisualEffects.Reset();
                CommandState.Reset();
            }

            if (_updateHarmonyInstalled)
            {
                new Harmony(Guid + ".updates").UnpatchSelf();
                _updateHarmonyInstalled = false;
            }

            if (_versionBadgeHarmonyInstalled)
            {
                new Harmony(VersionBadgeHarmonyId).UnpatchSelf();
                _versionBadgeHarmonyInstalled = false;
            }

            if (_gameConfigHarmonyInstalled)
            {
                GameEvents.GameStarted -= RoleSettingsSync.OnGameStarted;
                GameEvents.PlayerJoined -= RoleSettingsSync.OnPlayerJoined;
                GameConfigOverlay.Hide();
                new Harmony(GameConfigHarmonyId).UnpatchSelf();
                _gameConfigHarmonyInstalled = false;
            }

            if (_nativeRoleOptionsHarmonyInstalled)
            {
                new Harmony(NativeRoleOptionsHarmonyId).UnpatchSelf();
                _nativeRoleOptionsHarmonyInstalled = false;
            }

            SettingsTabPage.Reset();
            HorseMode.Reset();

            RoleInfoCard.Reset();
            new Harmony(Guid + ".roleinfocard").UnpatchSelf();

            if (_settingsRowsHarmonyInstalled)
            {
                UiRuntime.Reset();
                new Harmony(SettingsRowsHarmonyId).UnpatchSelf();
                _settingsRowsHarmonyInstalled = false;
            }

            if (_creatorColorHarmonyInstalled)
            {
                new Harmony(CreatorColorHarmonyId).UnpatchSelf();
                CreatorColor.Reset();
                _creatorColorHarmonyInstalled = false;
            }

            if (_exileTextHarmonyInstalled)
            {
                new Harmony(Guid + ".exiletext").UnpatchSelf();
                _exileTextHarmonyInstalled = false;
            }

            new Harmony(Guid + ".boottrace").UnpatchSelf();

            GameEvents.GameStarted -= JanitorSystem.OnGameStarted;
            GameEvents.GameEnded -= JanitorSystem.OnGameEnded;
            GameEvents.GameStarted -= AltruistSystem.OnGameStarted;
            GameEvents.GameEnded -= AltruistSystem.OnGameEnded;
            GameEvents.PlayerExiled -= ExecutionerSystem.OnPlayerExiled;
            GameEvents.BeforeMurder -= ExecutionerSystem.OnBeforeMurder;
            GameEvents.GameStarted -= ExecutionerSystem.OnGameStarted;
            GameEvents.GameEnded -= ExecutionerSystem.OnGameEnded;
            GameEvents.GameStarted -= ArsonistSystem.OnGameStarted;
            GameEvents.GameEnded -= ArsonistSystem.OnGameEnded;
            GameEvents.GameStarted -= SwapperSystem.OnGameStarted;
            GameEvents.GameEnded -= SwapperSystem.OnGameEnded;
            GameEvents.AtMeeting -= SwapperSystem.OnMeetingStarted;
            GameEvents.AfterMeeting -= SwapperSystem.OnMeetingEnded;
            GameEvents.GameStarted -= MorphlingSystem.OnGameStarted;
            GameEvents.GameEnded -= MorphlingSystem.OnGameEnded;
            GameEvents.AtMeeting -= MorphlingSystem.OnMeetingStarted;
            GameEvents.GameStarted -= SpySystem.OnGameStarted;
            GameEvents.GameEnded -= SpySystem.OnGameEnded;
            GameEvents.GameStarted -= CamouflagerSystem.OnGameStarted;
            GameEvents.GameEnded -= CamouflagerSystem.OnGameEnded;
            GameEvents.AtMeeting -= CamouflagerSystem.OnMeetingStarted;
            GameEvents.GameStarted -= SwooperSystem.OnGameStarted;
            GameEvents.GameEnded -= SwooperSystem.OnGameEnded;
            GameEvents.AtMeeting -= SwooperSystem.OnMeetingStarted;
            GameEvents.GameStarted -= UnderdogSystem.OnGameStarted;
            GameEvents.GameEnded -= UnderdogSystem.OnGameEnded;
            GameEvents.GameStarted -= UndertakerSystem.OnGameStarted;
            GameEvents.GameEnded -= UndertakerSystem.OnGameEnded;
            GameEvents.AtMeeting -= UndertakerSystem.OnMeetingStarted;
            GameEvents.GameStarted -= InvestigatorSystem.OnGameStarted;
            GameEvents.GameEnded -= InvestigatorSystem.OnGameEnded;
            GameEvents.AtMeeting -= InvestigatorSystem.OnMeetingStarted;
            GameEvents.GameStarted -= TimeLordSystem.OnGameStarted;
            GameEvents.GameEnded -= TimeLordSystem.OnGameEnded;
            GameEvents.GameStarted -= SnitchSystem.OnGameStarted;
            GameEvents.GameEnded -= SnitchSystem.OnGameEnded;
            GameEvents.AtMeeting -= SnitchSystem.OnMeetingStarted;
            GameEvents.GameStarted -= PhantomSystem.OnGameStarted;
            GameEvents.GameEnded -= PhantomSystem.OnGameEnded;
            GameEvents.GameStarted -= ShifterSystem.OnGameStarted;
            GameEvents.GameEnded -= ShifterSystem.OnGameEnded;
            GameEvents.GameStarted -= GlitchSystem.OnGameStarted;
            GameEvents.GameEnded -= GlitchSystem.OnGameEnded;
            GameEvents.BeforeReport -= GlitchSystem.OnBeforeReport;
            GameEvents.GameStarted -= MinerSystem.OnGameStarted;
            GameEvents.GameEnded -= MinerSystem.OnGameEnded;
            GameEvents.GameStarted -= ModifierSystem.OnGameStarted;
            GameEvents.GameEnded -= ModifierSystem.OnGameEnded;
            GameEvents.BeforeMurder -= ModifierSystem.OnBeforeMurder;
            GameEvents.GameEnded -= CreatorColor.OnGameEnded;
            GameEvents.GameStarted -= CreatorColor.OnGameStarted;
            ModifierSystem.Reset();
            new Harmony(Guid + ".modifiers").UnpatchSelf();
            new Harmony(Guid + ".batch3").UnpatchSelf();
            new Harmony(Guid + ".batch4").UnpatchSelf();
            new Harmony(Guid + ".batch5").UnpatchSelf();
            new Harmony(Guid + ".mayorabstain").UnpatchSelf();
            CamouflagerSystem.Reset();
            SwooperSystem.Reset();
            UnderdogSystem.Reset();
            UndertakerSystem.Reset();
            InvestigatorSystem.Reset();
            TimeLordSystem.Reset();
            SnitchSystem.Reset();
            PhantomSystem.Reset();
            ShifterSystem.Reset();
            GlitchSystem.Reset();
            MinerSystem.Reset();
            JanitorSystem.Reset();
            AltruistSystem.Reset();
            ExecutionerSystem.Reset();
            ArsonistSystem.Reset();
            SwapperSystem.Reset();
            MorphlingSystem.Reset();
            SpySystem.Reset();
            new Harmony(Guid + ".mayor").UnpatchSelf();
            new Harmony(Guid + ".executioner").UnpatchSelf();
            new Harmony(Guid + ".arsonist").UnpatchSelf();
            new Harmony(Guid + ".swapper").UnpatchSelf();
            new Harmony(Guid + ".morphling").UnpatchSelf();
            new Harmony(Guid + ".spy").UnpatchSelf();

            if (_harmonyHooksInstalled)
            {
                new Harmony(Guid).UnpatchSelf();
                _harmonyHooksInstalled = false;
            }
            return true;
        }
    }
}
