using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Exercises <see cref="UserConfigTransfer"/>: an export carries a facility's setup without this
/// PC's folders, state or credentials, and an import takes it while keeping what belongs to the
/// importing PC.
/// </summary>
[Collection("AppLog")]
public sealed class UserConfigTransferTests : IDisposable
{
	private const string ArtccKey = "Services.AiracService.UserArtccId";
	private const string CycleKey = "Services.AiracService.AiracCycleId";
	private const string DatFolderKey = "Services.FileConversions.DatToGeojson.SourceFolder";
	private const string SctFolderKey = "Services.FileConversions.SctToGeojson.SourceFolder";
	private const string EramFolderKey = "Services.FileConversions.EramToGeojson.SourceFolder";

	private static readonly DateTimeOffset ExportedAt = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

	private static readonly PortablePathTokens Alice = new(
	[
		(PortablePathTokens.DesktopToken, @"C:\Users\alice\Desktop"),
		(PortablePathTokens.UserProfileToken, @"C:\Users\alice"),
	]);

	private static readonly PortablePathTokens Bob = new(
	[
		(PortablePathTokens.DesktopToken, @"C:\Users\bob\Desktop"),
		(PortablePathTokens.UserProfileToken, @"C:\Users\bob"),
	]);

	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_UserConfigTransfer_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points <see cref="UserConfigFile"/> and <see cref="AppLog"/> at throwaway directories.</summary>
	public UserConfigTransferTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_directory, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_directory, "config"));
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

	private string ExportPath => Path.Combine(_directory, "exports", "settings.json");

	// ============================ export ============================

	/// <summary>Shared settings go in as they are, folders go in tokenized, PC-only state and credentials stay out.</summary>
	[Fact]
	public void export_writes_shared_settings_and_tokenized_folders_only()
	{
		Dictionary<string, string> values = new()
		{
			[ArtccKey] = "ZOB",
			[UserConfigKeys.DefaultOutputDirectory] = @"C:\Users\alice\Desktop\FEB",
			[DatFolderKey] = @"D:\VATSIM\DAT",
			[SctFolderKey] = "",
			[UserConfigKeys.UpdateChannel] = "Alpha",
			[UserConfigKeys.NewsLastOpen] = "2026-09-01.1",
			["Secrets.GitHub.Pat"] = "ghp_secret",
		};

		UserConfigExportResult result = UserConfigTransfer.Export(ExportPath, values, "3.1.0", ExportedAt, Alice);

		Assert.Equal(new UserConfigExportResult(ExportPath, SettingCount: 3, FolderCount: 2, LeftOutCount: 3), result);

		string text = File.ReadAllText(ExportPath);
		Assert.DoesNotContain("ghp_secret", text, StringComparison.Ordinal);
		Assert.DoesNotContain("alice", text, StringComparison.OrdinalIgnoreCase);

		JsonNode root = JsonNode.Parse(text)!;
		Assert.Equal(UserConfigTransfer.FormatId, root["format"]!.GetValue<string>());
		Assert.Equal(UserConfigTransfer.FormatVersion, root["formatVersion"]!.GetValue<int>());
		Assert.Equal("3.1.0", root["appVersion"]!.GetValue<string>());

		JsonNode settings = root["settings"]!;
		Assert.Equal("ZOB", settings["Services"]!["AiracService"]!["UserArtccId"]!.GetValue<string>());
		Assert.Equal(@"%DESKTOP%\FEB", settings["General"]!["DefaultOutputDirectory"]!.GetValue<string>());
		Assert.Equal(@"D:\VATSIM\DAT", settings["Services"]!["FileConversions"]!["DatToGeojson"]!["SourceFolder"]!.GetValue<string>());
		Assert.Null(settings["General"]!["UpdateChannel"]);
		Assert.Null(settings["Secrets"]);
	}

	/// <summary>The public overload exports what <see cref="UserConfigFile"/> holds, and the file reads back.</summary>
	[Fact]
	public void export_of_the_live_config_reads_back()
	{
		UserConfigFile.TrySetValue(ArtccKey, "ZNY");
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Beta");

		UserConfigExportResult result = UserConfigTransfer.Export(ExportPath);

		Assert.Equal(1, result.SettingCount);

		UserConfigPackage package = UserConfigTransfer.Read(ExportPath);
		Assert.Equal("settings.json", package.FileName);
		Assert.Equal(UserConfigTransfer.FormatVersion, package.FormatVersion);
		Assert.False(package.IsPlainConfigFile);
		Assert.NotNull(package.ExportedUtc);
		Assert.Equal("ZNY", package.Values[ArtccKey]);
		Assert.False(package.Values.ContainsKey(UserConfigKeys.UpdateChannel));
	}

	// ============================ read ============================

	/// <summary>An export round-trips through <see cref="UserConfigTransfer.Read"/> with its header.</summary>
	[Fact]
	public void read_returns_the_export_header_and_values()
	{
		UserConfigTransfer.Export(ExportPath, new Dictionary<string, string> { [ArtccKey] = "ZOB" }, "3.1.0", ExportedAt, Alice);

		UserConfigPackage package = UserConfigTransfer.Read(ExportPath);

		Assert.Equal("3.1.0", package.AppVersion);
		Assert.Equal(ExportedAt, package.ExportedUtc);
		Assert.Equal("ZOB", package.Values[ArtccKey]);
	}

	/// <summary>A missing file is refused with a message naming it.</summary>
	[Fact]
	public void read_a_missing_file_throws()
	{
		UserConfigTransferException ex = Assert.Throws<UserConfigTransferException>(
			() => UserConfigTransfer.Read(Path.Combine(_directory, "nope.json")));

		Assert.Contains("nope.json", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A file far larger than any settings file is refused before it is read.</summary>
	[Fact]
	public void read_a_huge_file_throws()
	{
		Directory.CreateDirectory(_directory);
		string path = Path.Combine(_directory, "huge.json");
		using (FileStream stream = File.Create(path))
		{
			stream.SetLength(UserConfigTransfer.MaxFileBytes + 1);
		}

		UserConfigTransferException ex = Assert.Throws<UserConfigTransferException>(() => UserConfigTransfer.Read(path));

		Assert.Contains("too large", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>A file that cannot be opened is refused with the reason, not a raw IO exception.</summary>
	[Fact]
	public void read_a_locked_file_throws()
	{
		Directory.CreateDirectory(_directory);
		string path = Path.Combine(_directory, "locked.json");
		File.WriteAllText(path, "{}");

		using FileStream locked = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

		UserConfigTransferException ex = Assert.Throws<UserConfigTransferException>(() => UserConfigTransfer.Read(path));

		Assert.IsAssignableFrom<IOException>(ex.InnerException);
	}

	/// <summary>A plain <c>UserConfig.json</c> from another PC is accepted as it is.</summary>
	[Fact]
	public void parse_accepts_a_plain_user_config_file()
	{
		UserConfigPackage package = UserConfigTransfer.Parse(
			"""{ "General": { "UpdateChannel": "Beta" }, "Services": { "AiracService": { "UserArtccId": "ZOB" } } }""",
			"UserConfig.json");

		Assert.True(package.IsPlainConfigFile);
		Assert.Null(package.AppVersion);
		Assert.Null(package.ExportedUtc);
		Assert.Equal("ZOB", package.Values[ArtccKey]);
		Assert.Equal("Beta", package.Values[UserConfigKeys.UpdateChannel]);
	}

	/// <summary>Anything that is not an FE-Buddy settings file is refused with a message for the user.</summary>
	[Theory]
	[InlineData("{ not json", "not valid JSON")]
	[InlineData("[1, 2]", "not an FE-Buddy settings file")]
	[InlineData("""{ "name": "something else" }""", "not an FE-Buddy settings file")]
	[InlineData("""{ "General": "flat" }""", "not an FE-Buddy settings file")]
	[InlineData("""{ "format": "Other.Format", "formatVersion": 1, "settings": {} }""", "not an FE-Buddy settings file")]
	[InlineData("""{ "format": 7, "formatVersion": 1, "settings": {} }""", "not an FE-Buddy settings file")]
	[InlineData("""{ "format": "FE-Buddy.UserConfig", "settings": {} }""", "does not say which settings format")]
	[InlineData("""{ "format": "FE-Buddy.UserConfig", "formatVersion": "one", "settings": {} }""", "does not say which settings format")]
	[InlineData("""{ "format": "FE-Buddy.UserConfig", "formatVersion": 1 }""", "has no settings")]
	[InlineData("""{ "format": "FE-Buddy.UserConfig", "formatVersion": 99, "appVersion": "v9.0.0", "settings": {} }""", "FE-Buddy v9.0.0, which is newer")]
	[InlineData("""{ "format": "FE-Buddy.UserConfig", "formatVersion": 99, "settings": {} }""", "a newer FE-Buddy")]
	public void parse_refuses_what_it_cannot_import(string json, string expectedMessage)
	{
		UserConfigTransferException ex = Assert.Throws<UserConfigTransferException>(() => UserConfigTransfer.Parse(json, "x.json"));

		Assert.Contains(expectedMessage, ex.Message, StringComparison.Ordinal);
	}

	/// <summary>An unreadable export date is dropped rather than failing the import.</summary>
	[Fact]
	public void parse_ignores_a_bad_export_date()
	{
		UserConfigPackage package = UserConfigTransfer.Parse(
			"""{ "format": "FE-Buddy.UserConfig", "formatVersion": 1, "exportedUtc": "yesterday", "settings": { "General": { "PrettyPrintGeojson": true } } }""",
			"x.json");

		Assert.Null(package.ExportedUtc);
		Assert.Equal("true", package.Values["General.PrettyPrintGeojson"]);
	}

	// ============================ plan ============================

	/// <summary>
	/// The file's shared settings replace this PC's (dropping shared settings the file leaves out),
	/// while this PC's own state stays and the file's is ignored.
	/// </summary>
	[Fact]
	public void plan_replaces_shared_settings_and_keeps_this_pcs_state()
	{
		Dictionary<string, string> current = new()
		{
			[ArtccKey] = "ZNY",
			["Services.AiracService.Fixes.OutputBy"] = "Use",
			[UserConfigKeys.UpdateChannel] = "Beta",
			[UserConfigKeys.NewsLastOpen] = "2026-08-30.3",
			[CycleKey] = "2609",
			["Window.Left"] = "10",
		};

		UserConfigPackage package = Package(new()
		{
			[ArtccKey] = "ZOB",
			[CycleKey] = "2610",
			["General.PrettyPrintGeojson"] = "Y",
			[UserConfigKeys.UpdateChannel] = "Alpha",
			["Secrets.GitHub.Pat"] = "ghp_secret",
			["Services..Broken"] = "x",
		});

		UserConfigImportPlan plan = UserConfigTransfer.Plan(package, current, Bob, _ => true);

		Assert.Equal("ZOB", plan.Settings[ArtccKey]);
		Assert.Equal("Y", plan.Settings["General.PrettyPrintGeojson"]);
		Assert.False(plan.Settings.ContainsKey("Services.AiracService.Fixes.OutputBy"));
		Assert.Equal("Beta", plan.Settings[UserConfigKeys.UpdateChannel]);
		Assert.Equal("2610", plan.Settings[CycleKey]);
		Assert.Equal("2026-08-30.3", plan.Settings[UserConfigKeys.NewsLastOpen]);
		Assert.Equal("10", plan.Settings["Window.Left"]);
		Assert.False(plan.Settings.ContainsKey("Secrets.GitHub.Pat"));

		// ARTCC and cycle changed, pretty print added, Fixes removed.
		Assert.Equal(4, plan.ChangedCount);
		Assert.True(plan.HasChanges);
		Assert.Equal<string>([UserConfigKeys.UpdateChannel, "Secrets.GitHub.Pat", "Services..Broken"], plan.IgnoredKeys);

		// Only settings FE-Buddy has a name for are listed; the unknown Window section is kept but not named.
		Assert.Equal<string>(["News read status", "Update channel"], plan.KeptForThisPc);
	}

	/// <summary>Importing a file that matches this PC changes nothing.</summary>
	[Fact]
	public void plan_of_identical_settings_has_no_changes()
	{
		Dictionary<string, string> current = new() { [ArtccKey] = "ZOB" };

		UserConfigImportPlan plan = UserConfigTransfer.Plan(Package(new() { [ArtccKey] = "ZOB" }), current, Bob, _ => true);

		Assert.Equal(0, plan.ChangedCount);
		Assert.False(plan.HasChanges);
	}

	/// <summary>
	/// Folders match the file where they can work here - an output folder only needs its drive, a
	/// source folder must exist - and this PC keeps its own where they cannot. A folder the file
	/// does not set goes back to the default. Network paths are refused without being looked up.
	/// </summary>
	[Fact]
	public void plan_matches_folders_where_they_work_on_this_pc()
	{
		const string GoneKey = "Services.FileConversions.Gone.SourceFolder";
		const string OtherKey = "Services.FileConversions.Other.SourceFolder";

		Dictionary<string, string> current = new()
		{
			[UserConfigKeys.DefaultOutputDirectory] = @"E:\Mine",
			[DatFolderKey] = @"E:\Dat",
			[SctFolderKey] = @"E:\Sct",
			[EramFolderKey] = @"E:\Eram",
			[GoneKey] = @"E:\Gone",
		};

		UserConfigPackage package = Package(new()
		{
			[UserConfigKeys.DefaultOutputDirectory] = @"%DESKTOP%\FEB",
			[DatFolderKey] = @"D:\VATSIM\DAT",
			[SctFolderKey] = @"\\evil-server\share",
			[EramFolderKey] = "",
			[OtherKey] = @"E:\Same",
		});

		List<string> lookedUp = [];
		HashSet<string> existing = new(StringComparer.OrdinalIgnoreCase) { @"C:\", @"E:\Same" };

		UserConfigImportPlan plan = UserConfigTransfer.Plan(package, current, Bob, path =>
		{
			lookedUp.Add(path);
			return existing.Contains(path);
		});

		// The output folder's drive exists, so it is taken although the folder is not there yet.
		Assert.Equal(@"C:\Users\bob\Desktop\FEB", plan.Settings[UserConfigKeys.DefaultOutputDirectory]);

		// A source folder missing here, and a network path: this PC keeps its own.
		Assert.Equal(@"E:\Dat", plan.Settings[DatFolderKey]);
		Assert.Equal(@"E:\Sct", plan.Settings[SctFolderKey]);

		// Blank in the file, or not in it at all: back to the default.
		Assert.False(plan.Settings.ContainsKey(EramFolderKey));
		Assert.False(plan.Settings.ContainsKey(GoneKey));

		// A source folder that exists here is taken.
		Assert.Equal(@"E:\Same", plan.Settings[OtherKey]);

		Assert.Collection(
			plan.AppliedFolders,
			f =>
			{
				Assert.Equal(UserConfigKeys.DefaultOutputDirectory, f.Key);
				Assert.Equal("Default output directory", f.Label);
				Assert.Equal("created when FE-Buddy first writes to it", f.Note);
			},
			f =>
			{
				Assert.Equal(EramFolderKey, f.Key);
				Assert.Equal(string.Empty, f.Path);
				Assert.Equal("not set in the file, so the default is used", f.Note);
			},
			f => Assert.Equal(GoneKey, f.Key),
			f =>
			{
				Assert.Equal(OtherKey, f.Key);
				Assert.Null(f.Note);
			});

		Assert.Collection(
			plan.SkippedFolders,
			f =>
			{
				Assert.Equal(DatFolderKey, f.Key);
				Assert.Equal("is not found on this PC", f.Note);
			},
			f =>
			{
				Assert.Equal(SctFolderKey, f.Key);
				Assert.Equal("is not a folder on a drive of this PC", f.Note);
			});

		Assert.DoesNotContain(lookedUp, p => p.StartsWith(@"\\", StringComparison.Ordinal));
	}

	/// <summary>An output folder on a drive this PC does not have leaves this PC's own in place.</summary>
	[Fact]
	public void plan_keeps_this_pcs_output_folder_when_the_drive_is_missing()
	{
		Dictionary<string, string> current = new() { [UserConfigKeys.DefaultOutputDirectory] = @"E:\Mine" };

		UserConfigImportPlan plan = UserConfigTransfer.Plan(
			Package(new() { [UserConfigKeys.DefaultOutputDirectory] = @"Q:\Out" }), current, Bob, path => path != @"Q:\");

		Assert.Equal(@"E:\Mine", plan.Settings[UserConfigKeys.DefaultOutputDirectory]);
		Assert.Equal("is on a drive this PC does not have", Assert.Single(plan.SkippedFolders).Note);
		Assert.False(plan.HasChanges);
	}

	/// <summary>
	/// Folders under another user's profile, as a plain <c>UserConfig.json</c> has them, move to
	/// this user's, so the other user's name is never imported.
	/// </summary>
	[Fact]
	public void plan_moves_another_users_folders_to_this_user()
	{
		UserConfigImportPlan plan = UserConfigTransfer.Plan(
			Package(new()
			{
				[UserConfigKeys.DefaultOutputDirectory] = @"C:\Users\alice\OneDrive\Desktop\FEB",
				[DatFolderKey] = @"C:\Users\alice\VATSIM\DAT",
			}),
			new Dictionary<string, string>(),
			Bob,
			_ => true);

		Assert.Equal(@"C:\Users\bob\Desktop\FEB", plan.Settings[UserConfigKeys.DefaultOutputDirectory]);
		Assert.Equal(@"C:\Users\bob\VATSIM\DAT", plan.Settings[DatFolderKey]);
		Assert.DoesNotContain(plan.Settings.Values, v => v.Contains("alice", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>A folder the file shares with this PC's current value is taken quietly, not listed as applied.</summary>
	[Fact]
	public void plan_does_not_list_a_folder_that_is_already_set()
	{
		Dictionary<string, string> current = new() { [DatFolderKey] = @"d:\vatsim\dat" };

		UserConfigImportPlan plan = UserConfigTransfer.Plan(Package(new() { [DatFolderKey] = @"D:\VATSIM\DAT" }), current, Bob, _ => true);

		Assert.Empty(plan.AppliedFolders);
		Assert.Equal(@"D:\VATSIM\DAT", plan.Settings[DatFolderKey]);
	}

	/// <summary>A relative folder or a bare drive letter is not a full path, so it is skipped.</summary>
	[Theory]
	[InlineData(@"VATSIM\DAT")]
	[InlineData("D:")]
	[InlineData(@"1:\x")]
	[InlineData(@"D;\x")]
	[InlineData("D:x")]
	public void plan_skips_a_folder_that_is_not_a_full_drive_path(string folder)
	{
		UserConfigImportPlan plan = UserConfigTransfer.Plan(
			Package(new() { [DatFolderKey] = folder }), new Dictionary<string, string>(), Bob, _ => true);

		Assert.Equal("is not a folder on a drive of this PC", Assert.Single(plan.SkippedFolders).Note);
	}

	/// <summary>A drive path written with forward slashes is still a drive path.</summary>
	[Fact]
	public void plan_takes_a_drive_path_with_forward_slashes()
	{
		UserConfigImportPlan plan = UserConfigTransfer.Plan(
			Package(new() { [DatFolderKey] = "D:/VATSIM/DAT" }), new Dictionary<string, string>(), Bob, _ => true);

		Assert.Equal("D:/VATSIM/DAT", plan.Settings[DatFolderKey]);
		Assert.Empty(plan.SkippedFolders);
	}

	/// <summary>The public overload plans against the live config and this PC's folders.</summary>
	[Fact]
	public void plan_of_the_live_config_uses_it()
	{
		UserConfigFile.TrySetValue(ArtccKey, "ZNY");
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Beta");

		UserConfigImportPlan plan = UserConfigTransfer.Plan(Package(new()
		{
			[ArtccKey] = "ZOB",
			[DatFolderKey] = Path.GetTempPath(),
		}));

		Assert.Equal("ZOB", plan.Settings[ArtccKey]);
		Assert.Equal("Beta", plan.Settings[UserConfigKeys.UpdateChannel]);
		Assert.Equal(Path.GetTempPath(), plan.Settings[DatFolderKey]);
	}

	// ============================ apply ============================

	/// <summary>Applying writes the planned settings to disk and keeps the replaced file.</summary>
	[Fact]
	public void apply_writes_the_plan_and_keeps_the_old_file()
	{
		UserConfigFile.TrySetValue(ArtccKey, "ZNY");
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Beta");
		UserConfigFile.Write();

		UserConfigImportPlan plan = UserConfigTransfer.Plan(Package(new() { [ArtccKey] = "ZOB" }));
		UserConfigTransfer.Apply(plan);

		UserConfigFile.ReadAll();
		Assert.Equal("ZOB", UserConfigFile.GetValue(ArtccKey));
		Assert.Equal("Beta", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));

		JsonNode backup = JsonNode.Parse(File.ReadAllText(UserConfigFile.BeforeImportFilePath))!;
		Assert.Equal("ZNY", backup["Services"]!["AiracService"]!["UserArtccId"]!.GetValue<string>());
	}

	/// <summary>Export on one PC, import on another: the facility setup arrives and the folder follows the new user.</summary>
	[Fact]
	public void export_then_import_moves_a_setup_between_users()
	{
		Dictionary<string, string> alice = new()
		{
			[ArtccKey] = "ZOB",
			[UserConfigKeys.DefaultOutputDirectory] = @"C:\Users\alice\Desktop",
			[UserConfigKeys.UpdateChannel] = "Alpha",
		};
		Dictionary<string, string> bob = new() { [UserConfigKeys.UpdateChannel] = "Stable" };

		UserConfigTransfer.Export(ExportPath, alice, "3.1.0", ExportedAt, Alice);
		UserConfigImportPlan plan = UserConfigTransfer.Plan(UserConfigTransfer.Read(ExportPath), bob, Bob, _ => true);

		Assert.Equal("ZOB", plan.Settings[ArtccKey]);
		Assert.Equal(@"C:\Users\bob\Desktop", plan.Settings[UserConfigKeys.DefaultOutputDirectory]);
		Assert.Equal("Stable", plan.Settings[UserConfigKeys.UpdateChannel]);
	}

	/// <summary>The exception keeps the standard constructors.</summary>
	[Fact]
	public void transfer_exception_has_the_standard_constructors()
	{
		Assert.NotNull(new UserConfigTransferException().Message);
		Assert.Equal("m", new UserConfigTransferException("m").Message);
	}

	private static UserConfigPackage Package(Dictionary<string, string> values) =>
		new("test.json", UserConfigTransfer.FormatVersion, "3.1.0", ExportedAt, values);
}
