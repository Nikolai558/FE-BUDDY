namespace FeBuddy.Core.Domain.Geo.Models;

/// <summary>A point on the Earth, in decimal degrees.</summary>
public sealed record Location
{
	/// <summary>Creates a location.</summary>
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

	/// <summary>Latitude in decimal degrees, -90 to 90.</summary>
	public double DecLat { get; }

	/// <summary>Longitude in decimal degrees, -180 to 180.</summary>
	public double DecLon { get; }
}
