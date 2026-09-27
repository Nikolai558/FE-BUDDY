using FeBuddy.Versioning;
using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Versioning;

/// <summary>Exercises <see cref="ReleaseChannelNames"/>: every channel has a name people can read.</summary>
public sealed class ReleaseChannelNamesTests
{
	/// <summary>Release Candidate is spelled out; the others read as they are stored.</summary>
	[Theory]
	[InlineData(ReleaseChannel.Stable, "Stable")]
	[InlineData(ReleaseChannel.ReleaseCandidate, "Release Candidate")]
	[InlineData(ReleaseChannel.Beta, "Beta")]
	[InlineData(ReleaseChannel.Alpha, "Alpha")]
	public void display_name_is_readable(ReleaseChannel channel, string expected) =>
		Assert.Equal(expected, channel.DisplayName());
}
