using System.Linq;

namespace TownOfRoles.Core
{
    // Adapters for game members whose shape changed in the 2026.8.9 interop.
    internal static class GameReflection
    {
        // MeetingHud.playerStates (public Il2CppReferenceArray).
        public static PlayerVoteArea[] GetPlayerStates(MeetingHud meeting)
        {
            if (meeting == null) return null;
            try
            {
                var states = meeting.playerStates;
                return states == null ? null : states.ToArray();
            }
            catch
            {
                return null;
            }
        }

        // MeetingHud.state (public MeetingHud.VoteStates).
        public static MeetingHud.VoteStates GetMeetingState(MeetingHud meeting)
        {
            if (meeting == null) return MeetingHud.VoteStates.NotVoted;
            try
            {
                return meeting.state;
            }
            catch
            {
                return MeetingHud.VoteStates.NotVoted;
            }
        }

        // ShipStatus.AllVents (public setter).
        public static void SetAllVents(ShipStatus ship, Vent[] vents)
        {
            if (ship == null || vents == null) return;
            try
            {
                ship.AllVents = vents;
            }
            catch
            {
            }
        }

        // ExileController.completeString (public string).
        public static void SetCompleteString(ExileController controller, string text)
        {
            if (controller == null || text == null) return;
            try
            {
                controller.completeString = text;
            }
            catch
            {
            }
        }

        // ExileController.exiled (public GameData.PlayerInfo).
        public static GameData.PlayerInfo GetExileExiled(ExileController controller)
        {
            if (controller == null) return null;
            try
            {
                return controller.exiled;
            }
            catch
            {
                return null;
            }
        }
    }
}
