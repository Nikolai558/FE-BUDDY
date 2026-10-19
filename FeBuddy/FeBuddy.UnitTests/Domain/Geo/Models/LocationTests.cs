namespace FeBuddy.UnitTests.Domain.Geo.Models;

/// <summary>
/// Covers <see cref="Location"/>: it holds decimal degrees, rejects a value out of range, and two
/// locations with the same values are equal.
/// </summary>
public sealed class LocationTests
{
	[Theory]
	[InlineData(41.5472222, -108.3045806)]
	[InlineData(-90, 180)]
	[InlineData(0, 0)]
	public void constructor_keeps_the_decimal_degrees(double lat, double lon)
	{
		Location loc = new(lat, lon);

		Assert.Equal(lat, loc.DecLat);
		Assert.Equal(lon, loc.DecLon);
	}

	[Theory]
	[InlineData(91.0, -112.0)]
	[InlineData(43.5, -180.5)]
	public void constructor_with_a_value_out_of_range_throws(double lat, double lon)
	{
		Assert.Throws<ArgumentException>(() => new Location(lat, lon));
	}

	[Fact]
	public void equal_locations_have_equal_hash_codes()
	{
		Location a = new(43.5190050, -112.0639175);
		Location b = new(43.5190050, -112.0639175);

		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());
		Assert.NotEqual(a, new Location(43.5190050, -112.0639176));
	}
}
