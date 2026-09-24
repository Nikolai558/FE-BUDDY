using FeBuddy.Versioning;

namespace FeBuddy.UnitTests.Versioning;

/// <summary>
/// Covers <see cref="ProductVersion"/>: strict SemVer parsing, tag parsing, the channel a
/// pre-release tag implies, and SemVer precedence.
/// </summary>
public sealed class ProductVersionTests
{
	[Theory]
	[InlineData("2.8.3")]
	[InlineData("3.0.0-alpha.1")]
	[InlineData("3.0.0-beta.2")]
	[InlineData("3.0.0-rc.1")]
	[InlineData("3.0.0-dev")]
	[InlineData("0.0.1")]
	[InlineData("10.20.30-alpha.1+build.5")]
	public void Parse_ValidStrictSemVer_RoundTrips(string text)
	{
		Assert.Equal(text, ProductVersion.Parse(text).ToString());
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
	[InlineData("3.0.0-alpha.1")]
	public void TryParse_ValidInput_ReturnsTheVersion(string text)
	{
		Assert.True(ProductVersion.TryParse(text, out ProductVersion? version));
		Assert.Equal(text, version!.ToString());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("not-a-version")]
	[InlineData("2.8")]
	[InlineData("2.8.1.0")]
	public void TryParse_InvalidInput_ReturnsFalseAndNull(string? text)
	{
		Assert.False(ProductVersion.TryParse(text, out ProductVersion? version));
		Assert.Null(version);
	}

	[Theory]
	[InlineData("3.0.0", "3.0.0")]
	[InlineData("v3.0.0-rc.1", "3.0.0-rc.1")]
	[InlineData("V2.2.0", "2.2.0")]
	[InlineData("  v2.9.0 ", "2.9.0")]
	public void TryParseTag_AllowsALeadingV(string tag, string expected)
	{
		Assert.True(ProductVersion.TryParseTag(tag, out ProductVersion? version));
		Assert.Equal(expected, version!.ToString());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("v")]
	[InlineData("vnext")]
	[InlineData("nightly")]
	[InlineData("v2.9.0.0")]
	public void TryParseTag_RejectsNonVersions(string? tag)
	{
		Assert.False(ProductVersion.TryParseTag(tag, out ProductVersion? version));
		Assert.Null(version);
	}

	[Theory]
	[InlineData("3.0.0", false)]
	[InlineData("3.0.0-alpha.1", true)]
	[InlineData("3.0.0-rc.1", true)]
	public void IsPrerelease_ReflectsThePrereleaseTag(string text, bool expected)
	{
		Assert.Equal(expected, ProductVersion.Parse(text).IsPrerelease);
	}

	[Theory]
	[InlineData("3.0.0", ReleaseChannel.Stable)]
	[InlineData("3.0.0+build.7", ReleaseChannel.Stable)]
	[InlineData("3.0.0-alpha.1", ReleaseChannel.Alpha)]
	[InlineData("3.0.0-beta.2", ReleaseChannel.Beta)]
	[InlineData("3.0.0-rc.1", ReleaseChannel.ReleaseCandidate)]
	[InlineData("3.0.0-alpha", ReleaseChannel.Alpha)]
	[InlineData("3.0.0-beta", ReleaseChannel.Beta)]
	[InlineData("3.0.0-rc", ReleaseChannel.ReleaseCandidate)]
	[InlineData("3.0.0-RC.1", ReleaseChannel.ReleaseCandidate)]
	[InlineData("3.0.0-Beta.2", ReleaseChannel.Beta)]
	public void Channel_MapsThePrereleaseTag(string text, ReleaseChannel expected)
	{
		Assert.Equal(expected, ProductVersion.Parse(text).Channel);
	}

	[Theory]
	[InlineData("3.0.0-preview.1")]
	[InlineData("3.0.0-nightly")]
	[InlineData("3.0.0-dev")]
	[InlineData("3.0.0-0")]
	public void Channel_UnrecognisedPrereleaseTag_IsAlpha(string text)
	{
		Assert.Equal(ReleaseChannel.Alpha, ProductVersion.Parse(text).Channel);
	}

	[Theory]
	[InlineData("2.8.3", "2.8.4")]
	[InlineData("2.8.10", "2.8.11")]
	[InlineData("2.8.9", "2.8.10")]
	[InlineData("3.0.0-alpha.1", "3.0.0")]
	[InlineData("3.0.0-alpha.1", "3.0.0-beta.1")]
	[InlineData("3.0.0-beta.1", "3.0.0-rc.1")]
	[InlineData("3.0.0-rc.1", "3.0.0")]
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.2")]
	[InlineData("2.9.0", "3.0.0-alpha.1")]
	[InlineData("1.9.9", "2.0.0")]
	public void ComparePrecedenceTo_OrdersBySemVerPrecedence(string lower, string higher)
	{
		ProductVersion a = ProductVersion.Parse(lower);
		ProductVersion b = ProductVersion.Parse(higher);

		Assert.True(a.ComparePrecedenceTo(b) < 0);
		Assert.True(b.ComparePrecedenceTo(a) > 0);
	}

	[Theory]
	[InlineData("2.8.3", "2.8.3")]
	[InlineData("2.8.3+build.1", "2.8.3+build.2")]
	public void ComparePrecedenceTo_EqualPrecedence_IsZero(string left, string right)
	{
		Assert.Equal(0, ProductVersion.Parse(left).ComparePrecedenceTo(ProductVersion.Parse(right)));
	}

	[Fact]
	public void ComparePrecedenceTo_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => ProductVersion.Parse("3.0.0").ComparePrecedenceTo(null!));
	}

	[Fact]
	public void SemVersion_IsTheParsedValue()
	{
		Assert.Equal(3, ProductVersion.Parse("3.1.4-rc.2").SemVersion.Major);
	}
}
