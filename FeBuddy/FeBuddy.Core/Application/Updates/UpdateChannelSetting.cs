using FeBuddy.Core.Application.Updates.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;

using FeBuddy.Versioning;
using FeBuddy.Versioning.Models;

namespace FeBuddy.Core.Application.Updates;

/// <summary>
/// The update channel FE-Buddy checks: the one saved in <c>UserConfig.json</c> (<c>General.UpdateChannel</c>).
/// The first launch saves the running build's channel (<see cref="SaveDefaultIfUnset"/>), so an alpha
/// starts on Alpha, a beta on Beta, a release candidate on Release Candidate and a stable release on
/// Stable. From then on only the user changes it, in Settings.
/// </summary>
/// <remarks>
/// An update never changes the channel. A channel is the least stable kind of release the user
/// accepts, so a tester on Alpha who updates to a beta, a release candidate or 3.0.0 stays on Alpha
/// and keeps getting alphas; someone on Beta who installs an alpha by hand stays on Beta.
/// </remarks>
public static class UpdateChannelSetting
{
	private const string LogSource = "UpdateChannel";

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

	/// <summary>
	/// The saved channel, or <see cref="DefaultFor"/> <paramref name="runningVersion"/> when none is
	/// saved - a development build, or a launch that couldn't save one.
	/// </summary>
	/// <param name="runningVersion">The running build's version.</param>
	/// <returns>The channel to check.</returns>
	public static ReleaseChannel Read(string? runningVersion) =>
		VersionCheckResult.ParseChannel(UserConfigFile.GetValue(UserConfigKeys.UpdateChannel), DefaultFor(runningVersion));

	/// <summary>
	/// Saves the running build's channel (<see cref="DefaultFor"/>) when no channel is saved, or the
	/// saved one can't be read. Called at launch, right after the settings are read, so the channel
	/// is kept from the first launch on and an update can't change it.
	/// </summary>
	/// <remarks>
	/// Nothing is saved when the profile in use couldn't be read, since the write would replace
	/// the whole file. Nor for a development build (<c>3.1.0-dev</c>) or a version that can't be read:
	/// their Stable default isn't anyone's choice, so it would wrongly hold a developer's PC on
	/// Stable. Never throws; a failed write is logged and tried again at the next launch.
	/// </remarks>
	/// <param name="runningVersion">The running build's version.</param>
	/// <param name="configRead">What the launch's <see cref="UserConfigFile.ReadAll"/> found.</param>
	/// <returns>The channel saved, or <see langword="null"/> when nothing was saved.</returns>
	public static ReleaseChannel? SaveDefaultIfUnset(string? runningVersion, UserConfigReadResult configRead)
	{
		if (configRead == UserConfigReadResult.Unreadable
			|| VersionCheckResult.TryParseChannel(UserConfigFile.GetValue(UserConfigKeys.UpdateChannel), out _)
			|| !ProductVersion.TryParseTag(runningVersion, out ProductVersion? parsed)
			|| (parsed!.IsPrerelease && !VersionCheck.IsPublishedPreRelease(parsed)))
		{
			return null;
		}

		ReleaseChannel channel = DefaultFor(runningVersion);

		try
		{
			UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, channel.ToString());
			UserConfigFile.Save(UserConfigKeys.UpdateChannel);
			AppLog.Info(LogSource, $"No update channel was saved, so saved v{parsed}'s: {channel.DisplayName()}.");
			return channel;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not save the update channel ({channel.DisplayName()}): {ex.Message}. Trying again at the next launch.");
			return null;
		}
	}
}
