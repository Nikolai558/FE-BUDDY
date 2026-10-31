using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Airac.WxStations.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers what a whole AIRAC Service run says it did: every file it wrote
/// (<see cref="AiracServiceResult.FilesWritten"/>) and its one line (<see cref="AiracRunSummary.Describe"/>).
/// </summary>
public sealed class AiracRunSummaryTests
{
	private static readonly Dictionary<string, int> NoCounts = [];

	/// <summary>A run of every sub-service, each having written something.</summary>
	private static AiracServiceResult EveryPart() => new()
	{
		Messages = [],
		Elapsed = TimeSpan.Zero,
		OutputDirectory = "out",
		Airways = new AirwayServiceResult { Messages = [], Elapsed = TimeSpan.Zero, AirwayCount = 1200, ExcludedAirwayIds = ["V1"], GeojsonFilesWritten = ["aw.geojson"], GeojsonFeatureCountsByFile = NoCounts, AliasFilePath = "Airways.txt" },
		Airports = new AirportServiceResult { Messages = [], Elapsed = TimeSpan.Zero, AirportCount = 512, AirportsInRoiCount = 0, GeojsonFilesWritten = ["ap.geojson"], GeojsonFeatureCountsByFile = NoCounts, AliasFilePath = "Airports.txt" },
		Departures = new DepartureServiceResult { Messages = [], Elapsed = TimeSpan.Zero, ProcedureCount = 3, ProceduresInScopeCount = 3, AirportProcedureCount = 3, SkippedForMissingPointsCount = 1, GeojsonFilesWritten = ["dp.geojson"], GeojsonFeatureCountsByFile = NoCounts, AliasFilePath = "Departures.txt" },
		Arrivals = new ArrivalServiceResult { Messages = [], Elapsed = TimeSpan.Zero, ProcedureCount = 2, ProceduresInScopeCount = 2, AirportProcedureCount = 2, SkippedForMissingPointsCount = 0, GeojsonFilesWritten = ["st.geojson"], GeojsonFeatureCountsByFile = NoCounts, AliasFilePath = "Arrivals.txt" },
		Navaids = new NavaidServiceResult { Messages = [], Elapsed = TimeSpan.Zero, NavaidCount = 900, GeojsonNavaidCount = 900, GeojsonFilesWritten = ["nv.geojson"], GeojsonFeatureCountsByFile = NoCounts, AliasFilePath = "Navaids.txt" },
		ArtccBoundaries = new ArtccBoundaryServiceResult { Messages = [], Elapsed = TimeSpan.Zero, LocationCount = 20, RingCount = 40, GeojsonFilesWritten = ["ab.geojson"], GeojsonFeatureCountsByFile = NoCounts },
		Fixes = new FixServiceResult { Messages = [], Elapsed = TimeSpan.Zero, FixCount = 70000, GeojsonFixCount = 10, GeojsonFilesWritten = ["fx.geojson"], GeojsonFeatureCountsByFile = NoCounts },
		WxStations = new WxStationServiceResult { Messages = [], Elapsed = TimeSpan.Zero, StationCount = 30, TotalStationCount = 30, GeojsonStationCount = 30, GeojsonFilesWritten = ["wx.geojson"], GeojsonFeatureCountsByFile = NoCounts },
		Procedures = new ProcedureServiceResult { Messages = [], Elapsed = TimeSpan.Zero, AirportCount = 4, ProcedureCount = 9, NewCount = 1, ChangedCount = 2, DeletedCount = 0, ReAddedCount = 0, FilesWritten = ["Procedure_Changes.md"], AliasFilePath = "FAA_Chart_Recall.txt", AliasCommandCount = 50 },
		Telephony = new TelephonyServiceResult { Messages = [], Elapsed = TimeSpan.Zero, IcaoAssignmentCount = 1, SpecialCallSignCount = 1, NoDesignatorCount = 0, NoTelephonyCount = 0, ExpiredCount = 0, AliasFilePath = "Telephony.txt", AliasCommandCount = 7 },
		DuplicateAliasReport = new DuplicateAliasReportResult("Duplicate_Alias_Commands.txt", [new DuplicateAliasCommand(".x", [])]),
		CombinedAlias = new CombinedAliasResult { Messages = [], Elapsed = TimeSpan.Zero, FilePath = @"Aliases\Combined_Alias.txt", FeBuddyCommandCount = 10, CustomCommandCount = 5 },
	};

	/// <summary>Every file, in sub-service order, then the report and the combined file.</summary>
	[Fact]
	public void every_file_the_run_wrote_is_listed_in_sub_service_order()
	{
		Assert.Equal(
		[
			"aw.geojson", "Airways.txt", "ap.geojson", "Airports.txt", "dp.geojson", "Departures.txt", "st.geojson", "Arrivals.txt",
			"nv.geojson", "Navaids.txt", "ab.geojson", "fx.geojson", "wx.geojson", "Procedure_Changes.md", "FAA_Chart_Recall.txt",
			"Telephony.txt", "Duplicate_Alias_Commands.txt", @"Aliases\Combined_Alias.txt",
		], EveryPart().FilesWritten);

		Assert.Empty(new AiracServiceResult { Messages = [], Elapsed = TimeSpan.Zero, OutputDirectory = "out" }.FilesWritten);
	}

	/// <summary>The line names each sub-service's counts, and a run that made nothing says so.</summary>
	[Fact]
	public void the_line_names_what_each_sub_service_made()
	{
		string line = AiracRunSummary.Describe("2610", EveryPart());

		Assert.StartsWith("Cycle 2610: 1,200 airway(s), 1 excluded, 512 airport(s), 3 airport departure(s), 1 skipped, 2 airport arrival(s), 900 NAVAID(s), ", line, StringComparison.Ordinal);
		Assert.Contains("4 airport(s) with procedure changes, 3 change(s), 50 FAA Chart Recall command(s), 7 telephony command(s), Combined_Alias.txt (15 command(s)), 1 duplicate alias command(s).", line, StringComparison.Ordinal);
		Assert.Equal("Cycle 2610: nothing to produce.", AiracRunSummary.Describe("2610", new AiracServiceResult { Messages = [], Elapsed = TimeSpan.Zero, OutputDirectory = "out" }));
		Assert.Throws<ArgumentNullException>(() => AiracRunSummary.Describe("2610", null!));
	}

	/// <summary>
	/// A run that didn't export the sub-services' alias files names none of them, but still counts their
	/// commands - they are in Combined_Alias.txt - unless it stopped at the duplicate review.
	/// </summary>
	[Fact]
	public void unexported_alias_files_are_not_listed_but_their_commands_are_counted()
	{
		AiracServiceResult everyPart = EveryPart();
		AiracServiceResult unexported = everyPart with
		{
			IndividualAliasFilesExported = false,
			Airways = everyPart.Airways! with { AliasFilePath = null },
			Airports = everyPart.Airports! with { AliasFilePath = null },
			Departures = everyPart.Departures! with { AliasFilePath = null },
			Arrivals = everyPart.Arrivals! with { AliasFilePath = null },
			Navaids = everyPart.Navaids! with { AliasFilePath = null },
			Procedures = everyPart.Procedures! with { AliasFilePath = null },
			Telephony = everyPart.Telephony! with { AliasFilePath = null },
		};

		Assert.DoesNotContain(unexported.FilesWritten, file => file.EndsWith(".txt", StringComparison.Ordinal) && !file.Contains("Combined", StringComparison.Ordinal) && !file.Contains("Duplicate", StringComparison.Ordinal));
		Assert.Contains("50 FAA Chart Recall command(s), 7 telephony command(s), Combined_Alias.txt (15 command(s))", AiracRunSummary.Describe("2610", unexported), StringComparison.Ordinal);
		Assert.Equal("Combined_Alias.txt", unexported.AliasCommandsSavedIn(null, 7));
		Assert.Null(unexported.AliasCommandsSavedIn(null, 0));
		Assert.Equal("Telephony.txt", everyPart.AliasCommandsSavedIn("Telephony.txt", 7));
		Assert.Equal("7 command(s), in Combined_Alias.txt", unexported.DescribeAliasCommands(null, 7, "command(s)"));
		Assert.Equal("Telephony.txt: 7 command(s)", everyPart.DescribeAliasCommands(@"Aliases\Telephony.txt", 7, "command(s)"));

		// Stopped at the duplicate review: no Combined_Alias.txt, so the commands were saved nowhere.
		AiracServiceResult stopped = unexported with { CombinedAlias = null, StoppedAtDuplicateReview = true };
		Assert.DoesNotContain("FAA Chart Recall command(s)", AiracRunSummary.Describe("2610", stopped), StringComparison.Ordinal);
		Assert.DoesNotContain("telephony command(s)", AiracRunSummary.Describe("2610", stopped), StringComparison.Ordinal);
		Assert.Null(stopped.DescribeAliasCommands(null, 7, "command(s)"));
	}
}
