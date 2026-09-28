using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Launch;

/// <summary>
/// Covers <see cref="LegacyGitHubTokenNotice"/>: the notice about FE-Buddy 2.x's GitHub token
/// variable is due only while it has not been shown on this PC, is handed to the GUI once, and is
/// then recorded as shown.
/// </summary>
[Collection("AppLog")]
public sealed class LegacyGitHubTokenNoticeTests : IDisposable
{
	private static readonly EnvironmentVariableTarget[] ForUser = [EnvironmentVariableTarget.User];

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_LegacyTokenNotice_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public LegacyGitHubTokenNoticeTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real lookup, config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		LegacyGitHubTokenNotice.ConfigureForTesting(null);
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>When the variable is set, the notice is handed over once and recorded as shown, on disk too.</summary>
	[Fact]
	public void a_set_variable_is_handed_over_once_and_recorded()
	{
		LegacyGitHubTokenNotice.ConfigureForTesting(() => ForUser);

		Assert.False(LegacyGitHubTokenNotice.HasBeenShown);
		Assert.Equal(ForUser, LegacyGitHubTokenNotice.Check());
		Assert.Equal(ForUser, LegacyGitHubTokenNotice.Take());
		Assert.Empty(LegacyGitHubTokenNotice.Take());
		Assert.True(LegacyGitHubTokenNotice.HasBeenShown);

		UserConfigFile.ReadAll();
		Assert.Equal("Y", UserConfigFile.GetValue(UserConfigKeys.LegacyGitHubTokenNoticeShown));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("FEBUDDY_GITHUB_TOKEN environment variable is still set (User)", StringComparison.Ordinal));
	}

	/// <summary>Once shown, the notice is never due again - the variable is not even looked up.</summary>
	[Fact]
	public void once_shown_it_is_never_due_again()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.LegacyGitHubTokenNoticeShown, "Y");
		LegacyGitHubTokenNotice.ConfigureForTesting(() => throw new InvalidOperationException("looked up"));

		Assert.Empty(LegacyGitHubTokenNotice.Check());
		Assert.Empty(LegacyGitHubTokenNotice.Take());
	}

	/// <summary>With the variable not set, nothing is handed over and nothing is recorded - it is looked for again next launch.</summary>
	[Fact]
	public void no_variable_no_notice()
	{
		LegacyGitHubTokenNotice.ConfigureForTesting(() => []);

		Assert.Empty(LegacyGitHubTokenNotice.Check());
		Assert.Empty(LegacyGitHubTokenNotice.Take());
		Assert.False(LegacyGitHubTokenNotice.HasBeenShown);
	}

	/// <summary>Before the launch sequence has looked, there is nothing to hand over.</summary>
	[Fact]
	public void nothing_is_handed_over_before_the_check()
	{
		LegacyGitHubTokenNotice.ConfigureForTesting(() => ForUser);

		Assert.Empty(LegacyGitHubTokenNotice.Take());
		Assert.False(LegacyGitHubTokenNotice.HasBeenShown);
	}
}
