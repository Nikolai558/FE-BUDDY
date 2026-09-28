using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;
using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;
using FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Exercises the <see cref="AiracService"/> orchestrator: it dispatches each sub-service
/// (Airways, Airports, Departures) whose block is present and aggregates its result, writes every
/// sub-service into the one <c>AIRAC_&lt;cycle&gt;</c> folder, handles an earlier run's files as
/// asked, and is a safe no-op (with a warning) when nothing is selected.
/// </summary>
public sealed class AiracServiceTests : IDisposable
{
	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracService_" + Guid.NewGuid().ToString("N"));

	private static AiracCycleInfo Cycle => new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

	public void Dispose()
	{
		if (Directory.Exists(_output))
		{
			Directory.Delete(_output, recursive: true);
		}
	}

	/// <summary>Airports and Departures, alias files only: small, fast, and each writes one file.</summary>
	private AiracServiceSettings AliasOnlySettings(
		bool addFeBuddyOutputFolder = true,
		ExistingOutputAction existingOutput = ExistingOutputAction.Overwrite) => new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			AddFeBuddyOutputFolder = addFeBuddyOutputFolder,
			ExistingOutput = existingOutput,
			Airports = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
			Departures = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

	private string CycleFolder => Path.Combine(_output, "FE-Buddy_Output", "AIRAC_2610");

	[Fact]
	public async Task run_async_no_sub_service_selected_warns_and_does_nothing()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Airways = null,
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.Null(result.Airways);
		Assert.Contains(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task run_async_with_airways_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)],
			awyId: "J1",
			segments:
			[
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J1", 20, "BBBBB", "WP", "CCCCC"),
			]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			// OutputBy=None keeps the run in memory - no files are written by this test.
			Airways = new Dictionary<string, string>
			{
				{ "OutputBy", "None" },
				{ "GenerateAliasFile", "N" },
			},
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.Airways);
		Assert.Equal(1, result.Airways!.AirwayCount);
		Assert.Empty(result.Airways.GeojsonFilesWritten);
		Assert.Empty(result.Airways.ExcludedAirwayIds);
		Assert.False(Directory.Exists(result.OutputDirectory));
	}

	[Fact]
	public async Task run_async_with_airports_and_departures_blocks_runs_both_and_reports_progress()
	{
		NasrCsvDataCollection data = DepartureTestData.Dotss();
		AiracServiceSettings settings = AliasOnlySettings();

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(settings, data, new SynchronousProgress(reports.Add));

		Assert.Null(result.Airways);
		Assert.Equal(1, result.Airports!.AirportCount);
		Assert.Equal(1, result.Departures!.AirportProcedureCount);
		// Both sub-services wrote an alias file, so one more "AIRAC" report follows for the
		// duplicate-alias check once every sub-service has run.
		Assert.Equal(
			["Airports", "Airports", "Departures", "Departures", "AIRAC"],
			reports.Select(r => r.SubService));
		Assert.Equal(100, reports[^2].PercentComplete);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => AiracService.RunAsync(settings, data, cancellationToken: new CancellationToken(canceled: true)));
	}

	[Fact]
	public async Task every_sub_service_writes_into_the_one_cycle_folder()
	{
		AiracServiceResult result = await AiracService.RunAsync(AliasOnlySettings(), DepartureTestData.Dotss());

		Assert.Equal(CycleFolder, result.OutputDirectory);
		Assert.Equal(Path.Combine(CycleFolder, "Aliases", "Airports.txt"), result.Airports!.AliasFilePath);
		Assert.Equal(Path.Combine(CycleFolder, "Aliases", "Departures.txt"), result.Departures!.AliasFilePath);

		// The duplicate-alias report is part of the one cycle folder too.
		Assert.NotNull(result.DuplicateAliasReport);
		Assert.Equal(Path.Combine(CycleFolder, AiracOutputPaths.DuplicateAliasReportFileName), result.DuplicateAliasReport!.FilePath);
		Assert.True(File.Exists(result.DuplicateAliasReport.FilePath));
	}

	[Fact]
	public async Task a_dp_and_a_star_sharing_a_command_produce_the_duplicate_report_and_an_advisory_warning()
	{
		// ORF genuinely publishes both a NUTIY departure and a NUTIY arrival (see
		// ArrivalAliasWriter.CommandName's remarks), so both alias files write ".orfNUTIYf" -
		// exactly the case the duplicate-alias check exists to catch.
		NasrCsvDataCollection data = DepartureTestData.Build(
			bases: [DepartureTestData.Base("NUTIY", "ZDC", "NUTIY1.NUTIY", amendmentNo: "ONE", servedArpt: "ORF")],
			apts: [DepartureTestData.Apt("NUTIY", "ZDC", "NUTIY1.NUTIY", "BODY", "ORF")],
			routes: DepartureTestData.Body("NUTIY", "ZDC", "NUTIY1.NUTIY", "BODY", ["AAAAA", "BBBBB"]),
			fixes: [("AAAAA", 36.9, -76.2), ("BBBBB", 37.0, -76.3)]);

		NasrCsvDataCollection arrivalData = ArrivalTestData.Build(
			bases: [ArrivalTestData.Base("NUTIY", "ZDC", "TRANS.NUTIY1", amendmentNo: "ONE", servedArpt: "ORF")],
			apts: [ArrivalTestData.Apt("ZDC", "TRANS.NUTIY1", "BODY", "ORF")],
			routes: ArrivalTestData.Body("ZDC", "TRANS.NUTIY1", "BODY", ["CCCCC", "DDDDD"]),
			fixes: [("CCCCC", 36.95, -76.25), ("DDDDD", 37.05, -76.35)]);

		data.Star = arrivalData.Star;
		data.Fix!.FixBase.AddRange(arrivalData.Fix!.FixBase);
		data.Apt!.AptBase.Add(new AptCsvDataModel.AptBase
		{
			ArptId = "ORF",
			IcaoId = "KORF",
			BaseLatDecimal = 36.8946,
			BaseLongDecimal = -76.2012,
			RespArtccId = "ZDC",
		});

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Departures = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
			Arrivals = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.DuplicateAliasReport);
		Assert.NotEmpty(result.DuplicateAliasReport!.Duplicates);
		Assert.Contains(result.DuplicateAliasReport.Duplicates, d => d.Command.Equals(".orfNUTIYf", StringComparison.OrdinalIgnoreCase));
		Assert.True(File.Exists(result.DuplicateAliasReport.FilePath));
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("alias command(s) are used by more than one line", StringComparison.Ordinal));
	}

	[Fact]
	public async Task run_async_with_an_arrivals_block_runs_the_pipeline_and_aggregates()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Arrivals = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		Assert.Null(result.Departures);
		Assert.NotNull(result.Arrivals);
		Assert.Equal(1, result.Arrivals!.AirportProcedureCount);
		Assert.Equal(Path.Combine(CycleFolder, "Aliases", "Arrivals.txt"), result.Arrivals.AliasFilePath);

		// One alias file, no duplicates: the report still exists and says so.
		Assert.NotNull(result.DuplicateAliasReport);
		Assert.Empty(result.DuplicateAliasReport!.Duplicates);
		Assert.Contains(result.Messages, m => m.Text.Contains("No duplicate alias commands", StringComparison.Ordinal));
	}

	[Fact]
	public async Task an_arrivals_block_writes_star_named_geojson_into_the_cycle_folder()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Arrivals = new Dictionary<string, string> { { "GenerateAliasFile", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		Assert.Equal(1, result.Arrivals!.AirportProcedureCount);
		string linesFile = Path.Combine(CycleFolder, "Geojson", "ZLA", "LAS", "LAS_BLAID_STAR_Lines.geojson");
		Assert.Contains(linesFile, result.Arrivals.GeojsonFilesWritten);
		Assert.True(File.Exists(linesFile));
	}

	[Fact]
	public async Task a_run_with_only_arrivals_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Arrivals = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		// DeleteExisting only runs when something is selected: Arrivals alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Aliases", "Arrivals.txt")));
	}

	[Fact]
	public async Task a_null_arrivals_block_leaves_result_arrivals_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Arrivals = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Arrivals);
	}

	[Fact]
	public async Task run_async_with_a_navaids_block_runs_the_pipeline_and_aggregates()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Navaids = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		Assert.Null(result.Arrivals);
		Assert.NotNull(result.Navaids);
		Assert.Equal(1, result.Navaids!.NavaidCount);
		Assert.Equal(Path.Combine(CycleFolder, "Aliases", "Navaids.txt"), result.Navaids.AliasFilePath);

		// One alias file, no duplicates: the report still exists and says so.
		Assert.NotNull(result.DuplicateAliasReport);
		Assert.Empty(result.DuplicateAliasReport!.Duplicates);
		Assert.Contains(result.Messages, m => m.Text.Contains("No duplicate alias commands", StringComparison.Ordinal));
	}

	[Fact]
	public async Task a_navaids_block_writes_geojson_into_the_cycle_folder()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Navaids = new Dictionary<string, string> { { "GenerateAliasFile", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "NAVAIDs_Symbols.geojson");
		Assert.Contains(symbolsFile, result.Navaids!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_navaids_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Navaids = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		// DeleteExisting only runs when something is selected: NAVAIDs alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Aliases", "Navaids.txt")));
	}

	[Fact]
	public async Task a_null_navaids_block_leaves_result_navaids_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Navaids = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Navaids);
	}

	[Fact]
	public async Task run_async_with_an_artcc_boundaries_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[.. ArtccBoundaryTestData.ZobHighRows(), .. ArtccBoundaryTestData.ZobLowRows()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ArtccBoundaries = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.ArtccBoundaries);
		Assert.Equal(1, result.ArtccBoundaries!.LocationCount);
		Assert.Equal(2, result.ArtccBoundaries.RingCount);

		string highFile = Path.Combine(CycleFolder, "Geojson", "ARTCC-Boundary_High_Lines.geojson");
		Assert.Contains(highFile, result.ArtccBoundaries.GeojsonFilesWritten);
		Assert.True(File.Exists(highFile));
	}

	[Fact]
	public async Task a_run_with_only_artcc_boundaries_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			ArtccBoundaries = new Dictionary<string, string>(),
		};

		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			ArtccBoundaryTestData.ZobHighRows());

		await AiracService.RunAsync(settings, data);

		// DeleteExisting only runs when something is selected: ARTCC Boundaries alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "ARTCC-Boundary_High_Lines.geojson")));
	}

	[Fact]
	public async Task a_null_artcc_boundaries_block_leaves_result_artcc_boundaries_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { ArtccBoundaries = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.ArtccBoundaries);
	}

	[Fact]
	public async Task run_async_with_a_fixes_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Fixes = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.Null(result.ArtccBoundaries);
		Assert.NotNull(result.Fixes);
		Assert.Equal(1, result.Fixes!.FixCount);
	}

	[Fact]
	public async Task a_fixes_block_writes_geojson_into_the_cycle_folder()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Fixes = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "Fix_Symbols.geojson");
		Assert.Contains(symbolsFile, result.Fixes!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_fixes_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Fixes = new Dictionary<string, string>(),
		};

		await AiracService.RunAsync(settings, FixTestData.Build([FixTestData.AcmeRow()]));

		// DeleteExisting only runs when something is selected: Fixes alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "Fix_Symbols.geojson")));
	}

	[Fact]
	public async Task a_null_fixes_block_leaves_result_fixes_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Fixes = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Fixes);
	}

	[Fact]
	public async Task run_async_with_a_wx_stations_block_runs_the_pipeline_and_aggregates()
	{
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		Assert.Null(result.Fixes);
		Assert.NotNull(result.WxStations);
		Assert.Equal(1, result.WxStations!.StationCount);
	}

	[Fact]
	public async Task a_wx_stations_block_writes_geojson_into_the_cycle_folder()
	{
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "Wx_Symbols.geojson");
		Assert.Contains(symbolsFile, result.WxStations!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_wx_stations_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			WxStations = new Dictionary<string, string>(),
		};

		await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		// DeleteExisting only runs when something is selected: Wx Stations alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "Wx_Symbols.geojson")));
	}

	[Fact]
	public async Task a_null_wx_stations_block_leaves_result_wx_stations_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { WxStations = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss(), wxStationData: null);

		Assert.Null(result.WxStations);
	}

	[Fact]
	public async Task the_two_argument_overload_with_a_wx_stations_block_completes_with_the_wx_warning_and_zero_stations()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		// The two-argument RunAsync(settings, nasrData) overload passes no Wx station data at all
		// (WxStationService.Run receives a null WxStationDataCollection). That no longer throws: the
		// run completes with zero stations, nothing written, and a warning saying there was nothing
		// to build from.
		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.NotNull(result.WxStations);
		Assert.Equal(0, result.WxStations!.StationCount);
		Assert.Equal(0, result.WxStations.TotalStationCount);
		Assert.Empty(result.WxStations.GeojsonFilesWritten);
		Assert.Contains(result.WxStations.Messages, m =>
			m.Text.Contains("no weather station data to build from", StringComparison.Ordinal));
	}

	// ---- the AiracSupplementalData overload ----

	[Fact]
	public async Task the_supplemental_data_overload_is_the_real_implementation_the_others_delegate_to()
	{
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { WxStations = wxData });

		Assert.NotNull(result.WxStations);
		Assert.Equal(1, result.WxStations!.StationCount);
	}

	[Fact]
	public async Task the_wx_station_data_overload_delegates_to_the_supplemental_data_overload()
	{
		// Both overloads must reach WxStationService with the same data - the three-argument
		// overload just wraps it in an AiracSupplementalData with everything else left null.
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult viaWxOverload = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);
		AiracServiceResult viaSupplementalOverload = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { WxStations = wxData });

		Assert.Equal(viaSupplementalOverload.WxStations!.StationCount, viaWxOverload.WxStations!.StationCount);
	}

	[Fact]
	public async Task run_async_rejects_a_null_supplemental_data()
	{
		AiracServiceSettings settings = new() { SelectedCycle = Cycle, OutputDirectory = _output };

		await Assert.ThrowsAsync<ArgumentNullException>(
			() => AiracService.RunAsync(settings, new NasrCsvDataCollection(), (AiracSupplementalData)null!));
	}

	[Fact]
	public async Task supplemental_messages_come_first_in_the_results_messages()
	{
		ServiceMessage first = new(LogLevel.Info, "Test", "first supplemental message");
		ServiceMessage second = new(LogLevel.Warning, "Test", "second supplemental message");
		AiracServiceSettings settings = new() { SelectedCycle = Cycle, OutputDirectory = _output };

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Messages = [first, second] });

		Assert.Equal(first, result.Messages[0]);
		Assert.Equal(second, result.Messages[1]);
	}

	// ---- Telephony ----

	[Fact]
	public async Task a_telephony_block_with_parsed_data_writes_the_alias_file_and_sets_result_counts()
	{
		TelephonyDataCollection telephonyData = new()
		{
			Assignments =
			[
				new TelephonyHtmlDataModel.Assignment { Company = "AVIANCA", Country = "COLOMBIA", Telephony = "AVIANCA", ThreeLetterDesignator = "AVA" },
				new TelephonyHtmlDataModel.Assignment { Company = "NO DESIGNATOR AIRLINE", Country = "USA", Telephony = "SOMETHING", ThreeLetterDesignator = "..." },
				new TelephonyHtmlDataModel.Assignment { Company = "NO TELEPHONY AIRLINE", Country = "USA", Telephony = "", ThreeLetterDesignator = "XYZ" },
			],
			SpecialCallSigns =
			[
				new TelephonyHtmlDataModel.SpecialCallSign { Telephony = "AIR SIX", Identifier = "ARSIX", Agency = "Some Agency", ExpirationDate = "N/A" },
			],
		};

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = telephonyData });

		string aliasFile = Path.Combine(CycleFolder, "Aliases", "Telephony.txt");
		Assert.True(File.Exists(aliasFile));
		Assert.NotNull(result.Telephony);
		Assert.Equal(aliasFile, result.Telephony!.AliasFilePath);
		Assert.Equal(1, result.Telephony.IcaoAssignmentCount);
		Assert.Equal(1, result.Telephony.SpecialCallSignCount);
		Assert.Equal(1, result.Telephony.NoDesignatorCount);
		Assert.Equal(1, result.Telephony.NoTelephonyCount);

		Assert.NotNull(result.DuplicateAliasReport);
		Assert.Contains("Telephony.txt", File.ReadAllText(result.DuplicateAliasReport!.FilePath));
	}

	[Fact]
	public async Task a_telephony_block_with_no_data_completes_with_its_warning_and_writes_no_file()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.NotNull(result.Telephony);
		Assert.Null(result.Telephony!.AliasFilePath);
		Assert.False(File.Exists(Path.Combine(CycleFolder, "Aliases", "Telephony.txt")));
		Assert.Contains(result.Telephony.Messages, m =>
			m.Text.Contains("no telephony data to build from", StringComparison.Ordinal));
	}

	// ---- vNAS_Alias.txt ----

	private static TelephonyDataCollection OneOperator => new()
	{
		Assignments =
		[
			new TelephonyHtmlDataModel.Assignment { Company = "AVIANCA", Country = "COLOMBIA", Telephony = "AVIANCA", ThreeLetterDesignator = "AVA" },
		],
	};

	private string VnasAliasFile => Path.Combine(CycleFolder, "Upload_to_vNAS", "vNAS_Alias.txt");

	[Fact]
	public async Task an_alias_file_marked_for_vnas_stays_in_aliases_and_is_copied_into_vnas_alias_txt()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string> { ["uploadtovnas"] = "telephony.TXT" },
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });

		string telephony = Path.Combine(CycleFolder, "Aliases", "Telephony.txt");
		Assert.Equal(telephony, result.Telephony!.AliasFilePath);

		Assert.NotNull(result.VnasAlias);
		Assert.Equal(VnasAliasFile, result.VnasAlias!.FilePath);
		Assert.Equal(["Telephony.txt"], result.VnasAlias.FeBuddyFiles);
		Assert.Equal(0, result.VnasAlias.CustomFileCount);

		string written = File.ReadAllText(VnasAliasFile);
		Assert.StartsWith("; ===== FE-Buddy aliases (AIRAC 2610) start here.", written, StringComparison.Ordinal);
		Assert.Contains(File.ReadAllText(telephony).TrimEnd(), written, StringComparison.Ordinal);

		// Without vNAS Alias Upload the file has no facility aliases, and the Review tab says what uploading it would do.
		ServiceMessage onlyFeBuddy = Assert.Single(result.Messages, m => m.Text.StartsWith("vNAS Alias Upload is not selected", StringComparison.Ordinal));
		Assert.True(onlyFeBuddy.IsAdvisory);
		Assert.Equal(LogLevel.Warning, onlyFeBuddy.Level);
		Assert.Contains("uploading it would remove your facility's own aliases from vNAS", onlyFeBuddy.Text, StringComparison.Ordinal);
	}

	/// <summary>
	/// A renamed alias file is still the one UploadToVnas names by its key, so it goes into the vNAS
	/// alias file - and that file, the duplicate report and every message use the new names.
	/// </summary>
	[Fact]
	public async Task renamed_files_are_written_under_their_new_names_and_a_renamed_alias_file_still_goes_to_vnas()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string> { ["UploadToVnas"] = "Telephony.txt" },
			FileNames = new Dictionary<string, string>
			{
				["Telephony.txt"] = "ZOB Telephony",
				["vNAS_Alias.txt"] = "ZOB vNAS",
				["Duplicate_Alias_Commands.txt"] = "ZOB Duplicates",
			},
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });

		Assert.Equal(Path.Combine(CycleFolder, "Aliases", "ZOB Telephony.txt"), result.Telephony!.AliasFilePath);
		Assert.Equal(Path.Combine(CycleFolder, "Upload_to_vNAS", "ZOB vNAS.txt"), result.VnasAlias!.FilePath);
		Assert.Equal(["ZOB Telephony.txt"], result.VnasAlias.FeBuddyFiles);
		Assert.Equal(Path.Combine(CycleFolder, "ZOB Duplicates.txt"), result.DuplicateAliasReport!.FilePath);

		Assert.False(File.Exists(Path.Combine(CycleFolder, "Aliases", "Telephony.txt")));
		Assert.False(File.Exists(VnasAliasFile));
		Assert.False(File.Exists(Path.Combine(CycleFolder, "Duplicate_Alias_Commands.txt")));

		Assert.Contains("; ----- ZOB Telephony.txt -----", File.ReadAllText(result.VnasAlias.FilePath!), StringComparison.Ordinal);
		Assert.Contains("Files checked: ZOB Telephony.txt", File.ReadAllText(result.DuplicateAliasReport.FilePath), StringComparison.Ordinal);
		Assert.Contains(result.Messages, m => m.Text.StartsWith("vNAS Alias Upload is not selected, so ZOB vNAS.txt holds only", StringComparison.Ordinal));
	}

	[Fact]
	public async Task a_renamed_vnas_alias_file_an_earlier_run_left_is_deleted_under_its_new_name()
	{
		string renamed = Path.Combine(CycleFolder, "Upload_to_vNAS", "ZOB vNAS.txt");
		Directory.CreateDirectory(Path.GetDirectoryName(renamed)!);
		File.WriteAllText(renamed, ".old last run's aliases");

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
			FileNames = new Dictionary<string, string> { ["vNAS_Alias.txt"] = "ZOB vNAS" },
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });

		Assert.False(File.Exists(renamed));
		Assert.Contains(result.Messages, m => m.Text.StartsWith("No alias file is marked for vNAS, so ZOB vNAS.txt was not written.", StringComparison.Ordinal));
	}

	[Fact]
	public async Task a_new_name_for_no_file_is_a_warning_on_the_run()
	{
		AiracServiceSettings settings = AliasOnlySettings() with
		{
			FileNames = new Dictionary<string, string> { ["Departures_Lines"] = "Mine" },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		ServiceMessage warning = Assert.Single(result.Messages, m => m.Source == "OutputFileNamesParser");
		Assert.Equal(LogLevel.Warning, warning.Level);
	}

	/// <summary>The names are checked before an earlier run's files are deleted, so a bad one costs nothing.</summary>
	[Fact]
	public async Task a_new_name_that_cannot_be_used_stops_the_run_before_anything_is_deleted()
	{
		string earlier = Path.Combine(CycleFolder, "earlier.txt");
		Directory.CreateDirectory(CycleFolder);
		File.WriteAllText(earlier, "an earlier run's file");

		AiracServiceSettings settings = AliasOnlySettings(existingOutput: ExistingOutputAction.DeleteExisting) with
		{
			FileNames = new Dictionary<string, string> { ["Airports.txt"] = "Airports.txt" },
		};

		await Assert.ThrowsAsync<ArgumentException>(() => AiracService.RunAsync(settings, DepartureTestData.Dotss()));

		Assert.True(File.Exists(earlier));
	}

	[Fact]
	public async Task no_alias_file_marked_for_vnas_and_no_vnas_alias_block_writes_no_vnas_alias_txt()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });

		Assert.Null(result.VnasAlias);
		Assert.False(Directory.Exists(Path.Combine(CycleFolder, "Upload_to_vNAS")));
	}

	[Fact]
	public async Task rewriting_alias_files_without_marking_any_for_vnas_deletes_an_earlier_vnas_alias_txt()
	{
		// An earlier run of this cycle marked Telephony for vNAS; this one does not.
		Directory.CreateDirectory(Path.GetDirectoryName(VnasAliasFile)!);
		File.WriteAllText(VnasAliasFile, ".old last run's aliases");

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });

		Assert.Null(result.VnasAlias);
		Assert.False(File.Exists(VnasAliasFile));

		ServiceMessage deleted = Assert.Single(result.Messages, m => m.Text.StartsWith("No alias file is marked for vNAS", StringComparison.Ordinal));
		Assert.True(deleted.IsAdvisory);
		Assert.Equal(LogLevel.Info, deleted.Level);
		Assert.Contains("The one an earlier run wrote was deleted", deleted.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_run_that_writes_no_alias_file_leaves_an_earlier_vnas_alias_txt_alone()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(VnasAliasFile)!);
		File.WriteAllText(VnasAliasFile, ".old last run's aliases");

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Fixes = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, FixTestData.Build([FixTestData.AcmeRow()]));

		Assert.Null(result.VnasAlias);
		Assert.Equal(".old last run's aliases", File.ReadAllText(VnasAliasFile));
		Assert.DoesNotContain(result.Messages, m => m.Text.StartsWith("No alias file is marked for vNAS", StringComparison.Ordinal));
	}

	[Fact]
	public async Task an_earlier_vnas_alias_txt_that_cannot_be_deleted_is_a_warning()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(VnasAliasFile)!);
		File.WriteAllText(VnasAliasFile, ".old last run's aliases");

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string>(),
		};

		AiracServiceResult result;

		// Held open without delete sharing, as another program might, so it cannot be deleted.
		using (new FileStream(VnasAliasFile, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			result = await AiracService.RunAsync(
				settings, new NasrCsvDataCollection(), new AiracSupplementalData { Telephony = OneOperator });
		}

		Assert.True(File.Exists(VnasAliasFile));

		ServiceMessage notDeleted = Assert.Single(result.Messages, m => m.Text.StartsWith("No alias file is marked for vNAS", StringComparison.Ordinal));
		Assert.Equal(LogLevel.Warning, notDeleted.Level);
		Assert.Contains("could not be deleted", notDeleted.Text, StringComparison.Ordinal);
		Assert.Contains("do not upload it", notDeleted.Text, StringComparison.Ordinal);
	}

	[Fact]
	public async Task the_vnas_alias_block_puts_the_custom_files_first_and_reports_its_progress()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Telephony = new Dictionary<string, string> { ["UploadToVnas"] = "Telephony.txt" },
			VnasAlias = new Dictionary<string, string> { ["Sources.1.FilePath"] = @"C:\ZOB-Alias.txt" },
		};

		AiracSupplementalData supplemental = new()
		{
			Telephony = OneOperator,
			CustomAliasFiles =
			[
				AliasSourceLoad.Read(new AliasSource(1, AliasSourceKind.File, @"C:\ZOB-Alias.txt"), ".FeUseOnly first\r\n.dtwdv .ECHO DTW\r\n"),
				AliasSourceLoad.Failed(new AliasSource(2, AliasSourceKind.Url, "https://github.com/o/r/blob/main/Extra.txt"), "GitHub could not find it."),
			],
		};

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), supplemental, new SynchronousProgress(reports.Add));

		Assert.Equal(2, result.VnasAlias!.CustomFileCount);
		Assert.Equal(1, result.VnasAlias.CustomFilesMerged);
		Assert.StartsWith(".FeUseOnly first" + Environment.NewLine + ".dtwdv .ECHO DTW" + Environment.NewLine, File.ReadAllText(VnasAliasFile), StringComparison.Ordinal);

		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.StartsWith("Left custom alias file 2 (Extra.txt) out", StringComparison.Ordinal));
		Assert.DoesNotContain(result.Messages, m => m.Text.StartsWith("vNAS Alias Upload is not selected", StringComparison.Ordinal));

		AiracServiceProgress done = reports.Last(p => p.SubService == "vNAS Alias Upload");
		Assert.Equal(100, done.PercentComplete);
		Assert.StartsWith("vNAS_Alias.txt: 1 custom command(s), then ", done.Message, StringComparison.Ordinal);
		Assert.EndsWith(" from 1 FE-Buddy alias file(s).", done.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task a_vnas_alias_block_alone_counts_as_something_selected()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			VnasAlias = new Dictionary<string, string>(),
		};

		List<AiracServiceProgress> reports = [];
		AiracServiceResult result = await AiracService.RunAsync(
			settings, new NasrCsvDataCollection(), new AiracSupplementalData(), new SynchronousProgress(reports.Add));

		Assert.DoesNotContain(result.Messages, m => m.Text.Contains("no sub-service selected", StringComparison.Ordinal));
		Assert.Null(result.VnasAlias!.FilePath);
		Assert.Contains("vNAS_Alias.txt not written.", reports.Last().Message, StringComparison.Ordinal);
	}

	// ---- Procedures ----

	[Fact]
	public async Task a_run_with_only_procedures_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
		};

		// No Dtpp is supplied, so the Procedures sub-service completes with its advisory (see
		// below) and writes nothing - but DeleteExisting must still fire beforehand.
		await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.False(File.Exists(stale));
	}

	[Fact]
	public async Task a_procedures_block_alone_does_not_run_any_other_sub_service()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.Null(result.Airways);
		Assert.Null(result.Airports);
		Assert.Null(result.Departures);
		Assert.Null(result.Arrivals);
		Assert.Null(result.Navaids);
		Assert.Null(result.ArtccBoundaries);
		Assert.Null(result.Fixes);
		Assert.Null(result.WxStations);
		Assert.DoesNotContain(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task a_procedures_block_with_no_metafile_supplied_still_completes_with_the_advisory()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
		};

		// The two-argument overload supplies no supplemental data at all, so Dtpp is null: the
		// Procedures sub-service must complete with its advisory rather than throwing, even
		// though the NasrCsvDataCollection below has no Apt/ClsArsp parsed.
		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.NotNull(result.Procedures);
		Assert.Empty(result.Procedures!.FilesWritten);
		Assert.Contains(result.Procedures.Messages, m => m.IsAdvisory);
	}

	[Fact]
	public async Task run_async_with_a_procedures_block_and_a_metafile_runs_the_pipeline_and_fills_the_result()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2610",
			airports: [ProcedureTestData.AirportRow("AAA", alnum: 100)],
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "00100ILS1.PDF", userAction: "A")]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, nasr, new AiracSupplementalData { Dtpp = dtpp });

		Assert.NotNull(result.Procedures);
		Assert.Equal(1, result.Procedures!.AirportCount);
		Assert.Equal(1, result.Procedures.NewCount);
		Assert.Equal(
			Path.Combine(CycleFolder, "Publication_Docs", "Procedure_Changes.md"),
			result.Procedures.FilesWritten.Single(f => f.EndsWith(".md", StringComparison.Ordinal)));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Publication_Docs", "Procedures.json")));
	}

	[Fact]
	public async Task the_previous_cycle_metafile_flows_through_to_a_deleted_procedures_previous_chart_link()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.Nasr([ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2610",
			airports: [ProcedureTestData.AirportRow("AAA", alnum: 100)],
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "DELETED_JOB.PDF", userAction: "D")]);
		DtppMetafileDataCollection previousDtpp = ProcedureTestData.Dtpp("2609",
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "00100ILS1.PDF")]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB" },
		};

		AiracServiceResult result = await AiracService.RunAsync(
			settings, nasr, new AiracSupplementalData { Dtpp = dtpp, PreviousDtpp = previousDtpp });

		Assert.DoesNotContain(result.Procedures!.Messages, m =>
			m.Text.Contains("previous cycle's d-TPP Metafile is not available", StringComparison.Ordinal));

		string markdown = File.ReadAllText(Path.Combine(CycleFolder, "Publication_Docs", "Procedure_Changes.md"));
		Assert.Contains("https://aeronav.faa.gov/d-tpp/2609/00100ILS1.PDF", markdown);
	}

	[Fact]
	public async Task without_the_fe_buddy_output_folder_the_cycle_folder_sits_in_the_output_directory()
	{
		AiracServiceResult result = await AiracService.RunAsync(
			AliasOnlySettings(addFeBuddyOutputFolder: false), DepartureTestData.Dotss());

		Assert.Equal(Path.Combine(_output, "AIRAC_2610"), result.OutputDirectory);
		Assert.True(File.Exists(Path.Combine(_output, "AIRAC_2610", "Aliases", "Airports.txt")));
	}

	[Fact]
	public async Task has_existing_output_is_true_only_once_the_cycle_folder_holds_something()
	{
		AiracServiceSettings settings = AliasOnlySettings();

		Assert.False(AiracService.HasExistingOutput(settings));

		Directory.CreateDirectory(CycleFolder);
		Assert.False(AiracService.HasExistingOutput(settings));

		await AiracService.RunAsync(settings, DepartureTestData.Dotss());
		Assert.True(AiracService.HasExistingOutput(settings));
	}

	[Fact]
	public async Task overwrite_keeps_an_earlier_runs_other_files()
	{
		string stale = WriteStaleFile();

		await AiracService.RunAsync(AliasOnlySettings(), DepartureTestData.Dotss());

		Assert.True(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Aliases", "Airports.txt")));
	}

	[Fact]
	public async Task delete_existing_empties_the_cycle_folder_before_writing()
	{
		string stale = WriteStaleFile();

		await AiracService.RunAsync(
			AliasOnlySettings(existingOutput: ExistingOutputAction.DeleteExisting), DepartureTestData.Dotss());

		Assert.False(File.Exists(stale));
		Assert.False(Directory.Exists(Path.GetDirectoryName(stale)));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Aliases", "Airports.txt")));
	}

	[Fact]
	public async Task delete_existing_leaves_the_folder_alone_when_nothing_is_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = AliasOnlySettings(existingOutput: ExistingOutputAction.DeleteExisting) with
		{
			Airports = null,
			Departures = null,
		};

		await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.True(File.Exists(stale));
	}

	private sealed class SynchronousProgress(Action<AiracServiceProgress> report) : IProgress<AiracServiceProgress>
	{
		public void Report(AiracServiceProgress value) => report(value);
	}

	[Fact]
	public async Task run_async_rejects_null_arguments_and_a_blank_output_directory()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync((AiracServiceSettings)null!, new NasrCsvDataCollection()));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync(settings, (NasrCsvDataCollection)null!));
		await Assert.ThrowsAsync<ArgumentException>(() => AiracService.RunAsync(settings with { OutputDirectory = " " }, new NasrCsvDataCollection()));
		Assert.Throws<ArgumentNullException>(() => AiracService.HasExistingOutput(null!));
	}

	/// <summary>Leaves a file from "an earlier run" in the cycle folder, in a sub-folder, and returns its path.</summary>
	private string WriteStaleFile()
	{
		string directory = Path.Combine(CycleFolder, "Geojson");
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, "Airways_High_Lines.geojson");
		File.WriteAllText(path, "{}");
		return path;
	}
}
