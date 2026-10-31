using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers the settings files' layout check (<see cref="UserConfigFile.CheckLayouts"/>): a file a newer FE-Buddy
/// saved is set aside and a backup this one reads put back, files in an older layout are brought forward
/// together - a step moving a setting between a profile and <c>Shared.json</c> - and what a release can't read is
/// found, for going back to it.
/// </summary>
[Collection("AppLog")]
public sealed class UserConfigLayoutCheckTests : IDisposable
{
	private const string Placement = "General.Placement";

	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_UserConfigLayouts_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points <see cref="UserConfigFile"/> and <see cref="AppLog"/> at throwaway directories.</summary>
	public UserConfigLayoutCheckTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_directory, "logs"));
		UserConfigFile.ConfigureForTesting(_directory);
		Directory.CreateDirectory(UserConfigFile.Directory);
	}

	/// <summary>Restores defaults and deletes the throwaway directory.</summary>
	public void Dispose()
	{
		UserConfigMigrations.ConfigureForTesting(null, null);
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_directory, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	// ---- a file a newer FE-Buddy saved ----

	/// <summary>
	/// A profile a newer FE-Buddy saved is moved to Reverted with its undo snapshot, its newest backup this one
	/// reads put back and read; the GUI is told once.
	/// </summary>
	[Fact]
	public void a_profile_a_newer_fe_buddy_saved_is_set_aside_and_its_backup_put_back()
	{
		const string Newer = """{ "ConfigVersion": 3, "General": { "Facility": "ZNY" } }""";
		File.WriteAllText(UserConfigFile.ConfigFilePath, Newer);
		File.WriteAllText(UserConfigFile.BroughtForwardFilePath(2), """{ "ConfigVersion": 2, "General": { "Facility": "ZOB" } }""");
		File.WriteAllText(UserConfigFile.PreviousFilePath, """{ "General": { "Facility": "ZNY" } }""");
		int raised = 0;
		void OnSetAside(object? sender, EventArgs e) => raised++;
		UserConfigFile.FilesSetAside += OnSetAside;

		try
		{
			Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		}
		finally
		{
			UserConfigFile.FilesSetAside -= OnSetAside;
		}

		Assert.Equal("ZOB", UserConfigFile.GetValue(UserConfigKeys.Facility));
		Assert.Equal(2, Stamp(UserConfigFile.ConfigFilePath));
		Assert.True(File.Exists(UserConfigFile.BroughtForwardFilePath(2)));
		Assert.False(File.Exists(UserConfigFile.PreviousFilePath));

		UserConfigSetAside setAside = Assert.Single(UserConfigFile.TakeSetAside());
		Assert.Equal(UserConfigFile.RevertedDirectory, Path.GetDirectoryName(setAside.Folder));
		Assert.Equal([new UserConfigSetAsideFile("Default", 3, 2)], setAside.Files);
		Assert.Equal(Newer, File.ReadAllText(Path.Combine(setAside.Folder, "UserConfig.Default.json")));
		Assert.True(File.Exists(Path.Combine(setAside.Folder, "UserConfig-previous.Default.json")));
		Assert.Equal(1, raised);
		Assert.Empty(UserConfigFile.TakeSetAside());
	}

	/// <summary>With no backup this FE-Buddy reads, the profile starts on its defaults, still in the list.</summary>
	[Fact]
	public void with_no_backup_a_profile_starts_on_its_defaults_and_keeps_its_place()
	{
		File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "ConfigVersion": 3, "General": { "Facility": "ZNY" } }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "Facility": "ZNY" } }""");

		UserConfigFile.ReadAll();

		Assert.Null(UserConfigFile.GetValue(UserConfigKeys.Facility));
		Assert.Equal(2, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Equal(["Default", "ZNY"], UserConfigFile.Profiles());
		Assert.Equal([new UserConfigSetAsideFile("Default", 3, null)], Assert.Single(UserConfigFile.TakeSetAside()).Files);
	}

	/// <summary>
	/// At launch every profile is checked, not just the one in use, and Shared.json too: each goes back to its
	/// backup, all in one folder and one notice. A profile this FE-Buddy reads is left alone.
	/// </summary>
	[Fact]
	public void launch_sets_aside_every_profile_and_shared_json_a_newer_fe_buddy_saved()
	{
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 3, "ActiveProfile": "ZOB", "General": { "UpdateChannel": "Beta" } }""");
		File.WriteAllText(UserConfigFile.SharedBroughtForwardFilePath(2), """{ "ConfigVersion": 2, "ActiveProfile": "ZOB", "General": { "UpdateChannel": "Alpha" } }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZOB"), """{ "ConfigVersion": 4, "General": { "Facility": "ZNY" } }""");
		File.WriteAllText(Path.Combine(UserConfigFile.Directory, "UserConfig-v2.ZOB.json"), """{ "ConfigVersion": 2, "General": { "Facility": "ZOB" } }""");
		const string Readable = """{ "ConfigVersion": 2, "General": { "Facility": "ZDC" } }""";
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZDC"), Readable);

		UserConfigFile.CheckLayouts();

		UserConfigSetAside setAside = Assert.Single(UserConfigFile.TakeSetAside());
		Assert.Equal([new UserConfigSetAsideFile("ZOB", 4, 2), new UserConfigSetAsideFile(null, 3, 2)], setAside.Files);
		Assert.True(File.Exists(Path.Combine(setAside.Folder, "Shared.json")));
		Assert.Equal(Readable, File.ReadAllText(UserConfigFile.ProfileFilePath("ZDC")));

		UserConfigFile.ReadAll();
		Assert.Equal("ZOB", UserConfigFile.ActiveProfile);
		Assert.Equal("ZOB", UserConfigFile.GetValue(UserConfigKeys.Facility));
		Assert.Equal("Alpha", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith(
			"Settings a newer FE-Buddy saved can't be read by this one, so they were set aside", StringComparison.Ordinal));
	}

	/// <summary>A Shared.json with no backup starts on its defaults, but keeps the profile in use.</summary>
	[Fact]
	public void a_newer_shared_json_with_no_backup_keeps_the_profile_in_use()
	{
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 3, "ActiveProfile": "ZNY", "General": { "UpdateChannel": "Beta" } }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("Default"), """{ "ConfigVersion": 2 }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "Facility": "ZNY" } }""");

		UserConfigFile.ReadAll();

		Assert.Equal("ZNY", UserConfigFile.ActiveProfile);
		Assert.Null(UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
		Assert.Equal(2, Stamp(UserConfigFile.SharedFilePath));
		Assert.Equal([new UserConfigSetAsideFile(null, 3, null)], Assert.Single(UserConfigFile.TakeSetAside()).Files);
	}

	/// <summary>Each check sets its files aside in a folder of its own: an earlier one is never written over.</summary>
	[Fact]
	public void each_check_sets_its_files_aside_in_a_folder_of_its_own()
	{
		File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "ConfigVersion": 3, "General": { "Facility": "ONE" } }""");
		UserConfigFile.ReadAll();
		File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "ConfigVersion": 3, "General": { "Facility": "TWO" } }""");
		UserConfigFile.ReadAll();

		IReadOnlyList<UserConfigSetAside> setAside = UserConfigFile.TakeSetAside();

		Assert.Equal(2, setAside.Count);
		Assert.NotEqual(setAside[0].Folder, setAside[1].Folder);
		Assert.Contains("ONE", File.ReadAllText(Path.Combine(setAside[0].Folder, "UserConfig.Default.json")), StringComparison.Ordinal);
		Assert.Contains("TWO", File.ReadAllText(Path.Combine(setAside[1].Folder, "UserConfig.Default.json")), StringComparison.Ordinal);
	}

	/// <summary>A newer file that can't be moved is used as it is and keeps its stamp; the next read sets it aside.</summary>
	[Fact]
	public void a_newer_file_that_cannot_be_moved_is_used_as_it_is_until_it_can_be()
	{
		File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "ConfigVersion": 3, "General": { "Facility": "ZNY" } }""");

		using (new FileStream(UserConfigFile.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			UserConfigFile.ReadAll();
		}

		Assert.Equal("ZNY", UserConfigFile.GetValue(UserConfigKeys.Facility));
		Assert.Empty(UserConfigFile.TakeSetAside());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains(
			"'UserConfig.Default.json' was saved by a newer FE-Buddy, but could not be set aside", StringComparison.Ordinal));

		UserConfigFile.ReadAll();

		Assert.Single(UserConfigFile.TakeSetAside());
		Assert.Null(UserConfigFile.GetValue(UserConfigKeys.Facility));
	}

	// ---- files in an older layout ----

	/// <summary>Shared.json and every profile in an older layout are brought forward together, each kept as it was.</summary>
	[Fact]
	public void older_files_are_brought_forward_together_each_kept_as_it_was()
	{
		UserConfigMigrations.ConfigureForTesting(3,
		[
			new UserConfigMigration(3, "Renamed two settings", settings =>
			{
				settings.Move("General.OldFacility", UserConfigKeys.Facility);
				settings.Move("General.Channel", UserConfigKeys.UpdateChannel);
			}),
		]);
		const string SharedFile = """{ "ConfigVersion": 2, "ActiveProfile": "ZOB", "General": { "Channel": "Beta" } }""";
		const string ZobFile = """{ "ConfigVersion": 2, "General": { "OldFacility": "ZOB" } }""";
		const string ZnyFile = """{ "ConfigVersion": 2, "General": { "OldFacility": "ZNY" } }""";
		File.WriteAllText(UserConfigFile.SharedFilePath, SharedFile);
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZOB"), ZobFile);
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), ZnyFile);

		UserConfigFile.CheckLayouts();

		Assert.Equal(3, Stamp(UserConfigFile.SharedFilePath));
		Assert.Equal(3, Stamp(UserConfigFile.ProfileFilePath("ZOB")));
		Assert.Equal(3, Stamp(UserConfigFile.ProfileFilePath("ZNY")));
		Assert.Equal(SharedFile, File.ReadAllText(UserConfigFile.SharedBroughtForwardFilePath(2)));
		Assert.Equal(ZobFile, File.ReadAllText(Path.Combine(UserConfigFile.Directory, "UserConfig-v2.ZOB.json")));
		Assert.Equal(ZnyFile, File.ReadAllText(Path.Combine(UserConfigFile.Directory, "UserConfig-v2.ZNY.json")));
		Assert.Equal("ZNY", Setting(UserConfigFile.ProfileFilePath("ZNY"), "General", "Facility"));
		Assert.Equal("Beta", Setting(UserConfigFile.SharedFilePath, "General", "UpdateChannel"));
		Assert.Equal("ZOB", JsonNode.Parse(File.ReadAllText(UserConfigFile.SharedFilePath))!["ActiveProfile"]!.GetValue<string>());
	}

	/// <summary>
	/// A step can move a setting out of the profiles into Shared.json: the profile in use is brought forward
	/// first, so its value is the one kept, and every profile's own copy goes.
	/// </summary>
	[Fact]
	public void a_step_can_move_a_setting_from_the_profiles_into_shared_json()
	{
		UserConfigMigrations.ConfigureForTesting(3,
			[new UserConfigMigration(3, "The window's place is this PC's", settings => settings.MoveToShared(Placement, UserConfigKeys.MainWindowPlacement))]);
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 2, "ActiveProfile": "ZNY" }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("Default"), """{ "ConfigVersion": 2, "General": { "Placement": "default's" } }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "Placement": "ZNY's" } }""");

		UserConfigFile.CheckLayouts();

		Assert.Equal("ZNY's", Setting(UserConfigFile.SharedFilePath, "General", "MainWindowPlacement"));
		Assert.Null(Setting(UserConfigFile.ProfileFilePath("Default"), "General", "Placement"));
		Assert.Null(Setting(UserConfigFile.ProfileFilePath("ZNY"), "General", "Placement"));

		UserConfigFile.ReadAll();
		Assert.Equal("ZNY's", UserConfigFile.GetValue(UserConfigKeys.MainWindowPlacement));
	}

	/// <summary>
	/// A step can hand a setting in Shared.json to every profile: each gets a copy unless it has its own, and the
	/// step's shared part takes it out of Shared.json once they all have.
	/// </summary>
	[Fact]
	public void a_step_can_hand_a_shared_setting_to_every_profile()
	{
		UserConfigMigrations.ConfigureForTesting(3, [HandOutPlacement]);
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 2, "ActiveProfile": "Default", "General": { "MainWindowPlacement": "shared" } }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("Default"), """{ "ConfigVersion": 2 }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "Placement": "ZNY's" } }""");

		UserConfigFile.CheckLayouts();

		Assert.Equal("shared", Setting(UserConfigFile.ProfileFilePath("Default"), "General", "Placement"));
		Assert.Equal("ZNY's", Setting(UserConfigFile.ProfileFilePath("ZNY"), "General", "Placement"));
		Assert.Null(Setting(UserConfigFile.SharedFilePath, "General", "MainWindowPlacement"));
	}

	/// <summary>
	/// A profile behind Shared.json - copied in by hand once Shared.json had handed the setting out - sees
	/// Shared.json as it is now: the setting is gone, so the profile gets its default.
	/// </summary>
	[Fact]
	public void a_profile_behind_shared_json_sees_it_as_it_is_now()
	{
		UserConfigMigrations.ConfigureForTesting(3, [HandOutPlacement]);
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 3, "ActiveProfile": "ZNY" }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "Facility": "ZNY" } }""");

		UserConfigFile.ReadAll();

		Assert.Equal(3, Stamp(UserConfigFile.ProfileFilePath("ZNY")));
		Assert.Equal("ZNY", UserConfigFile.GetValue(UserConfigKeys.Facility));
		Assert.Null(UserConfigFile.GetValue(Placement));
	}

	// ---- going back to an older release ----

	/// <summary>The profiles and Shared.json a release can't read are those saved in a layout newer than its own.</summary>
	[Fact]
	public void unreadable_by_names_the_profiles_and_shared_json_a_release_cannot_read()
	{
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 3, "ActiveProfile": "ZOB" }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZOB"), """{ "ConfigVersion": 3 }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2 }""");
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZDC"), """{ "ConfigVersion": 4 }""");

		(IReadOnlyList<string> profiles, bool shared) = UserConfigFile.UnreadableBy("3.0.0");

		Assert.Equal(["ZDC", "ZOB"], profiles);
		Assert.True(shared);
		(IReadOnlyList<string> beforeUserConfig, bool sharedBeforeUserConfig) = UserConfigFile.UnreadableBy("2.9.3");
		Assert.Empty(beforeUserConfig);
		Assert.False(sharedBeforeUserConfig);
		Assert.Equal(3, Stamp(UserConfigFile.ProfileFilePath("ZOB")));
	}

	/// <summary>A profile not in use is read from its file, brought to the current layout in memory only.</summary>
	[Fact]
	public void read_profile_reads_one_not_in_use_in_the_current_layout()
	{
		UserConfigMigrations.ConfigureForTesting(3,
			[new UserConfigMigration(3, "Renamed the facility", settings => settings.Move("General.OldFacility", UserConfigKeys.Facility))]);
		File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), """{ "ConfigVersion": 2, "General": { "OldFacility": "ZNY" } }""");

		IReadOnlyDictionary<string, string> settings = UserConfigFile.ReadProfile("zny");

		Assert.Equal("ZNY", settings[UserConfigKeys.Facility]);
		Assert.Equal(2, Stamp(UserConfigFile.ProfileFilePath("ZNY")));
		Assert.Throws<ArgumentException>(() => UserConfigFile.ReadProfile("ZDC"));
	}

	/// <summary>Hands Shared.json's window placement to each profile, then takes it out of Shared.json.</summary>
	private static readonly UserConfigMigration HandOutPlacement =
		new(3, "The window's place is each profile's", settings => settings.CopyFromShared(UserConfigKeys.MainWindowPlacement, Placement))
		{
			ThenShared = shared => shared.Remove(UserConfigKeys.MainWindowPlacement),
		};

	private static int? Stamp(string path) =>
		JsonNode.Parse(File.ReadAllText(path))![UserConfigVersion.FileKey]?.GetValue<int>();

	private static string? Setting(string path, string section, string key) =>
		JsonNode.Parse(File.ReadAllText(path))![section]?[key]?.GetValue<string>();
}
