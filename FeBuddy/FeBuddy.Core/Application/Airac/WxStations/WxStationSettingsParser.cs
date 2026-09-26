using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.WxStations;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Wx Stations sub-service into a typed, validated <see cref="WxStationSettings"/>.
/// </summary>
/// <remarks>
/// The simplest AIRAC sub-service settings parser: there is no <c>OutputBy</c> (one merged file
/// pair), no alias file, and no <c>feb.*</c> properties - a station's label is always its ICAO ID
/// and (when it has one) its IATA ID and site name, never a chosen set of properties.
/// </remarks>
public static class WxStationSettingsParser
{
	private const string LogSource = "WxStationSettingsParser";

	/// <summary>The keys only Wx Stations reads, on top of <see cref="SubServiceSettingsReader.CommonKeys"/>.</summary>
	private static readonly IReadOnlySet<string> OwnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"EmitSymbols", "EmitText",
	};

	/// <summary>
	/// The one CRC-defaults class Wx Stations ever draws from: <see cref="WxStationOutputFiles.AllClass"/>,
	/// with Symbol and Text output.
	/// </summary>
	private static readonly IReadOnlyDictionary<string, CrcFeatureKind[]> CrcKindsByClass =
		new Dictionary<string, CrcFeatureKind[]>(StringComparer.OrdinalIgnoreCase)
		{
			[WxStationOutputFiles.AllClass] = [CrcFeatureKind.Symbol, CrcFeatureKind.Text],
		};

	/// <summary>
	/// Parses and validates <paramref name="wxStationSettings"/> into a typed
	/// <see cref="WxStationSettings"/>.
	/// </summary>
	/// <param name="wxStationSettings">The raw settings dictionary.</param>
	/// <returns>The typed settings plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">Thrown when a required setting is missing or invalid.</exception>
	public static WxStationSettingsParseResult Parse(IReadOnlyDictionary<string, string> wxStationSettings)
	{
		ArgumentNullException.ThrowIfNull(wxStationSettings);

		string outputDirectory = SettingsValueReader.RequiredString(wxStationSettings, "OutputDirectory");

		bool emitSymbols = SettingsValueReader.YesNo(wxStationSettings, "EmitSymbols", defaultValue: true);
		bool emitText = SettingsValueReader.YesNo(wxStationSettings, "EmitText", defaultValue: true);

		// GeoJSON is the only output the Wx Stations sub-service has, so turning both off would
		// produce nothing at all.
		if (!emitSymbols && !emitText)
		{
			throw new ArgumentException(
				"'EmitSymbols' and 'EmitText' are both \"N\", so the Wx Stations sub-service would produce nothing. " +
				"Turn at least one back on, or deselect Wx Stations.");
		}

		List<ServiceMessage> messages = [];

		// IncludeFebCustomProperties/FebProperties are shared keys every sub-service parser reads,
		// but Wx Stations has no feb.* properties to offer - a station's label is always its ICAO
		// ID, IATA ID and site name.
		if (SettingsValueReader.YesNo(wxStationSettings, "IncludeFebCustomProperties", defaultValue: false))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				"'IncludeFebCustomProperties' is \"Y\", but Wx Stations has no FE-Buddy properties to add, so it was ignored."));
		}

		RegionOfInterest? roi = SubServiceSettingsReader.ReadRoi(wxStationSettings);
		int coordinatePrecision = SubServiceSettingsReader.ReadCoordinatePrecision(wxStationSettings);

		// No alias file, so aliasFileKey is null: no UploadToVnas/CrcDefaultsFor entry can name one.
		VnasFileChoices vnas = SubServiceSettingsReader.ReadVnasFiles(
			wxStationSettings, aliasFileKey: null, WxStationOutputFiles.IsGeojsonKey,
			example: $"{WxStationOutputFiles.Symbols}, {WxStationOutputFiles.Text}");

		Dictionary<string, CrcSymbolDefaults> symbolDefaults = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, CrcTextDefaults> textDefaults = new(StringComparer.OrdinalIgnoreCase);

		if (emitSymbols && vnas.HasCrcDefaults(WxStationOutputFiles.Symbols))
		{
			symbolDefaults[WxStationOutputFiles.AllClass] = CrcDefaultsReader.ReadSymbol(wxStationSettings, $"Crc.{WxStationOutputFiles.AllClass}.Symbol");
		}

		if (emitText && vnas.HasCrcDefaults(WxStationOutputFiles.Text))
		{
			textDefaults[WxStationOutputFiles.AllClass] = CrcDefaultsReader.ReadText(wxStationSettings, $"Crc.{WxStationOutputFiles.AllClass}.Text");
		}

		messages.AddRange(SubServiceSettingsReader.UnknownKeyWarnings(
			wxStationSettings, OwnKeys, CrcKindsByClass, LogSource,
			labelSource: "a station's label is always its ICAO ID, then its IATA ID and name"));

		WxStationSettings settings = new()
		{
			OutputDirectory = outputDirectory,
			EmitSymbols = emitSymbols,
			EmitText = emitText,
			Vnas = vnas,
			Roi = roi,
			CoordinatePrecision = coordinatePrecision,
			SymbolDefaults = symbolDefaults,
			TextDefaults = textDefaults,
		};

		return new WxStationSettingsParseResult(settings, messages);
	}
}
