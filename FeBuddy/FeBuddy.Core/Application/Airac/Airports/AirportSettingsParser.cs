using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Airports;

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

	/// <summary>The keys only Airports reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateGeojson", "EmitAirportSymbols", "EmitAirportText", "EmitRunwayLines", "GenerateAliasFile",
	};

	/// <summary>Airports draws points (Symbol and Text); Runways draws lines.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[nameof(AirportCrcClass.Airports)] = [CrcFeatureKind.Symbol, CrcFeatureKind.Text],
			[nameof(AirportCrcClass.Runways)] = [CrcFeatureKind.Line],
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
	public static AirportSettingsParseResult Parse(IReadOnlyDictionary<string, string> airportSettings)
	{
		ArgumentNullException.ThrowIfNull(airportSettings);

		string outputDirectory = SettingsValueReader.RequiredString(airportSettings, "OutputDirectory");

		bool generateGeojson = SettingsValueReader.YesNo(airportSettings, "GenerateGeojson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(airportSettings, "GenerateAliasFile", defaultValue: true);

		// Selecting the sub-service and then turning off both of its outputs asks for a run
		// that writes nothing. The GUI blocks this on the General tab; the parser is the backstop
		// for the harness and for a hand-edited UserConfig.
		if (!generateGeojson && !generateAliasFile)
		{
			throw new ArgumentException(
				"GenerateGeojson and GenerateAliasFile are both \"N\", so the Airports sub-service would produce nothing. " +
				"Turn one back on, or leave Airports out of the run.");
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

		(bool includeFebProperties, IReadOnlyList<AirportFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<AirportFebProperty>(airportSettings, example: "faaId,icaoId,elev");

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(airportSettings);
		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(airportSettings);

		CrcDefaultsFiles crcFiles = SubServiceSettingsReader.ReadCrcDefaultsFiles(
			airportSettings, AirportOutputFiles.IsGeojsonKey,
			example: $"{AirportOutputFiles.AirportsSymbols}, {AirportOutputFiles.RunwaysLines}");

		// A file's defaults are needed only when it gets CRC-ERAM defaults AND is actually
		// written; only then are its values required.
		Dictionary<AirportCrcClass, CrcLineDefaults> lineDefaults = [];
		Dictionary<AirportCrcClass, CrcSymbolDefaults> symbolDefaults = [];
		Dictionary<AirportCrcClass, CrcTextDefaults> textDefaults = [];

		if (generateGeojson && emitSymbols && crcFiles.HasCrcDefaults(AirportOutputFiles.AirportsSymbols))
		{
			symbolDefaults[AirportCrcClass.Airports] =
				CrcDefaultsReader.ReadSymbol(airportSettings, $"Crc.{AirportCrcClass.Airports}.Symbol");
		}

		if (generateGeojson && emitText && crcFiles.HasCrcDefaults(AirportOutputFiles.AirportsText))
		{
			textDefaults[AirportCrcClass.Airports] =
				CrcDefaultsReader.ReadText(airportSettings, $"Crc.{AirportCrcClass.Airports}.Text");
		}

		if (generateGeojson && emitRunways && crcFiles.HasCrcDefaults(AirportOutputFiles.RunwaysLines))
		{
			lineDefaults[AirportCrcClass.Runways] =
				CrcDefaultsReader.ReadLine(airportSettings, $"Crc.{AirportCrcClass.Runways}.Line");
		}

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			airportSettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "an airport's label is always built from its identifier and name");

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
			CrcDefaultsFiles = crcFiles,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			LineDefaults = lineDefaults,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults
		};

		return new AirportSettingsParseResult(settings, messages);
	}
}
