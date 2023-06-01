namespace UnitTests.Library.Models;

public class LocationTests
{

  [Theory]
  [InlineData("N043.31.08.418", "W112.03.50.103")]
  [InlineData("N051.52.16.430", "W176.40.26.800")]
  [InlineData("N000.00.00.000", "E000.00.00.000")]
  [InlineData("S000.00.00.000", "E000.00.00.000")]
  [InlineData("S000.00.00.000", "W112.03.50.103")]
  [InlineData("N043.31.08.418", "W000.00.00.000")]
  public void validate_dms_input_should_be_true(string Lat, string Lon)
  {
    // Arrange

    // Act
    bool result = Location.IsValidDMS(Lat, Lon);

    // Assert
    Assert.True(result);
  }

  [Theory]
  [InlineData("043.31.08.418", "W112.03.50.103")]
  [InlineData("N051.52.16.430", "176.40.26.800")]
  [InlineData("N043.31.08.418", null)]
  [InlineData(null, "W112.03.50.103")]
  [InlineData(null, null)]
  [InlineData("", "176.40.26.800")]
  [InlineData("", "")]
  [InlineData("N043.31.08.418", "")]
  public void validate_dms_input_should_be_false(string Lat, string Lon)
  {
    // Arrange

    // Act
    bool result = Location.IsValidDMS(Lat, Lon);

    // Assert
    Assert.False(result);
  }


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

  [Theory]
  [InlineData("N045.00.00.000", 45.0)]
  [InlineData("S045.00.00.000", -45.0)]
  [InlineData("E118.00.00.000", 118.0)]
  [InlineData("W118.00.00.000", -118.0)]
  [InlineData("N041.32.50.000", 41.5472222)]
  [InlineData("W112.07.40.000", -112.1277778)]
  [InlineData("N045.49.43.089", 45.8286358)]
  [InlineData("W108.18.16.490", -108.3045806)]
  [InlineData("N043.31.08.418", 43.5190050)]
  [InlineData("W112.03.50.103", -112.0639175)]
  public void converting_dms_to_dec_should_be_correct(string Dms, double ExpectedResult)
  {
    // Arrange
    // Act
    double actualResult = Location.ToDecimal(Dms);
    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(45.0, true, "N045.00.00.000")]
  [InlineData(-45.0, true, "S045.00.00.000")]
  [InlineData(118.0, false, "E118.00.00.000")]
  [InlineData(-118.0, false, "W118.00.00.000")]
  [InlineData(41.5472222, true, "N041.32.50.000")]
  [InlineData(-112.1277778, false, "W112.07.40.000")]
  [InlineData(45.8286358, true, "N045.49.43.089")]
  [InlineData(-108.3045806, false, "W108.18.16.490")]
  [InlineData(43.5190050, true, "N043.31.08.418")]
  [InlineData(-112.0639175, false, "W112.03.50.103")]
  public void converting_dec_to_dms_should_be_correct(double Dec, bool isLat, string ExpectedResult)
  {
    // Arrange
    // Act
    string actualResult = Location.ToDMS(Dec, isLat);
    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(45.0, 10.0)]
  [InlineData(-45.0, 180)]
  [InlineData(41, 118.0)]
  [InlineData(25, -118.0)]
  [InlineData(41.5472222, 112)]
  [InlineData(88, -112.1277778)]
  [InlineData(45.8286358, 179.55)]
  [InlineData(-88, -108.3045806)]
  [InlineData(43.5190050, -179.55)]
  [InlineData(20, -112.0639175)]
  [InlineData(-90, -180)]
  [InlineData(90, 180)]
  public void validate_decimal_input_should_be_true(double Lat, double Lon)
  {
    // Arrange
    // Act
    bool result = Location.IsValidDecimal(Lat, Lon);
    // Assert
    Assert.True(result);
  }

  [Theory]
  [InlineData(-112.0639175, 179.111)]
  [InlineData(-91.0, 112.4545484)]
  [InlineData(91.0, 118.0)]
  [InlineData(91.4548156, 118.0)]
  [InlineData(45.0, -181.1813548)]
  [InlineData(-45.0, 200.1803658)]
  public void validate_decimal_input_should_be_false(double Lat, double Lon)
  {
    // Arrange
    // Act
    bool result = Location.IsValidDecimal(Lat, Lon);
    // Assert
    Assert.False(result);
  }
}
