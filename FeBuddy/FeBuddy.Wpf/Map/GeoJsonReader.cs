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
/// </summary>
public static class GeoJsonReader
{
	private static readonly string[] DefaultsFlags = ["isLineDefaults", "isSymbolDefaults", "isTextDefaults"];

	/// <summary>Parses <paramref name="json"/> into a flat list of map geometries.</summary>
	/// <param name="json">The GeoJSON text.</param>
	/// <returns>Every Point, LineString and Polygon geometry found, in file order.</returns>
	/// <exception cref="FormatException">The text is not usable GeoJSON.</exception>
	public static IReadOnlyList<MapGeometry> Read(string json)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(json);
		}
		catch (JsonException ex)
		{
			throw new FormatException("File is not valid JSON: " + ex.Message, ex);
		}

		using (doc)
		{
			var result = new List<MapGeometry>();
			try
			{
				ReadNode(doc.RootElement, label: null, result);
			}
			catch (InvalidOperationException ex)
			{
				throw new FormatException("A coordinate is not a pair of numbers: " + ex.Message, ex);
			}

			if (result.Count == 0)
			{
				throw new FormatException("No Point, LineString or Polygon geometry was found.");
			}

			return result;
		}
	}

	private static void ReadNode(JsonElement node, string? label, List<MapGeometry> into)
	{
		if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("type", out var typeProp))
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
						ReadNode(feature, label: null, into);
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
					ReadNode(geometry, ReadLabel(properties), into);
				}

				break;

			case "GeometryCollection":
				if (node.TryGetProperty("geometries", out var geometries) && geometries.ValueKind == JsonValueKind.Array)
				{
					foreach (var g in geometries.EnumerateArray())
					{
						ReadNode(g, label, into);
					}
				}

				break;

			default:
				var parsed = ReadGeometry(typeProp.GetString(), node, label);
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

	private static MapGeometry? ReadGeometry(string? type, JsonElement node, string? label)
	{
		if (!node.TryGetProperty("coordinates", out var coords))
		{
			return null;
		}

		switch (type)
		{
			case "Point":
				return ReadPoint(coords) is { } point
					? new MapGeometry(MapGeometryKind.Point, [[point]], label)
					: null;

			case "MultiPoint":
				List<IReadOnlyList<GeoPoint>> points = [];
				foreach (var p in coords.EnumerateArray())
				{
					if (ReadPoint(p) is { } one)
					{
						points.Add([one]);
					}
				}

				return points.Count > 0 ? new MapGeometry(MapGeometryKind.Point, points, label) : null;

			case "LineString":
				return new MapGeometry(MapGeometryKind.Line, [ReadRun(coords)]);

			case "MultiLineString":
				return new MapGeometry(MapGeometryKind.Line,
					[.. coords.EnumerateArray().Select(ReadRun)]);

			case "Polygon":
				return new MapGeometry(MapGeometryKind.Polygon,
					[.. coords.EnumerateArray().Select(ReadRun)]);

			case "MultiPolygon":
				var rings = new List<IReadOnlyList<GeoPoint>>();
				foreach (var polygon in coords.EnumerateArray())
				{
					foreach (var ring in polygon.EnumerateArray())
					{
						rings.Add(ReadRun(ring));
					}
				}

				return new MapGeometry(MapGeometryKind.Polygon, rings);

			default:
				return null;
		}
	}

	private static IReadOnlyList<GeoPoint> ReadRun(JsonElement array)
	{
		var run = new List<GeoPoint>();
		foreach (var p in array.EnumerateArray())
		{
			if (ReadPoint(p) is { } point)
			{
				run.Add(point);
			}
		}

		return run;
	}

	/// <summary>Reads <c>[lon, lat, (elevation...)]</c>; a point off the planet (lat past ±90) is dropped.</summary>
	private static GeoPoint? ReadPoint(JsonElement pair)
	{
		var e = pair.EnumerateArray();
		e.MoveNext();
		var lon = e.Current.GetDouble();
		e.MoveNext();
		var lat = e.Current.GetDouble();
		return lat is >= -90.0 and <= 90.0 && double.IsFinite(lon) ? new GeoPoint(lat, lon) : null;
	}
}
