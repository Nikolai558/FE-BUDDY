using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers what the Review tab says about each sub-service's run: the alias file by the name the run wrote
/// it under, a name from the File Names tab included, and Airports' ROI count only when the tab used an ROI -
/// against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class RunResultSummaryTests : IDisposable
{
	private const string Aliases = @"C:\Out\AIRAC_2610\Aliases";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_RunSummary_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public RunResultSummaryTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
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

	private static AiracServiceResult Run(Func<AiracServiceResult, AiracServiceResult> with) =>
		with(new AiracServiceResult { OutputDirectory = @"C:\Out\AIRAC_2610", Messages = [], Elapsed = TimeSpan.Zero });

	private static AirportServiceResult Airports(string? aliasFile) => new()
	{
		AirportCount = 120,
		AirportsInRoiCount = 40,
		GeojsonFilesWritten = [],
		GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
		AliasFilePath = aliasFile is null ? null : Path.Combine(Aliases, aliasFile),
		AliasCommandCount = 240,
		Messages = [],
		Elapsed = TimeSpan.Zero,
	};

	/// <summary>With no ROI, Airports doesn't say how many airports are in one.</summary>
	[Fact]
	public void airports_counts_the_roi_only_when_it_uses_one()
	{
		AirportsViewModel tab = new() { Area = SubServiceArea.Everything };
		AiracServiceResult result = Run(r => r with { Airports = Airports("Airports.txt") });

		Assert.Equal("120 airports, Airports.txt: 240 alias command(s)", tab.DescribeRunResult(result)!.Summary);

		tab.Area = SubServiceArea.Roi;
		Assert.Equal("120 airports, 40 in the ROI, Airports.txt: 240 alias command(s)", tab.DescribeRunResult(result)!.Summary);

		Assert.Equal("120 airports, 40 in the ROI", tab.DescribeRunResult(Run(r => r with { Airports = Airports(null) }))!.Summary);
	}

	/// <summary>A run in which every sub-service wrote an alias file, each renamed on the File Names tab.</summary>
	private static AiracServiceResult EveryAliasFile() => Run(r => r with
	{
		Airports = Airports("ZOB_Airports.txt"),
		Arrivals = new ArrivalServiceResult
		{
			ProcedureCount = 3,
			ProceduresInScopeCount = 3,
			AirportProcedureCount = 3,
			SkippedForMissingPointsCount = 0,
			GeojsonFilesWritten = [],
			GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
			AliasFilePath = Path.Combine(Aliases, "ZOB_STARs.txt"),
			AliasCommandCount = 3,
			Messages = [],
			Elapsed = TimeSpan.Zero,
		},
		Departures = new DepartureServiceResult
		{
			ProcedureCount = 2,
			ProceduresInScopeCount = 2,
			AirportProcedureCount = 2,
			SkippedForMissingPointsCount = 0,
			GeojsonFilesWritten = [],
			GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
			AliasFilePath = Path.Combine(Aliases, "ZOB_SIDs.txt"),
			AliasCommandCount = 2,
			Messages = [],
			Elapsed = TimeSpan.Zero,
		},
		Navaids = new NavaidServiceResult
		{
			NavaidCount = 5,
			GeojsonNavaidCount = 5,
			GeojsonFilesWritten = [],
			GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
			AliasFilePath = Path.Combine(Aliases, "ZOB_Navaids.txt"),
			AliasCommandCount = 10,
			Messages = [],
			Elapsed = TimeSpan.Zero,
		},
		Airways = new AirwayServiceResult
		{
			AirwayCount = 4,
			GeojsonFilesWritten = [],
			GeojsonFeatureCountsByFile = new Dictionary<string, int>(),
			AliasFilePath = Path.Combine(Aliases, "ZOB_Airways.txt"),
			AliasAirwayLineCount = 4,
			Messages = [],
			Elapsed = TimeSpan.Zero,
		},
		Telephony = new TelephonyServiceResult
		{
			IcaoAssignmentCount = 3,
			SpecialCallSignCount = 2,
			VirtualAirlineCount = 0,
			VatsimRadarVirtualAirlineCount = 0,
			NoDesignatorCount = 0,
			NoTelephonyCount = 0,
			ExpiredCount = 0,
			AliasFilePath = Path.Combine(Aliases, "ZOB_Telephony.txt"),
			AliasCommandCount = 9,
			Messages = [],
			Elapsed = TimeSpan.Zero,
		},
	});

	/// <summary>An alias file renamed on the File Names tab is called by its new name.</summary>
	[Fact]
	public void each_alias_file_is_named_as_the_run_wrote_it()
	{
		AiracServiceResult result = EveryAliasFile();

		Assert.Contains("ZOB_Airports.txt: 240 alias command(s)", new AirportsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_STARs.txt: 3 alias command(s)", new ArrivalsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_SIDs.txt: 2 alias command(s)", new DeparturesViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_Navaids.txt: 10 alias command(s)", new NavaidsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_Airways.txt: 4 alias line(s)", new AirwaysViewModel().DescribeRunResult(result)!.Summary);
		Assert.StartsWith("ZOB_Telephony.txt: 9 command(s)", new TelephonyViewModel().DescribeRunResult(result)!.Summary, StringComparison.Ordinal);
	}

	/// <summary>
	/// When the sub-services' own alias files weren't exported, each tab says its commands are in the combined
	/// file - by the name the run wrote it under - and names no file of its own.
	/// </summary>
	[Fact]
	public void unexported_alias_files_are_described_as_in_the_combined_file()
	{
		AiracServiceResult every = EveryAliasFile();
		AiracServiceResult result = every with
		{
			IndividualAliasFilesExported = false,
			CombinedAlias = new CombinedAliasResult { FilePath = Path.Combine(Aliases, "ZOB vNAS.txt"), Messages = [], Elapsed = TimeSpan.Zero },
			Airports = every.Airports! with { AliasFilePath = null },
			Arrivals = every.Arrivals! with { AliasFilePath = null },
			Departures = every.Departures! with { AliasFilePath = null },
			Navaids = every.Navaids! with { AliasFilePath = null },
			Airways = every.Airways! with { AliasFilePath = null },
			Telephony = every.Telephony! with { AliasFilePath = null },
			Procedures = new ProcedureServiceResult
			{
				AirportCount = 0,
				ProcedureCount = 0,
				NewCount = 0,
				ChangedCount = 0,
				DeletedCount = 0,
				ReAddedCount = 0,
				FilesWritten = [],
				AliasCommandCount = 50,
				AliasAirportCount = 4,
				Messages = [],
				Elapsed = TimeSpan.Zero,
			},
		};

		Assert.Equal("120 airports, 240 alias command(s), in ZOB vNAS.txt", new AirportsViewModel { Area = SubServiceArea.Everything }.DescribeRunResult(result)!.Summary);
		Assert.Contains(", 3 alias command(s), in ZOB vNAS.txt", new ArrivalsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains(", 2 alias command(s), in ZOB vNAS.txt", new DeparturesViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains(", 10 alias command(s), in ZOB vNAS.txt", new NavaidsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains(", 4 alias line(s), in ZOB vNAS.txt", new AirwaysViewModel().DescribeRunResult(result)!.Summary);
		Assert.Equal(
			"9 command(s) for 3 ICAO operator(s) and 2 U.S. special call sign(s), in ZOB vNAS.txt",
			new TelephonyViewModel().DescribeRunResult(result)!.Summary);
		Assert.Equal("50 command(s) for 4 airport(s), in ZOB vNAS.txt", new ProceduresViewModel().DescribeRunResult(result)!.Summary);

		// Stopped at the duplicate review: nothing was saved, so nothing is said.
		AiracServiceResult stopped = result with { CombinedAlias = null, StoppedAtDuplicateReview = true };
		Assert.Equal("Nothing written", new TelephonyViewModel().DescribeRunResult(stopped)!.Summary);
		Assert.DoesNotContain("alias", new NavaidsViewModel().DescribeRunResult(stopped)!.Summary, StringComparison.Ordinal);
	}
}
