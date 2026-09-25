using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.DatToGeojson;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the DAT to GeoJSON conversion into a typed, validated <see cref="DatToGeojsonSettings"/>.
/// </summary>
public static class DatToGeojsonSettingsParser
{
	/// <summary>
	/// What separates the paths in <c>SourceFiles</c>. A comma cannot be used, as it is legal in
	/// a Windows path; <c>|</c> is not.
	/// </summary>
	public const char SourceFileSeparator = '|';

	/// <summary>The CRC defaults class every converted file's Line defaults are keyed under: <c>Crc.VideoMap.Line.*</c>.</summary>
	public const string CrcClassName = "VideoMap";

	/// <summary>The largest cropping distance accepted, in nautical miles - far beyond any facility map.</summary>
	public const double MaxCroppingDistanceNm = 1000;

	private const string LogSource = "DatToGeojsonSettingsParser";

	/// <summary>The keys only this conversion reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"SourceFolder", "SourceFiles", "CroppingDistance",
	};

	/// <summary>A video map is lines only.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[CrcClassName] = [CrcFeatureKind.Line],
		};

	/// <summary>
	/// Parses and validates <paramref name="settings"/> into a typed <see cref="DatToGeojsonSettings"/>.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or a value is invalid, or when the source is not
	/// exactly one of a folder or a list of files.
	/// </exception>
	public static DatToGeojsonSettingsParseResult Parse(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string outputDirectory = SettingsValueReader.RequiredString(settings, "OutputDirectory");

		string? sourceFolder = SettingsValueReader.OptionalString(settings, "SourceFolder");
		string[] sourceFiles = SettingsValueReader.OptionalString(settings, "SourceFiles")
			?.Split(SourceFileSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			?? [];

		bool hasFolder = sourceFolder is not null;
		bool hasFiles = sourceFiles.Length > 0;

		if (hasFolder == hasFiles)
		{
			throw new ArgumentException(
				"Give either 'SourceFolder' (convert every .dat file in a folder) or 'SourceFiles' " +
				$"(the .dat files to convert, separated by '{SourceFileSeparator}'), but not both.");
		}

		double? croppingDistance = SettingsValueReader.OptionalPositiveDecimal(settings, "CroppingDistance", MaxCroppingDistanceNm);

		// Only a ticked Include box makes the Line defaults required.
		bool includeLineDefaults = CrcDefaultsReader.ReadInclude(settings, CrcFeatureKind.Line);
		CrcLineDefaults? lineDefaults = includeLineDefaults
			? CrcDefaultsReader.ReadLine(settings, $"Crc.{CrcClassName}.Line")
			: null;

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			settings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "a video map has no labels");

		DatToGeojsonSettings parsed = new()
		{
			OutputDirectory = outputDirectory,
			AddFeBuddyOutputFolder = SettingsValueReader.YesNo(settings, "AddFeBuddyOutputFolder", defaultValue: true),
			CoordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(settings),
			SourceFolder = sourceFolder,
			SourceFiles = sourceFiles,
			CroppingDistanceNm = croppingDistance,
			IncludeCrcLineDefaults = includeLineDefaults,
			LineDefaults = lineDefaults,
		};

		return new DatToGeojsonSettingsParseResult(parsed, messages);
	}
}
