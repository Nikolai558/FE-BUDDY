using System.Text.Encodings.Web;
using System.Text.Json;

namespace FeBuddy.Wpf.Map.Models;

/// <summary>
/// What a Ctrl+click on the map shows about a shape (issue #327): the GeoJSON feature it came from -
/// its geometry type, where it is in the file and its properties - or, for a live AIRAC layer, what
/// the cycle's data says about it.
/// </summary>
/// <remarks>
/// A file's properties are kept as the JSON text the file has, and only read when they are shown,
/// so a file of many thousand features costs one string each. A GeometryCollection's shapes share
/// their feature.
/// </remarks>
public sealed class MapFeature
{
	private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
	private static readonly JsonSerializerOptions Indented = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };

	private readonly string? _propertiesJson;
	private readonly IReadOnlyList<MapProperty>? _properties;

	private MapFeature(string kind, int? number, string? propertiesJson, IReadOnlyList<MapProperty>? properties)
	{
		Kind = kind;
		Number = number;
		_propertiesJson = propertiesJson;
		_properties = properties;
	}

	/// <summary>What the shape is: its GeoJSON geometry type (<c>LineString</c>), or what the data calls it (<c>Airport</c>).</summary>
	public string Kind { get; }

	/// <summary>The feature's place in its file's list of features, counting from 1; <see langword="null"/> when it isn't in one.</summary>
	public int? Number { get; }

	/// <summary>Whether it came from a GeoJSON file, whose properties can be copied as the JSON they are.</summary>
	public bool IsFromFile => _properties is null;

	/// <summary>A GeoJSON feature.</summary>
	/// <param name="geometryType">Its geometry's type, e.g. <c>MultiLineString</c>.</param>
	/// <param name="number">Its place in the file's features, from 1; <see langword="null"/> for none.</param>
	/// <param name="propertiesJson">Its <c>properties</c> object as the file has it; <see langword="null"/> when it has none.</param>
	/// <returns>The feature.</returns>
	public static MapFeature FromGeoJson(string geometryType, int? number, string? propertiesJson) =>
		new(geometryType, number, propertiesJson, null);

	/// <summary>A shape built from data rather than a file (a live AIRAC layer's airport, say).</summary>
	/// <param name="kind">What it is, e.g. <c>Airport</c>.</param>
	/// <param name="properties">What the data says about it. Blank values are left out.</param>
	/// <returns>The feature.</returns>
	public static MapFeature FromData(string kind, IEnumerable<MapProperty> properties) =>
		new(kind, null, null, [.. properties.Where(p => !string.IsNullOrWhiteSpace(p.Value))]);

	/// <summary>
	/// Its properties, in the file's order, each value as text: a string as it is, a number, true,
	/// false or null as the file writes it, and an object or array as one line of JSON.
	/// </summary>
	/// <returns>The properties; none when there are none, or the file's can't be read.</returns>
	public IReadOnlyList<MapProperty> Properties()
	{
		if (_properties is not null)
		{
			return _properties;
		}

		if (_propertiesJson is null)
		{
			return [];
		}

		try
		{
			using JsonDocument doc = JsonDocument.Parse(_propertiesJson);
			return doc.RootElement.ValueKind == JsonValueKind.Object
				? [.. doc.RootElement.EnumerateObject().Select(p => new MapProperty(p.Name, Show(p.Value)))]
				: [];
		}
		catch (JsonException)
		{
			return [];
		}
	}

	/// <summary>
	/// What the copy button puts on the clipboard: a file's <c>properties</c> object as indented JSON,
	/// ready to paste into a file, or one <c>Name: value</c> line per property.
	/// </summary>
	/// <returns>The text; empty when there are no properties.</returns>
	public string CopyText()
	{
		if (IsFromFile)
		{
			if (_propertiesJson is null)
			{
				return string.Empty;
			}

			try
			{
				using JsonDocument doc = JsonDocument.Parse(_propertiesJson);
				return JsonSerializer.Serialize(doc.RootElement, Indented);
			}
			catch (JsonException)
			{
				return _propertiesJson;
			}
		}

		return string.Join(Environment.NewLine, _properties!.Select(p => $"{p.Name}: {p.Value}"));
	}

	private static string Show(JsonElement value) => value.ValueKind switch
	{
		JsonValueKind.String => value.GetString()!,
		JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Serialize(value, Compact),
		_ => value.GetRawText(),
	};
}

/// <summary>One property of a <see cref="MapFeature"/>.</summary>
/// <param name="Name">Its name, e.g. <c>style</c> or <c>FAA ID</c>.</param>
/// <param name="Value">Its value, as text.</param>
public readonly record struct MapProperty(string Name, string Value);
