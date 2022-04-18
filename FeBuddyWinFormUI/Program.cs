using FeBuddyLibrary;
using FeBuddyLibrary.Helpers;
using Squirrel;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeBuddyWinFormUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            
            // TODO - Get system info and log it into file first thing. -https://docs.microsoft.com/en-us/previous-versions/windows/embedded/ee436483(v=msdn.10)
            Logger.CreateLogFile();
            SquirrelLogger.Register(); // wire up Squirrel logging to our log file too
          
            Logger.LogMessage("DEBUG", "PROGRAM STARTED");

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
            var version = CheckForUpdates();

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
                    var myExeName = Path.GetFileName(AssemblyRuntimeInfo.EntryExePath);
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

        /// <summary>
        /// Checks for updates, asks the user if they want to update now, and then
        /// returns the current version.
        /// </summary>
        private static string CheckForUpdates()
        {
            // By default (on install) AllowPreRelease is false. This setting will only change if the user
            // "checks" the "Opt-In PreRelease" button under the settings menu
            using var ghu = new GithubUpdateManager("https://github.com/Nikolai558/FE-BUDDY", Properties.Settings.Default.AllowPreRelease);

            var currentVersion = ghu.CurrentlyInstalledVersion();

            if (currentVersion == null || !ghu.IsInstalledApp)
            {
                // we can't update if we're not a published app!
                return "dev";
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
                        var installedUpdate = ghu.UpdateApp().Result;
                        if (installedUpdate != null)
                        {
                            Logger.LogMessage("INFO", "RESTARTING...");
                            UpdateManager.RestartApp();
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
                MessageBox.Show($"FE-BUDDY could not perform an update check due to either your internet connection or GitHub Server issues.\n\n" + e.ToString());
            }

            // we have decided not to update, lets return current version
            return currentVersion.ToString();
        }
    }
}
