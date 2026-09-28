using System.Net;
using System.Net.Http.Headers;

using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Http;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Infrastructure.Nasr;

/// <summary>
/// Establishes whether the FAA has published a given AIRAC cycle's NASR CSV subscription yet.
/// </summary>
/// <remarks>
/// <para>
/// <c>AiracCycleResolver</c> will happily name a next-cycle identity weeks before its data
/// exists. Publication is a separate fact and must be checked separately: a cheap HTTP
/// <c>HEAD</c> (falling back to a one-byte ranged <c>GET</c> when HEAD is not allowed) against
/// the cycle's CSV URL.
/// </para>
/// <para>
/// A not-yet-published next cycle is <b>normal, not a failure</b>: it is logged at
/// <see cref="LogLevel.Info"/>, never <see cref="LogLevel.Warning"/>, and never produces an
/// error. A network problem is <see cref="AiracCyclePublicationState.Unknown"/> - the caller
/// retries rather than concluding the cycle does not exist. Availability is never derived from
/// a date rule; the probe is the only authority.
/// </para>
/// </remarks>
public static class AiracCycleAvailability
{
	private const string LogSource = "AiracCycleAvailability";

	/// <summary>
	/// Probes whether <paramref name="cycle"/>'s NASR CSV archive is published. Never throws.
	/// </summary>
	/// <param name="cycle">The cycle to probe.</param>
	/// <param name="httpClient">An <see cref="HttpClient"/> to use, or <see langword="null"/> to create one. A test seam.</param>
	/// <param name="cancellationToken">Cancels the probe.</param>
	/// <returns>
	/// <see cref="AiracCyclePublicationState.Published"/>, <see cref="AiracCyclePublicationState.NotYetPublished"/>,
	/// or <see cref="AiracCyclePublicationState.Unknown"/>.
	/// </returns>
	public static async Task<AiracCyclePublicationState> ProbeAsync(
		AiracCycleInfo cycle,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(cycle);

		string url = NasrCycleDownloader.BuildCsvDownloadUrl(cycle);

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(TimeSpan.FromSeconds(15)) : null;
		HttpClient client = httpClient ?? owned!;

		try
		{
			AiracCyclePublicationState state = await SendAsync(client, HttpMethod.Head, url, cycle, cancellationToken).ConfigureAwait(false);

			// Some servers/CDNs reject HEAD (405) - retry once with a one-byte ranged GET.
			if (state == AiracCyclePublicationState.Unknown)
			{
				state = await SendAsync(client, HttpMethod.Get, url, cycle, cancellationToken, rangedFirstByte: true).ConfigureAwait(false);
			}

			return state;
		}
		catch (Exception ex)
		{
			AppLog.Info(LogSource, $"Cycle {cycle.AiracCycleId}: publication probe could not reach the FAA ({ex.Message}). Will retry.");
			return AiracCyclePublicationState.Unknown;
		}
	}

	private static async Task<AiracCyclePublicationState> SendAsync(
		HttpClient client,
		HttpMethod method,
		string url,
		AiracCycleInfo cycle,
		CancellationToken cancellationToken,
		bool rangedFirstByte = false)
	{
		try
		{
			using HttpRequestMessage request = new(method, url);

			if (rangedFirstByte)
			{
				request.Headers.Range = new RangeHeaderValue(0, 0);
			}

			using HttpResponseMessage response = await client
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
				.ConfigureAwait(false);

			if (response.IsSuccessStatusCode)
			{
				AppLog.Info(LogSource, $"Cycle {cycle.AiracCycleId}: published (HTTP {(int)response.StatusCode}).");
				return AiracCyclePublicationState.Published;
			}

			if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
			{
				AppLog.Info(LogSource, $"Cycle {cycle.AiracCycleId}: not yet published by the FAA (HTTP {(int)response.StatusCode}).");
				return AiracCyclePublicationState.NotYetPublished;
			}

			// 405 (method not allowed), 5xx, redirects we did not follow, etc. - inconclusive.
			return AiracCyclePublicationState.Unknown;
		}
		catch (Exception ex)
		{
			AppLog.Info(LogSource, $"Cycle {cycle.AiracCycleId}: publication probe error ({ex.Message}). Will retry.");
			return AiracCyclePublicationState.Unknown;
		}
	}
}
