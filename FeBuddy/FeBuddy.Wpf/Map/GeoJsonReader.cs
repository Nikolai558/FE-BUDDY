using System.Text.Json;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// A small, forgiving GeoJSON reader built on <see cref="System.Text.Json"/> -
/// no NuGet dependency. It only cares about what the map draws: geometry, plus a vNAS text
/// feature's <c>text</c> lines so they can be drawn as labels. Other properties are ignored.
/// <para>
/// Handles FeatureCollection / Feature / bare geometry, GeometryCollection, and
/// all six primitive types. Coordinates are read as <c>[lon, lat]</c> per the
/// spec; extra ordinates (elevation) are ignored. CRC's <c>isLineDefaults</c> /
/// <c>isSymbolDefaults</c> / <c>isTextDefaults</c> features (parked at latitude 180) are
/// skipped - they carry settings, not map data.
/// </para>
/// <para>
/// A coordinate that is not a pair of numbers, or is off the globe, is dropped, and so is a line
/// or ring left with too few points to draw, and a geometry left with nothing - so a file whose
/// coordinates are all unusable is reported as such rather than loading as nothing.
/// </para>
/// </summary>
public static class GeoJsonReader
{
	private static readonly string[] DefaultsFlags = ["isLineDefaults", "isSymbolDefaults", "isTextDefaults"];

	/// <summary>Parses <paramref name="json"/> into a flat list of map geometries.</summary>
	/// <param name="json">The GeoJSON text.</param>
	/// <returns>Every Point, LineString and Polygon geometry found, in file order.</returns>
	/// <exception cref="FormatException">
	/// The text is not usable GeoJSON. Its message is short enough to show beside the file - why it
	/// cannot be drawn - with any detail in the inner exception.
	/// </exception>
	public static IReadOnlyList<MapGeometry> Read(string json)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(json);
		}
		catch (JsonException ex)
		{
			throw new FormatException("Not valid JSON", ex);
		}

		using (doc)
		{
			var result = new List<MapGeometry>();
			ReadState state = new();

			try
			{
				ReadNode(doc.RootElement, label: null, result, state);
			}
			catch (InvalidOperationException ex)
			{
				throw new FormatException("Not laid out as GeoJSON", ex);
			}

			if (result.Count == 0)
			{
				// Coordinates that were all off the globe are most likely in another projection
				// (metres, say), which the map cannot place.
				throw new FormatException(state.DroppedPoints > 0
					? "Coordinates are not longitude and latitude"
					: "No map features in this file");
			}

			return result;
		}
	}

	private static void ReadNode(JsonElement node, string? label, List<MapGeometry> into, ReadState state)
	{
		if (node.ValueKind != JsonValueKind.Object
			|| !node.TryGetProperty("type", out var typeProp)
			|| typeProp.ValueKind != JsonValueKind.String)
		{
			return;
		}

		switch (typeProp.GetString())
		{
			case "FeatureCollection":
				if (node.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array)
				{
					foreach (var feature in features.EnumerateArray())
					{
						ReadNode(feature, label: null, into, state);
					}
				}

				break;

			case "Feature":
				JsonElement properties = node.TryGetProperty("properties", out var p) ? p : default;
				if (IsDefaultsFeature(properties))
				{
					break;
				}

				if (node.TryGetProperty("geometry", out var geometry))
				{
					ReadNode(geometry, ReadLabel(properties), into, state);
				}

				break;

			case "GeometryCollection":
				if (node.TryGetProperty("geometries", out var geometries) && geometries.ValueKind == JsonValueKind.Array)
				{
					foreach (var g in geometries.EnumerateArray())
					{
						ReadNode(g, label, into, state);
					}
				}

				break;

			default:
				var parsed = ReadGeometry(typeProp.GetString(), node, label, state);
				if (parsed is not null)
				{
					into.Add(parsed);
				}

				break;
		}
	}

	private static bool IsDefaultsFeature(JsonElement properties) =>
		properties.ValueKind == JsonValueKind.Object
		&& DefaultsFlags.Any(flag => properties.TryGetProperty(flag, out var value) && value.ValueKind == JsonValueKind.True);

	/// <summary>A vNAS text feature's <c>text</c> (an array of lines, or a plain string), or null.</summary>
	private static string? ReadLabel(JsonElement properties)
	{
		if (properties.ValueKind != JsonValueKind.Object || !properties.TryGetProperty("text", out var text))
		{
			return null;
		}

		return text.ValueKind switch
		{
			JsonValueKind.String => text.GetString(),
			JsonValueKind.Array => string.Join('\n', text.EnumerateArray()
				.Where(line => line.ValueKind == JsonValueKind.String)
				.Select(line => line.GetString())),
			_ => null,
		};
	}

	/// <summary>One geometry, or <see langword="null"/> when nothing in it can be drawn.</summary>
	private static MapGeometry? ReadGeometry(string? type, JsonElement node, string? label, ReadState state)
	{
		if (!node.TryGetProperty("coordinates", out var coords))
		{
			return null;
		}

		switch (type)
		{
			case "Point":
				return ReadPoint(coords, state) is { } point
					? new MapGeometry(MapGeometryKind.Point, [[point]], label)
					: null;

			case "MultiPoint":
				List<IReadOnlyList<GeoPoint>> points = [];
				foreach (JsonElement p in Items(coords))
				{
					if (ReadPoint(p, state) is { } one)
					{
						points.Add([one]);
					}
				}

				return points.Count > 0 ? new MapGeometry(MapGeometryKind.Point, points, label) : null;

			case "LineString":
				return Build(MapGeometryKind.Line, [ReadRun(coords, state)]);

			case "MultiLineString":
				return Build(MapGeometryKind.Line, [.. Items(coords).Select(line => ReadRun(line, state))]);

			case "Polygon":
				return Build(MapGeometryKind.Polygon, [.. Items(coords).Select(ring => ReadRun(ring, state))]);

			case "MultiPolygon":
				return Build(MapGeometryKind.Polygon, [.. Items(coords).SelectMany(Items).Select(ring => ReadRun(ring, state))]);

			default:
				return null;
		}
	}

	/// <summary>
	/// A line or polygon from the runs that can be drawn - at least two points for a line, three
	/// for a ring - or <see langword="null"/> when none can.
	/// </summary>
	private static MapGeometry? Build(MapGeometryKind kind, List<IReadOnlyList<GeoPoint>> runs)
	{
		int fewest = kind == MapGeometryKind.Polygon ? 3 : 2;
		List<IReadOnlyList<GeoPoint>> drawable = [.. runs.Where(run => run.Count >= fewest)];

		return drawable.Count > 0 ? new MapGeometry(kind, drawable) : null;
	}

	private static IReadOnlyList<GeoPoint> ReadRun(JsonElement array, ReadState state) =>
		[.. Items(array).Select(p => ReadPoint(p, state)).OfType<GeoPoint>()];

	/// <summary>An array's items; nothing when it is not an array.</summary>
	private static IEnumerable<JsonElement> Items(JsonElement array) =>
		array.ValueKind == JsonValueKind.Array ? array.EnumerateArray() : [];

	/// <summary>
	/// Reads <c>[lon, lat, (elevation...)]</c>. Anything else - not two numbers, a latitude past ±90,
	/// a longitude that is not a number - is dropped and counted.
	/// </summary>
	private static GeoPoint? ReadPoint(JsonElement pair, ReadState state)
	{
		if (pair.ValueKind == JsonValueKind.Array
			&& pair.GetArrayLength() >= 2
			&& pair[0].ValueKind == JsonValueKind.Number
			&& pair[1].ValueKind == JsonValueKind.Number
			&& pair[0].TryGetDouble(out double lon)
			&& pair[1].TryGetDouble(out double lat)
			&& lat is >= -90.0 and <= 90.0
			&& double.IsFinite(lon))
		{
			return new GeoPoint(lat, lon);
		}

		state.DroppedPoints++;
		return null;
	}

	/// <summary>What one read has dropped along the way, to explain an empty result.</summary>
	private sealed class ReadState
	{
		public int DroppedPoints { get; set; }
	}
}
