using BepInEx.Configuration;

namespace TownOfRoles.Core
{
    // Self-update settings. The mod checks a hosted latest.json manifest on launch, compares
    // versions, and shows an in-game modal when a newer Town Of Roles build is available.
    internal static class UpdateConfig
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> ManifestUrl { get; private set; }
        public static ConfigEntry<bool> AllowDownload { get; private set; }

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                "Updates", "Enabled", true,
                "Check for Town Of Roles updates on launch and show the in-game update prompt when a newer version is available.");

            ManifestUrl = config.Bind(
                "Updates", "ManifestUrl",
                "https://github.com/hallowmarshmallow/townofroles/releases/latest/download/latest.json",
                "URL of the latest.json update manifest. With GitHub Releases, use the stable 'releases/latest/download/latest.json' URL so it always points at the newest release.");

            AllowDownload = config.Bind(
                "Updates", "AllowDownload", true,
                "Allow the mod to download and stage the updated DLL when you press Update in the prompt. Disable to only show a notification.");
        }
    }
}
