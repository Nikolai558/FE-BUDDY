using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers a run that doesn't export each sub-service's own alias file
/// (<see cref="AiracServiceSettings.ExportIndividualAliasFiles"/> off, the default): the files are written to the
/// run's scratch folder under FE-Buddy's names and combined from there, <c>Aliases</c> holds only
/// <c>Combined_Alias.txt</c>, an earlier run's individual files go, and the scratch folder goes however the run
/// ends. <see cref="AiracServiceTests"/> covers a run that exports them.
/// </summary>
[Collection("AppLog")]
public sealed class AiracServiceAliasExportTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AliasExport_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log and FE-Buddy's scratch area at throwaway folders.</summary>
	public AiracServiceAliasExportTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		TempWorkspace.ConfigureForTesting(Path.Combine(_root, "temp"));
	}

	/// <summary>Restores both and deletes the folder.</summary>
	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
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

	private static AiracCycleInfo Cycle => new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

	private string Output => Path.Combine(_root, "out");

	private string Aliases => Path.Combine(Output, "FE-Buddy_Output", "AIRAC_2610", "Aliases");

	/// <summary>Where the runs' unexported alias files go: a folder per run under it.</summary>
	private string Scratch => Path.Combine(_root, "temp", AiracService.UnexportedAliasFolder);

	/// <summary>Airports and Departures, alias files only, not exported.</summary>
	private AiracServiceSettings Settings() => new()
	{
		SelectedCycle = Cycle,
		OutputDirectory = Output,
		Airports = new Dictionary<string, string> { { "Area", "Everything" }, { "GenerateGeojson", "N" } },
		Departures = new Dictionary<string, string> { { "Area", "Everything" }, { "GenerateGeojson", "N" } },
	};

	private bool ScratchIsEmpty() => !Directory.Exists(Scratch) || !Directory.EnumerateFileSystemEntries(Scratch).Any();

	private string[] FilesInAliases() => [.. Directory.EnumerateFiles(Aliases).Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal)];

	[Fact]
	public void the_option_is_off_by_default()
	{
		Assert.False(Settings().ExportIndividualAliasFiles);
	}

	/// <summary>
	/// Only Combined_Alias.txt - under the name the user gave it - is in Aliases. Its sections are headed with
	/// FE-Buddy's names, the users' renames of the files themselves aside, and no result names a file that went.
	/// </summary>
	[Fact]
	public async Task aliases_holds_only_the_combined_file_its_sections_headed_with_fe_buddys_names()
	{
		AiracServiceSettings settings = Settings() with
		{
			FileNames = new Dictionary<string, string> { ["Airports.txt"] = "ZOB Airports", ["Combined_Alias.txt"] = "ZOB vNAS" },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss(), new AiracSupplementalData());

		Assert.Equal(["ZOB vNAS.txt"], FilesInAliases());
		string combined = File.ReadAllText(Path.Combine(Aliases, "ZOB vNAS.txt"));
		Assert.Contains("; ----- Airports.txt -----", combined, StringComparison.Ordinal);
		Assert.Contains("; ----- Departures.txt -----", combined, StringComparison.Ordinal);
		Assert.Equal(["Airports.txt", "Departures.txt"], result.CombinedAlias!.FeBuddyFiles);

		Assert.False(result.IndividualAliasFilesExported);
		Assert.Null(result.Airports!.AliasFilePath);
		Assert.Null(result.Departures!.AliasFilePath);
		Assert.True(result.Airports.AliasCommandCount > 0);
		Assert.Equal("ZOB vNAS.txt", result.AliasCommandsSavedIn(null, result.Airports.AliasCommandCount));
		Assert.Equal(
			$"{result.Airports.AliasCommandCount:N0} alias command(s), in ZOB vNAS.txt",
			result.DescribeAliasCommands(result.Airports.AliasFilePath, result.Airports.AliasCommandCount, "alias command(s)"));
		Assert.Equal(
			[result.DuplicateAliasReport!.FilePath, Path.Combine(Aliases, "ZOB vNAS.txt")],
			result.FilesWritten);
		Assert.True(ScratchIsEmpty());
	}

	/// <summary>The duplicate report names each file as its section of the combined file, and the user's choices still settle duplicates.</summary>
	[Fact]
	public async Task duplicates_are_settled_and_reported_by_section()
	{
		AiracServiceSettings settings = Settings() with
		{
			Airports = null,
			Arrivals = new Dictionary<string, string> { { "Area", "Everything" }, { "GenerateGeojson", "N" } },
		};

		AiracServiceResult reported = await AiracService.RunAsync(settings, AiracServiceTests.NutiyAtOrf(), new AiracSupplementalData());

		string report = File.ReadAllText(reported.DuplicateAliasReport!.FilePath);
		Assert.Contains(".orfNUTIYf", Assert.Single(reported.DuplicateAliasReport.Duplicates).Command, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Files checked: Departures.txt, Arrivals.txt, in Combined_Alias.txt" + Environment.NewLine, report, StringComparison.Ordinal);
		Assert.Contains("Those files aren't exported: each is a section of Combined_Alias.txt", report, StringComparison.Ordinal);

		AiracServiceResult settled = await AiracService.RunAsync(
			settings with
			{
				DuplicateAliasChoices =
				[
					new DuplicateAliasRule("Departures.txt", ".orfNUTIYf", 1, DuplicateAliasAction.Keep),
					new DuplicateAliasRule("Arrivals.txt", ".orfNUTIYf", 1, DuplicateAliasAction.Rename, ".orfNUTIYa"),
				],
			},
			AiracServiceTests.NutiyAtOrf(),
			new AiracSupplementalData());

		string combined = File.ReadAllText(settled.CombinedAlias!.FilePath!);
		Assert.Empty(settled.DuplicateAliasReport!.Duplicates);
		Assert.Contains(".orfNUTIYa .FF ", combined, StringComparison.Ordinal);
		Assert.Equal(["Combined_Alias.txt"], FilesInAliases());
		Assert.True(ScratchIsEmpty());
	}

	/// <summary>
	/// Every individual file an earlier run left goes, under FE-Buddy's name and under the user's, whether or not
	/// this run writes it; anything else in Aliases stays.
	/// </summary>
	[Fact]
	public async Task an_earlier_runs_individual_files_are_deleted_under_both_names()
	{
		Directory.CreateDirectory(Aliases);

		foreach (string name in new[] { "Airports.txt", "ZOB Airports.txt", "Navaids.txt", "Notes.txt" })
		{
			File.WriteAllText(Path.Combine(Aliases, name), ".old last run's aliases");
		}

		AiracServiceResult result = await AiracService.RunAsync(
			Settings() with { FileNames = new Dictionary<string, string> { ["Airports.txt"] = "ZOB Airports" } },
			DepartureTestData.Dotss(),
			new AiracSupplementalData());

		Assert.Equal(["Combined_Alias.txt", "Notes.txt"], FilesInAliases());
		ServiceMessage deleted = Assert.Single(result.Messages, m => m.Text.Contains("which an earlier run left in Aliases", StringComparison.Ordinal));
		Assert.Equal(LogLevel.Info, deleted.Level);
		Assert.True(deleted.IsAdvisory);
		Assert.Equal(
			"Airports.txt, ZOB Airports.txt and Navaids.txt, which an earlier run left in Aliases, were deleted so they can't be taken for this run's.",
			deleted.Text);
	}

	/// <summary>One that can't be deleted is a warning not to upload it; the others still go.</summary>
	[Fact]
	public async Task an_earlier_file_that_cannot_be_deleted_is_a_warning()
	{
		Directory.CreateDirectory(Aliases);
		string navaids = Path.Combine(Aliases, "Navaids.txt");
		File.WriteAllText(navaids, ".old");
		File.WriteAllText(Path.Combine(Aliases, "Airports.txt"), ".old");
		AiracServiceResult result;

		using (new FileStream(navaids, FileMode.Open, FileAccess.Read, FileShare.None))
		{
			result = await AiracService.RunAsync(Settings(), DepartureTestData.Dotss(), new AiracSupplementalData());
		}

		ServiceMessage warning = Assert.Single(result.Messages, m => m.Text.Contains("which an earlier run left in Aliases", StringComparison.Ordinal));
		Assert.Equal(LogLevel.Warning, warning.Level);
		Assert.StartsWith("Navaids.txt, which an earlier run left in Aliases, could not be deleted (", warning.Text, StringComparison.Ordinal);
		Assert.EndsWith("): it isn't this run's, so don't upload it. Airports.txt was deleted.", warning.Text, StringComparison.Ordinal);
		Assert.False(File.Exists(Path.Combine(Aliases, "Airports.txt")));
	}

	/// <summary>A run that writes no alias file - nothing to combine - leaves an earlier run's files alone, as it does Combined_Alias.txt.</summary>
	[Fact]
	public async Task a_run_that_writes_no_alias_file_leaves_earlier_ones_alone()
	{
		Directory.CreateDirectory(Aliases);
		File.WriteAllText(Path.Combine(Aliases, "Airports.txt"), ".old");

		AiracServiceResult result = await AiracService.RunAsync(
			Settings() with { Airports = null, Departures = null, Fixes = new Dictionary<string, string> { { "Area", "Everything" } } },
			DepartureTestData.Dotss(),
			new AiracSupplementalData());

		Assert.True(File.Exists(Path.Combine(Aliases, "Airports.txt")));
		Assert.Null(result.CombinedAlias);
	}

	/// <summary>A run that exports them keeps today's behaviour: an earlier run's other files stay.</summary>
	[Fact]
	public async Task a_run_that_exports_them_leaves_earlier_ones_alone()
	{
		Directory.CreateDirectory(Aliases);
		File.WriteAllText(Path.Combine(Aliases, "Navaids.txt"), ".old");

		AiracServiceResult result = await AiracService.RunAsync(
			Settings() with { ExportIndividualAliasFiles = true }, DepartureTestData.Dotss(), new AiracSupplementalData());

		Assert.True(result.IndividualAliasFilesExported);
		Assert.Equal(["Airports.txt", "Combined_Alias.txt", "Departures.txt", "Navaids.txt"], FilesInAliases());
		Assert.Equal(Path.Combine(Aliases, "Airports.txt"), result.Airports!.AliasFilePath);
	}

	/// <summary>Stopped at the duplicate review: nothing is saved to Aliases, an earlier combined file goes, and so does the scratch folder.</summary>
	[Fact]
	public async Task stopping_at_the_review_saves_nothing_to_aliases()
	{
		Directory.CreateDirectory(Aliases);
		File.WriteAllText(Path.Combine(Aliases, "Combined_Alias.txt"), ".old last run's aliases");
		AiracServiceSettings settings = Settings() with
		{
			Airports = null,
			Arrivals = new Dictionary<string, string> { { "Area", "Everything" }, { "GenerateGeojson", "N" } },
			ReviewDuplicateAliases = true,
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, AiracServiceTests.NutiyAtOrf(), new AiracSupplementalData(), reviewDuplicates: (_, _) => Task.FromResult<IReadOnlyList<DuplicateAliasRule>?>(null));

		Assert.True(result.StoppedAtDuplicateReview);
		Assert.Empty(FilesInAliases());
		Assert.Null(result.Departures!.AliasFilePath);
		Assert.Null(result.AliasCommandsSavedIn(null, result.Departures.AliasCommandCount));
		Assert.Contains("Files checked: Departures.txt, Arrivals.txt" + Environment.NewLine, File.ReadAllText(result.DuplicateAliasReport!.FilePath), StringComparison.Ordinal);
		Assert.Contains(result.Messages, m => m.Text.StartsWith("You stopped the run at the duplicate alias commands, so it saved no alias file and no Combined_Alias.txt.", StringComparison.Ordinal));
		Assert.True(ScratchIsEmpty());
	}

	/// <summary>A run that fails after an alias file was written leaves no scratch folder behind.</summary>
	[Fact]
	public async Task the_scratch_folder_goes_when_the_run_fails()
	{
		// Combined_Alias.txt open elsewhere: the run fails writing it, after Airports and Departures wrote theirs.
		Directory.CreateDirectory(Aliases);

		using (new FileStream(Path.Combine(Aliases, "Combined_Alias.txt"), FileMode.Create, FileAccess.ReadWrite, FileShare.None))
		{
			await Assert.ThrowsAnyAsync<IOException>(() => AiracService.RunAsync(Settings(), DepartureTestData.Dotss(), new AiracSupplementalData()));
		}

		Assert.True(Directory.Exists(Scratch), "The alias files were written to the scratch area before the run failed.");
		Assert.True(ScratchIsEmpty());
	}

	/// <summary>A run cancelled after an alias file was written leaves no scratch folder behind.</summary>
	[Fact]
	public async Task the_scratch_folder_goes_when_the_run_is_cancelled()
	{
		using CancellationTokenSource cancel = new();
		SynchronousProgress progress = new(report =>
		{
			if (report.SubService == "Airports" && report.PercentComplete >= 100)
			{
				cancel.Cancel();
			}
		});

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => AiracService.RunAsync(Settings(), DepartureTestData.Dotss(), new AiracSupplementalData(), progress, cancel.Token));

		Assert.True(Directory.Exists(Scratch));
		Assert.True(ScratchIsEmpty());
	}

	private sealed class SynchronousProgress(Action<AiracServiceProgress> report) : IProgress<AiracServiceProgress>
	{
		public void Report(AiracServiceProgress value) => report(value);
	}
}
