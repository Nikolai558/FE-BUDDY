using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Import Settings window's view-model (<see cref="ImportSettingsViewModel"/>): a new
/// profile is the first choice, named after the file's facility when it can be; Import is on only
/// for a usable name or a change to make; and the summary says what the chosen import does.
/// </summary>
[Collection("AppLog")]
public sealed class ImportSettingsViewModelTests
{
	private const string Artcc = "Services.AiracService.UserArtccId";
	private const string Pretty = "General.PrettyPrintGeojson";
	private const string Dat = "Services.FileConversions.DatToGeojson.SourceFolder";

	private static readonly PortablePathTokens Bob = new(
	[
		(PortablePathTokens.DesktopToken, @"C:\Users\bob\Desktop"),
		(PortablePathTokens.UserProfileToken, @"C:\Users\bob"),
	]);

	private static ImportSettingsViewModel Choice(
		Dictionary<string, string> file,
		Dictionary<string, string> current,
		IReadOnlyList<string>? profiles = null,
		IReadOnlyList<string>? unsaved = null,
		string? appVersion = "3.1.0",
		Func<string, bool>? directoryExists = null)
	{
		UserConfigPackage package = new("zob.json", UserConfigTransfer.FormatVersion, appVersion, new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero), file);

		return new ImportSettingsViewModel(
			UserConfigTransfer.Plan(package, current, Bob, directoryExists ?? (_ => true), mode: UserConfigImportMode.Replace),
			UserConfigTransfer.Plan(package, current, Bob, directoryExists ?? (_ => true), mode: UserConfigImportMode.Merge),
			"Default",
			profiles ?? ["Default"],
			unsaved ?? [],
			"v3.1.0");
	}

	[Fact]
	public void a_new_profile_named_after_the_files_facility_is_the_first_choice()
	{
		ImportSettingsViewModel choice = Choice(new() { [Artcc] = "ZOB" }, new() { [Artcc] = "ZOB" });

		Assert.Equal(ImportDestination.NewProfile, choice.Destination);
		Assert.True(choice.IsNewProfile);
		Assert.Equal("ZOB", choice.NewProfileName);
		Assert.True(choice.CanImport);
		Assert.Equal("ZOB gets the file's settings.", choice.Summary);
		Assert.Equal("zob.json, exported from FE-Buddy v3.1.0 on 7 Oct 2026.", choice.FromFile);
		Assert.Equal("FE-Buddy switches to it. Default stays as it is.", choice.NewProfileDescription);
		Assert.Equal("Add them to Default", choice.MergeLabel);
		Assert.Equal("Replace Default's settings with them", choice.ReplaceLabel);
	}

	/// <summary>A facility a profile already has, or none, leaves the name to the user; Import waits for one.</summary>
	[Fact]
	public void the_name_must_be_usable_and_not_taken()
	{
		ImportSettingsViewModel choice = Choice(new() { [Artcc] = "zob" }, [], profiles: ["Default", "ZOB"]);

		Assert.Equal(string.Empty, choice.NewProfileName);
		Assert.False(choice.CanImport);
		Assert.False(choice.ImportCommand.CanExecute(null));
		Assert.Null(choice.NameProblem);
		Assert.Equal("The new profile gets the file's settings.", choice.Summary);

		choice.NewProfileName = "Zob";
		Assert.Equal("There is already a profile called ZOB.", choice.NameProblem);
		Assert.True(choice.HasNameProblem);
		Assert.False(choice.CanImport);

		choice.NewProfileName = "ZOB 2";
		Assert.True(choice.CanImport);

		// Only a new profile has a name to check.
		choice.NewProfileName = "a:b";
		choice.IsMerge = true;
		Assert.Null(choice.NameProblem);
	}

	[Fact]
	public void merge_and_replace_say_what_changes_in_the_profile()
	{
		ImportSettingsViewModel choice = Choice(new() { [Artcc] = "ZOB" }, new() { [Artcc] = "ZNY", [Pretty] = "Y" }, unsaved: ["Settings"]);

		choice.IsMerge = true;
		Assert.Equal(ImportDestination.Merge, choice.Destination);
		Assert.Equal(UserConfigImportMode.Merge, choice.Plan.Mode);
		Assert.True(choice.CanImport);
		Assert.StartsWith("1 setting changes in Default.", choice.Summary, StringComparison.Ordinal);
		Assert.Contains("Unsaved changes on Settings will be lost.", choice.Summary, StringComparison.Ordinal);
		Assert.EndsWith("Default's settings as they are now are kept in UserConfig-before-import.Default.json in case you want them back.", choice.Summary, StringComparison.Ordinal);

		choice.IsReplace = true;
		Assert.Equal(UserConfigImportMode.Replace, choice.Plan.Mode);
		Assert.StartsWith("2 settings change in Default.", choice.Summary, StringComparison.Ordinal);
		Assert.True(choice.IsReplace && !choice.IsMerge && !choice.IsNewProfile);
	}

	/// <summary>Merging a file whose settings the profile already has changes nothing, so Import is off.</summary>
	[Fact]
	public void nothing_to_change_turns_import_off()
	{
		ImportSettingsViewModel choice = Choice(new() { [Artcc] = "ZOB" }, new() { [Artcc] = "ZOB", [Pretty] = "Y" });

		choice.IsMerge = true;

		Assert.False(choice.CanImport);
		Assert.Equal("Nothing changes: Default already has these settings.", choice.Summary);

		choice.IsReplace = true;
		Assert.True(choice.CanImport);
	}

	/// <summary>The summary names a different version, and lists folders taken and not, a few at most.</summary>
	[Fact]
	public void the_summary_names_versions_and_folders()
	{
		Dictionary<string, string> file = new() { [Dat] = @"D:\Dat" };
		for (int i = 1; i <= 10; i++)
		{
			file[$"Services.FileConversions.T{i}.SourceFolder"] = $@"Q:\Gone{i}";
		}

		ImportSettingsViewModel choice = Choice(file, [], appVersion: "3.0.0", directoryExists: path => !path.StartsWith('Q'));
		string summary = choice.Summary;

		Assert.Contains("This PC runs FE-Buddy v3.1.0, the file came from v3.0.0.", summary, StringComparison.Ordinal);
		Assert.Contains("Folders and files:\n  • ", summary, StringComparison.Ordinal);
		Assert.Contains(@"`D:\Dat`", summary, StringComparison.Ordinal);
		Assert.Contains("Not taken, as they do not work on this PC:", summary, StringComparison.Ordinal);
		Assert.Contains("  • …and 2 more", summary, StringComparison.Ordinal);
	}

	[Fact]
	public void a_plain_file_or_one_without_a_version_says_so()
	{
		UserConfigPackage plain = UserConfigTransfer.Parse("""{ "General": { "PrettyPrintGeojson": "Y" } }""", "UserConfig.json");
		UserConfigImportPlan plan = UserConfigTransfer.Plan(plain, new Dictionary<string, string>(), Bob, _ => true);

		Assert.Equal("UserConfig.json, a settings file from another PC.", new ImportSettingsViewModel(plan, plan, "Default", [], [], "3.1.0").FromFile);
		Assert.Equal("zob.json.", Choice([], [], appVersion: null).FromFile);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void either_button_closes_the_window_and_says_which(bool confirm)
	{
		ImportSettingsViewModel choice = Choice(new() { [Artcc] = "ZOB" }, []);
		int closes = 0;
		choice.CloseRequested += (_, _) => closes++;

		(confirm ? choice.ImportCommand : choice.CancelCommand).Execute(null);

		Assert.Equal(1, closes);
		Assert.Equal(confirm, choice.Confirmed);
	}

	[Fact]
	public void nothing_is_refused()
	{
		ImportSettingsViewModel choice = Choice([], []);

		Assert.Throws<ArgumentNullException>(() => new ImportSettingsViewModel(null!, choice.Plan, "Default", [], [], "3.1.0"));
		Assert.Throws<ArgumentNullException>(() => new ImportSettingsViewModel(choice.Plan, null!, "Default", [], [], "3.1.0"));
		Assert.Throws<ArgumentNullException>(() => new ImportSettingsViewModel(choice.Plan, choice.Plan, "Default", null!, [], "3.1.0"));
		Assert.Throws<ArgumentNullException>(() => new ImportSettingsViewModel(choice.Plan, choice.Plan, "Default", [], null!, "3.1.0"));
	}
}
