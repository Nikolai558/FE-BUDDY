using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the modal update window (remediation plan 4.2): shows the running version, the
/// latest version on the user's channel, and the notes for every release in between, newest
/// first. FE-Buddy 3.0 ships as an MSI, so "update" means <b>download the new installer</b> -
/// the button opens the release page where the MSI lives; <b>Later</b> dismisses (and colours
/// the version text amber for the session).
/// </summary>
public sealed class UpdateWindowViewModel : ObservableObject
{
    private const string ReleasesPage = "https://github.com/Nikolai558/FE-BUDDY/releases/latest";

    /// <summary>Creates the view-model from a completed version check that found an update.</summary>
    /// <param name="version">The version-check result. <see cref="VersionCheckResult.UpdateAvailable"/> is expected to be true.</param>
    public UpdateWindowViewModel(VersionCheckResult version)
    {
        ArgumentNullException.ThrowIfNull(version);

        CurrentVersion = string.IsNullOrWhiteSpace(version.CurrentVersion) ? "dev" : version.CurrentVersion.TrimStart('v', 'V');
        LatestVersion = version.LatestVersion ?? "unknown";
        Channel = version.Channel.ToString();
        ReleaseUrl = string.IsNullOrWhiteSpace(version.LatestReleaseUrl) ? ReleasesPage : version.LatestReleaseUrl!;

        IReadOnlyList<ReleaseSummary> releases = version.NewerReleases.Count > 0
            ? version.NewerReleases
            : [new ReleaseSummary(LatestVersion, null, false, null, ReleaseUrl)];

        Releases = releases.Select((release, index) => new ReleaseNotesItem(release, isFirst: index == 0)).ToList();
        ReleasesBehind = Releases.Count > 1 ? $"{Releases.Count} releases since v{CurrentVersion}" : null;

        DownloadCommand = new RelayCommand(() =>
        {
            BrowserLauncher.Open(ReleaseUrl);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        });

        LaterCommand = new RelayCommand(() =>
        {
            UserDeclined = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>Raised when the window should close.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The running application version.</summary>
    public string CurrentVersion { get; }

    /// <summary>The newest version available on <see cref="Channel"/>.</summary>
    public string LatestVersion { get; }

    /// <summary>The update channel that was checked.</summary>
    public string Channel { get; }

    /// <summary>One entry per release newer than <see cref="CurrentVersion"/>, newest first.</summary>
    public IReadOnlyList<ReleaseNotesItem> Releases { get; }

    /// <summary>"3 releases since v2.8.1" when the user is more than one release behind; otherwise <see langword="null"/>.</summary>
    public string? ReleasesBehind { get; }

    /// <summary>The release page URL (where the MSI download lives).</summary>
    public string ReleaseUrl { get; }

    /// <summary><see langword="true"/> once the user dismisses without downloading - the shell then colours the version text as a warning for the session.</summary>
    public bool UserDeclined { get; private set; }

    /// <summary>Opens the release page (the MSI download) in the browser and closes the window.</summary>
    public ICommand DownloadCommand { get; }

    /// <summary>Dismisses the window without downloading.</summary>
    public ICommand LaterCommand { get; }
}

/// <summary>One release's section in the update window: its version header and its notes.</summary>
public sealed class ReleaseNotesItem
{
    /// <summary>Creates the section for <paramref name="release"/>.</summary>
    /// <param name="release">The release.</param>
    /// <param name="isFirst"><see langword="true"/> for the top section, which has no divider above it.</param>
    public ReleaseNotesItem(ReleaseSummary release, bool isFirst)
    {
        ArgumentNullException.ThrowIfNull(release);

        Version = "v" + release.Version;
        PublishedOn = release.PublishedAt?.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
        IsPrerelease = release.IsPrerelease;
        Notes = string.IsNullOrWhiteSpace(release.Notes) ? "No release notes were provided for this version." : release.Notes;
        IsFirst = isFirst;
    }

    /// <summary>The version header, e.g. <c>v2.9.0</c>.</summary>
    public string Version { get; }

    /// <summary>The publish date, e.g. <c>Aug 30, 2026</c>, or <see langword="null"/> when unknown.</summary>
    public string? PublishedOn { get; }

    /// <summary><see langword="true"/> for an alpha or beta release (shows a "Pre-release" chip).</summary>
    public bool IsPrerelease { get; }

    /// <summary>The release notes (Markdown).</summary>
    public string Notes { get; }

    /// <summary><see langword="true"/> for the newest release, which has no divider above it.</summary>
    public bool IsFirst { get; }
}
