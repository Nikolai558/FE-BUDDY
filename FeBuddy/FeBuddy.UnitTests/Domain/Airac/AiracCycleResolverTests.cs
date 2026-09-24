using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.UnitTests.Domain.Airac;

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

	[Theory]
	[InlineData("2020-12-31", "2014", "31_Dec_2020")] // a 14-cycle year
	[InlineData("2021-01-28", "2101", "28_Jan_2021")] // numbering restarts at 01 in the new year
	[InlineData("2025-01-23", "2501", "23_Jan_2025")] // before the reference cycle
	[InlineData("2026-12-24", "2613", "24_Dec_2026")]
	[InlineData("2027-01-21", "2701", "21_Jan_2027")]
	[InlineData("2119-12-21", "1913", "21_Dec_2119")] // far future: no table to run out of
	public void published_cycles_are_calculated_from_their_effective_date(string effective, string expectedId, string expectedCsvDate)
	{
		DateOnly effectiveDate = DateOnly.Parse(effective, System.Globalization.CultureInfo.InvariantCulture);

		AiracCycleInfo cycle = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, effectiveDate);

		Assert.Equal(expectedId, cycle.AiracCycleId);
		Assert.Equal(expectedCsvDate, cycle.NasrCsvEffectiveDate);
		Assert.Equal(effectiveDate, cycle.EffectiveDateUtc);
	}

	[Fact]
	public void consecutive_cycles_are_exactly_28_days_apart_across_a_year_boundary()
	{
		DateOnly asOf = new(2026, 12, 30); // in 2613; the next cycle is 2701

		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current, asOf);
		AiracCycleInfo next = AiracCycleResolver.GetCycle(AiracCyclePosition.Next, asOf);

		Assert.Equal("2613", current.AiracCycleId);
		Assert.Equal("2701", next.AiracCycleId);
		Assert.Equal(28, next.EffectiveDateUtc.DayNumber - current.EffectiveDateUtc.DayNumber);
	}

	[Fact]
	public void an_unknown_position_is_rejected()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			AiracCycleResolver.GetCycle((AiracCyclePosition)99, new DateOnly(2026, 9, 6)));
	}

	[Fact]
	public void without_a_date_the_current_utc_date_is_used()
	{
		AiracCycleInfo current = AiracCycleResolver.GetCycle(AiracCyclePosition.Current);

		Assert.True(current.EffectiveDateUtc <= DateOnly.FromDateTime(DateTime.UtcNow));
		Assert.True(AiracCycleResolver.GetCycle(AiracCyclePosition.Next).EffectiveDateUtc > DateOnly.FromDateTime(DateTime.UtcNow));
	}
}
