using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomic;
using MarshAPI;
using TownOfRoles.Core;
using TownOfRoles.Roles.Assassin;
using TownOfRoles.Roles.Arsonist;
using TownOfRoles.Roles.Modifiers;

namespace TownOfRoles.Commands
{
    internal static class CommandSystem
    {
        private const string ReviveRpc = "townofroles.Revive";
        private const string RequestCustomCommandRpc = "townofroles.RequestCustomCommand";
        private const string CustomCommandRpc = "townofroles.CustomCommand";
        private const string SystemMessageRpc = "townofroles.SystemMessage";

        public static void Initialize()
        {
            ChatCommands.Reply = SystemChat.Show;
            ChatCommands.Enabled = () => CommandConfig.Enabled?.Value == true;
            ChatCommands.Fallback = HandleCustomCommand;
            ChatCommands.ExtraHelp = CustomHelpLines;

            ChatCommands.HelpHeader = "Town of Roles commands:";
            ChatCommands.HelpSelfLine = "/help — this list";

            ChatCommands.Register(new ChatCommand("forcestart", "/forcestart — start the game now (host)", ForceStart)
            {
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("nickname", "/nickname <name> — change your name", Nickname)
            {
                Aliases = new[] { "nick" },
            });

            ChatCommands.Register(new ChatCommand("gradient", "/gradient [on|off] — hallowmarsh gradient", Gradient));
            ChatCommands.Register(new ChatCommand("rainbow", "/rainbow [on|off] — rainbow skin", Rainbow));
            ChatCommands.Register(new ChatCommand("color", "/color gradient|rainbow [on|off]", Color));

            ChatCommands.Register(new ChatCommand("system", "/system <msg> — host system broadcast", SystemMessage)
            {
                Aliases = new[] { "systemmessage" },
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("tpin", "/tpin|/tpout [player] — dropship teleport (host)",
                ctx => Teleport(ctx, true))
            {
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("tpout", "/tpin|/tpout [player] — dropship teleport (host)",
                ctx => Teleport(ctx, false))
            {
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("nogameend", "/nogameend [on|off] — disable game end (host)", NoGameEnd)
            {
                Aliases = new[] { "noend" },
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("setrole", "/setrole [player] <role> — assign a custom role (host)", SetRole)
            {
                Aliases = new[] { "role" },
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("revive", "/revive [player] — revive a dead player (host)", Revive)
            {
                Aliases = new[] { "resurrect" },
                HostOnly = true,
            });

            ChatCommands.Register(new ChatCommand("meeting", "/meeting — call an emergency meeting (Button Barry)", ButtonBarryMeeting)
            {
                Visible = ctx => RoleConfig.ModifierButtonBarry?.Value == true
                                 && ModifierSystem.Has(ctx.Sender.PlayerId, ModifierSystem.ButtonBarry),
            });

            ChatCommands.Register(new ChatCommand("guess", "/guess <player> <role> — Assassin guess", AssassinGuess)
            {
                Aliases = new[] { "assassinate" },
                Visible = ctx => RoleConfig.Assassin?.Value == true && AssassinSystem.IsAssassin(ctx.Sender),
            });

            ChatCommands.Register(new ChatCommand("ignite", "/ignite — Arsonist ignite", ArsonistIgnite)
            {
                Visible = ctx => RoleConfig.Arsonist?.Value == true && ArsonistSystem.IsArsonist(ctx.Sender),
            });

        }

        private static bool HandleCustomCommand(ChatCommandContext ctx)
        {
            var table = GetCustomCommandTable();
            if (table == null || !table.TryGetValue(ctx.Name, out var template)) return false;

            if (CommandConfig.CustomCommandHostOnly?.Value == true && !ChatCommands.IsHost)
            {
                ctx.Reply("Only the host can use /" + ctx.Name + ".");
                return true;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastCustomCommandRequest).TotalMilliseconds < 400) return true;
            _lastCustomCommandRequest = now;

            try
            {
                if (ChatCommands.IsHost)
                {
                    var message = FormatCustomCommand(template, ctx.Sender, ctx.Args);
                    TownOfRolesRpcMux.Send(CustomCommandRpc, message);
                    SystemChat.Show(message);
                }
                else
                {
                    TownOfRolesRpcMux.Send(RequestCustomCommandRpc, ctx.Name, string.Join(" ", ctx.Args));
                }
            }
            catch (Exception e)
            {
                ctx.Reply("Custom command failed: " + e.Message);
            }

            return true;
        }

        private static IEnumerable<string> CustomHelpLines()
        {
            var table = GetCustomCommandTable();
            if (table == null) return null;

            var lines = new List<string>();
            foreach (var pair in table)
                lines.Add("/" + pair.Key + " — " + pair.Value.Replace("{player}", "you").Replace("{args}", "…"));

            return lines;
        }

        [AtomicRpc(RequestCustomCommandRpc)]
        private static void OnRequestCustomCommandRpc(byte senderId, string command, string args)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            var table = GetCustomCommandTable();
            if (table == null || !table.TryGetValue(command ?? string.Empty, out var template)) return;

            var sender = FindByClientId(senderId);
            if (sender == null || sender.Data == null || sender.Data.Disconnected) return;

            var split = string.IsNullOrEmpty(args)
                ? new string[0]
                : args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var message = FormatCustomCommand(template, sender, split);
            TownOfRolesRpcMux.Send(CustomCommandRpc, message);
        }

        // A message's sender arrives as a client id, and ChatCommands.FindPlayer reads a
        // numeric string as a player id, so the two only line up while the id spaces happen to
        // agree. The welcome line was named after the wrong player, or after nobody.
        private static PlayerControl FindByClientId(byte clientId)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null) continue;
                var client = player.GetClient();
                if (client != null && client.Id == clientId) return player;
            }

            return null;
        }

        [AtomicRpc(CustomCommandRpc)]
        private static void OnCustomCommandRpc(byte senderId, string message)
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost) return;
            if (string.IsNullOrWhiteSpace(message)) return;
            SystemChat.Show(message);
        }

        private static Dictionary<string, string> GetCustomCommandTable()
        {
            var raw = CommandConfig.CustomCommands?.Value;
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entries = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var separator = entry.IndexOf("=>", StringComparison.Ordinal);
                if (separator <= 0) continue;
                var name = entry.Substring(0, separator).Trim().ToLowerInvariant();
                if (name.Length == 0) continue;
                var text = entry.Substring(separator + 2).Trim();
                table[name] = text;
            }
            return table.Count == 0 ? null : table;
        }

        private static string FormatCustomCommand(string template, PlayerControl sender, string[] args)
        {
            var text = template ?? string.Empty;
            text = text.Replace("{player}", ChatCommands.DisplayName(sender));
            text = text.Replace("{args}", string.Join(" ", args ?? new string[0]));
            if (text.Length > 200) text = text.Substring(0, 200);
            return text;
        }

        private static DateTime _lastCustomCommandRequest = DateTime.MinValue;

        private static void ForceStart(ChatCommandContext ctx)
        {
            var error = GameActions.TryForceStart();
            if (error != null)
            {
                ctx.Warn(error);
                return;
            }

            ctx.Reply("Force-start requested.");
        }

        private static void Reply(ChatCommandContext ctx, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                if (line.Length > 0) ctx.Reply(line);
            }
        }

        private static void Nickname(ChatCommandContext ctx)
        {
            if (ctx.Args.Length == 0)
            {
                ctx.ShowUsage();
                return;
            }

            var name = string.Join(" ", ctx.Args).Trim();
            if (name.Length == 0 || name.Length > 24)
            {
                ctx.Reply("Nickname must be between 1 and 24 characters.");
                return;
            }

            ctx.Sender.RpcSetName(name);
            ctx.Reply($"Nickname changed to {name}.");
        }

        private static void Gradient(ChatCommandContext ctx) => ApplyGradient(ctx, ctx.Args);

        private static void Rainbow(ChatCommandContext ctx) => ApplyRainbow(ctx, ctx.Args);

        private static void ApplyGradient(ChatCommandContext ctx, string[] args)
        {
            var enabled = ParseToggle(args, true);
            if (enabled == null)
            {
                ctx.ShowUsage();
                return;
            }

            VisualEffects.SetGradient(enabled.Value);
            ctx.Reply($"hallowmarsh gradient {(enabled.Value ? "enabled" : "disabled")}.");
        }

        private static void ApplyRainbow(ChatCommandContext ctx, string[] args)
        {
            var enabled = ParseToggle(args, true);
            if (enabled == null)
            {
                ctx.ShowUsage();
                return;
            }

            VisualEffects.SetRainbow(enabled.Value);
            ctx.Reply($"Native rainbow color skin {(enabled.Value ? "enabled" : "disabled")}.");
        }

        private static void Color(ChatCommandContext ctx)
        {
            if (ctx.Args.Length == 0)
            {
                ctx.Warn("Usage: /color gradient [on|off] or /color rainbow [on|off]");
                return;
            }

            var mode = ctx.Args[0].ToLowerInvariant();
            var rest = ctx.Args.Skip(1).ToArray();
            if (mode == "gradient")
            {
                ApplyGradient(ctx, rest);
                return;
            }
            if (mode == "rainbow")
            {
                ApplyRainbow(ctx, rest);
                return;
            }

            ctx.Reply("Use /color gradient or /color rainbow.");
        }

        private static void SystemMessage(ChatCommandContext ctx)
        {
            if (ctx.Args.Length == 0)
            {
                ctx.ShowUsage();
                return;
            }

            var message = string.Join(" ", ctx.Args).Trim();
            if (message.Length > 120)
                message = message.Substring(0, 120);

            SystemChat.Show(message);
            try { TownOfRolesRpcMux.Send(SystemMessageRpc, message); }
            catch (Exception e) { ctx.Warn("Lobby broadcast failed: " + e.Message); }
        }

        [AtomicRpc(SystemMessageRpc)]
        private static void OnSystemMessageRpc(byte senderId, string message)
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost) return;
            if (string.IsNullOrWhiteSpace(message)) return;
            SystemChat.Show(message);
        }

        private static void Teleport(ChatCommandContext ctx, bool intoDropship)
        {
            var target = ctx.Target;
            if (target == null)
            {
                ctx.Warn("Usage: /tpin [player] or /tpout [player]");
                return;
            }

            var ship = ShipStatus.Instance;
            if (ship == null || target.NetTransform == null || target.Data == null)
            {
                ctx.Warn("Teleport is unavailable on this screen/map.");
                return;
            }

            var count = Math.Max(1, PlayerControl.AllPlayerControls.Count);
            var position = ship.GetSpawnLocation(target.PlayerId, count, intoDropship);
            target.NetTransform.RpcSnapTo(position);
            ctx.Reply($"Teleported {ChatCommands.DisplayName(target)} {(intoDropship ? "into" : "out of")} the dropship.");
        }

        private static void NoGameEnd(ChatCommandContext ctx)
        {
            var enabled = ParseToggle(ctx.Args, true);
            if (enabled == null)
            {
                ctx.ShowUsage();
                return;
            }

            CommandState.SetNoGameEnd(enabled.Value);
            ctx.Reply($"No-game-end mode {(enabled.Value ? "enabled" : "disabled")}.");
        }

        private static void SetRole(ChatCommandContext ctx)
        {
            if (CommandConfig.AllowSetRole?.Value != true)
            {
                ctx.Reply("/setrole is disabled in the Town Of Roles config.");
                return;
            }
            if (ctx.Args.Length < 1)
            {
                ctx.ShowUsage();
                return;
            }

            PlayerControl target;
            string roleName;
            if (ctx.Args.Length == 1)
            {
                target = ctx.Sender;
                roleName = ctx.Args[0];
            }
            else
            {
                target = ctx.TargetOf(ctx.Args.Take(ctx.Args.Length - 1));
                roleName = ctx.Args[ctx.Args.Length - 1];
            }

            if (target == null || target.Data == null || target.Data.Disconnected || target.Data.IsDead)
            {
                ctx.Reply("Target player is unavailable or dead.");
                return;
            }

            var canonicalRole = ResolveRoleName(roleName);
            if (canonicalRole == null)
            {
                ctx.Reply("Unknown role. Available custom roles: Sheriff, Engineer, Jester, Medic, Seer, Vigilante, Assassin.");
                return;
            }

            var manager = RoleManager.Instance;
            if (manager == null)
            {
                ctx.Warn("Role manager is not ready.");
                return;
            }

            manager.AssignRole(target, canonicalRole);
            ctx.Reply($"Assigned {canonicalRole} to {ChatCommands.DisplayName(target)}.");
        }

        private static void Revive(ChatCommandContext ctx)
        {
            var target = ctx.Target;
            var error = GameActions.TryRevive(target);
            if (error != null)
            {
                ctx.Reply(error);
                return;
            }

            TownOfRolesRpcMux.Send(ReviveRpc, target.PlayerId);
            ctx.Reply($"Revived {ChatCommands.DisplayName(target)}.");
        }

        internal static void ReviveLocalPlayer()
        {
            var local = PlayerControl.LocalPlayer;
            var error = GameActions.TryRevive(local);
            if (error == null) TownOfRolesRpcMux.Send(ReviveRpc, local.PlayerId);
            ChatCommands.Write(error ?? "Revived you.");
        }

        [AtomicRpc(ReviveRpc)]
        private static void OnReviveRpc(byte senderId, byte playerId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || client.AmHost) return;
            if (senderId != client.HostId) return;

            var target = ChatCommands.FindPlayer(playerId.ToString(CultureInfo.InvariantCulture));
            if (target == null || target.Data == null)
            {
                CommandState.QueueRevive(playerId);
                return;
            }
            if (target.Data.IsDead) target.Revive();
        }

        private static void AssassinGuess(ChatCommandContext ctx)
        {
            ctx.Handled = AssassinSystem.TryHandleGuess(ctx.Sender, ctx.Args);
        }

        private static void ArsonistIgnite(ChatCommandContext ctx) => ArsonistSystem.TryIgnite(ctx.Sender);

        private static void ButtonBarryMeeting(ChatCommandContext ctx)
        {
            var error = GameActions.TryCallMeeting(ctx.Sender);
            if (error != null) ctx.Reply(error);
        }

        public static void TickLocalEffects()
        {
            VisualEffects.Tick();
            TryApplyPendingRevive();
        }

        private static void TryApplyPendingRevive()
        {
            var pending = CommandState.TakePendingRevive();
            if (!pending.HasValue) return;
            var target = ChatCommands.FindPlayer(pending.Value.ToString(CultureInfo.InvariantCulture));
            if (target == null || target.Data == null)
            {
                CommandState.QueueRevive(pending.Value);
                return;
            }
            if (target.Data.IsDead) target.Revive();
        }

        private static string ResolveRoleName(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "sheriff":
                case "townofroles.sheriff":
                    return RoleConfig.Sheriff?.Value == true ? "townofroles.Sheriff" : null;
                case "engineer":
                case "townofroles.engineer":
                    return RoleConfig.Engineer?.Value == true ? "townofroles.Engineer" : null;
                case "jester":
                case "townofroles.jester":
                    return RoleConfig.Jester?.Value == true ? "townofroles.Jester" : null;
                case "medic":
                case "townofroles.medic":
                    return RoleConfig.Medic?.Value == true ? "townofroles.Medic" : null;
                case "seer":
                case "townofroles.seer":
                    return RoleConfig.Seer?.Value == true ? "townofroles.Seer" : null;
                case "vigilante":
                case "townofroles.vigilante":
                    return RoleConfig.Vigilante?.Value == true ? "townofroles.Vigilante" : null;
                case "assassin":
                case "townofroles.assassin":
                    return RoleConfig.Assassin?.Value == true ? "townofroles.Assassin" : null;
                case "janitor":
                case "townofroles.janitor":
                    return RoleConfig.Janitor?.Value == true ? "townofroles.Janitor" : null;
                case "altruist":
                case "townofroles.altruist":
                    return RoleConfig.Altruist?.Value == true ? "townofroles.Altruist" : null;
                case "mayor":
                case "townofroles.mayor":
                    return RoleConfig.Mayor?.Value == true ? "townofroles.Mayor" : null;
                case "executioner":
                case "townofroles.executioner":
                    return RoleConfig.Executioner?.Value == true ? "townofroles.Executioner" : null;
                case "arsonist":
                case "townofroles.arsonist":
                    return RoleConfig.Arsonist?.Value == true ? "townofroles.Arsonist" : null;
                case "swapper":
                case "townofroles.swapper":
                    return RoleConfig.Swapper?.Value == true ? "townofroles.Swapper" : null;
                case "morphling":
                case "townofroles.morphling":
                    return RoleConfig.Morphling?.Value == true ? "townofroles.Morphling" : null;
                case "spy":
                case "townofroles.spy":
                    return RoleConfig.Spy?.Value == true ? "townofroles.Spy" : null;
                default:
                    return null;
            }
        }

        private static bool? ParseToggle(string[] args, bool defaultValue)
        {
            if (args.Length == 0) return defaultValue;
            if (args.Length != 1) return null;
            switch (args[0].ToLowerInvariant())
            {
                case "on":
                case "enable":
                case "enabled":
                    return true;
                case "off":
                case "disable":
                case "disabled":
                    return false;
                default:
                    return null;
            }
        }
    }
}
