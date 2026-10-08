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
        private static void RegisterSheriff()
        {
            RpcRegistration.Register(typeof(SheriffSystem));

            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Sheriff.SheriffRole());

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

            GameEvents.PlayerExiled += JesterSystem.OnPlayerExiled;
            GameEvents.GameStarted += JesterSystem.OnGameStarted;
            GameEvents.GameEnded += JesterSystem.OnGameEnded;
            _jesterEventHooksInstalled = true;
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

        private static void RegisterJanitor()
        {
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
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Mayor.MayorRole());
        }

        private static void RegisterExecutioner()
        {
            RpcRegistration.Register(typeof(ExecutionerSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Executioner.ExecutionerRole());
            GameEvents.PlayerExiled += ExecutionerSystem.OnPlayerExiled;

            GameEvents.BeforeMurder += ExecutionerSystem.OnBeforeMurder;
            GameEvents.GameStarted += ExecutionerSystem.OnGameStarted;
            GameEvents.GameEnded += ExecutionerSystem.OnGameEnded;
        }

        private static void RegisterLovers()
        {
            RpcRegistration.Register(typeof(LoverSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Lovers.LoverRole());
            GameEvents.GameStarted += LoverSystem.OnGameStarted;
            GameEvents.GameEnded += LoverSystem.OnGameEnded;
        }

        private static void RegisterArsonist()
        {
            RpcRegistration.Register(typeof(ArsonistSystem));
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Arsonist.ArsonistRole());
            GameEvents.GameStarted += ArsonistSystem.OnGameStarted;
            GameEvents.GameEnded += ArsonistSystem.OnGameEnded;
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

        private static void RegisterFirstBatchRoles()
        {
            if (RoleConfig.Medic.Value)
            {
                RpcRegistration.Register(typeof(MedicSystem));
                RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Medic.MedicRole());

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

        private static void RegisterEngineer()
        {
            RoleRegistry.RegisterVirtual(new TownOfRoles.Roles.Engineer.EngineerRole());

            RoleTick.EveryFrame("EngineerVentTint", TownOfRoles.Roles.Engineer.EngineerVentTint.Tick);

            GameEvents.GameStarted += EngineerAbility.OnGameStarted;
            GameEvents.GameEnded += EngineerAbility.OnGameEnded;
            _engineerEventHooksInstalled = true;
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

        private void RegisterRoles()
        {
            if (RoleConfig.Sheriff.Value)
                BootTrace.Run(nameof(RegisterSheriff), () => RegisterSheriff());

            if (RoleConfig.Engineer.Value)
                BootTrace.Run(nameof(RegisterEngineer), () => RegisterEngineer());

            if (RoleConfig.Jester.Value)
            {
                BootTrace.Run(nameof(RegisterJester), () => RegisterJester());
                BootTrace.Run(nameof(InstallJesterHarmony), () => InstallJesterHarmony());
            }

            if (RoleConfig.Medic.Value || RoleConfig.Seer.Value || RoleConfig.Vigilante.Value)
                BootTrace.Run(nameof(RegisterFirstBatchRoles), () => RegisterFirstBatchRoles());

            BootTrace.Run("CustomRoleAbilities.Initialize", () => CustomRoleAbilities.Initialize());

            if (RoleConfig.Assassin.Value)
            {
                BootTrace.Run(nameof(RegisterAssassin), () => RegisterAssassin());
                BootTrace.Run(nameof(InstallAssassinHarmony), () => InstallAssassinHarmony());
            }

            if (RoleConfig.Janitor.Value)
                BootTrace.Run(nameof(RegisterJanitor), () => RegisterJanitor());

            if (RoleConfig.Altruist.Value)
                BootTrace.Run(nameof(RegisterAltruist), () => RegisterAltruist());

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
                BootTrace.Run(nameof(RegisterMorphling), () => RegisterMorphling());

            if (RoleConfig.Spy.Value)
                BootTrace.Run(nameof(RegisterSpy), () => RegisterSpy());

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
                BootTrace.Run(nameof(InstallMayorAbstainHarmony), () => InstallMayorAbstainHarmony());

            BootTrace.Run(nameof(RegisterModifiers), () => RegisterModifiers());
        }
    }
}
