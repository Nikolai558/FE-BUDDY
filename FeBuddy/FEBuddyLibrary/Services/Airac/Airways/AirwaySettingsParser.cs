using System.Globalization;
using System.Text.RegularExpressions;

using FEBuddyLibrary.Models.Geojson;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

namespace FEBuddyLibrary.Services.Airac.Airways;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FEBuddyTest</c>)
/// supplies for the Airways services into a typed, validated <see cref="AirwaySettings"/>.
/// </summary>
/// <remarks>
/// This is the only place in the Airways services that touches the raw settings dictionary;
/// every downstream type works with <see cref="AirwaySettings"/>. See the build plan's
/// Settings Contract section for the full accepted-key table.
/// </remarks>
public static class AirwaySettingsParser
{
	private static readonly AirwayAltitudeClass[] AllClasses =
	{
		AirwayAltitudeClass.High, AirwayAltitudeClass.Low, AirwayAltitudeClass.Other
	};

	/// <summary>Dictionary keys recognized outside of the <c>Crc.*</c> property-default keys.</summary>
	private static readonly HashSet<string> KnownScalarKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"OutputDirectory", "OutputBy", "BufferAirwayWaypoints", "IncludeFebCustomProperties",
		"IncludeAirwayWaypointIds", "GenerateAliasFile", "SplitAtAntimeridian",
		"IncludeCrcEramPropertyDefaults", "FilterByRoi",
		"RoiSwLat", "RoiSwLon", "RoiNeLat", "RoiNeLon",
		"ExcludedDesignations", "EmitLines", "EmitSymbols", "EmitText",
		"AliasRoiScope", "CoordinatePrecision", "AddFeBuddyOutputFolder"
	};

	private static readonly HashSet<string> LinePropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "thickness" };

	private static readonly HashSet<string> SymbolPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "style", "size" };

	// Deliberately excludes "text": per-waypoint text cannot be meaningfully configured as a
	// single class-wide default, since every waypoint's label differs. See AirwaySettings.TextDefaults.
	private static readonly HashSet<string> TextPropertyNames =
		new(StringComparer.OrdinalIgnoreCase) { "bcg", "filters", "size", "underline", "xOffset", "yOffset", "opaque" };

	private static readonly Regex CrcKeyPattern = new(
		@"^Crc\.(High|Low|Other)\.(Line|Symbol|Text)\.(\w+)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	/// <summary>
	/// Parses and validates <paramref name="airwaySettings"/> into a typed
	/// <see cref="AirwaySettings"/>.
	/// </summary>
	/// <param name="airwaySettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing warnings.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, or a present setting's value is invalid.
	/// </exception>
	public static AirwaySettingsParseResult Parse(Dictionary<string, string> airwaySettings)
	{
		ArgumentNullException.ThrowIfNull(airwaySettings);

		string outputDirectory = RequireNonEmpty(airwaySettings, "OutputDirectory").Trim();
		AirwayGeojsonOutputBy outputBy = ParseOutputBy(RequireNonEmpty(airwaySettings, "OutputBy"));

		bool bufferAirwayWaypoints = ParseYesNo(airwaySettings, "BufferAirwayWaypoints", defaultValue: false);
		bool includeFebCustomProperties = ParseYesNo(airwaySettings, "IncludeFebCustomProperties", defaultValue: false);
		bool includeAirwayWaypointIds = ParseYesNo(airwaySettings, "IncludeAirwayWaypointIds", defaultValue: false);
		bool generateAliasFile = ParseYesNo(airwaySettings, "GenerateAliasFile", defaultValue: true);
		bool splitAtAntimeridian = ParseYesNo(airwaySettings, "SplitAtAntimeridian", defaultValue: true);
		bool includeCrcDefaults = ParseYesNo(airwaySettings, "IncludeCrcEramPropertyDefaults", defaultValue: false);
		bool filterByRoi = ParseYesNo(airwaySettings, "FilterByRoi", defaultValue: false);

		RegionOfInterest? roi = filterByRoi ? ParseRoi(airwaySettings) : null;

		IReadOnlyCollection<string> excludedDesignations = ParseExcludedDesignations(airwaySettings);

		bool emitLines = ParseYesNo(airwaySettings, "EmitLines", defaultValue: true);
		bool emitSymbols = ParseYesNo(airwaySettings, "EmitSymbols", defaultValue: true);
		bool emitText = ParseYesNo(airwaySettings, "EmitText", defaultValue: true);

		// All three kinds off is only meaningful when nothing is being written anyway.
		if (!emitLines && !emitSymbols && !emitText && outputBy != AirwayGeojsonOutputBy.None)
		{
			throw new ArgumentException(
				"EmitLines, EmitSymbols and EmitText are all \"N\", but OutputBy is not \"None\". " +
				"Turn at least one kind back on, or set OutputBy to \"None\".");
		}

		AliasRoiScope aliasRoiScope = ParseAliasRoiScope(airwaySettings);
		int coordinatePrecision = ParseCoordinatePrecision(airwaySettings);
		bool addFeBuddyOutputFolder = ParseYesNo(airwaySettings, "AddFeBuddyOutputFolder", defaultValue: true);

		Dictionary<AirwayAltitudeClass, CrcLineProperties> lineDefaults = new();
		Dictionary<AirwayAltitudeClass, CrcSymbolProperties> symbolDefaults = new();
		Dictionary<AirwayAltitudeClass, CrcTextProperties> textDefaults = new();

		if (includeCrcDefaults)
		{
			foreach (AirwayAltitudeClass cls in AllClasses)
			{
				CrcLineProperties line = ParseLineProperties(airwaySettings, cls);
				CrcSymbolProperties symbol = ParseSymbolProperties(airwaySettings, cls);
				CrcTextProperties text = ParseTextProperties(airwaySettings, cls);

				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateLine(line), cls, "Line");
				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateSymbol(symbol), cls, "Symbol");
				ThrowIfInvalid(CrcGeojsonPropertyValidator.ValidateText(text), cls, "Text");

				lineDefaults[cls] = line;
				symbolDefaults[cls] = symbol;
				textDefaults[cls] = text;
			}
		}

		List<string> warnings = new();
		CollectUnknownKeyWarnings(airwaySettings, warnings);

		AirwaySettings settings = new()
		{
			OutputDirectory = outputDirectory,
			OutputBy = outputBy,
			BufferAirwayWaypoints = bufferAirwayWaypoints,
			IncludeFebCustomProperties = includeFebCustomProperties,
			IncludeAirwayWaypointIds = includeAirwayWaypointIds,
			GenerateAliasFile = generateAliasFile,
			SplitAtAntimeridian = splitAtAntimeridian,
			IncludeCrcEramPropertyDefaults = includeCrcDefaults,
			Roi = roi,
			ExcludedDesignations = excludedDesignations,
			EmitLines = emitLines,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			AliasRoiScope = aliasRoiScope,
			CoordinatePrecision = coordinatePrecision,
			AddFeBuddyOutputFolder = addFeBuddyOutputFolder,
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new AirwaySettingsParseResult(settings, warnings);
	}

	private static AirwayGeojsonOutputBy ParseOutputBy(string value)
	{
		if (value.Equals("None", StringComparison.OrdinalIgnoreCase))
			return AirwayGeojsonOutputBy.None;

		if (value.Equals("HighLow", StringComparison.OrdinalIgnoreCase))
			return AirwayGeojsonOutputBy.HighLow;

		if (value.Equals("Designation", StringComparison.OrdinalIgnoreCase))
			return AirwayGeojsonOutputBy.Designation;

		throw new ArgumentException(
			$"OutputBy value '{value}' is invalid. Must be \"None\", \"HighLow\", or \"Designation\".");
	}

	private static IReadOnlyCollection<string> ParseExcludedDesignations(Dictionary<string, string> settings)
	{
		if (!settings.TryGetValue("ExcludedDesignations", out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<string>();
		}

		return value
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(designation => designation.ToUpperInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	private static AliasRoiScope ParseAliasRoiScope(Dictionary<string, string> settings)
	{
		if (!settings.TryGetValue("AliasRoiScope", out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return AliasRoiScope.All;
		}

		value = value.Trim();

		if (value.Equals("All", StringComparison.OrdinalIgnoreCase))
			return AliasRoiScope.All;

		if (value.Equals("RoiAirways", StringComparison.OrdinalIgnoreCase))
			return AliasRoiScope.RoiAirways;

		throw new ArgumentException($"AliasRoiScope value '{value}' is invalid. Must be \"All\" or \"RoiAirways\".");
	}

	private static int ParseCoordinatePrecision(Dictionary<string, string> settings)
	{
		if (!settings.TryGetValue("CoordinatePrecision", out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return 6;
		}

		if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
			|| parsed < 0 || parsed > 15)
		{
			throw new ArgumentException($"CoordinatePrecision value '{value}' is invalid. Must be an integer from 0 to 15.");
		}

		return parsed;
	}

	private static RegionOfInterest ParseRoi(Dictionary<string, string> settings)
	{
		string swLatText = RequireNonEmpty(settings, "RoiSwLat");
		string swLonText = RequireNonEmpty(settings, "RoiSwLon");
		string neLatText = RequireNonEmpty(settings, "RoiNeLat");
		string neLonText = RequireNonEmpty(settings, "RoiNeLon");

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

	private static CrcLineProperties ParseLineProperties(Dictionary<string, string> settings, AirwayAltitudeClass cls)
	{
		return new CrcLineProperties
		{
			Bcg = ParseOptionalInt(settings, CrcKey(cls, "Line", "bcg")),
			Filters = ParseRequiredIntList(settings, CrcKey(cls, "Line", "filters")),
			Style = NormalizeStyle(ParseOptionalString(settings, CrcKey(cls, "Line", "style")), CrcGeojsonPropertyValidator.ValidLineStyles),
			Thickness = ParseOptionalInt(settings, CrcKey(cls, "Line", "thickness"))
		};
	}

	private static CrcSymbolProperties ParseSymbolProperties(Dictionary<string, string> settings, AirwayAltitudeClass cls)
	{
		return new CrcSymbolProperties
		{
			Bcg = ParseOptionalInt(settings, CrcKey(cls, "Symbol", "bcg")),
			Filters = ParseRequiredIntList(settings, CrcKey(cls, "Symbol", "filters")),
			Style = NormalizeStyle(ParseOptionalString(settings, CrcKey(cls, "Symbol", "style")), CrcGeojsonPropertyValidator.ValidSymbolStyles),
			Size = ParseOptionalInt(settings, CrcKey(cls, "Symbol", "size"))
		};
	}

	private static CrcTextProperties ParseTextProperties(Dictionary<string, string> settings, AirwayAltitudeClass cls)
	{
		return new CrcTextProperties
		{
			Bcg = ParseOptionalInt(settings, CrcKey(cls, "Text", "bcg")),
			Filters = ParseRequiredIntList(settings, CrcKey(cls, "Text", "filters")),
			// Placeholder only: the isDefaults Text Feature is never rendered by CRC, and
			// every real Text Feature always supplies its own waypoint-specific "text"
			// override built by AirwayGeojsonService. See AirwaySettings.TextDefaults.
			Text = new[] { $"{cls}_DEFAULT" },
			Size = ParseOptionalInt(settings, CrcKey(cls, "Text", "size")),
			Underline = ParseOptionalYesNo(settings, CrcKey(cls, "Text", "underline")),
			XOffset = ParseOptionalInt(settings, CrcKey(cls, "Text", "xOffset")),
			YOffset = ParseOptionalInt(settings, CrcKey(cls, "Text", "yOffset")),
			Opaque = ParseOptionalYesNo(settings, CrcKey(cls, "Text", "opaque"))
		};
	}

	private static string CrcKey(AirwayAltitudeClass cls, string kind, string property) =>
		$"Crc.{cls}.{kind}.{property}";

	private static string RequireNonEmpty(Dictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException($"airwaySettings must contain a non-empty '{key}' value.");
		}

		return value;
	}

	private static bool ParseYesNo(Dictionary<string, string> settings, string key, bool defaultValue)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return ParseYesNoValue(key, value);
	}

	private static bool? ParseOptionalYesNo(Dictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		return ParseYesNoValue(key, value);
	}

	private static bool ParseYesNoValue(string key, string value)
	{
		value = value.Trim();

		if (value.Equals("Y", StringComparison.OrdinalIgnoreCase))
			return true;

		if (value.Equals("N", StringComparison.OrdinalIgnoreCase))
			return false;

		throw new ArgumentException($"'{key}' must be either \"Y\" or \"N\", but was '{value}'.");
	}

	private static string? ParseOptionalString(Dictionary<string, string> settings, string key)
	{
		return settings.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
			? value.Trim()
			: null;
	}

	private static int? ParseOptionalInt(Dictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
		{
			throw new ArgumentException($"'{key}' value '{value}' is not a valid integer.");
		}

		return parsed;
	}

	private static IReadOnlyList<int> ParseRequiredIntList(Dictionary<string, string> settings, string key)
	{
		string value = RequireNonEmpty(settings, key);

		string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (parts.Length == 0)
		{
			throw new ArgumentException($"'{key}' must contain at least one comma-separated integer value.");
		}

		int[] result = new int[parts.Length];

		for (int i = 0; i < parts.Length; i++)
		{
			if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]))
			{
				throw new ArgumentException($"'{key}' entry '{parts[i]}' is not a valid integer.");
			}
		}

		return result;
	}

	private static string? NormalizeStyle(string? raw, IReadOnlyList<string> validValues)
	{
		if (raw is null)
		{
			return null;
		}

		foreach (string candidate in validValues)
		{
			if (string.Equals(candidate, raw, StringComparison.OrdinalIgnoreCase))
			{
				return candidate;
			}
		}

		// Not a recognized value; return as-is so the downstream CRC property validator
		// reports it with the full list of valid options.
		return raw;
	}

	private static void ThrowIfInvalid(Models.Geojson.CrcPropertyValidationResult result, AirwayAltitudeClass cls, string kind)
	{
		if (!result.IsValid)
		{
			throw new ArgumentException(
				$"Invalid CRC {kind} property defaults for altitude class '{cls}':{Environment.NewLine}" +
				string.Join(Environment.NewLine, result.Errors));
		}
	}

	private static void CollectUnknownKeyWarnings(Dictionary<string, string> settings, List<string> warnings)
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
				warnings.Add($"Unrecognized airwaySettings key '{key}' was ignored.");
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
				warnings.Add(
					$"'{key}' was ignored: per-waypoint text cannot be configured as a " +
					"class-wide default and is always generated from each waypoint's own ID.");
				continue;
			}

			warnings.Add($"Unrecognized airwaySettings key '{key}' was ignored.");
		}
	}
}
