using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Harness;

/// <summary>What one harness Telephony run produced: what the downloads said, and the service's result.</summary>
/// <param name="DownloadMessages">
/// What getting the FAA pages said - fresh, an older kept copy and its age, or none - exactly as a
/// real run shows it on the Review tab.
/// </param>
/// <param name="Result">What <c>TelephonyService.Run</c> returned.</param>
internal sealed record TelephonyRun(IReadOnlyList<ServiceMessage> DownloadMessages, TelephonyServiceResult Result);

/// <summary>
/// Exercises the Telephony service exactly the way an AIRAC Service run does: download the latest
/// FAA telephony pages (falling back on FE-Buddy's kept copies), build the settings dictionary, call
/// the one public entry point, and hand the result back for reporting. Contains no Telephony logic
/// of its own.
/// </summary>
/// <remarks>
/// Unlike most runners, this does not take <c>NasrCsvDataCollection</c>: Telephony's data is not
/// part of a NASR cycle. The downloads replace the real kept copies under
/// <c>%APPDATA%\FE-Buddy\Telephony</c>, the same as a GUI run would.
/// </remarks>
internal static class TelephonyRunner
{
	/// <summary>
	/// Downloads (or falls back on) the FAA telephony pages and runs the Telephony sub-service
	/// against them using the settings in <see cref="HarnessSettings.TelephonySettings"/>.
	/// </summary>
	/// <returns>What the downloads said and what the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static async Task<TelephonyRun> RunAsync()
	{
		AiracSharedDataLoadResult<TelephonyDataCollection> loaded = await AiracSharedDataLoader.LoadTelephonyAsync().ConfigureAwait(false);
		TelephonyServiceResult result = TelephonyService.Run(loaded.Data, HarnessSettings.TelephonySettings());

		return new TelephonyRun(loaded.Messages, result);
	}
}
