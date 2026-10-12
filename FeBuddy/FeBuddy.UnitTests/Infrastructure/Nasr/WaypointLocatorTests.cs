using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

using static FeBuddy.Core.Infrastructure.Nasr.WaypointLocator;

namespace FeBuddy.UnitTests.Infrastructure.Nasr;

/// <summary>
/// Covers <see cref="WaypointLocator.Find"/>: argument checks, a lookup
/// restricted to one source, and data that is missing or has blank identifiers.
/// </summary>
public sealed class WaypointLocatorTests
{
	private static NasrCsvDataCollection Data()
	{
		FixCsvDataCollection fixes = new();
		fixes.FixBase.Add(new FixCsvDataModel.FixBase { FixId = " ", LatDecimal = 1, LongDecimal = 1 });
		fixes.FixBase.Add(new FixCsvDataModel.FixBase { FixId = "ALPHA", LatDecimal = 40, LongDecimal = -80 });

		NavCsvDataCollection navaids = new();
		navaids.NavBase.Add(new NavCsvDataModel.NavBase { NavId = "", LatDecimal = 2, LongDecimal = 2 });
		navaids.NavBase.Add(new NavCsvDataModel.NavBase { NavId = "SEA", LatDecimal = 47.4, LongDecimal = -122.3 });

		AptCsvDataCollection airports = new();
		airports.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "SEA", IcaoId = "KSEA", BaseLatDecimal = 47.45, BaseLongDecimal = -122.31 });

		return new NasrCsvDataCollection { Fix = fixes, Nav = navaids, Apt = airports };
	}

	[Fact]
	public void arguments_are_checked()
	{
		Assert.Throws<ArgumentNullException>(() => Find(null!, "ALPHA"));
		Assert.Throws<ArgumentException>(() => Find(Data(), " "));
	}

	[Fact]
	public void a_known_type_searches_only_that_source()
	{
		NasrCsvDataCollection data = Data();

		Assert.Equal((47.4, -122.3, "navaid"), Find(data, "SEA", WaypointType.Navaid));
		Assert.Equal((47.45, -122.31, "airport"), Find(data, "SEA", WaypointType.Airport));
		Assert.Null(Find(data, "SEA", WaypointType.Fix));
		Assert.Null(Find(data, "SEA", (WaypointType)99));
	}

	[Fact]
	public void blank_identifiers_in_the_data_are_never_matched()
	{
		NasrCsvDataCollection data = Data();

		Assert.Equal((40.0, -80.0, "fix"), Find(data, "alpha"));
		Assert.Null(Find(data, "NOPE"));
	}

	[Fact]
	public void a_group_that_was_never_parsed_is_named_in_the_error_not_found_silently()
	{
		NasrCsvDataCollection empty = new();

		InvalidOperationException fix = Assert.Throws<InvalidOperationException>(() => Find(empty, "ALPHA"));
		Assert.Contains("NASR FIX data has not been parsed", fix.Message, StringComparison.Ordinal);
		Assert.Contains("NasrCsvGroups.InUse", fix.Message, StringComparison.Ordinal);

		// A 3-letter ID is a NAVAID first, then an airport.
		Assert.Contains("NASR NAV data", Assert.Throws<InvalidOperationException>(() => Find(empty, "SEA")).Message, StringComparison.Ordinal);

		NasrCsvDataCollection navaidsOnly = new() { Nav = new NavCsvDataCollection() };
		Assert.Contains("NASR APT data", Assert.Throws<InvalidOperationException>(() => Find(navaidsOnly, "SEA")).Message, StringComparison.Ordinal);
	}
}
