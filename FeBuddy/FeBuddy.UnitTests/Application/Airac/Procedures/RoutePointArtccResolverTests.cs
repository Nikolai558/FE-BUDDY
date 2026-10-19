using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Procedures;

/// <summary>
/// Covers <see cref="RoutePointArtccResolver"/> (Feature #367): a route point is in a facility when
/// the fix or NAVAID it is names it in either ARTCC field; it is matched by type and region, not the
/// first row with its ID; one still matched by more than one row counts if any does, and is logged;
/// one in neither file counts by the airport it names only when airports are allowed.
/// </summary>
[Collection("AppLog")]
public sealed class RoutePointArtccResolverTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_RoutePoints_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log at a throwaway folder.</summary>
	public RoutePointArtccResolverTests() => AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));

	/// <summary>Restores the real log, and deletes the folder.</summary>
	public void Dispose()
	{
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

	private static RoutePointArtccResolver Resolver(
		IEnumerable<FixCsvDataModel.FixBase>? fixes = null,
		IEnumerable<NavCsvDataModel.NavBase>? navaids = null,
		IEnumerable<AptCsvDataModel.AptBase>? airports = null) =>
		new(ProcedureTestData.RouteNasr(apt: airports, fixes: fixes, navaids: navaids));

	[Theory]
	[InlineData("ZDC", "ZNY", true)] // high
	[InlineData("ZNY", "ZDC", true)] // low
	[InlineData(null, "ZDC", true)] // low only, as 186 of 2611's are
	[InlineData("ZNY", "ZOB", false)]
	[InlineData(null, null, false)]
	public void a_fix_is_in_the_facility_either_artcc_field_names(string? high, string? low, bool inFacility)
	{
		RoutePointArtccResolver resolver = Resolver(fixes: [ProcedureTestData.FixRow("DUCKY", high, low)]);

		Assert.Equal(inFacility, resolver.IsInFacility(new ProcedureRoutePoint("DUCKY", "WP", "K6"), "ZDC", useAirports: false));
	}

	[Theory]
	[InlineData("ZDC", "ZNY", true)]
	[InlineData("ZNY", "zdc", true)]
	[InlineData("ZNY", "ZOB", false)]
	public void a_navaid_is_in_the_facility_either_artcc_field_names(string? high, string? low, bool inFacility)
	{
		RoutePointArtccResolver resolver = Resolver(navaids: [ProcedureTestData.NavRow("DCA", "VOR/DME", high, low)]);

		Assert.Equal(inFacility, resolver.IsInFacility(new ProcedureRoutePoint("DCA", "VOR/DME", null), "ZDC", useAirports: false));
	}

	/// <summary>Two NAVAIDs share an ID: the route's type says which one it is.</summary>
	[Fact]
	public void an_ndb_and_a_vor_with_one_id_are_told_apart_by_type()
	{
		RoutePointArtccResolver resolver = Resolver(navaids:
		[
			ProcedureTestData.NavRow("SB", "NDB", "ZAU", "ZAU"),
			ProcedureTestData.NavRow("SB", "VORTAC", "ZLA", "ZLA"),
		]);

		Assert.True(resolver.IsInFacility(new ProcedureRoutePoint("SB", "VORTAC", null), "ZLA", useAirports: false));
		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("SB", "VORTAC", null), "ZAU", useAirports: false));
		Assert.True(resolver.IsInFacility(new ProcedureRoutePoint("SB", "NDB", null), "ZAU", useAirports: false));
		Assert.DoesNotContain(AppLog.Entries, entry => entry.Message.StartsWith("Route point ", StringComparison.Ordinal));
	}

	[Fact]
	public void two_fixes_with_one_id_are_told_apart_by_region()
	{
		RoutePointArtccResolver resolver = Resolver(fixes:
		[
			ProcedureTestData.FixRow("ALPHA", "ZDC", "ZDC", region: "K6"),
			ProcedureTestData.FixRow("ALPHA", "ZAN", "ZAN", region: "PA"),
		]);

		Assert.True(resolver.IsInFacility(new ProcedureRoutePoint("ALPHA", "WP", "K6"), "ZDC", useAirports: false));
		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("ALPHA", "WP", "PA"), "ZDC", useAirports: false));
		Assert.DoesNotContain(AppLog.Entries, entry => entry.Message.StartsWith("Route point ", StringComparison.Ordinal));
	}

	/// <summary>With no region on the route to tell them apart, it is in any facility one of them is in, and logged once.</summary>
	[Fact]
	public void a_point_still_matching_two_rows_is_in_either_facility_and_logged_once()
	{
		RoutePointArtccResolver resolver = Resolver(navaids:
		[
			ProcedureTestData.NavRow("SB", "NDB", "ZLA", "ZLA"),
			ProcedureTestData.NavRow("SB", "NDB", null, null),
		]);
		ProcedureRoutePoint point = new("SB", "NDB", null);

		Assert.True(resolver.IsInFacility(point, "ZLA", useAirports: false));
		Assert.False(resolver.IsInFacility(point, "ZAU", useAirports: false));

		Assert.Single(AppLog.Entries, entry => entry.Message.StartsWith("Route point SB (NDB) matches 2 rows", StringComparison.Ordinal));
	}

	/// <summary>A region on only one side doesn't rule a row out: a route point with none, or a fix with none.</summary>
	[Fact]
	public void a_region_missing_on_either_side_rules_nothing_out()
	{
		RoutePointArtccResolver resolver = Resolver(fixes:
		[
			ProcedureTestData.FixRow("ALPHA", "ZDC", "ZDC", region: "K6"),
			ProcedureTestData.FixRow("ALPHA", "ZAN", "ZAN", region: "PA"),
			ProcedureTestData.FixRow("BRAVO", "ZNY", "ZNY", region: " "),
		]);

		Assert.True(resolver.IsInFacility(new ProcedureRoutePoint("ALPHA", "WP", null), "ZAN", useAirports: false));
		Assert.True(resolver.IsInFacility(new ProcedureRoutePoint("BRAVO", "WP", "K6"), "ZNY", useAirports: false));
		Assert.Single(AppLog.Entries, entry => entry.Message.StartsWith("Route point ALPHA (WP) matches 2 rows", StringComparison.Ordinal));
	}

	/// <summary>Found, but in no ARTCC: in no facility, and never counted by an airport of the same name.</summary>
	[Fact]
	public void rows_in_no_artcc_put_a_point_in_no_facility_and_say_so_when_ambiguous()
	{
		RoutePointArtccResolver resolver = Resolver(
			fixes: [ProcedureTestData.FixRow("GHOST", null, " ", region: ""), ProcedureTestData.FixRow("GHOST", " ", null, region: "")],
			airports: [ProcedureTestData.AptBaseRow("GHOST", respArtccId: "ZDC")]);

		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("GHOST", "WP", "K6"), "ZDC", useAirports: true));
		Assert.Single(AppLog.Entries, entry => entry.Message == "Route point GHOST (WP, region K6) matches 2 rows; it counts as in any facility one of them is in (none).");
	}

	[Fact]
	public void rows_missing_their_ids_or_types_are_read_without_failing()
	{
		RoutePointArtccResolver resolver = Resolver(
			fixes: [ProcedureTestData.FixRow(null!, "ZDC", "ZDC")],
			navaids: [ProcedureTestData.NavRow(null!, null!, "ZDC", "ZDC")]);

		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("DCA", "VOR/DME", null), "ZDC", useAirports: false));
	}

	[Fact]
	public void a_fix_matched_by_type_never_takes_a_navaid_with_its_id()
	{
		RoutePointArtccResolver resolver = Resolver(navaids: [ProcedureTestData.NavRow("DUCKY", "VORTAC", "ZDC", "ZDC")]);

		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("DUCKY", "WP", null), "ZDC", useAirports: false));
		Assert.False(resolver.IsInFacility(new ProcedureRoutePoint("DUCKY", "NDB", null), "ZDC", useAirports: false));
	}

	[Theory]
	[InlineData("DCA", false, false)] // an airport, but airports aren't allowed: Area isn't None
	[InlineData("DCA", true, true)] // its FAA ID, with Area None
	[InlineData("KDCA", true, true)] // its ICAO ID
	[InlineData("XYZ", true, false)] // not an airport either
	public void a_point_in_neither_file_counts_by_its_airport_only_when_airports_are_allowed(string id, bool useAirports, bool inFacility)
	{
		RoutePointArtccResolver resolver = Resolver(airports:
		[
			ProcedureTestData.AptBaseRow("DCA", icaoId: "KDCA", respArtccId: "ZDC"),
			ProcedureTestData.AptBaseRow("JFK", icaoId: "KJFK", respArtccId: " "),
		]);

		Assert.Equal(inFacility, resolver.IsInFacility(new ProcedureRoutePoint(id, "WP", null), "ZDC", useAirports));
	}

	[Fact]
	public void without_airports_a_point_in_neither_file_is_in_no_facility()
	{
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr();
		nasr.Apt = null;

		Assert.False(new RoutePointArtccResolver(nasr).IsInFacility(new ProcedureRoutePoint("DCA", "WP", null), "ZDC", useAirports: true));
	}

	[Fact]
	public void the_resolver_needs_the_fix_and_nav_groups_and_its_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => new RoutePointArtccResolver(null!));
		Assert.Throws<InvalidOperationException>(() => new RoutePointArtccResolver(new NasrCsvDataCollection { Nav = new NavCsvDataCollection() }));
		Assert.Throws<InvalidOperationException>(() => new RoutePointArtccResolver(new NasrCsvDataCollection { Fix = new FixCsvDataCollection() }));

		RoutePointArtccResolver resolver = Resolver();
		Assert.Throws<ArgumentNullException>(() => resolver.IsInFacility(null!, "ZDC", useAirports: false));
		Assert.Throws<ArgumentException>(() => resolver.IsInFacility(new ProcedureRoutePoint("A", "WP", null), " ", useAirports: false));
	}
}
