namespace FeBuddy.Core.Domain.Geo.Models;

/// <summary>
/// A point on the Earth, held in both decimal degrees and DMS text. Setting either form updates
/// the other.
/// </summary>
/// <remarks>
/// The geometry code works in decimal degrees; the DMS form is for display and for the
/// DMS-based formats FE-Buddy reads and writes. See <see cref="GeoMath"/> for the DMS format.
/// </remarks>
public class Location
{
	private string _dmsLat;
	private string _dmsLon;
	private double _decLat;
	private double _decLon;

	/// <summary>Creates a location from DMS text.</summary>
	/// <param name="latitude">Latitude, e.g. <c>N043.31.08.418</c>.</param>
	/// <param name="longitude">Longitude, e.g. <c>W112.03.50.103</c>.</param>
	/// <exception cref="ArgumentException">Thrown when either value is not valid DMS.</exception>
	public Location(string latitude, string longitude)
	{
		if (!GeoMath.IsValidDms(latitude, longitude))
		{
			throw new ArgumentException("Invalid DMS input when creating a Location class.");
		}

		DmsLat = latitude;
		DmsLon = longitude;
	}

	/// <summary>Creates a location from decimal degrees.</summary>
	/// <param name="latitude">Latitude, -90 to 90.</param>
	/// <param name="longitude">Longitude, -180 to 180.</param>
	/// <exception cref="ArgumentException">Thrown when either value is out of range.</exception>
	public Location(double latitude, double longitude)
	{
		if (!GeoMath.IsValidDecimal(latitude, longitude))
		{
			throw new ArgumentException("Invalid decimal input when creating a Location class.");
		}

		DecLat = latitude;
		DecLon = longitude;
	}

	/// <summary>Latitude as DMS text, e.g. <c>N043.31.08.418</c>. Setting it updates <see cref="DecLat"/>.</summary>
	public string DmsLat
	{
		get => _dmsLat;
		set
		{
			_dmsLat = value;
			_decLat = GeoMath.ToDecimal(value);
		}
	}

	/// <summary>Longitude as DMS text, e.g. <c>W112.03.50.103</c>. Setting it updates <see cref="DecLon"/>.</summary>
	public string DmsLon
	{
		get => _dmsLon;
		set
		{
			_dmsLon = value;
			_decLon = GeoMath.ToDecimal(value);
		}
	}

	/// <summary>Latitude in decimal degrees. Setting it updates <see cref="DmsLat"/>.</summary>
	public double DecLat
	{
		get => _decLat;
		set
		{
			_decLat = value;
			_dmsLat = GeoMath.ToDms(value, isLatitude: true);
		}
	}

	/// <summary>Longitude in decimal degrees. Setting it updates <see cref="DmsLon"/>.</summary>
	public double DecLon
	{
		get => _decLon;
		set
		{
			_decLon = value;
			_dmsLon = GeoMath.ToDms(value, isLatitude: false);
		}
	}

	/// <summary>Two locations are equal when both their decimal and DMS values match.</summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="Location"/> with the same values.</returns>
	public override bool Equals(object? obj) =>
		obj is Location other
		&& GetType() == other.GetType()
		&& _dmsLat == other._dmsLat
		&& _dmsLon == other._dmsLon
		&& _decLat == other._decLat
		&& _decLon == other._decLon;

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_dmsLat, _dmsLon, _decLat, _decLon);
}
