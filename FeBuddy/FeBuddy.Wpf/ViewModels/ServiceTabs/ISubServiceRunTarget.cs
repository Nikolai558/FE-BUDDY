using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Implemented by a sub-service tab that takes part in a run: it contributes its settings block
/// beforehand and describes its results afterwards.
/// </summary>
/// <remarks>
/// The AIRAC Service screen drives the run through this interface rather than through each
/// sub-service's own type, so adding the next sub-service does not mean editing the run method
/// again - the only sub-service-specific line left there is which settings block on
/// <see cref="AiracServiceSettings"/> the block belongs to.
/// </remarks>
public interface ISubServiceRunTarget
{
	/// <summary>
	/// Builds this sub-service's raw settings block, the same dictionary the library's parser
	/// reads. It leaves out <c>OutputDirectory</c>: the AIRAC Service points every sub-service at
	/// the run's cycle folder itself.
	/// </summary>
	/// <returns>The settings block.</returns>
	IReadOnlyDictionary<string, string> BuildSettingsBlock();

	/// <summary>Tells the tab whether the selected cycle's data is ready to use.</summary>
	/// <param name="ready">
	/// Whether the service is ready and the selected cycle's data has been handed over
	/// (<see cref="LoadCycleDependentLists"/>): a cycle still loading in the background isn't yet.
	/// </param>
	/// <param name="unavailableReason">
	/// Why the selected cycle won't load - not published, or failed - for the tab to say instead of
	/// "Loading"; <see langword="null"/> while it is loading or loaded.
	/// </param>
	void SetReadiness(bool ready, string? unavailableReason = null);

	/// <summary>
	/// Hands the tab the selected cycle's option lists, worked out in Core off the UI thread
	/// (<see cref="AiracCycleOptionListBuilder"/>), for it to fill its toggles from. Called on the UI
	/// thread whenever another cycle is picked: a list built before keeps what is ticked on screen.
	/// </summary>
	/// <param name="lists">The selected cycle's option lists.</param>
	void LoadCycleDependentLists(AiracCycleOptionLists lists);

	/// <summary>
	/// Describes this sub-service's part of a finished run for the Review tab: what it produced,
	/// and its warnings and routine notices.
	/// </summary>
	/// <param name="result">The aggregated AIRAC Service result.</param>
	/// <returns>The block, or <see langword="null"/> when this sub-service was not part of the run.</returns>
	SubServiceRunResult? DescribeRunResult(AiracServiceResult result);
}
