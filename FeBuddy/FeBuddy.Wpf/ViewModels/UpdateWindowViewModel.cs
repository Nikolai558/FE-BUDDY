using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the modal update window: shows the running version, the latest
/// version on the user's channel, and the notes for every release in between, newest first.
/// </summary>
/// <remarks>
/// <b>Update now</b> downloads the release's MSI (with progress), runs it elevated with its
/// normal UI, and sets <see cref="InstallerStarted"/> so the shell closes FE-Buddy - the MSI
/// cannot replace files that are in use, and relaunches FE-Buddy when it finishes. It first
/// warns if closing would lose unfinished work. A copy the MSI did not install (a dev build), a
/// release with no MSI, or a failed download opens the release page instead. <b>Later</b>
/// dismisses (and colours the version text amber for the session), or cancels a download.
/// </remarks>
public sealed class UpdateWindowViewModel : ObservableObject
{
	private const string ReleasesPage = "https://github.com/Nikolai558/FE-BUDDY/releases/latest";

	// ERROR_CANCELLED: the user said no at the UAC prompt.
	private const int ErrorCancelled = 1223;

	private readonly bool _isMsiInstalled;
	private readonly ReleaseInstaller? _installer;
	private readonly Func<IReadOnlyList<string>> _unfinishedWork;
	private CancellationTokenSource? _download;
	private bool _useReleasePage;
	private bool _isBusy;
	private double _progressPercent;
	private string? _progressText;
	private string? _errorText;

	/// <summary>Creates the view-model from a completed version check that found an update.</summary>
	/// <param name="version">The version-check result. <see cref="VersionCheckResult.UpdateAvailable"/> is expected to be <see langword="true"/>.</param>
	/// <param name="isMsiInstalled">Whether this copy is the MSI-installed one (only that copy installs updates itself).</param>
	/// <param name="unfinishedWork">Describes what closing FE-Buddy now would lose (a running job, unsaved edits); empty when nothing.</param>
	public UpdateWindowViewModel(VersionCheckResult version, bool isMsiInstalled, Func<IReadOnlyList<string>> unfinishedWork)
	{
		ArgumentNullException.ThrowIfNull(version);
		ArgumentNullException.ThrowIfNull(unfinishedWork);

		_isMsiInstalled = isMsiInstalled;
		_installer = version.LatestInstaller;
		_unfinishedWork = unfinishedWork;

		CurrentVersion = string.IsNullOrWhiteSpace(version.CurrentVersion) ? "dev" : version.CurrentVersion.TrimStart('v', 'V');
		LatestVersion = version.LatestVersion ?? "unknown";
		Channel = version.Channel.ToString();
		ReleaseUrl = string.IsNullOrWhiteSpace(version.LatestReleaseUrl) ? ReleasesPage : version.LatestReleaseUrl!;

		IReadOnlyList<ReleaseSummary> releases = version.NewerReleases.Count > 0
			? version.NewerReleases
			: [new ReleaseSummary(LatestVersion, null, false, null, ReleaseUrl)];

		Releases = [.. releases.Select((release, index) => new ReleaseNotesItem(release, isFirst: index == 0))];
		ReleasesBehind = Releases.Count > 1 ? $"{Releases.Count} releases since v{CurrentVersion}" : null;

		UpdateCommand = new RelayCommand(() => _ = UpdateAsync(), () => !IsBusy);

		LaterCommand = new RelayCommand(() =>
		{
			if (IsBusy)
			{
				CancelDownload();
				return;
			}

			UserDeclined = true;
			CloseRequested?.Invoke(this, EventArgs.Empty);
		});
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>
	/// Asks the user whether to close FE-Buddy despite the listed unfinished work; set by the
	/// window so the prompt is owned by it. Without it, the update goes ahead.
	/// </summary>
	public Func<IReadOnlyList<string>, bool>? ConfirmCloseWithUnfinishedWork { get; set; }

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

	/// <summary>The latest release's page, where its MSI lives.</summary>
	public string ReleaseUrl { get; }

	/// <summary><see langword="true"/> when <b>Update now</b> downloads and runs the installer, rather than opening the release page.</summary>
	public bool CanInstall => _isMsiInstalled && _installer is not null && !_useReleasePage;

	/// <summary>The main button's label.</summary>
	public string UpdateButtonText => IsBusy ? "Downloading…" : CanInstall ? "Update now" : "Open release page";

	/// <summary>The secondary button's label: Later, or Cancel while downloading.</summary>
	public string LaterButtonText => IsBusy ? "Cancel" : "Later";

	/// <summary>What the main button will do.</summary>
	public string FooterText =>
		CanInstall ? "Update now downloads the installer and runs it. FE-Buddy closes while it installs and opens again when it finishes; your settings are kept."
		: !_isMsiInstalled ? "This copy of FE-Buddy was not installed by the FE-Buddy installer (a development build), so the update opens the release page instead."
		: _installer is null ? "This release has no installer attached, so the update opens the release page."
		: "Download the installer from the release page and run it; your settings are kept.";

	/// <summary><see langword="true"/> while the installer downloads or starts.</summary>
	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if (SetProperty(ref _isBusy, value))
			{
				OnPropertyChanged(nameof(UpdateButtonText));
				OnPropertyChanged(nameof(LaterButtonText));
				CommandManager.InvalidateRequerySuggested();
			}
		}
	}

	/// <summary>Download progress, 0 to 100.</summary>
	public double ProgressPercent
	{
		get => _progressPercent;
		private set => SetProperty(ref _progressPercent, value);
	}

	/// <summary>The line under the progress bar, e.g. "12.4 MB of 52.7 MB".</summary>
	public string? ProgressText
	{
		get => _progressText;
		private set => SetProperty(ref _progressText, value);
	}

	/// <summary>Why the last attempt did not install, or <see langword="null"/>.</summary>
	public string? ErrorText
	{
		get => _errorText;
		private set => SetProperty(ref _errorText, value);
	}

	/// <summary><see langword="true"/> once the user dismisses without updating - the shell then colours the version text as a warning for the session.</summary>
	public bool UserDeclined { get; private set; }

	/// <summary><see langword="true"/> once the installer is running - the shell then closes FE-Buddy.</summary>
	public bool InstallerStarted { get; private set; }

	/// <summary>Downloads and runs the installer, or opens the release page (see <see cref="CanInstall"/>).</summary>
	public ICommand UpdateCommand { get; }

	/// <summary>Dismisses the window, or cancels a download in progress.</summary>
	public ICommand LaterCommand { get; }

	/// <summary>Cancels a download in progress (also called when the window closes).</summary>
	public void CancelDownload() => _download?.Cancel();

	private async Task UpdateAsync()
	{
		if (!CanInstall)
		{
			BrowserLauncher.Open(ReleaseUrl);
			CloseRequested?.Invoke(this, EventArgs.Empty);
			return;
		}

		IReadOnlyList<string> unfinished = _unfinishedWork();
		if (unfinished.Count > 0 && ConfirmCloseWithUnfinishedWork?.Invoke(unfinished) == false)
		{
			return;
		}

		ErrorText = null;
		ProgressPercent = 0;
		ProgressText = "Starting the download…";
		IsBusy = true;

		using CancellationTokenSource download = new();
		_download = download;

		string msiPath;
		try
		{
			var progress = new Progress<DownloadProgress>(ReportProgress);
			msiPath = await UpdateInstaller.DownloadAsync(_installer!, progress, cancellationToken: download.Token);
		}
		catch (OperationCanceledException)
		{
			IsBusy = false;
			ProgressText = null;
			return;
		}
		catch (Exception ex)
		{
			FallBackToReleasePage($"The installer could not be downloaded ({ex.Message}).");
			return;
		}
		finally
		{
			_download = null;
		}

		ProgressText = "Starting the installer…";

		try
		{
			Process.Start(new ProcessStartInfo("msiexec.exe", UpdateInstaller.InstallerArguments(msiPath))
			{
				UseShellExecute = true,
				Verb = "runas",
			});
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
		{
			AppLog.Info("UpdateInstaller", "Update cancelled at the administrator prompt.");
			IsBusy = false;
			ProgressText = null;
			ErrorText = "Update cancelled - installing needs administrator permission. Choose Update now to try again.";
			return;
		}
		catch (Exception ex)
		{
			FallBackToReleasePage($"The installer could not be started ({ex.Message}).");
			return;
		}

		AppLog.Info("UpdateInstaller", $"Started the installer for v{LatestVersion}; closing FE-Buddy.");
		InstallerStarted = true;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}

	private void ReportProgress(DownloadProgress progress)
	{
		const double Mb = 1024 * 1024;

		ProgressPercent = progress.Percent ?? 0;
		ProgressText = progress.TotalBytes is long total
			? string.Format(CultureInfo.CurrentCulture, "{0:0.0} MB of {1:0.0} MB", progress.BytesReceived / Mb, total / Mb)
			: string.Format(CultureInfo.CurrentCulture, "{0:0.0} MB downloaded", progress.BytesReceived / Mb);
	}

	private void FallBackToReleasePage(string reason)
	{
		_useReleasePage = true;
		IsBusy = false;
		ProgressText = null;
		ErrorText = reason + " Open the release page to download it yourself.";
		OnPropertyChanged(nameof(CanInstall));
		OnPropertyChanged(nameof(UpdateButtonText));
		OnPropertyChanged(nameof(FooterText));
	}
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
