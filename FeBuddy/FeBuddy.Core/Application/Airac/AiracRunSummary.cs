using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>What an AIRAC Service run produced, in one line: what the GUI says when the run is done.</summary>
public static class AiracRunSummary
{
	/// <summary>The run's one line, e.g. <c>Cycle 2610: 1,204 airway(s), 512 airport(s), Combined_Alias.txt (9,812 command(s)).</c></summary>
	/// <param name="cycleId">The cycle that was run.</param>
	/// <param name="result">The run's result.</param>
	/// <returns>The line.</returns>
	public static string Describe(string cycleId, AiracServiceResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		List<string> parts = [];

		if (result.Airways is { } airways)
		{
			parts.Add($"{airways.AirwayCount:N0} airway(s)"
				+ (airways.ExcludedAirwayIds.Count > 0 ? $", {airways.ExcludedAirwayIds.Count} excluded" : string.Empty));
		}

		if (result.Airports is { } airports)
		{
			parts.Add($"{airports.AirportCount:N0} airport(s)");
		}

		if (result.Departures is { } departures)
		{
			parts.Add($"{departures.AirportProcedureCount:N0} airport departure(s)"
				+ (departures.SkippedForMissingPointsCount > 0 ? $", {departures.SkippedForMissingPointsCount} skipped" : string.Empty));
		}

		if (result.Arrivals is { } arrivals)
		{
			parts.Add($"{arrivals.AirportProcedureCount:N0} airport arrival(s)"
				+ (arrivals.SkippedForMissingPointsCount > 0 ? $", {arrivals.SkippedForMissingPointsCount} skipped" : string.Empty));
		}

		if (result.Navaids is { } navaids)
		{
			parts.Add($"{navaids.NavaidCount:N0} NAVAID(s)");
		}

		if (result.ArtccBoundaries is { } b)
		{
			parts.Add($"{b.RingCount:N0} ARTCC boundary line(s)");
		}

		if (result.Fixes is { } fixes)
		{
			parts.Add($"{fixes.FixCount:N0} fix(es)");
		}

		if (result.WxStations is { } wxStations)
		{
			parts.Add($"{wxStations.StationCount:N0} weather station(s)");
		}

		if (result.Procedures is { } procedures)
		{
			if (procedures.FilesWritten.Count > 0)
			{
				parts.Add($"{procedures.AirportCount:N0} airport(s) with procedure changes"
					+ (procedures.NewCount + procedures.ChangedCount + procedures.DeletedCount > 0
						? $", {procedures.NewCount + procedures.ChangedCount + procedures.DeletedCount} change(s)"
						: string.Empty));
			}

			if (procedures.AliasFilePath is not null)
			{
				parts.Add($"{procedures.AliasCommandCount:N0} FAA Chart Recall command(s)");
			}
		}

		if (result.Telephony is { AliasFilePath: not null } telephony)
		{
			parts.Add($"{telephony.AliasCommandCount:N0} telephony command(s)");
		}

		if (result.CombinedAlias is { FilePath: not null } combinedAlias)
		{
			parts.Add($"{Path.GetFileName(combinedAlias.FilePath)} ({combinedAlias.CustomCommandCount + combinedAlias.FeBuddyCommandCount:N0} command(s))");
		}

		if (result.DuplicateAliasReport is { Duplicates.Count: > 0 } duplicateAliasReport)
		{
			parts.Add($"{duplicateAliasReport.Duplicates.Count:N0} duplicate alias command(s)");
		}

		return parts.Count == 0
			? $"Cycle {cycleId}: nothing to produce."
			: $"Cycle {cycleId}: {string.Join(", ", parts)}.";
	}
}
