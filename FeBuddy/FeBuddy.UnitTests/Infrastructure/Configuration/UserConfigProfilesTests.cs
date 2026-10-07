using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers settings profiles (issue #324): the settings every profile shares kept in
/// <c>Shared.json</c>, the rest in the profile's own file; an older FE-Buddy's <c>UserConfig.json</c>
/// copied in as the Default profile; and creating, switching, renaming and deleting profiles - against
/// a throwaway folder standing for <c>%APPDATA%\FE-Buddy</c>.
/// </summary>
[Collection("AppLog")]
public sealed class UserConfigProfilesTests : IDisposable
{
	private const string Channel = "General.UpdateChannel";
	private const string News = "General.NewsLastOpen";
	private const string Artcc = "Services.AiracService.UserArtccId";
	private const string Pretty = "General.PrettyPrintGeojson";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Profiles_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the settings and the log at a throwaway folder.</summary>
	public UserConfigProfilesTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(_root);
	}

	/// <summary>Restores the real settings and log, and deletes the folder.</summary>
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

	private static JsonNode? File(string path) => System.IO.File.Exists(path) ? JsonNode.Parse(System.IO.File.ReadAllText(path)) : null;

	private static string? At(JsonNode? root, string dottedPath)
	{
		JsonNode? node = root;

		foreach (string segment in dottedPath.Split('.'))
		{
			node = node?[segment];
		}

		return node?.GetValue<string>();
	}

	private string ProfilesFolder => Path.Combine(_root, UserConfigFile.ProfilesFolderName);

	private void WriteRoot(string fileName, string json)
	{
		Directory.CreateDirectory(_root);
		System.IO.File.WriteAllText(Path.Combine(_root, fileName), json);
	}

	// ---- one set of settings, two files ----

	[Fact]
	public void the_shared_settings_go_in_shared_json_and_the_rest_in_the_profile()
	{
		UserConfigFile.TrySetValue(Channel, "Beta");
		UserConfigFile.TrySetValue(Artcc, "ZOB");
		UserConfigFile.Write();

		Assert.Equal(Path.Combine(ProfilesFolder, "UserConfig.Default.json"), UserConfigFile.ConfigFilePath);
		Assert.Equal("Beta", At(File(UserConfigFile.SharedFilePath), Channel));
		Assert.Equal("Default", At(File(UserConfigFile.SharedFilePath), "ActiveProfile"));
		Assert.Null(At(File(UserConfigFile.ConfigFilePath), Channel));
		Assert.Equal("ZOB", At(File(UserConfigFile.ConfigFilePath), Artcc));

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));
		Assert.Equal("ZOB", UserConfigFile.GetValue(Artcc));
	}

	/// <summary>Saving one node writes the shared settings under it, and only those.</summary>
	[Fact]
	public void saving_a_node_writes_only_the_shared_settings_under_it()
	{
		UserConfigFile.TrySetValue(Channel, "Beta");
		UserConfigFile.Save("Services.AiracService");

		Assert.False(System.IO.File.Exists(UserConfigFile.SharedFilePath));

		// A save reads the files back, so the unsaved change went with it.
		UserConfigFile.TrySetValue(Channel, "Beta");
		UserConfigFile.TrySetValue(Pretty, "Y");
		UserConfigFile.Save("General");

		Assert.Equal("Beta", At(File(UserConfigFile.SharedFilePath), Channel));
		Assert.Equal("Y", At(File(UserConfigFile.ConfigFilePath), Pretty));
		Assert.Null(At(File(UserConfigFile.ConfigFilePath), Channel));

		UserConfigFile.RemoveValues(Channel);
		UserConfigFile.Save(Channel);

		Assert.Null(At(File(UserConfigFile.SharedFilePath), Channel));
		Assert.Null(File(UserConfigFile.SharedFilePath)!["General"]);
	}

	/// <summary>A profile that still holds a shared setting (written before Shared.json, or by hand) gives it to Shared.json; Shared.json's own win.</summary>
	[Fact]
	public void a_profiles_copy_of_a_shared_setting_moves_to_shared_json_unless_it_has_one()
	{
		Directory.CreateDirectory(ProfilesFolder);
		System.IO.File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "General": { "UpdateChannel": "Alpha", "NewsLastOpen": "2026-01-01.1" } }""");
		System.IO.File.WriteAllText(UserConfigFile.SharedFilePath, """{ "General": { "UpdateChannel": "Stable" } }""");

		UserConfigFile.ReadAll();

		Assert.Equal("Stable", UserConfigFile.GetValue(Channel));
		Assert.Equal("2026-01-01.1", UserConfigFile.GetValue(News));
		Assert.Equal("2026-01-01.1", At(File(UserConfigFile.SharedFilePath), News));
		Assert.Equal("Stable", At(File(UserConfigFile.SharedFilePath), Channel));

		// The profile's copies go at its next save.
		UserConfigFile.Save("General");
		Assert.Null(File(UserConfigFile.ConfigFilePath)!["General"]);
	}

	/// <summary>A profile that can't be read still leaves the shared settings, so a save can't wipe them.</summary>
	[Fact]
	public void an_unreadable_profile_keeps_the_shared_settings()
	{
		Directory.CreateDirectory(ProfilesFolder);
		System.IO.File.WriteAllText(UserConfigFile.SharedFilePath, """{ "General": { "UpdateChannel": "Beta" } }""");
		System.IO.File.WriteAllText(UserConfigFile.ConfigFilePath, "{ not json");

		Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));

		System.IO.File.WriteAllText(UserConfigFile.ConfigFilePath, "[1]");
		Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));

		System.IO.File.Delete(UserConfigFile.ConfigFilePath);
		Assert.Equal(UserConfigReadResult.Missing, UserConfigFile.ReadAll());
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));
	}

	[Fact]
	public void an_undo_never_puts_a_shared_setting_back_in_the_profile()
	{
		UserConfigFile.TrySetValue(Pretty, "N");
		UserConfigFile.Save("General");
		System.IO.File.WriteAllText(UserConfigFile.PreviousFilePath, """{ "General": { "UpdateChannel": "Alpha", "PrettyPrintGeojson": "Y" } }""");

		Assert.True(UserConfigFile.Undo("General"));

		Assert.Equal("Y", At(File(UserConfigFile.ConfigFilePath), Pretty));
		Assert.Null(At(File(UserConfigFile.ConfigFilePath), Channel));
	}

	// ---- an older FE-Buddy's file ----

	/// <summary>It is copied, so an older FE-Buddy rolled back to still finds its settings.</summary>
	[Fact]
	public void an_older_fe_buddys_settings_file_is_copied_in_as_the_default_profile()
	{
		WriteRoot("UserConfig.json", """{ "ConfigVersion": 1, "General": { "UpdateChannel": "Beta", "PrettyPrintGeojson": "Y" } }""");
		WriteRoot("UserConfig.previous.json", "{}");
		WriteRoot("UserConfig.before-import.json", "{}");
		WriteRoot("UserConfig.v1.json", "{}");
		Assert.True(UserConfigFile.HasSettings);

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("Default", UserConfigFile.ActiveProfile);
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));
		Assert.Equal("Y", UserConfigFile.GetValue(Pretty));
		Assert.Equal("Beta", At(File(UserConfigFile.SharedFilePath), Channel));
		Assert.Equal(
			["Shared.json", "UserConfig-before-import.Default.json", "UserConfig-previous.Default.json", "UserConfig-v1.Default.json", "UserConfig.Default.json"],
			Directory.GetFiles(ProfilesFolder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
		Assert.Equal(4, Directory.GetFiles(_root, "UserConfig*.json").Length);
		Assert.Contains("Beta", System.IO.File.ReadAllText(Path.Combine(_root, "UserConfig.json")), StringComparison.Ordinal);
	}

	[Fact]
	public void an_older_file_that_cannot_be_copied_is_tried_again_next_time()
	{
		WriteRoot("UserConfig.json", """{ "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""");

		using (new FileStream(Path.Combine(_root, "UserConfig.json"), FileMode.Open, FileAccess.Read, FileShare.None))
		{
			Assert.Equal(UserConfigReadResult.Missing, UserConfigFile.ReadAll());
		}

		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith($"Could not copy '{Path.Combine(_root, "UserConfig.json")}'", StringComparison.Ordinal));

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		Assert.Equal("ZOB", UserConfigFile.GetValue(Artcc));
	}

	/// <summary>Once there are profiles, an older FE-Buddy's file (one run again, say) is left alone.</summary>
	[Fact]
	public void an_older_file_is_left_alone_once_there_are_profiles()
	{
		UserConfigFile.TrySetValue(Artcc, "ZOB");
		UserConfigFile.Write();
		WriteRoot("UserConfig.json", """{ "Services": { "AiracService": { "UserArtccId": "ZNY" } } }""");

		UserConfigFile.ReadAll();

		Assert.Equal("ZOB", UserConfigFile.GetValue(Artcc));
		Assert.True(System.IO.File.Exists(Path.Combine(_root, "UserConfig.json")));
	}

	// ---- the profiles ----

	[Fact]
	public void a_new_profile_starts_from_a_copy_or_the_defaults_and_shares_the_shared_settings()
	{
		UserConfigFile.TrySetValue(Channel, "Beta");
		UserConfigFile.TrySetValue(Artcc, "ZOB");
		UserConfigFile.Write();

		UserConfigFile.CreateProfile(" ZOB Copy ", UserConfigFile.SnapshotValues());
		UserConfigFile.CreateProfile("Blank", null);

		Assert.Equal(["Blank", "Default", "ZOB Copy"], UserConfigFile.Profiles());
		Assert.Null(At(File(UserConfigFile.ProfileFilePath("ZOB Copy")), Channel));

		UserConfigFile.SwitchProfile("zob copy");
		Assert.Equal("ZOB Copy", UserConfigFile.ActiveProfile);
		Assert.Equal("ZOB", UserConfigFile.GetValue(Artcc));
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));

		UserConfigFile.SwitchProfile("Blank");
		Assert.Null(UserConfigFile.GetValue(Artcc));
		Assert.Equal("Beta", UserConfigFile.GetValue(Channel));
		Assert.Equal("Blank", At(File(UserConfigFile.SharedFilePath), "ActiveProfile"));

		// The next launch opens the profile last used.
		UserConfigFile.ConfigureForTesting(_root);
		UserConfigFile.ReadAll();
		Assert.Equal("Blank", UserConfigFile.ActiveProfile);
	}

	[Fact]
	public void a_name_another_profile_has_in_any_case_is_refused()
	{
		UserConfigFile.CreateProfile("ZOB", null);

		ArgumentException taken = Assert.Throws<ArgumentException>(() => UserConfigFile.CreateProfile("zob", null));
		Assert.StartsWith("There is already a settings profile called 'ZOB'.", taken.Message, StringComparison.Ordinal);
		Assert.Throws<ArgumentException>(() => UserConfigFile.CreateProfile("bad:name", null));
		Assert.Throws<ArgumentException>(() => UserConfigFile.SwitchProfile("Nobody"));
		Assert.True(UserConfigFile.ProfileExists(" zob "));
		Assert.False(UserConfigFile.ProfileExists("ZNY"));
	}

	[Theory]
	[InlineData("ZOB", null)]
	[InlineData(" New Procs 2 ", null)]
	[InlineData("ZOB-1.A", null)]
	[InlineData("", "Type a name for the profile.")]
	[InlineData(null, "Type a name for the profile.")]
	[InlineData("ZOB/ZNY", @"A profile name can't contain \ / : * ? "" < > |.")]
	[InlineData("ZOB\t1", @"A profile name can't contain \ / : * ? "" < > |.")]
	[InlineData("ZOB.", "A profile name can't end with a dot.")]
	[InlineData("con", "Windows keeps CON as a device name, so no file can have it. Choose another name.")]
	[InlineData("Lpt1.backup", "Windows keeps LPT1 as a device name, so no file can have it. Choose another name.")]
	public void a_profile_name_follows_windows_rules_for_a_file_name(string? name, string? problem) =>
		Assert.Equal(problem, UserConfigFile.ProfileNameProblem(name));

	[Fact]
	public void a_name_over_the_limit_is_refused()
	{
		Assert.Equal("Keep it to 64 characters or fewer.", UserConfigFile.ProfileNameProblem(new string('a', 65)));
		Assert.Null(UserConfigFile.ProfileNameProblem(new string('a', 64)));
	}

	/// <summary>A profile is renamed with its backups; the one in use stays in use under its new name.</summary>
	[Fact]
	public void renaming_moves_the_profile_and_its_backups()
	{
		UserConfigFile.TrySetValue(Artcc, "ZOB");
		UserConfigFile.Save("Services");
		UserConfigFile.Save("Services");
		Assert.True(System.IO.File.Exists(UserConfigFile.PreviousFilePath));

		UserConfigFile.RenameProfile("default", "ZOB");

		Assert.Equal("ZOB", UserConfigFile.ActiveProfile);
		Assert.Equal(["ZOB"], UserConfigFile.Profiles());
		Assert.True(System.IO.File.Exists(Path.Combine(ProfilesFolder, "UserConfig-previous.ZOB.json")));
		Assert.False(System.IO.File.Exists(Path.Combine(ProfilesFolder, "UserConfig-previous.Default.json")));
		Assert.Equal("ZOB", At(File(UserConfigFile.SharedFilePath), "ActiveProfile"));

		// Only its case.
		UserConfigFile.RenameProfile("ZOB", "zob");
		Assert.Equal(["zob"], UserConfigFile.Profiles());

		UserConfigFile.CreateProfile("ZNY", null);
		Assert.Throws<ArgumentException>(() => UserConfigFile.RenameProfile("ZNY", "ZOB"));
		UserConfigFile.RenameProfile("ZNY", "ZNY Old");
		Assert.Equal("zob", UserConfigFile.ActiveProfile);
		Assert.Equal(["ZNY Old", "zob"], UserConfigFile.Profiles());
	}

	/// <summary>A profile goes with its backups, never another's whose name ends the same way; the one in use can't go.</summary>
	[Fact]
	public void deleting_removes_a_profile_and_only_its_own_backups()
	{
		UserConfigFile.CreateProfile("B", null);
		UserConfigFile.CreateProfile("A.B", null);
		System.IO.File.WriteAllText(Path.Combine(ProfilesFolder, "UserConfig-previous.B.json"), "{}");
		System.IO.File.WriteAllText(Path.Combine(ProfilesFolder, "UserConfig-previous.A.B.json"), "{}");

		UserConfigFile.DeleteProfile("b");

		Assert.Equal(["A.B", "Default"], UserConfigFile.Profiles());
		Assert.False(System.IO.File.Exists(Path.Combine(ProfilesFolder, "UserConfig-previous.B.json")));
		Assert.True(System.IO.File.Exists(Path.Combine(ProfilesFolder, "UserConfig-previous.A.B.json")));

		UserConfigFile.Write();
		Assert.Throws<InvalidOperationException>(() => UserConfigFile.DeleteProfile("Default"));
		Assert.Throws<ArgumentException>(() => UserConfigFile.DeleteProfile("Nobody"));
	}

	/// <summary>When the profile Shared.json names has gone, Default is used, or else the first there is.</summary>
	[Fact]
	public void a_profile_that_has_gone_falls_back_to_default_or_the_first()
	{
		Directory.CreateDirectory(ProfilesFolder);
		System.IO.File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ActiveProfile": "Gone" }""");
		System.IO.File.WriteAllText(UserConfigFile.ProfileFilePath("ZNY"), "{}");
		System.IO.File.WriteAllText(UserConfigFile.ProfileFilePath("ZAU"), "{}");

		UserConfigFile.ReadAll();
		Assert.Equal("ZAU", UserConfigFile.ActiveProfile);

		System.IO.File.WriteAllText(UserConfigFile.ProfileFilePath("Default"), "{}");
		UserConfigFile.ReadAll();
		Assert.Equal("Default", UserConfigFile.ActiveProfile);

		// A name that can't be a profile's is taken as Default too.
		System.IO.File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ActiveProfile": "a/b" }""");
		UserConfigFile.ReadAll();
		Assert.Equal("Default", UserConfigFile.ActiveProfile);
	}

	/// <summary>Before any profile has a file, the one in use is listed all the same.</summary>
	[Fact]
	public void with_no_file_yet_the_profile_in_use_is_still_listed()
	{
		Assert.False(UserConfigFile.HasSettings);
		Assert.Equal(["Default"], UserConfigFile.Profiles());
	}

	/// <summary>The profile in use gets its file before another is made or it is renamed, so it is never lost.</summary>
	[Fact]
	public void the_profile_in_use_is_written_before_another_is_made_or_it_is_renamed()
	{
		UserConfigFile.CreateProfile("ZOB", null);

		Assert.True(System.IO.File.Exists(UserConfigFile.ProfileFilePath("Default")));
		UserConfigFile.SwitchProfile("ZOB");
		Assert.Equal(["Default", "ZOB"], UserConfigFile.Profiles());

		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "fresh"));
		UserConfigFile.RenameProfile("Default", "ZNY");

		Assert.Equal("ZNY", UserConfigFile.ActiveProfile);
		Assert.Equal(["ZNY"], UserConfigFile.Profiles());
	}
}
