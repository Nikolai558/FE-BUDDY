using System.Globalization;

using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Arrivals sub-service into a typed, validated <see cref="ArrivalSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Arrivals sub-service that touches the raw dictionary. Generic value
/// reading lives in <see cref="SettingsValueReader"/>, shared with every other sub-service.
/// </remarks>
public static class ArrivalSettingsParser
{
	private const string LogSource = "ArrivalSettingsParser";

	/// <summary>The keys only Arrivals reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateGeojson", "EmitLines", "EmitSymbols", "EmitText",
		"ArtccFilter",
		"AmendmentFilter", "AmendedWithinCycles", "AmendedWithinDays", "AmendedOnOrAfter",
		"RoiMode",
	};

	/// <summary>Arrivals has one CRC defaults class, which draws all three kinds.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[nameof(ArrivalCrcClass.Arrivals)] = Enum.GetValues<CrcFeatureKind>(),
		};

	/// <summary>
	/// Parses and validates <paramref name="arrivalSettings"/> into a typed
	/// <see cref="ArrivalSettings"/>.
	/// </summary>
	/// <param name="arrivalSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// output choices would produce no output at all.
	/// </exception>
	public static ArrivalSettingsParseResult Parse(IReadOnlyDictionary<string, string> arrivalSettings)
	{
		ArgumentNullException.ThrowIfNull(arrivalSettings);

		string outputDirectory = SettingsValueReader.RequiredString(arrivalSettings, "OutputDirectory");

		bool generateGeojson = SettingsValueReader.YesNo(arrivalSettings, "GenerateGeojson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(arrivalSettings, "GenerateAliasFile", defaultValue: true);

		// The GUI blocks this at the tab; the parser is the backstop for the harness and for a
		// hand-edited UserConfig.
		if (!generateGeojson && !generateAliasFile)
		{
			throw new ArgumentException(
				"GenerateGeojson and GenerateAliasFile are both \"N\", so the Arrivals sub-service would produce nothing. " +
				"Turn one back on, or deselect Arrivals.");
		}

		bool emitLines = SettingsValueReader.YesNo(arrivalSettings, "EmitLines", defaultValue: true);
		bool emitSymbols = SettingsValueReader.YesNo(arrivalSettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(arrivalSettings, "EmitText", defaultValue: true);

		if (generateGeojson && !emitLines && !emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"EmitLines, EmitSymbols and EmitText are all \"N\", but GenerateGeojson is \"Y\". " +
				"Turn at least one file back on, or set GenerateGeojson to \"N\".");
		}

		IReadOnlyCollection<string> artccFilter = [.. SettingsValueReader.StringList(arrivalSettings, "ArtccFilter")
			.Select(artcc => artcc.ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)];

		// Only the value the chosen mode uses is read (and required); the others are ignored.
		ArrivalAmendmentFilter amendmentFilter = SettingsValueReader.OptionalEnum(
			arrivalSettings, "AmendmentFilter", ArrivalAmendmentFilter.None,
			hint: "Use \"None\" (keep every procedure), \"Cycles\" (with AmendedWithinCycles), " +
				"\"Days\" (with AmendedWithinDays) or \"Date\" (with AmendedOnOrAfter).");

		int amendedWithinCycles = amendmentFilter == ArrivalAmendmentFilter.Cycles
			? SettingsValueReader.RequiredIntInRange(arrivalSettings, "AmendedWithinCycles", minimum: 1, maximum: 1000)
			: 0;

		int amendedWithinDays = amendmentFilter == ArrivalAmendmentFilter.Days
			? SettingsValueReader.RequiredIntInRange(arrivalSettings, "AmendedWithinDays", minimum: 1, maximum: 36500)
			: 0;

		DateOnly? amendedOnOrAfter = amendmentFilter == ArrivalAmendmentFilter.Date
			? ParseAmendedOnOrAfter(arrivalSettings)
			: null;

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(arrivalSettings);
		ArrivalRoiMode roiMode = SettingsValueReader.OptionalEnum(
			arrivalSettings, "RoiMode", ArrivalRoiMode.Airport,
			hint: "Use \"Airport\" (every arrival of an airport inside the ROI) or \"Waypoint\" " +
				"(any arrival with a point inside the ROI).");

		(bool includeFebProperties, IReadOnlyList<ArrivalFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<ArrivalFebProperty>(arrivalSettings, example: "arrivalName,pointId,arptId");

		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(arrivalSettings);

		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			arrivalSettings, ArrivalOutputFiles.Alias, ArrivalOutputFiles.IsGeojsonKey,
			example: $"{ArrivalOutputFiles.Lines}, {ArrivalOutputFiles.Alias}");

		// A kind's defaults are needed only when its files get CRC-ERAM defaults AND are actually
		// written; only then are its values required.
		Dictionary<ArrivalCrcClass, CrcLineDefaults> lineDefaults = [];
		Dictionary<ArrivalCrcClass, CrcSymbolDefaults> symbolDefaults = [];
		Dictionary<ArrivalCrcClass, CrcTextDefaults> textDefaults = [];

		const ArrivalCrcClass cls = ArrivalCrcClass.Arrivals;

		if (generateGeojson && emitLines && vnas.HasCrcDefaults(ArrivalOutputFiles.Lines))
			lineDefaults[cls] = CrcDefaultsReader.ReadLine(arrivalSettings, $"Crc.{cls}.Line");

		if (generateGeojson && emitSymbols && vnas.HasCrcDefaults(ArrivalOutputFiles.Symbols))
			symbolDefaults[cls] = CrcDefaultsReader.ReadSymbol(arrivalSettings, $"Crc.{cls}.Symbol");

		if (generateGeojson && emitText && vnas.HasCrcDefaults(ArrivalOutputFiles.Text))
			textDefaults[cls] = CrcDefaultsReader.ReadText(arrivalSettings, $"Crc.{cls}.Text");

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			arrivalSettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "each point is labelled with its own identifier");

		ArrivalSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			GenerateGeojson = generateGeojson,
			EmitLines = emitLines,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			GenerateAliasFile = generateAliasFile,
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

		return new ArrivalSettingsParseResult(settings, messages);
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
