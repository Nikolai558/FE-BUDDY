using System.Text.Json;

using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Domain.Airways.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Covers which <c>feb.*</c> properties land on which Airways Feature: <c>feb.awyId</c> as the
/// airway's own ID on Lines and as every sharing airway on Symbols and Text,
/// <c>feb.pointId</c> on Symbols only, and <c>feb.waypoints</c> on Lines only.
/// </summary>
public sealed class AirwayGeojsonFebPropertiesTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AwyFeb_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	[Fact]
	public void lines_carry_the_airway_id_and_ordered_waypoints_when_selected()
	{
		Dictionary<string, JsonElement> lines = PropertiesById(
			Generate(true, AirwayFebProperty.AwyId, AirwayFebProperty.PointId, AirwayFebProperty.Waypoints), "Lines", "feb.awyId");

		JsonElement j2 = lines["J2"];
		Assert.Equal(JsonValueKind.String, j2.GetProperty("feb.awyId").ValueKind);
		Assert.Equal(new[] { "BBBBB", "CCCCC" }, Strings(j2.GetProperty("feb.waypoints")));
		Assert.False(j2.TryGetProperty("feb.pointId", out _));
		Assert.Equal(new[] { "AAAAA", "BBBBB" }, Strings(lines["J1"].GetProperty("feb.waypoints")));
	}

	[Fact]
	public void lines_leave_out_the_waypoints_when_not_selected()
	{
		Dictionary<string, JsonElement> lines = PropertiesById(
			Generate(true, AirwayFebProperty.AwyId), "Lines", "feb.awyId");

		Assert.Equal(2, lines.Count);
		Assert.All(lines.Values, properties => Assert.False(properties.TryGetProperty("feb.waypoints", out _)));
	}

	[Fact]
	public void symbols_list_every_airway_sharing_the_point_and_the_point_id()
	{
		Dictionary<string, JsonElement> symbols = PropertiesById(
			Generate(true, AirwayFebProperty.AwyId, AirwayFebProperty.PointId, AirwayFebProperty.Waypoints), "Symbols", "feb.pointId");

		Assert.Equal(new[] { "J1", "J2" }, Strings(symbols["BBBBB"].GetProperty("feb.awyId")));
		Assert.Equal(new[] { "J1" }, Strings(symbols["AAAAA"].GetProperty("feb.awyId")));
		Assert.Equal(new[] { "J2" }, Strings(symbols["CCCCC"].GetProperty("feb.awyId")));
		Assert.All(symbols.Values, properties => Assert.False(properties.TryGetProperty("feb.waypoints", out _)));
	}

	[Fact]
	public void text_lists_every_airway_sharing_the_point_and_no_point_id()
	{
		string directory = Generate(true, AirwayFebProperty.AwyId, AirwayFebProperty.PointId, AirwayFebProperty.Waypoints);
		List<JsonElement> text = Features(directory, "Text");

		JsonElement shared = Assert.Single(text, properties => Strings(properties.GetProperty("text"))[0] == "BBBBB");
		Assert.Equal(new[] { "J1", "J2" }, Strings(shared.GetProperty("feb.awyId")));
		Assert.All(text, properties =>
		{
			Assert.False(properties.TryGetProperty("feb.pointId", out _));
			Assert.False(properties.TryGetProperty("feb.waypoints", out _));
		});
	}

	[Fact]
	public void nothing_feb_is_written_when_feb_properties_are_off()
	{
		string directory = Generate(false, AirwayFebProperty.AwyId, AirwayFebProperty.PointId, AirwayFebProperty.Waypoints);

		foreach (string kind in new[] { "Lines", "Symbols", "Text" })
		{
			Assert.All(Features(directory, kind), properties =>
				Assert.DoesNotContain(properties.EnumerateObject(), property => property.Name.StartsWith("feb.", StringComparison.Ordinal)));
		}
	}

	/// <summary>
	/// Writes two High airways that share <c>BBBBB</c> (J1: AAAAA-BBBBB, J2: BBBBB-CCCCC)
	/// into one HighLow group and returns the directory the files landed in.
	/// </summary>
	private string Generate(bool includeFeb, params AirwayFebProperty[] febProperties)
	{
		AirwayPoint a = new("AAAAA", "WP", 40.0, -80.0, "fix");
		AirwayPoint b = new("BBBBB", "WP", 41.0, -81.0, "fix");
		AirwayPoint c = new("CCCCC", "WP", 42.0, -82.0, "fix");

		AirwaySettings settings = new()
		{
			OutputDirectory = _outputDirectory,
			OutputBy = AirwayGeojsonOutputBy.HighLow,
			BufferAirwayWaypoints = false,
			IncludeFebCustomProperties = includeFeb,
			FebProperties = febProperties,
			GenerateAliasFile = false,
			SplitAtAntimeridian = true,
			IncludeCrcLineDefaults = false,
			IncludeCrcSymbolDefaults = false,
			IncludeCrcTextDefaults = false,
			Roi = null,
		};

		AirwayGeojsonGenerateResult result = AirwayGeojsonWriter.Generate(
			new[] { BuildAirway("J2", b, c), BuildAirway("J1", a, b) }, settings);

		Assert.Equal(3, result.FilesWritten.Count);
		return Path.GetDirectoryName(result.FilesWritten[0])!;
	}

	private static Airway BuildAirway(string awyId, AirwayPoint from, AirwayPoint to) => new()
	{
		AwyId = awyId,
		Designation = "J",
		AwyLocation = "C",
		AltitudeClass = AirwayAltitudeClass.High,
		Segments = new[] { new AirwaySegment(from.PointId, to.PointId, IsGap: false, MaxAuthAlt: null) },
		Points = new[] { from, to },
		Geometry = AirwayGeometryBuilder.GeometryFactory.CreateLineString(new[]
		{
			new Coordinate(from.Longitude, from.Latitude),
			new Coordinate(to.Longitude, to.Latitude),
		}),
	};

	/// <summary>Reads the <c>properties</c> object of every Feature in <c>Airways_High_&lt;kind&gt;.geojson</c>.</summary>
	private static List<JsonElement> Features(string directory, string kind)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, $"Airways_High_{kind}.geojson")));

		return document.RootElement.GetProperty("features").EnumerateArray()
			.Select(feature => feature.GetProperty("properties").Clone())
			.ToList();
	}

	/// <summary>
	/// Reads the Features of one file keyed by a string property, or by the first entry of an
	/// array property.
	/// </summary>
	private static Dictionary<string, JsonElement> PropertiesById(string directory, string kind, string keyProperty) =>
		Features(directory, kind).ToDictionary(properties =>
		{
			JsonElement key = properties.GetProperty(keyProperty);
			return key.ValueKind == JsonValueKind.Array ? key[0].GetString()! : key.GetString()!;
		});

	private static string[] Strings(JsonElement array) =>
		array.EnumerateArray().Select(item => item.GetString()!).ToArray();
}
