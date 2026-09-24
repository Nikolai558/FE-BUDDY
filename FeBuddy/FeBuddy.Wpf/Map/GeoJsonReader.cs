using System.Text.Json;

namespace FeBuddy.Wpf.Map;

/// <summary>
/// A small, forgiving GeoJSON reader built on <see cref="System.Text.Json"/> -
/// no NuGet dependency. It only cares about geometry (the map is a viewer), so
/// feature properties are ignored.
/// <para>
/// Handles FeatureCollection / Feature / bare geometry, GeometryCollection, and
/// all six primitive types. Coordinates are read as <c>[lon, lat]</c> per the
/// spec; extra ordinates (elevation) are ignored.
/// </para>
/// </summary>
public static class GeoJsonReader
{
	/// <summary>Parses <paramref name="json"/> into a flat list of map geometries.</summary>
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
			ReadNode(doc.RootElement, result);
			if (result.Count == 0)
			{
				throw new FormatException("No Point, LineString or Polygon geometry was found.");
			}

			return result;
		}
	}

	private static void ReadNode(JsonElement node, List<MapGeometry> into)
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
						ReadNode(feature, into);
					}
				}

				break;

			case "Feature":
				if (node.TryGetProperty("geometry", out var geometry))
				{
					ReadNode(geometry, into);
				}

				break;

			case "GeometryCollection":
				if (node.TryGetProperty("geometries", out var geometries) && geometries.ValueKind == JsonValueKind.Array)
				{
					foreach (var g in geometries.EnumerateArray())
					{
						ReadNode(g, into);
					}
				}

				break;

			default:
				var parsed = ReadGeometry(typeProp.GetString(), node);
				if (parsed is not null)
				{
					into.Add(parsed);
				}

				break;
		}
	}

	private static MapGeometry? ReadGeometry(string? type, JsonElement node)
	{
		if (!node.TryGetProperty("coordinates", out var coords))
		{
			return null;
		}

		switch (type)
		{
			case "Point":
				return new MapGeometry(MapGeometryKind.Point, [[ReadPoint(coords)]]);

			case "MultiPoint":
				return new MapGeometry(MapGeometryKind.Point,
					[.. coords.EnumerateArray().Select(p => (IReadOnlyList<GeoPoint>)[ReadPoint(p)])]);

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
			run.Add(ReadPoint(p));
		}

		return run;
	}

	private static GeoPoint ReadPoint(JsonElement pair)
	{
		// [lon, lat, (elevation...)]
		var e = pair.EnumerateArray();
		e.MoveNext();
		var lon = e.Current.GetDouble();
		e.MoveNext();
		var lat = e.Current.GetDouble();
		return new GeoPoint(lat, lon);
	}
}
