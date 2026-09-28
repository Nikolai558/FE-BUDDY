using System.Globalization;

using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Departures;

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

	/// <summary>The keys only Departures reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateGeojson", "EmitLines", "EmitSymbols", "EmitText",
		"IncludeObstacleDepartures", "ArtccFilter",
		"AmendmentFilter", "AmendedWithinCycles", "AmendedWithinDays", "AmendedOnOrAfter",
		"RoiMode", "GenerateAliasFile",
	};

	/// <summary>Departures has one CRC defaults class, which draws all three kinds.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[nameof(DepartureCrcClass.Departures)] = Enum.GetValues<CrcFeatureKind>(),
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
	public static DepartureSettingsParseResult Parse(IReadOnlyDictionary<string, string> departureSettings)
	{
		ArgumentNullException.ThrowIfNull(departureSettings);

		string outputDirectory = SettingsValueReader.RequiredString(departureSettings, "OutputDirectory");

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

		IReadOnlyCollection<string> artccFilter = [.. SettingsValueReader.StringList(departureSettings, "ArtccFilter")
			.Select(artcc => artcc.ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)];

		// Only the value the chosen mode uses is read (and required); the others are ignored.
		DepartureAmendmentFilter amendmentFilter = SettingsValueReader.OptionalEnum(
			departureSettings, "AmendmentFilter", DepartureAmendmentFilter.None,
			hint: "Use \"None\" (keep every procedure), \"Cycles\" (with AmendedWithinCycles), " +
				"\"Days\" (with AmendedWithinDays) or \"Date\" (with AmendedOnOrAfter).");

		int amendedWithinCycles = amendmentFilter == DepartureAmendmentFilter.Cycles
			? SettingsValueReader.RequiredIntInRange(departureSettings, "AmendedWithinCycles", minimum: 1, maximum: 1000)
			: 0;

		int amendedWithinDays = amendmentFilter == DepartureAmendmentFilter.Days
			? SettingsValueReader.RequiredIntInRange(departureSettings, "AmendedWithinDays", minimum: 1, maximum: 36500)
			: 0;

		DateOnly? amendedOnOrAfter = amendmentFilter == DepartureAmendmentFilter.Date
			? ParseAmendedOnOrAfter(departureSettings)
			: null;

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(departureSettings);
		DepartureRoiMode roiMode = SettingsValueReader.OptionalEnum(
			departureSettings, "RoiMode", DepartureRoiMode.Airport,
			hint: "Use \"Airport\" (every departure of an airport inside the ROI) " +
				"or \"Waypoint\" (any departure with a point inside the ROI).");

		(bool includeFebProperties, IReadOnlyList<DepartureFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<DepartureFebProperty>(departureSettings, example: "dpName,pointId,arptId");

		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(departureSettings);

		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			departureSettings, DepartureOutputFiles.Alias, DepartureOutputFiles.IsGeojsonKey,
			example: $"{DepartureOutputFiles.Lines}, {DepartureOutputFiles.Alias}");

		// A kind's defaults are needed only when its files get CRC-ERAM defaults AND are actually
		// written; only then are its values required.
		Dictionary<DepartureCrcClass, CrcLineDefaults> lineDefaults = [];
		Dictionary<DepartureCrcClass, CrcSymbolDefaults> symbolDefaults = [];
		Dictionary<DepartureCrcClass, CrcTextDefaults> textDefaults = [];

		const DepartureCrcClass cls = DepartureCrcClass.Departures;

		if (generateGeojson && emitLines && vnas.HasCrcDefaults(DepartureOutputFiles.Lines))
			lineDefaults[cls] = CrcDefaultsReader.ReadLine(departureSettings, $"Crc.{cls}.Line");

		if (generateGeojson && emitSymbols && vnas.HasCrcDefaults(DepartureOutputFiles.Symbols))
			symbolDefaults[cls] = CrcDefaultsReader.ReadSymbol(departureSettings, $"Crc.{cls}.Symbol");

		if (generateGeojson && emitText && vnas.HasCrcDefaults(DepartureOutputFiles.Text))
			textDefaults[cls] = CrcDefaultsReader.ReadText(departureSettings, $"Crc.{cls}.Text");

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			departureSettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "each point is labelled with its own identifier");

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
			Vnas = vnas,
			CoordinatePrecision = coordinatePrecision,
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new DepartureSettingsParseResult(settings, messages);
	}

	private static DateOnly ParseAmendedOnOrAfter(IReadOnlyDictionary<string, string> settings)
	{
		string raw = SettingsValueReader.RequiredString(settings, "AmendedOnOrAfter");

		if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
		{
			throw new ArgumentException(
				$"'AmendedOnOrAfter' value '{raw}' is not a valid date. Use the format yyyy-MM-dd, e.g. \"2026-01-01\".");
		}

		return date;
	}
}
