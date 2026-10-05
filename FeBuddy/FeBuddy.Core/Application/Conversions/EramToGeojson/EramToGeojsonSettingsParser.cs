using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions.EramToGeojson;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the ERAM to GeoJSON conversion into a typed, validated <see cref="EramToGeojsonSettings"/>.
/// </summary>
/// <remarks>
/// <c>OutputLayout</c> is <c>ByFilters</c>, <c>ByAttributes</c>, <c>Raw</c> or <c>RawPlus</c>.
/// </remarks>
public static class EramToGeojsonSettingsParser
{
	/// <summary>
	/// The CRC defaults class the tab's defaults are keyed under: <c>Crc.GeoMap.Line.*</c>,
	/// <c>Crc.GeoMap.Symbol.*</c> and <c>Crc.GeoMap.Text.*</c>.
	/// </summary>
	public const string CrcClassName = "GeoMap";

	private const string LogSource = "EramToGeojsonSettingsParser";

	/// <summary>
	/// The keys only this conversion reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>
	/// and <see cref="ConversionSettingsReader.ConversionKeys"/>.
	/// </summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(
		ConversionSettingsReader.ConversionKeys.Concat(["OutputLayout", "DefaultsSource"]), StringComparer.OrdinalIgnoreCase);


	/// <summary>A GeoMap draws lines, symbols and text.</summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[CrcClassName] = Enum.GetValues<CrcFeatureKind>(),
		};

	/// <summary>
	/// Parses and validates <paramref name="settings"/> into a typed <see cref="EramToGeojsonSettings"/>.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a required setting is missing or a value is invalid, or when the source is not
	/// exactly one of a folder or a list of files.
	/// </exception>
	public static EramToGeojsonSettingsParseResult Parse(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string outputDirectory = ConversionSettingsReader.ReadOutputDirectory(settings);
		(string? sourceFolder, IReadOnlyList<string> sourceFiles) = ConversionSettingsReader.ReadSource(settings);

		IReadOnlyList<ServiceMessage> messages = SubServiceSettingsReader.UnknownKeyWarnings(
			settings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "each text keeps its own text from the GeoMap");

		EramOutputLayout layout = SettingsValueReader.OptionalEnum(
			settings, "OutputLayout", EramOutputLayout.ByAttributes,
			hint: "Use \"ByFilters\" (a folder per set of filters), \"ByAttributes\" (a file per shared look), \"Raw\" (one file per map) " +
				"or \"RawPlus\" (one file per map, matching lines joined and symbols grouped).");

		EramDefaultsSource source = SettingsValueReader.OptionalEnum(
			settings, "DefaultsSource", EramDefaultsSource.Xml,
			hint: "Use \"Xml\" (carry over the XML's defaults), \"XmlThenCard\" (the tab's defaults where the XML gives none) or \"Card\" (the tab's defaults only).");

		// The tab's defaults are read only when they can be used, and then only the kinds whose
		// Include box is ticked.
		bool usesCard = source != EramDefaultsSource.Xml;

		(bool includeFeb, IReadOnlyList<EramFebProperty> febProperties) =
			SubServiceSettingsReader.ReadFebProperties<EramFebProperty>(settings, example: "mapObjectType,lineObjectId,symbolId");

		EramToGeojsonSettings parsed = new()
		{
			OutputDirectory = outputDirectory,
			AddFeBuddyOutputFolder = ConversionSettingsReader.ReadAddFeBuddyOutputFolder(settings),
			CoordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(settings),
			SourceFolder = sourceFolder,
			SourceFiles = sourceFiles,
			OutputLayout = layout,
			DefaultsSource = source,
			LineDefaults = usesCard && ConversionSettingsReader.ReadCrcInclude(settings, CrcFeatureKind.Line)
				? CrcDefaultsReader.ReadLine(settings, $"Crc.{CrcClassName}.Line")
				: null,
			SymbolDefaults = usesCard && ConversionSettingsReader.ReadCrcInclude(settings, CrcFeatureKind.Symbol)
				? CrcDefaultsReader.ReadSymbol(settings, $"Crc.{CrcClassName}.Symbol")
				: null,
			TextDefaults = usesCard && ConversionSettingsReader.ReadCrcInclude(settings, CrcFeatureKind.Text)
				? CrcDefaultsReader.ReadText(settings, $"Crc.{CrcClassName}.Text")
				: null,
			IncludeFebProperties = includeFeb,
			FebProperties = febProperties,
		};

		return new EramToGeojsonSettingsParseResult(parsed, messages);
	}

}
