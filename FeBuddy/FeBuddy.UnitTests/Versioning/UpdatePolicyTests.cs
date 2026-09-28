using FeBuddy.Versioning;

namespace FeBuddy.UnitTests.Versioning;

/// <summary>
/// Covers <see cref="UpdatePolicy"/>, the rule the installer enforces: forward (or same) is
/// always allowed, a downgrade only off a pre-release, and unparseable input fails open.
/// </summary>
public sealed class UpdatePolicyTests
{
	[Fact]
	public void nothing_installed_any_candidate_is_allowed()
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed(null, ProductVersion.Parse("3.0.0")));
		Assert.True(UpdatePolicy.IsTransitionAllowed(null, ProductVersion.Parse("0.0.1-alpha.1")));
	}

	[Fact]
	public void a_null_candidate_is_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => UpdatePolicy.IsTransitionAllowed(ProductVersion.Parse("3.0.0"), null!));
	}

	[Theory]
	[InlineData("2.8.3", "2.8.4")]
	[InlineData("2.8.3", "2.8.3")]
	[InlineData("2.9.0", "3.0.0-alpha.1")]
	[InlineData("2.9.0", "3.0.0")]
	[InlineData("3.0.0-alpha.1", "3.0.0-alpha.2")]
	[InlineData("3.0.0-rc.1", "3.0.0")]
	public void equal_or_forward_precedence_is_allowed(string installed, string candidate)
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed(ProductVersion.Parse(installed), ProductVersion.Parse(candidate)));
	}

	[Theory]
	[InlineData("2.8.4", "2.8.3")]
	[InlineData("3.0.0", "2.9.0")]
	[InlineData("3.0.0", "3.0.0-rc.1")]
	public void downgrade_from_stable_is_blocked(string installed, string candidate)
	{
		Assert.False(UpdatePolicy.IsTransitionAllowed(ProductVersion.Parse(installed), ProductVersion.Parse(candidate)));
	}

	[Theory]
	[InlineData("3.0.0-alpha.1", "2.9.0")]
	[InlineData("3.0.0-beta.2", "3.0.0-alpha.1")]
	[InlineData("3.0.0-rc.1", "2.9.0")]
	public void downgrade_off_a_prerelease_is_allowed(string installed, string candidate)
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed(ProductVersion.Parse(installed), ProductVersion.Parse(candidate)));
	}

	[Fact]
	public void string_overload_applies_the_same_rule()
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed("2.9.0", "3.0.0"));
		Assert.False(UpdatePolicy.IsTransitionAllowed("3.0.0", "2.9.0"));
		Assert.True(UpdatePolicy.IsTransitionAllowed("3.0.0-alpha.1", "2.9.0"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void string_overload_no_installed_version_is_a_fresh_install(string? installed)
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed(installed, "3.0.0"));
	}

	[Theory]
	[InlineData("not-a-version", "3.0.0")]
	[InlineData("3.0.0", "also-not-a-version")]
	[InlineData("3.0.0", null)]
	public void string_overload_unparseable_input_fails_open(string? installed, string? candidate)
	{
		Assert.True(UpdatePolicy.IsTransitionAllowed(installed, candidate));
	}
}
