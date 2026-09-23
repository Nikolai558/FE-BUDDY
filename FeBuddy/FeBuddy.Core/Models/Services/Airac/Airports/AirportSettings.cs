using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Core.Models.Services.Airac.Airports;

/// <summary>
/// Fully-parsed, typed settings for the Airports sub-service (GeoJSON generation and alias file
/// generation). Built once by <c>AirportSettingsParser.Parse</c> from the raw
/// <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>) supplies; every
/// other Airports service consumes this typed object, never the raw dictionary.
/// </summary>
public sealed record AirportSettings
{
	/// <summary>Directory the Airports services write their output under.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Whether to write GeoJSON at all. <see langword="false"/> means alias output only.</summary>
	public required bool GenerateGeojson { get; init; }

	/// <summary>Emit <c>Airports_Symbols.geojson</c> - one Point per airport.</summary>
	public bool EmitAirportSymbols { get; init; } = true;

	/// <summary>Emit <c>Airports_Text.geojson</c> - one labelled Point per airport.</summary>
	public bool EmitAirportText { get; init; } = true;

	/// <summary>Emit <c>Runways_Lines.geojson</c> - one MultiLineString per airport with runways.</summary>
	public bool EmitRunwayLines { get; init; } = true;

	/// <summary>Whether to write the <c>Airports.txt</c> alias file.</summary>
	public required bool GenerateAliasFile { get; init; }

	/// <summary>
	/// When <see langword="true"/>, airport Features carry the <c>feb.*</c> properties listed
	/// in <see cref="FebProperties"/>.
	/// </summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>
	/// Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is
	/// <see langword="true"/>. The Text file always omits
	/// <see cref="AirportFebProperty.FaaId"/> and <see cref="AirportFebProperty.Name"/>, which
	/// its <c>text</c> array already carries.
	/// </summary>
	public IReadOnlyCollection<AirportFebProperty> FebProperties { get; init; } = Array.Empty<AirportFebProperty>();

	/// <summary>
	/// Whether the Line defaults (<see cref="LineDefaults"/>) are written as an isLineDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcLineDefaults { get; init; }

	/// <summary>
	/// Whether the Symbol defaults (<see cref="SymbolDefaults"/>) are written as an isSymbolDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcSymbolDefaults { get; init; }

	/// <summary>
	/// Whether the Text defaults (<see cref="TextDefaults"/>) are written as an isTextDefaults
	/// Feature. <see langword="true"/> only when the user asked for them and the file they belong
	/// to is being produced.
	/// </summary>
	public required bool IncludeCrcTextDefaults { get; init; }

	/// <summary>
	/// The Region of Interest the GeoJSON output is filtered to, or <see langword="null"/> for
	/// no filtering. An airport is in or out as a whole, tested on its reference point; the
	/// alias file ignores this entirely and always covers the full database.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// When <see langword="true"/> (default), output is written under a <c>FE-Buddy_Output</c>
	/// folder inside <see cref="OutputDirectory"/>; when <see langword="false"/>, straight into
	/// <see cref="OutputDirectory"/> (the <c>Airports</c> sub-folder is kept either way).
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>CRC line property defaults, keyed by class. Populated when <see cref="IncludeCrcLineDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcLineDefaults>();

	/// <summary>CRC symbol property defaults, keyed by class. Populated when <see cref="IncludeCrcSymbolDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcSymbolDefaults>();

	/// <summary>
	/// CRC text property defaults, keyed by class. Populated when <see cref="IncludeCrcTextDefaults"/> is <see langword="true"/>.
	/// </summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcTextDefaults>();
}
