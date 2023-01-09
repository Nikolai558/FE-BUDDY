using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    public partial class LandingForm : Form
    {
        private readonly string _currentVersion;
        readonly PrivateFontCollection _pfc = new PrivateFontCollection();
        private ToolTip _toolTip;

        public LandingForm(string currentVersion)
        {
            Logger.LogMessage("DEBUG", "INITIALIZING COMPONENT");

            _toolTip = new ToolTip();
            _pfc.AddFontFile("Properties\\romantic.ttf");

            this.FormClosed += (s, args) => Application.Exit();

            _currentVersion = currentVersion;

            InitializeComponent();
            menuStrip.Renderer = new MyRenderer();


            // It should grab from the assembily info. 
            this.Text = $"FE-BUDDY - V{_currentVersion}";
            this.allowBetaMenuItem.Checked = Properties.Settings.Default.AllowPreRelease;
        }

        private class MyRenderer : ToolStripProfessionalRenderer
        {
            public MyRenderer() : base(new MyColors()) { }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
                r.Inflate(-4, -6);
                e.Graphics.DrawLines(new Pen(Color.Green, 2), new Point[]{
                new Point(r.Left, r.Bottom - r.Height /2),
                new Point(r.Left + r.Width /3,  r.Bottom),
                new Point(r.Right, r.Top)});

                // this is incharge of changing the checkbox apearance..... figure out how I want it to look and what I can do to get it there.
                // This is base render (default) leave this until our custom apearance can be "rendered"
                // base.OnRenderItemCheck(e);
            }
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
            public override Color CheckBackground
            {
                get { return Color.Black; }
            }
            public override Color MenuItemBorder
            {
                get { return Color.Gray; }
            }
            public override Color ToolStripBorder
            {
                get { return Color.Black; }
            }
            public override Color ToolStripDropDownBackground
            {
                get { return Color.Black; }
            }
        }

        private void LandingForm_Closing(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "Landing FORM CLOSING");
        }

        private void LandingForm_Shown(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "SHOWING Landing FORM");
        }

        private void LandingForm_Load(object sender, EventArgs e)
        {
            Logger.LogMessage("DEBUG", "LOADING Landing FORM");


            // TODO - Add fonts to buttons. 
            InstructionsMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            CreditsMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            ChangeLogMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            UninstallMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            FAQMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            RoadmapMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            informationToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            discordToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            settingsToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            reportIssuesToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            allowBetaMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
            newsToolStripMenuItem.Font = new Font(_pfc.Families[0], 12, FontStyle.Regular);
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

        private void landingStartButton_MouseHover(object sender, EventArgs e)
        {
            _toolTip.SetToolTip(landingStartButton, "I'm not your Buddy, Guy...");
        }

        private void landingStartButton_Click(object sender, EventArgs e)
        {
            // this.show() => should be this.close()... I don't like hitting the X button and it taking us back to the main menu
            // Need to do a button to handle that if it works PROPERLY... I had issues with that but maybe it is fixed or maybe I was doing it wrong before. 

            if (getparseAiracDataSelection.Checked)
            {
                var airacDataForm = new AiracDataForm(_currentVersion);
                airacDataForm.FormClosing += (s, args) => this.Show();
                airacDataForm.Show();
                this.Hide();
            }
            else if (convertSct2DxfSelection.Checked)
            {
                DialogResult warningMSG = MessageBox.Show(
                    "This feature is still a work-in-progress.\nWe have been able to get this to work with CAD programs in only very limited situations and we are asking for your help to see what works and what doesn't.\n\nIf you have a CAD program and want to assist us in troubleshooting, please use this feature and load the file in your CAD program.\n\nThen please join the FE-Buddy Discord, navigate to the #dxf-conversions channel and post:\n\n1) Successful or Not-Successful\n2) CAD program name\n3) Operating system\n4) Any further details such as error messages",
                    "CAUTION",
                    MessageBoxButtons.OK);


                var sctToDxfForm = new SctToDxfForm(_currentVersion);
                sctToDxfForm.FormClosing += (s, args) => this.Show();
                sctToDxfForm.Show();
                this.Hide();
            }
            else if (convertDat2SctSelection.Checked)
            {
                var datToSctForm = new DatToSctForm(_currentVersion);
                datToSctForm.FormClosing += (s, args) => this.Show();
                datToSctForm.Show();
                this.Hide();
            }
            else if (convertKml2SCTSelection.Checked)
            {
                DialogResult warningMSG = MessageBox.Show(
                    "This feature is still a work-in-progress.\n\nAs of right now only .SCT2 -> .KML conversions are possible.\n\nWe have been able to get this to work with some SCT2 files and we are asking for your help to see what works and what doesn't.\n\nIf your sector file does not work with this, then please join the FE-Buddy Discord, navigate to the #kml-conversions channel and post:\n\n1) Successful or Not-Successful\n2) Sector File\n3) Any further details such as error messages",
                    "CAUTION",
                    MessageBoxButtons.OK);
                var kmlConversionForm = new KmlConversionForm(_currentVersion);
                kmlConversionForm.FormClosing += (s, args) => this.Show();
                kmlConversionForm.Show();
                this.Hide();
            }
            else if (convertVstarsVeramToGeoJson.Checked)
            {
                DialogResult warningMSG = MessageBox.Show(
                    "Notes:\n\nOnly the “(facility ID) Video Maps.xml” or “(facility ID) GeoMaps.xml” files are legal source files. FEB does not read the main vSTARS/vERAM.gz files.\n\nIf your facility file has illegal/”Fake” longitudes, they will be converted back to legal (ex: -180.1 will be converted back to 179.9 and vice versa).\n\nLines that cross the Antimeridian line will be split at the Antimeridian line.\n\nvERAM:\nIf you have an item that uses an XSI: TYPE that is not defined by Defaults above the Elements array, that item will NOT be included in the GeoJSON output.\n\nTest the output files with a GeoJSON reader such as QGIS or https://geojson.io/ prior to uploading to vNAS Admin site.",
                    "INFO",
                    MessageBoxButtons.OK);

                var geojsonForm = new GeoJsonForm(_currentVersion);
                geojsonForm.FormClosing += (s, args) => this.Show();
                geojsonForm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("This feature has not been implemented yet.");
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
