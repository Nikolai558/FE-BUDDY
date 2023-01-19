using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FeBuddyLibrary;
using FeBuddyLibrary.DataAccess;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;

namespace FeBuddyWinFormUI
{
    public partial class GeoJsonForm : Form
    {
        private readonly string _currentVersion;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();
        private string fullSourceFilePath;
        private string videoMapFolderName;
        private VideoMapFileFormat videoMapFileFormat = VideoMapFileFormat.shortName;
        private ToolTip _toolTip;


        public GeoJsonForm(string currentVersion)
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");
            _pfc.AddFontFile("Properties\\romantic.ttf");

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();
            
            _toolTip = new ToolTip();

            // It should grab from the assembily info. 
            this.Text = $"FE-BUDDY - V{currentVersion}";

            GlobalConfig.outputDirBase = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            GlobalConfig.outputDirBase = Path.Combine(GlobalConfig.outputDirBase, "FE-BUDDY-GeoJSONs");

            outputPathLabel.Text = GlobalConfig.outputDirBase;
            outputPathLabel.Visible = true;
            outputPathLabel.MaximumSize = new Size(257, 82);
            _currentVersion = currentVersion;
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

        private void GeojsonForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "GeoJson Form CLOSING");
        }

        private void sourceFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog sourceFilePath = new OpenFileDialog();
            sourceFilePath.Filter = "xml files (*.xml)|*.xml";
            //sourceFilePath.RestoreDirectory = true;

            if (vStarsSelection.Checked)
            {
                sourceFilePath.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%userprofile%\AppData\Roaming\vSTARS\Video Maps\");
            }
            else if (vEramSelection.Checked)
            {
                sourceFilePath.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%userprofile%\AppData\Local\vERAM\GeoMaps\");
            }
            else
            {
                // I dont think this will ever be possible but just in case. 
                sourceFilePath.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%userprofile%\Downloads");
            }

            sourceFilePath.ShowDialog();
            fullSourceFilePath = sourceFilePath.FileName;

            sourceFileLabel.Text = fullSourceFilePath;

            if (fullSourceFilePath.Length >= 20)
            {
                if (fullSourceFilePath[^17..].Contains('\\'))
                {
                    sourceFileLabel.Text = "..\\" + fullSourceFilePath[^17..].Split('\\')[^1];
                }
                else
                {
                    sourceFileLabel.Text = "..\\.." + fullSourceFilePath[^15..];
                }
            }

            sourceFileLabel.Visible = true;
            sourceFileLabel.MaximumSize = new Size(257, 82);
        }

        private void ChooseDirButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "USER CHOOSING DIFFERENT OUTPUT DIRECTORY");
            FolderBrowserDialog outputDir = new FolderBrowserDialog();

            outputDir.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%userprofile%\Downloads");

            outputDir.ShowDialog();

            //fullSourceFilePath = Path.Combine(fullSourceFilePath, "FE-BUDDY-GeoJSONs");

            GlobalConfig.outputDirBase = Path.Combine(outputDir.SelectedPath, "FE-BUDDY-GeoJSONs");

            if (GlobalConfig.outputDirBase == "FE-BUDDY-GeoJSONs")
            {
                GlobalConfig.outputDirBase = "";
            }

            outputPathLabel.Text = GlobalConfig.outputDirBase;

            if (GlobalConfig.outputDirBase.Length >= 20)
            {
                if (GlobalConfig.outputDirBase[^17..].Contains('\\'))
                {
                    outputPathLabel.Text = "..\\" + GlobalConfig.outputDirBase[^17..].Split('\\')[^2];
                }
                else
                {
                    outputPathLabel.Text = "..\\.." + GlobalConfig.outputDirBase[^15..];
                }
            }

            outputPathLabel.Visible = true;
            outputPathLabel.MaximumSize = new Size(257, 82);
        }
        private void vEramSelection_CheckedChanged(object sender, EventArgs e)
        {

            fileOutputFormatLabel.Text = "vERAM Output File Name Format:";

            //convertGroupBox.Enabled = false;
            shortNameSelection.Enabled = false;
            longNameSelection.Enabled = false;
            bothSelection.Enabled = false;
            shortNameSelection.Visible = false;
            longNameSelection.Visible = false;
            bothSelection.Visible = false;

            seperateGeoJsonOutputButton.Select();
            seperateGeoJsonOutputButton.Enabled = true;
            seperateGeoJsonOutputButton.Visible = true;
            // TODO - Once functionality has been done Be sure to uncomment the line below.
            combineLikeGeoMapObjButton.Enabled = true;
            combineLikeGeoMapObjButton.Visible = true;
        }

        private void vStarsSelection_CheckedChanged(object sender, EventArgs e)
        {
            //convertGroupBox.Enabled = true;
            fileOutputFormatLabel.Text = "vSTARS Output File Name Format";

            shortNameSelection.Select();
            shortNameSelection.Enabled = true;
            longNameSelection.Enabled = true;
            bothSelection.Enabled = true;
            shortNameSelection.Visible = true;
            longNameSelection.Visible = true;
            bothSelection.Visible = true;

            seperateGeoJsonOutputButton.Enabled = false;
            seperateGeoJsonOutputButton.Visible = false;
            combineLikeGeoMapObjButton.Enabled = false;
            combineLikeGeoMapObjButton.Visible = false;
        }

        private void shortNameSelection_CheckedChanged(object sender, EventArgs e)
        {
            videoMapFileFormat = VideoMapFileFormat.shortName;
        }

        private void longNameSelection_CheckedChanged(object sender, EventArgs e)
        {
            videoMapFileFormat = VideoMapFileFormat.longName;
        }

        private void bothSelection_CheckedChanged(object sender, EventArgs e)
        {
            videoMapFileFormat = VideoMapFileFormat.both;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(fullSourceFilePath) || fullSourceFilePath.Split('.')[^1] != "xml")
            {
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                result = MessageBox.Show("Source File Path is Empty", "An invalid operation occured.", buttons);
                return;
            }

            if (string.IsNullOrEmpty(GlobalConfig.outputDirBase))
            {
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                result = MessageBox.Show("Output Directory is Empty", "An invalid operation occured.", buttons);
                return;
            }

            if (!File.Exists(fullSourceFilePath))
            {
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                result = MessageBox.Show("Source File does not exist.", "An invalid operation occured.", buttons);
                return;
            }

            StartConversion();
        }
        
        private void EnableButtons(bool isEnabled)
        {
            vStarsSelection.Enabled = isEnabled;
            vEramSelection.Enabled = isEnabled;
            shortNameSelection.Enabled = isEnabled;
            longNameSelection.Enabled = isEnabled;
            bothSelection.Enabled = isEnabled;
            sourceFileButton.Enabled = isEnabled;
            chooseDirButton.Enabled = isEnabled;
            startButton.Enabled = isEnabled;
        }

        private void StartConversion()
        {
            EnableButtons(false);

            Logger.LogMessage("INFO", "SETTING UP Conversion WORKER");

            var worker = new BackgroundWorker();
            worker.RunWorkerCompleted += Worker_StartParsingCompleted;
            worker.DoWork += Worker_StartConversionDoWork;

            worker.RunWorkerAsync();
        }

        private void Worker_StartConversionDoWork(object sender, DoWorkEventArgs e)
        {
            GeoJson geoJsonConverter = new GeoJson();

            if (vStarsSelection.Checked)
            {
                string videoMapName = fullSourceFilePath.Split('\\')[^1].Replace(".xml", string.Empty);
                var geo = geoJsonConverter.ReadVideoMap(fullSourceFilePath);

                List<string> unknownAsdexColors = geoJsonConverter.ValidateAsdexProperties(geo)["UNKNOWN"];
                if (unknownAsdexColors.Count > 0)
                {
                    AsdexColorErrorsForm asdexErrorForm = new AsdexColorErrorsForm(unknownAsdexColors, geoJsonConverter);
                    asdexErrorForm.ShowDialog();
                    //Show new form with correcting options
                    //apply users choices to that dictionary.

                }

                geoJsonConverter.WriteVideoMapGeoJson(GlobalConfig.outputDirBase, geo, videoMapName, videoMapFileFormat);
            }
            else if (vEramSelection.Checked)
            {
                var geo = geoJsonConverter.ReadGeoMap(fullSourceFilePath);

                if (seperateGeoJsonOutputButton.Checked)
                {
                    geoJsonConverter.WriteGeoMapGeoJson(GlobalConfig.outputDirBase, geo);
                }
                else if (combineLikeGeoMapObjButton.Checked)
                {
                    geoJsonConverter.WriteCombinedGeoMapGeoJson(GlobalConfig.outputDirBase, geo);
                }
            }

            // geoJsonConverter.PostProcess(GlobalConfig.outputDirBase);
            // Do Post processing of geojson here (combine features in feature collection if the features have lat/lon that should be combined.
            // this is to handle for reversed start lat/lon and end lat/lon elements inside the veram xml.
        }

        private void Worker_StartParsingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DialogResult warningMSG = MessageBox.Show(
                    $"ERROR\n\nWhile completing your selected task, FE-BUDDY came across the following issue:\n{e.Error.Message}\n\nThis could be due to a bug in the program, or a unexpected or incorrectly formatted item in the source file.\n\nPlease attempt to fix this issue and run the program again. If you continue to have an issue, please reach out the FE-BUDDY developers by reporting this issue and including a screenshot with the source file.\n\nhttps://github.com/Nikolai558/FE-BUDDY/issues",
                    "CAUTION",
                    MessageBoxButtons.OK);
            }

            EnableButtons(true);

            Logger.LogMessage("INFO", "PROCESSING COMPLETED");
            //File.Copy(Logger._logFilePath, $"{GlobalConfig.outputDirectory}\\FE-BUDDY_LOG.txt");
        }

        private void geoJsonForm_Shown(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "SHOWING MAIN FORM");
        }

        private void GeoJsonForm_Load(object sender, EventArgs e)
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

        private void newsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "REPORT ISSUES MENU ITEM CLICKED");
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/wiki#news") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/wiki#news");
        }

        private void seperateGeoJsonOutputButton_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(seperateGeoJsonOutputButton, "Example:\n\n3nm RINGS.geojson\r\nAIRPORT TEXT.geojson\r\nALL ZLC SECTORS.geojson\r\nAPPROACH CONTROL.geojson");

        }

        private void combineLikeGeoMapObjButton_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(combineLikeGeoMapObjButton, "Example:\n\nFILTER 13___LINES___TDM F___BCG 11___Style Solid___Thickness 1.geojson\r\nFILTER 08___TEXT___TDM F___BCG 8___Size 1___Underline false___Opaque true___XOffset 0___YOffset 0.geojson");
        }
    }
}
