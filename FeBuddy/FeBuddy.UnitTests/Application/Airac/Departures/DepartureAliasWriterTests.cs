using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Departures;

/// <summary>
/// Covers the pure command-building half of <see cref="DepartureAliasWriter"/>. <c>Generate</c>
/// writes to disk and is not exercised here.
/// </summary>
public class DepartureAliasWriterTests
{
	private static DepartureAirportProcedure LocatedDotss()
	{
		NasrCsvDataCollection data = DepartureTestData.Dotss();
		DepartureProcedureReadResult read = DepartureBuilder.ReadProcedures(data);
		DepartureLocateResult located = DepartureBuilder.Locate(read.Procedures, data);

		return Assert.Single(located.AirportProcedures);
	}

	[Fact]
	public void the_dotss2_command_at_lax_draws_every_point_exactly_once()
	{
		const string prefix = ".laxDOTSSf .FF ";

		string command = DepartureAliasWriter.BuildCommand(LocatedDotss());

		Assert.StartsWith(prefix, command);

		string[] points = command[prefix.Length..].Split(' ');
		string[] expected =
		[
			"DLREY", "ENNEY", "NAANC", "HAYNK", "PEVEE", "HOLTZ", "DOTSS", "DOCKR", "WEILR", "SHAEF",
			"FABRA", "HIIPR", "ADORE", "EYEDL", "HOMER", "CLEEE", "WIILD", "BLCKD", "CSTWY", "CNERY"
		];

		Assert.Equal(points.Length, points.Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(expected.OrderBy(p => p, StringComparer.Ordinal), points.OrderBy(p => p, StringComparer.Ordinal));
	}

	[Fact]
	public void the_command_name_lower_cases_the_airport_and_keeps_the_code_id_upper_case()
	{
		DepartureAirportProcedure airportProcedure = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "ABC"),
			"1U7",
			new DeparturePoint("ALPHA", "WP", 40.0, -100.0));

		Assert.Equal(".1u7ABCf", DepartureAliasWriter.CommandName(airportProcedure));
	}

	[Fact]
	public void a_single_point_procedure_still_gets_a_command()
	{
		DepartureAirportProcedure airportProcedure = DepartureTestData.AirportProcedure(
			DepartureTestData.Procedure(codeId: "ABC"),
			"AAA",
			new DeparturePoint("ALPHA", "WP", 40.0, -100.0));

		Assert.False(airportProcedure.HasDrawableRoute);
		Assert.Equal(".aaaABCf .FF ALPHA", DepartureAliasWriter.BuildCommand(airportProcedure));
	}
}
