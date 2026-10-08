using MarshAPI;
using InnerNet;
using UnityEngine;
using TMPro;
using TownOfRoles.Assets;
using TownOfRoles.Core;
using TownOfRoles.Roles.Engineer;
using TownOfRoles.Roles.Medic;
using TownOfRoles.Roles.Seer;
using TownOfRoles.Roles.Sheriff;
using TownOfRoles.Roles.Vigilante;
using TownOfRoles.Roles.Janitor;
using TownOfRoles.Roles.Altruist;
using TownOfRoles.Roles.Arsonist;
using TownOfRoles.Roles.Morphling;
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

namespace TownOfRoles.Roles
{
    internal static class CustomRoleAbilities
    {
        private static readonly EngineerButton Engineer = new();
        private static readonly MedicButton Medic = new();
        private static readonly SeerButton Seer = new();
        private static readonly SheriffButton Sheriff = new();
        private static readonly VigilanteButton Vigilante = new();
        private static readonly JanitorButton Janitor = new();
        private static readonly AltruistButton Altruist = new();
        private static readonly ArsonistButton Arsonist = new();
        private static readonly MorphlingSampleButton MorphlingSample = new();
        private static readonly MorphlingButton Morphling = new();
        private static readonly CamouflagerButton Camouflager = new();
        private static readonly SwooperButton Swooper = new();
        private static readonly UndertakerButton Undertaker = new();
        private static readonly TimeLordButton TimeLord = new();
        private static readonly ShifterButton Shifter = new();
        private static readonly GlitchMimicButton GlitchMimic = new();
        private static readonly GlitchHackButton GlitchHack = new();
        private static readonly GlitchKillButton GlitchKill = new();
        private static readonly MinerButton Miner = new();

        private static readonly UiAbilityButton[] All = { Engineer, Medic, Seer, Sheriff, Vigilante, Janitor, Altruist, Arsonist, MorphlingSample, Morphling, Camouflager, Swooper, Undertaker, TimeLord, Shifter, GlitchMimic, GlitchHack, GlitchKill, Miner };

        private static readonly BepInEx.Logging.ManualLogSource Log =
            BepInEx.Logging.Logger.CreateLogSource("TownOfRoles");

        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            UiAbilityButtons.Enabled = RoleConfig.CustomAbilityButtons?.Value != false;
            UiAbilityButtons.Register(All);
            UiAbilityButtons.NothingVisible += ShowNotFoundOnScreen;
        }

        private static void ShowNotFoundOnScreen(string message)
        {
            try { SystemChat.Show(message); }
            catch { }
        }

        internal static string DescribeButtons() => UiAbilityButtons.Report();

        private sealed class EngineerButton : UiAbilityButton
        {
            protected override string Name => "Fix Sab";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.EngineerFixCooldown, 30f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Engineer ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Engineer?.Value == true &&
                       EngineerSystem.IsEngineer(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => EngineerAbility.CanFixSab(PlayerControl.LocalPlayer);
            protected override void OnActivate() => EngineerAbility.TryFixSab(PlayerControl.LocalPlayer);
        }

        private sealed class MedicButton : UiAbilityButton
        {
            protected override string Name => "Protect";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Medic ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Medic?.Value == true &&
                       MedicSystem.IsMedic(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => MedicSystem.CanProtectNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => MedicSystem.TryProtect(PlayerControl.LocalPlayer);
            protected override int? UsesRemaining => MedicSystem.RemainingUses(PlayerControl.LocalPlayer);
        }

        private sealed class SeerButton : UiAbilityButton
        {
            protected override string Name => "Investigate";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Seer ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Seer?.Value == true &&
                       SeerSystem.IsSeer(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => SeerSystem.CanInvestigateNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => SeerSystem.TryInvestigate(PlayerControl.LocalPlayer);
            protected override int? UsesRemaining => SeerSystem.RemainingUses(PlayerControl.LocalPlayer);
        }

        private sealed class SheriffButton : UiAbilityButton
        {
            protected override string Name => "Shoot";
            protected override float Cooldown => SheriffOptions.KillCooldown;

            protected override float CooldownRemaining => SheriffAbilityHolder.SecondsRemaining;

            protected override Sprite CreateIcon(Sprite original) => RoleArt.Kill ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Sheriff?.Value == true &&
                       SheriffSystem.IsSheriff(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() =>
                !SheriffAbilityHolder.IsCoolingDown && SheriffSystem.HasTarget(PlayerControl.LocalPlayer);
            protected override void OnActivate()
            {
                var local = PlayerControl.LocalPlayer;
                if (!SheriffAbilityHolder.TryStartCooldown()) return;
                SheriffSystem.TryShoot(local);
            }
        }

        private sealed class VigilanteButton : UiAbilityButton
        {
            protected override string Name => "Shoot";
            protected override float CooldownRemaining => VigilanteSystem.SecondsRemaining(PlayerControl.LocalPlayer);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Kill ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Vigilante?.Value == true &&
                       VigilanteSystem.IsVigilante(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => VigilanteSystem.CanShootNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => VigilanteSystem.TryShoot(PlayerControl.LocalPlayer);
            protected override int? UsesRemaining => VigilanteSystem.RemainingUses(PlayerControl.LocalPlayer);
        }

        private sealed class JanitorButton : UiAbilityButton
        {
            protected override string Name => "Clean";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.JanitorCleanCooldown, 10f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Janitor ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Janitor?.Value == true &&
                       JanitorSystem.IsJanitor(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => JanitorSystem.CanCleanNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => JanitorSystem.TryClean(PlayerControl.LocalPlayer);
        }

        private sealed class AltruistButton : UiAbilityButton
        {
            protected override string Name => "Revive";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Revive ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Altruist?.Value == true &&
                       AltruistSystem.IsAltruist(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => AltruistSystem.CanReviveNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => AltruistSystem.TryRevive(PlayerControl.LocalPlayer);
            protected override int? UsesRemaining => AltruistSystem.RemainingUses(PlayerControl.LocalPlayer);
        }

        private sealed class ArsonistButton : UiAbilityButton
        {
            protected override string Name => "Douse";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.ArsonistDouseCooldown, 10f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Douse ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Arsonist?.Value == true &&
                       ArsonistSystem.IsArsonist(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => ArsonistSystem.CanDouseNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => ArsonistSystem.TryDouse(PlayerControl.LocalPlayer);
        }

        private sealed class MorphlingSampleButton : UiAbilityButton
        {
            protected override string Name => "Sample";
            public override string Group => "Morphling";

            protected override float CooldownRemaining => MorphlingSystem.SecondsRemaining(PlayerControl.LocalPlayer);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Sample ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Morphling?.Value == true &&
                       MorphlingSystem.IsMorphling(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => MorphlingSystem.CanSampleNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => MorphlingSystem.TrySample(PlayerControl.LocalPlayer);
        }

        private sealed class MorphlingButton : UiAbilityButton
        {
            protected override string Name => "Morph";

            public override string Group => "Morphling";
            protected override float CooldownRemaining => MorphlingSystem.SecondsRemaining(PlayerControl.LocalPlayer);

            protected override float EffectRemaining => MorphlingSystem.ShiftSecondsRemaining(PlayerControl.LocalPlayer);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Morph ?? original;

            protected override string DescribeGate()
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null && local.Data != null && !local.Data.IsDead &&
                    RoleConfig.Morphling?.Value == true && MorphlingSystem.IsMorphling(local))
                    return "no DNA sampled yet";
                return base.DescribeGate();
            }
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;

                return local != null && local.Data != null && RoleConfig.Morphling?.Value == true &&
                       MorphlingSystem.IsMorphling(local) && !local.Data.IsDead &&
                       MorphlingSystem.HasSample(local);
            }
            protected override bool CanActivate() => MorphlingSystem.CanMorphNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => MorphlingSystem.TryMorph(PlayerControl.LocalPlayer);
        }

        private sealed class CamouflagerButton : UiAbilityButton
        {
            protected override string Name => "Camouflage";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Camouflage ?? original;
            protected override float CooldownRemaining => CamouflagerSystem.SecondsRemaining(PlayerControl.LocalPlayer);
            protected override float EffectRemaining => CamouflagerSystem.ActiveSecondsRemaining;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Camouflager?.Value == true &&
                       CamouflagerSystem.IsCamouflager(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => CamouflagerSystem.CanCamouflageNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => CamouflagerSystem.TryCamouflage(PlayerControl.LocalPlayer);
        }

        private sealed class SwooperButton : UiAbilityButton
        {
            protected override string Name => "Swoop";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Swoop ?? original;
            protected override float CooldownRemaining => SwooperSystem.SecondsRemaining(PlayerControl.LocalPlayer);

            protected override float EffectRemaining => SwooperSystem.VanishedSecondsRemaining(PlayerControl.LocalPlayer);
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Swooper?.Value == true &&
                       SwooperSystem.IsSwooper(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => SwooperSystem.CanSwoopNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => SwooperSystem.TrySwoop(PlayerControl.LocalPlayer);
        }

        private sealed class TimeLordButton : UiAbilityButton
        {
            protected override string Name => "Rewind";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.RewindCooldown, 30f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Rewind ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.TimeLord?.Value == true &&
                       TimeLordSystem.IsTimeLord(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => TimeLordSystem.CanRewindNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => TimeLordSystem.TryRewind(PlayerControl.LocalPlayer);
        }

        private sealed class UndertakerButton : UiAbilityButton
        {
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.UndertakerDragCooldown, 10f);

            protected override string Name => "Drag";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Drag ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Undertaker?.Value == true &&
                       UndertakerSystem.IsUndertaker(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => UndertakerSystem.CanDragNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => UndertakerSystem.TryDrag(PlayerControl.LocalPlayer);
        }

        private sealed class ShifterButton : UiAbilityButton
        {
            protected override string Name => "Shift";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.ShiftCooldown, 30f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Shift ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Shifter?.Value == true &&
                       ShifterSystem.IsShifter(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => ShifterSystem.CanShiftNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => ShifterSystem.TryShift(PlayerControl.LocalPlayer);
        }

        private sealed class GlitchMimicButton : UiAbilityButton
        {
            protected override string Name => "Mimic";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.GlitchMimicCooldown, 30f);
            protected override float EffectRemaining => GlitchSystem.MimicSecondsRemaining(PlayerControl.LocalPlayer);
            public override string Group => "Glitch";
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Shift ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Glitch?.Value == true &&
                       GlitchSystem.IsGlitch(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => GlitchSystem.CanMimicNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => GlitchSystem.TryMimic(PlayerControl.LocalPlayer);
        }

        private sealed class GlitchHackButton : UiAbilityButton
        {
            protected override string Name => "Hack";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.GlitchHackCooldown, 30f);
            protected override float EffectRemaining => GlitchSystem.HackSecondsRemaining(PlayerControl.LocalPlayer);

            public override string Group => "Glitch";
            protected override Sprite CreateIcon(Sprite original) => original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Glitch?.Value == true &&
                       GlitchSystem.IsGlitch(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => GlitchSystem.CanHackNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => GlitchSystem.TryHack(PlayerControl.LocalPlayer);
        }

        private sealed class GlitchKillButton : UiAbilityButton
        {
            protected override string Name => "Kill";

            public override string Group => "Glitch";
            protected override float CooldownRemaining => GlitchSystem.SecondsUntilKillReady(PlayerControl.LocalPlayer);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Kill ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Glitch?.Value == true &&
                       GlitchSystem.IsGlitch(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => GlitchSystem.CanKillNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => GlitchSystem.TryKill(PlayerControl.LocalPlayer);
        }

        private sealed class MinerButton : UiAbilityButton
        {
            protected override string Name => "Mine";
            protected override float Cooldown => RoleConfig.Seconds(RoleConfig.MineCooldown, 30f);
            protected override Sprite CreateIcon(Sprite original) => RoleArt.Mine ?? original;
            protected override bool IsVisible()
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && local.Data != null && RoleConfig.Miner?.Value == true &&
                       MinerSystem.IsMiner(local) && !local.Data.IsDead;
            }
            protected override bool CanActivate() => MinerSystem.CanMineNow(PlayerControl.LocalPlayer);
            protected override void OnActivate() => MinerSystem.TryMine(PlayerControl.LocalPlayer);
        }
    }
}
