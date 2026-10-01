using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ClassicUs.Reactor;
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
    // Town Of Roles for Classic Us, a port of the classic Town-Of-Us role mod onto the MarshAPI
    // + Reactor framework.
    [BepInPlugin(Guid, "Town Of Roles", Version)]
    [BepInDependency(ReactorPlugin.Guid)]
    [BepInDependency(MarshAPIPlugin.Guid)]
    public sealed class TownOfRolesPlugin : BasePlugin
    {
        public const string Guid = "townofroles";

        // The release this build is. The badge appends the "(Beta)" tag, so the version itself
        // stays a plain number that a bug report can quote.
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
            // Before anything that can die: names the running build, and reads back the step the
            // previous launch died in so this one leaves it out. See BootTrace.
            Log.LogInfo($"Town Of Roles {Version} - build {BootTrace.BuildStamp()}");
            BootTrace.RecoverCrashes();

            // CRASH DIAGNOSTIC: log any unhandled managed exception with its
            // stack BEFORE it crosses into native code and the process aborts
            // with PAL_SEHException (which discards the managed stack). If the
            // boot crash is a managed exception from any patch or callback, this
            // line shows exactly where it was thrown.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                try
                {
                    Log.LogError("[TOR-FATAL] Unhandled exception: " + e.ExceptionObject);
                }
                catch
                {
                    // Never throw inside the handler.
                }
            };

            // Tell Reactor this mod is present (mod list only; the handshake /
            // mod-set verification system was removed from Reactor).
            BootTrace.Run("ReactorAPI.Register", () => ReactorAPI.Register("Town Of Roles", Version));

            // Per-role enable toggles (BepInEx/config/TownOfRoles.cfg).
            RoleConfig.Init(Config);
            CommandConfig.Init(Config);
            CreatorColor.Init(Config);
            UpdateConfig.Init(Config);

            // Apply (or finish applying) any staged self-update and clear a staging
            // folder no pending update is using. This is the loader-independent path: the
            // mod starts a detached swap helper rather than depending on a preloader
            // patcher, and re-arms it here so a marker left by a session whose helper was
            // killed still lands. Non-fatal, and inert when nothing is staged.
            BootTrace.Run("UpdateApplier.Arm", () => UpdateApplier.Arm());

            // Announce this mod to MarshAPI. The registry is what a mods menu lists and
            // what owns the player's on/off choice for the mod, so it is registered even
            // in inert diagnostic mode: the plugin is loaded either way.
            var marshMod = ModRegistry.Register(
                id: "townofroles",
                displayName: "Town Of Roles",
                version: Version,
                author: "hallowmarsh",
                color: new UnityEngine.Color(1f, 0.41f, 0.71f));
            marshMod.Tag = "26 roles";

            // Put the session switch in its default state before anything reads it, so a
            // switch left on by an earlier build cannot be inherited by this one.
            SessionDisable.Apply();

            // MarshAPI cannot unload a plugin, so "off" has to be applied here. What this
            // mod can retract at runtime is its options tab; the next OptionsMenuBehaviour
            // .Awake rebuilds it when the mod is switched back on.
            marshMod.OnEnabledChanged += enabled =>
            {
                if (enabled) return;
                TorPage.Reset();
            };

            // The mods list is added to the game's options menu. Off unless the config
            // asks for it, so an untouched install sees nothing new.
            if (RoleConfig.ModsMenu?.Value == true) ModsPage.Enable();

            // The mux goes in first, before any role registers an [ReactorRpc] key. Reactor
            // reserves 39 custom ids (212-250) and this mod has ~68 keys; an overflowed key
            // gets id 0, and Send(0) reaches a vanilla handler, which segfaults on join. If
            // the mux cannot install, role RPC registration is skipped entirely.
            try
            {
                BootTrace.Run("TownOfRolesRpcMux.Install", () => TownOfRolesRpcMux.Install());
            }
            catch (Exception ex)
            {
                Log.LogError("TownOfRoles RPC mux failed to install: " + ex);
            }

            // Creator identity handshake: every client can receive and verify the
            // creator's claim (secret-matched), and the claim must not leak into
            // the next lobby after a game ends.
            RpcRegistration.Register(typeof(CreatorColor));
            GameEvents.GameStarted += CreatorColor.OnGameStarted;
            GameEvents.GameEnded += CreatorColor.OnGameEnded;

            if (UpdateConfig.Enabled?.Value == true)
            {
                BootTrace.Run(nameof(InstallUpdateHarmony), () => InstallUpdateHarmony());
                UpdateSystem.StartCheck();
            }
            // Never inject custom rows into Classic Us' native Game Options menu.
            // On 8.9 that MarshAPI path can freeze the menu; role settings live in
            // the three BepInEx config sections instead.
            if (CommandConfig.Enabled.Value)
            {
                // Registers the mod's commands on MarshAPI's framework and points its
                // reply/enabled/fallback seams at this mod.
                CommandSystem.Initialize();
                RpcRegistration.Register(typeof(CommandSystem));
                BootTrace.Run(nameof(InstallCommandHarmony), () => InstallCommandHarmony());
            }
            else
            {
                // The framework's dispatch patch is installed by MarshAPI whether or not
                // this mod wants commands, so a disabled config has to say so, otherwise
                // the framework's own /help would still answer here.
                ChatCommands.Enabled = () => false;
            }

            // The lobby's per-team custom-role caps (Town Of Us' MaxImpostorRoles /
            // MaxNeutralRoles). Installed here because the API has no config of its own:
            // RoleRegistry is MarshAPI's, and MarshAPI cannot read this mod's file. Must be
            // set before any role is registered, since it is read during assignment.
            RoleRegistry.TeamCap = RoleConfig.RoleCap;

            if (RoleConfig.Sheriff.Value)
            {
                BootTrace.Run(nameof(RegisterSheriff), () => RegisterSheriff());
            }

            if (RoleConfig.Engineer.Value)
            {
                BootTrace.Run(nameof(RegisterEngineer), () => RegisterEngineer());
            }

            if (RoleConfig.Jester.Value)
            {
                BootTrace.Run(nameof(RegisterJester), () => RegisterJester());
                BootTrace.Run(nameof(InstallJesterHarmony), () => InstallJesterHarmony());
            }

            if (RoleConfig.Medic.Value || RoleConfig.Seer.Value || RoleConfig.Vigilante.Value)
                BootTrace.Run(nameof(RegisterFirstBatchRoles), () => RegisterFirstBatchRoles());

            // The buttons are registered with MarshAPI either way, so there is one
            // copy of them whether or not they are drawn; the [Gameplay]
            // EnableCustomAbilityButtons toggle is applied to the registry, and the TOR
            // tab's session switch can overrule it. MarshAPI owns the HUD tick, the
            // native-template capture and the per-round reset.
            BootTrace.Run("CustomRoleAbilities.Initialize", () => CustomRoleAbilities.Initialize());

            // Whether any role owns an ability button decides whether the HUD hook is
            // installed at all. This system was the source of native freezes/PAL crashes on
            // some Linux builds, so the no-role case stays a real skip rather than a hidden
            // button per role: creating nineteen invisible clones a round is not free.
            if (RoleConfig.Sheriff.Value || RoleConfig.Vigilante.Value ||
                RoleConfig.Engineer.Value || RoleConfig.Medic.Value || RoleConfig.Seer.Value ||
                RoleConfig.Janitor.Value || RoleConfig.Altruist.Value ||
                RoleConfig.Arsonist.Value || RoleConfig.Morphling.Value ||
                RoleConfig.Camouflager.Value || RoleConfig.Swooper.Value || RoleConfig.Undertaker.Value ||
                RoleConfig.TimeLord.Value || RoleConfig.Shifter.Value || RoleConfig.Glitch.Value ||
                RoleConfig.Miner.Value)
            {
                BootTrace.Run(nameof(InstallSessionResetHarmony), () => InstallSessionResetHarmony());
            }
            else
            {
                // No role that owns an ability button is enabled, so there is nothing to
                // draw: the registry keeps the buttons, and the tick stops.
                UiAbilityButtons.Enabled = false;
            }

            if (RoleConfig.Assassin.Value)
            {
                BootTrace.Run(nameof(RegisterAssassin), () => RegisterAssassin());
                BootTrace.Run(nameof(InstallAssassinHarmony), () => InstallAssassinHarmony());
            }

            if (RoleConfig.Janitor.Value)
            {
                BootTrace.Run(nameof(RegisterJanitor), () => RegisterJanitor());
            }

            if (RoleConfig.Altruist.Value)
            {
                BootTrace.Run(nameof(RegisterAltruist), () => RegisterAltruist());
            }

            if (RoleConfig.Mayor.Value)
            {
                BootTrace.Run(nameof(RegisterMayor), () => RegisterMayor());
                BootTrace.Run(nameof(InstallMayorHarmony), () => InstallMayorHarmony());
            }

            if (RoleConfig.Executioner.Value)
            {
                BootTrace.Run(nameof(RegisterExecutioner), () => RegisterExecutioner());
                BootTrace.Run(nameof(InstallExecutionerHarmony), () => InstallExecutionerHarmony());
            }

            if (RoleConfig.Arsonist.Value)
            {
                BootTrace.Run(nameof(RegisterArsonist), () => RegisterArsonist());
                BootTrace.Run(nameof(InstallArsonistHarmony), () => InstallArsonistHarmony());
            }

            if (RoleConfig.Lovers.Value)
            {
                BootTrace.Run(nameof(RegisterLovers), () => RegisterLovers());
                BootTrace.Run(nameof(InstallLoversHarmony), () => InstallLoversHarmony());
            }

            if (RoleConfig.Swapper.Value)
            {
                BootTrace.Run(nameof(RegisterSwapper), () => RegisterSwapper());
                BootTrace.Run(nameof(InstallSwapperHarmony), () => InstallSwapperHarmony());
            }

            if (RoleConfig.Morphling.Value)
            {
                BootTrace.Run(nameof(RegisterMorphling), () => RegisterMorphling());
            }

            if (RoleConfig.Spy.Value)
            {
                BootTrace.Run(nameof(RegisterSpy), () => RegisterSpy());
            }

            if (RoleConfig.Camouflager.Value || RoleConfig.Swooper.Value ||
                RoleConfig.Underdog.Value || RoleConfig.Undertaker.Value)
            {
                BootTrace.Run(nameof(RegisterBatch3Impostors), () => RegisterBatch3Impostors());
            }

            if (RoleConfig.Investigator.Value || RoleConfig.TimeLord.Value ||
                RoleConfig.Snitch.Value || RoleConfig.Phantom.Value)
            {
                BootTrace.Run(nameof(RegisterBatch4Roles), () => RegisterBatch4Roles());
                BootTrace.Run(nameof(InstallBatch4Harmony), () => InstallBatch4Harmony());
            }

            if (RoleConfig.Shifter.Value || RoleConfig.Glitch.Value || RoleConfig.Miner.Value)
            {
                BootTrace.Run(nameof(RegisterBatch5Roles), () => RegisterBatch5Roles());
                BootTrace.Run(nameof(InstallBatch5Harmony), () => InstallBatch5Harmony());
            }

            if (RoleConfig.Mayor.Value)
            {
                BootTrace.Run(nameof(InstallMayorAbstainHarmony), () => InstallMayorAbstainHarmony());
            }

            // Player modifiers (Torch / Diseased / Flash / Tiebreaker / Drunk /
            // Giant / Button Barry). Registered regardless of which toggles are on
            // (inert when all are disabled).
            BootTrace.Run(nameof(RegisterModifiers), () => RegisterModifiers());

            if (RoleConfig.PresentationEnabled?.Value == true)
            {
                BootTrace.Run(nameof(InstallPresentationHarmony), () => InstallPresentationHarmony());
            }

            // Delegate-free click routing for all custom UI buttons. Classic Us
            // terminates the process when a managed delegate is first marshalled
            // into Il2Cpp (OnClick.AddListener), so every button built by this
            // mod is dispatched through the native PassiveButton.ReceiveClickDown
            // pipeline instead. Installed unconditionally; inert without registrations.
            BootTrace.Run(nameof(InstallSettingsRowsHarmony), () => InstallSettingsRowsHarmony());

            // Custom exile reveal text ("X was the Jester/Executioner/Arsonist/
            // Phantom/The Glitch") re-applied every frame while the exile animates.
            // Replaces the removed compiler-generated coroutine patches. Installed
            // unconditionally; inert (one null check) outside an exile.
            BootTrace.Run(nameof(InstallExileTextHarmony), () => InstallExileTextHarmony());

            // Creator-only name color: the mod creator's name cycles blue/pink
            // (name-matched, config-gated).
            BootTrace.Run(nameof(InstallCreatorColorHarmony), () => InstallCreatorColorHarmony());

            // "TownOfRoles vX.Y.Z" under the game's version readout (top-left), plus
            // the in-game credit ("Made by hallowmarsh") above the ping/fps labels.
            BootTrace.Run(nameof(InstallVersionBadgeHarmony), () => InstallVersionBadgeHarmony());

            // Tasks-tab role info card ("Your Role: X, description"). Non-fatal:
            // a missing importantTextTask field on some build just disables it.
            BootTrace.Run(nameof(InstallRoleInfoCardHarmony), () => InstallRoleInfoCardHarmony());

            // The mod's own tab on the game's options menu: switch the mod off for the
            // session, block the round from ending, start the round now. Registered from
            // here rather than owned here, OptionsTabs owns the tab lifecycle.
            BootTrace.Run("TorPage.Register", () => TorPage.Register());

            // Seasonal horse mode, the one client tweak the release build keeps. Applied
            // now so a config-file edit takes effect without waiting for a round.
            BootTrace.Run("HorseModeConfig.Init", () => HorseModeConfig.Init(Config));
            BootTrace.Run("HorseMode.Apply", () => HorseMode.Apply());
            GameEvents.GameStarted += _ => HorseMode.Apply();

            // In-game role settings: sync registry + the tabbed config overlay.
            BootTrace.Run("RoleSettingsSync.Init", () => RoleSettingsSync.Init());

            // The game's own role-option page (prototype, off by default). OptionSource feeds
            // every role's DeclareOptions from the one option table above, and the bridge
            // patches keep the game's option store and the mod's gameplay reading one value.
            NativeRoleOptions.OptionSource = RoleNativeOptions.Declare;
            if (NativeRoleOptions.Enabled)
            {
                BootTrace.Run(nameof(InstallNativeRoleOptionsHarmony), () => InstallNativeRoleOptionsHarmony());
            }
            // Registered regardless of the overlay toggle: clients must mirror the
            // host's role settings even when the tabs are disabled.
            RpcRegistration.Register(typeof(RoleSettingsSync));
            if (RoleConfig.GameConfigOverlay?.Value == true)
            {
                BootTrace.Run(nameof(InstallGameConfigHarmony), () => InstallGameConfigHarmony());
                GameEvents.GameStarted += RoleSettingsSync.OnGameStarted;
                GameEvents.PlayerJoined += RoleSettingsSync.OnPlayerJoined;
            }

            // ModsMenuAPI (the in-game bottom-right "Mods" menu) is not present in
            // MarshAPI 1.5.1, so this mod's own mod/role toggles are not registered
            // here. Role enablement is driven solely by the BepInEx config toggles.

            // Keep role registration independent from the experimental HUD/meeting
            // hooks. The Freeplay computer is provided by MarshAPI and only needs the
            // virtual role registration above; skipping these patches avoids touching
            // IL2CPP lifecycle methods while diagnosing native Linux crashes.
            if (RoleConfig.GameplayHooks.Value)
            {
                // Event-level Sheriff hooks (report suppression and lifecycle
                // bookkeeping) remain behind this diagnostic switch.
            }

            // Load the mod's assets and warm its code on ClassicUs' "Check for
            // updates" splash screen, the first managed callback in the MainMenu
            // scene. Runs before the menu is built, so no icon decode or table
            // build is left for a round to pay for.
            BootTrace.Run(nameof(InstallBootScreenHarmony), () => InstallBootScreenHarmony());

            // Startup crash diagnostics: marker lines as the boot scene runs.
            // Always armed so the next crash log shows exactly how far boot got.
            BootTrace.Run(nameof(InstallBootTraceHarmony), () => InstallBootTraceHarmony());
            BootTrace.Mark("M1 Load complete - boot trace armed");

            // The last statement on the boot path. A marker that outlives the launch is the crash
            // report, so nothing may be added below this line.
            BootTrace.Complete();
        }

        // Hooks the splash / "check for updates" screen so this mod preloads its assets and
        // code there (see BootScreenLoad). Both patches are independent: a target missing on
        // some build only skips that one.
        private void InstallBootScreenHarmony()
        {
            if (_bootScreenHarmonyInstalled) return;

            var preload = RoleConfig.BootScreenPreload?.Value == true;
            var badge = RoleConfig.BootScreenBadge?.Value == true;
            if (!preload && !badge)
            {
                return;
            }

            _bootScreenHarmonyInstalled = true;
            var harmony = new Harmony(BootScreenHarmonyId);
            foreach (var patchType in new[]
            {
                typeof(BootScreenLoad.SplashManager_Start_BootScreenPatch),
                typeof(BootScreenLoad.GithubReleaseChecker_Awake_BootScreenPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                }
                catch (Exception e)
                {
                    Log.LogWarning("Boot-screen hook skipped (" + patchType.Name + "): " + e.Message);
                }
            }
        }

        private void InstallBootTraceHarmony()
        {
            // Non-fatal: each marker installs independently and is inert if a
            // target does not exist on some build.
            var harmony = new Harmony(Guid + ".boottrace");
            foreach (var patchType in new[]
            {
                typeof(BootTrace.M2_AmongUsClient_Awake),
                typeof(BootTrace.M3_MainMenuManager_Start),
                typeof(BootTrace.M4_VersionShower_Start),
                typeof(BootTrace.M5_GameStartManager_Start),
                typeof(BootTrace.M6_HudManager_Start),
                typeof(BootTrace.M7_MeetingHud_Start),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                }
                catch (Exception e)
                {
                    Log.LogWarning("Boot trace marker skipped (" + patchType.Name + "): " + e.Message);
                }
            }
        }

        // Registers everything the Sheriff needs. Guarded by RoleConfig.Sheriff so a disabled
        // role never enters the role pool.
        private static void RegisterSheriff()
        {
            // Every client can receive the Sheriff's kill-record RPC.
            RpcRegistration.Register(typeof(SheriffSystem));

            // Virtual roles ride on the vanilla Crewmate/Impostor backing roles, no
            // IL2CPP type injection required.
            // NOTE: the game ships its own global-namespace SheriffRole, so a simple
            // name would resolve to the game's class; fully-qualify our descriptor.
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Sheriff.SheriffRole());

            // The Sheriff cannot report bodies they shot themselves, and its kill tables
            // are cleared whenever a round starts or ends (HudManager.Start only fires
            // once, so game events close the no-meeting lifecycle gap). Keep these hooks
            // behind the same diagnostic gate as the Harmony patches.
            if (RoleConfig.GameplayHooks.Value)
            {
                GameEvents.BeforeReport += SheriffSystem.OnBeforeReport;
                GameEvents.GameStarted += SheriffSystem.OnGameStarted;
                GameEvents.GameEnded += SheriffSystem.OnGameEnded;
                _eventHooksInstalled = true;
            }
        }

        private static void RegisterJester()
        {
            RpcRegistration.Register(typeof(JesterSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Jester.JesterRole());
            // (No per-frame tick: the Jester has no per-frame work, its win sites fire
            // from events and the end screen is MarshAPI's central ModdedGameOver.)
            GameEvents.PlayerExiled += JesterSystem.OnPlayerExiled;
            GameEvents.GameStarted += JesterSystem.OnGameStarted;
            GameEvents.GameEnded += JesterSystem.OnGameEnded;
            _jesterEventHooksInstalled = true;
        }

        private void InstallJesterHarmony()
        {
            // The Jester's end screen is drawn by MarshAPI's central ModdedGameOver
            // patches; the per-role EndGameManager patch this method used to install is
            // gone. Kept as a hook point so the role's registration shape does not
            // change, and still non-fatal by construction.
            _jesterHarmonyInstalled = true;
        }

        private void InstallPresentationHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".presentation");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(HudManager_Update_PresentationPatch),
                typeof(MeetingHud_Update_PresentationPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Role presentation patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            _presentationHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterAssassin()
        {
            RpcRegistration.Register(typeof(AssassinSystem));
            RpcRegistration.Register(typeof(AssassinSettingsSync));
            AssassinSettingsSync.InitFromConfig();
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Assassin.AssassinRole());
            GameEvents.GameStarted += AssassinSystem.OnGameStarted;
            GameEvents.GameEnded += AssassinSystem.OnGameEnded;
            GameEvents.GameStarted += AssassinSettingsSync.OnGameStarted;
            GameEvents.GameEnded += AssassinSettingsSync.OnGameEnded;
            GameEvents.PlayerJoined += AssassinSettingsSync.OnPlayerJoined;
            GameEvents.AtMeeting += AssassinSystem.OnMeetingStarted;
            GameEvents.AfterMeeting += AssassinSystem.OnMeetingEnded;
            _assassinEventHooksInstalled = true;
        }

        private void InstallAssassinHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(AssassinHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(MeetingHud_Start_AssassinPatch),
                typeof(MeetingHud_Confirm_AssassinPatch),
                typeof(MeetingHud_VotingComplete_AssassinPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Assassin patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            _assassinHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterJanitor()
        {
            // Every client can receive the Janitor's clean RPC.
            RpcRegistration.Register(typeof(JanitorSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Janitor.JanitorRole());
            GameEvents.GameStarted += JanitorSystem.OnGameStarted;
            GameEvents.GameEnded += JanitorSystem.OnGameEnded;
        }

        private static void RegisterAltruist()
        {
            RpcRegistration.Register(typeof(AltruistSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Altruist.AltruistRole());
            GameEvents.GameStarted += AltruistSystem.OnGameStarted;
            GameEvents.GameEnded += AltruistSystem.OnGameEnded;
        }

        private static void RegisterMayor()
        {
            // Mayor is passive: the vote-bank tally patch is the only hook.
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Mayor.MayorRole());
        }

        private void InstallMayorHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".mayor");
            try
            {
                harmony.CreateClassProcessor(typeof(MeetingHud_CalculateVotes_MayorPatch)).Patch();
            }
            catch (Exception e)
            {
                Log.LogWarning("Mayor vote-bank hook skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private static void RegisterExecutioner()
        {
            // Every client can receive the Executioner's assignment/win RPCs.
            RpcRegistration.Register(typeof(ExecutionerSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Executioner.ExecutionerRole());
            GameEvents.PlayerExiled += ExecutionerSystem.OnPlayerExiled;
            // Conversion rule: the Executioner becomes Jester/Crewmate when
            // their target dies without being voted out.
            GameEvents.BeforeMurder += ExecutionerSystem.OnBeforeMurder;
            GameEvents.GameStarted += ExecutionerSystem.OnGameStarted;
            GameEvents.GameEnded += ExecutionerSystem.OnGameEnded;
        }

        private void InstallExecutionerHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // role its extras and never stops the plugin loading. Its per-frame work is a
            // registration, not a patch of its own.
            RoleTick.EveryFrame("Executioner", ExecutionerSystem.Tick);

            var harmony = new Harmony(Guid + ".executioner");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_ExecutionerPatch),
                // EndGameManager Update/SetEverythingUp moved to MarshAPI's central
                // ModdedGameOver pair; the Executioner claims its title instead.
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Executioner patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterLovers()
        {
            // The pair table travels over the RPC mux, so every client must be
            // able to receive the assignment, not just the two lovers.
            RpcRegistration.Register(typeof(LoverSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Lovers.LoverRole());
            GameEvents.GameStarted += LoverSystem.OnGameStarted;
            GameEvents.GameEnded += LoverSystem.OnGameEnded;
        }

        private void InstallLoversHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(LoversHarmonyId);
            int ok = 0;
            RoleTick.EveryFrame("Lovers", LoverSystem.Tick);

            foreach (var patchType in new[]
            {
                typeof(ShipStatus_CheckEndCriteria_LoversPatch),
                // EndGameManager Update/SetEverythingUp moved to MarshAPI's central
                // ModdedGameOver pair; the Lovers claim their titles instead.
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Lovers patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterArsonist()
        {
            // Every client can receive the Arsonist's douse/ignite/win RPCs.
            RpcRegistration.Register(typeof(ArsonistSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Arsonist.ArsonistRole());
            GameEvents.GameStarted += ArsonistSystem.OnGameStarted;
            GameEvents.GameEnded += ArsonistSystem.OnGameEnded;
        }

        private void InstallArsonistHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".arsonist");
            int ok = 0;
            RoleTick.EveryFrame("Arsonist", ArsonistSystem.Tick);

            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_ArsonistPatch),
                // EndGameManager Update/SetEverythingUp moved to MarshAPI's central
                // ModdedGameOver pair; the Arsonist claims its title instead.
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Arsonist patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterSwapper()
        {
            RpcRegistration.Register(typeof(SwapperSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Swapper.SwapperRole());
            GameEvents.GameStarted += SwapperSystem.OnGameStarted;
            GameEvents.GameEnded += SwapperSystem.OnGameEnded;
            GameEvents.AtMeeting += SwapperSystem.OnMeetingStarted;
            GameEvents.AfterMeeting += SwapperSystem.OnMeetingEnded;
        }

        private void InstallSwapperHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".swapper");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(MeetingHud_Start_SwapperPatch),
                typeof(MeetingHud_Confirm_SwapperPatch),
                typeof(MeetingHud_VotingComplete_SwapperPatch),
                typeof(SwapperSystem.MeetingHud_CalculateVotes_SwapperPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Swapper patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterMorphling()
        {
            RpcRegistration.Register(typeof(MorphlingSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Morphling.MorphlingRole());
            GameEvents.GameStarted += MorphlingSystem.OnGameStarted;
            GameEvents.GameEnded += MorphlingSystem.OnGameEnded;
            GameEvents.AtMeeting += MorphlingSystem.OnMeetingStarted;
            RoleTick.EveryFrame("Morphling", MorphlingSystem.Tick);
        }

        private static void RegisterSpy()
        {
            RpcRegistration.Register(typeof(SpySystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Spy.SpyRole());
            GameEvents.GameStarted += SpySystem.OnGameStarted;
            GameEvents.GameEnded += SpySystem.OnGameEnded;
            RoleTick.EveryFrame("Spy", SpySystem.Tick);
        }

        private static void RegisterBatch3Impostors()
        {
            if (RoleConfig.Camouflager.Value)
            {
                RpcRegistration.Register(typeof(CamouflagerSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Camouflager.CamouflagerRole());
                GameEvents.GameStarted += CamouflagerSystem.OnGameStarted;
                GameEvents.GameEnded += CamouflagerSystem.OnGameEnded;
                GameEvents.AtMeeting += CamouflagerSystem.OnMeetingStarted;
                RoleTick.Every("Camouflager", CamouflagerSystem.Tick, 4);
            }
            if (RoleConfig.Swooper.Value)
            {
                RpcRegistration.Register(typeof(SwooperSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Swooper.SwooperRole());
                GameEvents.GameStarted += SwooperSystem.OnGameStarted;
                GameEvents.GameEnded += SwooperSystem.OnGameEnded;
                GameEvents.AtMeeting += SwooperSystem.OnMeetingStarted;
                RoleTick.Every("Swooper", SwooperSystem.Tick, 4);
            }
            if (RoleConfig.Underdog.Value)
            {
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Underdog.UnderdogRole());
                GameEvents.GameStarted += UnderdogSystem.OnGameStarted;
                GameEvents.GameEnded += UnderdogSystem.OnGameEnded;
                RoleTick.Every("Underdog", UnderdogSystem.Tick, 4);
            }
            if (RoleConfig.Undertaker.Value)
            {
                RpcRegistration.Register(typeof(UndertakerSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Undertaker.UndertakerRole());
                GameEvents.GameStarted += UndertakerSystem.OnGameStarted;
                GameEvents.GameEnded += UndertakerSystem.OnGameEnded;
                GameEvents.AtMeeting += UndertakerSystem.OnMeetingStarted;
                RoleTick.Every("Undertaker", UndertakerSystem.Tick, 4);
            }
        }

        private static void RegisterBatch4Roles()
        {
            if (RoleConfig.Investigator.Value)
            {
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Investigator.InvestigatorRole());
                GameEvents.GameStarted += InvestigatorSystem.OnGameStarted;
                GameEvents.GameEnded += InvestigatorSystem.OnGameEnded;
                GameEvents.AtMeeting += InvestigatorSystem.OnMeetingStarted;
                RoleTick.Every("Investigator", InvestigatorSystem.Tick, 4);
            }
            if (RoleConfig.TimeLord.Value)
            {
                RpcRegistration.Register(typeof(TimeLordSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.TimeLord.TimeLordRole());
                GameEvents.GameStarted += TimeLordSystem.OnGameStarted;
                GameEvents.GameEnded += TimeLordSystem.OnGameEnded;
                RoleTick.Every("TimeLord", TimeLordSystem.Tick, 4);
            }
            if (RoleConfig.Snitch.Value)
            {
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Snitch.SnitchRole());
                GameEvents.GameStarted += SnitchSystem.OnGameStarted;
                GameEvents.GameEnded += SnitchSystem.OnGameEnded;
                GameEvents.AtMeeting += SnitchSystem.OnMeetingStarted;
                RoleTick.Every("Snitch", SnitchSystem.Tick, 4);
            }
            if (RoleConfig.Phantom.Value)
            {
                RpcRegistration.Register(typeof(PhantomSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Phantom.PhantomRole());
                GameEvents.GameStarted += PhantomSystem.OnGameStarted;
                GameEvents.GameEnded += PhantomSystem.OnGameEnded;
                RoleTick.Every("Phantom", PhantomSystem.Tick, 4);
            }
        }

        private void InstallBatch4Harmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".batch4");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_PhantomPatch),
                // EndGameManager Update/SetEverythingUp moved to MarshAPI's central
                // ModdedGameOver pair; the Phantom claims its title instead.
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Batch-4 patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterFirstBatchRoles()
        {
            if (RoleConfig.Medic.Value)
            {
                RpcRegistration.Register(typeof(MedicSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Medic.MedicRole());
                // This tick's patch was written and never installed, so a shield outlived
                // both its Medic and its wearer and the green shield visual never refreshed.
                RoleTick.EveryFrame("Medic", MedicSystem.Tick);
            }
            if (RoleConfig.Seer.Value)
            {
                RpcRegistration.Register(typeof(SeerSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Seer.SeerRole());
            }
            if (RoleConfig.Vigilante.Value)
            {
                RpcRegistration.Register(typeof(VigilanteSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Vigilante.VigilanteRole());
            }

            if (RoleConfig.Medic.Value)
            {
                GameEvents.BeforeMurder += MedicSystem.OnBeforeMurder;
                GameEvents.AfterReport += MedicSystem.OnAfterReport;
                GameEvents.GameStarted += MedicSystem.OnGameStarted;
                GameEvents.GameEnded += MedicSystem.OnGameEnded;
                // Body Report needs cross-client kill records.
                GameEvents.AfterMurder += Core.KillLog.OnAfterMurder;
                GameEvents.GameStarted += Core.KillLog.OnGameStarted;
                GameEvents.GameEnded += Core.KillLog.OnGameEnded;
                _medicEventHooksInstalled = true;
            }
            if (RoleConfig.Seer.Value)
            {
                GameEvents.GameStarted += SeerSystem.OnGameStarted;
                GameEvents.GameEnded += SeerSystem.OnGameEnded;
                _seerEventHooksInstalled = true;
            }
            if (RoleConfig.Vigilante.Value)
            {
                GameEvents.GameStarted += VigilanteSystem.OnGameStarted;
                GameEvents.GameEnded += VigilanteSystem.OnGameEnded;
                _vigilanteEventHooksInstalled = true;
            }
            _batchEventHooksInstalled = true;
        }

        // The session-reset patches. The ability buttons' own HUD patches used to be installed
        // here; they belong to MarshAPI now, so this is only the leak guard.
        private void InstallSessionResetHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".abilities");
            try
            {
                // Leaving a lobby/freeplay mid-round never raises GameEnded, which
                // used to leak role state into the next session. ExitGame covers
                // explicit quits; HandleDisconnect covers kicks/errors/host close.
                // Each applies independently so a renamed game method can only
                // degrade to "state persists", never block plugin load.
                foreach (var patchType in new[]
                {
                    typeof(Core.SessionReset.AmongUsClient_ExitGame_SessionPatch),
                    typeof(Core.SessionReset.InnerNetClient_HandleDisconnect_SessionPatch),
                })
                {
                    try { harmony.CreateClassProcessor(patchType).Patch(); }
                    catch (Exception e)
                    {
                        Log.LogWarning("Session reset patch skipped (" + patchType.Name + "): " + e.Message);
                    }
                }

                _batchHarmonyInstalled = true;
            }
            catch (Exception e)
            {
                Log.LogWarning("Session reset patches skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private static void RegisterEngineer()
        {
            // Engineer uses only MarshAPI's virtual-role registration. CanVent is
            // applied to the native CrewmateRole, so the game's existing vent
            // controls, animations, and networking remain authoritative.
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Engineer.EngineerRole());

            // The vent button/outline keep their impostor red for a venting crewmate
            // (the game's cyan path is Hide-and-Seek-only); this re-tints them locally.
            RoleTick.EveryFrame("EngineerVentTint", TownOfRoles.Roles.Engineer.EngineerVentTint.Tick);

            // Engineer lifecycle reset is independent from the optional Sheriff hooks.
            GameEvents.GameStarted += EngineerAbility.OnGameStarted;
            GameEvents.GameEnded += EngineerAbility.OnGameEnded;
            _engineerEventHooksInstalled = true;
        }

        private void InstallCreatorColorHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(CreatorColorHarmonyId);
            try
            {
                harmony.CreateClassProcessor(typeof(CreatorColor.HudManager_Update_CreatorColorPatch)).Patch();
                _creatorColorHarmonyInstalled = true;
            }
            catch (Exception e)
            {
                Log.LogWarning("Creator color skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private void InstallCommandHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(CommandHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(PlayerControl_FixedUpdate_VisualEffectsPatch),
                typeof(ShipStatus_CheckEndCriteria_CommandPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Command patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            _commandHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        private static void RegisterBatch5Roles()
        {
            if (RoleConfig.Shifter.Value)
            {
                RpcRegistration.Register(typeof(ShifterSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Shifter.ShifterRole());
                GameEvents.GameStarted += ShifterSystem.OnGameStarted;
                GameEvents.GameEnded += ShifterSystem.OnGameEnded;
            }
            if (RoleConfig.Glitch.Value)
            {
                RpcRegistration.Register(typeof(GlitchSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Glitch.GlitchRole());
                GameEvents.GameStarted += GlitchSystem.OnGameStarted;
                GameEvents.GameEnded += GlitchSystem.OnGameEnded;
                RoleTick.Every("Glitch", GlitchSystem.Tick, 4);
                GameEvents.BeforeReport += GlitchSystem.OnBeforeReport;
            }
            if (RoleConfig.Miner.Value)
            {
                RpcRegistration.Register(typeof(MinerSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Miner.MinerRole());
                GameEvents.GameStarted += MinerSystem.OnGameStarted;
                GameEvents.GameEnded += MinerSystem.OnGameEnded;
            }
        }

        private void InstallBatch5Harmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".batch5");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_GlitchPatch),
                // EndGameManager Update/SetEverythingUp moved to MarshAPI's central
                // ModdedGameOver pair; the Glitch claims its title instead.
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Batch-5 patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private void InstallMayorAbstainHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".mayorabstain");
            try
            {
                harmony.CreateClassProcessor(typeof(MeetingHud_Start_MayorAbstainPatch)).Patch();
                harmony.CreateClassProcessor(typeof(MeetingHud_Confirm_MayorAbstainPatch)).Patch();
                harmony.CreateClassProcessor(typeof(MeetingHud_VotingComplete_MayorAbstainPatch)).Patch();
            }
            catch (System.Exception e)
            {
                Log.LogWarning("Mayor Abstain hook skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private void InstallRoleInfoCardHarmony()
        {
            var harmony = new Harmony(Guid + ".roleinfocard");
            try
            {
                harmony.CreateClassProcessor(typeof(HudManager_Update_RoleInfoCardPatch)).Patch();
            }
            catch (Exception e)
            {
                Log.LogWarning("Role info card skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private void InstallGameConfigHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(GameConfigHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(SettingMenu_OnEnable_ConfigOverlayPatch),
                typeof(GameSettingMenu_SetupFromData_ConfigOverlayPatch),
                typeof(HudManager_Update_ConfigOverlayPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Game config patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }

            _gameConfigHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        // The two patches that keep the game's role-option page and the mod's settings in
        // agreement: one after the host's option set is applied, one after a native row is
        // clicked.
        private void InstallNativeRoleOptionsHarmony()
        {
            var harmony = new Harmony(NativeRoleOptionsHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(RoleOptionsManager_ReadJson_NativeOptionsPatch),
                typeof(RoleSettingGameOption_ChangeRoleOption_NativeOptionsPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Native role option patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }

            _nativeRoleOptionsHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        private void InstallVersionBadgeHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(VersionBadgeHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(VersionShower_Start_VersionBadgePatch),
                typeof(PingTracker_Update_CreditPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Version badge patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            _versionBadgeHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        // All modifiers are off in most lobbies, and this is pure local upkeep, so the whole
        // tick is skipped rather than run and thrown away.
        private static void ModifiersTick()
        {
            if (!ModifierSystem.AnyEnabled) return;
            ModifierSystem.Tick();
            ModifierSystem.ApplyGiantScales();
        }

        private void RegisterModifiers()
        {
            RpcRegistration.Register(typeof(ModifierSystem));
            GameEvents.GameStarted += ModifierSystem.OnGameStarted;
            GameEvents.GameEnded += ModifierSystem.OnGameEnded;
            GameEvents.BeforeMurder += ModifierSystem.OnBeforeMurder;
            InstallModifierHarmony();
        }

        private void InstallModifierHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".modifiers");
            int ok = 0;
            RoleTick.EveryFrame("Modifiers", ModifiersTick);

            foreach (var patchType in new[]
            {
                typeof(PlayerPhysics_FixedUpdate_DrunkPatch),
                typeof(MeetingHud_CalculateVotes_TiebreakerPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Modifier patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }
            if (ok == 0) harmony.UnpatchSelf();
        }

        private void InstallSettingsRowsHarmony()
        {
            // The role rows appended below the game's own config options, plus their scroll fix.
            // The click router is MarshAPI's single UiRuntime patch, so there is one registry per
            // process. Each patch installs independently.
            var harmony = new Harmony(SettingsRowsHarmonyId);
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(SettingMenu_OnEnable_NativeSettingsRowsPatch),
                typeof(HudManager_Update_SettingsScrollPatch),
            })
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                    ok++;
                }
                catch (Exception e)
                {
                    Log.LogWarning("Settings-rows patch skipped (" + patchType.Name + "): " + e.Message);
                }
            }

            _settingsRowsHarmonyInstalled = ok > 0;
            if (ok == 0) harmony.UnpatchSelf();
        }

        private void InstallExileTextHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".exiletext");
            try
            {
                harmony.CreateClassProcessor(typeof(HudManager_Update_ExileTextFixPatch)).Patch();
                _exileTextHarmonyInstalled = true;
            }
            catch (Exception e)
            {
                Log.LogWarning("Exile text fix skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
        }

        private void InstallUpdateHarmony()
        {
            // Each patch is applied on its own, so a missing target on some build costs this
            // feature a hook and never stops the plugin loading.
            var harmony = new Harmony(Guid + ".updates");
            try
            {
                harmony.CreateClassProcessor(typeof(HudManager_Update_UpdateModalPatch)).Patch();
                _updateHarmonyInstalled = true;
            }
            catch (Exception e)
            {
                Log.LogWarning("Update modal skipped: " + e.Message);
                harmony.UnpatchSelf();
            }
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

            TorPage.Reset();
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

            // New-role lifecycle cleanup (delegate removal of unsubscribed
            // handlers and UnpatchSelf on an unpatched id are both no-ops).
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
