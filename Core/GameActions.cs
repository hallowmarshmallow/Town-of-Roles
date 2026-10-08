using System;
using MarshAPI;

namespace TownOfRoles.Core
{
    internal static class GameActions
    {
        public static string TryCallMeeting(PlayerControl sender)
        {
            if (sender == null) return "There is no local player to call a meeting as.";

            try
            {
                if (AmongUsClient.Instance == null) return "Not connected to a game.";
                if (MeetingHud.Instance != null) return "A meeting is already in progress.";

                sender.CmdReportDeadBody(null);
                return null;
            }
            catch (Exception e)
            {
                return "Could not call a meeting: " + e.Message;
            }
        }

        public static string TryForceStart()
        {
            try
            {
                var client = AmongUsClient.Instance;
                if (client == null) return "Force start is unavailable right now.";
                if (PlayerControl.AllPlayerControls.Count == 0)
                    return "Force start is unavailable before the lobby is ready.";

                client.StartGame();
                return null;
            }
            catch (Exception e)
            {
                return "Could not force start: " + e.Message;
            }
        }

        public static string TryRevive(PlayerControl target)
        {
            if (target == null || target.Data == null) return "That player was not found.";
            if (target.Data.Disconnected) return "That player is disconnected.";
            if (!target.Data.IsDead) return ChatCommands.DisplayName(target) + " is already alive.";

            try
            {
                target.Revive();
                return null;
            }
            catch (Exception e)
            {
                return "Could not revive: " + e.Message;
            }
        }
    }
}
