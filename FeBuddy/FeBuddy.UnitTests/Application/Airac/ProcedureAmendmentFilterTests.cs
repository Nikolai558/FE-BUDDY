using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Departures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers what Departures and Arrivals share with their tabs: the amendment filter's limits and date format
/// (<see cref="ProcedureAmendmentFilter"/>), and how their GeoJSON files are named and filed, as a page lists them.
/// </summary>
public sealed class ProcedureAmendmentFilterTests
{
	/// <summary>Both parsers take the limits, and refuse one past them.</summary>
	[Theory]
	[InlineData("Cycles", "AmendedWithinCycles", ProcedureAmendmentFilter.MaxWithinCycles)]
	[InlineData("Days", "AmendedWithinDays", ProcedureAmendmentFilter.MaxWithinDays)]
	public void both_parsers_take_the_limits_and_no_more(string filter, string key, int maximum)
	{
		Dictionary<string, string> settings = new() { ["OutputDirectory"] = @"C:\Output", ["Area"] = "Everything", ["AmendmentFilter"] = filter, [key] = $"{maximum}" };

		DepartureSettingsParser.Parse(settings);
		ArrivalSettingsParser.Parse(settings);

		settings[key] = $"{maximum + 1}";
		Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings));
		Assert.Throws<ArgumentException>(() => ArrivalSettingsParser.Parse(settings));
	}

	/// <summary>Both parsers read the date in the one format, and say which.</summary>
	[Fact]
	public void both_parsers_read_the_date_in_one_format()
	{
		Dictionary<string, string> settings = new() { ["OutputDirectory"] = @"C:\Output", ["Area"] = "Everything", ["AmendmentFilter"] = "Date", ["AmendedOnOrAfter"] = "2026-01-31" };

		Assert.Equal(new DateOnly(2026, 1, 31), DepartureSettingsParser.Parse(settings).Settings.AmendedOnOrAfter);
		Assert.Equal(new DateOnly(2026, 1, 31), ArrivalSettingsParser.Parse(settings).Settings.AmendedOnOrAfter);

		settings["AmendedOnOrAfter"] = "01/31/2026";
		Assert.Contains($"Use the format {ProcedureAmendmentFilter.DateFormat}", Assert.Throws<ArgumentException>(() => DepartureSettingsParser.Parse(settings)).Message, StringComparison.Ordinal);
	}

	/// <summary>How a page lists the files: one spelling of the name, and the folder under the cycle's Geojson folder.</summary>
	[Fact]
	public void the_file_patterns_are_spelled_one_way()
	{
		Assert.Equal("<Airport ID>_<Departure ID>", DepartureOutputFiles.GeojsonFileNamePattern);
		Assert.Equal("<Airport ID>_<Arrival ID>_STAR", ArrivalOutputFiles.GeojsonFileNamePattern);
		Assert.Equal(@"Geojson\<ARTCC ID>\<Airport ID>", DepartureOutputFiles.GeojsonFolderPattern);
		Assert.Equal(DepartureOutputFiles.GeojsonFolderPattern, ArrivalOutputFiles.GeojsonFolderPattern);
	}
}
