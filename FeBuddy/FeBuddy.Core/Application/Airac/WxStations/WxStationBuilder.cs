using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.WxStations;
using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.Core.Application.Airac.WxStations;

/// <summary>
/// Builds every <see cref="WxStation"/> the Wx Stations sub-service works with, from the parsed
/// <c>stations.cache.xml</c> data.
/// </summary>
/// <remarks>
/// Everything is built once, for the whole file, before any filtering: the ROI
/// (<see cref="WxStationGeojsonWriter.FilterToRoi"/>) applies downstream, so this builder never
/// sees the parsed settings - the same shape as <c>FixBuilder</c>.
/// </remarks>
public static class WxStationBuilder
{
	private const string LogSource = "WxStationBuilder";

	/// <summary>
	/// Builds every included station from the parsed Wx station data.
	/// </summary>
	/// <param name="data">
	/// The parsed <c>stations.cache.xml</c> data for the cycle, or <see langword="null"/> when it
	/// has not been downloaded yet.
	/// </param>
	/// <returns>
	/// Every included station, ordered by ICAO ID (ignoring case), stable, the file's total
	/// station count, and any messages collected.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// A station is included only when all of the following hold: it is a US (or US territory)
	/// station (<see cref="WxStationCountries.IsIncluded"/>); it has a non-blank ICAO ID; it
	/// reports <c>METAR</c> among its <c>site_type</c> entries; and it has usable coordinates -
	/// both present and within valid latitude/longitude ranges. A METAR station with everything
	/// else in order but placeholder or out-of-range coordinates (the feed uses
	/// <c>-99.99, -99.99</c>) is left out with one Info message naming it; every other exclusion is
	/// silent.
	/// </remarks>
	public static WxStationBuildResult Read(WxStationDataCollection? data)
	{
		if (data is null)
		{
			throw new InvalidOperationException(
				"No weather station data for this cycle: 'stations.cache.xml' is missing from the cycle's folder. " +
				"FE-Buddy downloads it at launch, so restart FE-Buddy with an internet connection.");
		}

		List<ServiceMessage> messages = [];
		List<WxStation> stations = [];
		List<string> unusableCoordinateIcaoIds = [];

		foreach (WxStationXmlDataModel.Station row in data.Stations)
		{
			if (!WxStationCountries.IsIncluded(row.Country))
			{
				continue;
			}

			if (string.IsNullOrWhiteSpace(row.IcaoId))
			{
				continue;
			}

			string icaoId = row.IcaoId.Trim();

			if (!row.SiteTypes.Contains("METAR", StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}

			if (row.Latitude is not double latitude || row.Longitude is not double longitude)
			{
				continue;
			}

			// NaN passes both range checks, so it is ruled out on its own.
			if (!double.IsFinite(latitude) || !double.IsFinite(longitude)
				|| latitude is < -90 or > 90 || longitude is < -180 or > 180)
			{
				unusableCoordinateIcaoIds.Add(icaoId);
				continue;
			}

			stations.Add(new WxStation(
				IcaoId: icaoId,
				IataId: row.IataId ?? string.Empty,
				Site: row.Site ?? string.Empty,
				Latitude: latitude,
				Longitude: longitude,
				Country: row.Country!));
		}

		if (unusableCoordinateIcaoIds.Count > 0)
		{
			unusableCoordinateIcaoIds.Sort(StringComparer.OrdinalIgnoreCase);

			string subject = unusableCoordinateIcaoIds.Count == 1
				? "1 METAR station has"
				: $"{unusableCoordinateIcaoIds.Count} METAR stations have";
			string verb = unusableCoordinateIcaoIds.Count == 1 ? "was" : "were";

			messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
				$"{subject} no usable coordinates and {verb} left out: {string.Join(", ", unusableCoordinateIcaoIds)}."));
		}

		List<WxStation> orderedStations = [.. stations.OrderBy(station => station.IcaoId, StringComparer.OrdinalIgnoreCase)];

		return new WxStationBuildResult(orderedStations, data.Stations.Count, messages);
	}
}
