using System.Globalization;

using FeBuddy.Core.Domain.Airac.Models;

namespace FeBuddy.Core.Domain.Airac;

/// <summary>
/// Works out which AIRAC cycle is in effect on a date, and the cycles either side of it.
/// </summary>
/// <remarks>
/// <para>
/// AIRAC cycles follow a fixed 28-day cadence, so every cycle can be calculated from any one
/// known cycle - no lookup table to maintain.
/// </para>
/// <para>
/// A cycle's ID is the two-digit year it takes effect in, then its number within that year:
/// cycle 01 is the first to take effect on or after 1 January. Most years have 13 cycles; about
/// one year in nine has 14 (e.g. 2014, effective 2020-12-31).
/// </para>
/// </remarks>
public static class AiracCycleResolver
{
	private const int DaysPerCycle = 28;

	/// <summary>The cycle every other is counted from: 2601, effective 2026-01-22.</summary>
	private static readonly DateOnly ReferenceEffectiveDate = new(2026, 1, 22);

	/// <summary>
	/// Resolves one AIRAC cycle relative to <paramref name="asOfUtc"/>.
	/// </summary>
	/// <param name="position">Which cycle to resolve: the one in effect, or the one before or after it.</param>
	/// <param name="asOfUtc">
	/// The date to resolve against; defaults to today in UTC. Always pass a UTC date: cycles
	/// change at 0000Z, so a local date can land on the wrong cycle.
	/// </param>
	/// <returns>The cycle's ID, effective date, and the date string its NASR CSV download uses.</returns>
	public static AiracCycleInfo GetCycle(AiracCyclePosition position, DateOnly? asOfUtc = null)
	{
		DateOnly date = asOfUtc ?? DateOnly.FromDateTime(DateTime.UtcNow);

		int offset = position switch
		{
			AiracCyclePosition.Previous => -1,
			AiracCyclePosition.Current => 0,
			AiracCyclePosition.Next => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown AIRAC cycle position.")
		};

		// Floor division: a date before the reference cycle belongs to a cycle before it.
		int cyclesSinceReference = (int)Math.Floor((date.DayNumber - ReferenceEffectiveDate.DayNumber) / (double)DaysPerCycle);

		return ForEffectiveDate(ReferenceEffectiveDate.AddDays((cyclesSinceReference + offset) * DaysPerCycle));
	}

	private static AiracCycleInfo ForEffectiveDate(DateOnly effectiveDate)
	{
		// The year's first cycle takes effect within its first 28 days, so the cycle number is
		// how many whole cycles into the year this one starts, plus one.
		int numberInYear = (effectiveDate.DayOfYear - 1) / DaysPerCycle + 1;

		return new AiracCycleInfo(
			AiracCycleId: $"{effectiveDate.Year % 100:00}{numberInYear:00}",
			NasrCsvEffectiveDate: effectiveDate.ToString("dd_MMM_yyyy", CultureInfo.InvariantCulture),
			EffectiveDateUtc: effectiveDate);
	}
}
