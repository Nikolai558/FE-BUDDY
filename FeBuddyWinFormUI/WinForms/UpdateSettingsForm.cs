using System;
using System.Drawing;
using System.Windows.Forms;
using FeBuddy.Versioning;
using FeBuddyLibrary.Helpers;

namespace FeBuddyWinFormUI
{
    /// <summary>
    /// Lets the user pick a minimum update channel (see FeBuddy.Versioning.ReleaseChannel
    /// and FeBuddyLibrary.Update.UpdateChecker for what this actually controls).
    /// Replaces the old hidden/disabled "Dev Testing Mode" menu item - this is meant to
    /// actually be used, not something you need to be told by the developer to enable.
    /// </summary>
    public partial class UpdateSettingsForm : Form
    {
        // The native radio glyph reads poorly against this form's near-black background,
        // so the selected option is also highlighted by color - redundant, more reliable
        // signal than the dot alone.
        private static readonly Color SelectedColor = Color.FromArgb(255, 205, 110);
        private static readonly Color UnselectedColor = Color.Gainsboro;

        /// <summary>The channel selected when the dialog was closed with Save.</summary>
        public ReleaseChannel SelectedChannel { get; private set; }

        public UpdateSettingsForm()
        {
            InitializeComponent();

            foreach (var radio in AllChannelRadioButtons())
            {
                radio.CheckedChanged += (s, e) => UpdateSelectionHighlight();
            }

            SelectedChannel = ReadSavedChannel();
            SelectRadioFor(SelectedChannel);
            UpdateSelectionHighlight();
        }

        private RadioButton[] AllChannelRadioButtons() =>
            new[] { stableRadioButton, rcRadioButton, betaRadioButton, alphaRadioButton };

        private void UpdateSelectionHighlight()
        {
            foreach (var radio in AllChannelRadioButtons())
            {
                radio.ForeColor = radio.Checked ? SelectedColor : UnselectedColor;
            }
        }

        private static ReleaseChannel ReadSavedChannel()
        {
            var saved = Properties.Settings.Default.UpdateChannel;
            return Enum.TryParse<ReleaseChannel>(saved, out var channel) ? channel : ReleaseChannel.Stable;
        }

        private void SelectRadioFor(ReleaseChannel channel)
        {
            switch (channel)
            {
                case ReleaseChannel.Alpha:
                    alphaRadioButton.Checked = true;
                    break;
                case ReleaseChannel.Beta:
                    betaRadioButton.Checked = true;
                    break;
                case ReleaseChannel.ReleaseCandidate:
                    rcRadioButton.Checked = true;
                    break;
                case ReleaseChannel.Stable:
                default:
                    stableRadioButton.Checked = true;
                    break;
            }
        }

        private ReleaseChannel GetSelectedRadioChannel()
        {
            if (alphaRadioButton.Checked) return ReleaseChannel.Alpha;
            if (betaRadioButton.Checked) return ReleaseChannel.Beta;
            if (rcRadioButton.Checked) return ReleaseChannel.ReleaseCandidate;
            return ReleaseChannel.Stable;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SelectedChannel = GetSelectedRadioChannel();

            Properties.Settings.Default.UpdateChannel = SelectedChannel.ToString();
            Properties.Settings.Default.Save();

            Logger.LogMessage("INFO", $"Update channel set to {SelectedChannel}");
        }
    }
}
