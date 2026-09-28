namespace FeBuddy.Core.Infrastructure.WxStations.Models;

/// <summary>
/// Row model for the aviationweather.gov Wx Stations cache file (<c>stations.cache.xml</c>).
/// </summary>
public class WxStationXmlDataModel
{
	/// <summary>One <c>&lt;Station&gt;</c> element under <c>response/data</c>.</summary>
	public class Station
	{
		/// <summary>
		/// Station Identifier
		/// _Src: stations.cache.xml(station_id)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// Usually the same as <see cref="IcaoId"/>, but present even for stations (buoys, etc.)
		/// that have no ICAO identifier.
		/// </remarks>
		public string? StationId { get; set; }

		/// <summary>
		/// ICAO Station Identifier
		/// _Src: stations.cache.xml(icao_id)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? IcaoId { get; set; }

		/// <summary>
		/// IATA Station Identifier
		/// _Src: stations.cache.xml(iata_id)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? IataId { get; set; }

		/// <summary>
		/// FAA Station Identifier
		/// _Src: stations.cache.xml(faa_id)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? FaaId { get; set; }

		/// <summary>
		/// WMO Station Identifier
		/// _Src: stations.cache.xml(wmo_id)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? WmoId { get; set; }

		/// <summary>
		/// Station Latitude, in decimal degrees
		/// _Src: stations.cache.xml(latitude)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> when the element is missing, empty, or not a valid
		/// invariant-culture number - never validated against the -90..90 range here, so a
		/// placeholder value (the feed uses <c>-99.99</c>) still reads as a number.
		/// </remarks>
		public double? Latitude { get; set; }

		/// <summary>
		/// Station Longitude, in decimal degrees
		/// _Src: stations.cache.xml(longitude)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> when the element is missing, empty, or not a valid
		/// invariant-culture number - never validated against the -180..180 range here, so a
		/// placeholder value (the feed uses <c>-99.99</c>) still reads as a number.
		/// </remarks>
		public double? Longitude { get; set; }

		/// <summary>
		/// Station Elevation, in meters
		/// _Src: stations.cache.xml(elevation_m)
		/// _DataType: double
		/// _Nullable: Yes
		/// </summary>
		/// <remarks>
		/// <see langword="null"/> when the element is missing, empty, or not a valid
		/// invariant-culture number.
		/// </remarks>
		public double? ElevationM { get; set; }

		/// <summary>
		/// Site Name/Description
		/// _Src: stations.cache.xml(site)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Site { get; set; }

		/// <summary>
		/// State/Province Code
		/// _Src: stations.cache.xml(state)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? State { get; set; }

		/// <summary>
		/// Country Code
		/// _Src: stations.cache.xml(country)
		/// _DataType: string
		/// _Nullable: Yes
		/// </summary>
		public string? Country { get; set; }

		/// <summary>
		/// Site Types
		/// _Src: stations.cache.xml(site_type)
		/// _DataType: string list
		/// _Nullable: No
		/// </summary>
		/// <remarks>
		/// The names of <c>&lt;site_type&gt;</c>'s child elements, e.g. <c>METAR</c>, <c>TAF</c> -
		/// each an empty marker element (<c>&lt;METAR/&gt;</c>), so only its presence matters,
		/// never a value. Empty when the station has no <c>&lt;site_type&gt;</c> children.
		/// </remarks>
		public IReadOnlyList<string> SiteTypes { get; set; } = [];
	}
}
