using System;
using System.Diagnostics;
using System.Windows.Forms;
using FeBuddyLibrary.Helpers;

namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// Backs the "Uninstall" menu item on every FE-BUDDY form. Removes the running
    /// install using the mechanism that matches how it was installed:
    ///
    ///  - MSI install (Program Files - <see cref="InstalledProduct.IsMsiInstalled"/>):
    ///    launches "msiexec /x {ProductCode}", the standard interactive Windows uninstall
    ///    (confirmation + progress UI, self-elevating via UAC). The ProductCode comes from
    ///    the registry value the installer writes (FE-BUDDY.Installer\InstallerState.wxs).
    ///  - Squirrel install (%LocalAppData%\FE-BUDDY -
    ///    <see cref="SquirrelInstall.IsCurrentProcessSquirrelInstalled"/>): runs
    ///    "Update.exe --uninstall", Clowd.Squirrel's own uninstaller, which removes the
    ///    files, the per-user Add/Remove Programs entry and the shortcuts it created.
    ///  - Anything else (dev build, xcopy, an install we can't classify): no destructive
    ///    action - just points the user at Windows "Installed apps".
    ///
    /// Replaces the old hand-rolled batch file that blind-deleted %LocalAppData%\FE-BUDDY
    /// and the shortcuts by path - which left an orphaned Add/Remove Programs entry for
    /// both install types and did nothing at all for a Program Files MSI install.
    ///
    /// The confirmation prompt is the caller's job; by the time this runs the user has
    /// already said yes.
    /// </summary>
    public static class AppUninstaller
    {
        /// <summary>
        /// Starts the uninstall appropriate to this install. If an uninstaller was
        /// launched, exits the process (exit code 0) so it isn't removing files out from
        /// under a running FE-BUDDY. Returns normally only when nothing was started.
        /// </summary>
        public static void Uninstall()
        {
            if (InstalledProduct.IsMsiInstalled())
            {
                if (TryStartMsiUninstall())
                {
                    Environment.Exit(0);
                }

                return;
            }

            if (SquirrelInstall.IsCurrentProcessSquirrelInstalled())
            {
                if (TryStartSquirrelUninstall())
                {
                    Environment.Exit(0);
                }

                return;
            }

            Logger.LogMessage("WARNING", "AppUninstaller: this copy is neither an MSI nor a Squirrel install - nothing to uninstall automatically.");
            MessageBox.Show(
                "This copy of FE-BUDDY isn't a standard install, so there's nothing to uninstall automatically.\n\n" +
                "If you installed it with the FE-BUDDY installer, remove it from Windows Settings → Apps → Installed apps.",
                "Uninstall FE-BUDDY",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static bool TryStartMsiUninstall()
        {
            var productCode = InstalledProduct.GetProductCode();
            if (string.IsNullOrWhiteSpace(productCode))
            {
                Logger.LogMessage("WARNING", "AppUninstaller: MSI install detected but no ProductCode in the registry - pointing the user at Windows 'Installed apps'.");
                MessageBox.Show(
                    "FE-BUDDY couldn't determine its installer identity.\n\n" +
                    "Please uninstall it from Windows Settings → Apps → Installed apps.",
                    "Uninstall FE-BUDDY",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/x {productCode}",
                    UseShellExecute = true,
                };

                Logger.LogMessage("INFO", $"AppUninstaller: launching msiexec /x {productCode}");
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "AppUninstaller: failed to launch msiexec /x - " + ex.Message);
                MessageBox.Show(
                    "FE-BUDDY couldn't start the Windows uninstaller.\n\n" +
                    "Please uninstall it from Windows Settings → Apps → Installed apps.",
                    "Uninstall FE-BUDDY",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }

        private static bool TryStartSquirrelUninstall()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = SquirrelInstall.UpdateExePath,
                    Arguments = "--uninstall",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                Logger.LogMessage("INFO", $"AppUninstaller: launching \"{SquirrelInstall.UpdateExePath}\" --uninstall");
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "AppUninstaller: failed to launch Update.exe --uninstall - " + ex.Message);
                MessageBox.Show(
                    "FE-BUDDY couldn't start its uninstaller.",
                    "Uninstall FE-BUDDY",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
