using System;
using System.Diagnostics;
using System.IO;
using FeBuddyLibrary.Helpers;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Facts about, and cleanup of, a leftover Clowd.Squirrel install - the per-user
    /// auto-updater FE-BUDDY is migrating away from (see docs/SQUIRREL-TO-MSI-MIGRATION.md).
    ///
    /// Squirrel installs per-user under %LocalAppData%\FE-BUDDY\ with Update.exe at the
    /// root and the app itself in an app-&lt;version&gt;\ (or current\) subfolder. It also
    /// registers a per-user Add/Remove Programs entry and routes its shortcuts through
    /// Update.exe - so the leftover install must be removed by invoking Squirrel's own
    /// uninstaller (Update.exe --uninstall), never by hand-deleting the folder.
    ///
    /// The migration flow this supports:
    ///  - A Squirrel-installed FE-BUDDY (2.8.4+) sees IsCurrentProcessSquirrelInstalled()
    ///    is true and offers to download + run the MSI.
    ///  - Once the MSI copy (Program Files) is what's running, LeftoverInstallExists() is
    ///    still true, so it calls TryUninstall() - safe now, because the running process
    ///    is no longer the Squirrel copy that Update.exe is about to delete.
    /// </summary>
    public static class SquirrelInstall
    {
        private static string RootDir =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FE-BUDDY");

        /// <summary>Full path to Squirrel's Update.exe for the current user (may not exist).</summary>
        public static string UpdateExePath => Path.Combine(RootDir, "Update.exe");

        /// <summary>
        /// True if a Squirrel install exists for the current user - i.e. Update.exe is
        /// present under %LocalAppData%\FE-BUDDY\ - regardless of which copy of FE-BUDDY
        /// is running right now. Update.exe (not just the folder) is the marker: after
        /// --uninstall runs, Update.exe is removed even if an empty folder lingers
        /// briefly, so this stops reporting a leftover once cleanup has actually worked.
        /// </summary>
        public static bool LeftoverInstallExists() => File.Exists(UpdateExePath);

        /// <summary>
        /// True if the currently running FE-BUDDY IS the Squirrel-installed copy - running
        /// from a subfolder of %LocalAppData%\FE-BUDDY\ with Update.exe alongside. This is
        /// the "I should offer to migrate to the MSI" check; it is deliberately false for
        /// an MSI install, a dev build, or a copy run from anywhere else.
        /// </summary>
        public static bool IsCurrentProcessSquirrelInstalled()
        {
            if (!LeftoverInstallExists())
            {
                return false;
            }

            var baseDir = SafeFullPath(AppContext.BaseDirectory);
            var root = SafeFullPath(RootDir);

            return baseDir != null
                && root != null
                && baseDir.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Invokes Squirrel's own uninstaller (Update.exe --uninstall) and waits up to
        /// <paramref name="timeout"/> for it to finish. Returns true only if Update.exe
        /// ran and exited 0 (or there was nothing to remove). Never throws - a failed or
        /// timed-out cleanup is logged and left for the next launch to retry, since
        /// LeftoverInstallExists() will still be true.
        ///
        /// Refuses to run if called from the Squirrel copy itself (Update.exe deleting the
        /// files out from under the running process); only call this from the MSI copy.
        /// </summary>
        public static bool TryUninstall(TimeSpan timeout)
        {
            try
            {
                if (!LeftoverInstallExists())
                {
                    return true;
                }

                if (IsCurrentProcessSquirrelInstalled())
                {
                    Logger.LogMessage("WARNING", "SquirrelInstall.TryUninstall was called from the Squirrel copy itself - refusing (would delete the running app).");
                    return false;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = UpdateExePath,
                    Arguments = "--uninstall",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                Logger.LogMessage("INFO", $"SquirrelInstall: running \"{UpdateExePath}\" --uninstall");

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Logger.LogMessage("WARNING", "SquirrelInstall: Process.Start returned null for Update.exe --uninstall.");
                    return false;
                }

                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    Logger.LogMessage("WARNING", "SquirrelInstall: Update.exe --uninstall timed out - will retry on next launch.");
                    return false;
                }

                Logger.LogMessage("INFO", $"SquirrelInstall: Update.exe --uninstall exited {process.ExitCode}.");
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "SquirrelInstall: Update.exe --uninstall failed - " + ex.Message);
                return false;
            }
        }

        private static string SafeFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            }
            catch
            {
                return null;
            }
        }
    }
}
