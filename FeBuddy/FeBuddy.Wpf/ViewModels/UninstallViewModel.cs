using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs the Uninstall FE-Buddy window (Settings ▸ Uninstall FE-Buddy): what the uninstall removes,
/// what it leaves, and the one choice it offers - save a copy of the settings first.
/// </summary>
/// <remarks>
/// Only the choice is made here. Settings saves the copy, starts Windows' uninstall
/// (<see cref="Core.Application.Updates.AppUninstall"/>) and closes FE-Buddy.
/// </remarks>
public sealed class UninstallViewModel : ObservableObject
{
	/// <summary>What deleting the settings loses.</summary>
	public const string SettingsLoss =
		"Every settings profile - your output folder and facility, the default Region of Interest, every " +
		"AIRAC Service tab's choices (CRC ERAM defaults and file names included), your custom alias files, " +
		"the File Conversions' folders and options, the Map's files and layers - and your update channel, " +
		"which GitHub token FE-Buddy uses, and which News posts you have read.";

	/// <summary>How the saved copy is used, shown under the Save a copy first box.</summary>
	public const string CopyNote =
		"You choose where (the Desktop, to start with); any other profile is saved beside it, its name in " +
		"brackets. If you install FE-Buddy again, Settings ▸ Import… brings each back, all but this PC's own " +
		"choices: the update channel, the GitHub token, the credential for each web address and which News " +
		"posts you have read.";

	/// <summary>What the uninstall leaves, one line each.</summary>
	public static IReadOnlyList<string> Kept { get; } =
	[
		"Everything in your output folder (FE-Buddy_Output and the files you made)",
		"FE-Buddy's settings and credentials in other people's Windows accounts on this PC",
	];

	/// <summary>What happens once the user confirms, shown above the buttons.</summary>
	public const string NextSteps =
		"FE-Buddy closes, and Windows asks you to confirm and for administrator permission. If you " +
		"cancel there, nothing is removed - just start FE-Buddy again.";

	private bool _saveCopyFirst = true;

	/// <summary>Creates the view-model.</summary>
	/// <param name="hasSettings">Whether there are saved settings (a settings profile) to lose.</param>
	/// <param name="credentialCount">How many credentials are saved in Windows Credential Manager.</param>
	/// <param name="unfinishedWork">What closing FE-Buddy now would lose (a running job, unsaved edits); empty when nothing.</param>
	public UninstallViewModel(bool hasSettings, int credentialCount, IReadOnlyList<string> unfinishedWork)
	{
		ArgumentNullException.ThrowIfNull(unfinishedWork);

		HasSettings = hasSettings;
		CredentialCount = Math.Max(0, credentialCount);
		UnfinishedWork = unfinishedWork;

		List<string> removed =
		[
			"FE-Buddy itself: the program, its Start menu and Desktop shortcuts, and its entry in Installed apps",
			"The AIRAC, Telephony and Wx Station data it has downloaded, its logs and its temporary files",
		];

		if (HasSettings)
		{
			removed.Add("Your settings and their backups");
		}

		if (CredentialCount > 0)
		{
			removed.Add(CredentialCount == 1
				? "Your saved credential, from Windows Credential Manager"
				: $"Your {CredentialCount} saved credentials, from Windows Credential Manager");
		}

		Removed = removed;

		ConfirmCommand = new RelayCommand(() => Close(confirmed: true));
		CancelCommand = new RelayCommand(() => Close(confirmed: false));
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>What this uninstall removes, one line each.</summary>
	public IReadOnlyList<string> Removed { get; }

	/// <summary>Whether there are saved settings to lose, and so to offer a copy of.</summary>
	public bool HasSettings { get; }

	/// <summary>How many credentials are saved.</summary>
	public int CredentialCount { get; }

	/// <summary>Save a copy of the settings before they are removed. On by default.</summary>
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

	/// <summary>Whether a copy is saved first: only when there are settings to copy.</summary>
	public bool SavesCopy => HasSettings && SaveCopyFirst;

	/// <summary>What closing FE-Buddy now would lose, one line each; empty when nothing.</summary>
	public IReadOnlyList<string> UnfinishedWork { get; }

	/// <summary>Whether closing FE-Buddy now would lose anything.</summary>
	public bool HasUnfinishedWork => UnfinishedWork.Count > 0;

	/// <summary>Whether the user chose to uninstall.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>Closes the window and uninstalls.</summary>
	public ICommand ConfirmCommand { get; }

	/// <summary>Closes the window without uninstalling.</summary>
	public ICommand CancelCommand { get; }

	private void Close(bool confirmed)
	{
		Confirmed = confirmed;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
