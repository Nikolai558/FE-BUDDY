using System;
using FeBuddy.Versioning;
using Xunit;

namespace FeBuddy.Versioning.Tests
{
    public class ProductVersionTests
    {
        [Theory]
        [InlineData("2.8.3")]
        [InlineData("2.8.4-alpha.1")]
        [InlineData("2.8.4-beta.2")]
        [InlineData("2.8.4-rc.1")]
        [InlineData("0.0.1")]
        [InlineData("10.20.30-alpha.1+build.5")]
        public void Parse_ValidStrictSemVer_RoundTrips(string text)
        {
            var version = ProductVersion.Parse(text);

            Assert.Equal(text, version.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("v2.8.3")]
        [InlineData("2.8")]
        [InlineData("2.8.3.0")]
        [InlineData("2.8.3-")]
        [InlineData("garbage")]
        public void Parse_InvalidInput_ThrowsFormatException(string? text)
        {
            Assert.Throws<FormatException>(() => ProductVersion.Parse(text!));
        }

        [Theory]
        [InlineData("2.8.3")]
        [InlineData("2.8.4-alpha.1")]
        public void TryParse_ValidInput_ReturnsTrueAndNonNull(string text)
        {
            var ok = ProductVersion.TryParse(text, out var version);

            Assert.True(ok);
            Assert.NotNull(version);
            Assert.Equal(text, version!.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-version")]
        [InlineData("2.8")]
        public void TryParse_InvalidInput_ReturnsFalseAndNull(string? text)
        {
            var ok = ProductVersion.TryParse(text, out var version);

            Assert.False(ok);
            Assert.Null(version);
        }

        [Theory]
        [InlineData("2.8.4", false)]
        [InlineData("2.8.4-alpha.1", true)]
        [InlineData("2.8.4-rc.1", true)]
        public void IsPrerelease_ReflectsPrereleaseTag(string text, bool expected)
        {
            Assert.Equal(expected, ProductVersion.Parse(text).IsPrerelease);
        }

        [Theory]
        [InlineData("2.8.4", ReleaseChannel.Stable)]
        [InlineData("2.8.4-alpha.1", ReleaseChannel.Alpha)]
        [InlineData("2.8.4-beta.2", ReleaseChannel.Beta)]
        [InlineData("2.8.4-rc.1", ReleaseChannel.ReleaseCandidate)]
        [InlineData("2.8.4-alpha", ReleaseChannel.Alpha)]
        [InlineData("2.8.4-beta", ReleaseChannel.Beta)]
        [InlineData("2.8.4-rc", ReleaseChannel.ReleaseCandidate)]
        public void Channel_MapsPrereleaseTagToChannel(string text, ReleaseChannel expected)
        {
            Assert.Equal(expected, ProductVersion.Parse(text).Channel);
        }

        [Theory]
        [InlineData("2.8.4-RC.1")]
        [InlineData("2.8.4-Beta.2")]
        [InlineData("2.8.4-ALPHA.1")]
        public void Channel_TagMatchIsCaseInsensitive(string text)
        {
            var channel = ProductVersion.Parse(text).Channel;

            Assert.Contains(channel, new[]
            {
                ReleaseChannel.Alpha,
                ReleaseChannel.Beta,
                ReleaseChannel.ReleaseCandidate,
            });
            Assert.NotEqual(ReleaseChannel.Stable, channel);
        }

        [Theory]
        [InlineData("2.8.4-preview.1")]
        [InlineData("2.8.4-nightly")]
        [InlineData("2.8.4-0")]
        public void Channel_UnrecognizedPrereleaseTag_IsTreatedAsAlpha(string text)
        {
            Assert.Equal(ReleaseChannel.Alpha, ProductVersion.Parse(text).Channel);
        }

        [Theory]
        [InlineData("2.8.3", "2.8.4")]
        [InlineData("2.8.4-alpha.1", "2.8.4")]
        [InlineData("2.8.4-alpha.1", "2.8.4-beta.1")]
        [InlineData("2.8.4-beta.1", "2.8.4-rc.1")]
        [InlineData("2.8.4-alpha.1", "2.8.4-alpha.2")]
        [InlineData("1.9.9", "2.0.0")]
        public void ComparePrecedenceTo_OrdersVersionsBySemVerPrecedence(string lower, string higher)
        {
            var a = ProductVersion.Parse(lower);
            var b = ProductVersion.Parse(higher);

            Assert.True(a.ComparePrecedenceTo(b) < 0);
            Assert.True(b.ComparePrecedenceTo(a) > 0);
        }

        [Fact]
        public void ComparePrecedenceTo_EqualVersions_ReturnsZero()
        {
            var a = ProductVersion.Parse("2.8.3");
            var b = ProductVersion.Parse("2.8.3");

            Assert.Equal(0, a.ComparePrecedenceTo(b));
        }

        [Fact]
        public void ComparePrecedenceTo_IgnoresBuildMetadata()
        {
            var a = ProductVersion.Parse("2.8.3+build.1");
            var b = ProductVersion.Parse("2.8.3+build.2");

            Assert.Equal(0, a.ComparePrecedenceTo(b));
        }
    }
}
