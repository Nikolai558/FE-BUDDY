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
using FeBuddyLibrary.Dxf.Data;
using FeBuddyLibrary.Dxf.Models;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Update;
using FeBuddyLibrary.Models;
using FeBuddyLibrary.Models.MetaFileModels;

namespace FeBuddyWinFormUI
{
    public partial class SctToGeojsonForm : Form
    {
        private readonly string _currentVersion;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();
        private string fullSourceFilePath;

        public SctToGeojsonForm(string currentVersion)
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");
            _pfc.AddFontFile("Properties\\romantic.ttf");

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();

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
            sourceFilePath.Filter = "SCT2 files (*.sct2)|*.sct2|SCT files (*.sct)|*.sct|All files (*.*)|*.*";
            //sourceFilePath.RestoreDirectory = true;

            sourceFilePath.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%userprofile%\Downloads");

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

        private void StartButton_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(fullSourceFilePath) || (fullSourceFilePath.Split('.')[^1].ToLower() != "sct2" && fullSourceFilePath.Split('.')[^1].ToLower() != "sct"))
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

            SctFileModel sctModel = geoJsonConverter.ReadSctFile(fullSourceFilePath);

            geoJsonConverter.WriteSctGeoJson(GlobalConfig.outputDirBase, sctModel, Path.GetFileNameWithoutExtension(fullSourceFilePath));
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
            if (dialogResult != DialogResult.Yes)
            {
                return;
            }

            Logger.LogMessage("WARNING", "CONFIRMATION USER WANTS TO UNINSTALL");
            AppUninstaller.Uninstall();
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
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/development/docs/ROADMAP.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/development/docs/ROADMAP.md");
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
            Process.Start(new ProcessStartInfo("https://github.com/Nikolai558/FE-BUDDY/blob/development/docs/Credits.md") { UseShellExecute = true });
            //Process.Start("https://github.com/Nikolai558/FE-BUDDY/blob/development/docs/Credits.md");
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

    }
}
