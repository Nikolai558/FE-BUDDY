using System;
using System.Globalization;
using FeBuddyLibrary.Helpers;
using Xunit;

namespace FeBuddyLibrary.Tests
{
    /// <summary>
    /// Regression guard for issue #166: on a machine whose Windows region uses ',' as the
    /// decimal separator, the culture-sensitive number parsing / formatting inside the
    /// coordinate conversion routines produced wrong coordinates and invalid GeoJSON (bare
    /// ',' in JSON number positions, degrees off by ~12) or threw FormatException.
    ///
    /// Program.Main pins the process culture to InvariantCulture as the primary fix; these
    /// tests assert LatLonHelpers is ALSO correct on its own under a hostile thread culture,
    /// so the conversion math can't silently regress if that process-level setting is ever
    /// moved, removed, or bypassed (e.g. a background thread, or the 3.0 rewrite).
    /// </summary>
    public class LatLonCultureInvarianceTests
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // 'de-DE' uses ',' for the decimal separator and '.' for digit grouping - the
        // combination that broke #166.
        private static void RunUnderCommaDecimalCulture(Action body)
        {
            var hostile = CultureInfo.GetCultureInfo("de-DE");
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = hostile;
                CultureInfo.CurrentUICulture = hostile;

                // Sanity check the environment actually is hostile, so a future framework
                // change that ignores the assignment doesn't turn this into a no-op test.
                Assert.Equal("12,5", 12.5.ToString(CultureInfo.CurrentCulture));

                body();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Fact]
        public void CreateDMS_UnderCommaDecimalCulture_StillEmitsDotFormattedResult()
        {
            RunUnderCommaDecimalCulture(() =>
            {
                Assert.Equal("N039.51.00.000", LatLonHelpers.CreateDMS(39.85, lat: true));
                Assert.Equal("W075.30.00.000", LatLonHelpers.CreateDMS(-75.5, lat: false));
                Assert.Equal("N045.15.00.000", LatLonHelpers.CreateDMS(45.25, lat: true));
            });
        }

        [Fact]
        public void CreateDecFormat_UnderCommaDecimalCulture_StillParsesAndEmitsInvariant()
        {
            RunUnderCommaDecimalCulture(() =>
            {
                var lat = LatLonHelpers.CreateDecFormat("N039.51.00.000", roundSixPlaces: false);
                var lon = LatLonHelpers.CreateDecFormat("W075.30.00.000", roundSixPlaces: false);

                // Output must be '.'-separated and parseable as an invariant number.
                Assert.DoesNotContain(",", lat);
                Assert.DoesNotContain(",", lon);
                Assert.Equal(39.85, double.Parse(lat, Inv), precision: 6);
                Assert.Equal(-75.5, double.Parse(lon, Inv), precision: 6);
            });
        }

        [Fact]
        public void CorrectLatLon_ConvertEast_UnderCommaDecimalCulture_DoesNotThrowAndConverts()
        {
            RunUnderCommaDecimalCulture(() =>
            {
                // Pre-#166 this threw FormatException inside CreateDMS under a comma-decimal culture.
                var result = LatLonHelpers.CorrectLatLon("E075.30.00.000", Lat: false, ConvertEast: true);

                Assert.StartsWith("W", result);
                Assert.DoesNotContain(",", result);
            });
        }
    }
}
