using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Map;
using FeBuddy.Core.Application.Map.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Domain.Map.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airports.Fixtures;
using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Map;

/// <summary>
/// Covers <see cref="AiracMapData"/>: each live layer's name and tooltip, the shapes it holds - every ARTCC ring
/// with its label, only towered airports and their runways, only the VOR family and TACANs - and where a ring's
/// label goes, the 180th meridian included.
/// </summary>
public sealed class AiracMapDataTests
{
	/// <summary>Every layer has its own name and a tooltip.</summary>
	[Fact]
	public void every_layer_has_its_own_name_and_a_description()
	{
		AiracLayerKind[] kinds = Enum.GetValues<AiracLayerKind>();

		Assert.Equal(kinds.Length, kinds.Select(AiracMapData.Name).Distinct().Count());
		Assert.All(kinds, kind => Assert.False(string.IsNullOrWhiteSpace(AiracMapData.Description(kind))));
		Assert.Equal("Towered Airports", AiracMapData.Name(AiracLayerKind.ToweredAirports));
	}

	/// <summary>Each ring is a polygon followed by its label, and the two are one feature to a Ctrl+click.</summary>
	[Fact]
	public void each_boundary_ring_comes_with_its_label()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.BuildAllSamples();
		List<ArtccBoundaryRing> rings = [.. ArtccBoundaryBuilder.Read(data).Rings.Where(r => r.Points.Count >= 2)];

		IReadOnlyList<MapGeometry> shapes = AiracMapData.ArtccBoundaries(data);

		Assert.NotEmpty(rings);
		Assert.Equal(rings.Count * 2, shapes.Count);

		for (int i = 0; i < rings.Count; i++)
		{
			MapGeometry ring = shapes[i * 2];
			MapGeometry label = shapes[(i * 2) + 1];

			Assert.Equal(MapGeometryKind.Polygon, ring.Kind);
			Assert.Equal(rings[i].Points.Count, ring.Parts[0].Count);
			Assert.Equal(MapGeometryKind.Point, label.Kind);
			Assert.Equal(rings[i].Location.LocationId, label.Label);
			Assert.Same(ring.Feature, label.Feature);
		}
	}

	/// <summary>A field with no tower is left off, and so is a runway with an end that has no position.</summary>
	[Fact]
	public void only_towered_airports_and_their_drawable_runways_are_shown()
	{
		NasrCsvDataCollection data = AirportTestDataBuilder.Build(
			airports:
			[
				AirportTestDataBuilder.Base("CLE", towerTypeCode: "ATCT-TRACON"),
				AirportTestDataBuilder.Base("OCF", towerTypeCode: "NON-ATCT"),
			],
			runways:
			[
				AirportTestDataBuilder.Runway("CLE", "06L/24R", 9000),
				AirportTestDataBuilder.Runway("CLE", "10/28", 6000),
				AirportTestDataBuilder.Runway("OCF", "18/36", 7000),
			],
			runwayEnds:
			[
				AirportTestDataBuilder.RunwayEnd("CLE", "06L/24R", "06L", 41.40, -81.86),
				AirportTestDataBuilder.RunwayEnd("CLE", "06L/24R", "24R", 41.42, -81.83),
				AirportTestDataBuilder.RunwayEnd("CLE", "10/28", "10", null, null),
				AirportTestDataBuilder.RunwayEnd("CLE", "10/28", "28", 41.41, -81.84),
				AirportTestDataBuilder.RunwayEnd("OCF", "18/36", "18", 29.18, -82.22),
				AirportTestDataBuilder.RunwayEnd("OCF", "18/36", "36", 29.16, -82.22),
			]);

		ToweredAirportShapes shapes = AiracMapData.ToweredAirports(data);

		MapGeometry airport = Assert.Single(shapes.Airports);
		Assert.Equal(MapGeometryKind.Point, airport.Kind);
		Assert.Equal("CLE", airport.Label);
		Assert.Equal("Airport", airport.Feature!.Kind);

		MapGeometry runway = Assert.Single(shapes.Runways);
		Assert.Equal(MapGeometryKind.Line, runway.Kind);
		Assert.Equal([new GeoPoint(41.40, -81.86), new GeoPoint(41.42, -81.83)], runway.Parts[0]);
		Assert.Contains(new MapProperty("Runway", "06L/24R"), runway.Feature!.Properties());
	}

	/// <summary>The VOR family and TACANs are drawn; NDBs, DMEs, VOTs and the rest aren't.</summary>
	[Fact]
	public void only_the_vor_family_and_tacans_are_shown()
	{
		IReadOnlyList<MapGeometry> shapes = AiracMapData.Navaids(NavaidTestData.Build(NavaidTestData.AllSampleRows()));

		Assert.All(shapes, s => Assert.Equal(MapGeometryKind.Point, s.Kind));
		Assert.Contains(shapes, s => s.Label == "CGT");
		Assert.Contains(shapes, s => s.Label == "ELY");
		Assert.DoesNotContain(shapes, s => s.Label is "AA" or "ELO" or "OM" or "NY");
		Assert.DoesNotContain(shapes, s => s.Feature!.Properties().Contains(new MapProperty("Type", "VOT")));
	}

	/// <summary>A ring over the 180th meridian is labelled by itself, not on the far side of the world.</summary>
	[Fact]
	public void a_ring_over_the_antimeridian_is_labelled_beside_itself()
	{
		GeoPoint label = AiracMapData.LabelPoint([new GeoPoint(50, 170), new GeoPoint(52, -170), new GeoPoint(54, -170), new GeoPoint(52, 170)]);

		Assert.Equal(52, label.Lat, 6);
		Assert.Equal(180, Math.Abs(label.Lon), 6);
	}
}
