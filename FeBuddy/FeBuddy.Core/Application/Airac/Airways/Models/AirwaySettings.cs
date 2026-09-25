using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// Fully-parsed, typed settings for the Airways services (GeoJSON generation and alias file
/// generation). Built once by <c>AirwaySettingsParser.Parse</c> from the raw
/// <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>) supplies; every
/// other Airways service consumes this typed object, never the raw dictionary.
/// </summary>
public sealed record AirwaySettings
{
	/// <summary>Directory the Airways services write their output under.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>How to group airways into GeoJSON files. <see cref="AirwayGeojsonOutputBy.None"/> generates no GeoJSON.</summary>
	public required AirwayGeojsonOutputBy OutputBy { get; init; }

	/// <summary>
	/// When <see langword="true"/>, each airway leg is shortened by a fixed radius around its
	/// endpoint waypoints (2.5 NM for 5-character fixes, 5 NM otherwise) so lines stop short
	/// of waypoint symbols/text.
	/// </summary>
	public required bool BufferAirwayWaypoints { get; init; }

	/// <summary>When <see langword="true"/>, Features carry the <c>feb.*</c> properties listed in <see cref="FebProperties"/>.</summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is <see langword="true"/>.</summary>
	public IReadOnlyCollection<AirwayFebProperty> FebProperties { get; init; } = [];

	/// <summary>Whether to write the <c>Airways.txt</c> alias file.</summary>
	public required bool GenerateAliasFile { get; init; }

	/// <summary>Which airways the <c>Airways.txt</c> alias file covers.</summary>
	public AliasRoiScope AliasRoiScope { get; init; } = AliasRoiScope.All;

	/// <summary>Whether to split airway geometry at the antimeridian.</summary>
	public required bool SplitAtAntimeridian { get; init; }

	/// <summary>
	/// Designations (from <see cref="Airway.Designation"/>, i.e. derived from <c>AWY_ID</c>) to
	/// drop entirely - before any geometry work, so GeoJSON and the alias file agree.
	/// Case-insensitive, upper-cased.
	/// </summary>
	public IReadOnlyCollection<string> ExcludedDesignations { get; init; } = [];

	/// <summary>Emit the <c>_Lines</c> GeoJSON files. Default <see langword="true"/>.</summary>
	public bool EmitLines { get; init; } = true;

	/// <summary>Emit the <c>_Symbols</c> GeoJSON files. Default <see langword="true"/>.</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit the <c>_Text</c> GeoJSON files. Default <see langword="true"/>.</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>
	/// Maximum decimal places for coordinates written to GeoJSON.
	/// Default 6.
	/// </summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// When <see langword="true"/> (default), output is written under a <c>FE-Buddy_Output</c>
	/// folder inside <see cref="OutputDirectory"/>; when <see langword="false"/>, straight into
	/// <see cref="OutputDirectory"/>. The <c>Airways</c> sub-folder is kept either way.
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

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

	/// <summary>The Region of Interest to filter and clip output to, or <see langword="null"/> for no ROI filtering.</summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>CRC line property defaults, keyed by altitude class. Populated when <see cref="IncludeCrcLineDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcLineDefaults>();

	/// <summary>CRC symbol property defaults, keyed by altitude class. Populated when <see cref="IncludeCrcSymbolDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcSymbolDefaults> SymbolDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcSymbolDefaults>();

	/// <summary>
	/// CRC text property defaults, keyed by altitude class. Populated when <see cref="IncludeCrcTextDefaults"/> is <see langword="true"/>.
	/// </summary>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcTextDefaults> TextDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcTextDefaults>();
}
