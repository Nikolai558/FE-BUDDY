using FeBuddyLibrary;
using FeBuddyLibrary.Helpers;
using Squirrel;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

            //Console.WriteLine(Logger.logFilePath);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DirectoryHelpers.CheckTempDir();
            // API CALL TO GITHUB, WARNING ONLY 60 PER HOUR IS ALLOWED, WILL BREAK IF WE DO MORE!
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

            Application.Run(new LandingForm());

            // Start the application
            Application.Run(new MainForm());
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

                    UpdateProgram();
                    StartNewVersion();
                    Logger.LogMessage("INFO", "CLOSING OLD PROGRAM VERSION");
                    Environment.Exit(1);
                }
                else
                {
                    Logger.LogMessage("INFO", "USER DID NOT WANT TO UPDATE");

                    // User does not want to Update
                }
            }
        }

        private static async void UpdateProgram()
        {
            Logger.LogMessage("INFO", "UPDATING PROGRAM");

            using (var updateManager = new UpdateManager($"{GlobalConfig.tempPath}"))
            {
                var releaseEntry = await updateManager.UpdateApp();
            }
        }

        private static void StartNewVersion()
        {
            Logger.LogMessage("DEBUG", "PREPARING TO START NEW PROGRAM VERSION");

            string filePath = $"{GlobalConfig.tempPath}\\startNewVersion.bat";
            string writeMe =
                "SET /A COUNT=0\n\n" +
                ":CHK\n" +
                $"IF EXIST \"%userprofile%\\AppData\\Local\\FE-BUDDY\\app-{GlobalConfig.GithubVersion}\\FE-BUDDY.exe\" goto FOUND\n" +
                "SET /A COUNT=%COUNT% + 1\n" +
                "IF %COUNT% GEQ 12 GOTO FOUND\n" +
                "PING 127.0.0.1 -n 3 >nul\n" +
                "GOTO CHK\n\n" +
                ":FOUND\n" +
                $"start \"\" \"%userprofile%\\AppData\\Local\\FE-BUDDY\\app-{GlobalConfig.GithubVersion}\\FE-BUDDY.exe\"\n";

            File.WriteAllText(filePath, writeMe);
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + $"\"{GlobalConfig.tempPath}\\startNewVersion.bat\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Logger.LogMessage("INFO", "STARTING NEW PROGRAM VERSION");
            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            _ = Process.ExitCode;
            Process.Close();
        }
    }
}
