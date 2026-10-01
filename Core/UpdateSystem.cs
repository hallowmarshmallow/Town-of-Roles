using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using TownOfRoles.Core;

namespace TownOfRoles.Core
{
    // Self-update pipeline (main mod side).
    internal static class UpdateSystem
    {
        public const string StagingDirName = ".townofroles-update";
        public const string PendingFileName = "pending.json";

        // The plugin file an update replaces. Named once: the download, the pending marker and
        // the applier script all have to agree on it.
        public const string StagedFileName = "TownOfRoles.dll";

        public const string CurrentVersion = TownOfRolesPlugin.Version;

        // The plugins folder, or the conventional path when BepInEx's own Paths have not
        // resolved yet. Shared with UpdateApplier so the staging directory is computed one way.
        public static string PluginDirectory()
        {
            try { return Paths.PluginPath; }
            catch { return Path.Combine(Paths.GameRootPath, "BepInEx", "plugins"); }
        }

        private static readonly HttpClient Http = CreateClient();
        private static UpdateInfo _latest;
        private static bool _promptShownThisSession;

        public static UpdateInfo Latest => _latest;

        public sealed class UpdateInfo
        {
            public string Version;
            public string Notes;
            public string Url;
            public string Sha256;
            public bool Valid;
        }

        public static void StartCheck()
        {
            if (UpdateConfig.Enabled?.Value != true) return;
            if (string.IsNullOrWhiteSpace(UpdateConfig.ManifestUrl?.Value)) return;
            if (UpdateConfig.ManifestUrl.Value.Contains("OWNER/REPO")) return; // placeholder

            _ = Task.Run(() => CheckAsync());
        }

        private static async Task CheckAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var json = await Http.GetStringAsync(UpdateConfig.ManifestUrl.Value, cts.Token).ConfigureAwait(false);
                var info = ParseManifest(json);
                if (info == null || !info.Valid) return;

                if (string.IsNullOrEmpty(info.Version)) return;
                if (!Version.TryParse(info.Version, out var manifestVersion)) return;
                if (!Version.TryParse(CurrentVersion, out var currentVersion)) return;
                if (manifestVersion <= currentVersion) return;

                _latest = info;
            }
            catch
            {
                // Update checks are best-effort and must never disturb the game.
            }
        }

        // Called once per frame while in a lobby; shows the prompt at the right moment.
        public static bool ShouldPromptNow()
        {
            if (_latest == null || _promptShownThisSession) return false;
            if (!HudManager.InstanceExists) return false;
            try
            {
                // Only in the lobby/main menu, never mid-round or in a meeting.
                if (ShipStatus.Instance != null) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void MarkPromptShown() => _promptShownThisSession = true;

        // Downloads the staged DLL, verifies SHA-256, and writes the pending marker. Returns a
        // human-readable status for the modal.
        public static async Task<string> DownloadAndStageAsync()
        {
            var info = _latest;
            if (info == null) return "No update data.";
            if (UpdateConfig.AllowDownload?.Value != true) return "Downloads are disabled in the config.";

            string pluginsDir = PluginDirectory();
            string stagingDir = Path.Combine(pluginsDir, StagingDirName);
            string stagedPath = Path.Combine(stagingDir, StagedFileName);
            string pendingPath = Path.Combine(stagingDir, PendingFileName);

            try
            {
                Directory.CreateDirectory(stagingDir);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                byte[] bytes;
                using (var response = await Http.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    bytes = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(info.Sha256))
                {
                    string actual = Sha256Hex(bytes);
                    if (!string.Equals(actual, info.Sha256, StringComparison.OrdinalIgnoreCase))
                        return "Download failed integrity check (SHA-256 mismatch).";
                }

                await File.WriteAllBytesAsync(stagedPath, bytes, cts.Token).ConfigureAwait(false);

                var pending = "{\"version\":\"" + JsonEscape(info.Version) + "\","
                            + "\"target\":\"" + StagedFileName + "\","
                            + "\"staged\":\"" + StagedFileName + "\","
                            + "\"sha256\":\"" + (info.Sha256 ?? Sha256Hex(bytes)) + "\"}";
                await File.WriteAllTextAsync(pendingPath, pending, Encoding.UTF8, cts.Token).ConfigureAwait(false);

                // Arm the post-exit applier now, while this process still owns the
                // session: it has to be running before the game closes so the swap can
                // happen the moment the DLL is released. Never a preloader-patched
                // dependency, see UpdateApplier.
                UpdateApplier.Arm();

                return "Update ready — restart the game to apply.";
            }
            catch (Exception e)
            {
                return "Update failed: " + e.Message;
            }
        }

        internal static UpdateInfo ParseManifest(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var info = new UpdateInfo
            {
                Version = GetJsonString(json, "version"),
                Notes = GetJsonString(json, "notes"),
                Url = GetJsonString(json, "url"),
                Sha256 = GetJsonString(json, "sha256"),
            };
            info.Valid = !string.IsNullOrEmpty(info.Version) && !string.IsNullOrEmpty(info.Url);
            return info;
        }

        internal static string GetJsonString(string json, string key)
        {
            string needle = "\"" + key + "\"";
            int start = json.IndexOf(needle, StringComparison.Ordinal);
            if (start < 0) return null;
            int colon = json.IndexOf(':', start + needle.Length);
            if (colon < 0) return null;
            int quote = json.IndexOf('"', colon);
            if (quote < 0) return null;
            int end = json.IndexOf('"', quote + 1);
            if (end < 0) return null;
            return json.Substring(quote + 1, end - quote - 1);
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TownOfRoles/" + CurrentVersion);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return client;
        }
    }
}
