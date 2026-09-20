using FeBuddy.Core.Models.Services.General;

namespace FeBuddy.Core.Models.Services.Airac;

/// <summary>
/// Everything <c>AiracService.RunAsync</c> needs for one AIRAC Service run: the cross-cutting
/// choices (which cycle, which facility, where to write, the default ROI, coordinate
/// precision) plus one raw settings block per sub-service.
/// </summary>
/// <remarks>
/// <para>
/// Each sub-service block is the <c>Dictionary&lt;string, string&gt;</c> the GUI builds from
/// <c>UserConfig</c> values (never from live screen state), keeping "what was saved" and "what
/// runs" identical - see the build plan's Settings Contract and Phase 1.3. A <see langword="null"/>
/// block means that sub-service was not selected for this run.
/// </para>
/// <para>
/// <see cref="Airways"/> is the only sub-service with a backend today; the GUI also lists
/// sub-services that have none yet, and those never produce a block here. This record stays
/// shaped so the rest can be added later as sibling blocks without changing the orchestrator's
/// contract.
/// </para>
/// </remarks>
public sealed record AiracServiceSettings
{
	/// <summary>The AIRAC cycle to run against (its parsed NASR data is supplied separately).</summary>
	public required AiracCycleInfo SelectedCycle { get; init; }

	/// <summary>The user's ARTCC / facility ID, e.g. <c>ZOA</c>.</summary>
	public required string ArtccId { get; init; }

	/// <summary>The directory the user pointed output at.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>
	/// When <see langword="true"/> (the default), output is written under a
	/// <c>FE-Buddy_Output</c> folder inside <see cref="OutputDirectory"/>; when
	/// <see langword="false"/>, straight into <see cref="OutputDirectory"/>. Honoured by the
	/// sub-service writers (Phase 3.7).
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>The default Region of Interest, or <see langword="null"/> when the user has not set one.</summary>
	public RegionOfInterest? DefaultRoi { get; init; }

	/// <summary>Maximum decimal places for coordinates written to GeoJSON (Phase 3.6). Default 6.</summary>
	public int CoordinatePrecision { get; init; } = 6;

	/// <summary>
	/// The Airways sub-service settings block, or <see langword="null"/> when Airways was not
	/// selected for this run.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Airways { get; init; }

	/// <summary>
	/// The Airports sub-service settings block, or <see langword="null"/> when Airports was not
	/// selected for this run.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Airports { get; init; }
}
