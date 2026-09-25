using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Departures;

/// <summary>
/// Covers <see cref="DepartureFilter"/>: the obstacle-departure, ARTCC and amendment-date filters.
/// </summary>
public sealed class DepartureFilterTests
{
	private static readonly DateOnly CycleDate = new(2026, 9, 3);

	/// <summary>A fixed "today" for the Days mode, well after <see cref="CycleDate"/>.</summary>
	private static readonly DateOnly Today = new(2026, 9, 22);

	private static readonly RegionOfInterest SoCal = new(33.0, -119.0, 35.0, -117.0);

	private static DepartureSettings Settings() => new()
	{
		OutputDirectory = @"C:\Output",
		GenerateGeojson = true,
		GenerateAliasFile = true,
		IncludeFebCustomProperties = false,
		IncludeCrcLineDefaults = false,
		IncludeCrcSymbolDefaults = false,
		IncludeCrcTextDefaults = false
	};

	private static DepartureProcedure Amended(string codeId, DateOnly amended) =>
		DepartureTestData.Procedure(codeId: codeId, amendmentEffectiveDate: amended, cycleEffectiveDate: CycleDate);

	[Fact]
	public void obstacle_departures_are_dropped_when_excluded()
	{
		DepartureProcedure sid = DepartureTestData.Procedure(codeId: "SID");
		DepartureProcedure odp = DepartureTestData.Procedure(codeId: "ODP", isObstacleDeparture: true);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[sid, odp], Settings() with { IncludeObstacleDepartures = false }, messages);

		Assert.Equal(["SID"], kept.Select(p => p.CodeId));
	}

	[Fact]
	public void obstacle_departures_are_kept_when_included()
	{
		DepartureProcedure sid = DepartureTestData.Procedure(codeId: "SID");
		DepartureProcedure odp = DepartureTestData.Procedure(codeId: "ODP", isObstacleDeparture: true);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[sid, odp], Settings() with { IncludeObstacleDepartures = true }, messages);

		Assert.Equal(["SID", "ODP"], kept.Select(p => p.CodeId));
	}

	[Fact]
	public void the_artcc_filter_keeps_only_the_listed_artccs()
	{
		DepartureProcedure zla = DepartureTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		DepartureProcedure zoa = DepartureTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		DepartureProcedure zse = DepartureTestData.Procedure(codeId: "THR", artcc: "ZSE");
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[zla, zoa, zse], Settings() with { ArtccFilter = ["ZLA", "ZSE"] }, messages);

		Assert.Equal(["ZLA", "ZSE"], kept.Select(p => p.Artcc));
	}

	[Fact]
	public void an_empty_artcc_filter_keeps_every_artcc()
	{
		DepartureProcedure zla = DepartureTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		DepartureProcedure zoa = DepartureTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure([zla, zoa], Settings(), messages);

		Assert.Equal(2, kept.Count);
	}

	[Fact]
	public void amended_within_one_cycle_keeps_only_this_cycles_amendments()
	{
		DepartureProcedure thisCycle = Amended("NEW", new DateOnly(2026, 9, 3));
		DepartureProcedure dayBefore = Amended("OLD", new DateOnly(2026, 9, 2));
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[thisCycle, dayBefore], Settings() with { AmendmentFilter = DepartureAmendmentFilter.Cycles, AmendedWithinCycles = 1 }, messages);

		Assert.Equal(["NEW"], kept.Select(p => p.CodeId));
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
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[onCutoff, beforeCutoff], Settings() with { AmendmentFilter = DepartureAmendmentFilter.Cycles, AmendedWithinCycles = 4 }, messages);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
	}

	[Fact]
	public void an_unparseable_amendment_date_is_dropped_with_a_warning()
	{
		DepartureProcedure unreadable = DepartureTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[unreadable], Settings() with { AmendmentFilter = DepartureAmendmentFilter.Cycles, AmendedWithinCycles = 1 }, messages);

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
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure([unreadable], Settings(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_days_keeps_the_cutoff_day_and_drops_the_day_before()
	{
		// 2026-09-22 minus 90 days is 2026-06-24.
		DateOnly cutoff = new(2026, 6, 24);
		Assert.Equal(cutoff, Today.AddDays(-90));

		DepartureProcedure onCutoff = Amended("KEEP", cutoff);
		DepartureProcedure beforeCutoff = Amended("DROP", cutoff.AddDays(-1));
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[onCutoff, beforeCutoff],
			Settings() with { AmendmentFilter = DepartureAmendmentFilter.Days, AmendedWithinDays = 90 },
			messages,
			Today);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_days_counts_back_from_today_not_the_cycle_date()
	{
		// Amended on the cycle date, which is 19 days before Today: outside a 10-day window.
		DepartureProcedure onCycleDate = Amended("OLD", CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[onCycleDate],
			Settings() with { AmendmentFilter = DepartureAmendmentFilter.Days, AmendedWithinDays = 10 },
			messages,
			Today);

		Assert.Empty(kept);
	}

	[Fact]
	public void amended_on_or_after_keeps_the_date_itself_and_drops_the_day_before()
	{
		DateOnly since = new(2026, 1, 1);
		DepartureProcedure onDate = Amended("KEEP", since);
		DepartureProcedure dayBefore = Amended("DROP", since.AddDays(-1));
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[onDate, dayBefore],
			Settings() with { AmendmentFilter = DepartureAmendmentFilter.Date, AmendedOnOrAfter = since },
			messages,
			Today);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void date_mode_without_a_date_throws()
	{
		List<ServiceMessage> messages = [];

		Assert.Throws<ArgumentException>(() => DepartureFilter.ByProcedure(
			[Amended("ANY", CycleDate)],
			Settings() with { AmendmentFilter = DepartureAmendmentFilter.Date },
			messages,
			Today));
	}

	public static TheoryData<DepartureAmendmentFilter> ActiveAmendmentFilters =>
	[
		DepartureAmendmentFilter.Cycles,
		DepartureAmendmentFilter.Days,
		DepartureAmendmentFilter.Date,
	];

	private static DepartureSettings WithAmendmentFilter(DepartureAmendmentFilter mode) => Settings() with
	{
		AmendmentFilter = mode,
		AmendedWithinCycles = 1,
		AmendedWithinDays = 1,
		AmendedOnOrAfter = Today
	};

	[Theory]
	[MemberData(nameof(ActiveAmendmentFilters))]
	public void future_amendments_are_kept_in_every_mode(DepartureAmendmentFilter mode)
	{
		DepartureProcedure future = Amended("FUT", Today.AddDays(30));
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[future], WithAmendmentFilter(mode), messages, Today);

		Assert.Equal(["FUT"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Theory]
	[MemberData(nameof(ActiveAmendmentFilters))]
	public void an_unparseable_amendment_date_is_dropped_with_a_warning_in_every_mode(DepartureAmendmentFilter mode)
	{
		DepartureProcedure unreadable = DepartureTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[unreadable], WithAmendmentFilter(mode), messages, Today);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("NOT A DATE", message.Text);
	}

	[Fact]
	public void a_missing_cycle_date_is_dropped_with_a_warning_in_cycles_mode()
	{
		DepartureProcedure noCycle = DepartureTestData.Procedure(
			codeId: "NOCYC", amendmentEffectiveDate: CycleDate, cycleEffectiveDate: null);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[noCycle], WithAmendmentFilter(DepartureAmendmentFilter.Cycles), messages, Today);

		Assert.Empty(kept);
		Assert.Equal(LogLevel.Warning, Assert.Single(messages).Level);
	}

	[Fact]
	public void none_mode_keeps_every_procedure_whatever_its_amendment_values()
	{
		DepartureProcedure ancient = Amended("OLD", new DateOnly(1990, 1, 1));
		DepartureProcedure recent = Amended("NEW", CycleDate);

		// The per-mode values are set but must be ignored while the mode is None.
		DepartureSettings settings = WithAmendmentFilter(DepartureAmendmentFilter.None);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureProcedure> kept = DepartureFilter.ByProcedure(
			[ancient, recent], settings, messages, Today);

		Assert.Equal(["OLD", "NEW"], kept.Select(p => p.CodeId));
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
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			[partlyInside, outside],
			Settings() with { Roi = SoCal, RoiMode = DepartureRoiMode.Waypoint },
			DepartureTestData.Build(),
			messages);

		Assert.Equal(["IN"], kept.Select(p => p.Procedure.CodeId));
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

		NasrCsvDataCollection data = DepartureTestData.Build(airports:
		[
			DepartureTestData.Airport(DepartureTestData.LaxId, DepartureTestData.LaxLatitude, DepartureTestData.LaxLongitude, "KLAX"),
			DepartureTestData.Airport("FAR", 46.92, -96.81, "KFAR"),
		]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			[atLax, atFar],
			Settings() with { Roi = SoCal, RoiMode = DepartureRoiMode.Airport },
			data,
			messages);

		Assert.Equal(["LAXDP"], kept.Select(p => p.Procedure.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void airport_mode_drops_an_airport_with_no_apt_base_row_with_an_info_message()
	{
		DepartureAirportProcedure atUnknown = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "CYQG"), "CYQG",
			new DeparturePoint("NEARB", "WP", 34.0, -118.0));
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			[atUnknown],
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
		List<ServiceMessage> messages = [];

		IReadOnlyList<DepartureAirportProcedure> kept = DepartureFilter.ByRoi(
			[anywhere], Settings(), DepartureTestData.Build(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}
}
