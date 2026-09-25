using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// Everything <c>AiracService.RunAsync</c> needs for one run: which cycle to use, where to write,
/// and one raw settings block per selected sub-service.
/// </summary>
/// <remarks>
/// Each block is the key/value dictionary the GUI builds from the saved <c>UserConfig</c>
/// values - never from what is on screen - so a run always uses what was saved. A
/// <see langword="null"/> block means that sub-service was not selected. The blocks leave out
/// <c>OutputDirectory</c>: the service sets it on each to <see cref="CycleOutputDirectory"/>.
/// </remarks>
public sealed record AiracServiceSettings
{
	/// <summary>The AIRAC cycle to run against (its parsed NASR data is supplied separately).</summary>
	public required AiracCycleInfo SelectedCycle { get; init; }

	/// <summary>The folder the user pointed output at (Settings ▸ Default Output Directory).</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>
	/// Whether the cycle folder goes inside a <c>FE-Buddy_Output</c> folder in
	/// <see cref="OutputDirectory"/>. Default <see langword="true"/>.
	/// </summary>
	public bool AddFeBuddyOutputFolder { get; init; } = true;

	/// <summary>
	/// What to do with files an earlier run of this cycle left in <see cref="CycleOutputDirectory"/>.
	/// Default <see cref="ExistingOutputAction.Overwrite"/>.
	/// </summary>
	public ExistingOutputAction ExistingOutput { get; init; } = ExistingOutputAction.Overwrite;

	/// <summary>
	/// The folder this run writes every file into, e.g.
	/// <c>C:\Users\me\Desktop\FE-Buddy_Output\AIRAC_2610</c> (see <see cref="AiracOutputPaths"/>).
	/// </summary>
	public string CycleOutputDirectory =>
		AiracOutputPaths.CycleDirectory(OutputDirectory, AddFeBuddyOutputFolder, SelectedCycle.AiracCycleId);

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

	/// <summary>
	/// The Arrivals sub-service settings block, or <see langword="null"/> when Arrivals was not
	/// selected for this run.
	/// </summary>
	public IReadOnlyDictionary<string, string>? Arrivals { get; init; }
}
