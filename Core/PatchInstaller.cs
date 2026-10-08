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
    public sealed partial class TownOfRolesPlugin
    {
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

        private void InstallJesterHarmony()
        {
            _jesterHarmonyInstalled = true;
        }

        private void InstallPresentationHarmony()
        {
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

        private void InstallAssassinHarmony()
        {
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

        private void InstallMayorHarmony()
        {
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

        private void InstallExecutionerHarmony()
        {
            RoleTick.EveryFrame("Executioner", ExecutionerSystem.Tick);

            var harmony = new Harmony(Guid + ".executioner");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_ExecutionerPatch),

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

        private void InstallLoversHarmony()
        {
            var harmony = new Harmony(LoversHarmonyId);
            int ok = 0;
            RoleTick.EveryFrame("Lovers", LoverSystem.Tick);

            foreach (var patchType in new[]
            {
                typeof(ShipStatus_CheckEndCriteria_LoversPatch),

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

        private void InstallArsonistHarmony()
        {
            var harmony = new Harmony(Guid + ".arsonist");
            int ok = 0;
            RoleTick.EveryFrame("Arsonist", ArsonistSystem.Tick);

            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_ArsonistPatch),

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

        private void InstallSwapperHarmony()
        {
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

        private void InstallBatch4Harmony()
        {
            var harmony = new Harmony(Guid + ".batch4");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_PhantomPatch),

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

        private void InstallSessionResetHarmony()
        {
            var harmony = new Harmony(Guid + ".abilities");
            try
            {
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

        private void InstallCreatorColorHarmony()
        {
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

        private void InstallBatch5Harmony()
        {
            var harmony = new Harmony(Guid + ".batch5");
            int ok = 0;
            foreach (var patchType in new[]
            {
                typeof(ExileController_Begin_GlitchPatch),

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

        private void InstallModifierHarmony()
        {
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

        private void InstallHooks()
        {
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
                UiAbilityButtons.Enabled = false;
            }

            if (RoleConfig.PresentationEnabled?.Value == true)
                BootTrace.Run(nameof(InstallPresentationHarmony), () => InstallPresentationHarmony());

            BootTrace.Run(nameof(InstallSettingsRowsHarmony), () => InstallSettingsRowsHarmony());
            BootTrace.Run(nameof(InstallExileTextHarmony), () => InstallExileTextHarmony());
            BootTrace.Run(nameof(InstallCreatorColorHarmony), () => InstallCreatorColorHarmony());
            BootTrace.Run(nameof(InstallVersionBadgeHarmony), () => InstallVersionBadgeHarmony());
            BootTrace.Run(nameof(InstallRoleInfoCardHarmony), () => InstallRoleInfoCardHarmony());
        }
    }
}
