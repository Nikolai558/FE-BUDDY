using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Fixes;
using FeBuddy.Core.Domain.Fixes.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using static FeBuddy.Core.Infrastructure.Nasr.Models.FixCsvDataModel;

namespace FeBuddy.Core.Application.Airac.Fixes;

/// <summary>
/// Builds every <see cref="Fix"/> the Fixes sub-service works with, from <c>FIX_BASE</c>.
/// </summary>
/// <remarks>
/// Everything is built once, for the whole database, before any filtering: the ROI
/// (<see cref="FixGeojsonWriter.FilterToRoi"/>) and the settings-driven grouping/exclusion both
/// apply downstream, so this builder never sees the parsed settings - the same shape as
/// <c>NavaidBuilder</c>.
/// </remarks>
public static class FixBuilder
{
	private const string LogSource = "FixBuilder";

	/// <summary>
	/// Builds every fix from the parsed NASR data.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Fix</c> must not be null.</param>
	/// <returns>Every fix, ordered by identifier (ignoring case), stable, plus any messages collected.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Fix has not been parsed.</exception>
	/// <remarks>
	/// Every <c>FIX_BASE</c> row becomes a <see cref="Fix"/> except a blank <c>FIX_ID</c> (skipped,
	/// one warning each). Duplicate identifiers are allowed in real NASR data and are never merged
	/// - see <see cref="Fix"/>.
	/// </remarks>
	public static FixBuildAllResult Read(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Fix is null)
		{
			throw new InvalidOperationException(
				"Fix data (FIX) has not been parsed. The Fixes sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = [];
		List<Fix> fixes = [];

		foreach (FixBase row in allNasrCsvData.Fix.FixBase)
		{
			string fixId = row.FixId?.Trim() ?? string.Empty;

			if (fixId.Length == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					"A FIX_BASE record has no FIX_ID and was skipped."));
				continue;
			}

			string fixUseCode = row.FixUseCode?.Trim() ?? string.Empty;

			fixes.Add(new Fix(
				fixId,
				row.LatDecimal,
				row.LongDecimal,
				fixUseCode,
				FixUses.Name(fixUseCode),
				FixCharts.Parse(row.Charts)));
		}

		List<Fix> orderedFixes = [.. fixes.OrderBy(fix => fix.FixId, StringComparer.OrdinalIgnoreCase)];

		return new FixBuildAllResult(orderedFixes, messages);
	}
}
