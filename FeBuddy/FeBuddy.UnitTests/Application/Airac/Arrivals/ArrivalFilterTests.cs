using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers <see cref="ArrivalFilter"/>: the per-airport ARTCC filter, the amendment-date filter,
/// and the ROI filter.
/// </summary>
public sealed class ArrivalFilterTests
{
	private static readonly DateOnly CycleDate = new(2026, 9, 3);

	/// <summary>A fixed "today" for the Days mode, well after <see cref="CycleDate"/>.</summary>
	private static readonly DateOnly Today = new(2026, 9, 22);

	private static readonly RegionOfInterest SoCal = new(33.0, -119.0, 35.0, -117.0);

	private static ArrivalSettings Settings() => new()
	{
		OutputDirectory = @"C:\Output",
		GenerateGeojson = true,
		GenerateAliasFile = true,
		IncludeFebCustomProperties = false,
	};

	private static ArrivalProcedure Amended(string codeId, DateOnly amended) =>
		ArrivalTestData.Procedure(codeId: codeId, amendmentEffectiveDate: amended, cycleEffectiveDate: CycleDate);

	/// <summary>A procedure shared by ZDC and ZNY, serving DOV (ZDC) and ILG (ZNY) - modelled on the real ARLFT.</summary>
	private static ArrivalProcedure MultiArtccProcedure(string codeId, IReadOnlyDictionary<string, string> artccByAirport, params string[] servedAirports) =>
		new()
		{
			ArrivalName = codeId,
			ArtccText = "ZDC ZNY",
			Artccs = ["ZDC", "ZNY"],
			ArtccByAirport = artccByAirport,
			ComputerCode = "TRANS." + codeId + "1",
			CodeId = codeId,
			AmendmentNo = "ONE",
			AmendmentEffectiveDateText = string.Empty,
			ServedAirports = servedAirports,
			Bodies = [new ArrivalRawRoute("BODY", 1, ArrivalRouteKind.Body, null, [new ArrivalRawPoint("ALPHA", "RP")])],
			Transitions = [],
			BodiesByAirport = new Dictionary<string, IReadOnlyList<(string Name, int Sequence)>>()
		};

	[Fact]
	public void the_artcc_filter_keeps_only_the_listed_artccs()
	{
		ArrivalProcedure zla = ArrivalTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		ArrivalProcedure zoa = ArrivalTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		ArrivalProcedure zse = ArrivalTestData.Procedure(codeId: "THR", artcc: "ZSE");
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[zla, zoa, zse], Settings() with { ArtccFilter = ["ZLA", "ZSE"] }, messages);

		Assert.Equal(["ZLA", "ZSE"], kept.Select(p => p.ArtccText));
	}

	[Fact]
	public void an_empty_artcc_filter_keeps_every_artcc()
	{
		ArrivalProcedure zla = ArrivalTestData.Procedure(codeId: "ONE", artcc: "ZLA");
		ArrivalProcedure zoa = ArrivalTestData.Procedure(codeId: "TWO", artcc: "ZOA");
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure([zla, zoa], Settings(), messages);

		Assert.Equal(2, kept.Count);
	}

	[Fact]
	public void the_artcc_filter_is_per_airport_for_a_multi_artcc_star()
	{
		ArrivalProcedure arlft = MultiArtccProcedure(
			"ARLFT", new Dictionary<string, string> { ["DOV"] = "ZDC", ["ILG"] = "ZNY" }, "DOV", "ILG");
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[arlft], Settings() with { ArtccFilter = ["ZNY"] }, messages);

		ArrivalProcedure result = Assert.Single(kept);
		Assert.Equal(["ILG"], result.ServedAirports);
	}

	[Fact]
	public void the_artcc_filter_keeps_every_served_airport_and_the_same_instance_when_nothing_was_dropped()
	{
		ArrivalProcedure arlft = MultiArtccProcedure(
			"ARLFT", new Dictionary<string, string> { ["DOV"] = "ZDC", ["ILG"] = "ZNY" }, "DOV", "ILG");
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[arlft], Settings() with { ArtccFilter = ["ZDC", "ZNY"] }, messages);

		ArrivalProcedure result = Assert.Single(kept);
		Assert.Equal(["DOV", "ILG"], result.ServedAirports);
		Assert.Same(arlft, result);
	}

	[Fact]
	public void the_artcc_filter_drops_a_multi_artcc_star_when_none_of_its_airports_match()
	{
		ArrivalProcedure arlft = MultiArtccProcedure(
			"ARLFT", new Dictionary<string, string> { ["DOV"] = "ZDC", ["ILG"] = "ZNY" }, "DOV", "ILG");
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[arlft], Settings() with { ArtccFilter = ["ZOB"] }, messages);

		Assert.Empty(kept);
	}

	[Fact]
	public void amended_within_one_cycle_keeps_only_this_cycles_amendments()
	{
		ArrivalProcedure thisCycle = Amended("NEW", new DateOnly(2026, 9, 3));
		ArrivalProcedure dayBefore = Amended("OLD", new DateOnly(2026, 9, 2));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[thisCycle, dayBefore], Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Cycles, AmendedWithinCycles = 1 }, messages);

		Assert.Equal(["NEW"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_four_cycles_keeps_the_cutoff_day_and_drops_the_day_before()
	{
		// 2026-09-03 minus 3 x 28 = 84 days is 2026-06-11.
		DateOnly cutoff = new(2026, 6, 11);
		Assert.Equal(cutoff, CycleDate.AddDays(-84));

		ArrivalProcedure onCutoff = Amended("KEEP", cutoff);
		ArrivalProcedure beforeCutoff = Amended("DROP", cutoff.AddDays(-1));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[onCutoff, beforeCutoff], Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Cycles, AmendedWithinCycles = 4 }, messages);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
	}

	[Fact]
	public void an_unparseable_amendment_date_is_dropped_with_a_warning()
	{
		ArrivalProcedure unreadable = ArrivalTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[unreadable], Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Cycles, AmendedWithinCycles = 1 }, messages);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("NOT A DATE", message.Text);
	}

	[Fact]
	public void amendment_dates_are_ignored_when_the_filter_is_off()
	{
		ArrivalProcedure unreadable = ArrivalTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure([unreadable], Settings(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_days_keeps_the_cutoff_day_and_drops_the_day_before()
	{
		// 2026-09-22 minus 90 days is 2026-06-24.
		DateOnly cutoff = new(2026, 6, 24);
		Assert.Equal(cutoff, Today.AddDays(-90));

		ArrivalProcedure onCutoff = Amended("KEEP", cutoff);
		ArrivalProcedure beforeCutoff = Amended("DROP", cutoff.AddDays(-1));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[onCutoff, beforeCutoff],
			Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Days, AmendedWithinDays = 90 },
			messages,
			Today);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void amended_within_days_counts_back_from_today_not_the_cycle_date()
	{
		// Amended on the cycle date, which is 19 days before Today: outside a 10-day window.
		ArrivalProcedure onCycleDate = Amended("OLD", CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[onCycleDate],
			Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Days, AmendedWithinDays = 10 },
			messages,
			Today);

		Assert.Empty(kept);
	}

	[Fact]
	public void amended_on_or_after_keeps_the_date_itself_and_drops_the_day_before()
	{
		DateOnly since = new(2026, 1, 1);
		ArrivalProcedure onDate = Amended("KEEP", since);
		ArrivalProcedure dayBefore = Amended("DROP", since.AddDays(-1));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[onDate, dayBefore],
			Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Date, AmendedOnOrAfter = since },
			messages,
			Today);

		Assert.Equal(["KEEP"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void date_mode_without_a_date_throws()
	{
		List<ServiceMessage> messages = [];

		Assert.Throws<ArgumentException>(() => ArrivalFilter.ByProcedure(
			[Amended("ANY", CycleDate)],
			Settings() with { AmendmentFilter = ArrivalAmendmentFilter.Date },
			messages,
			Today));
	}

	public static TheoryData<ArrivalAmendmentFilter> ActiveAmendmentFilters =>
	[
		ArrivalAmendmentFilter.Cycles,
		ArrivalAmendmentFilter.Days,
		ArrivalAmendmentFilter.Date,
	];

	private static ArrivalSettings WithAmendmentFilter(ArrivalAmendmentFilter mode) => Settings() with
	{
		AmendmentFilter = mode,
		AmendedWithinCycles = 1,
		AmendedWithinDays = 1,
		AmendedOnOrAfter = Today
	};

	[Theory]
	[MemberData(nameof(ActiveAmendmentFilters))]
	public void future_amendments_are_kept_in_every_mode(ArrivalAmendmentFilter mode)
	{
		ArrivalProcedure future = Amended("FUT", Today.AddDays(30));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[future], WithAmendmentFilter(mode), messages, Today);

		Assert.Equal(["FUT"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Theory]
	[MemberData(nameof(ActiveAmendmentFilters))]
	public void an_unparseable_amendment_date_is_dropped_with_a_warning_in_every_mode(ArrivalAmendmentFilter mode)
	{
		ArrivalProcedure unreadable = ArrivalTestData.Procedure(
			codeId: "BAD", amendmentEffectiveDate: null, amendmentEffectiveDateText: "NOT A DATE", cycleEffectiveDate: CycleDate);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[unreadable], WithAmendmentFilter(mode), messages, Today);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("NOT A DATE", message.Text);
	}

	[Fact]
	public void a_missing_cycle_date_is_dropped_with_a_warning_in_cycles_mode()
	{
		ArrivalProcedure noCycle = ArrivalTestData.Procedure(
			codeId: "NOCYC", amendmentEffectiveDate: CycleDate, cycleEffectiveDate: null);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[noCycle], WithAmendmentFilter(ArrivalAmendmentFilter.Cycles), messages, Today);

		Assert.Empty(kept);
		Assert.Equal(LogLevel.Warning, Assert.Single(messages).Level);
	}

	[Fact]
	public void none_mode_keeps_every_procedure_whatever_its_amendment_values()
	{
		ArrivalProcedure ancient = Amended("OLD", new DateOnly(1990, 1, 1));
		ArrivalProcedure recent = Amended("NEW", CycleDate);

		// The per-mode values are set but must be ignored while the mode is None.
		ArrivalSettings settings = WithAmendmentFilter(ArrivalAmendmentFilter.None);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalProcedure> kept = ArrivalFilter.ByProcedure(
			[ancient, recent], settings, messages, Today);

		Assert.Equal(["OLD", "NEW"], kept.Select(p => p.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void waypoint_mode_keeps_a_pair_with_any_point_inside_the_roi()
	{
		ArrivalAirportProcedure partlyInside = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "IN"), "AAA",
			new ArrivalPoint("FARAW", "RP", 45.0, -100.0),
			new ArrivalPoint("NEARB", "RP", 34.0, -118.0));
		ArrivalAirportProcedure outside = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "OUT"), "AAA",
			new ArrivalPoint("FARAW", "RP", 45.0, -100.0),
			new ArrivalPoint("FARBB", "RP", 46.0, -101.0));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalAirportProcedure> kept = ArrivalFilter.ByRoi(
			[partlyInside, outside],
			Settings() with { Roi = SoCal, RoiMode = ArrivalRoiMode.Waypoint },
			ArrivalTestData.Build(),
			messages);

		Assert.Equal(["IN"], kept.Select(p => p.Procedure.CodeId));
	}

	[Fact]
	public void airport_mode_keeps_by_the_airport_reference_point()
	{
		// Points are deliberately placed opposite to their airport so only the airport decides.
		ArrivalAirportProcedure atSocal = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "SOCST"), "SOCAL",
			new ArrivalPoint("FARAW", "RP", 45.0, -100.0));
		ArrivalAirportProcedure atFar = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "FARST"), "FAR",
			new ArrivalPoint("NEARB", "RP", 34.0, -118.0));

		NasrCsvDataCollection data = ArrivalTestData.Build(airports:
		[
			ArrivalTestData.Airport("SOCAL", 33.9425, -118.408, "KSOC"),
			ArrivalTestData.Airport("FAR", 46.92, -96.81, "KFAR"),
		]);
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalAirportProcedure> kept = ArrivalFilter.ByRoi(
			[atSocal, atFar],
			Settings() with { Roi = SoCal, RoiMode = ArrivalRoiMode.Airport },
			data,
			messages);

		Assert.Equal(["SOCST"], kept.Select(p => p.Procedure.CodeId));
		Assert.Empty(messages);
	}

	[Fact]
	public void airport_mode_drops_an_airport_with_no_apt_base_row_with_an_info_message()
	{
		ArrivalAirportProcedure atUnknown = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "CYQG"), "CYQG",
			new ArrivalPoint("NEARB", "RP", 34.0, -118.0));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalAirportProcedure> kept = ArrivalFilter.ByRoi(
			[atUnknown],
			Settings() with { Roi = SoCal, RoiMode = ArrivalRoiMode.Airport },
			ArrivalTestData.Build(),
			messages);

		Assert.Empty(kept);
		ServiceMessage message = Assert.Single(messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("CYQG", message.Text);
	}

	[Fact]
	public void no_roi_keeps_everything()
	{
		ArrivalAirportProcedure anywhere = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "ANY"), "NOAPT",
			new ArrivalPoint("FARAW", "RP", 45.0, -100.0));
		List<ServiceMessage> messages = [];

		IReadOnlyList<ArrivalAirportProcedure> kept = ArrivalFilter.ByRoi(
			[anywhere], Settings(), ArrivalTestData.Build(), messages);

		Assert.Single(kept);
		Assert.Empty(messages);
	}
}
