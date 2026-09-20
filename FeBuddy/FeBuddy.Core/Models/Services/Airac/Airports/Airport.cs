namespace FeBuddy.Core.Models.Services.Airac.Airports;

/// <summary>
/// One airport, assembled from every NASR source the Airports sub-service reads: the base
/// record, its runways, its CTAF / weather frequencies, and the class airspace it underlies.
/// </summary>
/// <remarks>
/// Built once per run by <c>AirportBuilder</c>. Every downstream Airports service (GeoJSON,
/// alias) consumes this and never touches the NASR models again, so the "which CSV did this
/// value come from" question is answered in exactly one place.
/// </remarks>
public sealed record Airport
{
	/// <summary>FAA identifier, <c>APT_BASE.ARPT_ID</c> (e.g. <c>SEA</c>).</summary>
	public required string FaaId { get; init; }

	/// <summary>ICAO identifier, <c>APT_BASE.ICAO_ID</c> (e.g. <c>KSEA</c>), or <see langword="null"/> when the airport has none.</summary>
	public string? IcaoId { get; init; }

	/// <summary>Airport name, <c>APT_BASE.ARPT_NAME</c>.</summary>
	public required string Name { get; init; }

	/// <summary>Airport reference point latitude in decimal degrees.</summary>
	public required double Latitude { get; init; }

	/// <summary>Airport reference point longitude in decimal degrees.</summary>
	public required double Longitude { get; init; }

	/// <summary>
	/// Field elevation in feet, <c>APT_BASE.ELEV</c>, or <see langword="null"/> when NASR
	/// publishes none. Nullable so a field with no published elevation reads as blank rather
	/// than as sea level.
	/// </summary>
	public double? Elevation { get; init; }

	/// <summary>Responsible ARTCC identifier, <c>APT_BASE.RESP_ARTCC_ID</c>.</summary>
	public required string RespArtccId { get; init; }

	/// <summary>Traffic pattern altitude in feet, <c>APT_BASE.TPA</c>, or <see langword="null"/> when NASR does not publish one.</summary>
	public int? TrafficPatternAltitude { get; init; }

	/// <summary>Tie-in Flight Service Station identifier, <c>APT_BASE.FSS_ID</c>.</summary>
	public string? FssId { get; init; }

	/// <summary>Tower type, already mapped to its display form by <c>AirportFieldMaps</c> (e.g. <c>TWR</c>, <c>No-TWR</c>).</summary>
	public required string TowerType { get; init; }

	/// <summary>Facility type, already mapped to its display form by <c>AirportFieldMaps</c> (e.g. <c>AIRPORT</c>, <c>HELIPORT</c>).</summary>
	public required string FacilityType { get; init; }

	/// <summary>CTAF frequency from <c>FRQ</c>, or <see langword="null"/> when the airport publishes none.</summary>
	public string? CtafFrequency { get; init; }

	/// <summary>AWOS/ASOS frequency from <c>FRQ</c>, or <see langword="null"/> when the airport publishes none.</summary>
	public string? WeatherFrequency { get; init; }

	/// <summary>The <c>FREQ_USE</c> value the <see cref="WeatherFrequency"/> came from (e.g. <c>ASOS</c>).</summary>
	public string? WeatherFrequencyUse { get; init; }

	/// <summary>
	/// The class airspace overlying the airport as a display string (e.g. <c>Delta</c>,
	/// <c>Bravo, Delta, &amp; Echo</c>), or <see langword="null"/> when <c>CLS_ARSP</c> has no
	/// row for it.
	/// </summary>
	public string? ClassAirspace { get; init; }

	/// <summary>
	/// The airport's runways - only true runways, i.e. <c>RWY_ID</c> values containing a
	/// <c>/</c>. Helipads and other single-point surfaces are excluded upstream.
	/// </summary>
	public IReadOnlyList<AirportRunway> Runways { get; init; } = Array.Empty<AirportRunway>();

	/// <summary>
	/// The longest entry in <see cref="Runways"/>, or <see langword="null"/> when the airport
	/// has none. Ties break on <c>RWY_ID</c> ordinal so output is stable cycle to cycle.
	/// </summary>
	public AirportRunway? LongestRunway { get; init; }
}
