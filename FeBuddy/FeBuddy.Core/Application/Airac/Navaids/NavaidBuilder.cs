using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using static FeBuddy.Core.Infrastructure.Nasr.Models.NavCsvDataModel;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// Builds every <see cref="Navaid"/> the NAVAIDs sub-service works with, from <c>NAV_BASE</c>.
/// </summary>
/// <remarks>
/// Everything is built once, for the whole database, before any filtering: the settings-driven
/// <c>ExcludedTypes</c> filter (<see cref="NavaidFilter"/>) and the ROI
/// (<see cref="NavaidGeojsonWriter.FilterToRoi"/>) both apply downstream, so this builder never
/// sees the parsed settings - the same shape as <c>AirportBuilder</c>.
/// </remarks>
public static class NavaidBuilder
{
	private const string LogSource = "NavaidBuilder";
	private const string ShutdownStatus = "SHUTDOWN";

	/// <summary>
	/// Builds every eligible NAVAID from the parsed NASR data.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Nav</c> must not be null.</param>
	/// <returns>
	/// Every NAVAID, ordered by identifier, then type, then name (ignoring case), then latitude
	/// and longitude, plus any messages collected.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="allNasrCsvData"/>.Nav has not been parsed.</exception>
	/// <remarks>
	/// Every <c>NAV_BASE</c> row becomes a <see cref="Navaid"/> except a <c>NAV_STATUS</c> of
	/// <c>SHUTDOWN</c> (skipped, reported as one Info message with the count) and a blank
	/// <c>NAV_ID</c> (skipped, one warning each). Duplicate identifiers and names are normal in
	/// real NASR data and are never merged - see <see cref="Navaid"/>. A <c>NAV_TYPE</c> FE-Buddy
	/// does not recognize is still included, with one warning per such type.
	/// </remarks>
	public static NavaidBuildAllResult BuildAll(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Nav is null)
		{
			throw new InvalidOperationException(
				"NAVAID data (NAV) has not been parsed. The NAVAIDs sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = [];
		List<Navaid> navaids = [];
		HashSet<string> warnedUnknownTypes = new(StringComparer.OrdinalIgnoreCase);
		int shutdownCount = 0;

		foreach (NavBase row in allNasrCsvData.Nav.NavBase)
		{
			if ((row.NavStatus?.Trim() ?? string.Empty).Equals(ShutdownStatus, StringComparison.OrdinalIgnoreCase))
			{
				shutdownCount++;
				continue;
			}

			string navId = row.NavId?.Trim() ?? string.Empty;

			if (navId.Length == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"A NAV_BASE record for '{row.Name?.Trim()}' has no NAV_ID and was skipped."));
				continue;
			}

			string navType = (row.NavType?.Trim() ?? string.Empty).ToUpperInvariant();

			if (!NavaidTypes.IsKnown(navType) && warnedUnknownTypes.Add(navType))
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"NAVAID type '{navType}' is not one FE-Buddy recognizes. NAVAIDs of this type are still included."));
			}

			navaids.Add(new Navaid(
				navId,
				navType,
				row.Name?.Trim() ?? string.Empty,
				row.Freq,
				row.LatDecimal,
				row.LongDecimal,
				row.LowAltArtccId?.Trim() ?? string.Empty,
				row.HighAltArtccId?.Trim() ?? string.Empty));
		}

		if (shutdownCount > 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{shutdownCount} NAVAID(s) with a NAV_STATUS of 'SHUTDOWN' were skipped."));
		}

		navaids.Sort((left, right) =>
		{
			int byId = string.Compare(left.NavId, right.NavId, StringComparison.OrdinalIgnoreCase);
			if (byId != 0)
			{
				return byId;
			}

			int byType = string.Compare(left.NavType, right.NavType, StringComparison.OrdinalIgnoreCase);
			if (byType != 0)
			{
				return byType;
			}

			int byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
			if (byName != 0)
			{
				return byName;
			}

			int byLatitude = left.Latitude.CompareTo(right.Latitude);
			return byLatitude != 0 ? byLatitude : left.Longitude.CompareTo(right.Longitude);
		});

		return new NavaidBuildAllResult(navaids, messages);
	}
}
