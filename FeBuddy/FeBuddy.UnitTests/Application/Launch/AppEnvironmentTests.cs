using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.UnitTests.Application.Launch;

/// <summary>
/// Covers how <see cref="AppEnvironment"/> works out AIRAC cycles: from the UTC time check once
/// it has run, from the machine's clock only before then.
/// </summary>
[Collection("AppLog")]
public sealed class AppEnvironmentTests : IDisposable
{
	public AppEnvironmentTests() => AppEnvironment.ResetForTesting();

	public void Dispose() => AppEnvironment.ResetForTesting();

	[Fact]
	public void cycles_come_from_the_time_check_not_the_machine_clock()
	{
		// Far from any real "today", so a pass cannot come from the machine clock.
		AppEnvironment.LaunchUtcNow = new DateTime(2027, 3, 15, 6, 0, 0, DateTimeKind.Utc);

		Assert.Equal(new DateOnly(2027, 3, 15), AppEnvironment.CheckedUtcDate);
		Assert.Equal("2701", AppEnvironment.GetAiracCycle(AiracCyclePosition.Previous).AiracCycleId);
		Assert.Equal("2702", AppEnvironment.GetAiracCycle(AiracCyclePosition.Current).AiracCycleId);
		Assert.Equal("2703", AppEnvironment.GetAiracCycle(AiracCyclePosition.Next).AiracCycleId);
	}

	[Fact]
	public void before_the_time_check_the_machine_clock_stands_in()
	{
		Assert.Null(AppEnvironment.LaunchUtcNow);

		// Read before and after, so 0000Z passing between the reads can't fail the test.
		DateOnly before = DateOnly.FromDateTime(DateTime.UtcNow);
		DateOnly checkedDate = AppEnvironment.CheckedUtcDate;
		AiracCycleInfo cycle = AppEnvironment.GetAiracCycle(AiracCyclePosition.Current);
		DateOnly after = DateOnly.FromDateTime(DateTime.UtcNow);

		Assert.InRange(checkedDate, before, after);
		Assert.Contains(cycle, new[] { before, after }.Select(date => AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOfUtc: date)));
	}
}
