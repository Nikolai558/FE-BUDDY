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
	private const string LogSource = "Settings";

	private readonly Func<string, string, string, bool> _confirm;
	private readonly Func<ProfileNameViewModel, bool> _askName;
	private readonly Func<IReadOnlyList<string>> _unsavedPages;
	private readonly Func<IReadOnlyList<string>> _reload;
	private readonly Dispatcher _dispatcher;
	private string? _selectedProfile;
	private bool _busy;
	private Task _working = Task.CompletedTask;

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

		NewCommand = new RelayCommand(New, () => !_busy);
		RenameCommand = new RelayCommand(Rename, () => !_busy);
		DeleteCommand = new RelayCommand(Delete, () => CanDelete && !_busy);

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

	/// <summary>
	/// The profile change under way - its file work off the UI thread, then the pages reading the
	/// profile on it - or a finished task. Unit tests wait for it.
	/// </summary>
	internal Task Working => _working;

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
		if (_busy || profile.Equals(ActiveProfile, StringComparison.OrdinalIgnoreCase))
		{
			ShowActive();
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

		Run([("Could not switch settings profile", () => UserConfigFile.SwitchProfile(profile))], done =>
		{
			if (done == 0)
			{
				Refresh();
				return;
			}

			AfterSwitch("Settings profile switched", $"FE-Buddy now uses the {ActiveProfile} profile.");
		});
	}

	private void New()
	{
		if (_busy)
		{
			return;
		}

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

		Run(
			[
				("Could not create the profile", () => UserConfigFile.CreateProfile(name, values)),
				("Could not switch settings profile", () => UserConfigFile.SwitchProfile(name)),
			],
			done =>
			{
				switch (done)
				{
					case 0:
						return;
					case 1:
						Refresh();
						return;
					default:
						AfterSwitch("Settings profile created", values is null
							? $"FE-Buddy now uses {ActiveProfile}, with every setting at its default."
							: $"FE-Buddy now uses {ActiveProfile}, a copy of {active}'s settings.");
						return;
				}
			});
	}

	private void Rename()
	{
		if (_busy)
		{
			return;
		}

		string active = ActiveProfile;
		ProfileNameViewModel question = new(
			"Rename Settings Profile",
			$"A new name for {active}. Its settings stay as they are.",
			"Rename",
			Profiles,
			renaming: active,
			copyFrom: null,
			unsavedPages: []);

		if (!_askName(question))
		{
			return;
		}

		string name = question.TrimmedName;

		Run([("Could not rename the profile", () => UserConfigFile.RenameProfile(active, name))], done =>
		{
			if (done == 1)
			{
				Refresh();
				Toast.Success("Settings profile renamed", $"{active} is now called {ActiveProfile}.");
			}
		});
	}

	private void Delete()
	{
		if (_busy)
		{
			return;
		}

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

		Run(
			[
				("Could not switch settings profile", () => UserConfigFile.SwitchProfile(next)),
				($"Could not delete {active}", () => UserConfigFile.DeleteProfile(active)),
			],
			done =>
			{
				if (done == 0)
				{
					Refresh();
					return;
				}

				bool deleted = done == 2;
				AfterSwitch(deleted ? "Settings profile deleted" : "Settings profile switched", deleted
					? $"{active} is deleted. FE-Buddy now uses {ActiveProfile}."
					: $"FE-Buddy now uses {ActiveProfile}.");
			});
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

	/// <summary>
	/// Does a profile change's file work off the UI thread - the profile's files can be slow to answer
	/// (OneDrive, a network profile) - one step after another until one fails, telling the user why. Then,
	/// on the UI thread, <paramref name="then"/> takes how many steps were done, to have the pages follow.
	/// Nothing else can change the profiles meanwhile.
	/// </summary>
	/// <param name="steps">Each step's message should it fail, and the step.</param>
	/// <param name="then">What follows, given how many steps were done.</param>
	private void Run(IReadOnlyList<(string Failure, Action Step)> steps, Action<int> then)
	{
		_busy = true;
		CommandManager.InvalidateRequerySuggested();

		_working = Task.Run(() =>
			{
				for (int i = 0; i < steps.Count; i++)
				{
					try
					{
						steps[i].Step();
					}
					catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
					{
						return (Done: i, Error: ex);
					}
				}

				return (Done: steps.Count, Error: (Exception?)null);
			})
			.ContinueWith(
				work => _dispatcher.InvokeAsync(() =>
				{
					_busy = false;
					(int done, Exception? error) = work.Result;

					if (error is not null)
					{
						AppLog.Warning(LogSource, $"{steps[done].Failure}: {error.Message}");
						Toast.Error(steps[done].Failure, error.Message);
					}

					then(done);
					CommandManager.InvalidateRequerySuggested();
				}).Task,
				CancellationToken.None,
				TaskContinuationOptions.None,
				TaskScheduler.Default)
			.Unwrap();
	}
}
