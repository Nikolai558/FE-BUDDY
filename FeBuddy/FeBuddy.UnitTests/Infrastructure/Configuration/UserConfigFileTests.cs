using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Exercises <see cref="UserConfigFile"/>: dictionary round-trip through the nested JSON file,
/// a resilient launch read path, per-node saves, and the one-step per-node undo.
/// </summary>
[Collection("AppLog")]
public sealed class UserConfigFileTests : IDisposable
{
	private const string AirwaysNode = "Services.AiracService.Airways";
	private const string GeneralNode = "General";

	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_UserConfig_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points <see cref="UserConfigFile"/> and <see cref="AppLog"/> at throwaway directories.</summary>
	public UserConfigFileTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_directory, "logs"));
		UserConfigFile.ConfigureForTesting(_directory);

		// Tests write the profile's file straight in.
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
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, recursive: true);
			}
		}
		catch
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>Values set in memory survive a <see cref="UserConfigFile.Write"/> / <see cref="UserConfigFile.ReadAll"/> cycle, and land in a nested file.</summary>
	[Fact]
	public void write_then_read_all_round_trips_values_and_nests_them()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Stable");
		UserConfigFile.TrySetValue("Services.AiracService.UserArtccId", "ZOA");
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");

		UserConfigFile.Write();

		// Force a fresh read from disk.
		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("Stable", UserConfigFile.GetValue("General.UpdateChannel"));
		Assert.Equal("ZOA", UserConfigFile.GetValue("Services.AiracService.UserArtccId"));
		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));

		JsonNode root = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Equal("ZOA", root["Services"]!["AiracService"]!["UserArtccId"]!.GetValue<string>());
		Assert.Equal("HighLow", root["Services"]!["AiracService"]!["Airways"]!["OutputBy"]!.GetValue<string>());
	}

	/// <summary>A missing config file on the launch read path yields defaults, not an exception.</summary>
	[Fact]
	public void read_all_missing_file_does_not_throw_and_returns_defaults()
	{
		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));

		Assert.Equal(UserConfigReadResult.Missing, UserConfigFile.ReadAll());

		Assert.Null(UserConfigFile.GetValue("General.UpdateChannel"));
	}

	/// <summary>An unset key returns <see langword="null"/> rather than throwing.</summary>
	[Fact]
	public void get_value_missing_key_returns_null()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Stable");
		UserConfigFile.Write();

		Assert.Null(UserConfigFile.GetValue("Services.AiracService.Nope"));
	}

	/// <summary>
	/// <see cref="UserConfigFile.RemoveValues(string)"/> drops a value and everything below it, and a
	/// save then leaves them off disk - a numbered list written again with fewer entries.
	/// </summary>
	[Fact]
	public void remove_values_drops_a_subtree_and_nothing_beside_it()
	{
		const string Node = "Services.AiracService.ConcatenateAliases";
		UserConfigFile.TrySetValue(Node + ".Sources.1.Url", "https://example.com/a.txt");
		UserConfigFile.TrySetValue(Node + ".Sources.2.FilePath", @"C:\b.txt");
		UserConfigFile.TrySetValue(Node + ".SourcesNote", "kept: only its name starts the same");
		UserConfigFile.Save(Node);

		Assert.Equal(2, UserConfigFile.RemoveValues(Node + ".Sources"));
		Assert.Equal(0, UserConfigFile.RemoveValues(Node + ".Sources"));
		Assert.Equal(0, UserConfigFile.RemoveValues(".bad."));
		Assert.Equal(1, UserConfigFile.RemoveValues(Node + ".SourcesNote"));
		UserConfigFile.TrySetValue(Node + ".SourcesNote", "kept: only its name starts the same");

		UserConfigFile.TrySetValue(Node + ".Sources.1.FilePath", @"C:\a.txt");
		UserConfigFile.Save(Node);

		Assert.Null(UserConfigFile.GetValue(Node + ".Sources.2.FilePath"));
		Assert.Null(UserConfigFile.GetValue(Node + ".Sources.1.Url"));
		Assert.Equal(@"C:\a.txt", UserConfigFile.GetValue(Node + ".Sources.1.FilePath"));
		Assert.NotNull(UserConfigFile.GetValue(Node + ".SourcesNote"));
	}

	/// <summary><see cref="UserConfigFile.Save(string)"/> persists only its own subtree.</summary>
	[Fact]
	public void save_writes_only_its_own_subtree()
	{
		// Nothing else has been persisted yet.
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.TrySetValue(AirwaysNode + ".BufferAirwayWaypoints", "true");
		UserConfigFile.TrySetValue("General.UpdateChannel", "Beta");

		UserConfigFile.Save(AirwaysNode);

		JsonNode root = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Equal("Designation", root["Services"]!["AiracService"]!["Airways"]!["OutputBy"]!.GetValue<string>());

		// The General value was in memory but not part of the saved node, so it is not on disk.
		Assert.Null(root["General"]);
	}

	/// <summary>Saving one node does not revert another node's saved value, and undo is per node.</summary>
	[Fact]
	public void save_and_undo_are_isolated_per_node()
	{
		// Persist General once.
		UserConfigFile.TrySetValue("General.UpdateChannel", "Stable");
		UserConfigFile.Save(GeneralNode);

		// Persist Airways with an initial value, then change it and save again so a snapshot exists.
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);

		Assert.True(UserConfigFile.CanUndo(AirwaysNode));

		// Undo Airways -> back to HighLow; General is untouched.
		Assert.True(UserConfigFile.Undo(AirwaysNode));
		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
		Assert.Equal("Stable", UserConfigFile.GetValue("General.UpdateChannel"));

		// Undo depth is exactly one: a second undo of the same node does nothing.
		Assert.False(UserConfigFile.CanUndo(AirwaysNode));
		Assert.False(UserConfigFile.Undo(AirwaysNode));
	}

	/// <summary>
	/// A profile that is there but can't be read for a moment (antivirus, a sync tool) is never written
	/// back with only the node being saved: the save fails, and every other setting in it survives.
	/// </summary>
	[Fact]
	public void save_and_undo_never_write_over_a_profile_they_cannot_read()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Stable");
		UserConfigFile.Save(GeneralNode);
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);
		string before = File.ReadAllText(UserConfigFile.ConfigFilePath);

		using (File.Open(UserConfigFile.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Rte");

			Assert.Throws<IOException>(() => UserConfigFile.Save(AirwaysNode));
			Assert.Throws<IOException>(() => UserConfigFile.Undo(AirwaysNode));
		}

		Assert.Equal(before, File.ReadAllText(UserConfigFile.ConfigFilePath));
	}

	/// <summary>A profile that isn't valid JSON is replaced by a save, as before: there is nothing in it to keep.</summary>
	[Fact]
	public void save_replaces_a_profile_that_is_not_json()
	{
		File.WriteAllText(UserConfigFile.ConfigFilePath, "{ not json");
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");

		UserConfigFile.Save(AirwaysNode);

		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
	}

	/// <summary>
	/// A profile that couldn't be read at launch leaves defaults in memory: writing them all would
	/// replace every setting, so <see cref="UserConfigFile.Write"/> refuses until a read succeeds.
	/// </summary>
	[Fact]
	public void write_refuses_after_the_profile_could_not_be_read_at_launch()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Write();
		string before = File.ReadAllText(UserConfigFile.ConfigFilePath);

		// A new launch: nothing of the profile in memory yet.
		UserConfigFile.ConfigureForTesting(_directory);

		using (File.Open(UserConfigFile.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());
		}

		Assert.Throws<IOException>(UserConfigFile.Write);
		Assert.Equal(before, File.ReadAllText(UserConfigFile.ConfigFilePath));

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		UserConfigFile.Write();
		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
	}

	/// <summary>
	/// A profile already read that is locked when it is read again keeps its settings in memory, rather
	/// than every page reading defaults until the next read; another profile's are never kept for it.
	/// </summary>
	[Fact]
	public void a_locked_re_read_keeps_the_settings_read_before_but_never_another_profiles()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Write();

		using (File.Open(UserConfigFile.ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());
			Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
		}

		UserConfigFile.Write();
		Assert.Contains("HighLow", File.ReadAllText(UserConfigFile.ConfigFilePath), StringComparison.Ordinal);

		UserConfigFile.CreateProfile("Other", new Dictionary<string, string> { [AirwaysNode + ".OutputBy"] = "Designation" });
		string other = UserConfigFile.ProfileFilePath("Other");

		using (File.Open(other, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			UserConfigFile.SwitchProfile("Other");
			Assert.Null(UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
			Assert.Throws<IOException>(UserConfigFile.Write);
		}
	}

	/// <summary>
	/// <see cref="UserConfigFile.CanUndo"/> answers from memory, never the snapshot file, since WPF asks on every
	/// key press: a snapshot locked by another program still counts, and a profile's own snapshot is read on switching.
	/// </summary>
	[Fact]
	public void can_undo_answers_from_memory_not_the_file()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);

		using (new FileStream(UserConfigFile.PreviousFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			Assert.True(UserConfigFile.CanUndo(AirwaysNode));
			Assert.False(UserConfigFile.CanUndo(GeneralNode));
		}

		UserConfigFile.CreateProfile("Other", null);
		UserConfigFile.SwitchProfile("Other");
		Assert.False(UserConfigFile.CanUndo(AirwaysNode));

		UserConfigFile.SwitchProfile(UserConfigFile.DefaultProfile);
		Assert.True(UserConfigFile.CanUndo(AirwaysNode));

		UserConfigFile.RenameProfile(UserConfigFile.DefaultProfile, "Renamed");
		Assert.True(UserConfigFile.CanUndo(AirwaysNode));
	}

	// ---- SaveValues: a save made off the UI thread ----

	private const string MapNode = "Services.MapService";

	/// <summary>
	/// The values are set and the node saved as one step, and the files aren't read again: a setting another
	/// page has set but not saved yet is still there, and still unsaved. A quiet save isn't in the activity log.
	/// </summary>
	[Fact]
	public void save_values_sets_and_saves_the_node_and_leaves_other_unsaved_settings_alone()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");

		Assert.True(UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, UserConfigFile.DefaultProfile, quietly: true));

		JsonNode file = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Equal("N", file["Services"]!["MapService"]!["Gridlines"]!.GetValue<string>());
		Assert.Null(file["Services"]!["AiracService"]);
		Assert.Equal("N", UserConfigFile.GetValue(MapNode + ".Gridlines"));
		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
		Assert.DoesNotContain(AppLog.Entries, e => e.Level >= LogLevel.Info && e.Message == $"Saved '{MapNode}'.");

		UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "Y" }, UserConfigFile.DefaultProfile);
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Info && e.Message == $"Saved '{MapNode}'.");
	}

	/// <summary>Chosen in one profile, the values aren't saved into another that came into use meanwhile.</summary>
	[Fact]
	public void save_values_saves_nothing_once_another_profile_is_in_use()
	{
		Assert.False(UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, "Other"));

		Assert.Null(UserConfigFile.GetValue(MapNode + ".Gridlines"));
		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));
	}

	/// <summary>When the file can't be written, nothing is taken for saved: memory keeps what the file holds.</summary>
	[Fact]
	public void save_values_that_cannot_be_written_change_nothing_in_memory()
	{
		UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "Y" }, UserConfigFile.DefaultProfile);
		File.Delete(UserConfigFile.PreviousFilePath);
		Directory.CreateDirectory(UserConfigFile.PreviousFilePath);

		Assert.ThrowsAny<Exception>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string>
		{
			[MapNode + ".Gridlines"] = "N",
			[MapNode + ".BaseMapOpacity"] = "40",
		}, UserConfigFile.DefaultProfile));

		Assert.Equal("Y", UserConfigFile.GetValue(MapNode + ".Gridlines"));
		Assert.Null(UserConfigFile.GetValue(MapNode + ".BaseMapOpacity"));
	}

	/// <summary>Only keys under the node are taken.</summary>
	[Fact]
	public void save_values_takes_only_keys_under_the_node()
	{
		Assert.Throws<ArgumentException>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [AirwaysNode + ".OutputBy"] = "x" }, UserConfigFile.DefaultProfile));
		Assert.Throws<ArgumentException>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + "..x"] = "x" }, UserConfigFile.DefaultProfile));
		Assert.Throws<ArgumentException>(() => UserConfigFile.SaveValues(" ", new Dictionary<string, string>(), UserConfigFile.DefaultProfile));
		Assert.Throws<ArgumentNullException>(() => UserConfigFile.SaveValues(MapNode, null!, UserConfigFile.DefaultProfile));
		Assert.Throws<ArgumentNullException>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string>(), null!));
	}

	/// <summary>Undo with no snapshot for the node is a no-op returning <see langword="false"/>.</summary>
	[Fact]
	public void undo_with_no_snapshot_returns_false()
	{
		Assert.False(UserConfigFile.CanUndo(AirwaysNode));
		Assert.False(UserConfigFile.Undo(AirwaysNode));

		// Even after a single first save (nothing existed before it), there is nothing to revert to.
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);

		Assert.False(UserConfigFile.CanUndo(AirwaysNode));
		Assert.False(UserConfigFile.Undo(AirwaysNode));
	}

	/// <summary>A config file that is valid JSON but not an object yields defaults and a warning.</summary>
	[Fact]
	public void read_all_non_object_json_uses_defaults_and_warns()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, "[1, 2]");

		Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());

		Assert.Empty(UserConfigFile.SnapshotValues());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("is not a JSON object", StringComparison.Ordinal));
	}

	/// <summary>A corrupt config file yields defaults and a warning rather than an exception.</summary>
	[Fact]
	public void read_all_corrupt_json_uses_defaults_and_warns()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Stable");
		Directory.CreateDirectory(_directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, "{ not json");

		Assert.Equal(UserConfigReadResult.Unreadable, UserConfigFile.ReadAll());

		Assert.Null(UserConfigFile.GetValue("General.UpdateChannel"));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Could not read config file", StringComparison.Ordinal));
	}

	/// <summary>Nulls read back as empty strings and arrays as their JSON text.</summary>
	[Fact]
	public void read_all_flattens_nulls_and_arrays()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, """{ "General": { "Cleared": null, "List": [1, 2], "Count": 5 } }""");

		UserConfigFile.ReadAll();

		Assert.Equal(string.Empty, UserConfigFile.GetValue("General.Cleared"));
		Assert.Equal("[1,2]", UserConfigFile.GetValue("General.List"));
		Assert.Equal("5", UserConfigFile.GetValue("General.Count"));
	}

	/// <summary>Blank or malformed paths are refused without touching the file.</summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(".General")]
	[InlineData("General.")]
	[InlineData("General..UpdateChannel")]
	public void malformed_paths_are_refused(string path)
	{
		Assert.False(UserConfigFile.TrySetValue(path, "x"));

		if (string.IsNullOrWhiteSpace(path))
		{
			Assert.Null(UserConfigFile.GetValue(path));
			Assert.False(UserConfigFile.CanUndo(path));
			Assert.False(UserConfigFile.Undo(path));
			Assert.Throws<ArgumentException>(() => UserConfigFile.Save(path));
		}
	}

	/// <summary>A null value is stored as an empty string.</summary>
	[Fact]
	public void try_set_value_null_stores_empty()
	{
		Assert.True(UserConfigFile.TrySetValue("General.UpdateChannel", null!));

		Assert.Equal(string.Empty, UserConfigFile.GetValue("General.UpdateChannel"));
	}

	/// <summary>Saving a single leaf writes just that value, even over a corrupt file.</summary>
	[Fact]
	public void save_a_leaf_over_a_corrupt_file_writes_the_leaf()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, "{ not json");
		UserConfigFile.TrySetValue("General.PrettyPrintGeojson", "Y");

		UserConfigFile.Save("General.PrettyPrintGeojson");

		JsonNode root = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Equal("Y", root["General"]!["PrettyPrintGeojson"]!.GetValue<string>());
	}

	/// <summary>Saving a node with nothing in memory under it removes it from the file.</summary>
	[Fact]
	public void save_an_empty_node_removes_it_from_the_file()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		UserConfigFile.ConfigureForTesting(_directory); // clear memory, keep the file

		UserConfigFile.Save(AirwaysNode);

		JsonNode root = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Null(root["Services"]!["AiracService"]!["Airways"]);
		Assert.True(UserConfigFile.CanUndo(AirwaysNode));
	}

	/// <summary>A snapshot that recorded "nothing here" undoes to removing the node.</summary>
	[Fact]
	public void undo_a_null_snapshot_removes_the_node()
	{
		UserConfigFile.TrySetValue("General.PrettyPrintGeojson", "Y");
		UserConfigFile.Save(GeneralNode);
		File.WriteAllText(UserConfigFile.PreviousFilePath, """{ "General": null }""");

		Assert.True(UserConfigFile.Undo(GeneralNode));

		Assert.Null(UserConfigFile.GetValue("General.PrettyPrintGeojson"));
		Assert.False(UserConfigFile.CanUndo(GeneralNode));
	}

	/// <summary><see cref="UserConfigFile.ReplaceAll"/> swaps every value, backs up the old file and drops the per-node undo snapshots.</summary>
	[Fact]
	public void replace_all_swaps_every_value_backs_up_the_file_and_drops_undo()
	{
		UserConfigFile.TrySetValue("General.PrettyPrintGeojson", "N");
		UserConfigFile.Save(GeneralNode);
		UserConfigFile.TrySetValue("General.PrettyPrintGeojson", "Y");
		UserConfigFile.Save(GeneralNode);
		Assert.True(UserConfigFile.CanUndo(GeneralNode));

		UserConfigFile.ReplaceAll(new Dictionary<string, string>
		{
			["Services.AiracService.UserArtccId"] = "ZOB",
			["General..Broken"] = "dropped",
			["General.AddFeBuddyOutputFolder"] = null!,
		});

		Assert.Equal("ZOB", UserConfigFile.GetValue("Services.AiracService.UserArtccId"));
		Assert.Null(UserConfigFile.GetValue("General.PrettyPrintGeojson"));
		Assert.Null(UserConfigFile.GetValue("General..Broken"));
		Assert.Equal(string.Empty, UserConfigFile.GetValue("General.AddFeBuddyOutputFolder"));
		Assert.False(UserConfigFile.CanUndo(GeneralNode));

		JsonNode backup = JsonNode.Parse(File.ReadAllText(UserConfigFile.BeforeImportFilePath))!;
		Assert.Equal("Y", backup["General"]!["PrettyPrintGeojson"]!.GetValue<string>());
	}

	/// <summary>With no config file yet there is nothing to back up, and the new file is still written.</summary>
	[Fact]
	public void replace_all_without_a_file_writes_one_and_no_backup()
	{
		UserConfigFile.ReplaceAll(new Dictionary<string, string> { ["General.PrettyPrintGeojson"] = "Y" });

		Assert.True(File.Exists(UserConfigFile.ConfigFilePath));
		Assert.False(File.Exists(UserConfigFile.BeforeImportFilePath));
		Assert.Equal("Y", UserConfigFile.GetValue("General.PrettyPrintGeojson"));
	}

	/// <summary>A copy of the values is handed out, so changing it does not change the config.</summary>
	[Fact]
	public void snapshot_values_is_a_copy()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Beta");

		IReadOnlyDictionary<string, string> snapshot = UserConfigFile.SnapshotValues();
		UserConfigFile.TrySetValue("General.UpdateChannel", "Alpha");

		Assert.Equal("Beta", snapshot["General.UpdateChannel"]);
	}

	// ============================ shared settings saved as they change ============================

	/// <summary>
	/// <see cref="UserConfigFile.SaveShared"/> writes <c>Shared.json</c> alone: the profile's file and its
	/// undo snapshot are left as they are, and a setting set but not saved stays in memory.
	/// </summary>
	[Fact]
	public void save_shared_writes_shared_json_alone()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Write();
		string profileBefore = File.ReadAllText(UserConfigFile.ConfigFilePath);

		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Use"); // set, not saved
		UserConfigFile.TrySetValue(UserConfigKeys.MainWindowPlacement, "100,50,1200,780,96,Normal");
		UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement);

		Assert.Equal(profileBefore, File.ReadAllText(UserConfigFile.ConfigFilePath));
		Assert.False(File.Exists(UserConfigFile.PreviousFilePath));
		Assert.Equal("Use", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
		Assert.Equal("100,50,1200,780,96,Normal", UserConfigFile.ReadSharedValue(UserConfigKeys.MainWindowPlacement));
	}

	[Fact]
	public void save_shared_removes_a_shared_setting_that_is_unset()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.MainWindowPlacement, "100,50,1200,780,96,Normal");
		UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement);

		UserConfigFile.RemoveValues(UserConfigKeys.MainWindowPlacement);
		UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement);

		Assert.Null(UserConfigFile.ReadSharedValue(UserConfigKeys.MainWindowPlacement));
	}

	/// <summary>Saved before the launch has read the settings, the profile in use stays the one the file names.</summary>
	[Fact]
	public void save_shared_keeps_the_profile_in_use()
	{
		UserConfigFile.Write();
		UserConfigFile.CreateProfile("ZNY", null);
		UserConfigFile.SwitchProfile("ZNY");
		UserConfigFile.ConfigureForTesting(_directory); // nothing read: the profile in memory is Default

		UserConfigFile.TrySetValue(UserConfigKeys.MainWindowPlacement, "100,50,1200,780,96,Normal");
		UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement);

		JsonNode shared = JsonNode.Parse(File.ReadAllText(UserConfigFile.SharedFilePath))!;
		Assert.Equal("ZNY", shared["ActiveProfile"]!.GetValue<string>());
	}

	[Fact]
	public void save_shared_rejects_a_setting_that_is_not_shared()
	{
		Assert.Throws<ArgumentException>(() => UserConfigFile.SaveShared(UserConfigKeys.MainWindowPlacement, AirwaysNode));
		Assert.Throws<ArgumentNullException>(() => UserConfigFile.SaveShared(null!));
		Assert.False(File.Exists(UserConfigFile.SharedFilePath));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("Services.AiracService.UserArtccId")]
	[InlineData(UserConfigKeys.WelcomeShown)]
	public void read_shared_value_is_null_for_a_setting_shared_json_does_not_hold(string? key)
	{
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "General": { "UpdateChannel": "Beta" }, "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""");

		Assert.Null(UserConfigFile.ReadSharedValue(key!));
		Assert.Equal("Beta", UserConfigFile.ReadSharedValue(UserConfigKeys.UpdateChannel));
	}

	// ============================ the layout version ============================

	/// <summary>A stand-in next layout: Settings ▸ Facility's ARTCC moved under General.</summary>
	private static readonly UserConfigMigration MoveArtcc =
		new(3, "Moved the facility's ARTCC under General", settings => settings.Move("Services.AiracService.UserArtccId", "General.Facility.ArtccId"));

	private static int? Stamp(string path) =>
		JsonNode.Parse(File.ReadAllText(path))![UserConfigVersion.FileKey]?.GetValue<int>();

	private void WriteFile(string json)
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, json);
	}

	/// <summary>Every write stamps the layout first in the file, and the stamp is never read back as a setting.</summary>
	[Fact]
	public void every_write_stamps_the_layout_and_reading_keeps_it_out_of_the_settings()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);

		Assert.Equal(UserConfigVersion.Current, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Equal(UserConfigVersion.FileKey, JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!.AsObject().First().Key);
		Assert.Null(UserConfigFile.GetValue(UserConfigVersion.FileKey));
		Assert.DoesNotContain(UserConfigVersion.FileKey, UserConfigFile.SnapshotValues().Keys);

		UserConfigFile.Write();
		Assert.Equal(UserConfigVersion.Current, Stamp(UserConfigFile.ConfigFilePath));

		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);
		Assert.True(UserConfigFile.Undo(AirwaysNode));
		Assert.Equal(UserConfigVersion.Current, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Null(Stamp(UserConfigFile.PreviousFilePath));
	}

	/// <summary>
	/// A file with no stamp is the oldest layout: with a newer one, it is brought forward at launch and
	/// written back stamped, the file as it was kept beside it.
	/// </summary>
	[Fact]
	public void a_file_with_no_stamp_is_the_oldest_layout_and_is_brought_forward()
	{
		UserConfigMigrations.ConfigureForTesting(3, [MoveArtcc]);
		UserConfigFile.ConfigureForTesting(_directory);
		const string OlderFile = """{ "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""";
		WriteFile(OlderFile);

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("ZOB", UserConfigFile.GetValue("General.Facility.ArtccId"));
		Assert.Equal(3, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Equal(OlderFile, File.ReadAllText(UserConfigFile.BroughtForwardFilePath(UserConfigVersion.Oldest)));
	}

	/// <summary>
	/// With a newer layout, an older file is brought forward at launch and written back stamped,
	/// the file as it was kept beside it, and the per-node undo snapshots (of the old layout) dropped.
	/// </summary>
	[Fact]
	public void an_older_file_is_brought_forward_written_back_and_kept_as_it_was()
	{
		UserConfigMigrations.ConfigureForTesting(3, [MoveArtcc]);
		UserConfigFile.ConfigureForTesting(_directory);
		const string OlderFile = """{ "ConfigVersion": 2, "Services": { "AiracService": { "UserArtccId": "ZOB", "Airways": { "OutputBy": "HighLow" } } } }""";
		WriteFile(OlderFile);
		File.WriteAllText(UserConfigFile.PreviousFilePath, """{ "Services": { "AiracService": { "Airways": { "OutputBy": "Designation" } } } }""");

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("ZOB", UserConfigFile.GetValue("General.Facility.ArtccId"));
		Assert.Null(UserConfigFile.GetValue("Services.AiracService.UserArtccId"));
		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));

		Assert.Equal(3, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Equal(OlderFile, File.ReadAllText(UserConfigFile.BroughtForwardFilePath(2)));
		Assert.False(File.Exists(UserConfigFile.PreviousFilePath));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Info && e.Message.Contains(
			"Brought the settings up from layout 2 to layout 3: Moved the facility's ARTCC under General. The file as it was is kept as 'UserConfig-v2.Default.json'.",
			StringComparison.Ordinal));

		// Read again: already in the current layout, so nothing more happens.
		File.Delete(UserConfigFile.BroughtForwardFilePath(2));
		UserConfigFile.ReadAll();
		Assert.False(File.Exists(UserConfigFile.BroughtForwardFilePath(2)));
	}

	/// <summary>A step that fails leaves the file alone; the settings are used as they are, and saves keep the old stamp so the next launch tries again.</summary>
	[Fact]
	public void a_step_that_fails_leaves_the_file_alone_and_keeps_its_old_stamp()
	{
		UserConfigMigrations.ConfigureForTesting(3, [new UserConfigMigration(3, "Breaks", _ => throw new FormatException("no"))]);
		UserConfigFile.ConfigureForTesting(_directory);
		const string OlderFile = """{ "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""";
		WriteFile(OlderFile);

		UserConfigFile.ReadAll();

		Assert.Equal("ZOB", UserConfigFile.GetValue("Services.AiracService.UserArtccId"));
		Assert.Equal(OlderFile, File.ReadAllText(UserConfigFile.ConfigFilePath));
		Assert.False(File.Exists(UserConfigFile.BroughtForwardFilePath(2)));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Could not bring the settings in 'UserConfig.Default.json' up from layout 2 to layout 3", StringComparison.Ordinal));

		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		Assert.Equal(2, Stamp(UserConfigFile.ConfigFilePath));
	}

	/// <summary>When the brought-forward file can't be written, the settings are still brought forward in memory and the file keeps its old stamp.</summary>
	[Fact]
	public void an_update_that_cannot_be_written_is_tried_again_next_launch()
	{
		UserConfigMigrations.ConfigureForTesting(3, [MoveArtcc]);
		UserConfigFile.ConfigureForTesting(_directory);
		WriteFile("""{ "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""");

		using (new FileStream(UserConfigFile.BroughtForwardFilePath(2), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
		{
			UserConfigFile.ReadAll();
		}

		Assert.Equal("ZOB", UserConfigFile.GetValue("General.Facility.ArtccId"));
		Assert.Null(Stamp(UserConfigFile.ConfigFilePath));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("FE-Buddy tries again at the next launch", StringComparison.Ordinal));

		// The next launch can write it.
		UserConfigFile.ReadAll();
		Assert.Equal(3, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Equal("ZOB", UserConfigFile.GetValue("General.Facility.ArtccId"));
	}

	/// <summary>
	/// Shared.json goes through the same steps, by its own stamp: a step that renames a shared setting
	/// renames it there, the file is written back stamped current, and a restart still has it.
	/// </summary>
	[Fact]
	public void an_older_shared_file_is_brought_forward_and_written_back()
	{
		UserConfigMigrations.ConfigureForTesting(3,
			[new UserConfigMigration(3, "Renamed the update channel", settings => settings.Move("General.Channel", UserConfigKeys.UpdateChannel))]);
		UserConfigFile.ConfigureForTesting(_directory);
		File.WriteAllText(UserConfigFile.SharedFilePath, """{ "ConfigVersion": 2, "ActiveProfile": "Default", "General": { "Channel": "Beta" } }""");

		UserConfigFile.ReadAll();

		Assert.Equal("Beta", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
		Assert.Equal(3, Stamp(UserConfigFile.SharedFilePath));
		JsonNode general = JsonNode.Parse(File.ReadAllText(UserConfigFile.SharedFilePath))!["General"]!;
		Assert.Equal("Beta", general["UpdateChannel"]!.GetValue<string>());
		Assert.Null(general["Channel"]);
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Info && e.Message.Contains(
			"Brought the settings in 'Shared.json' up from layout 2 to layout 3: Renamed the update channel.", StringComparison.Ordinal));

		// A restart: the file is in the current layout, and the setting is still there.
		UserConfigFile.ConfigureForTesting(_directory);
		UserConfigFile.ReadAll();
		Assert.Equal("Beta", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
	}

	/// <summary>A Shared.json saved by a newer FE-Buddy is used as it is, and a save keeps its stamp.</summary>
	[Fact]
	public void a_newer_shared_file_is_left_alone_and_keeps_its_stamp()
	{
		const string NewerShared = """{ "ConfigVersion": 99, "ActiveProfile": "Default", "General": { "UpdateChannel": "Beta" } }""";
		File.WriteAllText(UserConfigFile.SharedFilePath, NewerShared);

		UserConfigFile.ReadAll();

		Assert.Equal("Beta", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
		Assert.Equal(NewerShared, File.ReadAllText(UserConfigFile.SharedFilePath));

		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Stable");
		UserConfigFile.SaveShared(UserConfigKeys.UpdateChannel);
		Assert.Equal(99, Stamp(UserConfigFile.SharedFilePath));
	}

	/// <summary>An undo snapshot that can't be deleted is only a warning: the settings are still brought forward.</summary>
	[Fact]
	public void an_undo_snapshot_that_cannot_be_deleted_is_only_a_warning()
	{
		UserConfigMigrations.ConfigureForTesting(3, [MoveArtcc]);
		UserConfigFile.ConfigureForTesting(_directory);
		WriteFile("""{ "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""");
		File.WriteAllText(UserConfigFile.PreviousFilePath, "{}");

		using (new FileStream(UserConfigFile.PreviousFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			UserConfigFile.ReadAll();
		}

		Assert.Equal(3, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Could not delete 'UserConfig-previous.Default.json'", StringComparison.Ordinal));
	}

	/// <summary>A file a newer FE-Buddy saved keeps its stamp, so going back to the newer one never brings its own settings forward again.</summary>
	[Fact]
	public void a_file_from_a_newer_fe_buddy_keeps_its_stamp()
	{
		WriteFile("""{ "ConfigVersion": 7, "General": { "PrettyPrintGeojson": "Y" } }""");

		UserConfigFile.ReadAll();
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);

		Assert.Equal("Y", UserConfigFile.GetValue("General.PrettyPrintGeojson"));
		Assert.Equal(7, Stamp(UserConfigFile.ConfigFilePath));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("was saved by a newer FE-Buddy (settings layout 7", StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("\"two\"")]
	[InlineData("0")]
	[InlineData("null")]
	public void a_stamp_that_cannot_be_read_is_taken_as_the_oldest_layout(string stamp)
	{
		WriteFile($$"""{ "ConfigVersion": {{stamp}}, "General": { "UpdateChannel": "Beta" } }""");

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("Beta", UserConfigFile.GetValue("General.UpdateChannel"));
		Assert.Null(UserConfigFile.GetValue(UserConfigVersion.FileKey));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("has a settings layout stamp FE-Buddy can't read", StringComparison.Ordinal));
	}

	/// <summary>
	/// A file stamped with a layout older than the oldest is read as the oldest, without a warning: its
	/// settings are used as they are, and the next save stamps it.
	/// </summary>
	[Fact]
	public void a_file_stamped_older_than_the_oldest_layout_is_read_as_it()
	{
		const string OlderFile = """{ "ConfigVersion": 1, "Services": { "AiracService": { "Airways": { "OutputBy": "HighLow" } } } }""";
		WriteFile(OlderFile);

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());

		Assert.Equal("HighLow", UserConfigFile.GetValue(AirwaysNode + ".OutputBy"));
		Assert.Null(UserConfigFile.GetValue(UserConfigVersion.FileKey));
		Assert.DoesNotContain(AppLog.Entries, e => e.Level == LogLevel.Warning);
		Assert.Equal(OlderFile, File.ReadAllText(UserConfigFile.ConfigFilePath));

		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);
		Assert.Equal(UserConfigVersion.Oldest, Stamp(UserConfigFile.ConfigFilePath));
	}

	// ---- writes cut short, damaged files and the undo snapshot ----

	/// <summary>Saves the Airways node twice, so its undo snapshot holds it.</summary>
	private static void SaveAirwaysTwice()
	{
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "HighLow");
		UserConfigFile.Save(AirwaysNode);
		UserConfigFile.TrySetValue(AirwaysNode + ".OutputBy", "Designation");
		UserConfigFile.Save(AirwaysNode);
	}

	/// <summary>
	/// A profile cut short (by a crash, say) can't be read as settings, so a save replaces it; what is left of it
	/// is kept first, as <c>UserConfig-damaged.&lt;Profile&gt;.json</c>.
	/// </summary>
	[Fact]
	public void a_damaged_profile_is_kept()
	{
		const string damaged = "{\"General\":{\"Facility\":\"ZOB\"";
		WriteFile(damaged);
		UserConfigFile.ReadAll();

		UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, UserConfigFile.DefaultProfile);

		Assert.Equal(damaged, File.ReadAllText(Path.Combine(UserConfigFile.Directory, "UserConfig-damaged.Default.json")));
		Assert.Equal("N", UserConfigFile.GetValue(MapNode + ".Gridlines"));
	}

	/// <summary>
	/// A settings file is written beside itself first and only then moved over it, so a write that stops part way
	/// leaves the file as it was, and nothing behind.
	/// </summary>
	[Fact]
	public void a_write_that_stops_part_way_leaves_the_file_as_it_was()
	{
		UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "Y" }, UserConfigFile.DefaultProfile);
		string before = File.ReadAllText(UserConfigFile.ConfigFilePath);
		UserConfigFile.WritingFileForTesting = path =>
		{
			if (path == UserConfigFile.ConfigFilePath)
			{
				throw new IOException("power cut");
			}
		};

		Assert.Throws<IOException>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, UserConfigFile.DefaultProfile));

		Assert.Equal(before, File.ReadAllText(UserConfigFile.ConfigFilePath));
		Assert.Empty(Directory.GetFiles(UserConfigFile.Directory, "*.tmp"));
	}

	/// <summary>
	/// A profile that can't be written - read-only, say - once its undo snapshot has been: memory goes back to what
	/// the file holds, and Undo last save doesn't offer to undo the save that failed.
	/// </summary>
	[Fact]
	public void a_profile_that_cannot_be_written_rolls_memory_back_and_offers_no_undo()
	{
		UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "Y" }, UserConfigFile.DefaultProfile);
		File.SetAttributes(UserConfigFile.ConfigFilePath, FileAttributes.ReadOnly);

		try
		{
			Assert.Throws<UnauthorizedAccessException>(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, UserConfigFile.DefaultProfile));

			Assert.Equal("Y", UserConfigFile.GetValue(MapNode + ".Gridlines"));
			Assert.False(UserConfigFile.CanUndo(MapNode));
		}
		finally
		{
			File.SetAttributes(UserConfigFile.ConfigFilePath, FileAttributes.Normal);
		}
	}

	/// <summary>
	/// An undo snapshot locked by another program the first time it is asked for - at launch, say - isn't taken
	/// for empty for good: once it can be read, Undo last save is on again.
	/// </summary>
	[Fact]
	public void a_snapshot_locked_at_first_ask_is_read_again()
	{
		SaveAirwaysTwice();

		// A new launch: the snapshot hasn't been read yet.
		UserConfigFile.ConfigureForTesting(_directory);

		using (new FileStream(UserConfigFile.PreviousFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			UserConfigFile.ReadAll();
			Assert.False(UserConfigFile.CanUndo(AirwaysNode));
		}

		Assert.True(UserConfigFile.CanUndo(AirwaysNode));
	}

	/// <summary>An undo snapshot gone from disk turns Undo last save off at the first try, rather than leave it on doing nothing.</summary>
	[Fact]
	public void undo_with_no_snapshot_on_disk_turns_undo_off()
	{
		SaveAirwaysTwice();
		Assert.True(UserConfigFile.CanUndo(AirwaysNode));
		File.Delete(UserConfigFile.PreviousFilePath);

		Assert.False(UserConfigFile.Undo(AirwaysNode));
		Assert.False(UserConfigFile.CanUndo(AirwaysNode));
	}

	/// <summary>
	/// The profile in use and Undo last save answer while a save holds the files - on a slow folder that is a
	/// while - so the UI thread, which asks on every key press, never waits for it.
	/// </summary>
	[Fact]
	public async Task reads_do_not_wait_for_a_save()
	{
		SaveAirwaysTwice();
		using ManualResetEventSlim writing = new();
		using ManualResetEventSlim release = new();
		UserConfigFile.WritingFileForTesting = _ =>
		{
			writing.Set();
			release.Wait();
		};

		Task save = Task.Run(() => UserConfigFile.SaveValues(MapNode, new Dictionary<string, string> { [MapNode + ".Gridlines"] = "N" }, UserConfigFile.DefaultProfile));
		writing.Wait();
		Task<(bool CanUndo, string Profile)> reads = Task.Run(() => (UserConfigFile.CanUndo(AirwaysNode), UserConfigFile.ActiveProfile));

		// Only a guard against hanging: the reads take no time unless they wait for the save.
		bool answered = await Task.WhenAny(reads, Task.Delay(TimeSpan.FromSeconds(10))) == reads;
		release.Set();
		await save;

		Assert.True(answered, "The reads waited for the save.");
		Assert.Equal((true, UserConfigFile.DefaultProfile), await reads);
	}

	/// <summary>A copy of the settings inside FE-Buddy's own folder, which a reset or an uninstall empties, is recognised before it is made.</summary>
	[Fact]
	public void a_copy_inside_fe_buddys_folder_is_recognised()
	{
		Assert.True(UserConfigFile.IsInsideAppData(Path.Combine(AppDataReset.RootDirectory, "Backup.json")));
		Assert.True(UserConfigFile.IsInsideAppData(Path.Combine(AppDataReset.RootDirectory, "Logs", "..", "Backup.json")));
		Assert.False(UserConfigFile.IsInsideAppData(AppDataReset.RootDirectory.TrimEnd(Path.DirectorySeparatorChar) + " backups" + Path.DirectorySeparatorChar + "Backup.json"));
		Assert.False(UserConfigFile.IsInsideAppData(Path.Combine(_directory, "Backup.json")));
		Assert.Throws<ArgumentNullException>(() => UserConfigFile.IsInsideAppData(null!));
	}
}
