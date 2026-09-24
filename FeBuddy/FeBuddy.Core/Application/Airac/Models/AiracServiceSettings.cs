using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// Everything <c>AiracService.RunAsync</c> needs for one run: which cycle to use, and one raw
/// settings block per selected sub-service.
/// </summary>
/// <remarks>
/// Each block is the key/value dictionary the GUI builds from the saved <c>UserConfig</c>
/// values - never from what is on screen - so a run always uses what was saved. It carries
/// everything that sub-service needs, output folder included. A <see langword="null"/> block
/// means that sub-service was not selected.
/// </remarks>
public sealed record AiracServiceSettings
{
	/// <summary>The AIRAC cycle to run against (its parsed NASR data is supplied separately).</summary>
	public required AiracCycleInfo SelectedCycle { get; init; }

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

	/// <summary>
	/// The Departures sub-service settings block, or <see langword="null"/> when Departures was
	/// not selected for this run.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Departures { get; init; }
}
