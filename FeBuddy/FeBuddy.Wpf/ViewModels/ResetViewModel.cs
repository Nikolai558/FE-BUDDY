using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.FileSystem.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the Reset FE-Buddy window (Settings ▸ Reset FE-Buddy): what a reset always deletes, and
/// the two choices it leaves to the user - keep or delete the settings (and, deleting them, whether
/// to save a copy first), and, when any are saved, keep or delete the credentials.
/// </summary>
/// <remarks>
/// Only the choices are made here. Settings saves the copy, records the reset
/// (<see cref="AppDataReset.Request"/>) and restarts FE-Buddy, which carries it out as it starts.
/// Keeping is the default for both, so a reset never takes more than the user picked.
/// </remarks>
public sealed class ResetViewModel : ObservableObject
{
	/// <summary>What every reset deletes, one line each.</summary>
	public static IReadOnlyList<string> AlwaysDeleted { get; } =
	[
		"The AIRAC data FE-Buddy has downloaded (every cycle)",
		"The Telephony and Wx Station data it has downloaded",
		"Its logs",
		"The backups of your settings (the copies Undo last save and Import keep)",
	];

	/// <summary>What deleting the settings loses.</summary>
	public const string SettingsLoss =
		"Every settings profile - your output folder and facility, the default Region of Interest, every " +
		"AIRAC Service tab's choices (CRC ERAM defaults and file names included), your custom alias files, " +
		"the File Conversions' folders and options, the Map's files and layers - and your update channel, " +
		"which GitHub token FE-Buddy uses, and which News posts you have read. FE-Buddy starts with its defaults.";

	/// <summary>How the saved copy is used, shown under the Save a copy first box.</summary>
	public const string CopyNote =
		"You choose where (the Desktop, to start with); any other profile is saved beside it, its name in " +
		"brackets. Settings ▸ Import… brings each back, all but this PC's own choices: the update channel, " +
		"the GitHub token, the credential for each web address and which News posts you have read.";

	private bool _keepSettings = true;
	private bool _saveCopyFirst = true;
	private bool _keepCredentials = true;

	/// <summary>Creates the view-model.</summary>
	/// <param name="hasSettings">Whether there are saved settings (a settings profile) to keep or delete.</param>
	/// <param name="credentialCount">How many credentials are saved; with none, there is nothing to choose.</param>
	/// <param name="unfinishedWork">What closing FE-Buddy now would lose (a running job, unsaved edits); empty when nothing.</param>
	public ResetViewModel(bool hasSettings, int credentialCount, IReadOnlyList<string> unfinishedWork)
	{
		ArgumentNullException.ThrowIfNull(unfinishedWork);

		HasSettings = hasSettings;
		CredentialCount = Math.Max(0, credentialCount);
		UnfinishedWork = unfinishedWork;

		ConfirmCommand = new RelayCommand(() => Close(confirmed: true));
		CancelCommand = new RelayCommand(() => Close(confirmed: false));
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>Whether there are saved settings to keep or delete.</summary>
	public bool HasSettings { get; }

	/// <summary>Keep the settings profiles. The default.</summary>
	public bool KeepSettings
	{
		get => _keepSettings;
		set
		{
			if (SetProperty(ref _keepSettings, value))
			{
				OnPropertyChanged(nameof(DeleteSettings));
				OnPropertyChanged(nameof(DeletesSettings));
				OnPropertyChanged(nameof(SavesCopy));
			}
		}
	}

	/// <summary>Delete the settings profiles: the other side of <see cref="KeepSettings"/>, for the second radio button.</summary>
	public bool DeleteSettings
	{
		get => !KeepSettings;
		set => KeepSettings = !value;
	}

	/// <summary>Whether this reset deletes saved settings: there are some, and the user chose to delete them.</summary>
	public bool DeletesSettings => HasSettings && !KeepSettings;

	/// <summary>Save a copy of the settings before they are deleted. On by default.</summary>
	public bool SaveCopyFirst
	{
		get => _saveCopyFirst;
		set
		{
			if (SetProperty(ref _saveCopyFirst, value))
			{
				OnPropertyChanged(nameof(SavesCopy));
			}
		}
	}

	/// <summary>Whether a copy is saved first: only when the settings are being deleted.</summary>
	public bool SavesCopy => DeletesSettings && SaveCopyFirst;

	/// <summary>How many credentials are saved.</summary>
	public int CredentialCount { get; }

	/// <summary>Whether any credentials are saved, so there is a choice to make about them.</summary>
	public bool HasCredentials => CredentialCount > 0;

	/// <summary>The credentials section's heading, e.g. <c>Saved Credentials (3)</c>.</summary>
	public string CredentialsHeader => $"Saved Credentials ({CredentialCount})";

	/// <summary>Keep the saved credentials. The default.</summary>
	public bool KeepCredentials
	{
		get => _keepCredentials;
		set
		{
			if (SetProperty(ref _keepCredentials, value))
			{
				OnPropertyChanged(nameof(DeleteCredentials));
			}
		}
	}

	/// <summary>Delete the saved credentials: the other side of <see cref="KeepCredentials"/>.</summary>
	public bool DeleteCredentials
	{
		get => !KeepCredentials;
		set => KeepCredentials = !value;
	}

	/// <summary>What closing FE-Buddy now would lose, one line each; empty when nothing.</summary>
	public IReadOnlyList<string> UnfinishedWork { get; }

	/// <summary>Whether closing FE-Buddy now would lose anything.</summary>
	public bool HasUnfinishedWork => UnfinishedWork.Count > 0;

	/// <summary>Whether the user chose to reset.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>Closes the window and resets.</summary>
	public ICommand ConfirmCommand { get; }

	/// <summary>Closes the window without resetting.</summary>
	public ICommand CancelCommand { get; }

	/// <summary>The reset the user chose, for <see cref="AppDataReset.Request"/>.</summary>
	/// <param name="processId">This FE-Buddy's process id, for the next launch to wait on.</param>
	/// <returns>The request.</returns>
	public AppDataResetRequest BuildRequest(int processId) =>
		new(KeepSettings: !DeletesSettings, DeleteCredentials: HasCredentials && DeleteCredentials, WaitForProcessId: processId);

	private void Close(bool confirmed)
	{
		Confirmed = confirmed;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
