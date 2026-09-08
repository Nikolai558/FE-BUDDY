using System.Diagnostics;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

using FEBuddyLibrary.Handlers;
using FEBuddyLibrary.Models.Services.General;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the modal update window (remediation plan 4.2): shows the running version, the
/// latest version on the user's channel, and that release's notes, and offers
/// <b>Update</b> (delegates to the existing Squirrel plumbing) / <b>Cancel</b>.
/// </summary>
public sealed class UpdateWindowViewModel : ObservableObject
{
    private bool _isUpdating;

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
        ReleaseUrl = version.LatestReleaseUrl;

        UpdateCommand = new RelayCommand(RunUpdate, () => !IsUpdating);
        CancelCommand = new RelayCommand(() =>
        {
            UserDeclined = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        });
        OpenReleasePageCommand = new RelayCommand(OpenReleasePage, () => !string.IsNullOrWhiteSpace(ReleaseUrl));
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

    /// <summary>The GitHub release page URL, or <see langword="null"/>.</summary>
    public string? ReleaseUrl { get; }

    /// <summary><see langword="true"/> once the user closes the window without updating - the shell then colours the version text as a warning for the session.</summary>
    public bool UserDeclined { get; private set; }

    /// <summary><see langword="true"/> while an update is being applied.</summary>
    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (SetProperty(ref _isUpdating, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ICommand UpdateCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand OpenReleasePageCommand { get; }

    private void RunUpdate()
    {
        IsUpdating = true;
        Toast.Info("Updating", $"Downloading and installing v{LatestVersion}…");

        _ = Task.Run(() =>
        {
            try
            {
                // Squirrel's UpdateManager restarts the app on success, so control does not
                // normally return here.
                UdateHandler.Update();
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    IsUpdating = false;
                    Toast.Error("Update failed", ex.Message);
                });
            }
        });
    }

    private void OpenReleasePage()
    {
        if (string.IsNullOrWhiteSpace(ReleaseUrl))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(ReleaseUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Toast.Warn("Could not open the release page", ex.Message);
        }
    }
}
