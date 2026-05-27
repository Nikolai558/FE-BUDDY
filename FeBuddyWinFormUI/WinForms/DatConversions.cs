using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FeBuddyLibrary;
using FeBuddyLibrary.Dat;
using FeBuddyLibrary.Dxf;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    public partial class DatConversions : Form
    {
        private readonly string _currentVersion;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();
        private ConversionOptions _conversionOptions;
        private ToolTip _toolTip;

        public DatConversions(string currentVersion)
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
            Logger.LogMessage("DEBUG", "USER CHOOSING DIFFERENT Input file for DAT Conversion tool");

            OpenFileDialog inputFileDialog = new OpenFileDialog();

            // Allow multiple file selections
            inputFileDialog.Multiselect = true;

            // Display the file dialog
            DialogResult result = inputFileDialog.ShowDialog();

            // Check if files were selected
            if (result == DialogResult.OK && inputFileDialog.FileNames.Length > 0)
            {
                _conversionOptions.InputFilePathForDat = inputFileDialog.FileNames;

                // Clear and update the button text for each selected file
                sourceFileButton.Text = string.Join("\n", _conversionOptions.InputFilePathForDat.Select(filePath =>
                {
                    string text = "";
                    if (filePath.Length >= 20)
                    {
                        if (filePath[^17..].Contains('\\'))
                        {
                            text = "..\\" + filePath[^17..].Split('\\')[^1];
                        }
                        else
                        {
                            text = "..\\.." + filePath[^15..];
                        }
                    }
                    return text;
                }));

                sourceFileButton.TextAlign = ContentAlignment.MiddleCenter;
                sourceFileButton.AutoSize = false;
            }
        }

        private void outputDirButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "USER CHOOSING DIFFERENT output directory for DAT Conversion tool");

            FolderBrowserDialog outputDirDialog = new FolderBrowserDialog();

            outputDirDialog.ShowDialog();

            _conversionOptions.outputDirectory = outputDirDialog.SelectedPath;

            string text = _conversionOptions.outputDirectory;

            if (text.Length >= 20)
            {
                if (text[^17..].Contains('\\'))
                {
                    text = "..\\" + text[^17..].Split('\\')[^1];
                }
                else
                {
                    text = "..\\.." + text[^15..];
                }
            }

            outputDirButton.Text = text;
            outputDirButton.TextAlign = ContentAlignment.MiddleCenter;
            outputDirButton.AutoSize = false;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "USER clicked start button");

            string errorMessages = "";

            foreach (string filePath in _conversionOptions.InputFilePathForDat)
            {
                if (string.IsNullOrWhiteSpace(filePath)) errorMessages += "Input File Path is invalid.\n";
                if (string.IsNullOrWhiteSpace(_conversionOptions.outputDirectory)) errorMessages += "Output Directory is invalid.\n";

                if (filePath?.Split('.')[^1] != "dat") errorMessages += "Source file is not a .dat\n";

                if (!string.IsNullOrWhiteSpace(filePath) && !File.Exists(filePath))
                {
                    errorMessages += "Listen here, Buddy.... Do not change the file name after you've selected it in this program.\n";
                }
                if (!string.IsNullOrWhiteSpace(_conversionOptions.outputDirectory) && !Directory.Exists(_conversionOptions.outputDirectory))
                {
                    errorMessages += "Listen here, Buddy.... Do not change the folder name after you've selected it in this program.\n";
                }
                if (filePath.Split('\\')[^1].Split('.')[0].Length >= 26)
                {
                    errorMessages += "C'mon Mate, that's a bloody long name...Input file name must be less than 26 characters long.";
                }
                if (!string.IsNullOrWhiteSpace(cropingDistanceTextBox.Text))
                {
                    if (!IsValidCroppingDistance(cropingDistanceTextBox.Text))
                    {
                        errorMessages += "Cropping distance must be a number greater than 0.";
                    }
                }


                if (errorMessages != "")
                {
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result;

                    result = MessageBox.Show(errorMessages, "An invalid operation occured.", buttons);
                    return;
                }

                StartConversion(filePath);
                MessageBoxButtons buttons2 = MessageBoxButtons.OK;
                MessageBox.Show("Please allow a second or so before clicking ok, this is purely so we can ensure we dont overload the converter!", "Important!", buttons2);
            }
        }

        private bool IsValidCroppingDistance(string input)
        {
            double num;
            if (double.TryParse(input, out num))
            {
                return num > 0;
            }
            else
            {
                return false;
            }

        }

        private void ToggleComponents(bool isEnabled)
        {
            sourceFileButton.Enabled = isEnabled;
            outputDirButton.Enabled = isEnabled;
            startButton.Enabled = isEnabled;
            geojsonRadioButton.Enabled = isEnabled;
            sct2RadioButton.Enabled = isEnabled;
        }

        private void StartConversion(string filePath)
        {
            ToggleComponents(false);
            startButton.Text = "PROCESSING";

            var worker = new BackgroundWorker();
            worker.RunWorkerCompleted += Worker_StartConversionCompleted;
            worker.DoWork += Worker_StartConversionDoWork;

            worker.RunWorkerAsync(argument:filePath);
        }

        private void Worker_StartConversionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DialogResult warningMSG = MessageBox.Show(
                    $"ERROR\n\nWhile completing your selected task, FE-BUDDY came across the following issue:\n{e.Error.Message}\n\nThis could be due to a bug in the program, or a unexpected or incorrectly formatted item in the source file. (i.e. SCT2 File)\n\nPlease attempt to fix this issue and run the program again. If you continue to have an issue, please reach out the FE-BUDDY developers by reporting this issue and including a screenshot with the source file.\n\nhttps://github.com/Nikolai558/FE-BUDDY/issues",
                    "CAUTION",
                    MessageBoxButtons.OK);
            }

            startButton.Text = "Convert";
            ToggleComponents(true);
        }

        private void Worker_StartConversionDoWork(object sender, DoWorkEventArgs e)
        {
            double cropDistance;
            string filePath = (string)e.Argument;
            string outputFileName = "\\" + filePath.Split('\\')[^1].Split('.')[0];
            if (File.Exists(_conversionOptions.outputDirectory + outputFileName + ".sct2"))
            {
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                result = MessageBox.Show("This file exists in this directory, would you like to write over it?", "File Exists!", buttons);

                if (result == DialogResult.No)
                {
                    return;
                }
            }
            if (cropingDistanceTextBox.Text.Trim() == "")
            {
                cropDistance = -1;
            }
            else
            {
                cropDistance = double.Parse(cropingDistanceTextBox.Text);
            }

            if (sct2RadioButton.Checked)
            {
                Logger.LogMessage("INFO", "Starting DAT to SCT2 Conversion.");
                DatConversion.ReadDAT(filePath, _conversionOptions.outputDirectory + outputFileName, cropDistance, false);
            }
            else if (geojsonRadioButton.Checked)
            {
                Logger.LogMessage("INFO", "Starting DAT to GeoJson Conversion.");
                DatConversion.ReadDAT(filePath, _conversionOptions.outputDirectory + outputFileName, cropDistance, true);
            }
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
            newsToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);

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
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/development/ROADMAP.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/development/ROADMAP.md");
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
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/development/Credits.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/development/Credits.md");
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

        private void newsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "REPORT ISSUES MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/wiki#news") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/wiki#news");
        }

        private void cropingDistanceTextBox_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(cropingDistanceTextBox, "Type a numeric value here representing a distance from the \n.DAT file defined Point of Tangency that you want all lines drawn.\nLeave blank if you want the entire file drawn.\n\nExamples:\n106 = All lines within 106nm of the Point of Tangency will be drawn\nNothing = All lines will be drawn, regardless of distance");
        }
    }
}
