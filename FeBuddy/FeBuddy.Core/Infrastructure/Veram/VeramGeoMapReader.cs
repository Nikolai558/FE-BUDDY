using System.Globalization;
using System.Xml;

using FeBuddy.Core.Infrastructure.Veram.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Veram;

/// <summary>
/// Reads a vERAM GeoMaps XML file (a <c>GeoMapSet</c>): its maps, their objects with their
/// Line / Symbol / Text defaults, and every element with its own overrides.
/// </summary>
/// <remarks>
/// <para>
/// The layout is <c>GeoMapSet / GeoMaps / GeoMap / Objects / GeoMapObject</c>, each object holding
/// optional <c>LineDefaults</c>, <c>SymbolDefaults</c> and <c>TextDefaults</c> and an
/// <c>Elements</c> list whose <c>Element</c>s are told apart by <c>xsi:type</c>:
/// </para>
/// <code>
/// &lt;GeoMapObject Description="ZXX BOUNDARY" TdmOnly="false"&gt;
///   &lt;LineDefaults Bcg="1" Filters="1" Style="Solid" Thickness="1" /&gt;
///   &lt;Elements&gt;
///     &lt;Element xsi:type="Line" Filters="" StartLat="40.1" StartLon="-100.2" EndLat="40.2" EndLon="-100.1" /&gt;
///     &lt;Element xsi:type="Text" Filters="" Lat="40.1" Lon="-100.2" Lines="ZXX" /&gt;
/// </code>
/// <para>
/// The file is streamed rather than loaded whole, as GeoMaps files can be large. An element or
/// value that cannot be used is reported in <see cref="VeramGeoMapFile.Problems"/> and skipped, so
/// one bad element never loses the rest of the file; a file that is not well-formed XML, or not a
/// GeoMapSet at all, throws <see cref="InvalidDataException"/>.
/// </para>
/// </remarks>
public static class VeramGeoMapReader
{
	private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

	/// <summary>Reads a GeoMaps file from disk.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's maps and any elements that could not be used.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be read.</exception>
	/// <exception cref="InvalidDataException">Thrown when the file is not well-formed XML or not a GeoMapSet.</exception>
	public static VeramGeoMapFile Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		return Parse(stream, path);
	}

	/// <summary>Reads GeoMaps XML from a stream.</summary>
	/// <param name="stream">The XML.</param>
	/// <param name="sourcePath">What to call the source in the result, usually its path.</param>
	/// <returns>The content's maps and any elements that could not be used.</returns>
	/// <exception cref="InvalidDataException">Thrown when the content is not well-formed XML or not a GeoMapSet.</exception>
	public static VeramGeoMapFile Parse(Stream stream, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(stream);

		try
		{
			using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
			{
				IgnoreComments = true,
				IgnoreWhitespace = true,
				DtdProcessing = DtdProcessing.Prohibit,
			});

			return ReadGeoMapSet(reader, sourcePath);
		}
		catch (XmlException ex)
		{
			throw new InvalidDataException($"{Path.GetFileName(sourcePath)} is not well-formed XML: {ex.Message}", ex);
		}
	}

	private static VeramGeoMapFile ReadGeoMapSet(XmlReader reader, string sourcePath)
	{
		reader.MoveToContent();

		// MoveToContent lands on the root element (or throws), so its name is all there is to check.
		if (reader.LocalName != "GeoMapSet")
		{
			throw new InvalidDataException(
				$"{Path.GetFileName(sourcePath)} is not a vERAM GeoMaps file: it starts with <{reader.LocalName}>, not <GeoMapSet>.");
		}

		List<VeramGeoMap> maps = [];
		List<string> problems = [];
		MapBuilder? map = null;
		ObjectBuilder? mapObject = null;

		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.EndElement)
			{
				switch (reader.LocalName)
				{
					// An object is only ever started inside a map, so the map is there to add it to.
					case "GeoMapObject" when mapObject is not null:
						map!.Objects.Add(mapObject.Build());
						mapObject = null;
						break;

					case "GeoMap" when map is not null:
						maps.Add(map.Build());
						map = null;
						break;
				}

				continue;
			}

			if (reader.NodeType != XmlNodeType.Element)
			{
				continue;
			}

			int line = ((IXmlLineInfo)reader).LineNumber;

			switch (reader.LocalName)
			{
				case "GeoMap":
					map = new MapBuilder(reader.GetAttribute("Name") ?? string.Empty);

					// A map with no content closes itself: <GeoMap ... />.
					if (reader.IsEmptyElement)
					{
						maps.Add(map.Build());
						map = null;
					}

					break;

				case "GeoMapObject" when map is not null:
					mapObject = new ObjectBuilder(
						reader.GetAttribute("Description") ?? string.Empty,
						string.Equals(reader.GetAttribute("TdmOnly")?.Trim(), "true", StringComparison.OrdinalIgnoreCase));

					// An object with no content closes itself: <GeoMapObject ... />.
					if (reader.IsEmptyElement)
					{
						map.Objects.Add(mapObject.Build());
						mapObject = null;
					}

					break;

				case "LineDefaults" when mapObject is not null:
					mapObject.LineDefaults = ReadProperties(reader, line, problems);
					break;

				case "SymbolDefaults" when mapObject is not null:
					mapObject.SymbolDefaults = ReadProperties(reader, line, problems);
					break;

				case "TextDefaults" when mapObject is not null:
					mapObject.TextDefaults = ReadProperties(reader, line, problems);
					break;

				case "Element" when mapObject is not null:
					if (ReadElement(reader, line, problems) is { } element)
					{
						mapObject.Elements.Add(element);
					}

					break;
			}
		}

		return new VeramGeoMapFile(sourcePath, maps, problems);
	}

	private static VeramElement? ReadElement(XmlReader reader, int line, List<string> problems)
	{
		string? type = reader.GetAttribute("type", XsiNamespace);

		if (!Enum.TryParse(type, ignoreCase: true, out VeramElementKind kind) || !Enum.IsDefined(kind))
		{
			problems.Add($"Line {line}: an Element of type '{type}' is not a Line, Symbol or Text and was skipped.");
			return null;
		}

		VeramProperties overrides = ReadProperties(reader, line, problems);

		if (kind == VeramElementKind.Line)
		{
			if (TryReadCoordinate(reader, "StartLat", "StartLon", out Coordinate start)
				&& TryReadCoordinate(reader, "EndLat", "EndLon", out Coordinate end))
			{
				return new VeramElement(kind, start, end, null, overrides);
			}
		}
		else if (TryReadCoordinate(reader, "Lat", "Lon", out Coordinate position))
		{
			return new VeramElement(kind, position, null, kind == VeramElementKind.Text ? reader.GetAttribute("Lines") ?? string.Empty : null, overrides);
		}

		problems.Add($"Line {line}: a {kind} Element has a missing or invalid position and was skipped.");
		return null;
	}

	/// <summary>Reads a latitude and longitude attribute pair, wrapping the longitude into -180 to 180.</summary>
	private static bool TryReadCoordinate(XmlReader reader, string latName, string lonName, out Coordinate coordinate)
	{
		coordinate = null!;

		if (!TryReadDouble(reader.GetAttribute(latName), out double lat)
			|| !TryReadDouble(reader.GetAttribute(lonName), out double lon)
			|| lat is < -90 or > 90
			|| lon is < -540 or > 540)
		{
			return false;
		}

		// vERAM tolerates longitudes past ±180 (e.g. 190 for -170); GeoJSON does not.
		lon = (lon % 360 + 540) % 360 - 180;
		coordinate = new Coordinate(lon, lat);
		return true;
	}

	/// <summary>
	/// Reads the display properties on the current element. A value that is present but not a
	/// number (or true/false) is reported and left out; the rest are kept.
	/// </summary>
	private static VeramProperties ReadProperties(XmlReader reader, int line, List<string> problems)
	{
		int? ReadInt(string name)
		{
			string? raw = reader.GetAttribute(name);

			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
			{
				return value;
			}

			problems.Add($"Line {line}: {name}=\"{raw}\" is not a whole number and was ignored.");
			return null;
		}

		bool? ReadBool(string name)
		{
			string? raw = reader.GetAttribute(name);

			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			if (bool.TryParse(raw.Trim(), out bool value))
			{
				return value;
			}

			problems.Add($"Line {line}: {name}=\"{raw}\" is not true or false and was ignored.");
			return null;
		}

		IReadOnlyList<int>? ReadFilters()
		{
			string? raw = reader.GetAttribute("Filters");

			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			List<int> filters = [];

			foreach (string entry in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (!int.TryParse(entry, NumberStyles.Integer, CultureInfo.InvariantCulture, out int filter))
				{
					problems.Add($"Line {line}: Filters=\"{raw}\" is not a list of whole numbers and was ignored.");
					return null;
				}

				if (!filters.Contains(filter))
				{
					filters.Add(filter);
				}
			}

			return filters.Count > 0 ? filters : null;
		}

		string? style = reader.GetAttribute("Style");

		VeramProperties properties = new()
		{
			Bcg = ReadInt("Bcg"),
			Filters = ReadFilters(),
			Style = string.IsNullOrWhiteSpace(style) ? null : style.Trim(),
			Thickness = ReadInt("Thickness"),
			Size = ReadInt("Size"),
			Underline = ReadBool("Underline"),
			Opaque = ReadBool("Opaque"),
			XOffset = ReadInt("XOffset"),
			YOffset = ReadInt("YOffset"),
		};

		return properties.IsEmpty ? VeramProperties.None : properties;
	}

	private static bool TryReadDouble(string? raw, out double value)
	{
		value = 0;
		return raw is not null && double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	/// <summary>A GeoMap being read, until its end tag.</summary>
	private sealed class MapBuilder(string name)
	{
		public List<VeramGeoMapObject> Objects { get; } = [];

		public VeramGeoMap Build() => new(name, Objects);
	}

	/// <summary>A GeoMapObject being read, until its end tag.</summary>
	private sealed class ObjectBuilder(string description, bool tdmOnly)
	{
		public VeramProperties? LineDefaults { get; set; }

		public VeramProperties? SymbolDefaults { get; set; }

		public VeramProperties? TextDefaults { get; set; }

		public List<VeramElement> Elements { get; } = [];

		public VeramGeoMapObject Build() => new(description, tdmOnly, LineDefaults, SymbolDefaults, TextDefaults, Elements);
	}
}
