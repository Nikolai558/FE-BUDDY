using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Application.Launch;

/// <summary>
/// The one-time welcome to FE-Buddy 3 (#351), shown the first time FE-Buddy 3 opens on this PC: the
/// launch that finds no FE-Buddy 3 settings yet.
/// </summary>
/// <remarks>
/// <para>
/// The launch sequence calls <see cref="Check"/> as soon as it has read saved settings, before
/// anything is saved: the first launch saves the update channel, which writes the settings file. The
/// GUI calls <see cref="Take"/> when the shell opens and whenever <see cref="AppEnvironment.Changed"/>
/// fires, and shows the welcome the one time it returns <see langword="true"/>.
/// </para>
/// <para>
/// Once shown, <see cref="UserConfigKeys.WelcomeShown"/> is saved, so the welcome never shows again on
/// this PC - even from a build that saves nothing else at launch. That setting stays on this PC: a
/// settings export leaves it out. A reset that deletes FE-Buddy's settings brings the welcome back.
/// </para>
/// </remarks>
public static class WelcomeNotice
{
	private const string LogSource = "Launch";

	private static int _pending;

	/// <summary>Whether the welcome has already been shown on this PC.</summary>
	public static bool HasBeenShown => UserConfigFile.GetValue(UserConfigKeys.WelcomeShown) == "Y";

	/// <summary>
	/// Works out whether the welcome is due: there were no settings to read, and the welcome has not
	/// been shown on this PC.
	/// </summary>
	/// <param name="read">How reading the saved settings went this launch.</param>
	/// <returns><see langword="true"/> when the welcome is due.</returns>
	internal static bool Check(UserConfigReadResult read)
	{
		bool due = read == UserConfigReadResult.Missing && !HasBeenShown;

		if (due)
		{
			AppLog.Info(LogSource, "No FE-Buddy 3 settings yet, so this is the first launch; FE-Buddy will show its welcome.");
		}

		Interlocked.Exchange(ref _pending, due ? 1 : 0);
		return due;
	}

	/// <summary>
	/// Hands the welcome to the GUI, once: the first call after <see cref="Check"/> found it due
	/// returns <see langword="true"/> and records it as shown; every other call returns
	/// <see langword="false"/>.
	/// </summary>
	/// <returns><see langword="true"/> when the welcome should be shown now.</returns>
	public static bool Take()
	{
		bool due = Interlocked.Exchange(ref _pending, 0) == 1;

		if (due)
		{
			UserConfigFile.TrySetValue(UserConfigKeys.WelcomeShown, "Y");

			try
			{
				UserConfigFile.Save(UserConfigKeys.WelcomeShown);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// The welcome still shows now; it may just show once more next launch.
				AppLog.Warning(LogSource, $"Could not record that the welcome was shown: {ex.Message}");
			}
		}

		return due;
	}

	/// <summary>Clears any pending welcome. Unit tests only.</summary>
	internal static void ResetForTesting() => Interlocked.Exchange(ref _pending, 0);
}
