using FEBuddyLibrary.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Handlers;
public class CoordinateHandlerTests
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
    bool result = CoordinateHandler.IsValidDMS(Lat, Lon);

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
    bool result = CoordinateHandler.IsValidDMS(Lat, Lon);

    // Assert
    Assert.False(result);
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
    double actualResult = CoordinateHandler.ToDecimal(Dms);
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
    string actualResult = CoordinateHandler.ToDMS(Dec, isLat);
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
    bool result = CoordinateHandler.IsValidDecimal(Lat, Lon);
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
    bool result = CoordinateHandler.IsValidDecimal(Lat, Lon);
    // Assert
    Assert.False(result);
  }


  [Theory]
  [InlineData("N45.00.00.000 W045.00.00.000", "N50.00.00.000 W050.00.00.000", (double)362)]
  [InlineData("N45.00.00.000 W45.00.00.000", "N50.00.00.000 W50.00.00.000", (double)362)]
  [InlineData("N043.31.08.418 W112.03.50.103", "N041.32.50.000 W108.18.16.490", (double)204)]
  public void distance_Rounded_whole_number_between_two_dms_coords_should_be_correct(string PointA, string PointB, double ExpectedResult)
  {
    // Arrange
    Location _pointA = new Location(PointA.Split(' ')[0], PointA.Split(' ')[1]); ;
    Location _pointB = new Location(PointB.Split(' ')[0], PointB.Split(' ')[1]); ;

    // Act
    double actualResult = CoordinateHandler.Distance(_pointA, _pointB);
    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(43.5190050, -112.0639175, 41.5472222, -108.3045806, (double)204)]
  [InlineData(45.0, -45.0, 46.0, -46.0, (double)73)]
  [InlineData(45.0, -45.0, 45.1, -45.1, (double)7)]
  [InlineData(45.0, -45.0, 45.0001, -45.0001, (double)0)]
  public void distance_Rounded_whole_number_between_two_decimal_coords_should_be_correct(double PointALat, double PointALon, double PointBLat, double PointBLon, double ExpectedResult)
  {
    // Arrange
    Location _pointA = new Location(PointALat, PointALon);
    Location _pointB = new Location(PointBLat, PointBLon);

    // Act
    double actualResult = CoordinateHandler.Distance(_pointA, _pointB);

    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(45, -45, 45.0001, -45.0001, 0.007353)]
  [InlineData(45, -45, 46.665543614161486, -45, 100.0)]
  public void distance_NOT_ROUNDED_between_two_decimal_coords_should_be_correct(double PointALat, double PointALon, double PointBLat, double PointBLon, double ExpectedResult)
  {
    // Arrange
    Location _pointA = new Location(PointALat, PointALon);
    Location _pointB = new Location(PointBLat, PointBLon);

    // Act
    double actualResult = CoordinateHandler.Distance(_pointA, _pointB, Round: false);

    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(35, 179, 35, -179, true)] 
  [InlineData(38, -162, 25, -119, false)]
  public void crosses_the_am_should_be_correct(double StartLat, double StartLon, double EndLat, double EndLon, bool ExpectedResult)
  {
    // Arrange
    Location _pointA = new Location(StartLat, StartLon);
    Location _pointB = new Location(EndLat, EndLon);
    // Act
    bool actualResult = CoordinateHandler.CrossesAntimeridian(_pointA, _pointB);
    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }

  [Theory]
  [InlineData(37.4223878, -122.0841877, 52.3752182, 4.8839765, 29.787552309755483)]
  [InlineData(52.3752182, 4.8839765, 37.4223878, -122.0841877, 319.73956262516884)]
  [InlineData(45, 45, 46, 45, 0.0)]
  [InlineData(45, 45, 49, 45.5, 4.6907374307851342)]
  [InlineData(45, 45, 45.089517481040396, 45.012209062406434, 5.5000000000001137)]
  public void get_bearing_between_two_decimal_coords_should_be_correct(double PointALat, double PointALon, double PointBLat, double PointBLon, double ExpectedResult)
  {
    // Arrange
    Location _pointA = new Location(PointALat, PointALon); 
    Location _pointB = new Location(PointBLat, PointBLon);
    // Act
    double actualResult = CoordinateHandler.Bearing(_pointA, _pointB);
    // Assert
    Assert.Equal(ExpectedResult, actualResult);
  }
}
