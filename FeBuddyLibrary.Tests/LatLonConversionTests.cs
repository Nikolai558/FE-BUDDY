using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FeBuddyLibrary.Helpers;
using Xunit;

namespace FeBuddyLibrary.Tests
{
    /// <summary>
    /// Golden-value coverage for the coordinate conversion math in LatLonHelpers. These
    /// functions produce FE-BUDDY's actual SCT / GeoJSON / DXF output; a regression here
    /// silently ships wrong coordinates. Pins the observable behaviour so the 3.0 rewrite
    /// has a reference to check against.
    /// </summary>
    public class LatLonConversionTests
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---- CreateDMS: decimal degrees -> [NSEW]DDD.MM.SS.mmm --------------

        [Theory]
        [InlineData(0.0, true, "N000.00.00.000")]
        [InlineData(39.85, true, "N039.51.00.000")]
        [InlineData(-45.0, true, "S045.00.00.000")]
        [InlineData(45.25, true, "N045.15.00.000")]
        [InlineData(-75.5, false, "W075.30.00.000")]
        [InlineData(-120.75, false, "W120.45.00.000")]
        [InlineData(120.5, false, "E120.30.00.000")]
        public void CreateDMS_ConvertsKnownDecimalDegrees(double value, bool lat, string expected)
        {
            Assert.Equal(expected, LatLonHelpers.CreateDMS(value, lat));
        }

        // ---- CreateDecFormat: [NSEW]DDD.MM.SS.mmm -> decimal degrees -------

        [Theory]
        [InlineData("N000.00.00.000", 0.0)]
        [InlineData("N039.51.00.000", 39.85)]
        [InlineData("S039.51.00.000", -39.85)]
        [InlineData("E075.30.00.000", 75.5)]
        [InlineData("W075.30.00.000", -75.5)]
        [InlineData("N039.51.36.000", 39.86)]
        [InlineData("S000.30.00.000", -0.5)]
        public void CreateDecFormat_ConvertsKnownDmsStrings(string dms, double expectedDegrees)
        {
            var result = LatLonHelpers.CreateDecFormat(dms, roundSixPlaces: false);

            Assert.Equal(expectedDegrees, double.Parse(result, Inv), precision: 6);
        }

        [Fact]
        public void CreateDecFormat_RoundSixPlaces_TruncatesToSixDecimals()
        {
            var result = LatLonHelpers.CreateDecFormat("N039.51.36.000", roundSixPlaces: true);

            Assert.Equal("39.86", result);
        }

        [Theory]
        [InlineData("039.51.00.000")]   // no N/S/E/W prefix
        [InlineData("X039.51.00.000")]  // unrecognised prefix
        public void CreateDecFormat_MissingOrBadDeclination_Throws(string value)
        {
            Assert.Throws<Exception>(() => LatLonHelpers.CreateDecFormat(value, roundSixPlaces: false));
        }

        // ---- Round trip ---------------------------------------------------------

        [Theory]
        [InlineData("N039.51.00.000", true)]
        [InlineData("S045.00.00.000", true)]
        [InlineData("W075.30.00.000", false)]
        [InlineData("E120.15.00.000", false)]
        [InlineData("N012.34.56.000", true)]
        public void CreateDecFormat_ThenCreateDMS_RoundTrips(string dms, bool lat)
        {
            var degrees = double.Parse(LatLonHelpers.CreateDecFormat(dms, roundSixPlaces: false), Inv);

            Assert.Equal(dms, LatLonHelpers.CreateDMS(degrees, lat));
        }

        // ---- CorrectLatLon: raw FAA text -> standard format -------------------

        [Theory]
        [InlineData("N039.51.00.000", true, "N039.51.00.000")]
        [InlineData("N39.51.0.0", true, "N039.51.00.000")]      // padding
        [InlineData("W075.14.30.500", false, "W075.14.30.500")]
        [InlineData("W75.9.5.7", false, "W075.09.05.700")]      // padding
        public void CorrectLatLon_NormalisesToStandardFormat(string raw, bool lat, string expected)
        {
            Assert.Equal(expected, LatLonHelpers.CorrectLatLon(raw, lat, ConvertEast: false));
        }

        [Fact]
        public void CorrectLatLon_ConvertEast_TurnsEastCoordIntoWestEquivalent()
        {
            var result = LatLonHelpers.CorrectLatLon("E075.30.00.000", Lat: false, ConvertEast: true);

            Assert.StartsWith("W", result);
        }

        private static List<double> Position(double lon, double lat) => new() { lon, lat };

        [Fact]
        public void CheckAMCrossing_EndpointExactlyOnAntimeridian_ProducesADuplicateSeamPoint()
        {
            // start point is exactly on the antimeridian, end point is on the other side.
            var coords = LatLonHelpers.CheckAMCrossing(11.708889, -180.0, 11.5, 178.0);

            Assert.Equal(4, coords.Count); // treated as an AM crossing -> 4-point split
            var start = (List<double>)coords[0];
            var seam = (List<double>)coords[1];
            Assert.Equal(start[0], seam[0]);
            Assert.Equal(start[1], seam[1]); // the seam landed right on top of the start -> duplicate
        }

        [Fact]
        public void AppendCoordinate_SkipsPointEqualToLast()
        {
            var line = new List<dynamic> { Position(-180.0, 11.708889) };

            LatLonHelpers.AppendCoordinate(line, Position(-180.0, 11.708889));

            Assert.Single(line);
        }

        [Fact]
        public void AppendCoordinate_AddsDistinctPoint()
        {
            var line = new List<dynamic> { Position(-180.0, 11.708889) };

            LatLonHelpers.AppendCoordinate(line, Position(-179.5, 11.7));

            Assert.Equal(2, line.Count);
        }

        [Fact]
        public void AppendCoordinate_AddsToEmptyList()
        {
            var line = new List<dynamic>();

            LatLonHelpers.AppendCoordinate(line, Position(-180.0, 11.708889));

            Assert.Single(line);
        }

        [Fact]
        public void AmCrossingAtExactAntimeridian_DoesNotEmitConsecutiveDuplicateVertices()
        {
            // Mirrors GetAwyData.AddLineFeatures: a running LineString whose last point is
            // exactly on the antimeridian, then the next segment "crosses" it.
            var line = new List<dynamic> { Position(-180.0, 11.708889) };
            var coords = LatLonHelpers.CheckAMCrossing(
                (double)line.Last()[1], (double)line.Last()[0], 11.5, 178.0);

            LatLonHelpers.AppendCoordinate(line, coords[1]);

            for (int i = 1; i < line.Count; i++)
            {
                bool sameAsPrev = (double)line[i][0] == (double)line[i - 1][0]
                               && (double)line[i][1] == (double)line[i - 1][1];
                Assert.False(sameAsPrev, $"consecutive duplicate vertex at index {i}");
            }
        }
    }
}
