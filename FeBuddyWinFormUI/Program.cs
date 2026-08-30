using System;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using FeBuddy.Versioning;
using FeBuddyLibrary.DataAccess;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Update;
using FeBuddyWinFormUI.Properties;
using Squirrel;

namespace FeBuddyWinFormUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // See issue #166 for why this code is here. 
            var invariantCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = invariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = invariantCulture;
            Thread.CurrentThread.CurrentCulture = invariantCulture;
            Thread.CurrentThread.CurrentUICulture = invariantCulture;
            // End of issue #166 workaround

            // TODO - Get system info and log it into file first thing. -https://docs.microsoft.com/en-us/previous-versions/windows/embedded/ee436483(v=msdn.10)
            Logger.CreateLogFile();
            SquirrelLogger.Register(); // wire up Squirrel logging to our log file too

            Logger.LogMessage("DEBUG", "PROGRAM STARTED");

            MigrateUserSettingsIfNeeded();

            // Squirrel starts our app during updates, sometimes we need to handle these events.
            // Our program may exit after and exit after handling one of these events.
            SquirrelAwareApp.HandleEvents(OnAppInstalled, OnAppUpdated, null, OnAppUninstalled);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // CS: we should set DPI awareness as PerMonitorV2
            // however currently this currently causes the application to break on
            // high-dpi monitors since the forms have not been re-written to accomodate the 
            // new scaling requirements. See the following for more details:
            // - https://docs.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
            // - https://docs.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms?view=netframeworkdesktop-4.8

            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);

            DirectoryHelpers.CheckTempDir();

            // API CALL TO GITHUB, WARNING ONLY 60 PER HOUR IS ALLOWED, WILL BREAK IF WE DO MORE!
            // CS: Note, this GitHub limit is based on IP, so is shared with every process at a
            // household or organisation. A read-only github token should be generated to remove
            // this limit.
            string version;
            try
            {
                version = CheckForUpdates();
            }
            catch (Exception ex)
            {
                // The update/migration path must never be able to stop the app from
                // starting. Anything unhandled here gets logged and swallowed so we still
                // fall through to launching the UI with a best-effort version string.
                Logger.LogMessage("ERROR", "CheckForUpdates threw and was suppressed: " + ex);
                version = GetApplicationVersion();
            }

            //LandingForm landingForm = new LandingForm(version);
            //landingForm.Show();

            // Start the application
            Application.Run(new LandingForm(version));
            //Application.Run(new AiracDataForm(version));
        }

        private static void OnAppInstalled(SemanticVersion ver, IAppTools tools)
        {
            // create initial application shortcuts
            tools.CreateShortcutForThisExe(ShortcutLocation.StartMenuRoot | ShortcutLocation.Desktop);
        }

        private static void OnAppUpdated(SemanticVersion ver, IAppTools tools)
        {
            // Remove the Start Menu shortcut in the Kyle Sanders directory if it exists
            var startmenuDir = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var oldShortcutDir = Path.Combine(startmenuDir, "Programs", "Kyle Sanders");
            if (Directory.Exists(oldShortcutDir))
            {
                try
                {
                    // CS: if the previous directory exists during an update, lets replace it
                    // with a new shortcut in the start menu root. We can't use 
                    // 'CreateShortcutForThisExe' here, as it's ignored during updates.
                    var myExeName = Path.GetFileName(SquirrelRuntimeInfo.EntryExePath);
                    tools.CreateShortcutsForExecutable(myExeName, ShortcutLocation.StartMenuRoot, false, null, null);

                    // delete old shortcut
                    Directory.Delete(oldShortcutDir);
                    Logger.LogMessage("DEBUG", "REPLACED OLD START SHORTCUT");
                }
                catch (Exception ex)
                {
                    Logger.LogMessage("DEBUG", "FAILED TO REMOVE OLD SHORTCUT " + ex.Message);
                }
            }
        }

        private static void OnAppUninstalled(SemanticVersion ver, IAppTools tools)
        {
            tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenuRoot | ShortcutLocation.Desktop);

            // TODO this should delete all the temporary directories.. everything created by this app.
        }

        public static void BackupSettings()
        {
            string settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

            if (!File.Exists(settingsFile))
            {
                Logger.LogMessage("DEBUG", "No user.config to back up before update - skipping.");
                return;
            }

            string destination = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            File.Copy(settingsFile, destination, true);
        }

        public static void RestoreSettings()
        {
            string destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            string sourceFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\last.config";
            if (!File.Exists(sourceFile))
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            }
            catch (Exception)
            {
            }

            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch (Exception)
            {
            }

            try
            {
                File.Delete(sourceFile);
            }
            catch (Exception)
            {
            }

        }

        private static void MigrateUserSettingsIfNeeded()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";

                if (string.Equals(Settings.Default.SettingsVersion, currentVersion, StringComparison.Ordinal))
                {
                    return;
                }

                Settings.Default.Upgrade();
                Settings.Default.SettingsVersion = currentVersion;
                Settings.Default.Save();

                Logger.LogMessage("INFO",
                    $"User settings migrated to version {currentVersion}. UpdateChannel='{Settings.Default.UpdateChannel}'.");
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "MigrateUserSettingsIfNeeded failed and was suppressed: " + ex);
            }
        }

        private static string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly()
                           .GetName()
                           .Version?
                           .ToString(3) ?? "dev";
        }

        // IsRunningFromMsiInstall/GetInstalledProductSemVer moved to
        // FeBuddyLibrary.Update.InstalledProduct - LandingForm's "Revert to Latest Stable"
        // action needs the same facts, so they're no longer private to Program.cs.
        private static bool IsRunningFromMsiInstall() => InstalledProduct.IsMsiInstalled();

        private static ReleaseChannel ReadUpdateChannelSetting()
        {
            var saved = Properties.Settings.Default.UpdateChannel;
            return Enum.TryParse<ReleaseChannel>(saved, out var channel) ? channel : ReleaseChannel.Stable;
        }

        /// <summary>
        /// Builds a user-facing explanation for a failed GitHub release/update check.
        /// When FEBUDDY_GITHUB_TOKEN is set, the check will already have retried with it
        /// (see GitHubAuth / UpdateChecker) - so a failure at that point usually points at
        /// the token itself (missing scope, no access to a private release repo, expired)
        /// rather than a plain network blip, and the message says so. Without a token, it
        /// steers a private-repo tester toward setting one.
        /// </summary>
        private static string DescribeUpdateCheckFailure(Exception e)
        {
            var lead = GitHubAuth.GetOptionalToken() != null
                ? "FE-BUDDY tried to reach GitHub - including a retry using your "
                  + $"{GitHubAuth.EnvironmentVariableName} environment variable - and it still failed.\n\n"
                  + "If FE-BUDDY is pointed at a private repo, check that the token is valid and has "
                  + "access to it. Otherwise this is most likely a temporary internet or GitHub outage."
                : "FE-BUDDY could not reach GitHub to check for updates - most likely a temporary "
                  + "internet or GitHub outage.\n\n"
                  + $"If FE-BUDDY is being tested against a private repo, set the {GitHubAuth.EnvironmentVariableName} "
                  + "environment variable to a token that can access it.";

            return lead + "\n\n" + e.Message;
        }

        /// <summary>
        /// MSI-path update check: uses UpdateChecker (real GitHub Releases + FeBuddy.Versioning)
        /// rather than Squirrel's GithubUpdateManager - see the Squirrel-to-MSI migration plan
        /// for why these are two separate paths. Failure is reported the same way the existing
        /// Squirrel-path check below already does, for consistency.
        /// </summary>
        private static void CheckForMsiUpdate(string installedVersion)
        {
            try
            {
                var channel = ReadUpdateChannelSetting();
                var candidate = UpdateChecker.CheckForUpdateAsync(installedVersion, channel).GetAwaiter().GetResult();

                if (candidate == null)
                {
                    return;
                }

                Logger.LogMessage("INFO", $"Update available: CURRENT VERSION {installedVersion} / GITHUB VERSION {candidate.Version}");

                using var updateForm = new UpdateAvailableForm(installedVersion, candidate);
                updateForm.ShowDialog();
            }
            catch (Exception e)
            {
                Logger.LogMessage("WARNING", "Unable to check for updates: " + e.Message);
                MessageBox.Show(
                    DescribeUpdateCheckFailure(e),
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Squirrel -> MSI migration, app side (see docs/SQUIRREL-TO-MSI-MIGRATION.md).
        /// When this is the Squirrel-installed copy, offer to download and run the MSI.
        /// Prompted on every launch until the user goes through with it. UpdateAvailableForm
        /// does the download + elevated launch + process exit on Yes, so this only returns
        /// when the user declined - the caller then falls through to the legacy Squirrel
        /// update path unchanged.
        /// </summary>
        private static void OfferSquirrelToMsiMigration(string applicationVersion)
        {
            try
            {
                var channel = ReadUpdateChannelSetting();
                var candidate = UpdateChecker.GetLatestForChannelAsync(channel).GetAwaiter().GetResult();

                if (candidate == null)
                {
                    Logger.LogMessage("INFO", $"Squirrel->MSI: no MSI release available on the {channel} channel yet - skipping migration offer.");
                    return;
                }

                Logger.LogMessage("INFO", $"Squirrel->MSI: offering migration from Squirrel {applicationVersion} to MSI {candidate.Version}.");

                using var migrateForm = new UpdateAvailableForm(
                    applicationVersion,
                    candidate,
                    headerText: "*** FE-BUDDY HAS A NEW INSTALLER ***",
                    questionText: "Download and install it now?",
                    currentVersionText: "Installed via the old per-user auto-updater",
                    newVersionText: $"Windows Installer package  •  v{candidate.Version}");
                migrateForm.ShowDialog();
            }
            catch (Exception e)
            {
                // Non-fatal for the app - but not silent: the user asked to be told when a
                // token was tried and still didn't work. After this we still fall through
                // to the legacy Squirrel updater.
                Logger.LogMessage("WARNING", "Squirrel->MSI: migration check failed - " + e.Message);
                MessageBox.Show(
                    "FE-BUDDY couldn't check for its new installer.\n\n" + DescribeUpdateCheckFailure(e),
                    "Installer Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Checks for updates, asks the user if they want to update now, and then
        /// returns the current version.
        /// </summary>
        private static string CheckForUpdates()
        {
            var applicationVersion = GetApplicationVersion();
            if (IsRunningFromMsiInstall())
            {
                // The real installed semver (with any -alpha/-beta/-rc tag) lives in the
                // registry, written by the installer - GetApplicationVersion() only has the
                // assembly's numeric-only version, which can't represent a prerelease tag.
                var installedVersion = InstalledProduct.GetProductSemVer() ?? applicationVersion;

                // If the old Squirrel install is still on this machine, now is the safe
                // time to remove it: we're running the MSI copy from Program Files, not the
                // Squirrel copy Update.exe is about to delete. Retried every launch until
                // the Squirrel Update.exe is actually gone.
                if (SquirrelInstall.LeftoverInstallExists())
                {
                    Logger.LogMessage("INFO", "Leftover Squirrel install detected - invoking its uninstaller.");
                    if (!SquirrelInstall.TryUninstall(TimeSpan.FromSeconds(60)))
                    {
                        Logger.LogMessage("WARNING", "Squirrel uninstall did not complete - will retry on next launch.");
                    }

                    // Squirrel's uninstaller deletes shortcuts by path, so it takes the
                    // MSI's own Start Menu / Desktop shortcuts with it. Put back any that
                    // the install's saved preferences say should exist and are now gone.
                    // Run this even on a partial uninstall - it can still have removed the
                    // shortcuts without removing the files.
                    MsiShortcutRepair.RecreateMissing();
                }

                CheckForMsiUpdate(installedVersion);
                return installedVersion;
            }

            // Still running from a Squirrel install: offer to switch to the MSI before
            // doing anything else. If the user accepts, the process exits inside this call.
            if (SquirrelInstall.IsCurrentProcessSquirrelInstalled())
            {
                OfferSquirrelToMsiMigration(applicationVersion);
            }

            // By default (on install) AllowPreRelease is false. This setting will only change if the user
            // "checks" the "Opt-In PreRelease" button under the settings menu
            using var ghu = new GithubUpdateManager("https://github.com/Nikolai558/FE-BUDDY", Properties.Settings.Default.AllowPreRelease);

            var currentVersion = ghu.CurrentlyInstalledVersion();

            if (currentVersion == null || !ghu.IsInstalledApp)
            {
                // we can't update if we're not a published app!
                return $"{applicationVersion} - DEV";
            }

            try
            {
                var updateInfo = ghu.CheckForUpdate().Result;
                if (updateInfo != null && updateInfo.ReleasesToApply.Count > 0)
                {
                    // there are updates available
                    Logger.LogMessage("INFO", "Update available: " +
                        $"CURRENT VERSION {currentVersion} / " +
                        $"GITHUB VERSION {updateInfo.FutureReleaseEntry.Version}");

                    UpdateForm processForm = new UpdateForm(
                        updateInfo.CurrentlyInstalledVersion?.Version.ToString() ?? "dev",
                        updateInfo.FutureReleaseEntry.Version.ToString())
                    {
                        Size = new Size(600, 600)
                    };
                    processForm.ChangeTitle("Update Available");
                    processForm.ChangeUpdatePanel(new Point(12, 52));
                    processForm.ChangeUpdatePanel(new Size(560, 370));
                    processForm.ChangeProcessingLabel(new Point(5, 5));
                    processForm.DisplayMessages(true);
                    var result = processForm.ShowDialog();
                    if (result == DialogResult.Yes)
                    {
                        Logger.LogMessage("DEBUG", "USER WANTS TO UPDATE");

                        string updateInformationMessage =
                            "Once you click 'OK', all screens related to FE-BUDDY will close.\n\n" +
                            "Once the program has fully updated, it will restart. This may take some time.";

                        MessageBox.Show(updateInformationMessage);

                        // TODO: we should show some progress UI while doing this.
                        BackupSettings();
                        var installedUpdate = ghu.UpdateApp().Result;
                        if (installedUpdate != null)
                        {
                            Logger.LogMessage("INFO", "RESTARTING...");
                            UpdateManager.RestartApp();
                            RestoreSettings();
                        }
                        else
                        {
                            Logger.LogMessage("INFO", "Update detected but no release was downloaded.");
                            MessageBox.Show("The update has failed, please check the log file.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogMessage("WARNING", "Unable to check for updates: " + e.Message);
                MessageBox.Show(
                    DescribeUpdateCheckFailure(e),
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // we have decided not to update, lets return current version
            return currentVersion.ToString();
        }
    }
}
