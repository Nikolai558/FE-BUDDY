namespace FeBuddyLibrary.Update
{
    /// <summary>
    /// A single eligible update, chosen by UpdateChecker as the best available
    /// release for the caller's installed version and channel preference.
    /// </summary>
    public class UpdateCandidate
    {
        /// <summary>The real semantic version of the candidate release, e.g. "2.9.0-beta.1".</summary>
        public string Version { get; set; }

        /// <summary>The GitHub release's display name (may differ from the tag/version).</summary>
        public string ReleaseName { get; set; }

        /// <summary>The GitHub release's body/notes (Markdown), for display to the user.</summary>
        public string ReleaseNotes { get; set; }

        /// <summary>Direct download URL for the release's .msi asset.</summary>
        public string MsiDownloadUrl { get; set; }

        /// <summary>File name of the .msi asset, e.g. "FE-BUDDY-2.9.0-beta.1.msi".</summary>
        public string MsiFileName { get; set; }

        /// <summary>Size of the .msi asset in bytes, if known (0 if GitHub didn't report it).</summary>
        public long MsiSizeBytes { get; set; }
    }
}
