using FeBuddy.Core.Domain.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

namespace FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

/// <summary>
/// Builds small, real-looking <see cref="WxStationDataCollection"/> instances - and the
/// already-built <see cref="WxStation"/> objects the writer and service tests need - for Wx
/// Stations tests, so tests never depend on the real <c>stations.cache.xml</c>.
/// </summary>
internal static class WxStationTestData
{
	/// <summary>Builds a <see cref="WxStationDataCollection"/> holding only the given rows.</summary>
	public static WxStationDataCollection Build(IEnumerable<WxStationXmlDataModel.Station>? rows = null, int? numResults = null)
	{
		List<WxStationXmlDataModel.Station> stationList = [.. rows ?? []];
		return new WxStationDataCollection { Stations = stationList, NumResults = numResults ?? stationList.Count };
	}

	/// <summary>Builds one Station row. Every field <see cref="Core.Application.Airac.WxStations.WxStationBuilder"/> reads can be overridden.</summary>
	public static WxStationXmlDataModel.Station Row(
		string? icaoId,
		string country = "US",
		IReadOnlyList<string>? siteTypes = null,
		double? latitude = 42.212,
		double? longitude = -83.353,
		string? iataId = null,
		string? site = null) =>
		new()
		{
			IcaoId = icaoId,
			Country = country,
			SiteTypes = siteTypes ?? ["METAR"],
			Latitude = latitude,
			Longitude = longitude,
			IataId = iataId,
			Site = site,
		};

	// ---- sample rows ----

	/// <summary>KDTW: a US METAR station with an IATA ID and a site name.</summary>
	public static WxStationXmlDataModel.Station DtwRow() =>
		Row("KDTW", "US", ["METAR", "TAF"], 42.212, -83.353, "DTW", "Detroit/Metro Wayne Cnty");

	/// <summary>KPHX: a second US METAR station, sorted alphabetically after KDTW.</summary>
	public static WxStationXmlDataModel.Station PhxRow() =>
		Row("KPHX", "US", ["METAR"], 33.434, -112.012, "PHX", "Phoenix Sky Harbor Intl");

	/// <summary>A non-US station, otherwise a perfectly good METAR station.</summary>
	public static WxStationXmlDataModel.Station CanadianRow() => Row("CYYZ", "CA");

	/// <summary>A US station reporting TAF only, never METAR.</summary>
	public static WxStationXmlDataModel.Station TafOnlyRow() => Row("KTAF", siteTypes: ["TAF"]);

	/// <summary>A US METAR station with no ICAO ID at all.</summary>
	public static WxStationXmlDataModel.Station NoIcaoRow() => Row(null);

	/// <summary>A blank (not null) ICAO ID.</summary>
	public static WxStationXmlDataModel.Station BlankIcaoRow() => Row("   ");

	/// <summary>A US METAR station missing its latitude.</summary>
	public static WxStationXmlDataModel.Station NoLatitudeRow() => Row("KNOLA", latitude: null);

	/// <summary>A US METAR station missing its longitude.</summary>
	public static WxStationXmlDataModel.Station NoLongitudeRow() => Row("KNOLO", longitude: null);

	/// <summary>A US METAR station using the feed's out-of-range placeholder coordinates.</summary>
	public static WxStationXmlDataModel.Station PlaceholderCoordinatesRow(string icaoId = "KPLHD") =>
		Row(icaoId, latitude: -99.99, longitude: -99.99);

	/// <summary>A US METAR station whose latitude is out of the valid -90..90 range.</summary>
	public static WxStationXmlDataModel.Station OutOfRangeLatitudeRow(string icaoId = "KOORL") =>
		Row(icaoId, latitude: 95.0, longitude: -100.0);

	/// <summary>A US METAR station whose longitude is out of the valid -180..180 range.</summary>
	public static WxStationXmlDataModel.Station OutOfRangeLongitudeRow(string icaoId = "KOORG") =>
		Row(icaoId, latitude: 40.0, longitude: 190.0);

	/// <summary>A US METAR station whose coordinates are NaN.</summary>
	public static WxStationXmlDataModel.Station NanCoordinatesRow(string icaoId = "KNANX") =>
		Row(icaoId, latitude: double.NaN, longitude: double.NaN);

	/// <summary>A US METAR station whose latitude is positive infinity.</summary>
	public static WxStationXmlDataModel.Station InfiniteLatitudeRow(string icaoId = "KINFX") =>
		Row(icaoId, latitude: double.PositiveInfinity, longitude: -100.0);

	// ---- already-built WxStation records ----

	/// <summary>Builds a <see cref="WxStation"/> directly, bypassing Station-row building.</summary>
	public static WxStation BuiltStation(
		string icaoId = "KDTW",
		string iataId = "DTW",
		string site = "Detroit/Metro Wayne Cnty",
		double latitude = 42.212,
		double longitude = -83.353,
		string country = "US") =>
		new(icaoId, iataId, site, latitude, longitude, country);

	/// <summary>KDTW, matching <see cref="DtwRow"/>.</summary>
	public static WxStation Dtw() => BuiltStation();

	/// <summary>KPHX, matching <see cref="PhxRow"/>.</summary>
	public static WxStation Phx() => BuiltStation("KPHX", "PHX", "Phoenix Sky Harbor Intl", 33.434, -112.012);

	/// <summary>A station with no IATA ID and no site name.</summary>
	public static WxStation Bare(string icaoId = "KBARE") => BuiltStation(icaoId, string.Empty, string.Empty, 40.0, -100.0);
}
