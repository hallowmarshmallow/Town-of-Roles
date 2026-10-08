using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace TownOfRoles.Core
{
    internal static class RoleConfig
    {
        public static ConfigEntry<bool> Sheriff { get; private set; }
        public static ConfigEntry<bool> Engineer { get; private set; }
        public static ConfigEntry<bool> Jester { get; private set; }
        public static ConfigEntry<bool> Medic { get; private set; }
        public static ConfigEntry<bool> Seer { get; private set; }
        public static ConfigEntry<bool> Vigilante { get; private set; }
        public static ConfigEntry<bool> Assassin { get; private set; }
        public static ConfigEntry<bool> Janitor { get; private set; }
        public static ConfigEntry<bool> Altruist { get; private set; }
        public static ConfigEntry<bool> Mayor { get; private set; }
        public static ConfigEntry<bool> Executioner { get; private set; }
        public static ConfigEntry<bool> Arsonist { get; private set; }
        public static ConfigEntry<bool> Swapper { get; private set; }
        public static ConfigEntry<bool> Morphling { get; private set; }
        public static ConfigEntry<bool> Spy { get; private set; }
        public static ConfigEntry<bool> Camouflager { get; private set; }
        public static ConfigEntry<bool> Swooper { get; private set; }
        public static ConfigEntry<bool> Underdog { get; private set; }
        public static ConfigEntry<bool> Undertaker { get; private set; }
        public static ConfigEntry<bool> Investigator { get; private set; }
        public static ConfigEntry<bool> TimeLord { get; private set; }
        public static ConfigEntry<bool> Snitch { get; private set; }
        public static ConfigEntry<bool> Phantom { get; private set; }
        public static ConfigEntry<bool> Shifter { get; private set; }
        public static ConfigEntry<bool> Glitch { get; private set; }
        public static ConfigEntry<bool> Miner { get; private set; }
        public static ConfigEntry<bool> Lovers { get; private set; }

        public static ConfigEntry<int> MaxImpostorRoles { get; private set; }

        public static ConfigEntry<int> MaxNeutralRoles { get; private set; }

        public static ConfigEntry<int> SheriffCount { get; private set; }
        public static ConfigEntry<float> SheriffChance { get; private set; }
        public static ConfigEntry<int> EngineerCount { get; private set; }
        public static ConfigEntry<float> EngineerChance { get; private set; }
        public static ConfigEntry<int> JesterCount { get; private set; }
        public static ConfigEntry<float> JesterChance { get; private set; }
        public static ConfigEntry<int> MedicCount { get; private set; }
        public static ConfigEntry<float> MedicChance { get; private set; }
        public static ConfigEntry<int> SeerCount { get; private set; }
        public static ConfigEntry<float> SeerChance { get; private set; }
        public static ConfigEntry<int> VigilanteCount { get; private set; }
        public static ConfigEntry<float> VigilanteChance { get; private set; }
        public static ConfigEntry<int> AssassinCount { get; private set; }
        public static ConfigEntry<float> AssassinChance { get; private set; }
        public static ConfigEntry<int> JanitorCount { get; private set; }
        public static ConfigEntry<float> JanitorChance { get; private set; }
        public static ConfigEntry<int> AltruistCount { get; private set; }
        public static ConfigEntry<float> AltruistChance { get; private set; }
        public static ConfigEntry<int> MayorCount { get; private set; }
        public static ConfigEntry<float> MayorChance { get; private set; }
        public static ConfigEntry<int> ExecutionerCount { get; private set; }
        public static ConfigEntry<float> ExecutionerChance { get; private set; }
        public static ConfigEntry<int> ArsonistCount { get; private set; }
        public static ConfigEntry<float> ArsonistChance { get; private set; }
        public static ConfigEntry<int> SwapperCount { get; private set; }
        public static ConfigEntry<float> SwapperChance { get; private set; }
        public static ConfigEntry<int> MorphlingCount { get; private set; }
        public static ConfigEntry<float> MorphlingChance { get; private set; }
        public static ConfigEntry<int> SpyCount { get; private set; }
        public static ConfigEntry<float> SpyChance { get; private set; }
        public static ConfigEntry<int> CamouflagerCount { get; private set; }
        public static ConfigEntry<float> CamouflagerChance { get; private set; }
        public static ConfigEntry<int> SwooperCount { get; private set; }
        public static ConfigEntry<float> SwooperChance { get; private set; }
        public static ConfigEntry<int> UnderdogCount { get; private set; }
        public static ConfigEntry<float> UnderdogChance { get; private set; }
        public static ConfigEntry<int> UndertakerCount { get; private set; }
        public static ConfigEntry<float> UndertakerChance { get; private set; }
        public static ConfigEntry<int> InvestigatorCount { get; private set; }
        public static ConfigEntry<float> InvestigatorChance { get; private set; }
        public static ConfigEntry<int> TimeLordCount { get; private set; }
        public static ConfigEntry<float> TimeLordChance { get; private set; }
        public static ConfigEntry<int> SnitchCount { get; private set; }
        public static ConfigEntry<float> SnitchChance { get; private set; }
        public static ConfigEntry<int> PhantomCount { get; private set; }
        public static ConfigEntry<float> PhantomChance { get; private set; }
        public static ConfigEntry<int> ShifterCount { get; private set; }
        public static ConfigEntry<float> ShifterChance { get; private set; }
        public static ConfigEntry<int> GlitchCount { get; private set; }
        public static ConfigEntry<float> GlitchChance { get; private set; }
        public static ConfigEntry<int> MinerCount { get; private set; }
        public static ConfigEntry<float> MinerChance { get; private set; }
        public static ConfigEntry<int> LoversCount { get; private set; }
        public static ConfigEntry<float> LoversChance { get; private set; }
        public static ConfigEntry<bool> LoversBothDie { get; private set; }
        public static ConfigEntry<bool> LoversImpostorLover { get; private set; }

        public static ConfigEntry<float> SheriffKillCooldown { get; private set; }
        public static ConfigEntry<bool> SheriffKillOther { get; private set; }
        public static ConfigEntry<bool> SheriffBodyReport { get; private set; }
        public static ConfigEntry<float> EngineerFixCooldown { get; private set; }
        public static ConfigEntry<int> MedicUses { get; private set; }
        public static ConfigEntry<float> MedicCooldown { get; private set; }
        public static ConfigEntry<bool> MedicShieldBreaksOnKill { get; private set; }
        public static ConfigEntry<int> MedicReportNameDuration { get; private set; }
        public static ConfigEntry<int> MedicReportColorDuration { get; private set; }
        public static ConfigEntry<int> SeerUses { get; private set; }
        public static ConfigEntry<float> SeerCooldown { get; private set; }
        public static ConfigEntry<string> SeerRevealMode { get; private set; }
        public static ConfigEntry<int> VigilanteShots { get; private set; }
        public static ConfigEntry<float> VigilanteCooldown { get; private set; }
        public static ConfigEntry<bool> AssassinMultiKill { get; private set; }
        public static ConfigEntry<bool> AssassinMeetingUi { get; private set; }
        public static ConfigEntry<float> JanitorCleanCooldown { get; private set; }
        public static ConfigEntry<int> AltruistUses { get; private set; }
        public static ConfigEntry<float> AltruistCooldown { get; private set; }
        public static ConfigEntry<int> MayorVoteBank { get; private set; }
        public static ConfigEntry<bool> SheriffKillsNeutrals { get; private set; }
        public static ConfigEntry<float> ArsonistDouseCooldown { get; private set; }
        public static ConfigEntry<bool> ExecutionerConvertOnTargetDeath { get; private set; }
        public static ConfigEntry<string> ExecutionerConvertRole { get; private set; }
        public static ConfigEntry<float> MorphlingMorphCooldown { get; private set; }
        public static ConfigEntry<float> MorphlingMorphDuration { get; private set; }
        public static ConfigEntry<float> CamouflageCooldown { get; private set; }
        public static ConfigEntry<float> CamouflageDuration { get; private set; }
        public static ConfigEntry<float> SwoopCooldown { get; private set; }
        public static ConfigEntry<float> SwoopDuration { get; private set; }
        public static ConfigEntry<float> UnderdogCooldownMultiplier { get; private set; }
        public static ConfigEntry<float> UndertakerDragCooldown { get; private set; }
        public static ConfigEntry<float> FootprintInterval { get; private set; }
        public static ConfigEntry<float> FootprintDuration { get; private set; }
        public static ConfigEntry<bool> FootprintAnonymous { get; private set; }
        public static ConfigEntry<float> RewindCooldown { get; private set; }
        public static ConfigEntry<float> RewindSeconds { get; private set; }
        public static ConfigEntry<bool> RewindRevive { get; private set; }
        public static ConfigEntry<float> ShiftCooldown { get; private set; }
        public static ConfigEntry<float> GlitchMimicCooldown { get; private set; }
        public static ConfigEntry<float> GlitchMimicDuration { get; private set; }
        public static ConfigEntry<float> GlitchHackCooldown { get; private set; }
        public static ConfigEntry<float> GlitchHackDuration { get; private set; }
        public static ConfigEntry<float> GlitchKillCooldown { get; private set; }
        public static ConfigEntry<float> MineCooldown { get; private set; }
        public static ConfigEntry<bool> PresentationEnabled { get; private set; }
        public static ConfigEntry<bool> DeadSeeRoles { get; private set; }
        public static ConfigEntry<bool> ImpostorSeeRoles { get; private set; }

        public static ConfigEntry<bool> ModifierTorch { get; private set; }
        public static ConfigEntry<float> ModifierTorchProbability { get; private set; }
        public static ConfigEntry<bool> ModifierDiseased { get; private set; }
        public static ConfigEntry<float> ModifierDiseasedProbability { get; private set; }
        public static ConfigEntry<bool> ModifierFlash { get; private set; }
        public static ConfigEntry<float> ModifierFlashProbability { get; private set; }
        public static ConfigEntry<bool> ModifierTiebreaker { get; private set; }
        public static ConfigEntry<float> ModifierTiebreakerProbability { get; private set; }
        public static ConfigEntry<bool> ModifierDrunk { get; private set; }
        public static ConfigEntry<float> ModifierDrunkProbability { get; private set; }
        public static ConfigEntry<bool> ModifierGiant { get; private set; }
        public static ConfigEntry<float> ModifierGiantProbability { get; private set; }
        public static ConfigEntry<bool> ModifierButtonBarry { get; private set; }
        public static ConfigEntry<float> ModifierButtonBarryProbability { get; private set; }

        public static ConfigEntry<bool> GameplayHooks { get; private set; }
        public static ConfigEntry<bool> CustomAbilityButtons { get; private set; }
        public static ConfigEntry<bool> GameConfigOverlay { get; private set; }
        public static ConfigEntry<bool> NativeMenuRows { get; private set; }
        public static ConfigEntry<bool> ModsMenu { get; private set; }

        public static ConfigEntry<bool> BootScreenPreload { get; private set; }

        public static ConfigEntry<bool> BootScreenBadge { get; private set; }

        public static void Init(ConfigFile config)
        {
            var fileValues = ReadConfigFile(config);

            Sheriff = BindRoleToggle(config, "Crewmate Roles", "Sheriff", "Add the Sheriff to the role pool.");
            Engineer = BindRoleToggle(config, "Crewmate Roles", "Engineer", "Add the Engineer to the role pool.");
            Medic = BindRoleToggle(config, "Crewmate Roles", "Medic", "Add the Medic to the role pool.");
            Seer = BindRoleToggle(config, "Crewmate Roles", "Seer", "Add the Seer to the role pool.");
            Vigilante = BindRoleToggle(config, "Crewmate Roles", "Vigilante", "Add the Vigilante to the role pool.");
            Assassin = BindRoleToggle(config, "Impostor Roles", "Assassin", "Add the Assassin to the role pool.");
            Janitor = BindRoleToggle(config, "Impostor Roles", "Janitor", "Add the Janitor to the role pool.");
            Altruist = BindRoleToggle(config, "Crewmate Roles", "Altruist", "Add the Altruist to the role pool.");
            Mayor = BindRoleToggle(config, "Crewmate Roles", "Mayor", "Add the Mayor to the role pool.");
            Jester = BindRoleToggle(config, "Neutral Roles", "Jester", "Add the Jester to the role pool.");
            Executioner = BindRoleToggle(config, "Neutral Roles", "Executioner", "Add the Executioner to the role pool.");
            Arsonist = BindRoleToggle(config, "Neutral Roles", "Arsonist", "Add the Arsonist to the role pool.");
            Swapper = BindRoleToggle(config, "Crewmate Roles", "Swapper", "Add the Swapper to the role pool.");
            Morphling = BindRoleToggle(config, "Impostor Roles", "Morphling", "Add the Morphling to the role pool.");
            Spy = BindRoleToggle(config, "Crewmate Roles", "Spy", "Add the Spy to the role pool.");
            Camouflager = BindRoleToggle(config, "Impostor Roles", "Camouflager", "Add the Camouflager to the role pool.");
            Swooper = BindRoleToggle(config, "Impostor Roles", "Swooper", "Add the Swooper to the role pool.");
            Underdog = BindRoleToggle(config, "Impostor Roles", "Underdog", "Add the Underdog to the role pool.");
            Undertaker = BindRoleToggle(config, "Impostor Roles", "Undertaker", "Add the Undertaker to the role pool.");
            Investigator = BindRoleToggle(config, "Crewmate Roles", "Investigator", "Add the Investigator to the role pool.");
            TimeLord = BindRoleToggle(config, "Crewmate Roles", "TimeLord", "Add the Time Lord to the role pool.");
            Snitch = BindRoleToggle(config, "Crewmate Roles", "Snitch", "Add the Snitch to the role pool.");
            Phantom = BindRoleToggle(config, "Neutral Roles", "Phantom", "Add the Phantom to the role pool.");
            Shifter = BindRoleToggle(config, "Neutral Roles", "Shifter", "Add the Shifter to the role pool.");
            Glitch = BindRoleToggle(config, "Neutral Roles", "Glitch", "Add The Glitch to the role pool.");
            Miner = BindRoleToggle(config, "Impostor Roles", "Miner", "Add the Miner to the role pool.");
            Lovers = BindRoleToggle(config, "Neutral Roles", "Lovers", "Add the Lovers pair to the role pool: two players who win together and die together.");

            SheriffCount = BindCount(config, "Crewmate Roles", "SheriffCount", 1, "Maximum Sheriffs assigned per game.");
            SheriffChance = BindChance(config, "Crewmate Roles", "SheriffChance", 100f, "Chance for each Sheriff slot to be filled.");
            EngineerCount = BindCount(config, "Crewmate Roles", "EngineerCount", 1, "Maximum Engineers assigned per game.");
            EngineerChance = BindChance(config, "Crewmate Roles", "EngineerChance", 100f, "Chance for each Engineer slot to be filled.");
            MedicCount = BindCount(config, "Crewmate Roles", "MedicCount", 1, "Maximum Medics assigned per game.");
            MedicChance = BindChance(config, "Crewmate Roles", "MedicChance", 100f, "Chance for each Medic slot to be filled.");
            SeerCount = BindCount(config, "Crewmate Roles", "SeerCount", 1, "Maximum Seers assigned per game.");
            SeerChance = BindChance(config, "Crewmate Roles", "SeerChance", 100f, "Chance for each Seer slot to be filled.");
            VigilanteCount = BindCount(config, "Crewmate Roles", "VigilanteCount", 1, "Maximum Vigilantes assigned per game.");
            VigilanteChance = BindChance(config, "Crewmate Roles", "VigilanteChance", 100f, "Chance for each Vigilante slot to be filled.");
            AssassinCount = BindCount(config, "Impostor Roles", "AssassinCount", 1, "Maximum Assassins assigned per game.");
            AssassinChance = BindChance(config, "Impostor Roles", "AssassinChance", 100f, "Chance for each Assassin slot to be filled.");
            JanitorCount = BindCount(config, "Impostor Roles", "JanitorCount", 1, "Maximum Janitors assigned per game.");
            JanitorChance = BindChance(config, "Impostor Roles", "JanitorChance", 100f, "Chance for each Janitor slot to be filled.");
            AltruistCount = BindCount(config, "Crewmate Roles", "AltruistCount", 1, "Maximum Altruists assigned per game.");
            AltruistChance = BindChance(config, "Crewmate Roles", "AltruistChance", 100f, "Chance for each Altruist slot to be filled.");
            MayorCount = BindCount(config, "Crewmate Roles", "MayorCount", 1, "Maximum Mayors assigned per game.");
            MayorChance = BindChance(config, "Crewmate Roles", "MayorChance", 100f, "Chance for each Mayor slot to be filled.");
            JesterCount = BindCount(config, "Neutral Roles", "JesterCount", 1, "Maximum Jesters assigned per game.");
            JesterChance = BindChance(config, "Neutral Roles", "JesterChance", 100f, "Chance for each Jester slot to be filled.");
            ExecutionerCount = BindCount(config, "Neutral Roles", "ExecutionerCount", 1, "Maximum Executioners assigned per game.");
            ExecutionerChance = BindChance(config, "Neutral Roles", "ExecutionerChance", 100f, "Chance for each Executioner slot to be filled.");
            ArsonistCount = BindCount(config, "Neutral Roles", "ArsonistCount", 1, "Maximum Arsonists assigned per game.");
            ArsonistChance = BindChance(config, "Neutral Roles", "ArsonistChance", 100f, "Chance for each Arsonist slot to be filled.");
            SwapperCount = BindCount(config, "Crewmate Roles", "SwapperCount", 1, "Maximum Swappers assigned per game.");
            SwapperChance = BindChance(config, "Crewmate Roles", "SwapperChance", 100f, "Chance for each Swapper slot to be filled.");
            MorphlingCount = BindCount(config, "Impostor Roles", "MorphlingCount", 1, "Maximum Morphlings assigned per game.");
            MorphlingChance = BindChance(config, "Impostor Roles", "MorphlingChance", 100f, "Chance for each Morphling slot to be filled.");
            SpyCount = BindCount(config, "Crewmate Roles", "SpyCount", 1, "Maximum Spies assigned per game.");
            SpyChance = BindChance(config, "Crewmate Roles", "SpyChance", 100f, "Chance for each Spy slot to be filled.");
            CamouflagerCount = BindCount(config, "Impostor Roles", "CamouflagerCount", 1, "Maximum Camouflagers assigned per game.");
            CamouflagerChance = BindChance(config, "Impostor Roles", "CamouflagerChance", 100f, "Chance for each Camouflager slot to be filled.");
            SwooperCount = BindCount(config, "Impostor Roles", "SwooperCount", 1, "Maximum Swoopers assigned per game.");
            SwooperChance = BindChance(config, "Impostor Roles", "SwooperChance", 100f, "Chance for each Swooper slot to be filled.");
            UnderdogCount = BindCount(config, "Impostor Roles", "UnderdogCount", 1, "Maximum Underdogs assigned per game.");
            UnderdogChance = BindChance(config, "Impostor Roles", "UnderdogChance", 100f, "Chance for each Underdog slot to be filled.");
            UndertakerCount = BindCount(config, "Impostor Roles", "UndertakerCount", 1, "Maximum Undertakers assigned per game.");
            UndertakerChance = BindChance(config, "Impostor Roles", "UndertakerChance", 100f, "Chance for each Undertaker slot to be filled.");
            InvestigatorCount = BindCount(config, "Crewmate Roles", "InvestigatorCount", 1, "Maximum Investigators assigned per game.");
            InvestigatorChance = BindChance(config, "Crewmate Roles", "InvestigatorChance", 100f, "Chance for each Investigator slot to be filled.");
            TimeLordCount = BindCount(config, "Crewmate Roles", "TimeLordCount", 1, "Maximum Time Lords assigned per game.");
            TimeLordChance = BindChance(config, "Crewmate Roles", "TimeLordChance", 100f, "Chance for each Time Lord slot to be filled.");
            SnitchCount = BindCount(config, "Crewmate Roles", "SnitchCount", 1, "Maximum Snitches assigned per game.");
            SnitchChance = BindChance(config, "Crewmate Roles", "SnitchChance", 100f, "Chance for each Snitch slot to be filled.");
            PhantomCount = BindCount(config, "Neutral Roles", "PhantomCount", 1, "Maximum Phantoms assigned per game.");
            PhantomChance = BindChance(config, "Neutral Roles", "PhantomChance", 100f, "Chance for each Phantom slot to be filled.");
            ShifterCount = BindCount(config, "Neutral Roles", "ShifterCount", 1, "Maximum Shifters assigned per game.");
            ShifterChance = BindChance(config, "Neutral Roles", "ShifterChance", 100f, "Chance for each Shifter slot to be filled.");
            GlitchCount = BindCount(config, "Neutral Roles", "GlitchCount", 1, "Maximum Glitches assigned per game.");
            GlitchChance = BindChance(config, "Neutral Roles", "GlitchChance", 100f, "Chance for each Glitch slot to be filled.");
            MinerCount = BindCount(config, "Impostor Roles", "MinerCount", 1, "Maximum Miners assigned per game.");
            MinerChance = BindChance(config, "Impostor Roles", "MinerChance", 100f, "Chance for each Miner slot to be filled.");
            LoversCount = BindCount(config, "Neutral Roles", "LoversCount", 1, "Number of Lover pairs to assign per game. Each pair needs two Crewmates (plus an Impostor when enabled); 1 pair is the original Town-Of-Us behaviour.");
            LoversChance = BindChance(config, "Neutral Roles", "LoversChance", 100f, "Chance the Lovers pair is assigned at all.");

            SheriffKillCooldown = BindSeconds(config, "Crewmate Roles", "SheriffKillCooldown", 25f, "Seconds between Sheriff shots. Town-Of-Us default: 25.");
            SheriffKillOther = config.Bind("Crewmate Roles", "SheriffKillOther", false, "When Sheriff shoots a non-enemy, the target also dies. Town-Of-Us 'Sheriff Miskill Kills Crewmate' default: false (only the Sheriff dies).");
            SheriffBodyReport = config.Bind("Crewmate Roles", "SheriffBodyReport", true, "Allow the Sheriff to report bodies they shot themselves. Town-Of-Us default: true.");
            EngineerFixCooldown = BindSeconds(config, "Crewmate Roles", "EngineerFixCooldown", 30f, "Seconds between Engineer Fix Sab uses.");
            MedicUses = BindCount(config, "Crewmate Roles", "MedicUses", 1, "Number of shields the Medic can place per game.");
            MedicCooldown = BindSeconds(config, "Crewmate Roles", "MedicCooldown", 0f, "Seconds between Medic shields; zero allows immediate next use.");
            MedicShieldBreaksOnKill = config.Bind("Crewmate Roles", "MedicShieldBreaksOnKill", false, "Consume the shield when it blocks a murder. Town-Of-Us 'Shield breaks on murder attempt' default: false.");
            MedicReportNameDuration = config.Bind("Crewmate Roles", "MedicReportNameDuration", 0, "Body Report: seconds after a kill within which the Medic learns the killer's name. Town-Of-Us default: 0 (name is never revealed by default).");
            MedicReportColorDuration = config.Bind("Crewmate Roles", "MedicReportColorDuration", 15, "Body Report: seconds after a kill within which the Medic learns the killer's color shade. Town-Of-Us default: 15.");
            SeerUses = BindCount(config, "Crewmate Roles", "SeerUses", 1, "Number of investigations the Seer can perform per game.");
            SeerCooldown = BindSeconds(config, "Crewmate Roles", "SeerCooldown", 25f, "Seconds between Seer investigations. Town-Of-Us default: 25.");
            SeerRevealMode = config.Bind("Crewmate Roles", "SeerRevealMode", "Faction", "Faction or Role. Faction is safer for virtual custom roles.");
            VigilanteShots = BindCount(config, "Crewmate Roles", "VigilanteShots", 1, "Number of shots the Vigilante can take per game.");
            VigilanteCooldown = BindSeconds(config, "Crewmate Roles", "VigilanteCooldown", 0f, "Seconds between Vigilante shots; zero allows immediate next use.");
            AssassinMultiKill = config.Bind("Impostor Roles", "AssassinMultiKill", true, "Allow more than one successful Assassin guess in a meeting. Town-Of-Us default: true.");
            AssassinMeetingUi = config.Bind("Impostor Roles", "AssassinMeetingButtons", true, "Show Cycle/Guess buttons beside eligible players during meetings.");
            JanitorCleanCooldown = BindSeconds(config, "Impostor Roles", "JanitorCleanCooldown", 10f, "Seconds between Janitor cleans.");
            AltruistUses = BindCount(config, "Crewmate Roles", "AltruistUses", 1, "Number of revives the Altruist can perform (they die on use, so one per round).");
            AltruistCooldown = BindSeconds(config, "Crewmate Roles", "AltruistCooldown", 0f, "Seconds between Altruist revives; zero allows immediate next use.");
            MayorVoteBank = BindCount(config, "Crewmate Roles", "MayorVoteBank", 2, "How many votes the Mayor casts in a meeting.");
            SheriffKillsNeutrals = config.Bind("Crewmate Roles", "SheriffKillsNeutrals", true, "Allow the Sheriff to shoot neutral roles (Jester, Executioner).");
            ArsonistDouseCooldown = BindSeconds(config, "Neutral Roles", "ArsonistDouseCooldown", 25f, "Seconds between Arsonist douses. Town-Of-Us default: 25.");
            ExecutionerConvertOnTargetDeath = config.Bind("Neutral Roles", "ExecutionerConvertOnTargetDeath", true, "When the Executioner's target dies without being voted out, convert the Executioner to another role.");
            ExecutionerConvertRole = config.Bind("Neutral Roles", "ExecutionerConvertRole", "Jester", "Role the Executioner becomes when their target dies: Jester or Crewmate.");

            LoversBothDie = config.Bind("Neutral Roles", "LoversBothDie", true, "When one Lover dies, the other dies too. Town-Of-Us 'Both Lovers Die' default: true.");
            LoversImpostorLover = config.Bind("Neutral Roles", "LoversImpostorLover", true, "Allow one Lover of a pair to be an Impostor; the pair still wins together. Town Of Us rolls this chance, this build applies it whenever an Impostor is available.");
            MorphlingMorphCooldown = BindSeconds(config, "Impostor Roles", "MorphlingMorphCooldown", 25f, "Seconds between Morphling morphs. Town-Of-Us default: 25.");
            MorphlingMorphDuration = BindSeconds(config, "Impostor Roles", "MorphlingMorphDuration", 10f, "How long a Morphling morph lasts. Town-Of-Us default: 10.");
            CamouflageCooldown = BindSeconds(config, "Impostor Roles", "CamouflageCooldown", 25f, "Seconds between Camouflager camouflages. Town-Of-Us default: 25.");
            CamouflageDuration = BindSeconds(config, "Impostor Roles", "CamouflageDuration", 10f, "How long a Camouflager camouflage lasts. Town-Of-Us default: 10.");
            SwoopCooldown = BindSeconds(config, "Impostor Roles", "SwoopCooldown", 25f, "Seconds between Swooper swoops. Town-Of-Us default: 25.");
            SwoopDuration = BindSeconds(config, "Impostor Roles", "SwoopDuration", 10f, "How long a Swooper swoop lasts. Town-Of-Us default: 10.");
            UnderdogCooldownMultiplier = config.Bind("Impostor Roles", "UnderdogCooldownMultiplier", 0.5f, "Kill-cooldown multiplier for the Underdog while outnumbered (0.5 = half).");
            UndertakerDragCooldown = BindSeconds(config, "Impostor Roles", "UndertakerDragCooldown", 25f, "Seconds between Undertaker drags. Town-Of-Us default: 25.");
            FootprintInterval = BindSeconds(config, "Crewmate Roles", "FootprintInterval", 1f, "Seconds between Investigator footprint drops. Town-Of-Us default: 1.");
            FootprintDuration = BindSeconds(config, "Crewmate Roles", "FootprintDuration", 10f, "How long Investigator footprints stay visible. Town-Of-Us default: 10.");

            FootprintAnonymous = config.Bind("Crewmate Roles", "FootprintAnonymous", false, "Investigator footprints are grey instead of player-colored. Town-Of-Us AnonymousFootPrint default: false.");
            RewindCooldown = BindSeconds(config, "Crewmate Roles", "RewindCooldown", 25f, "Seconds between Time Lord rewinds. Town-Of-Us default: 25.");
            RewindSeconds = BindSeconds(config, "Crewmate Roles", "RewindSeconds", 3f, "How far back a Time Lord rewind goes. Town-Of-Us 'Rewind Duration' default: 3.");
            RewindRevive = config.Bind("Crewmate Roles", "RewindRevive", true, "Players killed inside the rewind window are revived by the rewind (Town-Of-Us behavior).");
            ShiftCooldown = BindSeconds(config, "Neutral Roles", "ShiftCooldown", 30f, "Seconds between Shifter shifts.");
            GlitchMimicCooldown = BindSeconds(config, "Neutral Roles", "GlitchMimicCooldown", 30f, "Seconds between Glitch mimics.");
            GlitchMimicDuration = BindSeconds(config, "Neutral Roles", "GlitchMimicDuration", 10f, "How long a Glitch mimic lasts.");
            GlitchHackCooldown = BindSeconds(config, "Neutral Roles", "GlitchHackCooldown", 30f, "Seconds between Glitch hacks.");
            GlitchHackDuration = BindSeconds(config, "Neutral Roles", "GlitchHackDuration", 10f, "How long a Glitch hack lasts.");
            GlitchKillCooldown = BindSeconds(config, "Neutral Roles", "GlitchKillCooldown", 30f, "Seconds between Glitch kills.");
            MineCooldown = BindSeconds(config, "Impostor Roles", "MineCooldown", 25f, "Seconds between Miner mines. Town-Of-Us default: 25.");
            PresentationEnabled = config.Bind("Presentation", "Enabled", true, "Show custom role names under visible player names during play and meetings.");
            DeadSeeRoles = config.Bind("Presentation", "DeadSeeRoles", true, "Allow dead players to see custom role names in-world and during meetings.");
            ImpostorSeeRoles = config.Bind("Presentation", "ImpostorSeeRoles", false, "Allow Impostors to see other players' custom role names.");

            ModifierTorch = BindRoleToggle(config, "Modifiers", "Torch", "Torch modifier: vision unaffected by lights sabotage (Crewmate).");
            ModifierTorchProbability = BindChance(config, "Modifiers", "TorchProbability", 0f, "Chance each eligible player gets the Torch modifier.");
            ModifierDiseased = BindRoleToggle(config, "Modifiers", "Diseased", "Diseased modifier: killing them triples the killer's kill cooldown (Crewmate).");
            ModifierDiseasedProbability = BindChance(config, "Modifiers", "DiseasedProbability", 0f, "Chance each eligible player gets the Diseased modifier.");
            ModifierFlash = BindRoleToggle(config, "Modifiers", "Flash", "Flash modifier: moves at 2x speed.");
            ModifierFlashProbability = BindChance(config, "Modifiers", "FlashProbability", 0f, "Chance each player gets the Flash modifier.");
            ModifierTiebreaker = BindRoleToggle(config, "Modifiers", "Tiebreaker", "Tiebreaker modifier: their vote decides tied meetings.");
            ModifierTiebreakerProbability = BindChance(config, "Modifiers", "TiebreakerProbability", 0f, "Chance each player gets the Tiebreaker modifier.");
            ModifierDrunk = BindRoleToggle(config, "Modifiers", "Drunk", "Drunk modifier: movement controls are inverted.");
            ModifierDrunkProbability = BindChance(config, "Modifiers", "DrunkProbability", 0f, "Chance each player gets the Drunk modifier.");
            ModifierGiant = BindRoleToggle(config, "Modifiers", "Giant", "Giant modifier: bigger body, slower walk.");
            ModifierGiantProbability = BindChance(config, "Modifiers", "GiantProbability", 0f, "Chance each player gets the Giant modifier.");
            ModifierButtonBarry = BindRoleToggle(config, "Modifiers", "ButtonBarry", "Button Barry modifier: can call an emergency meeting from anywhere (/meeting).");
            ModifierButtonBarryProbability = BindChance(config, "Modifiers", "ButtonBarryProbability", 0f, "Chance each player gets the Button Barry modifier.");

            MaxImpostorRoles = BindCount(config, "Role Limits", "MaxImpostorRoles", 1,
                "How many players may hold a custom Impostor role in one game. Town-Of-Us 'Max Impostor Roles' default: 1. Set 0 for no cap (this mod's old behaviour, and not what Town Of Us does).");
            MaxNeutralRoles = BindCount(config, "Role Limits", "MaxNeutralRoles", 1,
                "How many players may hold a custom Neutral role in one game. Town-Of-Us 'Max Neutral Roles' default: 1. Without this, every enabled neutral role rolls on its own and a lobby can field several at once. Set 0 for no cap.");

            GameplayHooks = config.Bind(
                "Gameplay", "EnableGameplayHooks", false,
                "Enable optional Sheriff kill/report hooks. Engineer Fix Sab hooks are independent.");
            CustomAbilityButtons = config.Bind(
                "Gameplay", "EnableCustomAbilityButtons", true,
                "Show dedicated ability buttons for Sheriff/Vigilante/Engineer/Medic/Seer. These buttons use the game's PassiveButton click path (UiRuntime), not managed Unity delegates, so they are safe on the current build.");
            GameConfigOverlay = config.Bind(
                "Menu", "GameConfigOverlay", false,
                "Show the mod's own hand-built role-settings overlay (Crewmate/Impostor/Neutral tabs) on top of the game-config menu. Off by default: the native rows below are the supported surface, and enabling both shows two role menus at once.");
            NativeMenuRows = config.Bind(
                "Menu", "NativeMenuRows", true,
                "Append the role list below the game's own options in its config window: one Count row per enabled role, plus that role's own settings (cooldowns, uses) while its count is 1 or more. Rows are built from the game's own row prefabs and clicked through UiRuntime, and the window's scroll range grows to reach them. This is the supported role-settings UI.");
            ModsMenu = config.Bind(
                "Menu", "ModsMenu", true,
                "Show the 'Mods' button in the bottom-right of the HUD, above the game's own button cluster. It opens the game's options menu on the Mods tab, which lists every loaded mod with an On/Off toggle. The button is built and placed by MarshAPI (UiHudButton), so it shares the HUD slot system with the ability buttons rather than owning a slot of its own.");
            BootScreenPreload = config.Bind(
                "Client", "BootScreenPreload", true,
                "Do the mod's asset and code warm-up on ClassicUs' 'Check for updates' splash screen - the first managed callback in the MainMenu scene - so nothing is decoded or built mid-round. Off only makes that work lazy; it does not turn off the boot-screen badge (see BootScreenBadge).");
            BootScreenBadge = config.Bind(
                "Client", "BootScreenBadge", true,
                "Draw the mod's version and enabled-role count on the boot screen (under the splash's loading text) and in its release popup. This is the only switch that touches what the boot screen looks like; the warm-up in BootScreenPreload runs either way.");
            MigrateLegacyEntries(config, fileValues);
        }

        public static int Count(ConfigEntry<int> entry, int fallback = 1) =>
            entry == null ? fallback : Clamp(entry.Value, 0, 15);

        public static float Chance(ConfigEntry<float> entry, float fallback = 100f) =>
            entry == null ? fallback : Clamp(entry.Value, 0f, 100f);

        public static float Seconds(ConfigEntry<float> entry, float fallback = 0f) =>
            entry == null ? fallback : Clamp(entry.Value, 0f, 600f);

        public static bool RevealRole =>
            SeerRevealMode != null && string.Equals(SeerRevealMode.Value?.Trim(), "Role", StringComparison.OrdinalIgnoreCase);

        public static int RoleCap(RoleTeamTypes team) =>
            team == RoleTeamTypes.Impostor ? Count(MaxImpostorRoles, 1)
            : team == RoleTeamTypes.Neutral ? Count(MaxNeutralRoles, 1)
            : 0;

        private static ConfigEntry<int> BindCount(ConfigFile config, string section, string key, int value, string description) =>
            config.Bind(section, key, value, description);

        private static ConfigEntry<float> BindChance(ConfigFile config, string section, string key, float value, string description) =>
            config.Bind(section, key, value, description);

        private static ConfigEntry<bool> BindRoleToggle(ConfigFile config, string section, string key, string description) =>
            config.Bind(section, key, true, description);

        private static ConfigEntry<float> BindSeconds(ConfigFile config, string section, string key, float value, string description) =>
            config.Bind(section, key, value, description);

        private static void MigrateLegacyEntries(ConfigFile config, Dictionary<ConfigDefinition, string> fileValues)
        {
            Migrate(config, fileValues, "Crewmate Roles", "Sheriff", "Roles", "Sheriff");
            Migrate(config, fileValues, "Crewmate Roles", "Engineer", "Roles", "Engineer");
            Migrate(config, fileValues, "Crewmate Roles", "Medic", "Roles", "Medic");
            Migrate(config, fileValues, "Crewmate Roles", "Seer", "Roles", "Seer");
            Migrate(config, fileValues, "Crewmate Roles", "Vigilante", "Roles", "Vigilante");
            Migrate(config, fileValues, "Impostor Roles", "Assassin", "Roles", "Assassin");
            Migrate(config, fileValues, "Neutral Roles", "Jester", "Roles", "Jester");

            Migrate(config, fileValues, "Crewmate Roles", "SheriffCount", "Role Pool", "SheriffCount");
            Migrate(config, fileValues, "Crewmate Roles", "SheriffChance", "Role Pool", "SheriffChance");
            Migrate(config, fileValues, "Crewmate Roles", "EngineerCount", "Role Pool", "EngineerCount");
            Migrate(config, fileValues, "Crewmate Roles", "EngineerChance", "Role Pool", "EngineerChance");
            Migrate(config, fileValues, "Crewmate Roles", "MedicCount", "Role Pool", "MedicCount");
            Migrate(config, fileValues, "Crewmate Roles", "MedicChance", "Role Pool", "MedicChance");
            Migrate(config, fileValues, "Crewmate Roles", "SeerCount", "Role Pool", "SeerCount");
            Migrate(config, fileValues, "Crewmate Roles", "SeerChance", "Role Pool", "SeerChance");
            Migrate(config, fileValues, "Crewmate Roles", "VigilanteCount", "Role Pool", "VigilanteCount");
            Migrate(config, fileValues, "Crewmate Roles", "VigilanteChance", "Role Pool", "VigilanteChance");
            Migrate(config, fileValues, "Impostor Roles", "AssassinCount", "Role Pool", "AssassinCount");
            Migrate(config, fileValues, "Impostor Roles", "AssassinChance", "Role Pool", "AssassinChance");
            Migrate(config, fileValues, "Neutral Roles", "JesterCount", "Role Pool", "JesterCount");
            Migrate(config, fileValues, "Neutral Roles", "JesterChance", "Role Pool", "JesterChance");

            Migrate(config, fileValues, "Crewmate Roles", "SheriffKillCooldown", "Gameplay", "SheriffKillCooldown");
            Migrate(config, fileValues, "Crewmate Roles", "EngineerFixCooldown", "Gameplay", "EngineerFixCooldown");
            Migrate(config, fileValues, "Crewmate Roles", "MedicUses", "Gameplay", "MedicUses");
            Migrate(config, fileValues, "Crewmate Roles", "MedicCooldown", "Gameplay", "MedicCooldown");
            Migrate(config, fileValues, "Crewmate Roles", "MedicShieldBreaksOnKill", "Medic", "ShieldBreaksOnKill");
            Migrate(config, fileValues, "Crewmate Roles", "SeerUses", "Gameplay", "SeerUses");
            Migrate(config, fileValues, "Crewmate Roles", "SeerCooldown", "Gameplay", "SeerCooldown");
            Migrate(config, fileValues, "Crewmate Roles", "SeerRevealMode", "Seer", "RevealMode");
            Migrate(config, fileValues, "Crewmate Roles", "VigilanteShots", "Gameplay", "VigilanteShots");
            Migrate(config, fileValues, "Crewmate Roles", "VigilanteCooldown", "Gameplay", "VigilanteCooldown");
            Migrate(config, fileValues, "Impostor Roles", "AssassinMultiKill", "Assassin", "MultiKill");
            Migrate(config, fileValues, "Impostor Roles", "AssassinMeetingButtons", "Assassin", "MeetingButtons");
            Migrate(config, fileValues, "Crewmate Roles", "SheriffKillOther", "Sheriff", "KillOther");
            Migrate(config, fileValues, "Crewmate Roles", "SheriffBodyReport", "Sheriff", "BodyReport");

            Migrate(config, fileValues, "Diagnostics", "EnableGameplayHooks", "Gameplay", "EnableGameplayHooks");
            Migrate(config, fileValues, "Diagnostics", "EnableCustomAbilityButtons", "Gameplay", "EnableCustomAbilityButtons");
            Migrate(config, fileValues, "Diagnostics", "BootScreenPreload", "Client", "BootScreenPreload");
            Migrate(config, fileValues, "Diagnostics", "BootScreenBadge", "Client", "BootScreenBadge");
            Migrate(config, fileValues, "Debug", "HorseMode", "Client", "HorseMode");

            config.Save();
        }

        private static void Migrate(
            ConfigFile config,
            Dictionary<ConfigDefinition, string> fileValues,
            string newSection,
            string newKey,
            string oldSection,
            string oldKey)
        {
            var newDefinition = new ConfigDefinition(newSection, newKey);
            var oldDefinition = new ConfigDefinition(oldSection, oldKey);
            if (fileValues.ContainsKey(newDefinition)) return;
            if (!fileValues.TryGetValue(oldDefinition, out var oldValue)) return;
            if (!TryGetEntry(config, newDefinition, out var newEntry)) return;

            try
            {
                object converted = newEntry.SettingType == typeof(string)
                    ? oldValue
                    : Convert.ChangeType(oldValue, newEntry.SettingType, CultureInfo.InvariantCulture);
                newEntry.BoxedValue = converted;
            }
            catch
            {
            }
        }

        private static Dictionary<ConfigDefinition, string> ReadConfigFile(ConfigFile config)
        {
            var values = new Dictionary<ConfigDefinition, string>();
            try
            {
                var path = config.GetType()
                    .GetProperty("ConfigFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(config) as string;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return values;

                var section = string.Empty;
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    var separator = line.IndexOf('=');
                    if (separator <= 0 || section.Length == 0) continue;
                    var key = line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();
                    if (key.Length > 0) values[new ConfigDefinition(section, key)] = value;
                }
            }
            catch
            {
            }
            return values;
        }

        private static bool TryGetEntry(ConfigFile config, ConfigDefinition definition, out ConfigEntryBase entry)
        {
            entry = null;
            var entries = GetEntries(config);
            if (entries == null) return false;

            try
            {
                var indexer = entries.GetType().GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                entry = indexer?.GetValue(entries, new object[] { definition }) as ConfigEntryBase;
                return entry != null;
            }
            catch
            {
                return false;
            }
        }

        private static object GetEntries(ConfigFile config)
        {
            try
            {
                return config.GetType()
                    .GetProperty("Entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(config);
            }
            catch
            {
                return null;
            }
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        private static Dictionary<string, ConfigEntry<bool>> _roleToggles;

        public static bool IsEnabled(string roleKey)
        {
            if (string.IsNullOrEmpty(roleKey)) return false;

            if (SessionDisable.IsOff) return false;

            if (_roleToggles == null || _roleToggles.Count == 0) _roleToggles = BuildRoleToggleLookup();
            return _roleToggles.TryGetValue(roleKey, out var entry) && entry != null && entry.Value;
        }

        public static int EnabledRoleCount()
        {
            int count = 0;
            try
            {
                foreach (var def in RoleCatalog.All)
                    if (IsEnabled(RoleCatalog.KeyOf(def))) count++;
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles")
                    .LogError("RoleConfig.EnabledRoleCount: " + e.Message);
            }
            return count;
        }

        private static Dictionary<string, ConfigEntry<bool>> BuildRoleToggleLookup()
        {
            var map = new Dictionary<string, ConfigEntry<bool>>(StringComparer.Ordinal);
            try
            {
                foreach (var property in typeof(RoleConfig).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (property.PropertyType != typeof(ConfigEntry<bool>)) continue;
                    if (property.GetValue(null) is ConfigEntry<bool> entry && !string.IsNullOrEmpty(property.Name))
                        map[property.Name] = entry;
                }
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles")
                    .LogError("RoleConfig: role toggle lookup failed: " + e.Message);
            }
            return map;
        }
    }
}
