using FeBuddy.Core.Domain.Geo;

namespace FeBuddy.UnitTests.Domain.Geo;

/// <summary>
/// Covers <see cref="GeoMath"/>: DMS and decimal validation and conversion, distance, bearing,
/// and antimeridian handling.
/// </summary>
public sealed class GeoMathTests
{
	/// <summary>
	/// Check to make sure that if you pass in a valid DMS latitude AND longitude, that the method returns true.
	/// </summary>
	/// <param name="lat">string: lattitude</param>
	/// <param name="lon">string: longitude</param>
	[Theory]
	[InlineData("N043.31.08.418", "W112.03.50.103")]
	[InlineData("N051.52.16.430", "W176.40.26.800")]
	[InlineData("N000.00.00.000", "E000.00.00.000")]
	[InlineData("S000.00.00.000", "E000.00.00.000")]
	[InlineData("S000.00.00.000", "W112.03.50.103")]
	[InlineData("N043.31.08.418", "W000.00.00.000")]
	public void validate_dms_input_should_be_true(string lat, string lon)
	{
		bool result = GeoMath.IsValidDms(lat, lon);

		Assert.True(result);
	}

	/// <summary>
	/// Check to make sure that if you pass in an invalid DMS latitude OR longitude, that the method returns false.
	/// </summary>
	/// <param name="lat"></param>
	/// <param name="lon"></param>
	[Theory]
	[InlineData("043.31.08.418", "W112.03.50.103")]
	[InlineData("N051.52.16.430", "176.40.26.800")]
	[InlineData("N043.31.08.418", null)]
	[InlineData(null, "W112.03.50.103")]
	[InlineData(null, null)]
	[InlineData("", "176.40.26.800")]
	[InlineData("", "")]
	[InlineData("N043.31.08.418", "")]
	[InlineData("N043.31.08", "W112.03.50.103")]
	[InlineData("N0X3.31.08.418", "W112.03.50.103")]
	[InlineData("N091.00.00.000", "W112.03.50.103")]
	[InlineData("N043.31.08.418", "W112.60.50.103")]
	public void validate_dms_input_should_be_false(string? lat, string? lon)
	{
		bool result = GeoMath.IsValidDms(lat, lon);

		Assert.False(result);
	}

	/// <summary>
	/// Check to make sure that the conversion between DMS and decimal is correct.
	/// </summary>
	/// <param name="dms">string: DMS Latitude OR Longitude</param>
	/// <param name="expectedResult">double: Expected Result of the DMS in decimal format.</param>
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
	public void converting_dms_to_dec_should_be_correct(string dms, double expectedResult)
	{
		double actualResult = GeoMath.ToDecimal(dms);
		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Check to make sure that the conversion between decimal and DMS is correct.
	/// </summary>
	/// <param name="dec">double: Decimal version of the Lattitude OR longitude.</param>
	/// <param name="isLat">bool: True, if the Decimal passed in is a Lattitude, otherwise False.</param>
	/// <param name="expectedResult">string: DMS Latitude OR Longitude</param>
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
	[InlineData(-176.6741111, false, "W176.40.26.800")] // seconds >= .5 must not round up the whole seconds
	[InlineData(45.99999989, true, "N046.00.00.000")] // 59.9996" rounds up and carries into the next degree
	public void converting_dec_to_dms_should_be_correct(double dec, bool isLat, string expectedResult)
	{
		string actualResult = GeoMath.ToDms(dec, isLat);
		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Verify that a valid decimal latitude AND longitude returns true.
	/// </summary>
	/// <param name="lat">double: Lattitude in Decimal Format</param>
	/// <param name="lon">double: Longitude in Decimal Format</param>
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
	public void validate_decimal_input_should_be_true(double lat, double lon)
	{
		bool result = GeoMath.IsValidDecimal(lat, lon);
		Assert.True(result);
	}

	/// <summary>
	/// Verify that an invalid decimal latitude OR longitude returns false.
	/// </summary>
	/// <param name="lat">double: Lattitude in Decimal Format</param>
	/// <param name="lon">double: Longitude in Decimal Format</param>
	[Theory]
	[InlineData(-112.0639175, 179.111)]
	[InlineData(-91.0, 112.4545484)]
	[InlineData(91.0, 118.0)]
	[InlineData(91.4548156, 118.0)]
	[InlineData(45.0, -181.1813548)]
	[InlineData(-45.0, 200.1803658)]
	public void validate_decimal_input_should_be_false(double lat, double lon)
	{
		bool result = GeoMath.IsValidDecimal(lat, lon);
		Assert.False(result);
	}

	/// <summary>
	/// Check to make sure the distance between two DMS coordinates is correct when rounding.
	/// </summary>
	/// <param name="pointA">string: First point with Lattitude and Longitude seperated by a space.</param>
	/// <param name="pointB">string: Second point with Lattitude and Longitude seperated by a space.</param>
	/// <param name="expectedResult">double: Expected result rounded to the nearest whole number.</param>
	[Theory]
	[InlineData("N45.00.00.000 W045.00.00.000", "N50.00.00.000 W050.00.00.000", (double)362)]
	[InlineData("N45.00.00.000 W45.00.00.000", "N50.00.00.000 W50.00.00.000", (double)362)]
	[InlineData("N043.31.08.418 W112.03.50.103", "N041.32.50.000 W108.18.16.490", (double)204)]
	public void distance_rounded_whole_number_between_two_dms_coords_should_be_correct(string pointA, string pointB, double expectedResult)
	{
		Location _pointA = new(pointA.Split(' ')[0], pointA.Split(' ')[1]); ;
		Location _pointB = new(pointB.Split(' ')[0], pointB.Split(' ')[1]); ;

		double actualResult = GeoMath.Distance(_pointA, _pointB);
		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Check to make sure the distance between two decimal coordinates is correct when rounding.
	/// </summary>
	/// <param name="pointALat">double: First point Lattitude</param>
	/// <param name="pointALon">double: First point Longitude</param>
	/// <param name="pointBLat">double: Second point Lattitude</param>
	/// <param name="pointBLon">double: Second point Longitude</param>
	/// <param name="expectedResult">double: Expected distance between the two points when rounded to the nearest whole number.</param>
	[Theory]
	[InlineData(43.5190050, -112.0639175, 41.5472222, -108.3045806, (double)204)]
	[InlineData(45.0, -45.0, 46.0, -46.0, (double)73)]
	[InlineData(45.0, -45.0, 45.1, -45.1, (double)7)]
	[InlineData(45.0, -45.0, 45.0001, -45.0001, (double)0)]
	public void distance_rounded_whole_number_between_two_decimal_coords_should_be_correct(double pointALat, double pointALon, double pointBLat, double pointBLon, double expectedResult)
	{
		Location _pointA = new(pointALat, pointALon);
		Location _pointB = new(pointBLat, pointBLon);

		double actualResult = GeoMath.Distance(_pointA, _pointB);

		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Check to make sure that the distance between two DMS coordinates is correct when not rounding.
	/// </summary>
	/// <param name="pointALat">double: First point Lattitude<</param>
	/// <param name="pointALon">double: First point Longitude<</param>
	/// <param name="pointBLat">double: Second point Lattitude</param>
	/// <param name="pointBLon">double: Second point Longitude</param>
	/// <param name="expectedResult">double: Expected result for the distance not rounded at all.</param>
	[Theory]
	[InlineData(45, -45, 45.0001, -45.0001, 0.007353)]
	[InlineData(45, -45, 46.665543614161486, -45, 100.0)]
	public void distance_not_rounded_between_two_decimal_coords_should_be_correct(double pointALat, double pointALon, double pointBLat, double pointBLon, double expectedResult)
	{
		Location _pointA = new(pointALat, pointALon);
		Location _pointB = new(pointBLat, pointBLon);

		double actualResult = GeoMath.Distance(_pointA, _pointB, round: false);

		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Check two coordintes to see if they cross the antimeridian or not.
	/// </summary>
	/// <param name="startLat">double: Starting Lattitude</param>
	/// <param name="startLon">double: Starting Longitude</param>
	/// <param name="endLat">double: Ending Lattitude</param>
	/// <param name="endLon">double: Ending Longitude</param>
	/// <param name="expectedResult">bool: Expected Result, True, if it does cross the antimeridian, otherwise false.</param>
	[Theory]
	[InlineData(35, 179, 35, -179, true)]
	[InlineData(38, -162, 25, -119, false)]
	[InlineData(40, -170, 40, 170, true)]
	[InlineData(40, 170, 40, -170, true)]
	[InlineData(40, -10, 40, 10, false)]
	// Regression case: a real AWY_SEG_ALT segment entirely within +140..+142 longitude (near
	// Guam), nowhere close to the antimeridian. A bearing-based CrossesAntimeridian reports this
	// pair as crossing, which crashed AntimeridianSplitter on a real NASR dataset's airways.
	[InlineData(21, 140.6, 16.75, 142.16666666, false)]
	public void crosses_the_am_should_be_correct(double startLat, double startLon, double endLat, double endLon, bool expectedResult)
	{
		Location _pointA = new(startLat, startLon);
		Location _pointB = new(endLat, endLon);
		bool actualResult = GeoMath.CrossesAntimeridian(_pointA, _pointB);
		Assert.Equal(expectedResult, actualResult);
	}

	/// <summary>
	/// Check the bearing between two coordinates.
	/// </summary>
	/// <param name="pointALat">double: First point Lattitude</param>
	/// <param name="pointALon">double: First point Longitude</param>
	/// <param name="pointBLat">double: Second point Lattitude</param>
	/// <param name="pointBLon">double: Second point Longitude</param>
	/// <param name="expectedResult">double: Expected Bearing from Point A to Point B</param>
	[Theory]
	[InlineData(37.4223878, -122.0841877, 52.3752182, 4.8839765, 29.787552309755483)]
	[InlineData(52.3752182, 4.8839765, 37.4223878, -122.0841877, 319.73956262516884)]
	[InlineData(45, 45, 46, 45, 0.0)]
	[InlineData(45, 45, 49, 45.5, 4.6907374307851342)]
	[InlineData(45, 45, 45.089517481040396, 45.012209062406434, 5.5000000000001137)]
	public void get_bearing_between_two_decimal_coords_should_be_correct(double pointALat, double pointALon, double pointBLat, double pointBLon, double expectedResult)
	{
		Location _pointA = new(pointALat, pointALon);
		Location _pointB = new(pointBLat, pointBLon);
		double actualResult = GeoMath.Bearing(_pointA, _pointB);
		Assert.Equal(expectedResult, actualResult);
	}

	[Theory]
	[InlineData(40, -170, 40, 170, 40, -180, 40, 180)]
	[InlineData(20, -170, 40, 170, 30, -180, 30, 180)]
	[InlineData(30, -170, -30, 170, 0, -180, 0, 180)]
	public void split_line_segment_at_antimeridian_should_return_four_coordinates(
		double startLat, double startLon, double endLat, double endLon,
		double expectedLat1, double expectedLon1, double expectedLat2, double expectedLon2)
	{
		var pointA = new Location(startLat, startLon);
		var pointB = new Location(endLat, endLon);

		var result = GeoMath.SplitLineSegmentAtAntimeridian(pointA, pointB);

		Assert.NotNull(result);
		Assert.Equal(4, result.Count);
		Assert.Equal(pointA, result[0]);
		Assert.Equal(new Location(expectedLat1, expectedLon1), result[1] as Location);
		Assert.Equal(new Location(expectedLat2, expectedLon2), result[2] as Location);
		Assert.Equal(pointB, result[3]);
	}

	/// <summary>
	/// Verifies PointAtDistanceAndBearing by round-tripping through the class's own Distance
	/// and Bearing methods: traveling a known distance at a known bearing from an origin must
	/// land at a point that is that same distance and bearing away from the origin.
	/// </summary>
	[Theory]
	[InlineData(45.0, -90.0, 0.0, 100.0)]
	[InlineData(45.0, -90.0, 90.0, 250.0)]
	[InlineData(0.0, 0.0, 225.0, 500.0)]
	[InlineData(-30.0, 160.0, 315.0, 75.0)]
	[InlineData(80.0, 170.0, 45.0, 300.0)]
	public void point_at_distance_and_bearing_lands_the_expected_distance_and_bearing_from_origin(
		double lat, double lon, double bearingDegrees, double distanceNm)
	{
		Location origin = new(lat, lon);

		Location destination = GeoMath.PointAtDistanceAndBearing(origin, bearingDegrees, distanceNm);

		double actualDistance = GeoMath.Distance(origin, destination, round: false);
		Assert.Equal(distanceNm, actualDistance, precision: 1);

		double actualBearing = GeoMath.Bearing(origin, destination);
		double bearingDifference = Math.Abs(actualBearing - bearingDegrees);
		bearingDifference = Math.Min(bearingDifference, 360 - bearingDifference);
		Assert.True(bearingDifference < 0.5, $"Expected bearing {bearingDegrees}, got {actualBearing}.");
	}

	/// <summary>
	/// A destination whose raw longitude would fall outside [-180, 180] (crossing the
	/// antimeridian) must be normalized back into range rather than throwing.
	/// </summary>
	[Fact]
	public void point_at_distance_and_bearing_normalizes_longitude_across_the_antimeridian()
	{
		Location origin = new(0.0, 179.9);

		Location destination = GeoMath.PointAtDistanceAndBearing(origin, bearingDegrees: 90.0, distanceNm: 50.0);

		Assert.InRange(destination.DecLon, -180.0, 180.0);
	}
}
