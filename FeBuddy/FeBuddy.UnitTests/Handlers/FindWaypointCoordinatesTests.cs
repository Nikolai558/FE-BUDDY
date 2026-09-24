using FeBuddy.Core.Handlers.General;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Parsers.NASR.CSV;

using static FeBuddy.Core.Handlers.General.FindWaypointCoordinates;

namespace FeBuddy.UnitTests.Handlers;

/// <summary>
/// Covers <see cref="FindWaypointCoordinates.GetCoordinates"/>: argument checks, a lookup
/// restricted to one source, and data that is missing or has blank identifiers.
/// </summary>
public class FindWaypointCoordinatesTests
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
		Assert.Throws<ArgumentNullException>(() => GetCoordinates(null!, "ALPHA"));
		Assert.Throws<ArgumentException>(() => GetCoordinates(Data(), " "));
	}

	[Fact]
	public void a_known_type_searches_only_that_source()
	{
		NasrCsvDataCollection data = Data();

		Assert.Equal((47.4, -122.3, "navaid"), GetCoordinates(data, "SEA", WaypointType.Navaid));
		Assert.Equal((47.45, -122.31, "airport"), GetCoordinates(data, "SEA", WaypointType.Airport));
		Assert.Null(GetCoordinates(data, "SEA", WaypointType.Fix));
		Assert.Null(GetCoordinates(data, "SEA", (WaypointType)99));
	}

	[Fact]
	public void blank_identifiers_in_the_data_are_never_matched()
	{
		NasrCsvDataCollection data = Data();

		Assert.Equal((40.0, -80.0, "fix"), GetCoordinates(data, "alpha"));
		Assert.Null(GetCoordinates(data, "NOPE"));
	}

	[Fact]
	public void a_collection_that_was_never_parsed_finds_nothing()
	{
		NasrCsvDataCollection empty = new();

		Assert.Null(GetCoordinates(empty, "ALPHA"));
		Assert.Null(GetCoordinates(empty, "SEA"));
	}
}
