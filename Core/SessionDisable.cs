using MarshAPI;

namespace TownOfRoles.Core
{
    internal static class SessionDisable
    {
        public static bool IsOff { get; private set; }

        public static void Set(bool off)
        {
            IsOff = off;
            Apply();
        }

        public static void Apply()
        {
            RoleRegistry.Suspended = IsOff;
            UiAbilityButtons.Enabled = !IsOff && RoleConfig.CustomAbilityButtons?.Value != false;
        }
    }
}
