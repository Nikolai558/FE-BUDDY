using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
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
	}

	/// <summary>Restores defaults and deletes the throwaway directory.</summary>
	public void Dispose()
	{
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
		UserConfigFile.TrySetValue("General.UpdateChannel", "Beta");

		UserConfigFile.Save("General.UpdateChannel");

		JsonNode root = JsonNode.Parse(File.ReadAllText(UserConfigFile.ConfigFilePath))!;
		Assert.Equal("Beta", root["General"]!["UpdateChannel"]!.GetValue<string>());
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
		UserConfigFile.TrySetValue("General.UpdateChannel", "Beta");
		UserConfigFile.Save(GeneralNode);
		File.WriteAllText(UserConfigFile.PreviousFilePath, """{ "General": null }""");

		Assert.True(UserConfigFile.Undo(GeneralNode));

		Assert.Null(UserConfigFile.GetValue("General.UpdateChannel"));
		Assert.False(UserConfigFile.CanUndo(GeneralNode));
	}

	/// <summary><see cref="UserConfigFile.ReplaceAll"/> swaps every value, backs up the old file and drops the per-node undo snapshots.</summary>
	[Fact]
	public void replace_all_swaps_every_value_backs_up_the_file_and_drops_undo()
	{
		UserConfigFile.TrySetValue("General.UpdateChannel", "Beta");
		UserConfigFile.Save(GeneralNode);
		UserConfigFile.TrySetValue("General.UpdateChannel", "Alpha");
		UserConfigFile.Save(GeneralNode);
		Assert.True(UserConfigFile.CanUndo(GeneralNode));

		UserConfigFile.ReplaceAll(new Dictionary<string, string>
		{
			["Services.AiracService.UserArtccId"] = "ZOB",
			["General..Broken"] = "dropped",
			["General.PrettyPrintGeojson"] = null!,
		});

		Assert.Equal("ZOB", UserConfigFile.GetValue("Services.AiracService.UserArtccId"));
		Assert.Null(UserConfigFile.GetValue("General.UpdateChannel"));
		Assert.Null(UserConfigFile.GetValue("General..Broken"));
		Assert.Equal(string.Empty, UserConfigFile.GetValue("General.PrettyPrintGeojson"));
		Assert.False(UserConfigFile.CanUndo(GeneralNode));

		JsonNode backup = JsonNode.Parse(File.ReadAllText(UserConfigFile.BeforeImportFilePath))!;
		Assert.Equal("Alpha", backup["General"]!["UpdateChannel"]!.GetValue<string>());
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
}
