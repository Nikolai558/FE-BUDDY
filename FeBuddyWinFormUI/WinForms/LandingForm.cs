using FeBuddyLibrary;
using FeBuddyLibrary.DataAccess;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FeBuddyWinFormUI
{
    public partial class LandingForm : Form
    {
        public LandingForm()
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();
            

            // It should grab from the assembily info. 
            this.Text = $"FE-BUDDY - V{GlobalConfig.ProgramVersion}";
        }

        private class MyRenderer : ToolStripProfessionalRenderer
        {
            public MyRenderer() : base(new MyColors()) { }
        }

        private class MyColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return Color.Black; }
            }
            public override Color MenuItemSelectedGradientBegin
            {
                get { return Color.Black; }
            }
            public override Color MenuItemSelectedGradientEnd
            {
                get { return Color.Black; }
            }
            public override Color MenuItemPressedGradientBegin
            {
                get { return Color.Black; }
            }

            public override Color MenuItemPressedGradientEnd
            {
                get { return Color.Black; }
            }
        }

        private void LandingForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "Landing FORM CLOSING");
        }

        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(
            Control control,
            string propertyName,
            object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate
                (SetControlPropertyThreadSafe),
                new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    control,
                    new object[] { propertyValue });
            }
        }

        private void LandingForm_Shown(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "SHOWING Landing FORM");
        }

        private void LandingForm_Load(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "LOADING Landing FORM");

            var pfc = new PrivateFontCollection();
            pfc.AddFontFile("Properties\\romantic.ttf");
            // TODO - Add fonts to buttons. 
            InstructionsMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            CreditsMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            ChangeLogMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            UninstallMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            FAQMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            RoadmapMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            informationToolStripMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            settingsToolStripMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            reportIssuesToolStripMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
        }
        
        private void UninstallMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("WARNING", "UNINSTALL MENU ITEM CLICKED");

            DialogResult dialogResult = MessageBox.Show("Would you like to UNINSTALL FE-BUDDY?", "Uninstall FE-BUDDY", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Logger.LogMessage("WARNING", "CONFIRMATION USER WANTS TO UNINSTALL");

                string uninstall_start_string = $"start \"\" \"{Path.GetTempPath()}UNINSTALL_FE-BUDDY.bat\"";

                string uninstallBatchFileString = "@echo off\n"
                        + "PING 127.0.0.1 - n 5 > nul\n"
                        + "tasklist /FI \"IMAGENAME eq FE-BUDDY.exe\" 2>NUL | find /I /N \"FE-BUDDY.exe\">NUL\n"
                        + "if \"%ERRORLEVEL%\"==\"0\" taskkill /F /im FE-BUDDY.exe\n"
                        + "\n"
                        + "TITLE FE-BUDDY UNINSTALL\n"
                        + "\n"
                        + "SET /A NOT_FOUND_COUNT=0\n"
                        + "\n"
                        + "CD /d \"%temp%\"\n"
                        + "	if NOT exist FE-BUDDY (\n"
                        + "		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1\n"
                        + "		SET FE-BUDDY_TEMP_FOLDER=NOT_FOUND\n"
                        + "	)\n"
                        + "	\n"
                        + "	if exist FE-BUDDY (\n"
                        + "		SET FE-BUDDY_TEMP_FOLDER=FOUND\n"
                        + "		RD /Q /S \"FE-BUDDY\"\n"
                        + "	)\n"
                        + "\n"
                        + "CD /d \"%userprofile%\\AppData\\Local\"\n"
                        + "	if NOT exist FE-BUDDY (\n"
                        + "		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1\n"
                        + "		SET FE-BUDDY_APPDATA_FOLDER=NOT_FOUND\n"
                        + "	)\n"
                        + "	\n"
                        + "	if exist FE-BUDDY (\n"
                        + "		SET FE-BUDDY_APPDATA_FOLDER=FOUND\n"
                        + "		RD /Q /S \"FE-BUDDY\"\n"
                        + "	)\n"
                        + "\n"
                        + "CD /d \"%userprofile%\\Desktop\"\n"
                        + "	if NOT exist FE-BUDDY.lnk (\n"
                        + "		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1\n"
                        + "		SET FE-BUDDY_SHORTCUT=NOT_FOUND\n"
                        + "	)\n"
                        + "\n"
                        + "	if exist FE-BUDDY.lnk (\n"
                        + "		SET FE-BUDDY_SHORTCUT=FOUND\n"
                        + "		DEL /Q \"FE-BUDDY.lnk\"\n"
                        + "	)\n"
                        + "\n"
                        + "IF %NOT_FOUND_COUNT%==0 SET UNINSTALL_STATUS=COMPLETE\n"
                        + "IF %NOT_FOUND_COUNT%==1 SET UNINSTALL_STATUS=PARTIAL\n"
                        + "IF %NOT_FOUND_COUNT%==2 SET UNINSTALL_STATUS=PARTIAL\n"
                        + "IF %NOT_FOUND_COUNT%==3 SET UNINSTALL_STATUS=FAIL\n"
                        + "\n"
                        + "IF %UNINSTALL_STATUS%==COMPLETE GOTO UNINSTALLED\n"
                        + "IF %UNINSTALL_STATUS%==PARTIAL GOTO UNINSTALLED\n"
                        + "IF %UNINSTALL_STATUS%==FAIL GOTO FAILED\n"
                        + "\n"
                        + "CLS\n"
                        + "\n"
                        + ":UNINSTALLED\n"
                        + "\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO SUCCESSFULLY UNINSTALLED THE FOLLOWING:\n"
                        + "ECHO.\n"
                        + "IF %FE-BUDDY_TEMP_FOLDER%==FOUND ECHO        -temp\\FE-BUDDY\n"
                        + "IF %FE-BUDDY_APPDATA_FOLDER%==FOUND ECHO        -AppData\\Local\\FE-BUDDY\n"
                        + "IF %FE-BUDDY_SHORTCUT%==FOUND ECHO        -Desktop\\FE-BUDDY Shortcut\n"
                        + "\n"
                        + ":FAILED\n"
                        + "\n"
                        + "IF NOT %NOT_FOUND_COUNT%==0 (\n"
                        + "	ECHO.\n"
                        + "	ECHO.\n"
                        + "	ECHO.\n"
                        + "	ECHO.\n"
                        + "	IF %UNINSTALL_STATUS%==PARTIAL ECHO NOT ABLE TO COMPLETELY UNINSTALL BECAUSE THE FOLLOWING COULD NOT BE FOUND:\n"
                        + "	IF %UNINSTALL_STATUS%==FAIL ECHO UNINSTALL FAILED COMPLETELY BECAUSE THE FOLLOWING COULD NOT BE FOUND:\n"
                        + "	ECHO.\n"
                        + "	IF %FE-BUDDY_TEMP_FOLDER%==NOT_FOUND ECHO        -temp\\FE-BUDDY\n"
                        + "	IF %FE-BUDDY_APPDATA_FOLDER%==NOT_FOUND ECHO        -AppData\\Local\\FE-BUDDY\n"
                        + "	IF %FE-BUDDY_SHORTCUT%==NOT_FOUND (\n"
                        + "		ECHO        -Desktop\\FE-BUDDY Shortcut\n"
                        + "		ECHO             --If the shortcut was renamed, delete the shortcut manually.\n"
                        + "	)\n"
                        + ")\n"
                        + "\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO ...PRESS ANY KEY TO EXIT\n"
                        + "\n"
                        + "PAUSE>NUL\n";

                File.WriteAllText($"{Path.GetTempPath()}UNINSTALL_FE-BUDDY.bat", uninstallBatchFileString);
                File.WriteAllText($"{Path.GetTempPath()}UNINSTALL_START_FE-BUDDY.bat", uninstall_start_string);

                ProcessStartInfo ProcessInfo;
                Process Process;

                ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + $"\"{Path.GetTempPath()}UNINSTALL_START_FE-BUDDY.bat\"")
                {
                    CreateNoWindow = false,
                    UseShellExecute = false
                };

                Process = Process.Start(ProcessInfo);

                Process.Close();
                Environment.Exit(1);
            }
        }

        private void InstructionsMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "INSTRUCTIONS MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://docs.google.com/presentation/d/e/2PACX-1vRMd6PIRrj0lPb4sAi9KB7iM3u5zn0dyUVLqEcD9m2e71nf0UPyEmkOs4ZwYsQdl7smopjdvw_iWEyP/embed") { UseShellExecute = true });
            //Process.Start("https://docs.google.com/presentation/d/e/2PACX-1vRMd6PIRrj0lPb4sAi9KB7iM3u5zn0dyUVLqEcD9m2e71nf0UPyEmkOs4ZwYsQdl7smopjdvw_iWEyP/embed");
        }

        private void RoadmapMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "ROADMAP MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/releases/ROADMAP.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/releases/ROADMAP.md");
        }

        private void FAQMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "FAQ MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://docs.google.com/presentation/d/e/2PACX-1vSlhz1DhDwZ-43BY4Q2vg-ff0QBGssxpmv4-nhZlz9LpGJvWjqLsHVaQwwsV1AGMWFFF_x_j_b3wTBO/embed") { UseShellExecute = true });
            //Process.Start("https://docs.google.com/presentation/d/e/2PACX-1vSlhz1DhDwZ-43BY4Q2vg-ff0QBGssxpmv4-nhZlz9LpGJvWjqLsHVaQwwsV1AGMWFFF_x_j_b3wTBO/embed");
        }

        private void ChangeLogMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "CHANGELOG MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/releases/ChangeLog.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/releases/ChangeLog.md");
        }

        private void CreditsMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "CREDITS MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/releases/Credits.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/releases/Credits.md");
            // CreditsForm frm = new CreditsForm();
            // frm.ShowDialog();
        }

        private void reportIssuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "REPORT ISSUES MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/issues") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/issues");
        }

        private void landingStartButton_MouseHover(object sender, EventArgs e)
        {
            var startToolTip = new ToolTip();
            startToolTip.SetToolTip(landingStartButton, "I'm not your friend, Guy...");
        }
    }
}
