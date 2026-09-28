using System.Globalization;
using System.Xml;
using System.Xml.Linq;

using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Eram;

/// <summary>
/// Reads an ERAM <c>Geomaps.xml</c> (the <c>Geomaps_Records</c> file of an ERAM adaptation
/// export): its maps, their objects with their Line / Symbol / Text defaults, and every line,
/// symbol, text and SAA with its own overrides.
/// </summary>
/// <remarks>
/// <para>
/// The layout is <c>Geomaps_Records / GeoMapRecord / GeoMapObjectType</c>, each object holding
/// optional <c>DefaultLineProperties</c>, <c>DefaultSymbolProperties</c> and
/// <c>TextDefaultProperties</c>, then its <c>GeoMapLine</c>, <c>GeoMapSymbol</c>,
/// <c>GeoMapText</c> and <c>GeoMapSaa</c> elements. Values are child elements, not attributes:
/// </para>
/// <code>
/// &lt;GeoMapObjectType&gt;
///   &lt;MapObjectType&gt;SECTOR&lt;/MapObjectType&gt;
///   &lt;MapGroupId&gt;4&lt;/MapGroupId&gt;
///   &lt;DefaultLineProperties&gt;
///     &lt;LineStyle&gt;Solid&lt;/LineStyle&gt; &lt;BCGGroup&gt;3&lt;/BCGGroup&gt; &lt;Thickness&gt;1&lt;/Thickness&gt;
///     &lt;GeoLineFilters&gt;&lt;FilterGroup&gt;3&lt;/FilterGroup&gt;&lt;/GeoLineFilters&gt;
///   &lt;/DefaultLineProperties&gt;
///   &lt;GeoMapLine&gt;
///     &lt;LineObjectId&gt;ZXX01&lt;/LineObjectId&gt;
///     &lt;StartLatitude&gt;40300000N&lt;/StartLatitude&gt; &lt;StartLongitude&gt;100150000W&lt;/StartLongitude&gt; ...
/// </code>
/// <para>
/// Positions are <c>ddmmssnn</c> + <c>N</c>/<c>S</c> and <c>dddmmssnn</c> + <c>E</c>/<c>W</c>
/// (<c>nn</c> is hundredths of a second); the <c>…Spherical</c> copies are not read. A seconds value
/// of <c>60</c> is accepted, as the FAA writes it where a value rounds up.
/// </para>
/// <para>
/// A symbol's own <c>GeoMapText</c> becomes a Text element at the symbol (or where it says). An
/// SAA's boundary segments become Line elements and its label a Text element, in the SAA's object.
/// </para>
/// <para>
/// The file is streamed one object at a time rather than loaded whole, as it can run to hundreds
/// of megabytes. An element or value that cannot be used is reported in
/// <see cref="EramGeoMapFile.Problems"/> and skipped, so one bad element never loses the rest of
/// the file; a file that is not well-formed XML, or not a <c>Geomaps_Records</c> file at all,
/// throws <see cref="InvalidDataException"/>.
/// </para>
/// </remarks>
public static class EramGeoMapReader
{
	/// <summary>The root element of an ERAM <c>Geomaps.xml</c>.</summary>
	public const string RootElement = "Geomaps_Records";

	/// <summary>Reads a Geomaps file from disk.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's maps and any elements that could not be used.</returns>
	/// <exception cref="IOException">Thrown when the file cannot be read.</exception>
	/// <exception cref="InvalidDataException">Thrown when the file is not well-formed XML or not a Geomaps file.</exception>
	public static EramGeoMapFile Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using FileStream stream = File.OpenRead(path);
		return Parse(stream, path);
	}

	/// <summary>
	/// Whether a file is an ERAM Geomaps file, from its first element alone. Used to pick
	/// <c>Geomaps.xml</c> out of a folder that holds the rest of an adaptation export too.
	/// </summary>
	/// <param name="path">The file to look at.</param>
	/// <returns><see langword="true"/> when it starts with <c>&lt;Geomaps_Records&gt;</c>; <see langword="false"/> otherwise, or when it cannot be read.</returns>
	public static bool IsGeoMapsFile(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			using XmlReader reader = XmlReader.Create(stream, Settings);

			// MoveToContent lands on the root element or throws, so its name is all there is to check.
			reader.MoveToContent();
			return reader.LocalName == RootElement;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
		{
			return false;
		}
	}

	/// <summary>Reads Geomaps XML from a stream.</summary>
	/// <param name="stream">The XML.</param>
	/// <param name="sourcePath">What to call the source in the result, usually its path.</param>
	/// <returns>The content's maps and any elements that could not be used.</returns>
	/// <exception cref="InvalidDataException">Thrown when the content is not well-formed XML or not a Geomaps file.</exception>
	public static EramGeoMapFile Parse(Stream stream, string sourcePath)
	{
		ArgumentNullException.ThrowIfNull(stream);

		try
		{
			using XmlReader reader = XmlReader.Create(stream, Settings);
			return ReadRecords(reader, sourcePath);
		}
		catch (XmlException ex)
		{
			throw new InvalidDataException($"{Path.GetFileName(sourcePath)} is not well-formed XML: {ex.Message}", ex);
		}
	}

	private static XmlReaderSettings Settings => new()
	{
		IgnoreComments = true,
		IgnoreWhitespace = true,
		DtdProcessing = DtdProcessing.Prohibit,
	};

	private static EramGeoMapFile ReadRecords(XmlReader reader, string sourcePath)
	{
		reader.MoveToContent();

		// MoveToContent lands on the root element (or throws), so its name is all there is to check.
		if (reader.LocalName != RootElement)
		{
			throw new InvalidDataException(
				$"{Path.GetFileName(sourcePath)} is not an ERAM Geomaps file: it starts with <{reader.LocalName}>, not <{RootElement}>.");
		}

		List<EramGeoMap> maps = [];
		List<string> problems = [];
		string? mapName = null;
		List<EramGeoMapObject>? objects = null;
		bool inMapId = false;

		while (reader.Read())
		{
			switch (reader.NodeType)
			{
				case XmlNodeType.Text when inMapId:
					mapName = reader.Value.Trim();
					continue;

				case XmlNodeType.EndElement:
					inMapId = false;

					if (reader.LocalName == "GeoMapRecord" && objects is not null)
					{
						maps.Add(new EramGeoMap(mapName ?? string.Empty, objects));
						mapName = null;
						objects = null;
					}

					continue;

				case not XmlNodeType.Element:
					continue;
			}

			switch (reader.LocalName)
			{
				case "GeoMapRecord":
					objects = [];

					// A record with no content closes itself: <GeoMapRecord />.
					if (reader.IsEmptyElement)
					{
						maps.Add(new EramGeoMap(string.Empty, objects));
						objects = null;
					}

					break;

				// Its text is the next node; reading it here would move the reader past what follows.
				case "GeomapId" when objects is not null:
					inMapId = !reader.IsEmptyElement;
					break;

				case "GeoMapObjectType" when objects is not null:
					using (XmlReader subtree = reader.ReadSubtree())
					{
						objects.Add(ReadObject(XElement.Load(subtree, LoadOptions.SetLineInfo), problems));
					}

					break;
			}
		}

		return new EramGeoMapFile(sourcePath, maps, problems);
	}

	// ================= one object =================

	private static EramGeoMapObject ReadObject(XElement mapObject, List<string> problems)
	{
		List<EramElement> elements = [];

		foreach (XElement child in mapObject.Elements())
		{
			switch (child.Name.LocalName)
			{
				case "GeoMapLine":
					AddLine(child, ReadProperties(child, problems), elements, problems);
					break;

				case "GeoMapSymbol":
					AddSymbol(child, elements, problems);
					break;

				case "GeoMapText":
					AddText(child, fallback: null, elements, problems);
					break;

				case "GeoMapSaa":
					AddSaa(child, elements, problems);
					break;
			}
		}

		return new EramGeoMapObject(
			Value(mapObject, "MapObjectType") ?? string.Empty,
			ReadInt(mapObject, "MapGroupId", problems),
			mapObject.Element("DefaultLineProperties") is { } line ? ReadProperties(line, problems) : null,
			mapObject.Element("DefaultSymbolProperties") is { } symbol ? ReadProperties(symbol, problems) : null,
			mapObject.Element("TextDefaultProperties") is { } text ? ReadProperties(text, problems) : null,
			elements);
	}

	private static void AddLine(XElement line, EramProperties overrides, List<EramElement> elements, List<string> problems)
	{
		if (TryReadPosition(line, "StartLatitude", "StartLongitude", out Coordinate start)
			&& TryReadPosition(line, "EndLatitude", "EndLongitude", out Coordinate end))
		{
			elements.Add(new EramElement(EramElementKind.Line, start, end, null, overrides));
			return;
		}

		problems.Add($"Line {LineOf(line)}: a {line.Name.LocalName} has a missing or invalid position and was skipped.");
	}

	private static void AddSymbol(XElement symbol, List<EramElement> elements, List<string> problems)
	{
		if (!TryReadPosition(symbol, "Latitude", "Longitude", out Coordinate position))
		{
			problems.Add($"Line {LineOf(symbol)}: a GeoMapSymbol has a missing or invalid position and was skipped.");
			return;
		}

		elements.Add(new EramElement(EramElementKind.Symbol, position, null, null, ReadProperties(symbol, problems)));

		// A symbol's own label is drawn at the symbol unless it gives a position of its own.
		if (symbol.Element("GeoMapText") is { } label)
		{
			AddText(label, position, elements, problems);
		}
	}

	private static void AddText(XElement text, Coordinate? fallback, List<EramElement> elements, List<string> problems)
	{
		IReadOnlyList<string> lines = [.. text.Elements("GeoTextStrings").Elements("TextLine").Select(line => line.Value.TrimEnd())];

		Coordinate? position = TryReadPosition(text, "Latitude", "Longitude", out Coordinate own) ? own : fallback;

		if (position is null)
		{
			problems.Add($"Line {LineOf(text)}: a GeoMapText has a missing or invalid position and was skipped.");
			return;
		}

		elements.Add(new EramElement(EramElementKind.Text, position, null, lines, ReadProperties(text, problems)));
	}

	private static void AddSaa(XElement saa, List<EramElement> elements, List<string> problems)
	{
		if (saa.Element("GeoMapSaaBoundary") is { } boundary)
		{
			EramProperties overrides = ReadProperties(boundary, problems);

			foreach (XElement segment in boundary.Elements("GeoSaaLinesSegments").Elements("GeoMapSaaLine"))
			{
				AddLine(segment, overrides, elements, problems);
			}
		}

		if (saa.Element("GeoMapSaaLabel") is { } label)
		{
			if (!TryReadPosition(label, "Latitude", "Longitude", out Coordinate position))
			{
				problems.Add($"Line {LineOf(label)}: SAA {Value(saa, "SaaID")}'s label has a missing or invalid position and was skipped.");
				return;
			}

			string[] lines = Value(label, "SaaLabel") is { } text ? [text] : [];
			elements.Add(new EramElement(EramElementKind.Text, position, null, lines, ReadProperties(label, problems)));
		}
	}

	// ================= values =================

	/// <summary>
	/// Reads the display properties among an element's children. A value that is present but not
	/// a number (or true/false) is reported and left out; the rest are kept.
	/// </summary>
	private static EramProperties ReadProperties(XElement element, List<string> problems)
	{
		EramProperties properties = new()
		{
			Bcg = ReadInt(element, "BCGGroup", problems),
			Filters = ReadFilters(element, problems),
			Style = Value(element, "LineStyle") ?? Value(element, "SymbolStyle"),
			Thickness = ReadInt(element, "Thickness", problems),
			Size = ReadInt(element, "FontSize", problems),
			Underline = ReadBool(element, "Underline", problems),
			XOffset = ReadInt(element, "XPixelOffset", problems),
			YOffset = ReadInt(element, "YPixelOffset", problems),
		};

		return properties.IsEmpty ? EramProperties.None : properties;
	}

	/// <summary>The <c>FilterGroup</c>s under an element's <c>GeoLineFilters</c>, <c>GeoSymbolFilters</c> or <c>GeoTextFilters</c>.</summary>
	private static IReadOnlyList<int>? ReadFilters(XElement element, List<string> problems)
	{
		XElement? container = element.Element("GeoLineFilters") ?? element.Element("GeoSymbolFilters") ?? element.Element("GeoTextFilters");

		if (container is null)
		{
			return null;
		}

		List<int> filters = [];

		foreach (XElement entry in container.Elements("FilterGroup"))
		{
			if (!int.TryParse(entry.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int filter))
			{
				problems.Add($"Line {LineOf(entry)}: FilterGroup \"{entry.Value}\" is not a whole number and was ignored.");
				continue;
			}

			if (!filters.Contains(filter))
			{
				filters.Add(filter);
			}
		}

		return filters.Count > 0 ? filters : null;
	}

	private static int? ReadInt(XElement parent, string name, List<string> problems)
	{
		if (parent.Element(name) is not { } element || string.IsNullOrWhiteSpace(element.Value))
		{
			return null;
		}

		if (int.TryParse(element.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
		{
			return value;
		}

		problems.Add($"Line {LineOf(element)}: {name} \"{element.Value}\" is not a whole number and was ignored.");
		return null;
	}

	private static bool? ReadBool(XElement parent, string name, List<string> problems)
	{
		if (parent.Element(name) is not { } element || string.IsNullOrWhiteSpace(element.Value))
		{
			return null;
		}

		if (bool.TryParse(element.Value.Trim(), out bool value))
		{
			return value;
		}

		problems.Add($"Line {LineOf(element)}: {name} \"{element.Value}\" is not true or false and was ignored.");
		return null;
	}

	/// <summary>A child's trimmed text, or <see langword="null"/> when it is missing or blank.</summary>
	private static string? Value(XElement parent, string name) =>
		parent.Element(name)?.Value.Trim() is { Length: > 0 } value ? value : null;

	private static bool TryReadPosition(XElement parent, string latName, string lonName, out Coordinate coordinate)
	{
		coordinate = null!;

		if (!TryParseLatitude(Value(parent, latName), out double lat) || !TryParseLongitude(Value(parent, lonName), out double lon))
		{
			return false;
		}

		coordinate = new Coordinate(lon, lat);
		return true;
	}

	/// <summary>Parses an ERAM latitude, <c>ddmmssnn</c> then <c>N</c> or <c>S</c>, e.g. <c>34300000N</c>.</summary>
	/// <param name="value">The text, or <see langword="null"/>.</param>
	/// <param name="degrees">Decimal degrees, negative in the south.</param>
	/// <returns>Whether it is a valid latitude.</returns>
	internal static bool TryParseLatitude(string? value, out double degrees) =>
		TryParseAngle(value, degreeDigits: 2, 'N', 'S', maxDegrees: 90, out degrees);

	/// <summary>Parses an ERAM longitude, <c>dddmmssnn</c> then <c>E</c> or <c>W</c>, e.g. <c>118350300W</c>.</summary>
	/// <param name="value">The text, or <see langword="null"/>.</param>
	/// <param name="degrees">Decimal degrees, negative in the west.</param>
	/// <returns>Whether it is a valid longitude.</returns>
	internal static bool TryParseLongitude(string? value, out double degrees) =>
		TryParseAngle(value, degreeDigits: 3, 'E', 'W', maxDegrees: 180, out degrees);

	private static bool TryParseAngle(string? value, int degreeDigits, char positive, char negative, int maxDegrees, out double degrees)
	{
		degrees = 0;

		if (value is null || value.Length != degreeDigits + 7)
		{
			return false;
		}

		char hemisphere = char.ToUpperInvariant(value[^1]);
		ReadOnlySpan<char> digits = value.AsSpan(0, value.Length - 1);

		if ((hemisphere != positive && hemisphere != negative) || digits.ContainsAnyExceptInRange('0', '9'))
		{
			return false;
		}

		int whole = int.Parse(digits[..degreeDigits], CultureInfo.InvariantCulture);
		int minutes = int.Parse(digits.Slice(degreeDigits, 2), CultureInfo.InvariantCulture);
		int seconds = int.Parse(digits.Slice(degreeDigits + 2, 2), CultureInfo.InvariantCulture);
		int hundredths = int.Parse(digits.Slice(degreeDigits + 4, 2), CultureInfo.InvariantCulture);

		// Seconds of 60 are the FAA's rounding and carry into the minute; anything past is an error.
		if (minutes >= 60 || seconds > 60 || (seconds == 60 && hundredths > 0))
		{
			return false;
		}

		double result = whole + minutes / 60.0 + (seconds + hundredths / 100.0) / 3600.0;

		if (result > maxDegrees)
		{
			return false;
		}

		degrees = hemisphere == negative ? -result : result;
		return true;
	}

	private static int LineOf(XElement element) => ((IXmlLineInfo)element).LineNumber;
}
