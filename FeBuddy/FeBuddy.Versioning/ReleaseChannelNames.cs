using FeBuddy.Versioning.Models;

namespace FeBuddy.Versioning;

/// <summary>How a <see cref="ReleaseChannel"/> is named to people, as opposed to how it is stored.</summary>
public static class ReleaseChannelNames
{
	/// <summary>The channel's name for people, e.g. <c>Release Candidate</c> for <see cref="ReleaseChannel.ReleaseCandidate"/>.</summary>
	/// <param name="channel">The channel.</param>
	/// <returns>The name to show.</returns>
	public static string DisplayName(this ReleaseChannel channel) => channel switch
	{
		ReleaseChannel.ReleaseCandidate => "Release Candidate",
		_ => channel.ToString(),
	};
}
