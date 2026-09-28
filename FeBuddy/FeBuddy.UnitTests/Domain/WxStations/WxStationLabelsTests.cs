using FeBuddy.Core.Domain.WxStations;
using FeBuddy.Core.Domain.WxStations.Models;

namespace FeBuddy.UnitTests.Domain.WxStations;

/// <summary>
/// Covers <see cref="WxStationLabels.SecondLine"/>: the IATA+site combination, an IATA with no
/// site, a site with no IATA, and neither.
/// </summary>
public sealed class WxStationLabelsTests
{
	private static WxStation Station(string iataId, string site) =>
		new("KDTW", iataId, site, 42.212, -83.353, "US");

	[Fact]
	public void an_iata_id_and_site_combine_with_an_underscore() =>
		Assert.Equal("DTW_Detroit/Metro Wayne Cnty", WxStationLabels.SecondLine(Station("DTW", "Detroit/Metro Wayne Cnty")));

	[Fact]
	public void an_iata_id_with_no_site_still_carries_the_trailing_underscore() =>
		Assert.Equal("DTW_", WxStationLabels.SecondLine(Station("DTW", "")));

	[Fact]
	public void no_iata_id_falls_back_to_the_site_name() =>
		Assert.Equal("Detroit/Metro Wayne Cnty", WxStationLabels.SecondLine(Station("", "Detroit/Metro Wayne Cnty")));

	[Fact]
	public void neither_an_iata_id_nor_a_site_is_empty() =>
		Assert.Equal(string.Empty, WxStationLabels.SecondLine(Station("", "")));

	[Fact]
	public void a_null_station_throws() =>
		Assert.Throws<ArgumentNullException>(() => WxStationLabels.SecondLine(null!));
}
