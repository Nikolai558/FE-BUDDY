using System.Net.Http;
using System.Text.Json;

using FEBuddyLibrary.Models.Services.General;

namespace FEBuddyLibrary.Services.General;

/// <summary>
/// Establishes a trustworthy "now" in UTC at launch, and as a side effect decides whether the
/// machine currently has internet access.
/// </summary>
/// <remarks>
/// The AIRAC cycle maths must not be driven by a user's possibly-wrong local clock, so the
/// check prefers a network time source: timeapi.io first, then any server's <c>Date</c>
/// response header, and only then the local clock (with <see cref="UtcTimeCheckResult.HasInternetConnection"/>
/// set to <see langword="false"/>). It never throws - a total failure just returns the local
/// clock.
/// </remarks>
public static class UtcTimeCheck
{
	private const string LogSource = "UtcTimeCheck";
	private const string TimeApiUrl = "https://timeapi.io/api/Time/current/zone?timeZone=UTC";
	private const string DateHeaderProbeUrl = "https://www.cloudflare.com/cdn-cgi/trace";

	/// <summary>
	/// Runs the check. Never throws.
	/// </summary>
	/// <param name="httpClient">
	/// An <see cref="HttpClient"/> to use, or <see langword="null"/> to create a short-timeout
	/// one. Supplying one is primarily a test seam.
	/// </param>
	/// <param name="cancellationToken">Cancels the network calls.</param>
	/// <returns>The best available UTC time, its source, and whether the network responded.</returns>
	public static async Task<UtcTimeCheckResult> RunAsync(
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		bool ownsClient = httpClient is null;
		HttpClient client = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

		try
		{
			if (client.DefaultRequestHeaders.UserAgent.Count == 0)
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FE-Buddy");
			}

			UtcTimeCheckResult? fromApi = await TryTimeApiAsync(client, cancellationToken).ConfigureAwait(false);
			if (fromApi is not null)
			{
				AppLog.Info(LogSource, $"UTC time from timeapi.io: {fromApi.UtcNow:yyyy-MM-dd HH:mm:ss}Z. Internet is available.");
				return fromApi;
			}

			UtcTimeCheckResult? fromHeader = await TryDateHeaderAsync(client, cancellationToken).ConfigureAwait(false);
			if (fromHeader is not null)
			{
				AppLog.Info(LogSource, $"UTC time from HTTP Date header: {fromHeader.UtcNow:yyyy-MM-dd HH:mm:ss}Z. Internet is available.");
				return fromHeader;
			}

			AppLog.Warning(LogSource, "No network time source responded. Falling back to the local clock; treating the machine as offline.");
			return new UtcTimeCheckResult(DateTime.UtcNow, UtcTimeSource.LocalClock, HasInternetConnection: false);
		}
		catch (Exception ex)
		{
			AppLog.Warning(LogSource, $"UTC time check failed unexpectedly ({ex.Message}). Using the local clock; treating the machine as offline.");
			return new UtcTimeCheckResult(DateTime.UtcNow, UtcTimeSource.LocalClock, HasInternetConnection: false);
		}
		finally
		{
			if (ownsClient)
			{
				client.Dispose();
			}
		}
	}

	private static async Task<UtcTimeCheckResult?> TryTimeApiAsync(HttpClient client, CancellationToken cancellationToken)
	{
		try
		{
			using HttpResponseMessage response = await client.GetAsync(TimeApiUrl, cancellationToken).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

			if (document.RootElement.TryGetProperty("dateTime", out JsonElement dateTimeElement)
				&& dateTimeElement.ValueKind == JsonValueKind.String
				&& DateTime.TryParse(
					dateTimeElement.GetString(),
					System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
					out DateTime parsed))
			{
				return new UtcTimeCheckResult(
					DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
					UtcTimeSource.TimeApi,
					HasInternetConnection: true);
			}
		}
		catch
		{
			// Handled by the caller falling through to the next source.
		}

		return null;
	}

	private static async Task<UtcTimeCheckResult?> TryDateHeaderAsync(HttpClient client, CancellationToken cancellationToken)
	{
		try
		{
			using HttpRequestMessage request = new(HttpMethod.Head, DateHeaderProbeUrl);
			using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

			DateTimeOffset? date = response.Headers.Date;

			if (date.HasValue)
			{
				return new UtcTimeCheckResult(date.Value.UtcDateTime, UtcTimeSource.HttpDateHeader, HasInternetConnection: true);
			}
		}
		catch
		{
			// Handled by the caller falling back to the local clock.
		}

		return null;
	}
}
