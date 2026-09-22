using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Airports;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Airports sub-service into a typed, validated <see cref="AirportSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Airports sub-service that touches the raw dictionary; everything
/// downstream works with <see cref="AirportSettings"/>. Generic value reading lives in
/// <see cref="SettingsValueReader"/>, shared with every other sub-service.
/// </remarks>
public static class AirportSettingsParser
{
	private const string LogSource = "AirportSettingsParser";

	/// <summary>Dictionary keys recognized outside of the <c>Crc.*</c> property-default keys.</summary>
	private static readonly HashSet<string> KnownScalarKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"OutputDirectory", "GenerateGeojson", "EmitAirportSymbols", "EmitAirportText",
		"EmitRunwayLines", "GenerateAliasFile", "IncludeFebCustomProperties", "FebProperties",
		"IncludeCrcEramPropertyDefaults", "FilterByRoi",
		"RoiSwLat", "RoiSwLon", "RoiNeLat", "RoiNeLon",
		"CoordinatePrecision", "AddFeBuddyOutputFolder"
	};

	private static readonly HashSet<string> LinePropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "thickness" };

	private static readonly HashSet<string> SymbolPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "size" };

	// Deliberately excludes "text": every airport supplies its own label (its identifier and
	// name), so a class-wide default for it would never be used.
	private static readonly HashSet<string> TextPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "size", "underline", "xOffset", "yOffset", "opaque" };

	private static readonly Regex CrcKeyPattern = new(
		@"^Crc\.(Airports|Runways)\.(Line|Symbol|Text)\.(\w+)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Dictionary<string, AirportFebProperty> FebPropertiesByName =
		new(StringComparer.OrdinalIgnoreCase)
		{
			["faaId"] = AirportFebProperty.FaaId,
			["icaoId"] = AirportFebProperty.IcaoId,
			["name"] = AirportFebProperty.Name,
			["elev"] = AirportFebProperty.Elev,
			["respArtcc"] = AirportFebProperty.RespArtcc,
			["tfcPtrnAlt"] = AirportFebProperty.TfcPtrnAlt,
			["fssId"] = AirportFebProperty.FssId,
			["twrType"] = AirportFebProperty.TwrType,
		};

	/// <summary>
	/// Parses and validates <paramref name="airportSettings"/> into a typed
	/// <see cref="AirportSettings"/>.
	/// </summary>
	/// <param name="airportSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// output choices would produce no output at all.
	/// </exception>
	public static AirportSettingsParseResult Parse(Dictionary<string, string> airportSettings)
	{
		ArgumentNullException.ThrowIfNull(airportSettings);

		string outputDirectory = SettingsValueReader.RequireNonEmpty(airportSettings, "OutputDirectory");

		bool generateGeojson = SettingsValueReader.YesNo(airportSettings, "GenerateGeojson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(airportSettings, "GenerateAliasFile", defaultValue: true);

		// Selecting the sub-service and then turning off both of its outputs asks for a run
		// that writes nothing. The GUI blocks this at the tab; the parser is the backstop for
		// the harness and for a hand-edited UserConfig.
		if (!generateGeojson && !generateAliasFile)
		{
			throw new ArgumentException(
				"GenerateGeojson and GenerateAliasFile are both \"N\", so the Airports sub-service would produce nothing. " +
				"Turn one back on, or deselect Airports.");
		}

		bool emitSymbols = SettingsValueReader.YesNo(airportSettings, "EmitAirportSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(airportSettings, "EmitAirportText", defaultValue: true);
		bool emitRunways = SettingsValueReader.YesNo(airportSettings, "EmitRunwayLines", defaultValue: true);

		if (generateGeojson && !emitSymbols && !emitText && !emitRunways)
		{
			throw new ArgumentException(
				"EmitAirportSymbols, EmitAirportText and EmitRunwayLines are all \"N\", but GenerateGeojson is \"Y\". " +
				"Turn at least one file back on, or set GenerateGeojson to \"N\".");
		}

		bool includeFebProperties = SettingsValueReader.YesNo(airportSettings, "IncludeFebCustomProperties", defaultValue: false);
		IReadOnlyCollection<AirportFebProperty> febProperties = ParseFebProperties(airportSettings, includeFebProperties);

		bool includeCrcDefaults = SettingsValueReader.YesNo(airportSettings, "IncludeCrcEramPropertyDefaults", defaultValue: false);
		bool filterByRoi = SettingsValueReader.YesNo(airportSettings, "FilterByRoi", defaultValue: false);

		RegionOfInterest? roi = filterByRoi ? ParseRoi(airportSettings) : null;

		int coordinatePrecision = SettingsValueReader.IntInRange(
			airportSettings, "CoordinatePrecision", defaultValue: 6, minimum: 0, maximum: 15);

		bool addFeBuddyOutputFolder = SettingsValueReader.YesNo(airportSettings, "AddFeBuddyOutputFolder", defaultValue: true);

		Dictionary<AirportCrcClass, CrcLineProperties> lineDefaults = new();
		Dictionary<AirportCrcClass, CrcSymbolProperties> symbolDefaults = new();
		Dictionary<AirportCrcClass, CrcTextProperties> textDefaults = new();

		if (includeCrcDefaults && generateGeojson)
		{
			// Only the blocks whose file is actually being written are required, so a user who
			// wants symbols only is never asked to fill in runway line properties.
			if (emitSymbols)
			{
				CrcSymbolProperties symbol = ParseSymbolProperties(airportSettings, AirportCrcClass.Airports);
				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateSymbol(symbol), AirportCrcClass.Airports, "Symbol");
				symbolDefaults[AirportCrcClass.Airports] = symbol;
			}

			if (emitText)
			{
				CrcTextProperties text = ParseTextProperties(airportSettings, AirportCrcClass.Airports);
				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateText(text), AirportCrcClass.Airports, "Text");
				textDefaults[AirportCrcClass.Airports] = text;
			}

			if (emitRunways)
			{
				CrcLineProperties line = ParseLineProperties(airportSettings, AirportCrcClass.Runways);
				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateLine(line), AirportCrcClass.Runways, "Line");
				lineDefaults[AirportCrcClass.Runways] = line;
			}
		}

		List<ServiceMessage> messages = new();
		CollectUnknownKeyWarnings(airportSettings, messages);

		AirportSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			GenerateGeojson = generateGeojson,
			EmitAirportSymbols = emitSymbols,
			EmitAirportText = emitText,
			EmitRunwayLines = emitRunways,
			GenerateAliasFile = generateAliasFile,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			IncludeCrcEramPropertyDefaults = includeCrcDefaults,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			AddFeBuddyOutputFolder = addFeBuddyOutputFolder,
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new AirportSettingsParseResult(settings, messages);
	}

	private static IReadOnlyCollection<AirportFebProperty> ParseFebProperties(
		Dictionary<string, string> settings,
		bool includeFebProperties)
	{
		if (!includeFebProperties)
		{
			return Array.Empty<AirportFebProperty>();
		}

		IReadOnlyList<string> names = SettingsValueReader.StringList(settings, "FebProperties");

		if (names.Count == 0)
		{
			throw new ArgumentException(
				"IncludeFebCustomProperties is \"Y\" but 'FebProperties' names none. " +
				"List the properties to write, e.g. \"faaId,icaoId,elev\".");
		}

		List<AirportFebProperty> properties = new();

		foreach (string name in names)
		{
			if (name.Equals("lat", StringComparison.OrdinalIgnoreCase) || name.Equals("lon", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(
					$"'FebProperties' entry '{name}' is no longer offered: every Feature's geometry already carries " +
					"its coordinates. Remove it from the list.");
			}

			if (!FebPropertiesByName.TryGetValue(name, out AirportFebProperty property))
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

	private static CrcLineProperties ParseLineProperties(Dictionary<string, string> settings, AirportCrcClass cls) =>
		new()
		{
			Bcg = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Line", "bcg")),
			Filters = SettingsValueReader.RequiredIntList(settings, CrcKey(cls, "Line", "filters")),
			Style = SettingsValueReader.NormalizeStyle(
				SettingsValueReader.OptionalString(settings, CrcKey(cls, "Line", "style")),
				CrcGeojsonPropertyValidator.ValidLineStyles),
			Thickness = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Line", "thickness"))
		};

	private static CrcSymbolProperties ParseSymbolProperties(Dictionary<string, string> settings, AirportCrcClass cls) =>
		new()
		{
			Bcg = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Symbol", "bcg")),
			Filters = SettingsValueReader.RequiredIntList(settings, CrcKey(cls, "Symbol", "filters")),
			Style = SettingsValueReader.NormalizeStyle(
				SettingsValueReader.OptionalString(settings, CrcKey(cls, "Symbol", "style")),
				CrcGeojsonPropertyValidator.ValidSymbolStyles),
			Size = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Symbol", "size"))
		};

	private static CrcTextProperties ParseTextProperties(Dictionary<string, string> settings, AirportCrcClass cls) =>
		new()
		{
			Bcg = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Text", "bcg")),
			Filters = SettingsValueReader.RequiredIntList(settings, CrcKey(cls, "Text", "filters")),
			// Placeholder only - never rendered. Each airport's Text Feature supplies its own
			// "text" override built from its identifier and name.
			Text = new[] { $"{cls}_DEFAULT" },
			Size = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Text", "size")),
			Underline = SettingsValueReader.OptionalYesNo(settings, CrcKey(cls, "Text", "underline")),
			XOffset = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Text", "xOffset")),
			YOffset = SettingsValueReader.OptionalInt(settings, CrcKey(cls, "Text", "yOffset")),
			Opaque = SettingsValueReader.OptionalYesNo(settings, CrcKey(cls, "Text", "opaque"))
		};

	private static string CrcKey(AirportCrcClass cls, string kind, string property) =>
		$"Crc.{cls}.{kind}.{property}";

	private static void ThrowIfInvalid(CrcPropertyValidationResult result, AirportCrcClass cls, string kind)
	{
		if (!result.IsValid)
		{
			throw new ArgumentException(
				$"Invalid CRC {kind} property defaults for '{cls}':{Environment.NewLine}" +
				string.Join(Environment.NewLine, result.Errors));
		}
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

			if (!match.Success)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Unrecognized airportSettings key '{key}' was ignored."));
				continue;
			}

			string cls = match.Groups[1].Value;
			string kind = match.Groups[2].Value;
			string property = match.Groups[3].Value;

			// Airports draws points (Symbol and Text); Runways draws lines. The other two
			// pairings cannot be produced, so a key using one is a typo worth reporting rather
			// than a value that silently does nothing.
			bool pairingExists = cls.Equals("Airports", StringComparison.OrdinalIgnoreCase)
				? !kind.Equals("Line", StringComparison.OrdinalIgnoreCase)
				: kind.Equals("Line", StringComparison.OrdinalIgnoreCase);

			if (!pairingExists)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"'{key}' was ignored: '{cls}' has no {kind} output."));
				continue;
			}

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
					$"'{key}' was ignored: an airport's label is always built from its identifier and name, so it cannot be set as a class-wide default."));
				continue;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Unrecognized airportSettings key '{key}' was ignored."));
		}
	}
}
