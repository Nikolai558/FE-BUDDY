using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers the pure command-building half of <see cref="ArrivalAliasWriter"/>. <c>Generate</c>
/// writes to disk and is not exercised here.
/// </summary>
public sealed class ArrivalAliasWriterTests
{
	private static ArrivalAirportProcedure LocatedBlaid()
	{
		NasrCsvDataCollection data = ArrivalTestData.Blaid();
		ArrivalProcedureReadResult read = ArrivalBuilder.ReadProcedures(data);
		ArrivalLocateResult located = ArrivalBuilder.Locate(read.Procedures, data);

		return Assert.Single(located.AirportProcedures);
	}

	[Fact]
	public void the_blaid2_command_at_las_draws_every_point_exactly_once_in_flying_order()
	{
		const string prefix = ".lasBLAIDf .FF ";

		string command = ArrivalAliasWriter.BuildCommand(LocatedBlaid());

		Assert.StartsWith(prefix, command);

		string[] points = command[prefix.Length..].Split(' ');
		string[] expected = ["BCE", "HOLDM", "AALAN", "EHK", "PGA", "BLAID"];

		Assert.Equal(points.Length, points.Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(expected, points);
	}

	[Fact]
	public void the_command_name_lower_cases_the_airport_and_keeps_the_code_id_upper_case()
	{
		ArrivalAirportProcedure airportProcedure = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "ABC"),
			"1U7",
			new ArrivalPoint("ALPHA", "RP", 40.0, -100.0));

		Assert.Equal(".1u7ABCf", ArrivalAliasWriter.CommandName(airportProcedure));
	}

	[Fact]
	public void a_single_point_procedure_still_gets_a_command()
	{
		ArrivalAirportProcedure airportProcedure = ArrivalTestData.AirportProcedure(
			ArrivalTestData.Procedure(codeId: "ABC"),
			"AAA",
			new ArrivalPoint("ALPHA", "RP", 40.0, -100.0));

		Assert.False(airportProcedure.HasDrawableRoute);
		Assert.Equal(".aaaABCf .FF ALPHA", ArrivalAliasWriter.BuildCommand(airportProcedure));
	}
}
