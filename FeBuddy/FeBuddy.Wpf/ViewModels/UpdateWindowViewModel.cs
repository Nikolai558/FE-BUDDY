using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the modal update window (remediation plan 4.2): shows the running version, the
/// latest version on the user's channel, and that release's notes. FE-Buddy 3.0 ships as an
/// MSI, so "update" means <b>download the new installer</b> - the button opens the release
/// page where the MSI lives; <b>Later</b> dismisses (and colours the version text amber for
/// the session).
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
        ReleaseNotes = string.IsNullOrWhiteSpace(version.LatestReleaseNotes)
            ? "No release notes were provided for this version."
            : version.LatestReleaseNotes!.Trim();
        ReleaseUrl = string.IsNullOrWhiteSpace(version.LatestReleaseUrl) ? ReleasesPage : version.LatestReleaseUrl!;

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

    /// <summary>The GitHub release body for <see cref="LatestVersion"/>.</summary>
    public string ReleaseNotes { get; }

    /// <summary>The release page URL (where the MSI download lives).</summary>
    public string ReleaseUrl { get; }

    /// <summary><see langword="true"/> once the user dismisses without downloading - the shell then colours the version text as a warning for the session.</summary>
    public bool UserDeclined { get; private set; }

    /// <summary>Opens the release page (the MSI download) in the browser and closes the window.</summary>
    public ICommand DownloadCommand { get; }

    /// <summary>Dismisses the window without downloading.</summary>
    public ICommand LaterCommand { get; }
}
