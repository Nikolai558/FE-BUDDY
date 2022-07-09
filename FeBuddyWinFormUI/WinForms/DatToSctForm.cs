using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using FeBuddyLibrary;
using FeBuddyLibrary.Dxf;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    public partial class DatToSctForm : Form
    {
        private readonly string _currentVersion;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();
        private ConversionOptions _conversionOptions;
        private ToolTip _toolTip;

        public DatToSctForm(string currentVersion)
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");
            _conversionOptions = new ConversionOptions();
            _toolTip = new ToolTip();

            _pfc.AddFontFile("Properties\\romantic.ttf");

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();

            // It should grab from the assembily info. 
            this.Text = $"FE-BUDDY - V{currentVersion}";

            _currentVersion = currentVersion;
        }

        private void inputButton_Click(object sender, EventArgs e)
        {
            

        }

        private void outputDirButton_Click(object sender, EventArgs e)
        {
            
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            
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

        private void DatToSctForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "SctToDxfForm_Closing");
        }

        private void AiracDataForm_Load(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "LOADING MAIN FORM");

            // TODO - Add fonts to buttons?
            InstructionsMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            CreditsMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            ChangeLogMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            UninstallMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            FAQMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            RoadmapMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            informationToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            settingsToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            reportIssuesToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            discordToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            //mainMenuMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
            //exitMenuItem.Font = new Font(pfc.Families[0], 12, FontStyle.Regular);
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
                        + "CD /d \"%appdata%\\Microsoft\\Windows\\Start Menu\\Programs\"\n"
                        + " if NOT exist \"Kyle Sanders\" (\n"
                        + "     SET OLD_START_SHORTCUT=NOT_FOUND\n"
                        + ")\n"
                        + "\n"
                        + "	if exist \"Kyle Sanders\" (\n"
                        + "		SET OLD_START_SHORTCUT=FOUND\n"
                        + "		RD /Q /S \"Kyle Sanders\"\n"
                        + "	)\n"
                        + "\n"
                        + "	if NOT exist FE-BUDDY.lnk (\n"
                        + "		SET /A NOT_FOUND_COUNT=%NOT_FOUND_COUNT% + 1\n"
                        + "		SET NEW_START_SHORTCUT=NOT_FOUND\n"
                        + "	)\n"
                        + "\n"
                        + "	if exist FE-BUDDY.lnk (\n"
                        + "		SET NEW_START_SHORTCUT=FOUND\n"
                        + "		DEL /Q \"FE-BUDDY.lnk\"\n"
                        + "	)\n"
                        + "\n"
                        + "IF %NOT_FOUND_COUNT%==0 SET UNINSTALL_STATUS=COMPLETE\n"
                        + "IF %NOT_FOUND_COUNT% GEQ 1 SET UNINSTALL_STATUS=PARTIAL\n"
                        + "IF %NOT_FOUND_COUNT%==4 SET UNINSTALL_STATUS=FAIL\n"
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
                        + "IF %OLD_START_SHORTCUT%==FOUND ECHO        -Start Menu\\Kyle Sanders\n"
                        + "IF %NEW_START_SHORTCUT%==FOUND ECHO        -Start Menu\\FE-BUDDY Shortcut\n"
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
                        + " IF %NEW_START_SHORTCUT%==NOT_FOUND ECHO        -Start Menu\\FE-BUDDY Shortcut\n"
                        + ")\n"
                        + "\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO.\n"
                        + "ECHO ...Close this prompt when ready.\n"
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

        private void allowBetaMenuItem_Click(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AllowPreRelease)
            {

                DialogResult warningMSG = MessageBox.Show(
                    "WARNING: \nDO NOT ENABLE THIS UNLESS \nTOLD TO DO SO BY THE DEVELOPER\n\n Enable Dev testing Mode?",
                    "DEV TESTING MODE",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Stop,
                    MessageBoxDefaultButton.Button2);


                if (warningMSG == DialogResult.Yes)
                {
                    allowBetaMenuItem.Checked = !allowBetaMenuItem.Checked;

                    Properties.Settings.Default.AllowPreRelease = allowBetaMenuItem.Checked;
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                allowBetaMenuItem.Checked = !allowBetaMenuItem.Checked;

                Properties.Settings.Default.AllowPreRelease = allowBetaMenuItem.Checked;
                Properties.Settings.Default.Save();
            }
        }

        private void discordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "DISCORD MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://discord.com/invite/GB46aeauH4") { UseShellExecute = true });
        }

        private void outputDirButton_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(outputDirButton, _conversionOptions.outputDirectory);
        }

        private void inputButton_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(sourceFileButton, _conversionOptions.InputFilePath);
        }
    }
}
