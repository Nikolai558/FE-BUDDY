using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

namespace UnitTests.Services.General;

public class AiracCycleResolverTests
{
	[Fact]
	public void resolves_current_previous_and_next_for_a_known_date()
	{
		// 2026-09-06 falls inside cycle 2609 (effective 2026-09-03, next cycle 2610 effective 2026-10-01).
		DateOnly asOf = new(2026, 9, 6);

		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOf);
		AiracCycleInfo previous = AiracCycleResolver.GetCycle(AiracCyclePosition.Previous, asOf);
		AiracCycleInfo next = AiracCycleResolver.GetCycle(AiracCyclePosition.Next, asOf);

		Assert.Equal("2609", current.AiracCycleId);
		Assert.Equal("03_Sep_2026", current.NasrCsvEffectiveDate);

		Assert.Equal("2608", previous.AiracCycleId);
		Assert.Equal("2610", next.AiracCycleId);
	}

	[Fact]
	public void a_date_exactly_on_an_effective_date_belongs_to_that_new_cycle()
	{
		DateOnly asOf = new(2026, 10, 1); // exactly cycle 2610's effective date

		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOf);

		Assert.Equal("2610", current.AiracCycleId);
	}

	[Fact]
	public void the_day_before_an_effective_date_still_belongs_to_the_prior_cycle()
	{
		DateOnly asOf = new(2026, 9, 30); // one day before cycle 2610 becomes effective

		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOf);

		Assert.Equal("2609", current.AiracCycleId);
	}

	[Fact]
	public void effective_dates_are_returned_correctly_for_the_csv_url_format()
	{
		DateOnly asOf = new(2026, 9, 6);

		AiracCycleInfo next = AiracCycleResolver.GetCycle(AiracCyclePosition.Next, asOf);

		Assert.Equal("01_Oct_2026", next.NasrCsvEffectiveDate);
		Assert.Equal(new DateOnly(2026, 10, 1), next.EffectiveDateUtc);
	}

	[Fact]
	public void a_date_before_the_earliest_known_cycle_throws()
	{
		DateOnly tooEarly = new(2000, 1, 1);

		Assert.Throws<InvalidOperationException>(() =>
			AiracCycleResolver.GetCycle(AiracCyclePosition.Current, tooEarly));
	}
}
