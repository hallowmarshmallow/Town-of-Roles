using BepInEx.Configuration;

namespace TownOfRoles.Core
{
    internal static class CommandConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> AlwaysCommandChat { get; private set; }
        public static ConfigEntry<bool> AllowSetRole { get; private set; }
        public static ConfigEntry<string> CustomCommands { get; private set; }
        public static ConfigEntry<bool> CustomCommandHostOnly { get; private set; }

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                "Commands", "Enabled", true,
                "Enable slash commands.");

            AlwaysCommandChat = config.Bind(
                "Commands", "AlwaysCommandChat", true,
                "Always-ON chat button.");

            AllowSetRole = config.Bind(
                "Commands", "AllowSetRole", true,
                "Allow the host to use /setrole and the Freeplay role selector for enabled custom roles.");
                
            CustomCommandHostOnly = config.Bind(
                "Commands", "CustomCommandHostOnly", false,
                "Restrict slash commands to host only.");
        }
    }
}
