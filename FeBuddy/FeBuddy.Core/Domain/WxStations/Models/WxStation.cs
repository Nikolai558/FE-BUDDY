namespace FeBuddy.Core.Domain.WxStations.Models;

/// <summary>
/// One weather-reporting station included in the Wx Stations sub-service's output, built from a
/// <c>stations.cache.xml</c> <c>&lt;Station&gt;</c> row that survived
/// <see cref="Application.Airac.WxStations.WxStationBuilder.Read"/>'s selection rules.
/// </summary>
/// <param name="IcaoId">ICAO station identifier. Never blank - a station without one is excluded.</param>
/// <param name="IataId">IATA station identifier, or empty when the station has none.</param>
/// <param name="Site">The station's site/location name, or empty when the source lists none.</param>
/// <param name="Latitude">Latitude in decimal degrees, within -90..90.</param>
/// <param name="Longitude">Longitude in decimal degrees, within -180..180.</param>
/// <param name="Country">The station's country code, e.g. <c>US</c>.</param>
public sealed record WxStation(
	string IcaoId,
	string IataId,
	string Site,
	double Latitude,
	double Longitude,
	string Country);
