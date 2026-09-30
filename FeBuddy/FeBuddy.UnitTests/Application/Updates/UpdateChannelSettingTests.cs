using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="UpdateChannelSetting"/>: the channel follows the running build until the user
/// saves one, and a saved one wins.
/// </summary>
[Collection("AppLog")]
public sealed class UpdateChannelSettingTests : IDisposable
{
	private readonly string _root =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_UpdateChannel_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log and the settings file at a throwaway folder.</summary>
	public UpdateChannelSettingTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores defaults and cleans up.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary>A published pre-release defaults to its own channel; anything else to Stable.</summary>
	[Theory]
	[InlineData("3.0.0-alpha.2", ReleaseChannel.Alpha)]
	[InlineData("v3.0.0-beta.1", ReleaseChannel.Beta)]
	[InlineData("3.1.0-rc.1", ReleaseChannel.ReleaseCandidate)]
	[InlineData("3.0.0", ReleaseChannel.Stable)]
	[InlineData("3.1.0-dev", ReleaseChannel.Stable)]
	[InlineData("dev", ReleaseChannel.Stable)]
	[InlineData("", ReleaseChannel.Stable)]
	[InlineData(null, ReleaseChannel.Stable)]
	public void the_default_channel_is_the_running_builds(string? runningVersion, ReleaseChannel expected)
	{
		Assert.Equal(expected, UpdateChannelSetting.DefaultFor(runningVersion));
	}

	[Fact]
	public void with_no_channel_saved_the_running_builds_is_read()
	{
		Assert.Equal(ReleaseChannel.Alpha, UpdateChannelSetting.Read("3.0.0-alpha.2"));
		Assert.Equal(ReleaseChannel.Stable, UpdateChannelSetting.Read("3.0.0"));
	}

	[Fact]
	public void a_saved_channel_wins_over_the_running_builds()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Stable");

		Assert.Equal(ReleaseChannel.Stable, UpdateChannelSetting.Read("3.0.0-alpha.2"));
	}

	[Fact]
	public void an_unreadable_saved_channel_falls_back_to_the_running_builds()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Nightly");

		Assert.Equal(ReleaseChannel.Beta, UpdateChannelSetting.Read("3.0.0-beta.1"));
	}
}
