using MarshAPI;

namespace TownOfRoles.Core
{
    // "Switch the mod off" for the rest of this session, the TOR tab's own switch.
    internal static class SessionDisable
    {
        // True while the mod is switched off for this session.
        public static bool IsOff { get; private set; }

        public static void Set(bool off)
        {
            IsOff = off;
            Apply();
        }

        // Pushes the flag into the two places that decide what happens in a round.
        public static void Apply()
        {
            RoleRegistry.Suspended = IsOff;
            UiAbilityButtons.Enabled = !IsOff && RoleConfig.CustomAbilityButtons?.Value != false;
        }
    }
}
