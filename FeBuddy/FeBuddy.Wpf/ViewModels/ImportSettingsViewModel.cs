using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ImportSettingsWindow</c>, which Settings ▸ Import… opens once it has read a file: where
/// its settings go - a new profile (the default), added to the profile in use, or in place of the
/// profile in use's - and what that does.
/// </summary>
/// <remarks>
/// Both imports are worked out before the window opens (<see cref="UserConfigTransfer.Plan(UserConfigPackage, UserConfigImportMode)"/>),
/// so the summary is the import that will be done. A new profile takes the replacing one: it gets the
/// file's settings and nothing else.
/// </remarks>
public sealed class ImportSettingsViewModel : ObservableObject
{
	/// <summary>How many folders the summary lists under each heading before "…and N more".</summary>
	private const int ListLength = 8;

	private readonly UserConfigImportPlan _replace;
	private readonly UserConfigImportPlan _merge;
	private readonly IReadOnlyList<string> _profiles;
	private readonly IReadOnlyList<string> _unsavedPages;
	private readonly string _runningVersion;
	private ImportDestination _destination;
	private string _newProfileName;

	/// <summary>Creates the view-model.</summary>
	/// <param name="replace">The import planned to replace the profile in use's settings.</param>
	/// <param name="merge">The import planned to add to them.</param>
	/// <param name="activeProfile">The profile in use.</param>
	/// <param name="profiles">Every profile, whose names a new one can't have.</param>
	/// <param name="unsavedPages">The pages whose unsaved edits the import would lose.</param>
	/// <param name="runningVersion">This FE-Buddy's version, to compare with the file's.</param>
	public ImportSettingsViewModel(
		UserConfigImportPlan replace,
		UserConfigImportPlan merge,
		string activeProfile,
		IReadOnlyList<string> profiles,
		IReadOnlyList<string> unsavedPages,
		string runningVersion)
	{
		ArgumentNullException.ThrowIfNull(replace);
		ArgumentNullException.ThrowIfNull(merge);
		ArgumentNullException.ThrowIfNull(profiles);
		ArgumentNullException.ThrowIfNull(unsavedPages);

		_replace = replace;
		_merge = merge;
		_profiles = profiles;
		_unsavedPages = unsavedPages;
		_runningVersion = runningVersion.TrimStart('v', 'V');
		ActiveProfile = activeProfile;
		_newProfileName = SuggestName(replace.Package, profiles);

		ImportCommand = new RelayCommand(() => Close(confirmed: true), () => CanImport);
		CancelCommand = new RelayCommand(() => Close(confirmed: false));
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>The profile in use.</summary>
	public string ActiveProfile { get; }

	/// <summary>The file, and where it came from, e.g. <c>ZOB.json, exported from FE-Buddy v3.1.0 on 7 Oct 2026.</c></summary>
	public string FromFile
	{
		get
		{
			UserConfigPackage package = _replace.Package;

			if (package.IsPlainConfigFile)
			{
				return $"{package.FileName}, a settings file from another PC.";
			}

			if (package.AppVersion is not { } version)
			{
				return $"{package.FileName}.";
			}

			string when = package.ExportedUtc is { } exported
				? $" on {exported.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
				: string.Empty;

			return $"{package.FileName}, exported from FE-Buddy v{version.TrimStart('v', 'V')}{when}.";
		}
	}

	/// <summary>Where the settings go.</summary>
	public ImportDestination Destination
	{
		get => _destination;
		set
		{
			if (SetProperty(ref _destination, value))
			{
				OnPropertyChanged(nameof(IsNewProfile));
				OnPropertyChanged(nameof(IsMerge));
				OnPropertyChanged(nameof(IsReplace));
				Changed();
			}
		}
	}

	/// <summary>A new profile: the first radio button.</summary>
	public bool IsNewProfile
	{
		get => Destination == ImportDestination.NewProfile;
		set { if (value) Destination = ImportDestination.NewProfile; }
	}

	/// <summary>Added to the profile in use: the second radio button.</summary>
	public bool IsMerge
	{
		get => Destination == ImportDestination.Merge;
		set { if (value) Destination = ImportDestination.Merge; }
	}

	/// <summary>In place of the profile in use's settings: the third radio button.</summary>
	public bool IsReplace
	{
		get => Destination == ImportDestination.Replace;
		set { if (value) Destination = ImportDestination.Replace; }
	}

	/// <summary>The new profile's name, as typed. It starts as the file's facility, when no profile has that name.</summary>
	public string NewProfileName
	{
		get => _newProfileName;
		set
		{
			if (SetProperty(ref _newProfileName, value ?? string.Empty))
			{
				Changed();
			}
		}
	}

	/// <summary>Why the new profile can't have the name typed, or <see langword="null"/>. Nothing is said while the box is empty.</summary>
	public string? NameProblem =>
		IsNewProfile && NewProfileName.Trim().Length > 0 ? ProfileNameViewModel.ProblemWith(NewProfileName, _profiles, renaming: null) : null;

	/// <summary>Whether <see cref="NameProblem"/> is shown.</summary>
	public bool HasNameProblem => NameProblem is not null;

	/// <summary>Under the new-profile choice.</summary>
	public string NewProfileDescription => $"FE-Buddy switches to it. {ActiveProfile} stays as it is.";

	/// <summary>The second choice's label.</summary>
	public string MergeLabel => $"Add them to {ActiveProfile}";

	/// <summary>Under it.</summary>
	public string MergeDescription =>
		$"Where both have a setting, the file's is used; {ActiveProfile}'s other settings stay. A list in the file, " +
		$"such as the custom alias files, replaces {ActiveProfile}'s whole list.";

	/// <summary>The third choice's label.</summary>
	public string ReplaceLabel => $"Replace {ActiveProfile}'s settings with them";

	/// <summary>Under it.</summary>
	public string ReplaceDescription =>
		$"{ActiveProfile} gets the file's settings and nothing else: a setting the file doesn't have goes back to its default.";

	/// <summary>The import <see cref="Destination"/> does: the replacing one for a new profile.</summary>
	public UserConfigImportPlan Plan => IsMerge ? _merge : _replace;

	/// <summary>Whether Import is on: a usable name for a new profile, or something to change in the profile in use.</summary>
	public bool CanImport => IsNewProfile
		? NewProfileName.Trim().Length > 0 && NameProblem is null
		: Plan.HasChanges;

	/// <summary>What the import does, for the summary. A folder or path is between backticks for the view's <c>bhv:InlineCode</c>.</summary>
	public string Summary
	{
		get
		{
			UserConfigImportPlan plan = Plan;
			StringBuilder text = new();

			if (IsNewProfile)
			{
				string name = NewProfileName.Trim().Length > 0 && NameProblem is null ? NewProfileName.Trim() : "The new profile";
				text.Append(CultureInfo.InvariantCulture, $"{name} gets the file's settings.");
			}
			else if (!plan.HasChanges)
			{
				text.Append(CultureInfo.InvariantCulture, $"Nothing changes: {ActiveProfile} already has these settings.");
			}
			else
			{
				text.Append(CultureInfo.InvariantCulture,
					$"{plan.ChangedCount} {(plan.ChangedCount == 1 ? "setting changes" : "settings change")} in {ActiveProfile}.");
			}

			// The same settings can produce different output on a different version of FE-Buddy.
			if (plan.Package.AppVersion?.TrimStart('v', 'V') is { } theirs && !string.Equals(theirs, _runningVersion, StringComparison.OrdinalIgnoreCase))
			{
				text.Append(
					$"\n\nThis PC runs FE-Buddy v{_runningVersion}, the file came from v{theirs}. The settings import all the same, "
					+ "but for identical output both PCs should run the same version.");
			}

			AppendList(text, "Folders and files:", plan.AppliedFolders, folder =>
				folder.Path.Length == 0 ? folder.Note!
				: folder.Note is null ? $"`{folder.Path}`"
				: $"`{folder.Path}` ({folder.Note})");

			AppendList(text, "Not taken, as they do not work on this PC:", plan.SkippedFolders, folder => $"the file's `{folder.Path}` {folder.Note}");

			if (plan.KeptForThisPc.Count > 0)
			{
				string kept = SettingsViewModel.JoinNames([.. plan.KeptForThisPc.Select(label => char.ToLowerInvariant(label[0]) + label[1..])]);
				text.Append(CultureInfo.InvariantCulture, $"\n\nKept as they are on this PC: {kept}.");
			}

			if (_unsavedPages.Count > 0)
			{
				text.Append(CultureInfo.InvariantCulture, $"\n\nUnsaved changes on {SettingsViewModel.JoinNames(_unsavedPages)} will be lost.");
			}

			if (!IsNewProfile && plan.HasChanges)
			{
				text.Append(CultureInfo.InvariantCulture,
					$"\n\n{ActiveProfile}'s settings as they are now are kept in {Path.GetFileName(UserConfigFile.BeforeImportFilePath)} in case you want them back.");
			}

			return text.ToString();
		}
	}

	/// <summary>Whether the user chose Import, not Cancel.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>Closes the window and imports. Off while <see cref="CanImport"/> is not.</summary>
	public ICommand ImportCommand { get; }

	/// <summary>Closes the window without importing.</summary>
	public ICommand CancelCommand { get; }

	/// <summary>The file's facility, when it has one and no profile is called that; else nothing.</summary>
	private static string SuggestName(UserConfigPackage package, IReadOnlyList<string> profiles) =>
		package.Values.GetValueOrDefault(SettingsViewModel.ArtccKey)?.Trim() is { Length: > 0 } facility
			&& ProfileNameViewModel.ProblemWith(facility, profiles, renaming: null) is null
			? facility
			: string.Empty;

	/// <summary>
	/// Adds a heading and one line per folder, the first <see cref="ListLength"/> of them, so a file
	/// with many folders cannot grow the summary off the screen.
	/// </summary>
	private static void AppendList(StringBuilder text, string heading, IReadOnlyList<ImportedFolder> folders, Func<ImportedFolder, string> detail)
	{
		if (folders.Count == 0)
		{
			return;
		}

		text.Append(CultureInfo.InvariantCulture, $"\n\n{heading}");

		foreach (ImportedFolder folder in folders.Take(ListLength))
		{
			text.Append(CultureInfo.InvariantCulture, $"\n  • {folder.Label}: {detail(folder)}");
		}

		if (folders.Count > ListLength)
		{
			text.Append(CultureInfo.InvariantCulture, $"\n  • …and {folders.Count - ListLength} more");
		}
	}

	private void Changed()
	{
		OnPropertyChanged(nameof(NameProblem));
		OnPropertyChanged(nameof(HasNameProblem));
		OnPropertyChanged(nameof(Plan));
		OnPropertyChanged(nameof(CanImport));
		OnPropertyChanged(nameof(Summary));
		CommandManager.InvalidateRequerySuggested();
	}

	private void Close(bool confirmed)
	{
		Confirmed = confirmed;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
