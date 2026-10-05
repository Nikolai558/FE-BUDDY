using FeBuddy.Core.Application.Updates;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Versioning.Models;

namespace FeBuddy.UnitTests.Application.Updates;

/// <summary>
/// Covers <see cref="UpdateChannelSetting"/>: the first launch saves the running build's channel,
/// and from then on no build changes it - an update never moves anyone to another channel.
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

	/// <summary>Only where nothing could be saved: a development build, or a launch whose write failed.</summary>
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

	[Theory]
	[InlineData("3.0.0-alpha.5", ReleaseChannel.Alpha, "Alpha")]
	[InlineData("3.0.0-beta.1", ReleaseChannel.Beta, "Beta")]
	[InlineData("3.0.0-rc.1", ReleaseChannel.ReleaseCandidate, "ReleaseCandidate")]
	[InlineData("3.0.0", ReleaseChannel.Stable, "Stable")]
	public void the_first_launch_saves_the_running_builds_channel(string runningVersion, ReleaseChannel expected, string saved)
	{
		Assert.Equal(expected, UpdateChannelSetting.SaveDefaultIfUnset(runningVersion, UserConfigReadResult.Missing));

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		Assert.Equal(saved, UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
	}

	/// <summary>The bug in issue #299: an alpha tester updated to a beta was moved to Beta.</summary>
	[Theory]
	[InlineData("3.0.0-beta.1")]
	[InlineData("3.0.0-rc.1")]
	[InlineData("3.0.0")]
	public void an_update_never_moves_a_saved_alpha(string laterVersion)
	{
		UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-alpha.5", UserConfigReadResult.Missing);

		Assert.Null(UpdateChannelSetting.SaveDefaultIfUnset(laterVersion, UserConfigReadResult.Read));

		Assert.Equal(ReleaseChannel.Alpha, UpdateChannelSetting.Read(laterVersion));
	}

	/// <summary>An alpha installed by hand doesn't lower a saved channel either; only Settings changes it.</summary>
	[Fact]
	public void installing_an_alpha_keeps_a_saved_beta()
	{
		UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-beta.1", UserConfigReadResult.Missing);

		Assert.Null(UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-alpha.6", UserConfigReadResult.Read));

		Assert.Equal(ReleaseChannel.Beta, UpdateChannelSetting.Read("3.0.0-alpha.6"));
	}

	/// <summary>A development build's Stable default isn't anyone's choice, so it isn't kept.</summary>
	[Theory]
	[InlineData("3.1.0-dev")]
	[InlineData("dev")]
	[InlineData("")]
	[InlineData(null)]
	public void a_development_build_saves_nothing(string? runningVersion)
	{
		Assert.Null(UpdateChannelSetting.SaveDefaultIfUnset(runningVersion, UserConfigReadResult.Missing));

		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));
		Assert.Null(UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
	}

	[Fact]
	public void an_unreadable_saved_channel_is_replaced_with_the_builds()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.UpdateChannel, "Nightly");

		Assert.Equal(ReleaseChannel.Beta, UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-beta.1", UserConfigReadResult.Read));

		Assert.Equal("Beta", UserConfigFile.GetValue(UserConfigKeys.UpdateChannel));
	}

	/// <summary>Saving into a file that couldn't be read would replace it, settings and all.</summary>
	[Fact]
	public void a_config_file_that_cannot_be_read_is_left_alone()
	{
		Directory.CreateDirectory(UserConfigFile.Directory);
		File.WriteAllText(UserConfigFile.ConfigFilePath, "{ not json");
		UserConfigReadResult read = UserConfigFile.ReadAll();

		Assert.Null(UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-alpha.5", read));

		Assert.Equal("{ not json", File.ReadAllText(UserConfigFile.ConfigFilePath));
	}

	[Fact]
	public void a_failed_write_is_logged_and_tried_again_next_launch()
	{
		// A folder where the undo file goes makes the save fail.
		Directory.CreateDirectory(UserConfigFile.PreviousFilePath);

		Assert.Null(UpdateChannelSetting.SaveDefaultIfUnset("3.0.0-alpha.5", UserConfigReadResult.Missing));

		Assert.False(File.Exists(UserConfigFile.ConfigFilePath));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning
			&& e.Message.StartsWith("Could not save the update channel (Alpha)", StringComparison.Ordinal));
	}
}
