using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers what the Review tab says about each sub-service's run (W2-37): the alias file by the name the run wrote
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

	/// <summary>An alias file renamed on the File Names tab is called by its new name.</summary>
	[Fact]
	public void each_alias_file_is_named_as_the_run_wrote_it()
	{
		AiracServiceResult result = Run(r => r with
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
		});

		Assert.Contains("ZOB_Airports.txt: 240 alias command(s)", new AirportsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_STARs.txt: 3 alias command(s)", new ArrivalsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_SIDs.txt: 2 alias command(s)", new DeparturesViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_Navaids.txt: 10 alias command(s)", new NavaidsViewModel().DescribeRunResult(result)!.Summary);
		Assert.Contains("ZOB_Airways.txt: 4 alias line(s)", new AirwaysViewModel().DescribeRunResult(result)!.Summary);
	}
}
