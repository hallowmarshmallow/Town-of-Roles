using System;
using MarshAPI;
using TMPro;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class UpdateModal
    {
        private const string WindowName = "UpdateModal";

        private static UiWindow _window;
        private static TextMeshPro _message;
        private static UiWindowButton _updateButton;
        private static UiWindowButton _laterButton;
        private static System.Threading.Tasks.Task<string> _downloadTask;

        public static bool IsVisible => _window != null && _window.IsVisible;

        public static void Poll()
        {
            try
            {
                if (!IsVisible && UpdateSystem.ShouldPromptNow())
                {
                    Show(UpdateSystem.Latest);
                    return;
                }

                if (_downloadTask == null || !IsVisible) return;
                if (!_downloadTask.IsCompleted) return;

                string status;
                if (_downloadTask.IsFaulted || _downloadTask.IsCanceled)
                    status = "Update failed: " +
                             (_downloadTask.Exception?.GetBaseException()?.Message ?? "unknown error");
                else
                    status = _downloadTask.Result;

                _downloadTask = null;
                SetMessage(status);

                _laterButton?.SetVisible(true);
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Update modal poll: " + e.Message);
            }
        }

        public static void Show(UpdateSystem.UpdateInfo info)
        {
            if (info == null) return;

            try
            {
                if (_window == null || !_window.IsAlive) Build();
                if (_window == null) return;

                SetMessage(BuildMessage(info));
                _updateButton?.SetVisible(UpdateConfig.AllowDownload?.Value != false);
                _laterButton?.SetVisible(true);
                _window.Show();
                UpdateSystem.MarkPromptShown();
            }
            catch (Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogError("Update modal: " + e.Message);
                Hide();
            }
        }

        public static void Hide() => _window?.Hide();

        private static void Build()
        {
            _window = UiWindow.Create(WindowName, new UiWindowOptions
            {
                Title = "Update Available",
                Position = new Vector3(0f, 0.1f, 0f),
                Size = new Vector2(6.6f, 4.6f),
            });

            if (_window == null) return;

            _message = _window.AddText(string.Empty, 2.0f);
            if (_message != null) _message.alignment = TextAlignmentOptions.Center;

            _updateButton = _window.AddButton("update", "Update", OnUpdateClicked, UiKit.GoodGreen);
            _laterButton = _window.AddButton("later", "Later", OnLaterClicked);
        }

        private static void SetMessage(string text)
        {
            if (_message == null) return;
            try { _message.text = text ?? string.Empty; } catch { }
        }

        private static string BuildMessage(UpdateSystem.UpdateInfo info)
        {
            string msg = "Town Of Roles v" + info.Version + " is available (you have v" +
                         UpdateSystem.CurrentVersion + ").\n\n";
            if (!string.IsNullOrEmpty(info.Notes))
                msg += info.Notes + "\n\n";
            msg += UpdateConfig.AllowDownload?.Value == false
                ? "Downloads are disabled in the config — download the new build manually."
                : "Download and install it now?";
            return msg;
        }

        private static void OnUpdateClicked()
        {
            if (_downloadTask != null) return;
            if (UpdateSystem.Latest == null) return;
            if (UpdateConfig.AllowDownload?.Value == false) return;

            try
            {
                _updateButton?.SetVisible(false);
                _laterButton?.SetVisible(false);
                SetMessage("Downloading update…");

                _downloadTask = System.Threading.Tasks.Task.Run(() => UpdateSystem.DownloadAndStageAsync());
            }
            catch (Exception e)
            {
                SetMessage("Update failed: " + e.Message);
                _laterButton?.SetVisible(true);
            }
        }

        private static void OnLaterClicked() => Hide();
    }

    [HarmonyLib.HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    internal static class HudManager_Update_UpdateModalPatch
    {
        private static void Postfix() => UpdateModal.Poll();
    }
}
