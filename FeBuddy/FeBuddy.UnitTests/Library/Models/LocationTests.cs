namespace FeBuddy.UnitTests.Library.Models;

public class LocationTests
{
  [Theory]
  [InlineData("N041.32.50.000", "W108.18.16.490", 41.5472222, -108.3045806)]
  [InlineData("N043.31.08.418", "W112.03.50.103", 43.5190050, -112.0639175)]
  [InlineData("N000.00.00.000", "E000.00.00.000", 0, 0)]
  public void constructor_with_dms_input_should_set_dec_properties(string Lat, string Lon, double DecLat, double DecLon)
  {
    // Arrange
    // Act
    Location loc = new Location(Lat, Lon);
    // Assert
    Assert.Equal(Lat, loc.DmsLat);
    Assert.Equal(Lon, loc.DmsLon);
    Assert.Equal(DecLat, loc.DecLat);
    Assert.Equal(DecLon, loc.DecLon);
  }

  [Theory]
  [InlineData(41.5472222, -108.3045806, "N041.32.50.000", "W108.18.16.490")]
  [InlineData(43.5190050, -112.0639175, "N043.31.08.418", "W112.03.50.103")]
  [InlineData(0, 0, "N000.00.00.000", "E000.00.00.000")]
  public void constructor_with_dec_input_should_set_dms_properties(double Lat, double Lon, string DmsLat, string DmsLon)
  {
    // Arrange
    // Act
    Location loc = new Location(Lat, Lon);
    // Assert
    Assert.Equal(Lat, loc.DecLat);
    Assert.Equal(Lon, loc.DecLon);
    Assert.Equal(DmsLat, loc.DmsLat);
    Assert.Equal(DmsLon, loc.DmsLon);
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
    Location a = new Location(43.5190050, -112.0639175);
    Location b = new Location(43.5190050, -112.0639175);

    Assert.Equal(a, b);
    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }
}
