using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Procedures sub-service into a typed, validated <see cref="ProcedureSettings"/>.
/// </summary>
/// <remarks>
/// The only place in the Procedures sub-service that touches the raw dictionary. Unlike every
/// other AIRAC sub-service, there is no GeoJSON and no <c>feb.*</c> properties -
/// <c>IncludeFebCustomProperties</c> is read only so it can say so. The only file
/// <c>UploadToVnas</c> may name is the alias file.
/// </remarks>
public static class ProcedureSettingsParser
{
	private const string LogSource = "ProcedureSettingsParser";

	/// <summary>
	/// The <c>JsonFields</c> chosen when the setting is absent or blank. Public so the Procedures
	/// GUI tab can default its Procedures.json Fields card's ticks to the same set, rather than
	/// keeping its own copy that could drift from this one.
	/// </summary>
	public static readonly IReadOnlyList<ProcedureJsonField> DefaultJsonFields =
	[
		ProcedureJsonField.IcaoId,
		ProcedureJsonField.AirportName,
		ProcedureJsonField.ResponsibleArtcc,
		ProcedureJsonField.AirspaceClass,
		ProcedureJsonField.ChartType,
		ProcedureJsonField.ChartUrl,
		ProcedureJsonField.Change,
		ProcedureJsonField.CompareUrl,
	];

	/// <summary>The keys only Procedures reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"GenerateChangesDocument", "GenerateProceduresJson", "GenerateAliasFile",
		"Facilities", "PrimaryFacility",
		"IncludeRoiAirports",
		"Airports", "Procedures", "AirportProcedures",
		"ChartTypes", "JsonFields",
	};

	/// <summary>Procedures writes no GeoJSON, so it draws from no CRC-ERAM defaults class at all.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses and validates <paramref name="procedureSettings"/> into a typed
	/// <see cref="ProcedureSettings"/>.
	/// </summary>
	/// <param name="procedureSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing, a value is invalid, or the combination of
	/// choices would produce no output or select no airport or procedure at all.
	/// </exception>
	public static ProcedureSettingsParseResult Parse(IReadOnlyDictionary<string, string> procedureSettings)
	{
		ArgumentNullException.ThrowIfNull(procedureSettings);

		string outputDirectory = SettingsValueReader.RequiredString(procedureSettings, "OutputDirectory");

		bool generateChangesDocument = SettingsValueReader.YesNo(procedureSettings, "GenerateChangesDocument", defaultValue: true);
		bool generateProceduresJson = SettingsValueReader.YesNo(procedureSettings, "GenerateProceduresJson", defaultValue: true);
		bool generateAliasFile = SettingsValueReader.YesNo(procedureSettings, "GenerateAliasFile", defaultValue: true);

		if (!generateChangesDocument && !generateProceduresJson && !generateAliasFile)
		{
			throw new ArgumentException(
				"'GenerateChangesDocument', 'GenerateProceduresJson' and 'GenerateAliasFile' are all \"N\", so the Procedures " +
				"sub-service would produce nothing. Turn at least one back on, or deselect Procedures.");
		}

		// The selection settings below pick what the two documents cover; the alias file covers every
		// airport in the metafile whatever they say.
		bool generatesDocument = generateChangesDocument || generateProceduresJson;

		List<ServiceMessage> messages = [];

		// Procedures writes no GeoJSON, so it has no feb.* properties to offer at all - JsonFields
		// covers the JSON document's optional fields instead.
		if (SettingsValueReader.YesNo(procedureSettings, "IncludeFebCustomProperties", defaultValue: false))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"'IncludeFebCustomProperties' is \"Y\", but Procedures has no FE-Buddy GeoJSON properties to add " +
				"(see 'JsonFields' for Procedures.json's optional fields instead), so it was ignored."));
		}

		IReadOnlyCollection<string> facilities = [.. SettingsValueReader.StringList(procedureSettings, "Facilities")
			.Select(f => f.Trim().ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)];

		string? primaryFacility = SettingsValueReader.OptionalString(procedureSettings, "PrimaryFacility")?.ToUpperInvariant();

		bool includeRoiAirports = SettingsValueReader.YesNo(procedureSettings, "IncludeRoiAirports", defaultValue: false);
		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(procedureSettings);

		if (generatesDocument && includeRoiAirports && roi is null)
		{
			throw new ArgumentException(
				"'IncludeRoiAirports' is \"Y\" but no Region of Interest is set. Set 'FilterByRoi' to \"Y\" and its four " +
				"corner keys ('RoiSwLat', 'RoiSwLon', 'RoiNeLat', 'RoiNeLon').");
		}

		IReadOnlyCollection<string> airports = [.. SettingsValueReader.StringList(procedureSettings, "Airports")];
		IReadOnlyCollection<string> procedures = [.. SettingsValueReader.StringList(procedureSettings, "Procedures")];
		IReadOnlyList<ProcedureAirportPick> airportProcedures = ParseAirportProcedures(procedureSettings);

		if (generatesDocument
			&& facilities.Count == 0 && !includeRoiAirports && airports.Count == 0 && procedures.Count == 0 && airportProcedures.Count == 0)
		{
			throw new ArgumentException(
				"Procedures has no inclusion source: set 'Facilities', turn on 'IncludeRoiAirports' (with a Region of " +
				"Interest), or list at least one 'Airports', 'Procedures' or 'AirportProcedures' entry.");
		}

		(IReadOnlyCollection<string> chartTypes, IReadOnlyList<ServiceMessage> chartTypeMessages) = ParseChartTypes(procedureSettings);
		messages.AddRange(chartTypeMessages);

		(IReadOnlyCollection<ProcedureJsonField> jsonFields, IReadOnlyList<ServiceMessage> jsonFieldMessages) = ParseJsonFields(procedureSettings);
		messages.AddRange(jsonFieldMessages);

		// Nothing Procedures writes is GeoJSON, so the alias file is the only key UploadToVnas may
		// name, and CrcDefaultsFor none at all.
		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			procedureSettings, ProcedureOutputFiles.Alias, isGeojsonFileKey: _ => false, example: ProcedureOutputFiles.Alias);

		messages.AddRange(SubServiceSettingsReader.UnknownKeyWarnings(
			procedureSettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "Procedures writes no GeoJSON, so it has no per-feature CRC output"));

		ProcedureSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			GenerateChangesDocument = generateChangesDocument,
			GenerateProceduresJson = generateProceduresJson,
			GenerateAliasFile = generateAliasFile,
			Vnas = vnas,
			Facilities = facilities,
			PrimaryFacility = primaryFacility,
			IncludeRoiAirports = includeRoiAirports,
			Roi = roi,
			Airports = airports,
			Procedures = procedures,
			AirportProcedures = airportProcedures,
			ChartTypes = chartTypes,
			JsonFields = jsonFields,
		};

		return new ProcedureSettingsParseResult(settings, messages);
	}

	/// <summary>Parses <c>AirportProcedures</c>: comma-separated <c>APT|PROCEDURE NAME</c> entries.</summary>
	private static IReadOnlyList<ProcedureAirportPick> ParseAirportProcedures(IReadOnlyDictionary<string, string> procedureSettings)
	{
		List<ProcedureAirportPick> picks = [];

		foreach (string entry in SettingsValueReader.StringList(procedureSettings, "AirportProcedures"))
		{
			string[] parts = entry.Split('|');

			if (parts.Length != 2 || parts[0].Trim().Length == 0 || parts[1].Trim().Length == 0)
			{
				throw new ArgumentException(
					$"'AirportProcedures' entry '{entry}' is not valid. Each entry must be an airport identifier and a " +
					"procedure name joined by exactly one '|', e.g. \"PIT|ILS OR LOC RWY 28C\".");
			}

			picks.Add(new ProcedureAirportPick(parts[0].Trim(), parts[1].Trim()));
		}

		return picks;
	}

	/// <summary>Parses <c>ChartTypes</c>, defaulting to <see cref="ProcedureChartTypes.Default"/> when absent or blank.</summary>
	private static (IReadOnlyCollection<string> ChartTypes, IReadOnlyList<ServiceMessage> Messages) ParseChartTypes(
		IReadOnlyDictionary<string, string> procedureSettings)
	{
		IReadOnlyList<string> raw = SettingsValueReader.StringList(procedureSettings, "ChartTypes");

		if (raw.Count == 0)
		{
			return (ProcedureChartTypes.Default, []);
		}

		List<string> chartTypes = [.. raw
			.Select(type => type.Trim().ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)];

		List<ServiceMessage> messages = [.. chartTypes
			.Where(type => !ProcedureChartTypes.IsKnown(type))
			.Select(type => new ServiceMessage(LogLevel.Warning, LogSource,
				$"'ChartTypes' entry '{type}' is not a chart type FE-Buddy knows ({string.Join(", ", ProcedureChartTypes.All)}). " +
				"It is still used."))];

		return (chartTypes, messages);
	}

	/// <summary>Parses <c>JsonFields</c>, defaulting to <see cref="DefaultJsonFields"/> when absent or blank.</summary>
	private static (IReadOnlyCollection<ProcedureJsonField> Fields, IReadOnlyList<ServiceMessage> Messages) ParseJsonFields(
		IReadOnlyDictionary<string, string> procedureSettings)
	{
		IReadOnlyList<string> raw = SettingsValueReader.StringList(procedureSettings, "JsonFields");

		if (raw.Count == 0)
		{
			return (DefaultJsonFields, []);
		}

		List<ProcedureJsonField> fields = [];
		List<ServiceMessage> messages = [];

		foreach (string name in raw)
		{
			if (FebProperties.TryParse(name, out ProcedureJsonField field))
			{
				if (!fields.Contains(field))
				{
					fields.Add(field);
				}
			}
			else
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"'JsonFields' entry '{name}' is not a known field. Valid values: " +
					string.Join(", ", FebProperties.AllNames<ProcedureJsonField>()) + ". It was ignored."));
			}
		}

		return (fields, messages);
	}
}
