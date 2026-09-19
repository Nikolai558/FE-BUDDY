using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Core.Models.Services.Airac.Airways;

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

	/// <summary>
	/// When <see langword="true"/>, Lines features carry <c>"feb.AwyId"</c>.
	/// </summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>
	/// When <see langword="true"/> (and <see cref="IncludeFebCustomProperties"/> is also
	/// <see langword="true"/>), Lines features also carry <c>"feb.AwyWaypoints"</c>: the
	/// airway's unique ordered waypoint ID list.
	/// </summary>
	public required bool IncludeAirwayWaypointIds { get; init; }

	/// <summary>Whether to write the <c>Airways.txt</c> alias file.</summary>
	public required bool GenerateAliasFile { get; init; }

	/// <summary>Which airways the <c>Airways.txt</c> alias file covers (remediation plan 3.5).</summary>
	public AliasRoiScope AliasRoiScope { get; init; } = AliasRoiScope.All;

	/// <summary>Whether to split airway geometry at the antimeridian.</summary>
	public required bool SplitAtAntimeridian { get; init; }

	/// <summary>
	/// Designations (from <see cref="Airway.Designation"/>, i.e. derived from <c>AWY_ID</c>) to
	/// drop entirely - before any geometry work, so GeoJSON and the alias file agree
	/// (remediation plan 3.3). Case-insensitive, upper-cased.
	/// </summary>
	public IReadOnlyCollection<string> ExcludedDesignations { get; init; } = Array.Empty<string>();

	/// <summary>Emit the <c>_Lines</c> GeoJSON files. Default <see langword="true"/> (remediation plan 3.4).</summary>
	public bool EmitLines { get; init; } = true;

	/// <summary>Emit the <c>_Symbols</c> GeoJSON files. Default <see langword="true"/> (remediation plan 3.4).</summary>
	public bool EmitSymbols { get; init; } = true;

	/// <summary>Emit the <c>_Text</c> GeoJSON files. Default <see langword="true"/> (remediation plan 3.4).</summary>
	public bool EmitText { get; init; } = true;

	/// <summary>
	/// Maximum decimal places for coordinates written to GeoJSON (remediation plan 3.6).
	/// Default 6.
	/// </summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// When <see langword="true"/> (default), output is written under a <c>FE-Buddy_Output</c>
	/// folder inside <see cref="OutputDirectory"/>; when <see langword="false"/>, straight into
	/// <see cref="OutputDirectory"/> (the <c>Airways</c> sub-folder is kept either way -
	/// remediation plan 3.7).
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>
	/// Whether CRC ERAM property defaults (<see cref="LineDefaults"/>,
	/// <see cref="SymbolDefaults"/>, <see cref="TextDefaults"/>) should be written as
	/// isDefaults Features in the generated GeoJSON files.
	/// </summary>
	public required bool IncludeCrcEramPropertyDefaults { get; init; }

	/// <summary>The Region of Interest to filter and clip output to, or <see langword="null"/> for no ROI filtering.</summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>CRC line property defaults, keyed by altitude class. Populated when <see cref="IncludeCrcEramPropertyDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcLineProperties> LineDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcLineProperties>();

	/// <summary>CRC symbol property defaults, keyed by altitude class. Populated when <see cref="IncludeCrcEramPropertyDefaults"/> is <see langword="true"/>.</summary>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcSymbolProperties> SymbolDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcSymbolProperties>();

	/// <summary>
	/// CRC text property defaults, keyed by altitude class. Populated when
	/// <see cref="IncludeCrcEramPropertyDefaults"/> is <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// The <see cref="CrcTextProperties.Text"/> value on each of these entries is a harmless,
	/// non-rendered placeholder (the isDefaults Text Feature is never drawn by CRC). A real
	/// airway waypoint's Text Feature always supplies its own <c>text</c> (the waypoint's
	/// PointId) as a per-feature override built by <c>AirwayGeojsonService</c>; the settings
	/// dictionary has no mechanism to configure per-waypoint text, since it necessarily
	/// differs for every waypoint.
	/// </remarks>
	public IReadOnlyDictionary<AirwayAltitudeClass, CrcTextProperties> TextDefaults { get; init; } =
		new Dictionary<AirwayAltitudeClass, CrcTextProperties>();
}
