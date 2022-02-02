using FeBuddyLibrary;
using FeBuddyLibrary.Helpers;
using Squirrel;
using System;
using System.Diagnostics;
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
            Logger.LogMessage("DEBUG", "PROGRAM STARTED");

            // Squirrel starts our app during updates, sometimes we need to handle these events.
            // Our program may exit after and exit after handling one of these events.
            SquirrelAwareApp.HandleEvents(OnAppInstalled, OnAppUpdated, null, OnAppUninstalled);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // CS: we should set DPI awareness as PerMonitorV2 in the application manifest
            // ideally, however currently this currently causes the application to break on
            // high-dpi monitors since the forms have not been re-written to accomodate the 
            // new scaling requirements. See the following for more details:
            // - https://docs.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
            // - https://docs.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms?view=netframeworkdesktop-4.8

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);

            DirectoryHelpers.CheckTempDir();

            // API CALL TO GITHUB, WARNING ONLY 60 PER HOUR IS ALLOWED, WILL BREAK IF WE DO MORE!
            // CS: Note, this GitHub limit is based on IP, so is shared with every process at a
            // household or organisation. A read-only github token should be generated to remove
            // this limit.
            try
            {
                WebHelpers.UpdateCheck();

                // Check Current Program Against Github, if different ask user if they want to update.
                CheckVersion();
            }
            catch (Exception)
            {
                Logger.LogMessage("WARNING", "COULD NOT PERFORM UPDATE CHECK");
                MessageBox.Show($"FE-BUDDY could not perform an update check due to either your internet connection or GitHub Server issues.\n\n");
                //Environment.Exit(-1);
            }

            // Start the application
            Application.Run(new MainForm());
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
        }

        private static void CheckVersion()
        {
            Logger.LogMessage("DEBUG", "CHECKING PROGRAM VERSION AGAINST GITHUB VERSION");

            // Check to see if Version's match.
            if (GlobalConfig.ProgramVersion != GlobalConfig.GithubVersion)
            {
                Logger.LogMessage("INFO", $"PROGRAM VERSION {GlobalConfig.ProgramVersion} / GITHUB VERSION {GlobalConfig.GithubVersion}");

                UpdateForm processForm = new UpdateForm
                {
                    Size = new Size(600, 600)
                };
                processForm.ChangeTitle("Update Available");
                processForm.ChangeUpdatePanel(new Point(12, 52));
                processForm.ChangeUpdatePanel(new Size(560, 370));
                processForm.ChangeProcessingLabel(new Point(5, 5));
                processForm.DisplayMessages(true);
                processForm.ShowDialog();

                if (GlobalConfig.updateProgram)
                {
                    Logger.LogMessage("DEBUG", "USER WANTS TO UPDATE");

                    string updateInformationMessage = "Once you click 'OK', all screens related to FE-BUDDY will close.\n\n" +
                        "Once the program has fully updated, it will restart.";

                    MessageBox.Show(updateInformationMessage);
                    DownloadHelpers.DownloadAssets();

                    UpdateProgram().Wait();
                    Logger.LogMessage("INFO", "CLOSING OLD PROGRAM VERSION");
                    UpdateManager.RestartApp();
                    Environment.Exit(1);
                }
                else
                {
                    Logger.LogMessage("INFO", "USER DID NOT WANT TO UPDATE");

                    // User does not want to Update
                }
            }
        }

        private static async Task<ReleaseEntry> UpdateProgram()
        {
            Logger.LogMessage("INFO", "UPDATING PROGRAM");

            using (var updateManager = new UpdateManager($"{GlobalConfig.tempPath}", "FE-Buddy"))
            {
                return await updateManager.UpdateApp();
            }
        }
    }
}
