using System;
using System.Collections.Generic;
using MarshAPI;
using HarmonyLib;
using InnerNet;
using TownOfRoles.Commands;
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

namespace TownOfRoles.Core
{
    internal static class SessionReset
    {
        private static readonly List<Action> Resets = new List<Action>
        {
            () => MarshAPI.UiAbilityButtons.ResetAll(),

            SheriffAbilityHolder.Reset,
            SheriffSystem.Reset,
            JesterSystem.Reset,
            MedicSystem.Reset,
            SeerSystem.Reset,
            VigilanteSystem.Reset,
            EngineerAbility.Reset,
            AltruistSystem.Reset,
            ExecutionerSystem.Reset,
            InvestigatorSystem.Reset,
            SnitchSystem.Reset,
            SpySystem.Reset,

            JanitorSystem.Reset,
            MorphlingSystem.Reset,
            SwooperSystem.Reset,
            UnderdogSystem.Reset,
            UndertakerSystem.Reset,
            MinerSystem.Reset,
            CamouflagerSystem.Reset,
            ShifterSystem.Reset,

            GlitchSystem.Reset,
            ArsonistSystem.Reset,
            SwapperSystem.Reset,
            PhantomSystem.Reset,
            TimeLordSystem.Reset,

            ModifierSystem.Reset,
            AssassinSystem.Reset,
            KillLog.Reset,
            CommandState.Reset,
            VisualEffects.Reset,
        };

        internal static void ResetAll()
        {
            foreach (var reset in Resets)
            {
                try { reset(); }
                catch (Exception) {   }
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
        internal static class AmongUsClient_ExitGame_SessionPatch
        {
            private static void Postfix(DisconnectReasons reason)
            {
                _ = reason;
                ResetAll();
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleDisconnect),
            new[] { typeof(DisconnectReasons), typeof(string) })]
        internal static class InnerNetClient_HandleDisconnect_SessionPatch
        {
            private static void Postfix(DisconnectReasons reason, string stringReason)
            {
                _ = reason; _ = stringReason;
                ResetAll();
            }
        }
    }
}
