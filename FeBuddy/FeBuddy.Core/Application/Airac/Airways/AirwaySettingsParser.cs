using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Airac.Airways;

/// <summary>
/// Parses the raw settings block the GUI (or <c>FeBuddy.Harness</c>) supplies for the Airways
/// sub-service into a typed, validated <see cref="AirwaySettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Airways sub-service that touches the raw dictionary; everything
/// downstream works with <see cref="AirwaySettings"/>. Settings every sub-service shares are read
/// by <see cref="SubServiceSettingsReader"/>.
/// </remarks>
public static class AirwaySettingsParser
{
	private const string LogSource = "AirwaySettingsParser";

	/// <summary>The keys only Airways reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"OutputBy", "BufferAirwayWaypoints", "SplitAtAntimeridian", "ExcludedDesignations",
		"EmitLines", "EmitSymbols", "EmitText", "AliasRoiScope",
	};

	/// <summary>CRC defaults are set per altitude class (<c>Crc.High.Line.bcg</c>), and every class draws all three kinds.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		Enum.GetValues<AirwayAltitudeClass>().ToDictionary(
			altitudeClass => altitudeClass.ToString(),
			_ => Enum.GetValues<CrcFeatureKind>(),
			StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="airwaySettings"/> into a typed
	/// <see cref="AirwaySettings"/>.
	/// </summary>
	/// <param name="airwaySettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus a warning for every key that was not recognized.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the output choices would
	/// produce no GeoJSON.
	/// </exception>
	public static AirwaySettingsParseResult Parse(IReadOnlyDictionary<string, string> airwaySettings)
	{
		ArgumentNullException.ThrowIfNull(airwaySettings);

		AirwayGeojsonOutputBy outputBy = SettingsValueReader.RequiredEnum<AirwayGeojsonOutputBy>(airwaySettings, "OutputBy");
		bool writingGeojson = outputBy != AirwayGeojsonOutputBy.None;

		bool emitLines = SettingsValueReader.YesNo(airwaySettings, "EmitLines", defaultValue: true);
		bool emitSymbols = SettingsValueReader.YesNo(airwaySettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(airwaySettings, "EmitText", defaultValue: true);

		// All three kinds off is only meaningful when nothing is being written anyway.
		if (writingGeojson && !emitLines && !emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"EmitLines, EmitSymbols and EmitText are all \"N\", but OutputBy is not \"None\". " +
				"Turn at least one kind back on, or set OutputBy to \"None\".");
		}

		(bool includeFebProperties, IReadOnlyList<AirwayFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<AirwayFebProperty>(airwaySettings, example: "awyId,pointId,waypoints");

		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			airwaySettings, AirwayOutputFiles.Alias, AirwayOutputFiles.IsGeojsonKey,
			example: $"{AirwayOutputFiles.GeojsonKey(nameof(AirwayAltitudeClass.High), CrcFeatureKind.Line)}, {AirwayOutputFiles.Alias}");

		// A class's defaults are needed only when a file that gets CRC-ERAM defaults is actually
		// written and can hold that class; only then are its values required.
		Dictionary<AirwayAltitudeClass, CrcLineDefaults> lineDefaults = [];
		Dictionary<AirwayAltitudeClass, CrcSymbolDefaults> symbolDefaults = [];
		Dictionary<AirwayAltitudeClass, CrcTextDefaults> textDefaults = [];

		foreach (AirwayAltitudeClass altitudeClass in Enum.GetValues<AirwayAltitudeClass>())
		{
			if (writingGeojson && emitLines && NeedsCrcDefaults(vnas, outputBy, altitudeClass, CrcFeatureKind.Line))
				lineDefaults[altitudeClass] = CrcDefaultsReader.ReadLine(airwaySettings, $"Crc.{altitudeClass}.Line");

			if (writingGeojson && emitSymbols && NeedsCrcDefaults(vnas, outputBy, altitudeClass, CrcFeatureKind.Symbol))
				symbolDefaults[altitudeClass] = CrcDefaultsReader.ReadSymbol(airwaySettings, $"Crc.{altitudeClass}.Symbol");

			if (writingGeojson && emitText && NeedsCrcDefaults(vnas, outputBy, altitudeClass, CrcFeatureKind.Text))
				textDefaults[altitudeClass] = CrcDefaultsReader.ReadText(airwaySettings, $"Crc.{altitudeClass}.Text");
		}

		AirwaySettings settings = new()
		{
			OutputDirectory = SettingsValueReader.RequiredString(airwaySettings, "OutputDirectory"),
			OutputBy = outputBy,
			BufferAirwayWaypoints = SettingsValueReader.YesNo(airwaySettings, "BufferAirwayWaypoints", defaultValue: false),
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			GenerateAliasFile = SettingsValueReader.YesNo(airwaySettings, "GenerateAliasFile", defaultValue: true),
			SplitAtAntimeridian = SettingsValueReader.YesNo(airwaySettings, "SplitAtAntimeridian", defaultValue: true),
			Vnas = vnas,
			Roi = SubServiceSettingsReader.ReadRoi(airwaySettings),
			ExcludedDesignations = SettingsValueReader.StringList(airwaySettings, "ExcludedDesignations")
				.Select(designation => designation.ToUpperInvariant())
				.ToHashSet(StringComparer.OrdinalIgnoreCase),
			EmitLines = emitLines,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			AliasRoiScope = SettingsValueReader.OptionalEnum(airwaySettings, "AliasRoiScope", AliasRoiScope.All),
			CoordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(airwaySettings),
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			airwaySettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "each waypoint is labelled with its own ID");

		return new AirwaySettingsParseResult(settings, messages);
	}

	/// <summary>
	/// Whether a file that gets CRC-ERAM defaults needs <paramref name="altitudeClass"/>'s defaults
	/// for <paramref name="kind"/>.
	/// </summary>
	/// <remarks>
	/// A High/Low file holds one class, so only its own class is needed. A designation file can
	/// hold every class (its isDefaults Feature takes the majority class, the rest are written as
	/// per-feature overrides), so any designation file of that kind needs all three.
	/// </remarks>
	private static bool NeedsCrcDefaults(
		VnasFileChoices vnas,
		AirwayGeojsonOutputBy outputBy,
		AirwayAltitudeClass altitudeClass,
		CrcFeatureKind kind) =>
		outputBy == AirwayGeojsonOutputBy.HighLow
			? vnas.HasCrcDefaults(AirwayOutputFiles.GeojsonKey(altitudeClass.ToString(), kind))
			: vnas.CrcDefaultsFiles.Any(key => AirwayOutputFiles.IsKind(key, kind));
}
