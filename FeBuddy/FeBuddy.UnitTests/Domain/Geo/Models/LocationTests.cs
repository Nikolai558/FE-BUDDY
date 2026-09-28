namespace FeBuddy.UnitTests.Domain.Geo.Models;

/// <summary>
/// Covers <see cref="Location"/>: building from DMS or decimal input, rejecting invalid input, and equality.
/// </summary>
public sealed class LocationTests
{
	[Theory]
	[InlineData("N041.32.50.000", "W108.18.16.490", 41.5472222, -108.3045806)]
	[InlineData("N043.31.08.418", "W112.03.50.103", 43.5190050, -112.0639175)]
	[InlineData("N000.00.00.000", "E000.00.00.000", 0, 0)]
	public void constructor_with_dms_input_should_set_dec_properties(string lat, string lon, double decLat, double decLon)
	{
		Location loc = new(lat, lon);
		Assert.Equal(lat, loc.DmsLat);
		Assert.Equal(lon, loc.DmsLon);
		Assert.Equal(decLat, loc.DecLat);
		Assert.Equal(decLon, loc.DecLon);
	}

	[Theory]
	[InlineData(41.5472222, -108.3045806, "N041.32.50.000", "W108.18.16.490")]
	[InlineData(43.5190050, -112.0639175, "N043.31.08.418", "W112.03.50.103")]
	[InlineData(0, 0, "N000.00.00.000", "E000.00.00.000")]
	public void constructor_with_dec_input_should_set_dms_properties(double lat, double lon, string dmsLat, string dmsLon)
	{
		Location loc = new(lat, lon);
		Assert.Equal(lat, loc.DecLat);
		Assert.Equal(lon, loc.DecLon);
		Assert.Equal(dmsLat, loc.DmsLat);
		Assert.Equal(dmsLon, loc.DmsLon);
	}

	[Fact]
	public void constructor_with_invalid_dms_input_should_throw()
	{
		Assert.Throws<ArgumentException>(() => new Location("N091.00.00.000", "W112.03.50.103"));
	}

	[Fact]
	public void constructor_with_invalid_dec_input_should_throw()
	{
		Assert.Throws<ArgumentException>(() => new Location(91.0, -112.0));
	}

	[Fact]
	public void equal_locations_have_equal_hash_codes()
	{
		Location a = new(43.5190050, -112.0639175);
		Location b = new(43.5190050, -112.0639175);

		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}
}
