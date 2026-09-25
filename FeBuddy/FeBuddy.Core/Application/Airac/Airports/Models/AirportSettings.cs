using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Airports.Models;

/// <summary>
/// Fully-parsed, typed settings for the Airports sub-service (GeoJSON generation and alias file
/// generation). Built once by <c>AirportSettingsParser.Parse</c> from the raw
/// <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>) supplies; every
/// other Airports service consumes this typed object, never the raw dictionary.
/// </summary>
public sealed record AirportSettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="AiracOutputPaths"/>.
	/// </summary>
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
	public IReadOnlyCollection<AirportFebProperty> FebProperties { get; init; } = [];

	/// <summary>
	/// Which files go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="AirportOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The Region of Interest the GeoJSON output is filtered to, or <see langword="null"/> for
	/// no filtering. An airport is in or out as a whole, tested on its reference point; the
	/// alias file ignores this entirely and always covers the full database.
	/// </summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>CRC line property defaults, keyed by class. Populated when <c>Runways_Lines</c> gets CRC-ERAM defaults.</summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcLineDefaults>();

	/// <summary>CRC symbol property defaults, keyed by class. Populated when <c>Airports_Symbols</c> gets CRC-ERAM defaults.</summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcSymbolDefaults>();

	/// <summary>
	/// CRC text property defaults, keyed by class. Populated when <c>Airports_Text</c> gets CRC-ERAM defaults.
	/// </summary>
	public IReadOnlyDictionary<AirportCrcClass, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<AirportCrcClass, CrcTextDefaults>();
}
