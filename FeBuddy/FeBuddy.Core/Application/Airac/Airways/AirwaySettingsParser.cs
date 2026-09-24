using FeBuddy.Core.Application.Airac.Airways.Models;
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

		// Each kind's defaults are written only when the user asked for them AND that file is
		// produced; only then are its values required.
		bool includeLineDefaults = CrcDefaultsReader.ReadInclude(airwaySettings, CrcFeatureKind.Line) && writingGeojson && emitLines;
		bool includeSymbolDefaults = CrcDefaultsReader.ReadInclude(airwaySettings, CrcFeatureKind.Symbol) && writingGeojson && emitSymbols;
		bool includeTextDefaults = CrcDefaultsReader.ReadInclude(airwaySettings, CrcFeatureKind.Text) && writingGeojson && emitText;

		Dictionary<AirwayAltitudeClass, CrcLineDefaults> lineDefaults = new();
		Dictionary<AirwayAltitudeClass, CrcSymbolDefaults> symbolDefaults = new();
		Dictionary<AirwayAltitudeClass, CrcTextDefaults> textDefaults = new();

		foreach (AirwayAltitudeClass altitudeClass in Enum.GetValues<AirwayAltitudeClass>())
		{
			if (includeLineDefaults)
				lineDefaults[altitudeClass] = CrcDefaultsReader.ReadLine(airwaySettings, $"Crc.{altitudeClass}.Line");

			if (includeSymbolDefaults)
				symbolDefaults[altitudeClass] = CrcDefaultsReader.ReadSymbol(airwaySettings, $"Crc.{altitudeClass}.Symbol");

			if (includeTextDefaults)
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
			IncludeCrcLineDefaults = includeLineDefaults,
			IncludeCrcSymbolDefaults = includeSymbolDefaults,
			IncludeCrcTextDefaults = includeTextDefaults,
			Roi = SubServiceSettingsReader.ReadRoi(airwaySettings),
			ExcludedDesignations = SettingsValueReader.StringList(airwaySettings, "ExcludedDesignations")
				.Select(designation => designation.ToUpperInvariant())
				.ToHashSet(StringComparer.OrdinalIgnoreCase),
			EmitLines = emitLines,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			AliasRoiScope = SettingsValueReader.OptionalEnum(airwaySettings, "AliasRoiScope", AliasRoiScope.All),
			CoordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(airwaySettings),
			AddFeBuddyOutputFolder = SettingsValueReader.YesNo(airwaySettings, "AddFeBuddyOutputFolder", defaultValue: true),
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			airwaySettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "each waypoint is labelled with its own ID");

		return new AirwaySettingsParseResult(settings, messages);
	}
}
