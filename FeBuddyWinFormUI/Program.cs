using FeBuddyLibrary;
using FeBuddyLibrary.Helpers;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeBuddyWinFormUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DirectoryHelpers.CheckTempDir();
            // API CALL TO GITHUB, WARNING ONLY 60 PER HOUR IS ALLOWED, WILL BREAK IF WE DO MORE!
            try
            {
                WebHelpers.UpdateCheck();
            }
            catch (Exception)
            {
                MessageBox.Show($"FE-BUDDY could not perform update check, please check internet connection.\n\nThis program will exit.\nPlease try again.");
                Environment.Exit(-1);
            }

            // Check Current Program Against Github, if different ask user if they want to update.
            CheckVersion();

            // Start the application
            Application.Run(new MainForm());
        }

        private static void CheckVersion()
        {
            // Check to see if Version's match.
            if (GlobalConfig.ProgramVersion != GlobalConfig.GithubVersion)
            {
                Processing processForm = new Processing
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
                    string updateInformationMessage = "Once you click 'OK', all screens related to FE-BUDDY will close.\n\n" +
                        "Once the program has fully updated, it will restart.";

                    MessageBox.Show(updateInformationMessage);
                    DownloadHelpers.DownloadAssets();

                    UpdateProgram();
                    StartNewVersion();
                    Environment.Exit(1);
                }
                else
                {
                    // User does not want to Update
                }
            }
        }

        private static async void UpdateProgram()
        {
            using (var updateManager = new UpdateManager($"{GlobalConfig.tempPath}"))
            {
                var releaseEntry = await updateManager.UpdateApp();
            }
        }

        private static void StartNewVersion()
        {
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

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            _ = Process.ExitCode;
            Process.Close();
        }
    }
}
