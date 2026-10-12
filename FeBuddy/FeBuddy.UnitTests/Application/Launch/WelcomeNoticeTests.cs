using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Launch;

/// <summary>
/// Covers <see cref="WelcomeNotice"/>: the welcome to FE-Buddy 3 is due only when there were no
/// settings to read and it has not been shown on this PC, is handed to the GUI once, and is then
/// recorded as shown.
/// </summary>
[Collection("AppLog")]
public sealed class WelcomeNoticeTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Welcome_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public WelcomeNoticeTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Clears the pending welcome, restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		WelcomeNotice.ResetForTesting();
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

	/// <summary>With no settings yet, the welcome is handed over once and recorded as shown, on disk too.</summary>
	[Fact]
	public void no_settings_hands_the_welcome_over_once_and_records_it()
	{
		Assert.False(WelcomeNotice.HasBeenShown);
		Assert.True(WelcomeNotice.Check(UserConfigReadResult.Missing));
		Assert.True(WelcomeNotice.Take());
		Assert.False(WelcomeNotice.Take());
		Assert.True(WelcomeNotice.HasBeenShown);

		Assert.Equal(UserConfigReadResult.Read, UserConfigFile.ReadAll());
		Assert.Equal("Y", UserConfigFile.GetValue(UserConfigKeys.WelcomeShown));
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Info && e.Message.Contains("first launch", StringComparison.Ordinal));
	}

	/// <summary>Settings that were read, or that couldn't be, mean FE-Buddy 3 has been here before.</summary>
	[Theory]
	[InlineData(UserConfigReadResult.Read)]
	[InlineData(UserConfigReadResult.Unreadable)]
	public void settings_already_here_mean_no_welcome(UserConfigReadResult read)
	{
		Assert.False(WelcomeNotice.Check(read));
		Assert.False(WelcomeNotice.Take());
		Assert.False(WelcomeNotice.HasBeenShown);
	}

	/// <summary>Once shown, the welcome is never due again, even with no profile file.</summary>
	[Fact]
	public void once_shown_it_is_never_due_again()
	{
		UserConfigFile.TrySetValue(UserConfigKeys.WelcomeShown, "Y");

		Assert.False(WelcomeNotice.Check(UserConfigReadResult.Missing));
		Assert.False(WelcomeNotice.Take());
	}

	/// <summary>Before the launch sequence has looked, there is nothing to hand over.</summary>
	[Fact]
	public void nothing_is_handed_over_before_the_check()
	{
		Assert.False(WelcomeNotice.Take());
		Assert.False(WelcomeNotice.HasBeenShown);
	}

	/// <summary>When the settings can't be written, the welcome still shows now, and the failure is logged.</summary>
	[Fact]
	public void the_welcome_still_shows_when_it_cannot_be_recorded()
	{
		// A file where the settings folder should be: nothing can be saved under it.
		string blocked = Path.Combine(_root, "blocked");
		Directory.CreateDirectory(_root);
		File.WriteAllText(blocked, "not a folder");
		UserConfigFile.ConfigureForTesting(blocked);

		Assert.True(WelcomeNotice.Check(UserConfigReadResult.Missing));
		Assert.True(WelcomeNotice.Take());
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith("Could not record that the welcome was shown", StringComparison.Ordinal));
	}
}
