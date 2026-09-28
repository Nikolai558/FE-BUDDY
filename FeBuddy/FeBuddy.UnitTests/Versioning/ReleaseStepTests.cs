using FeBuddy.Versioning;

namespace FeBuddy.UnitTests.Versioning;

/// <summary>
/// Covers <see cref="ReleaseStep"/>, the rule the release pre-flight enforces: every release is
/// exactly one step after the latest published one, and only release-shaped versions ship.
/// </summary>
public sealed class ReleaseStepTests
{
	[Theory]
	[InlineData("3.0.0")]
	[InlineData("3.0.0-alpha.1")]
	[InlineData("3.0.0-beta.12")]
	[InlineData("3.1.0-rc.2")]
	public void release_shaped_versions_are_accepted(string version)
	{
		Assert.True(ReleaseStep.IsReleaseShaped(ProductVersion.Parse(version)));
	}

	[Theory]
	[InlineData("3.0.0-dev")]
	[InlineData("3.0.0-alpha")]
	[InlineData("3.0.0-alpha.0")]
	[InlineData("3.0.0-alpha.beta")]
	[InlineData("3.0.0-alpha.1.1")]
	[InlineData("3.0.0-Alpha.1")]
	[InlineData("3.0.0-preview.1")]
	[InlineData("3.0.0+build.5")]
	[InlineData("3.0.0-rc.1+build.5")]
	public void other_versions_are_not_release_shaped(string version)
	{
		Assert.False(ReleaseStep.IsReleaseShaped(ProductVersion.Parse(version)));
	}

	[Theory]
	[InlineData("2.9.3", "3.0.0-alpha.1")]
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.2")]
	[InlineData("3.0.0-alpha.1", "3.0.0-beta.1")]
	[InlineData("3.0.0-alpha.1", "3.0.0-rc.1")]
	[InlineData("3.0.0-alpha.1", "3.0.0")]
	[InlineData("3.0.0-beta.9", "3.0.0-beta.10")]
	[InlineData("3.0.0-rc.2", "3.0.0")]
	[InlineData("3.0.0", "3.0.1")]
	[InlineData("3.0.0", "3.1.0")]
	[InlineData("3.0.0", "4.0.0")]
	[InlineData("3.0.9", "3.0.10")]
	[InlineData("3.0.0", "3.0.1-rc.1")]
	[InlineData("3.0.0", "3.1.0-beta.1")]
	[InlineData("3.0.0", "4.0.0-alpha.1")]
	public void one_step_forward_is_allowed(string previous, string next)
	{
		Assert.True(ReleaseStep.IsNextStep(ProductVersion.Parse(previous), ProductVersion.Parse(next)));
	}

	[Theory]
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.1")] // the same version again
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.3")] // a skipped number
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.4")]
	[InlineData("3.0.0-alpha.1", "3.0.0-beta.2")] // a new label must start at .1
	[InlineData("3.0.0-beta.1", "3.0.0-alpha.2")] // back to an earlier label
	[InlineData("3.0.0-alpha.1", "3.0.1")] // the target version changed mid-chain
	[InlineData("3.0.0-alpha.1", "3.1.0-alpha.1")]
	[InlineData("3.0.0", "3.0.2")] // a skipped patch
	[InlineData("3.0.0", "3.2.0")]
	[InlineData("3.0.0", "3.1.1")] // minor bump without resetting patch
	[InlineData("3.0.0", "5.0.0")]
	[InlineData("3.0.0", "3.0.1-alpha.2")]
	[InlineData("3.0.0", "2.9.4")] // backwards
	[InlineData("2.9.3", "3.0.0-dev")] // not release-shaped
	[InlineData("2.9.3", "3.0.0-alpha.1+build.5")]
	public void anything_else_is_rejected(string previous, string next)
	{
		Assert.False(ReleaseStep.IsNextStep(ProductVersion.Parse(previous), ProductVersion.Parse(next)));
	}

	[Fact]
	public void with_nothing_released_any_release_shaped_version_is_allowed()
	{
		Assert.True(ReleaseStep.IsNextStep(null, ProductVersion.Parse("0.1.0-alpha.3")));
		Assert.False(ReleaseStep.IsNextStep(null, ProductVersion.Parse("0.1.0-dev")));
	}

	[Fact]
	public void allowed_next_after_a_prerelease_lists_the_label_steps_and_the_release()
	{
		Assert.Equal(
			["3.0.0-alpha.2", "3.0.0-beta.1", "3.0.0-rc.1", "3.0.0"],
			ReleaseStep.AllowedNext(ProductVersion.Parse("3.0.0-alpha.1")).Select(v => v.ToString()));

		Assert.Equal(
			["3.0.0-rc.3", "3.0.0"],
			ReleaseStep.AllowedNext(ProductVersion.Parse("3.0.0-rc.2")).Select(v => v.ToString()));
	}

	[Fact]
	public void allowed_next_after_a_stable_release_lists_patch_minor_and_major()
	{
		Assert.Equal(
			[
				"2.9.4-alpha.1", "2.9.4-beta.1", "2.9.4-rc.1", "2.9.4",
				"2.10.0-alpha.1", "2.10.0-beta.1", "2.10.0-rc.1", "2.10.0",
				"3.0.0-alpha.1", "3.0.0-beta.1", "3.0.0-rc.1", "3.0.0",
			],
			ReleaseStep.AllowedNext(ProductVersion.Parse("2.9.3")).Select(v => v.ToString()));
	}

	[Fact]
	public void allowed_next_after_an_unusual_prerelease_keeps_only_higher_first_releases()
	{
		// An old or hand-made tag such as -dev: "rc" sorts above "dev"; "alpha" and "beta" do not.
		Assert.Equal(
			["3.0.0-rc.1", "3.0.0"],
			ReleaseStep.AllowedNext(ProductVersion.Parse("3.0.0-dev")).Select(v => v.ToString()));
	}

	[Fact]
	public void latest_picks_the_highest_parseable_tag()
	{
		ProductVersion? latest = ReleaseStep.Latest(["2.8.3", "v2.9.0", "2.9.1-alpha.1", "archive/v2-development", null, "2.9.3", "V2.2.0"]);

		Assert.Equal("2.9.3", latest?.ToString());
	}

	[Fact]
	public void latest_is_null_when_no_tag_parses()
	{
		Assert.Null(ReleaseStep.Latest(["not-a-version", ""]));
		Assert.Null(ReleaseStep.Latest([]));
	}

	[Fact]
	public void null_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => ReleaseStep.IsReleaseShaped(null!));
		Assert.Throws<ArgumentNullException>(() => ReleaseStep.AllowedNext(null!));
		Assert.Throws<ArgumentNullException>(() => ReleaseStep.IsNextStep(null, null!));
		Assert.Throws<ArgumentNullException>(() => ReleaseStep.Latest(null!));
	}
}
