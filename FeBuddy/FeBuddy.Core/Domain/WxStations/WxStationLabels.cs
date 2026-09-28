using FeBuddy.Core.Domain.WxStations.Models;

namespace FeBuddy.Core.Domain.WxStations;

/// <summary>
/// The Wx Stations Text label rule. A station's Text Feature always carries two lines: its ICAO
/// ID (written directly by the caller as the first <c>text</c> entry) and this class's
/// <see cref="SecondLine"/> as the second.
/// </summary>
public static class WxStationLabels
{
	/// <summary>
	/// The Text Feature's second label line: <c>&lt;IATA&gt;_&lt;site&gt;</c> when the station has
	/// an IATA ID (e.g. <c>DTW_Detroit/Metro Wayne Cnty</c>), otherwise just the site name - empty
	/// when the station has neither an IATA ID nor a site name.
	/// </summary>
	/// <param name="station">The station.</param>
	/// <returns>The second label line.</returns>
	public static string SecondLine(WxStation station)
	{
		ArgumentNullException.ThrowIfNull(station);

		return station.IataId.Length > 0
			? $"{station.IataId}_{station.Site}"
			: station.Site;
	}
}
