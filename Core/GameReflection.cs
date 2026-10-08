using System.Linq;

namespace TownOfRoles.Core
{
    internal static class GameReflection
    {
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
