using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.Airac.Departures;
using FeBuddy.Core.Services.General;

using FeBuddy.UnitTests.Services.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Departures;

public class DepartureFilterTests
{
	private static readonly DateOnly CycleDate = new(2026, 9, 3);

	private static readonly RegionOfInterest SoCal = new(33.0, -119.0, 35.0, -117.0);

	private static DepartureSettings Settings() => new()
	{
		OutputDirectory = @"C:\Output",
		GenerateGeojson = true,
		GenerateAliasFile = true,
		IncludeFebCustomProperties = false,
		IncludeCrcEramPropertyDefaults = false
	};

	private static DepartureProcedure Amended(string codeId, DateOnly amended) =>
		DepartureTestData.Procedure(codeId: codeId, amendmentEffectiveDate: amended, cycleEffectiveDate: CycleDate);

	[Fact]
	public void obstacle_departures_are_dropped_when_excluded()
	{
		DepartureProcedure sid = DepartureTestData.Procedure(codeId: "SID");
		DepartureProcedure odp = DepartureTestData.Procedure(codeId: "ODP", isObstacleDeparture: true);
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { sid, odp }, Settings() with { IncludeObstacleDepartures = false }, messages);

		Assert.Equal(new[] { "SID" }, kept.Select(p => p.CodeId));
	}

	[Fact]
	public void obstacle_departures_are_kept_when_included()
	{
		DepartureProcedure sid = DepartureTestData.Procedure(codeId: "SID");
		DepartureProcedure odp = DepartureTestData.Procedure(codeId: "ODP", isObstacleDeparture: true);
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { sid, odp }, Settings() with { IncludeObstacleDepartures = true }, messages);

		Assert.Equal(new[] { "SID", "ODP" }, kept.Select(p => p.CodeId));
	}

	[Fact]
	public void the_artcc_filter_keeps_only_the_listed_artccs()
	{
		DepartureProcedure zla = DepartureTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		DepartureProcedure zoa = DepartureTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		DepartureProcedure zse = DepartureTestData.Procedure(codeId: "THR", artcc: "ZSE");
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { zla, zoa, zse }, Settings() with { ArtccFilter = new[] { "ZLA", "ZSE" } }, messages);

		Assert.Equal(new[] { "ZLA", "ZSE" }, kept.Select(p => p.Artcc));
	}

	[Fact]
	public void an_empty_artcc_filter_keeps_every_artcc()
	{
		DepartureProcedure zla = DepartureTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		DepartureProcedure zoa = DepartureTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(new[] { zla, zoa }, Settings(), messages);

		Assert.Equal(2, kept.Count);
	}

	[Fact]
	public void amended_within_one_cycle_keeps_only_this_cycles_amendments()
	{
		DepartureProcedure thisCycle = Amended("NEW", new DateOnly(2026, 9, 3));
		DepartureProcedure dayBefore = Amended("OLD", new DateOnly(2026, 9, 2));
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { thisCycle, dayBefore }, Settings() with { AmendedWithinCycles = 1 }, messages);

		Assert.Equal(new[] { "NEW" }, kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_four_cycles_keeps_the_cutoff_day_and_drops_the_day_before()
	{
		// 2026-09-03 minus 3 x 28 = 84 days is 2026-06-11.
		DateOnly cutoff = new(2026, 6, 11);
		Assert.Equal(cutoff, CycleDate.AddDays(-84));

		DepartureProcedure onCutoff = Amended("KEEP", cutoff);
		DepartureProcedure beforeCutoff = Amended("DROP", cutoff.AddDays(-1));
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { onCutoff, beforeCutoff }, Settings() with { AmendedWithinCycles = 4 }, messages);

		Assert.Equal(new[] { "KEEP" }, kept.Select(p => p.CodeId));
	}

	[Fact]
	public void an_unparseable_amendment_date_is_dropped_with_a_warning()
	{
		DepartureProcedure unreadable = DepartureTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			new[] { unreadable }, Settings() with { AmendedWithinCycles = 1 }, messages);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("NOT A DATE", message.Text);
	}

	[Fact]
	public void amendment_dates_are_ignored_when_the_filter_is_off()
	{
		DepartureProcedure unreadable = DepartureTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(new[] { unreadable }, Settings(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}

	[Fact]
	public void waypoint_mode_keeps_a_pair_with_any_point_inside_the_roi()
	{
		DepartureAirportProcedure partlyInside = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "IN"), "AAA",
			new DeparturePoint("FARAW", "WP", 45.0, -100.0),
			new DeparturePoint("NEARB", "WP", 34.0, -118.0));
		DepartureAirportProcedure outside = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "OUT"), "AAA",
			new DeparturePoint("FARAW", "WP", 45.0, -100.0),
			new DeparturePoint("FARBB", "WP", 46.0, -101.0));
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			new[] { partlyInside, outside },
			Settings() with { Roi = SoCal, RoiMode = DepartureRoiMode.Waypoint },
			DepartureTestData.Build(),
			messages);

		Assert.Equal(new[] { "IN" }, kept.Select(p => p.Procedure.CodeId));
	}

	[Fact]
	public void airport_mode_keeps_by_the_airport_reference_point()
	{
		// Points are deliberately placed opposite to their airport so only the airport decides.
		DepartureAirportProcedure atLax = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "LAXDP"), "LAX",
			new DeparturePoint("FARAW", "WP", 45.0, -100.0));
		DepartureAirportProcedure atFar = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "FARDP"), "FAR",
			new DeparturePoint("NEARB", "WP", 34.0, -118.0));

		NasrCsvDataCollection data = DepartureTestData.Build(airports: new[]
		{
			DepartureTestData.Airport(DepartureTestData.LaxId, DepartureTestData.LaxLatitude, DepartureTestData.LaxLongitude, "KLAX"),
			DepartureTestData.Airport("FAR", 46.92, -96.81, "KFAR"),
		});
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			new[] { atLax, atFar },
			Settings() with { Roi = SoCal, RoiMode = DepartureRoiMode.Airport },
			data,
			messages);

		Assert.Equal(new[] { "LAXDP" }, kept.Select(p => p.Procedure.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void airport_mode_drops_an_airport_with_no_apt_base_row_with_an_info_message()
	{
		DepartureAirportProcedure atUnknown = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "CYQG"), "CYQG",
			new DeparturePoint("NEARB", "WP", 34.0, -118.0));
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			new[] { atUnknown },
			Settings() with { Roi = SoCal, RoiMode = DepartureRoiMode.Airport },
			DepartureTestData.Build(),
			messages);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("CYQG", message.Text);
	}

	[Fact]
	public void no_roi_keeps_everything()
	{
		DepartureAirportProcedure anywhere = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "ANY"), "NOAPT",
			new DeparturePoint("FARAW", "WP", 45.0, -100.0));
		List<ServiceMessage> messages = new();

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			new[] { anywhere }, Settings(), DepartureTestData.Build(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}
}
