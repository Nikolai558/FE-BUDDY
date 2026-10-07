using System.Windows.Threading;

using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Settings Profile card (<see cref="SettingsProfilesViewModel"/>) and the name question
/// it asks (<see cref="ProfileNameViewModel"/>): choosing a profile switches to it - asking first when
/// unsaved edits would be lost - and New…, Rename… and Delete… do what they say, every page reading
/// the settings again after a switch. Against a throwaway config, with the windows stood in for.
/// </summary>
[Collection("AppLog")]
public sealed class SettingsProfilesViewModelTests : IDisposable
{
	private const string Artcc = "Services.AiracService.UserArtccId";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_ProfileCard_" + Guid.NewGuid().ToString("N"));
	private readonly List<string> _asked = [];
	private IReadOnlyList<string> _unsaved = [];
	private bool _answer = true;
	private Func<ProfileNameViewModel, bool> _name = _ => false;
	private int _reloads;

	/// <summary>Points the config and the log at a throwaway folder, with two profiles.</summary>
	public SettingsProfilesViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(_root);

		UserConfigFile.TrySetValue(Artcc, "ZOB");
		UserConfigFile.Write();
		UserConfigFile.CreateProfile("ZNY", new Dictionary<string, string> { [Artcc] = "ZNY" });
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	private SettingsProfilesViewModel Card() => new(
		(title, message, confirmText) =>
		{
			_asked.Add($"{title}|{message}|{confirmText}");
			return _answer;
		},
		question => _name(question),
		() => _unsaved,
		() =>
		{
			_reloads++;
			return [];
		});

	private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

	[Fact]
	public void the_card_lists_the_profiles_and_shows_the_one_in_use() => StaThread.Run(() =>
	{
		SettingsProfilesViewModel card = Card();

		Assert.Equal(["Default", "ZNY"], card.Profiles);
		Assert.Equal("Default", card.SelectedProfile);
		Assert.Equal("Default", card.ActiveProfile);
		Assert.True(card.CanDelete);
		Assert.Equal($"Saved in `{UserConfigFile.ProfileFilePath("Default")}`.", card.FileNote);
	});

	/// <summary>Choosing another profile switches to it once the drop-down has closed, and every page reads it.</summary>
	[Fact]
	public void choosing_a_profile_switches_to_it() => StaThread.Run(() =>
	{
		SettingsProfilesViewModel card = Card();

		card.SelectedProfile = "ZNY";
		Assert.Equal("Default", UserConfigFile.ActiveProfile);

		Pump();

		Assert.Equal("ZNY", UserConfigFile.ActiveProfile);
		Assert.Equal("ZNY", UserConfigFile.GetValue(Artcc));
		Assert.Equal("ZNY", card.SelectedProfile);
		Assert.Equal(1, _reloads);
		Assert.Empty(_asked);
		Assert.Contains(Toast.Items, t => t.Title == "Settings profile switched");
	});

	/// <summary>The list going empty, or choosing the profile in use, is no choice.</summary>
	[Fact]
	public void nothing_and_the_profile_in_use_switch_nothing() => StaThread.Run(() =>
	{
		SettingsProfilesViewModel card = Card();

		card.SelectedProfile = null;
		card.SelectedProfile = "Default";
		card.SwitchTo("default");
		Pump();

		Assert.Equal("Default", card.SelectedProfile);
		Assert.Equal(0, _reloads);
	});

	/// <summary>With unsaved edits it asks first; saying no leaves the profile and puts the list back.</summary>
	[Fact]
	public void unsaved_edits_are_asked_about_and_no_puts_the_list_back() => StaThread.Run(() =>
	{
		_unsaved = ["Settings", "Airways"];
		_answer = false;
		SettingsProfilesViewModel card = Card();

		card.SelectedProfile = "ZNY";
		Pump();

		Assert.Equal("Default", UserConfigFile.ActiveProfile);
		Assert.Equal("Default", card.SelectedProfile);
		Assert.Equal(0, _reloads);
		Assert.Equal("Switch settings profile|Settings and Airways have unsaved changes, which will be lost. Save them first to keep them.|Switch to ZNY", Assert.Single(_asked));

		_answer = true;
		card.SwitchTo("ZNY");
		Assert.Equal("ZNY", UserConfigFile.ActiveProfile);
	});

	[Fact]
	public void a_profile_that_has_gone_is_not_switched_to() => StaThread.Run(() =>
	{
		SettingsProfilesViewModel card = Card();
		File.Delete(UserConfigFile.ProfileFilePath("ZNY"));

		card.SwitchTo("ZNY");

		Assert.Equal("Default", UserConfigFile.ActiveProfile);
		Assert.Equal(["Default"], card.Profiles);
		Assert.False(card.CanDelete);
		Assert.Contains(Toast.Items, t => t.Title == "Could not switch settings profile");
	});

	/// <summary>New… makes the profile - a copy, or every default - and switches to it.</summary>
	[Theory]
	[InlineData(true, "ZOB")]
	[InlineData(false, null)]
	public void new_makes_a_profile_and_switches_to_it(bool copy, string? artcc) => StaThread.Run(() =>
	{
		_unsaved = ["Settings"];
		ProfileNameViewModel? asked = null;
		_name = question =>
		{
			asked = question;
			question.Name = "  ZOB Test ";
			question.CopyCurrent = copy;
			return true;
		};
		SettingsProfilesViewModel card = Card();

		card.NewCommand.Execute(null);

		Assert.Equal("New Settings Profile", asked!.Heading);
		Assert.Equal("Default", asked.CopyFrom);
		Assert.Equal("Unsaved changes on Settings will be lost as FE-Buddy switches to it.", asked.UnsavedNote);
		Assert.Equal("ZOB Test", UserConfigFile.ActiveProfile);
		Assert.Equal(artcc, UserConfigFile.GetValue(Artcc));
		Assert.Equal(["Default", "ZNY", "ZOB Test"], card.Profiles);
		Assert.Equal(1, _reloads);
	});

	[Fact]
	public void cancelling_a_question_changes_nothing() => StaThread.Run(() =>
	{
		SettingsProfilesViewModel card = Card();

		card.NewCommand.Execute(null);
		card.RenameCommand.Execute(null);
		_answer = false;
		card.DeleteCommand.Execute(null);

		Assert.Equal(["Default", "ZNY"], UserConfigFile.Profiles());
		Assert.Equal("Default", UserConfigFile.ActiveProfile);
		Assert.Equal(0, _reloads);
	});

	[Fact]
	public void rename_renames_the_profile_in_use() => StaThread.Run(() =>
	{
		_name = question =>
		{
			Assert.False(question.CanConfirm);
			Assert.False(question.OffersCopy);
			question.Name = "ZOB";
			return true;
		};
		SettingsProfilesViewModel card = Card();

		card.RenameCommand.Execute(null);

		Assert.Equal("ZOB", UserConfigFile.ActiveProfile);
		Assert.Equal(["ZNY", "ZOB"], card.Profiles);
		Assert.Equal("ZOB", card.SelectedProfile);
		Assert.Equal(0, _reloads);
	});

	/// <summary>Delete… switches to Default, or else the first other profile, then deletes the one that was in use.</summary>
	[Fact]
	public void delete_switches_away_then_deletes() => StaThread.Run(() =>
	{
		UserConfigFile.CreateProfile("AAA", null);
		UserConfigFile.SwitchProfile("ZNY");
		SettingsProfilesViewModel card = Card();

		card.DeleteCommand.Execute(null);

		Assert.Equal("Default", UserConfigFile.ActiveProfile);
		Assert.Equal(["AAA", "Default"], card.Profiles);
		Assert.StartsWith("Delete settings profile|Delete the ZNY profile? FE-Buddy switches to Default", Assert.Single(_asked), StringComparison.Ordinal);

		card.DeleteCommand.Execute(null);

		Assert.Equal("AAA", UserConfigFile.ActiveProfile);
		Assert.Equal(["AAA"], card.Profiles);
		Assert.False(card.CanDelete);
		Assert.False(card.DeleteCommand.CanExecute(null));
		Assert.Equal(2, _reloads);
	});

	// ============================ the name question ============================

	[Theory]
	[InlineData("", null, false)]
	[InlineData("   ", null, false)]
	[InlineData("ZOB", null, true)]
	[InlineData(" zny ", "There is already a profile called ZNY.", false)]
	[InlineData("a/b", @"A profile name can't contain \ / : * ? "" < > |.", false)]
	public void a_new_name_must_be_usable_and_not_taken(string name, string? problem, bool canConfirm)
	{
		ProfileNameViewModel question = new("New", "d", "Create", ["Default", "ZNY"], renaming: null, copyFrom: "Default", unsavedPages: []) { Name = name };

		Assert.Equal(problem, question.Problem);
		Assert.Equal(problem is not null, question.HasProblem);
		Assert.Equal(canConfirm, question.CanConfirm);
		Assert.Equal(canConfirm, question.ConfirmCommand.CanExecute(null));
		Assert.Equal("Start with a copy of Default's settings", question.CopyLabel);
		Assert.True(question.CopyCurrent);
		Assert.False(question.HasUnsavedNote);
	}

	/// <summary>Renaming, the profile may keep its name in another case, but not exactly as it is.</summary>
	[Theory]
	[InlineData("ZNY", false)]
	[InlineData("zny", true)]
	[InlineData("Default", false)]
	public void a_rename_may_change_only_the_case(string name, bool canConfirm)
	{
		ProfileNameViewModel question = new("Rename", "d", "Rename", ["Default", "ZNY"], renaming: "ZNY", copyFrom: null, unsavedPages: []);

		Assert.Equal("ZNY", question.Name);
		question.Name = name;

		Assert.Equal(canConfirm, question.CanConfirm);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void either_button_closes_the_name_question_and_says_which(bool confirm)
	{
		ProfileNameViewModel question = new("New", "d", "Create", [], renaming: null, copyFrom: null, unsavedPages: []) { Name = "ZOB" };
		int closes = 0;
		question.CloseRequested += (_, _) => closes++;

		(confirm ? question.ConfirmCommand : question.CancelCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(confirm, question.Confirmed);
		Assert.Throws<ArgumentNullException>(() => new ProfileNameViewModel("h", "d", "c", null!, null, null, []));
		Assert.Throws<ArgumentNullException>(() => new ProfileNameViewModel("h", "d", "c", [], null, null, null!));
	}
}
