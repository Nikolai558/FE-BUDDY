using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Configuration;

using FeBuddy.Versioning;
using FeBuddy.Versioning.Models;

namespace FeBuddy.Core.Application.Updates;

/// <summary>
/// The update channel FE-Buddy checks: the one the user saved in Settings (<c>General.UpdateChannel</c>),
/// or - until they choose one - the channel of the build they are running, so an alpha keeps being
/// offered alphas, a beta betas, a release candidate release candidates and a stable release stable
/// releases.
/// </summary>
/// <remarks>
/// Settings writes the channel only when the user changes it, so the default keeps following the
/// build: a user who never chose one and moves from an alpha to a stable release is then on Stable.
/// </remarks>
public static class UpdateChannelSetting
{
	/// <summary>
	/// The channel for someone who has not chosen one: the running build's, when it is a published
	/// pre-release (<c>alpha</c>, <c>beta</c> or <c>rc</c>), otherwise <see cref="ReleaseChannel.Stable"/> -
	/// a stable release, a development build or a version that cannot be read.
	/// </summary>
	/// <param name="runningVersion">The running build's version, e.g. <c>3.0.0-alpha.2</c>.</param>
	/// <returns>The default channel.</returns>
	public static ReleaseChannel DefaultFor(string? runningVersion) =>
		ProductVersion.TryParseTag(runningVersion, out ProductVersion? parsed) && VersionCheck.IsPublishedPreRelease(parsed!)
			? parsed!.Channel
			: ReleaseChannel.Stable;

	/// <summary>The saved channel, or <see cref="DefaultFor"/> <paramref name="runningVersion"/> when none is saved.</summary>
	/// <param name="runningVersion">The running build's version.</param>
	/// <returns>The channel to check.</returns>
	public static ReleaseChannel Read(string? runningVersion) =>
		VersionCheckResult.ParseChannel(UserConfigFile.GetValue(UserConfigKeys.UpdateChannel), DefaultFor(runningVersion));
}
