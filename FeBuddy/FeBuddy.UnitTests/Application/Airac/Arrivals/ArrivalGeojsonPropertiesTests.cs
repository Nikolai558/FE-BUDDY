using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Domain.Arrivals.Models;

using NetTopologySuite.Features;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Covers which <c>feb.*</c> properties land on which Feature: <c>feb.pointId</c> on the
/// per-point Symbols and Text Features, <c>feb.waypoints</c> on the Lines Feature only, and
/// <c>feb.artcc</c> on the airport copy's own ARTCC rather than a single procedure-level one.
/// </summary>
public sealed class ArrivalGeojsonPropertiesTests
{
	private static ArrivalSettings SettingsWith(params string[] febProperties)
	{
		Dictionary<string, string> raw = new()
		{
			["OutputDirectory"] = @"C:\unused",
			["IncludeFebCustomProperties"] = "Y",
			["FebProperties"] = string.Join(',', febProperties),
		};

		return ArrivalSettingsParser.Parse(raw).Settings;
	}

	private static ArrivalAirportProcedure Sample(out ArrivalPoint point)
	{
		point = new ArrivalPoint("PAVYL", "RP", 42.2, -83.3);
		ArrivalPoint other = new("HHOWE", "RP", 42.4, -83.0);
		return ArrivalTestData.AirportProcedure(ArrivalTestData.Procedure(), "DTW", point, other);
	}

	[Fact]
	public void a_point_feature_carries_its_point_id_and_no_waypoint_list()
	{
		ArrivalAirportProcedure airportProcedure = Sample(out ArrivalPoint point);
		AttributesTable attributes = [];

		ArrivalGeojsonWriter.AddFebProperties(
			attributes, airportProcedure, SettingsWith("arrivalName", "pointId", "arptId", "waypoints"), point);

		Assert.Equal("PAVYL", attributes["feb.pointId"]);
		Assert.Equal("DTW", attributes["feb.arptId"]);
		Assert.False(attributes.Exists("feb.waypoints"));
	}

	[Fact]
	public void the_lines_feature_carries_the_waypoint_list_and_no_point_id()
	{
		ArrivalAirportProcedure airportProcedure = Sample(out _);
		AttributesTable attributes = [];

		ArrivalGeojsonWriter.AddFebProperties(
			attributes, airportProcedure, SettingsWith("arrivalName", "pointId", "waypoints"), point: null);

		Assert.False(attributes.Exists("feb.pointId"));
		Assert.Equal(new[] { "PAVYL", "HHOWE" }, (string[])attributes["feb.waypoints"]);
	}

	[Fact]
	public void point_id_and_waypoints_parse_in_the_order_given()
	{
		ArrivalSettings settings = SettingsWith("arrivalName", "pointId", "waypoints");

		Assert.Equal(
			[ArrivalFebProperty.ArrivalName, ArrivalFebProperty.PointId, ArrivalFebProperty.Waypoints],
			settings.FebProperties);
	}

	[Fact]
	public void feb_artcc_is_the_airport_copys_own_artcc_not_a_single_procedure_level_one()
	{
		ArrivalProcedure arlft = new()
		{
			ArrivalName = "ARLFT",
			ArtccText = "ZDC ZNY",
			Artccs = ["ZDC", "ZNY"],
			ArtccByAirport = new Dictionary<string, string> { ["DOV"] = "ZDC", ["ILG"] = "ZNY" },
			ComputerCode = "ENO.ARLFT2",
			CodeId = "ARLFT",
			AmendmentNo = "TWO",
			AmendmentEffectiveDateText = "2024/01/25",
			ServedAirports = ["33N", "DOV", "ILG"],
			Bodies = [new ArrivalRawRoute("BODY", 1, ArrivalRouteKind.Body, null, [new ArrivalRawPoint("ALPHA", "RP")])],
			Transitions = [],
			BodiesByAirport = new Dictionary<string, IReadOnlyList<(string Name, int Sequence)>>()
		};

		ArrivalAirportProcedure ilg = ArrivalTestData.AirportProcedure(arlft, "ILG", new ArrivalPoint("ALPHA", "RP", 39.68, -75.61));
		AttributesTable attributes = [];

		ArrivalGeojsonWriter.AddFebProperties(attributes, ilg, SettingsWith("artcc"), point: null);

		Assert.Equal("ZNY", attributes["feb.artcc"]);
	}
}
