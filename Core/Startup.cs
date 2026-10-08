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
        private void StartUp()
        {
            BootTrace.Run("AtomicAPI.Register", () => AtomicAPI.Register("Town Of Roles", Version));

            RoleConfig.Init(Config);
            CommandConfig.Init(Config);
            CreatorColor.Init(Config);
            UpdateConfig.Init(Config);

            BootTrace.Run("UpdateApplier.Arm", () => UpdateApplier.Arm());

            var marshMod = ModRegistry.Register(
                id: "townofroles",
                displayName: "Town Of Roles",
                version: Version,
                author: "hallowmarsh",
                color: new UnityEngine.Color(1f, 0.41f, 0.71f));
            marshMod.Tag = "26 roles";

            SessionDisable.Apply();

            marshMod.OnEnabledChanged += enabled =>
            {
                if (enabled) return;
                SettingsTabPage.Reset();
            };

            if (RoleConfig.ModsMenu?.Value == true) ModsPage.Enable();

            try
            {
                BootTrace.Run("TownOfRolesRpcMux.Install", () => TownOfRolesRpcMux.Install());
            }
            catch (Exception ex)
            {
                Log.LogError("TownOfRoles RPC mux failed to install: " + ex);
            }

            RpcRegistration.Register(typeof(CreatorColor));
            GameEvents.GameStarted += CreatorColor.OnGameStarted;
            GameEvents.GameEnded += CreatorColor.OnGameEnded;

            if (UpdateConfig.Enabled?.Value == true)
            {
                BootTrace.Run(nameof(InstallUpdateHarmony), () => InstallUpdateHarmony());
                UpdateSystem.StartCheck();
            }

            if (CommandConfig.Enabled.Value)
            {
                CommandSystem.Initialize();
                RpcRegistration.Register(typeof(CommandSystem));
                BootTrace.Run(nameof(InstallCommandHarmony), () => InstallCommandHarmony());
            }
            else
            {
                ChatCommands.Enabled = () => false;
            }

            RoleRegistry.TeamCap = RoleConfig.RoleCap;
        }

        private void FinishStartUp()
        {
            BootTrace.Run("SettingsTabPage.Register", () => SettingsTabPage.Register());

            BootTrace.Run("HorseModeConfig.Init", () => HorseModeConfig.Init(Config));
            BootTrace.Run("HorseMode.Apply", () => HorseMode.Apply());
            GameEvents.GameStarted += _ => HorseMode.Apply();

            BootTrace.Run("RoleSettingsSync.Init", () => RoleSettingsSync.Init());

            NativeRoleOptions.OptionSource = RoleNativeOptions.Declare;
            if (NativeRoleOptions.Enabled)
            {
                BootTrace.Run(nameof(InstallNativeRoleOptionsHarmony), () => InstallNativeRoleOptionsHarmony());
            }

            RpcRegistration.Register(typeof(RoleSettingsSync));
            if (RoleConfig.GameConfigOverlay?.Value == true)
            {
                BootTrace.Run(nameof(InstallGameConfigHarmony), () => InstallGameConfigHarmony());
                GameEvents.GameStarted += RoleSettingsSync.OnGameStarted;
                GameEvents.PlayerJoined += RoleSettingsSync.OnPlayerJoined;
            }

            BootTrace.Run(nameof(InstallBootScreenHarmony), () => InstallBootScreenHarmony());
            BootTrace.Run(nameof(InstallBootTraceHarmony), () => InstallBootTraceHarmony());
            BootTrace.Mark("M1 Load complete - boot trace armed");

            BootTrace.Complete();
        }
    }
}
