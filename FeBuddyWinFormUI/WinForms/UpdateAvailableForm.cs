using System;
using System.ComponentModel;
using System.Windows.Forms;
using FeBuddyLibrary.Helpers;
using FeBuddyLibrary.Update;

namespace FeBuddyWinFormUI
{
    /// <summary>
    /// MSI-path equivalent of the old Squirrel-era UpdateForm - built fresh rather than
    /// reusing it, since that one is tightly coupled to Squirrel-specific mechanics
    /// (hardcoded ChangeLog.md fetch, no download-progress concept). Flow: show what's
    /// available and ask -> on Yes, download with progress -> launch the MSI's own
    /// interactive installer, elevated -> exit immediately (does not wait for the
    /// installer or attempt anything silent/background - see UpdateInstaller for why).
    /// </summary>
    public partial class UpdateAvailableForm : Form
    {
        private readonly UpdateCandidate _candidate;

        /// <param name="headerText">
        /// Defaults to the normal "update available" wording. The revert-to-stable action
        /// passes its own wording instead - seeing "UPDATE AVAILABLE" while reverting to a
        /// numerically older version reads as confusing/wrong even though it's correct.
        /// </param>
        /// <param name="currentVersionText">
        /// Overrides the "Your program version: X" line. The Squirrel->MSI migration passes
        /// its own wording here - a raw version number would be misleading when what's
        /// actually changing is the install mechanism, not (necessarily) the version.
        /// </param>
        /// <param name="newVersionText">Overrides the "New version available: X" line, for the same reason.</param>
        public UpdateAvailableForm(
            string currentVersion,
            UpdateCandidate candidate,
            string headerText = "*** UPDATE AVAILABLE ***",
            string questionText = "Download and install this update now?",
            string currentVersionText = null,
            string newVersionText = null)
        {
            InitializeComponent();

            _candidate = candidate;

            headerLabel.Text = headerText;
            questionLabel.Text = questionText;
            currentVersionLabel.Text = currentVersionText ?? $"Your program version: {currentVersion}";
            newVersionLabel.Text = newVersionText ?? $"New version available: {candidate.Version}";
            releaseNotesLabel.Text = string.IsNullOrWhiteSpace(candidate.ReleaseNotes)
                ? "(No release notes provided.)"
                : candidate.ReleaseNotes;
        }

        private async void YesButton_Click(object sender, EventArgs e)
        {
            questionPanel.Visible = false;
            downloadPanel.Visible = true;

            var progress = new Progress<DownloadProgressInfo>(ReportDownloadProgress);

            try
            {
                var destinationPath = await UpdateInstaller
                    .DownloadAsync(_candidate, FeBuddyLibrary.GlobalConfig.tempPath, progress)
                    .ConfigureAwait(true);

                downloadingLabel.Text = "Starting installer...";

                UpdateInstaller.LaunchInstaller(destinationPath);

                // The MSI can't safely replace this process's own files while it's still
                // running - exit immediately rather than lingering. Nothing after this
                // point runs.
                Environment.Exit(0);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // User declined the UAC elevation prompt - a cancellation, not a failure.
                Logger.LogMessage("INFO", "Update install cancelled by user at the elevation prompt.");
                MessageBox.Show(
                    "Update cancelled - administrator permission is required to install updates.",
                    "Update Cancelled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ReturnToQuestionState();
            }
            catch (Exception ex)
            {
                Logger.LogMessage("WARNING", "Unable to download/launch update: " + ex.Message);
                MessageBox.Show(
                    "FE-BUDDY could not download or start the update.\n\n" + ex,
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ReturnToQuestionState();
            }
        }

        private void ReturnToQuestionState()
        {
            downloadPanel.Visible = false;
            questionPanel.Visible = true;
            downloadProgressBar.Style = ProgressBarStyle.Blocks;
            downloadProgressBar.Value = 0;
            downloadingLabel.Text = "Downloading Update...";
        }

        private void ReportDownloadProgress(DownloadProgressInfo info)
        {
            if (info.PercentComplete is int percent)
            {
                downloadProgressBar.Style = ProgressBarStyle.Blocks;
                downloadProgressBar.Value = Math.Clamp(percent, 0, 100);
                downloadStatusLabel.Text = $"{percent}%  ({FormatBytes(info.BytesReceived)} / {FormatBytes(info.TotalBytes!.Value)})";
            }
            else
            {
                // Server didn't report a size - show an indeterminate bar rather than a
                // meaningless/stuck percentage.
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                downloadStatusLabel.Text = FormatBytes(info.BytesReceived) + " downloaded";
            }
        }

        private static string FormatBytes(long bytes)
        {
            const double mb = 1024 * 1024;
            return bytes >= mb
                ? $"{bytes / mb:0.0} MB"
                : $"{bytes / 1024.0:0} KB";
        }
    }
}
