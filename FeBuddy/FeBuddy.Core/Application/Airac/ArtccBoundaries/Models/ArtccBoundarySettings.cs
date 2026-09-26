using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;

/// <summary>
/// Fully-parsed, typed settings for the ARTCC Boundaries sub-service. Built once by
/// <c>ArtccBoundarySettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c>
/// the GUI (or <c>FeBuddy.Harness</c>) supplies; every other ARTCC Boundaries service consumes
/// this typed object, never the raw dictionary.
/// </summary>
/// <remarks>
/// Unlike every other AIRAC sub-service, there is no <c>GenerateGeojson</c> toggle and no alias
/// file: the sub-service always writes GeoJSON Lines.
/// </remarks>
public sealed record ArtccBoundarySettings
{
	/// <summary>
	/// The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC
	/// Service. Files are laid out inside it by <see cref="AiracOutputPaths"/>.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>How to group boundary rings into GeoJSON files. Default <see cref="ArtccBoundaryOutputBy.HighLow"/>.</summary>
	public ArtccBoundaryOutputBy OutputBy { get; init; } = ArtccBoundaryOutputBy.HighLow;

	/// <summary>
	/// The LocationIds to include, trimmed and upper-cased. Empty (the default) includes every
	/// location.
	/// </summary>
	public IReadOnlyCollection<string> LocationFilter { get; init; } = [];

	/// <summary>Whether to split boundary lines at the antimeridian.</summary>
	public required bool SplitAtAntimeridian { get; init; }

	/// <summary>When <see langword="true"/>, Features carry the <c>feb.*</c> properties listed in <see cref="FebProperties"/>.</summary>
	public required bool IncludeFebCustomProperties { get; init; }

	/// <summary>Which <c>feb.*</c> properties to write when <see cref="IncludeFebCustomProperties"/> is <see langword="true"/>.</summary>
	public IReadOnlyCollection<ArtccBoundaryFebProperty> FebProperties { get; init; } = [];

	/// <summary>Maximum decimal places for coordinates written to GeoJSON. Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// Which files go to vNAS, and which of those get CRC-ERAM defaults, by file key (see
	/// <see cref="ArtccBoundaryOutputFiles"/>). Default: none.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>The Region of Interest to clip output to, or <see langword="null"/> for no ROI clipping.</summary>
	public RegionOfInterest? Roi { get; init; }

	/// <summary>
	/// CRC line property defaults, keyed by class name, case-insensitively (see
	/// <see cref="ArtccBoundaryOutputFiles.TryParseKey"/>): <see cref="ArtccBoundaryOutputFiles.HighClass"/>,
	/// <see cref="ArtccBoundaryOutputFiles.LowClass"/>, <see cref="ArtccBoundaryOutputFiles.UnlimitedClass"/>,
	/// or a <c>LocationId-ALTITUDE</c> class. Holds every class a file in
	/// <see cref="VnasFileChoices.CrcDefaultsFiles"/> needs.
	/// </summary>
	public IReadOnlyDictionary<string, CrcLineDefaults> LineDefaults { get; init; } =
		new Dictionary<string, CrcLineDefaults>(StringComparer.OrdinalIgnoreCase);
}
