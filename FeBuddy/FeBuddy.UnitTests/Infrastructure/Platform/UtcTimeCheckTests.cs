using System.Net;
using System.Net.Http;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;
using FeBuddy.Core.Infrastructure.Platform.Models;

namespace FeBuddy.UnitTests.Infrastructure.Platform;

/// <summary>
/// Covers <see cref="UtcTimeCheck"/>: each network time source and the local-clock fallback.
/// </summary>
[Collection("AppLog")]
public sealed class UtcTimeCheckTests : IDisposable
{
	private readonly string _logRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Log_" + Guid.NewGuid().ToString("N"));

	/// <summary>Redirects the log to a throwaway folder.</summary>
	public UtcTimeCheckTests() => AppLog.ConfigureForTesting(_logRoot);

	/// <summary>Restores the default log folder and cleans up.</summary>
	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);

		try
		{
			if (Directory.Exists(_logRoot))
			{
				Directory.Delete(_logRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>A good timeapi.io response yields <see cref="UtcTimeSource.TimeApi"/> and an online result.</summary>
	[Fact]
	public async Task utc_time_check_parses_time_api_response()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"dateTime":"2026-09-07T12:34:56.0000000"}"""),
		}));

		UtcTimeCheckResult result = await UtcTimeCheck.RunAsync(client);

		Assert.True(result.HasInternetConnection);
		Assert.Equal(UtcTimeSource.TimeApi, result.Source);
		Assert.Equal(new DateTime(2026, 9, 7, 12, 34, 56, DateTimeKind.Utc), result.UtcNow);
	}

	/// <summary>When every network call fails, the check falls back to the local clock and reports offline.</summary>
	[Fact]
	public async Task utc_time_check_falls_back_to_local_clock_when_offline()
	{
		using HttpClient client = new(new StubHttpHandler(_ => throw new HttpRequestException("no network")));

		UtcTimeCheckResult result = await UtcTimeCheck.RunAsync(client);

		Assert.False(result.HasInternetConnection);
		Assert.Equal(UtcTimeSource.LocalClock, result.Source);
	}

	/// <summary>When timeapi.io is down, the Date header of a HEAD probe supplies the time.</summary>
	[Theory]
	[InlineData(HttpStatusCode.ServiceUnavailable, "")]
	[InlineData(HttpStatusCode.OK, """{"somethingElse":1}""")]
	public async Task utc_time_check_time_api_unusable_falls_back_to_the_date_header(HttpStatusCode timeApiStatus, string timeApiBody)
	{
		DateTimeOffset serverDate = new(2026, 9, 7, 1, 2, 3, TimeSpan.Zero);

		using HttpClient client = new(new StubHttpHandler(request =>
		{
			if (request.Method == HttpMethod.Head)
			{
				HttpResponseMessage probe = new(HttpStatusCode.OK);
				probe.Headers.Date = serverDate;
				return probe;
			}

			return new HttpResponseMessage(timeApiStatus) { Content = new StringContent(timeApiBody) };
		}));

		UtcTimeCheckResult result = await UtcTimeCheck.RunAsync(client);

		Assert.Equal(UtcTimeSource.HttpDateHeader, result.Source);
		Assert.Equal(serverDate.UtcDateTime, result.UtcNow);
		Assert.True(result.HasInternetConnection);
	}
}
