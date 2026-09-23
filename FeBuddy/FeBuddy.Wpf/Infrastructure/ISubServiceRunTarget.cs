using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac;

namespace FeBuddy.Wpf.Infrastructure;

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
    /// reads.
    /// </summary>
    /// <param name="outputDirectory">The run's resolved output directory.</param>
    /// <param name="addFeBuddyOutputFolder">Whether to wrap output in a <c>FE-Buddy_Output</c> folder.</param>
    /// <returns>The settings block.</returns>
    IReadOnlyDictionary<string, string> BuildSettingsBlock(string outputDirectory, bool addFeBuddyOutputFolder);

    /// <summary>Tells the tab whether the AIRAC data is ready to use.</summary>
    /// <param name="ready">Whether the cycle cache reports readiness.</param>
    void SetReadiness(bool ready);

    /// <summary>
    /// Hands the tab the selected cycle's parsed data, for any option list built from it.
    /// </summary>
    /// <param name="data">The parsed NASR data for the selected cycle.</param>
    void LoadCycleDependentLists(NasrCsvDataCollection data);

    /// <summary>
    /// Describes this sub-service's part of a finished run for the Review tab: what it produced,
    /// and its warnings and routine notices.
    /// </summary>
    /// <param name="result">The aggregated AIRAC Service result.</param>
    /// <returns>The block, or <see langword="null"/> when this sub-service was not part of the run.</returns>
    SubServiceRunResult? DescribeRunResult(AiracServiceResult result);
}
