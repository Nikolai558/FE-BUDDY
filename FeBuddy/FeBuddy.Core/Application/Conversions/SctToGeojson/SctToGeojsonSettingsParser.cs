using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.SctToGeojson;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the SCT2 to GeoJSON conversion into a typed, validated <see cref="SctToGeojsonSettings"/>.
/// </summary>
public static class SctToGeojsonSettingsParser
{
	/// <summary>
	/// The CRC defaults class every converted file's defaults are keyed under:
	/// <c>Crc.SectorFile.Line.*</c> and <c>Crc.SectorFile.Text.*</c>.
	/// </summary>
	public const string CrcClassName = "SectorFile";

	private const string LogSource = "SctToGeojsonSettingsParser";

	/// <summary>A sector file converts to lines and labels; nothing is drawn as a symbol.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[CrcClassName] = [CrcFeatureKind.Line, CrcFeatureKind.Text],
		};

	/// <summary>
	/// Parses and validates <paramref name="settings"/> into a typed <see cref="SctToGeojsonSettings"/>.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or a value is invalid, or when the source is not
	/// exactly one of a folder or a list of files.
	/// </exception>
	public static SctToGeojsonSettingsParseResult Parse(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string outputDirectory = ConversionSettingsReader.ReadOutputDirectory(settings);
		(string? sourceFolder, IReadOnlyList<string> sourceFiles) = ConversionSettingsReader.ReadSource(settings);

		// Only a ticked Include box makes that kind's defaults required.
		bool includeLineDefaults = CrcDefaultsReader.ReadInclude(settings, CrcFeatureKind.Line);
		bool includeTextDefaults = CrcDefaultsReader.ReadInclude(settings, CrcFeatureKind.Text);

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			settings, ConversionSettingsReader.SourceKeys, CrcKindsByClass, LogSource,
			labelSource: "each label keeps its own text from the sector file");

		SctToGeojsonSettings parsed = new()
		{
			OutputDirectory = outputDirectory,
			AddFeBuddyOutputFolder = ConversionSettingsReader.ReadAddFeBuddyOutputFolder(settings),
			CoordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(settings),
			SourceFolder = sourceFolder,
			SourceFiles = sourceFiles,
			IncludeCrcLineDefaults = includeLineDefaults,
			LineDefaults = includeLineDefaults ? CrcDefaultsReader.ReadLine(settings, $"Crc.{CrcClassName}.Line") : null,
			IncludeCrcTextDefaults = includeTextDefaults,
			TextDefaults = includeTextDefaults ? CrcDefaultsReader.ReadText(settings, $"Crc.{CrcClassName}.Text") : null,
		};

		return new SctToGeojsonSettingsParseResult(parsed, messages);
	}
}
