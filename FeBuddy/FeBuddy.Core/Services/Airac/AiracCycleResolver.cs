using System.Globalization;

using FeBuddy.Core.Models.General;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Models.Services.Airac;

namespace FeBuddy.Core.Services.Airac;

/// <summary>
/// Resolves the Previous, Current, or Next AIRAC cycle relative to a date, from the
/// <see cref="AiracCycleIdEffectiveDates.AllCycleDates"/> lookup table.
/// </summary>
public static class AiracCycleResolver
{
	/// <summary>
	/// Resolves one AIRAC cycle relative to <paramref name="asOfUtc"/> (or today, UTC, when
	/// not supplied).
	/// </summary>
	/// <param name="position">Which cycle to resolve.</param>
	/// <param name="asOfUtc">
	/// The date to resolve "current" against. Defaults to today's date in UTC - always use
	/// UTC here, per the dev notes' rule that AIRAC cycle calculations must be based on UTC
	/// to stay consistent regardless of the user's local time zone.
	/// </param>
	/// <returns>The resolved cycle's identifier, effective date, and CSV download date string.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="asOfUtc"/> falls outside the range covered by the lookup
	/// table (either before its first entry, or <paramref name="position"/> would resolve
	/// past its last entry) - the table needs new entries appended.
	/// </exception>
	public static AiracCycleInfo GetCycle(AiracCyclePosition position, DateOnly? asOfUtc = null)
	{
		DateOnly today = asOfUtc ?? DateOnly.FromDateTime(DateTime.UtcNow);

		IReadOnlyList<AiracCycleIdEffectiveDates.AiracCycleDate> allCycles =
			AiracCycleIdEffectiveDates.AllCycleDates;

		int currentIndex = -1;

		for (int i = 0; i < allCycles.Count; i++)
		{
			DateOnly effectiveDate = ParseEffectiveDate(allCycles[i]);

			if (effectiveDate > today)
			{
				break;
			}

			currentIndex = i;
		}

		if (currentIndex < 0)
		{
			throw new InvalidOperationException(
				$"No AIRAC cycle data covers {today:yyyy-MM-dd}; the earliest known cycle " +
				$"starts {ParseEffectiveDate(allCycles[0]):yyyy-MM-dd}. The lookup table needs earlier entries.");
		}

		int targetIndex = position switch
		{
			AiracCyclePosition.Previous => currentIndex - 1,
			AiracCyclePosition.Current => currentIndex,
			AiracCyclePosition.Next => currentIndex + 1,
			_ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown AIRAC cycle position.")
		};

		if (targetIndex < 0 || targetIndex >= allCycles.Count)
		{
			throw new InvalidOperationException(
				$"No AIRAC cycle data available for '{position}' relative to {today:yyyy-MM-dd}. " +
				"The lookup table needs more entries.");
		}

		AiracCycleIdEffectiveDates.AiracCycleDate cycle = allCycles[targetIndex];

		return new AiracCycleInfo(
			cycle.AiracCycleId,
			cycle.NasrCsvAiracEffectiveDate,
			ParseEffectiveDate(cycle));
	}

	private static DateOnly ParseEffectiveDate(AiracCycleIdEffectiveDates.AiracCycleDate cycle) =>
		DateOnly.ParseExact(
			cycle.NasrGeneralAiracEffectiveDate,
			"yyyy-MM-dd",
			CultureInfo.InvariantCulture);
}
