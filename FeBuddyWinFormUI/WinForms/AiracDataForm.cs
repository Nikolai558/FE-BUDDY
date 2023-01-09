using System;
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
    public partial class AiracDataForm : Form
    {
        private bool nextAiracAvailable;
        private readonly string _currentVersion;
        private bool userClicked = false;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();


        public AiracDataForm(string currentVersion)
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");
            _pfc.AddFontFile("Properties\\romantic.ttf");

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();

            // It should grab from the assembily info. 
            this.Text = $"FE-BUDDY - V{currentVersion}";

            chooseDirButton.Enabled = false;
            startButton.Enabled = false;
            airacCycleGroupBox.Enabled = false;
            airacCycleGroupBox.Visible = false;

            convertGroupBox.Enabled = false;
            convertGroupBox.Visible = false;

            startGroupBox.Enabled = false;
            startGroupBox.Visible = false;

            processingGroupBox.Visible = true;
            processingGroupBox.Enabled = true;
            processingDataLabel.Visible = true;
            processingDataLabel.Enabled = true;

            facilityIdCombobox.DataSource = GlobalConfig.allArtcc;

            GlobalConfig.outputDirBase = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            filePathLabel.Text = GlobalConfig.outputDirBase;
            filePathLabel.Visible = true;
            filePathLabel.MaximumSize = new Size(257, 82);
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

        private void AiracDataForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "MAIN FORM CLOSING");
        }

        private void CurrentAiracSelection_CheckedChanged(object sender, EventArgs e)
        {
            currentAiracSelection.Text = GlobalConfig.currentAiracDate;
            nextAiracSelection.Text = GlobalConfig.nextAiracDate;
        }

        private void NextAiracSelection_CheckedChanged(object sender, EventArgs e)
        {
            currentAiracSelection.Text = GlobalConfig.currentAiracDate;
            nextAiracSelection.Text = GlobalConfig.nextAiracDate;
        }

        private void ChooseDirButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "USER CHOOSING DIFFERENT OUTPUT DIRECTORY");

            FolderBrowserDialog outputDir = new FolderBrowserDialog();

            outputDir.ShowDialog();

            GlobalConfig.outputDirBase = outputDir.SelectedPath;

            filePathLabel.Text = GlobalConfig.outputDirBase;
            filePathLabel.Visible = true;
            filePathLabel.MaximumSize = new Size(257, 82);
        }

        private void AiracDataStartButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "USER CLICKED START BUTTON");

            if (GlobalConfig.outputDirBase == null || GlobalConfig.outputDirBase == "")
            {
                Logger.LogMessage("WARNING", "OUTPUT DIRECTORY BASE IS NULL OR EMPTY");

                DialogResult dialogResult = MessageBox.Show("Seems there may be an error.\n Please verify you have chosen an output location.", "ERROR: NO Output Location", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    return;
                }
                else
                {
                    return;
                }
            }

            if (GlobalConfig.facilityID == "" || GlobalConfig.facilityID.Trim() == null)
            {
                Logger.LogMessage("WARNING", "FACILITY ID IS NULL OR EMPTY");

                DialogResult dialogResult = MessageBox.Show("Seems there may be an error.\n Please verify you have selected a correct Facility ID.", "ERROR: NO Facility ID", MessageBoxButtons.OK);
                if (dialogResult == DialogResult.OK)
                {
                    return;
                }
                else
                {
                    return;
                }
            }

            if (GlobalConfig.outputDirectory == null)
            {
                Logger.LogMessage("DEBUG", "SETTING OUTPUT DIRECTORY FULL FILE PATH FROM NULL");

                GlobalConfig.outputDirectory = $"{GlobalConfig.outputDirBase}\\FE-BUDDY_Output";

                if (Directory.Exists(GlobalConfig.outputDirectory))
                {
                    Logger.LogMessage("DEBUG", "OUTPUT DIRECTORY FILE PATH EXISTS, ADDING DATETIME VARIABLE TO END OF DIRECTORY NAME");

                    GlobalConfig.outputDirectory += $"-{DateTime.Now:MMddHHmmss}";
                }

                GlobalConfig.outputDirectory += "\\";
            }
            else
            {
                Logger.LogMessage("DEBUG", "SETTING OUTPUT DIRECTORY FULL FILE PATH FROM EXISTING");

                GlobalConfig.outputDirectory = $"{GlobalConfig.outputDirBase}\\FE-BUDDY_Output";

                if (Directory.Exists(GlobalConfig.outputDirectory))
                {
                    Logger.LogMessage("DEBUG", "OUTPUT DIRECTORY FILE PATH EXISTS, ADDING DATETIME VARIABLE TO END OF DIRECTORY NAME");
                    GlobalConfig.outputDirectory += $"-{DateTime.Now:MMddHHmmss}";
                }

                GlobalConfig.outputDirectory += "\\";
            }

            DirectoryHelpers.CreateDirectories();

            FileHelpers.WriteTestSctFile();

            menuStrip.Visible = false;
            chooseDirButton.Enabled = false;
            //startButton.Enabled = false;


            if (convertYes.Checked)
            {
                Logger.LogMessage("DEBUG", "CONVERT COORDS 'YES' SELECTED");

                GlobalConfig.Convert = true;
            }
            else if (convertNo.Checked)
            {
                Logger.LogMessage("DEBUG", "CONVERT COORDS 'NO' SELECTED");

                GlobalConfig.Convert = false;
            }

            airacCycleGroupBox.Enabled = false;
            airacCycleGroupBox.Visible = false;

            convertGroupBox.Enabled = false;
            convertGroupBox.Visible = false;

            startGroupBox.Enabled = false;
            startGroupBox.Visible = false;

            //TODO - Create Processing box instead of already having it. 

            processingGroupBox.Visible = true;
            processingGroupBox.Enabled = true;
            processingDataLabel.Visible = true;
            processingDataLabel.Enabled = true;

            StartParsing();
        }

        private void AiracDataExitButton_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("INFO", "EXIT BUTTON CLICKED");
            this.Close();
            //this.Hide();
            //Application.Exit();
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

        private void StartParsing()
        {
            Logger.LogMessage("INFO", "SETTING UP PARSING WORKER");

            AdjustProcessingBox();

            var worker = new BackgroundWorker();
            worker.RunWorkerCompleted += Worker_StartParsingCompleted;
            worker.DoWork += Worker_StartParsingDoWork;

            worker.RunWorkerAsync();
        }

        private void AdjustProcessingBox()
        {
            Logger.LogMessage("DEBUG", "ADJUSTING PROCESSING BOX");

            outputDirectoryLabel.Text = GlobalConfig.outputDirectory;
            outputDirectoryLabel.Visible = true;
            outputLocationLabel.Visible = true;

            processingGroupBox.Location = new Point(114, 59);
            processingGroupBox.Size = new Size(557, 213);

            outputLocationLabel.Location = new Point(9, 22);
            outputDirectoryLabel.Location = new Point(24, 47);
            processingDataLabel.Location = new Point(6, 102);
            exitButton.Location = new Point(187, 173);
        }

        private void Worker_StartParsingDoWork(object sender, DoWorkEventArgs e)
        {
            Logger.LogMessage("INFO", "PROCESSING STARTED");

            DirectoryHelpers.CheckTempDir();

            if (currentAiracSelection.Checked)
            {
                Logger.LogMessage("DEBUG", "CURRENT AIRAC IS SELECTED");

                GlobalConfig.airacEffectiveDate = currentAiracSelection.Text;
            }
            else if (nextAiracSelection.Checked)
            {
                Logger.LogMessage("DEBUG", "NEXT AIRAC IS SELECTED");

                GlobalConfig.airacEffectiveDate = nextAiracSelection.Text;
            }

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Downloading FAA Data");
            DownloadHelpers.DownloadAllFiles(GlobalConfig.airacEffectiveDate, AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]);

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Unzipping Files");
            DirectoryHelpers.UnzipAllDownloaded();

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Telephony");
            GetTelephony Telephony = new GetTelephony();
            Telephony.readFAAData($"{GlobalConfig.tempPath}\\{AiracDateCycleModel.AllCycleDates[GlobalConfig.airacEffectiveDate]}_TELEPHONY.html");

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing DPs and STARs");
            GetStarDpData ParseStarDp = new GetStarDpData();
            ParseStarDp.StarDpQuaterBackFunc(GlobalConfig.airacEffectiveDate);

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Airports");
            GetAptData ParseAPT = new GetAptData();
            ParseAPT.AptAndWxMain(GlobalConfig.airacEffectiveDate, GlobalConfig.facilityID);

            if (currentAiracSelection.Checked == true)
            {
                Logger.LogMessage("DEBUG", "CURRENT AIRAC SELECTED");
                SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Chart Recalls");
                GetFaaMetaFileData ParseMeta = new GetFaaMetaFileData();
                ParseMeta.QuarterbackFunc();

                SetControlPropertyThreadSafe(processingDataLabel, "Text", "Getting Publications");
                PublicationParser publications = new PublicationParser();
                publications.WriteAirportInfoTxt(GlobalConfig.facilityID);
            }
            else if (nextAiracSelection.Checked == true && nextAiracAvailable == true)
            {
                Logger.LogMessage("DEBUG", "NEXT AIRAC IS SELECTED, and it is available");
                SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Chart Recalls");
                GetFaaMetaFileData ParseMeta = new GetFaaMetaFileData();
                ParseMeta.QuarterbackFunc();

                SetControlPropertyThreadSafe(processingDataLabel, "Text", "Getting Publications");
                PublicationParser publications = new PublicationParser();
                publications.WriteAirportInfoTxt(GlobalConfig.facilityID);
            }

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Fixes");
            GetFixData ParseFixes = new GetFixData();
            ParseFixes.FixQuarterbackFunc(GlobalConfig.airacEffectiveDate);

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Boundaries");
            GetArbData ParseArb = new GetArbData();
            ParseArb.ArbMain(GlobalConfig.airacEffectiveDate);

            // ------------------------------------------------------------------------------------------------------
            // DO NOT CHANGE THE ORDER OF THESE TWO ITEMS, REFACTORING THIS IS NEEDED!!!!!
            // Warning: AwyData must be preformed before AtsAwyData, Refactoring of this code is needed! 
            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Airways");
            FileHelpers.CreateAwyGeomapHeadersAndEnding(true);

            GetAwyData ParseAWY = new GetAwyData();
            ParseAWY.AWYQuarterbackFunc(GlobalConfig.airacEffectiveDate);

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing ATS Airways");
            GetAtsAwyData ParseAts = new GetAtsAwyData();
            ParseAts.AWYQuarterbackFunc(GlobalConfig.airacEffectiveDate);
            FileHelpers.CreateAwyGeomapHeadersAndEnding(false);

            // ------------------------------------------------------------------------------------------------------


            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing NDBs");
            GetNavData ParseNDBs = new GetNavData();
            ParseNDBs.NAVQuarterbackFunc(GlobalConfig.airacEffectiveDate, GlobalConfig.facilityID);

            //SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing FAA Aircraft Data");
            //AircraftData ACData = new AircraftData();
            //ACData.CreateAircraftDataAlias($"{GlobalConfig.outputDirectory}\\ALIAS\\AircraftDataInfo.txt");

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Processing Waypoints XML");
            FileHelpers.WriteWaypointsXML();
            FileHelpers.AppendCommentToXML(GlobalConfig.airacEffectiveDate);
            FileHelpers.WriteNavXmlOutput();

            SetControlPropertyThreadSafe(processingDataLabel, "Text", "Checking Alias Commands");
            AliasCheck aliasCheck = new AliasCheck();
            aliasCheck.CheckForDuplicates($"{GlobalConfig.outputDirectory}\\ALIAS\\AliasTestFile.txt");
        }

        private void Worker_StartParsingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DialogResult warningMSG = MessageBox.Show(
                    $"ERROR\n\nWhile completing your selected task, FE-BUDDY came across the following issue:\n{e.Error.Message}\n\nThis could be due to a bug in the program, or a unexpected or incorrectly formatted item in the source file. (i.e. SCT2 File)\n\nPlease attempt to fix this issue and run the program again. If you continue to have an issue, please reach out the FE-BUDDY developers by reporting this issue and including a screenshot with the source file.\n\nhttps://github.com/Nikolai558/FE-BUDDY/issues",
                    "CAUTION",
                    MessageBoxButtons.OK);
            }

            Logger.LogMessage("INFO", "PROCESSING COMPLETED");
            File.Copy(Logger._logFilePath, $"{GlobalConfig.outputDirectory}\\FE-BUDDY_LOG.txt");

            processingDataLabel.Text = "Complete";
            processingDataLabel.Refresh();

            processingGroupBox.Visible = true;
            processingGroupBox.Enabled = true;

            menuStrip.Visible = true;

            exitButton.Visible = true;
            exitButton.Enabled = true;
        }

        private void GetAiracDate()
        {
            Logger.LogMessage("DEBUG", "SETTING UP AIRAC DATE WORKER");

            var Worker = new BackgroundWorker();
            Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            Worker.DoWork += Worker_DoWork;

            Worker.RunWorkerAsync();
        }

        private void AiracDataForm_Shown(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "SHOWING MAIN FORM");

            GetAiracDate();
            currentAiracSelection.Text = GlobalConfig.currentAiracDate;
            nextAiracSelection.Text = GlobalConfig.nextAiracDate;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Logger.LogMessage("DEBUG", "GETTING AIRAC DATE");

            if (GlobalConfig.nextAiracDate == null)
            {
                WebHelpers.GetAiracDateFromFAA();
            }
            nextAiracAvailable = WebHelpers.GetMetaUrlResponse();
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DialogResult warningMSG = MessageBox.Show(
                    $"ERROR\n\nWhile completing your selected task, FE-BUDDY came across the following issue:\n{e.Error.Message}\n\nThis could be due to a bug in the program, or a unexpected or incorrectly formatted item in the source file. (i.e. SCT2 File)\n\nPlease attempt to fix this issue and run the program again. If you continue to have an issue, please reach out the FE-BUDDY developers by reporting this issue and including a screenshot with the source file.\n\nhttps://github.com/Nikolai558/FE-BUDDY/issues",
                    "CAUTION",
                    MessageBoxButtons.OK);
            }

            currentAiracSelection.Text = GlobalConfig.currentAiracDate;
            nextAiracSelection.Text = GlobalConfig.nextAiracDate;

            processingGroupBox.Visible = false;
            processingGroupBox.Enabled = false;

            exitButton.Visible = false;
            exitButton.Enabled = false;

            processingDataLabel.Text = "Processing Data, Please Wait.";

            processingDataLabel.Visible = false;
            processingDataLabel.Enabled = false;

            chooseDirButton.Enabled = true;
            startButton.Enabled = true;

            airacCycleGroupBox.Enabled = true;
            airacCycleGroupBox.Visible = true;

            convertGroupBox.Enabled = true;
            convertGroupBox.Visible = true;

            startGroupBox.Enabled = true;
            startGroupBox.Visible = true;

            facilityIdCombobox.SelectedIndex = Properties.Settings.Default.UserArtccSetting;

            Logger.LogMessage("DEBUG", "AIRAC DATE WORKER COMPLETED");
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

        private void NextAiracSelection_Click(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "NEXT AIRAC SELECTED");
            if (!nextAiracAvailable)
            {
                Logger.LogMessage("DEBUG", "NEXT AIRAC SELECTED, NOT AVAILABLE YET");
                MetaNotFoundForm frm = new MetaNotFoundForm();
                frm.ShowDialog();
            }
        }

        private void FacilityIdCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "FACILITY COMBOBOX CLICKED");
            GlobalConfig.facilityID = facilityIdCombobox.SelectedItem.ToString();

            if (userClicked)
            {
                Properties.Settings.Default.UserArtccSetting = facilityIdCombobox.SelectedIndex;
                Properties.Settings.Default.Save();
            }
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

        private void facilityIdCombobox_Click(object sender, EventArgs e)
        {
            userClicked = true;
        }
    }
}
