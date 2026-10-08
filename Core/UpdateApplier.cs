using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TownOfRoles.Core
{
    internal static class UpdateApplier
    {
        private const int MaxWaits = 1800;
        private const int WaitSeconds = 2;

        private static bool _launched;

        public static void Arm()
        {
            try
            {
                string plugins = UpdateSystem.PluginDirectory();
                string staging = Path.Combine(plugins, UpdateSystem.StagingDirName);
                string pending = Path.Combine(staging, UpdateSystem.PendingFileName);
                string staged = Path.Combine(staging, UpdateSystem.StagedFileName);
                string target = Path.Combine(plugins, UpdateSystem.StagedFileName);

                if (!File.Exists(pending) || !File.Exists(staged))
                {
                    Cleanup(staging);
                    return;
                }

                if (_launched) return;

                string script = Path.Combine(staging, ScriptName());
                File.WriteAllText(script, BuildScript(IsWindows, staged, target, pending), Encoding.ASCII);
                Launch(script, staging);
                _launched = true;
            }
            catch (Exception e)
            {
                Log("Self-update applier could not be armed: " + e.Message);
            }
        }

        internal static string ScriptName() => IsWindows ? "apply-update.cmd" : "apply-update.sh";

        internal static bool IsWindows => Path.DirectorySeparatorChar == '\\';

        internal static string BuildScript(bool windows, string staged, string target, string pending)
        {
            var script = new StringBuilder();

            if (windows)
            {
                const string nl = "\r\n";
                script.Append("@echo off").Append(nl);
                script.Append("rem Town Of Roles self-update applier. Written by the mod; not a hand-edited file.").Append(nl);
                script.Append("rem Waits for the game to release the plugin DLL, then copies the staged build over it.").Append(nl);
                script.Append("setlocal").Append(nl);
                script.Append("set \"STAGED=").Append(Batch(staged)).Append('"').Append(nl);
                script.Append("set \"TARGET=").Append(Batch(target)).Append('"').Append(nl);
                script.Append("set \"PENDING=").Append(Batch(pending)).Append('"').Append(nl);
                script.Append("set /a WAITS=0").Append(nl);
                script.Append(":wait").Append(nl);
                script.Append("ping -n ").Append(WaitSeconds + 1).Append(" 127.0.0.1 >nul").Append(nl);
                script.Append("copy /y \"%STAGED%\" \"%TARGET%\" >nul 2>&1").Append(nl);
                script.Append("if not errorlevel 1 goto applied").Append(nl);
                script.Append("set /a WAITS+=1").Append(nl);
                script.Append("if %WAITS% GEQ ").Append(MaxWaits).Append(" goto giveup").Append(nl);
                script.Append("goto wait").Append(nl);
                script.Append(":applied").Append(nl);
                script.Append("del \"%PENDING%\" >nul 2>&1").Append(nl);
                script.Append("del \"%STAGED%\" >nul 2>&1").Append(nl);
                script.Append("exit /b 0").Append(nl);
                script.Append(":giveup").Append(nl);
                script.Append("exit /b 1").Append(nl);
                return script.ToString();
            }

            script.Append("#!/bin/sh").Append('\n');
            script.Append("# Town Of Roles self-update applier. Written by the mod; not a hand-edited file.\n");
            script.Append("# Renames the staged build over the plugin DLL once the game releases it.\n");
            script.Append("STAGED='").Append(Shell(staged)).Append("'\n");
            script.Append("TARGET='").Append(Shell(target)).Append("'\n");
            script.Append("PENDING='").Append(Shell(pending)).Append("'\n");
            script.Append("WAITS=0\n");
            script.Append("while [ \"$WAITS\" -lt ").Append(MaxWaits).Append(" ]; do\n");

            script.Append("  if mv -f \"$STAGED\" \"$TARGET\" 2>/dev/null; then\n");
            script.Append("    rm -f \"$PENDING\"\n");
            script.Append("    exit 0\n");
            script.Append("  fi\n");
            script.Append("  WAITS=$((WAITS + 1))\n");
            script.Append("  sleep ").Append(WaitSeconds).Append("\n");
            script.Append("done\n");
            script.Append("exit 1\n");
            return script.ToString();
        }

        private static string Batch(string value) =>
            (value ?? string.Empty).Replace("%", "%%");

        private static string Shell(string value) =>
            (value ?? string.Empty).Replace("'", "'\\''");

        private static void Launch(string script, string workingDirectory)
        {
            var start = new ProcessStartInfo
            {
                FileName = IsWindows ? "cmd.exe" : "/bin/sh",
                Arguments = IsWindows ? "/c \"\"" + script + "\"\"" : "\"" + script + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };
            Process.Start(start);
        }

        private static void Cleanup(string staging)
        {
            try
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
            catch
            {
            }
        }

        private static void Log(string message)
        {
            try
            {
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogWarning(message);
            }
            catch
            {
            }
        }
    }
}
