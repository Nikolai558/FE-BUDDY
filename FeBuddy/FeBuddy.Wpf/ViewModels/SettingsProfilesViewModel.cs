using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.Views;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The Settings Profile card, first on Settings (issue #324): which profile is in use, and New…,
/// Rename… and Delete…. Each takes effect at once, apart from the page's Save.
/// </summary>
/// <remarks>
/// <para>
/// Choosing another profile in the list switches to it: unsaved edits on any page are lost, so
/// FE-Buddy asks first when there are some, and puts the list back when the user says no. Every page
/// then reads the new profile's settings (<see cref="ConfigPages.ReloadEverything"/>).
/// </para>
/// <para>
/// Rename… and Delete… act on the profile in use. Delete switches to Default (or, without one, the
/// first other profile) before it deletes, and is off while there is only one profile.
/// </para>
/// </remarks>
public sealed class SettingsProfilesViewModel : ObservableObject
{
	/// <summary>Explains profiles, on the card.</summary>
	public const string Description =
		"A profile is a complete set of FE-Buddy's settings: your facility and output folder, every " +
		"service's choices, the File Conversions' and the Map's. Keep one for each facility you work " +
		"on, or one to try things out in. Your update channel, the News posts you have read, FE-Buddy's " +
		"GitHub token and your saved credentials are the same in every profile.";

	private const string LogSource = "Settings";

	private readonly Func<string, string, string, bool> _confirm;
	private readonly Func<ProfileNameViewModel, bool> _askName;
	private readonly Func<IReadOnlyList<string>> _unsavedPages;
	private readonly Func<IReadOnlyList<string>> _reload;
	private readonly Dispatcher _dispatcher;
	private string? _selectedProfile;

	/// <summary>Creates the card, asking the user in FE-Buddy's own windows.</summary>
	public SettingsProfilesViewModel()
		: this(
			(title, message, confirmText) => ConfirmWindow.Show(Application.Current?.MainWindow, title, message, confirmText),
			question => ProfileNameWindow.Ask(Application.Current?.MainWindow, question),
			ConfigPages.WithUnsavedChanges,
			ConfigPages.ReloadEverything)
	{
	}

	/// <summary>Creates the card. Tests stand in for the windows and the other pages.</summary>
	/// <param name="confirm">Asks a yes-or-no question: title, message and the yes button's label.</param>
	/// <param name="askName">Asks for a profile's name.</param>
	/// <param name="unsavedPages">The pages with unsaved edits.</param>
	/// <param name="reload">Has every page read the settings again; returns those that could not.</param>
	internal SettingsProfilesViewModel(
		Func<string, string, string, bool> confirm,
		Func<ProfileNameViewModel, bool> askName,
		Func<IReadOnlyList<string>> unsavedPages,
		Func<IReadOnlyList<string>> reload)
	{
		_confirm = confirm;
		_askName = askName;
		_unsavedPages = unsavedPages;
		_reload = reload;
		_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

		NewCommand = new RelayCommand(New);
		RenameCommand = new RelayCommand(Rename);
		DeleteCommand = new RelayCommand(Delete, () => CanDelete);

		Refresh();
	}

	/// <summary>Every profile, in name order.</summary>
	public ObservableCollection<string> Profiles { get; } = [];

	/// <summary>The profile in use.</summary>
	public string ActiveProfile => UserConfigFile.ActiveProfile;

	/// <summary>Where the profile in use is saved, between backticks for the view's <c>bhv:InlineCode</c>.</summary>
	public string FileNote => $"Saved in `{UserConfigFile.ConfigFilePath}`.";

	/// <summary>
	/// The profile shown in the list: the one in use. Choosing another switches to it, once the
	/// list's drop-down has closed.
	/// </summary>
	public string? SelectedProfile
	{
		get => _selectedProfile;
		set
		{
			// The list being refilled, or the page going away, sets nothing: that is no choice.
			if (value is null || value == _selectedProfile)
			{
				return;
			}

			_selectedProfile = value;

			if (!value.Equals(ActiveProfile, StringComparison.OrdinalIgnoreCase))
			{
				// After the drop-down closes, so a question asked first comes up over the page.
				_dispatcher.BeginInvoke(() => SwitchTo(value));
			}
		}
	}

	/// <summary>Whether there is another profile to switch to, so the one in use can be deleted.</summary>
	public bool CanDelete => Profiles.Count > 1;

	/// <summary>Makes a new profile - from a copy of the one in use, or every default - and switches to it.</summary>
	public ICommand NewCommand { get; }

	/// <summary>Renames the profile in use.</summary>
	public ICommand RenameCommand { get; }

	/// <summary>Switches to another profile, then deletes the one that was in use.</summary>
	public ICommand DeleteCommand { get; }

	/// <summary>Reads the profiles again: after an import made one, say.</summary>
	public void Refresh()
	{
		IReadOnlyList<string> profiles = UserConfigFile.Profiles();

		// Only what changed, so the list keeps its selection where it can.
		for (int i = Profiles.Count - 1; i >= 0; i--)
		{
			if (!profiles.Contains(Profiles[i], StringComparer.Ordinal))
			{
				Profiles.RemoveAt(i);
			}
		}

		for (int i = 0; i < profiles.Count; i++)
		{
			if (i >= Profiles.Count || Profiles[i] != profiles[i])
			{
				Profiles.Insert(i, profiles[i]);
			}
		}

		ShowActive();
		OnPropertyChanged(nameof(ActiveProfile));
		OnPropertyChanged(nameof(FileNote));
		OnPropertyChanged(nameof(CanDelete));
		CommandManager.InvalidateRequerySuggested();
	}

	/// <summary>Switches to <paramref name="profile"/>, asking first when unsaved edits would be lost.</summary>
	/// <param name="profile">The profile chosen.</param>
	internal void SwitchTo(string profile)
	{
		if (profile.Equals(ActiveProfile, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		IReadOnlyList<string> unsaved = _unsavedPages();

		if (unsaved.Count > 0
			&& !_confirm(
				"Switch settings profile",
				$"{SettingsViewModel.JoinNames(unsaved)} {(unsaved.Count == 1 ? "has" : "have")} unsaved changes, which will be lost. "
				+ "Save them first to keep them.",
				$"Switch to {profile}"))
		{
			ShowActive();
			return;
		}

		if (!Try("Could not switch settings profile", () => UserConfigFile.SwitchProfile(profile)))
		{
			Refresh();
			return;
		}

		AfterSwitch("Settings profile switched", $"FE-Buddy now uses the {ActiveProfile} profile.");
	}

	private void New()
	{
		string active = ActiveProfile;
		ProfileNameViewModel question = new(
			"New Settings Profile",
			$"FE-Buddy makes the profile and switches to it. {active} stays as it is, to switch back to at any time.",
			"Create",
			Profiles,
			renaming: null,
			copyFrom: active,
			unsavedPages: _unsavedPages());

		if (!_askName(question))
		{
			return;
		}

		string name = question.TrimmedName;
		IReadOnlyDictionary<string, string>? values = question.CopyCurrent ? UserConfigFile.SnapshotValues() : null;

		if (!Try("Could not create the profile", () => UserConfigFile.CreateProfile(name, values)))
		{
			return;
		}

		if (!Try("Could not switch settings profile", () => UserConfigFile.SwitchProfile(name)))
		{
			Refresh();
			return;
		}

		AfterSwitch("Settings profile created", values is null
			? $"FE-Buddy now uses {ActiveProfile}, with every setting at its default."
			: $"FE-Buddy now uses {ActiveProfile}, a copy of {active}'s settings.");
	}

	private void Rename()
	{
		string active = ActiveProfile;
		ProfileNameViewModel question = new(
			"Rename Settings Profile",
			$"A new name for {active}. Its settings stay as they are.",
			"Rename",
			Profiles,
			renaming: active,
			copyFrom: null,
			unsavedPages: []);

		if (!_askName(question)
			|| !Try("Could not rename the profile", () => UserConfigFile.RenameProfile(active, question.TrimmedName)))
		{
			return;
		}

		Refresh();
		Toast.Success("Settings profile renamed", $"{active} is now called {ActiveProfile}.");
	}

	private void Delete()
	{
		string active = ActiveProfile;
		string? next = Profiles.FirstOrDefault(p => !p.Equals(active, StringComparison.OrdinalIgnoreCase)
				&& p.Equals(UserConfigFile.DefaultProfile, StringComparison.OrdinalIgnoreCase))
			?? Profiles.FirstOrDefault(p => !p.Equals(active, StringComparison.OrdinalIgnoreCase));

		if (next is null)
		{
			return;
		}

		IReadOnlyList<string> unsaved = _unsavedPages();
		string lost = unsaved.Count > 0 ? $"\n\nUnsaved changes on {SettingsViewModel.JoinNames(unsaved)} will be lost." : string.Empty;

		if (!_confirm(
			"Delete settings profile",
			$"Delete the {active} profile? FE-Buddy switches to {next}, then deletes {active}'s settings from this PC. "
			+ $"They can't be brought back: Export… saves a copy first, should you want them again.{lost}",
			$"Delete {active}"))
		{
			return;
		}

		if (!Try("Could not switch settings profile", () => UserConfigFile.SwitchProfile(next)))
		{
			Refresh();
			return;
		}

		bool deleted = Try($"Could not delete {active}", () => UserConfigFile.DeleteProfile(active));

		AfterSwitch(deleted ? "Settings profile deleted" : "Settings profile switched", deleted
			? $"{active} is deleted. FE-Buddy now uses {ActiveProfile}."
			: $"FE-Buddy now uses {ActiveProfile}.");
	}

	/// <summary>Shows the profile in use in the list, once the list has finished changing too.</summary>
	private void ShowActive()
	{
		_selectedProfile = Profiles.FirstOrDefault(p => p.Equals(ActiveProfile, StringComparison.OrdinalIgnoreCase));
		OnPropertyChanged(nameof(SelectedProfile));

		// A ComboBox takes no notice of a change made while it is still setting the value itself.
		_dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(SelectedProfile)));
	}

	/// <summary>Has every page show the profile now in use, and says so.</summary>
	private void AfterSwitch(string title, string message)
	{
		Refresh();
		IReadOnlyList<string> notReloaded = _reload();
		Toast.Success(title, message);

		if (notReloaded.Count > 0)
		{
			// Saving on one of these would write the last profile's values into this one.
			Toast.Warn("Restart FE-Buddy",
				$"{SettingsViewModel.JoinNames(notReloaded)} could not show the {ActiveProfile} profile's settings. Restart FE-Buddy before saving there, " +
				"or the settings it shows would be saved into this profile.");
		}
	}

	/// <summary>Does <paramref name="action"/>, and tells the user why when it fails.</summary>
	private static bool Try(string failure, Action action)
	{
		try
		{
			action();
			return true;
		}
		catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"{failure}: {ex.Message}");
			Toast.Error(failure, ex.Message);
			return false;
		}
	}
}
