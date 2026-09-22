using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Services.Airac.Departures;

using NetTopologySuite.Features;

using FeBuddy.UnitTests.Services.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Services.Airac.Departures;

/// <summary>
/// Covers which <c>feb.*</c> properties land on which Feature: <c>feb.pointId</c> on the
/// per-point Symbols and Text Features, <c>feb.waypoints</c> on the Lines Feature only.
/// </summary>
public class DepartureGeojsonPropertiesTests
{
	private static DepartureSettings SettingsWith(params string[] febProperties)
	{
		Dictionary<string, string> raw = new()
		{
			["OutputDirectory"] = @"C:\unused",
			["IncludeFebCustomProperties"] = "Y",
			["FebProperties"] = string.Join(',', febProperties),
		};

		return DepartureSettingsParser.Parse(raw).Settings;
	}

	private static DepartureAirportProcedure Sample(out DeparturePoint point)
	{
		point = new DeparturePoint("PAVYL", "WP", 42.2, -83.3);
		DeparturePoint other = new("HHOWE", "WP", 42.4, -83.0);
		return DepartureTestData.AirportProcedure(DepartureTestData.Procedure(), "DTW", point, other);
	}

	[Fact]
	public void a_point_feature_carries_its_point_id_and_no_waypoint_list()
	{
		DepartureAirportProcedure airportProcedure = Sample(out DeparturePoint point);
		AttributesTable attributes = new();

		DepartureGeojsonService.AddFebProperties(
			attributes, airportProcedure, SettingsWith("dpName", "pointId", "arptId", "waypoints"), point);

		Assert.Equal("PAVYL", attributes["feb.pointId"]);
		Assert.Equal("DTW", attributes["feb.arptId"]);
		Assert.False(attributes.Exists("feb.waypoints"));
	}

	[Fact]
	public void the_lines_feature_carries_the_waypoint_list_and_no_point_id()
	{
		DepartureAirportProcedure airportProcedure = Sample(out _);
		AttributesTable attributes = new();

		DepartureGeojsonService.AddFebProperties(
			attributes, airportProcedure, SettingsWith("dpName", "pointId", "waypoints"), point: null);

		Assert.False(attributes.Exists("feb.pointId"));
		Assert.Equal(new[] { "PAVYL", "HHOWE" }, (string[])attributes["feb.waypoints"]);
	}

	[Fact]
	public void point_id_and_waypoints_parse_in_the_order_given()
	{
		DepartureSettings settings = SettingsWith("dpName", "pointId", "waypoints");

		Assert.Equal(
			new[] { DepartureFebProperty.DpName, DepartureFebProperty.PointId, DepartureFebProperty.Waypoints },
			settings.FebProperties);
	}
}
