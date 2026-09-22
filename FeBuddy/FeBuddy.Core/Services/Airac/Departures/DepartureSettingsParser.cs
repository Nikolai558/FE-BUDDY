using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Departures;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Departures sub-service into a typed, validated <see cref="DepartureSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Departures sub-service that touches the raw dictionary. Generic value
/// reading lives in <see cref="SettingsValueReader"/>, shared with every other sub-service.
/// </remarks>
public static class DepartureSettingsParser
{
	private const string LogSource = "DepartureSettingsParser";

	/// <summary>Dictionary keys recognized outside of the <c>Crc.*</c> property-default keys.</summary>
	private static readonly HashSet<string> KnownScalarKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"OutputDirectory", "GenerateGeojson", "EmitLines", "EmitSymbols", "EmitText",
		"GenerateAliasFile", "IncludeObstacleDepartures", "ArtccFilter",
		"AmendmentFilter", "AmendedWithinCycles", "AmendedWithinDays", "AmendedOnOrAfter",
		"IncludeFebCustomProperties", "FebProperties", CrcDefaultsReader.IncludeLineKey, CrcDefaultsReader.IncludeSymbolKey, CrcDefaultsReader.IncludeTextKey,
		"FilterByRoi", "RoiMode", "RoiSwLat", "RoiSwLon", "RoiNeLat", "RoiNeLon",
		"CoordinatePrecision", "AddFeBuddyOutputFolder"
	};

	private static readonly HashSet<string> LinePropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "thickness" };

	private static readonly HashSet<string> SymbolPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "size" };

	// Deliberately excludes "text": every point supplies its own label (its identifier), so a
	// class-wide default for it would never be used.
	private static readonly HashSet<string> TextPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "size", "underline", "xOffset", "yOffset", "opaque" };

	private static readonly Regex CrcKeyPattern = new(
		@"^Crc\.(\w+)\.(Line|Symbol|Text)\.(\w+)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Dictionary<string, DepartureFebProperty> FebPropertiesByName =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["dpName"] = DepartureFebProperty.DpName,
			["pointId"] = DepartureFebProperty.PointId,
			["arptId"] = DepartureFebProperty.ArptId,
			["artcc"] = DepartureFebProperty.Artcc,
			["amendmentNo"] = DepartureFebProperty.AmendmentNo,
			["amendEffDate"] = DepartureFebProperty.AmendEffDate,
			["waypoints"] = DepartureFebProperty.Waypoints,
		};

	/// <summary>
	/// Parses and validates <paramref name="departureSettings"/> into a typed
	/// <see cref="DepartureSettings"/>.
	/// </summary>
	/// <param name="departureSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// output choices would produce no output at all.
	/// </exception>
	public static DepartureSettingsParseResult Parse(Dictionary<string, string> departureSettings)
	{
		ArgumentNullException.ThrowIfNull(departureSettings);

		string outputDirectory = SettingsValueReader.RequireNonEmpty(departureSettings, "OutputDirectory");

		bool generateGeojson = SettingsValueReader.YesNo(departureSettings, "GenerateGeojson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(departureSettings, "GenerateAliasFile", defaultValue: true);

		// The GUI blocks this at the tab; the parser is the backstop for the harness and for a
		// hand-edited UserConfig.
		if (!generateGeojson && !generateAliasFile)
		{
			throw new ArgumentException(
				"GenerateGeojson and GenerateAliasFile are both \"N\", so the Departures sub-service would produce nothing. " +
				"Turn one back on, or deselect Departures.");
		}

		bool emitLines = SettingsValueReader.YesNo(departureSettings, "EmitLines", defaultValue: true);
		bool emitSymbols = SettingsValueReader.YesNo(departureSettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(departureSettings, "EmitText", defaultValue: true);

		if (generateGeojson && !emitLines && !emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"EmitLines, EmitSymbols and EmitText are all \"N\", but GenerateGeojson is \"Y\". " +
				"Turn at least one file back on, or set GenerateGeojson to \"N\".");
		}

		bool includeObstacleDepartures = SettingsValueReader.YesNo(departureSettings, "IncludeObstacleDepartures", defaultValue: true);

		IReadOnlyCollection<string> artccFilter = SettingsValueReader.StringList(departureSettings, "ArtccFilter")
			.Select(artcc => artcc.ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		// Only the value the chosen mode uses is read (and required); the others are ignored.
		DepartureAmendmentFilter amendmentFilter = ParseAmendmentFilter(departureSettings);

		int amendedWithinCycles = amendmentFilter == DepartureAmendmentFilter.Cycles
			? SettingsValueReader.RequiredIntInRange(departureSettings, "AmendedWithinCycles", minimum: 1, maximum: 1000)
			: 0;

		int amendedWithinDays = amendmentFilter == DepartureAmendmentFilter.Days
			? SettingsValueReader.RequiredIntInRange(departureSettings, "AmendedWithinDays", minimum: 1, maximum: 36500)
			: 0;

		DateOnly? amendedOnOrAfter = amendmentFilter == DepartureAmendmentFilter.Date
			? ParseAmendedOnOrAfter(departureSettings)
			: null;

		bool filterByRoi = SettingsValueReader.YesNo(departureSettings, "FilterByRoi", defaultValue: false);
		RegionOfInterest? roi = filterByRoi ? ParseRoi(departureSettings) : null;
		DepartureRoiMode roiMode = ParseRoiMode(departureSettings);

		bool includeFebProperties = SettingsValueReader.YesNo(departureSettings, "IncludeFebCustomProperties", defaultValue: false);
		IReadOnlyCollection<DepartureFebProperty> febProperties = ParseFebProperties(departureSettings, includeFebProperties);


		int coordinatePrecision = SettingsValueReader.IntInRange(
			departureSettings, "CoordinatePrecision", defaultValue: 6, minimum: 0, maximum: 15);

		bool addFeBuddyOutputFolder = SettingsValueReader.YesNo(departureSettings, "AddFeBuddyOutputFolder", defaultValue: true);

		// Each kind's defaults are written only when the user asked for them AND that file is
		// produced; only then are its values required.
		bool includeLineDefaults = CrcDefaultsReader.ReadInclude(departureSettings, CrcFeatureKind.Line) && generateGeojson && emitLines;
		bool includeSymbolDefaults = CrcDefaultsReader.ReadInclude(departureSettings, CrcFeatureKind.Symbol) && generateGeojson && emitSymbols;
		bool includeTextDefaults = CrcDefaultsReader.ReadInclude(departureSettings, CrcFeatureKind.Text) && generateGeojson && emitText;

		Dictionary<DepartureCrcClass, CrcLineDefaults> lineDefaults = new();
		Dictionary<DepartureCrcClass, CrcSymbolDefaults> symbolDefaults = new();
		Dictionary<DepartureCrcClass, CrcTextDefaults> textDefaults = new();

		const DepartureCrcClass cls = DepartureCrcClass.Departures;

		if (includeLineDefaults)
			lineDefaults[cls] = CrcDefaultsReader.ReadLine(departureSettings, $"Crc.{cls}.Line");

		if (includeSymbolDefaults)
			symbolDefaults[cls] = CrcDefaultsReader.ReadSymbol(departureSettings, $"Crc.{cls}.Symbol");

		if (includeTextDefaults)
			textDefaults[cls] = CrcDefaultsReader.ReadText(departureSettings, $"Crc.{cls}.Text");

		List<ServiceMessage> messages = new();
		CollectUnknownKeyWarnings(departureSettings, messages);

		DepartureSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			GenerateGeojson = generateGeojson,
			EmitLines = emitLines,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			GenerateAliasFile = generateAliasFile,
			IncludeObstacleDepartures = includeObstacleDepartures,
			ArtccFilter = artccFilter,
			AmendmentFilter = amendmentFilter,
			AmendedWithinCycles = amendedWithinCycles,
			AmendedWithinDays = amendedWithinDays,
			AmendedOnOrAfter = amendedOnOrAfter,
			Roi = roi,
			RoiMode = roiMode,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			IncludeCrcLineDefaults = includeLineDefaults,
			IncludeCrcSymbolDefaults = includeSymbolDefaults,
			IncludeCrcTextDefaults = includeTextDefaults,
			CoordinatePrecision = coordinatePrecision,
			AddFeBuddyOutputFolder = addFeBuddyOutputFolder,
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new DepartureSettingsParseResult(settings, messages);
	}

	private static DepartureRoiMode ParseRoiMode(Dictionary<string, string> settings)
	{
		string? raw = SettingsValueReader.OptionalString(settings, "RoiMode");

		if (string.IsNullOrWhiteSpace(raw))
		{
			return DepartureRoiMode.Airport;
		}

		// Matched by name only: Enum.TryParse would also accept "1" or "Airport,Waypoint".
		foreach (DepartureRoiMode mode in Enum.GetValues<DepartureRoiMode>())
		{
			if (raw.Trim().Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return mode;
			}
		}

		throw new ArgumentException(
			$"'RoiMode' value '{raw}' is not valid. Use \"Airport\" (every departure of an airport inside the ROI) " +
			"or \"Waypoint\" (any departure with a point inside the ROI).");
	}

	private static DepartureAmendmentFilter ParseAmendmentFilter(Dictionary<string, string> settings)
	{
		string? raw = SettingsValueReader.OptionalString(settings, "AmendmentFilter");

		if (string.IsNullOrWhiteSpace(raw))
		{
			return DepartureAmendmentFilter.None;
		}

		// Matched by name only: Enum.TryParse would also accept "1" or "Cycles,Days".
		foreach (DepartureAmendmentFilter mode in Enum.GetValues<DepartureAmendmentFilter>())
		{
			if (raw.Trim().Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return mode;
			}
		}

		throw new ArgumentException(
			$"'AmendmentFilter' value '{raw}' is not valid. Use \"None\" (keep every procedure), " +
			"\"Cycles\" (with AmendedWithinCycles), \"Days\" (with AmendedWithinDays) " +
			"or \"Date\" (with AmendedOnOrAfter).");
	}

	private static DateOnly ParseAmendedOnOrAfter(Dictionary<string, string> settings)
	{
		string raw = SettingsValueReader.RequireNonEmpty(settings, "AmendedOnOrAfter");

		if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
		{
			throw new ArgumentException(
				$"'AmendedOnOrAfter' value '{raw}' is not a valid date. Use the format yyyy-MM-dd, e.g. \"2026-01-01\".");
		}

		return date;
	}

	private static IReadOnlyCollection<DepartureFebProperty> ParseFebProperties(
		Dictionary<string, string> settings,
		bool includeFebProperties)
	{
		if (!includeFebProperties)
		{
			return Array.Empty<DepartureFebProperty>();
		}

		IReadOnlyList<string> names = SettingsValueReader.StringList(settings, "FebProperties");

		if (names.Count == 0)
		{
			throw new ArgumentException(
				"IncludeFebCustomProperties is \"Y\" but 'FebProperties' names none. " +
				"List the properties to write, e.g. \"dpName,pointId,arptId\".");
		}

		List<DepartureFebProperty> properties = new();

		foreach (string name in names)
		{
			if (!FebPropertiesByName.TryGetValue(name, out DepartureFebProperty property))
			{
				throw new ArgumentException(
					$"'FebProperties' entry '{name}' is not a known property. Valid values: " +
					string.Join(", ", FebPropertiesByName.Keys) + ".");
			}

			if (!properties.Contains(property))
			{
				properties.Add(property);
			}
		}

		return properties;
	}

	private static RegionOfInterest ParseRoi(Dictionary<string, string> settings)
	{
		string swLatText = SettingsValueReader.RequireNonEmpty(settings, "RoiSwLat");
		string swLonText = SettingsValueReader.RequireNonEmpty(settings, "RoiSwLon");
		string neLatText = SettingsValueReader.RequireNonEmpty(settings, "RoiNeLat");
		string neLonText = SettingsValueReader.RequireNonEmpty(settings, "RoiNeLon");

		if (!RoiFilter.IsCoordinateValidFormat(swLatText, swLonText, neLatText, neLonText, out string? formatError))
		{
			throw new ArgumentException($"Invalid Region of Interest: {formatError}");
		}

		double swLat = double.Parse(swLatText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double swLon = double.Parse(swLonText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double neLat = double.Parse(neLatText, NumberStyles.Float, CultureInfo.InvariantCulture);
		double neLon = double.Parse(neLonText, NumberStyles.Float, CultureInfo.InvariantCulture);

		if (!RoiFilter.IsCoordinatesRelativePositionValid(swLat, swLon, neLat, neLon, out string? positionError))
		{
			throw new ArgumentException($"Invalid Region of Interest: {positionError}");
		}

		return new RegionOfInterest(swLat, swLon, neLat, neLon);
	}

	private static void CollectUnknownKeyWarnings(Dictionary<string, string> settings, List<ServiceMessage> messages)
	{
		foreach (string key in settings.Keys)
		{
			if (KnownScalarKeys.Contains(key))
			{
				continue;
			}

			Match match = CrcKeyPattern.Match(key);

			if (!match.Success
				|| !match.Groups[1].Value.Equals(nameof(DepartureCrcClass.Departures), StringComparison.OrdinalIgnoreCase))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Unrecognized departureSettings key '{key}' was ignored."));
				continue;
			}

			string kind = match.Groups[2].Value;
			string property = match.Groups[3].Value;

			HashSet<string> allowedProperties = kind.Equals("Line", StringComparison.OrdinalIgnoreCase)
				? LinePropertyNames
				: kind.Equals("Symbol", StringComparison.OrdinalIgnoreCase)
					? SymbolPropertyNames
					: TextPropertyNames;

			if (allowedProperties.Contains(property))
			{
				continue;
			}

			if (kind.Equals("Text", StringComparison.OrdinalIgnoreCase) &&
				property.Equals("text", StringComparison.OrdinalIgnoreCase))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"'{key}' was ignored: each point is labelled with its own identifier, so a label cannot be set as a class-wide default."));
				continue;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Unrecognized departureSettings key '{key}' was ignored."));
		}
	}
}
