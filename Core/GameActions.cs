using System;
using MarshAPI;

namespace TownOfRoles.Core
{
    // The one-shot lobby actions the mod offers from more than one place.
    internal static class GameActions
    {
        // Calls an emergency meeting from wherever the sender is, with no body and no modifier
        // required.
        // Returns: Null on success, or the reason it could not be done.
        public static string TryCallMeeting(PlayerControl sender)
        {
            if (sender == null) return "There is no local player to call a meeting as.";

            try
            {
                if (AmongUsClient.Instance == null) return "Not connected to a game.";
                if (MeetingHud.Instance != null) return "A meeting is already in progress.";

                sender.CmdReportDeadBody(null); // null body = emergency meeting, from anywhere
                return null;
            }
            catch (Exception e)
            {
                return "Could not call a meeting: " + e.Message;
            }
        }

        // Starts the game now. Host only in practice, the RPC is ignored for anyone else, so
        // this reports the refusal rather than sending one.
        // Returns: Null on success, or the reason it could not be done.
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

        // Revives a player locally only. That is a real limitation, not a detail:
        // PlayerControl.Revive is not an RPC, so every other client still sees the target dead
        // until someone tells them.
        // Returns: Null on success, or the reason it could not be done.
        public static string TryRevive(PlayerControl target)
        {
            if (target == null || target.Data == null) return "That player was not found.";
            if (target.Data.Disconnected) return "That player is disconnected.";
            if (!target.Data.IsDead) return ChatCommands.DisplayName(target) + " is already alive.";

            try
            {
                // Classic Us exposes the native zero-argument revive operation. Use it instead
                // of manually changing IsDead or destroying bodies.
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
