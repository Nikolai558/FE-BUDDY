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

	/// <summary>The key listing the designations written to the High files only.</summary>
	public const string HighDesignationsKey = "HighDesignations";

	/// <summary>The key listing the designations written to the Low files only.</summary>
	public const string LowDesignationsKey = "LowDesignations";

	/// <summary>The key listing the designations written to both the High and the Low files.</summary>
	public const string BothDesignationsKey = "BothDesignations";

	/// <summary>The key for how far, in NM, a buffered line stops short of a 5-character fix.</summary>
	public const string FixBufferKey = "FixBufferNm";

	/// <summary>The key for how far, in NM, a buffered line stops short of any other waypoint.</summary>
	public const string NavaidBufferKey = "NavaidBufferNm";

	/// <summary>Each High/Low stratum and the key listing its designations.</summary>
	private static readonly (AirwayStratum Stratum, string Key)[] StratumKeys =
	[
		(AirwayStratum.High, HighDesignationsKey),
		(AirwayStratum.Low, LowDesignationsKey),
		(AirwayStratum.Both, BothDesignationsKey),
	];

	/// <summary>The keys only Airways reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"OutputBy", "BufferAirwayWaypoints", FixBufferKey, NavaidBufferKey, "SplitAtAntimeridian", "ExcludedDesignations",
		"EmitLines", "EmitSymbols", "EmitText", "AliasRoiScope", "GenerateAliasFile",
		HighDesignationsKey, LowDesignationsKey, BothDesignationsKey,
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

		CrcDefaultsFiles crcFiles = SubServiceSettingsReader.ReadCrcDefaultsFiles(
			airwaySettings, AirwayOutputFiles.IsGeojsonKey,
			example: $"{AirwayOutputFiles.GeojsonKey(nameof(AirwayAltitudeClass.High), CrcFeatureKind.Line)}, {AirwayOutputFiles.GeojsonKey(nameof(AirwayAltitudeClass.Low), CrcFeatureKind.Line)}");

		// A class's defaults are needed only when a file that gets CRC-ERAM defaults is actually
		// written and can hold that class; only then are its values required.
		Dictionary<AirwayAltitudeClass, CrcLineDefaults> lineDefaults = [];
		Dictionary<AirwayAltitudeClass, CrcSymbolDefaults> symbolDefaults = [];
		Dictionary<AirwayAltitudeClass, CrcTextDefaults> textDefaults = [];

		foreach (AirwayAltitudeClass altitudeClass in Enum.GetValues<AirwayAltitudeClass>())
		{
			if (writingGeojson && emitLines && NeedsCrcDefaults(crcFiles, outputBy, altitudeClass, CrcFeatureKind.Line))
				lineDefaults[altitudeClass] = CrcDefaultsReader.ReadLine(airwaySettings, $"Crc.{altitudeClass}.Line");

			if (writingGeojson && emitSymbols && NeedsCrcDefaults(crcFiles, outputBy, altitudeClass, CrcFeatureKind.Symbol))
				symbolDefaults[altitudeClass] = CrcDefaultsReader.ReadSymbol(airwaySettings, $"Crc.{altitudeClass}.Symbol");

			if (writingGeojson && emitText && NeedsCrcDefaults(crcFiles, outputBy, altitudeClass, CrcFeatureKind.Text))
				textDefaults[altitudeClass] = CrcDefaultsReader.ReadText(airwaySettings, $"Crc.{altitudeClass}.Text");
		}

		// The distances are read - and so can fail - only when buffered GeoJSON is actually written.
		bool buffer = SettingsValueReader.YesNo(airwaySettings, "BufferAirwayWaypoints", defaultValue: false);
		bool readsBufferDistances = writingGeojson && buffer;

		AirwaySettings settings = new()
		{
			OutputDirectory = SettingsValueReader.RequiredString(airwaySettings, "OutputDirectory"),
			OutputBy = outputBy,
			BufferAirwayWaypoints = buffer,
			FixBufferNm = readsBufferDistances
				? ReadBufferDistance(airwaySettings, FixBufferKey, AirwayWaypointBuffer.DefaultFixRadiusNm)
				: AirwayWaypointBuffer.DefaultFixRadiusNm,
			NavaidBufferNm = readsBufferDistances
				? ReadBufferDistance(airwaySettings, NavaidBufferKey, AirwayWaypointBuffer.DefaultNavaidRadiusNm)
				: AirwayWaypointBuffer.DefaultNavaidRadiusNm,
			IncludeFebCustomProperties = includeFebProperties,
			FebProperties = febProperties,
			GenerateAliasFile = SettingsValueReader.YesNo(airwaySettings, "GenerateAliasFile", defaultValue: true),
			SplitAtAntimeridian = SettingsValueReader.YesNo(airwaySettings, "SplitAtAntimeridian", defaultValue: true),
			CrcDefaultsFiles = crcFiles,
			Roi = SubServiceSettingsReader.ReadRoi(airwaySettings),
			ExcludedDesignations = SettingsValueReader.StringList(airwaySettings, "ExcludedDesignations")
				.Select(designation => designation.ToUpperInvariant())
				.ToHashSet(StringComparer.OrdinalIgnoreCase),
			DesignationStrata = ReadDesignationStrata(airwaySettings),
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

	/// <summary>Reads one buffer distance: 0 to <see cref="AirwayWaypointBuffer.MaxRadiusNm"/> NM, the default when absent or blank.</summary>
	/// <exception cref="ArgumentException">Thrown when the value is not a number in that range.</exception>
	private static double ReadBufferDistance(IReadOnlyDictionary<string, string> airwaySettings, string key, double defaultValue) =>
		SettingsValueReader.DecimalInRange(airwaySettings, key, defaultValue, minimum: 0, maximum: AirwayWaypointBuffer.MaxRadiusNm);

	/// <summary>
	/// Whether a file that gets CRC-ERAM defaults needs <paramref name="altitudeClass"/>'s defaults
	/// for <paramref name="kind"/>.
	/// </summary>
	/// <remarks>
	/// A High or Low file uses its own class's defaults for every airway in it, so only that class
	/// is needed - and never <see cref="AirwayAltitudeClass.Other"/>, which has no file. A
	/// designation file can hold every class (its isDefaults Feature takes the majority class, the
	/// rest are written as per-feature overrides), so any designation file of that kind needs all three.
	/// </remarks>
	private static bool NeedsCrcDefaults(
		CrcDefaultsFiles crcFiles,
		AirwayGeojsonOutputBy outputBy,
		AirwayAltitudeClass altitudeClass,
		CrcFeatureKind kind) =>
		outputBy == AirwayGeojsonOutputBy.HighLow
			? altitudeClass != AirwayAltitudeClass.Other && crcFiles.HasCrcDefaults(AirwayOutputFiles.GeojsonKey(altitudeClass.ToString(), kind))
			: crcFiles.Files.Any(key => AirwayOutputFiles.IsKind(key, kind));

	/// <summary>
	/// Reads which file each designation goes in: <see cref="HighDesignationsKey"/>,
	/// <see cref="LowDesignationsKey"/> and <see cref="BothDesignationsKey"/>, upper-cased. With none
	/// of the three keys in the block at all - a block written before they existed - the defaults
	/// apply (<see cref="AirwaySettings.DefaultDesignationStrata"/>); otherwise a designation in no
	/// list has no file.
	/// </summary>
	/// <exception cref="ArgumentException">Thrown when a designation is in more than one list.</exception>
	private static IReadOnlyDictionary<string, AirwayStratum> ReadDesignationStrata(IReadOnlyDictionary<string, string> airwaySettings)
	{
		if (!StratumKeys.Any(entry => airwaySettings.ContainsKey(entry.Key)))
		{
			return AirwaySettings.DefaultDesignationStrata;
		}

		Dictionary<string, AirwayStratum> strata = new(StringComparer.OrdinalIgnoreCase);

		foreach ((AirwayStratum stratum, string key) in StratumKeys)
		{
			foreach (string designation in SettingsValueReader.StringList(airwaySettings, key).Select(entry => entry.ToUpperInvariant()))
			{
				if (strata.TryGetValue(designation, out AirwayStratum earlier) && earlier != stratum)
				{
					throw new ArgumentException(
						$"Designation '{designation}' is in both {StratumKeys.First(entry => entry.Stratum == earlier).Key} and {key}. " +
						$"List it once: {BothDesignationsKey} writes it to the High and the Low files.");
				}

				strata[designation] = stratum;
			}
		}

		return strata;
	}
}
