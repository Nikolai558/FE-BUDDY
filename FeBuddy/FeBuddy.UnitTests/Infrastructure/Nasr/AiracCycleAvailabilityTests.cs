using System.Net;
using System.Net.Http;

using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.UnitTests.Infrastructure.Nasr;

/// <summary>
/// Verifies <see cref="AiracCycleAvailability.ProbeAsync"/>: 200 -&gt; published,
/// 404/403 -&gt; not yet published, a network error -&gt; unknown, and the HEAD -&gt; ranged-GET
/// fallback.
/// </summary>
[Collection("AppLog")]
public sealed class AiracCycleAvailabilityTests : IDisposable
{
	private static readonly AiracCycleInfo Cycle = new("2611", "29_Oct_2026", new DateOnly(2026, 10, 29));

	public AiracCycleAvailabilityTests() => AppLog.ConfigureForTesting(Path.Combine(Path.GetTempPath(), "FeBuddyTests_Avail_" + Guid.NewGuid().ToString("N")));

	public void Dispose() => AppLog.ConfigureForTesting(null);

	[Fact]
	public async Task Probe_200_IsPublished()
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

		Assert.Equal(AiracCyclePublicationState.Published, await AiracCycleAvailability.ProbeAsync(Cycle, client));
	}

	[Theory]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.Forbidden)]
	public async Task Probe_404_or_403_IsNotYetPublished(HttpStatusCode status)
	{
		using HttpClient client = new(new StubHttpHandler(_ => new HttpResponseMessage(status)));

		Assert.Equal(AiracCyclePublicationState.NotYetPublished, await AiracCycleAvailability.ProbeAsync(Cycle, client));
	}

	[Fact]
	public async Task Probe_NetworkError_IsUnknown()
	{
		using HttpClient client = new(new StubHttpHandler(_ => throw new HttpRequestException("dns failure")));

		Assert.Equal(AiracCyclePublicationState.Unknown, await AiracCycleAvailability.ProbeAsync(Cycle, client));
	}

	[Fact]
	public async Task Probe_HeadRejected_FallsBackToRangedGet()
	{
		using HttpClient client = new(new StubHttpHandler(request =>
			request.Method == HttpMethod.Head
				? new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
				: new HttpResponseMessage(HttpStatusCode.PartialContent)));

		Assert.Equal(AiracCyclePublicationState.Published, await AiracCycleAvailability.ProbeAsync(Cycle, client));
	}
}
