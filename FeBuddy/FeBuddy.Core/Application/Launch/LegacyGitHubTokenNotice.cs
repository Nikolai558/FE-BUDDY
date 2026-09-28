using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Core.Application.Launch;

/// <summary>
/// The one-time notice that FE-Buddy 2.x's <c>FEBUDDY_GITHUB_TOKEN</c> environment variable
/// (<see cref="LegacyGitHubTokenVariable"/>) is still set on this PC, holding a GitHub token in plain
/// text that FE-Buddy 3 never uses, so the user can delete it.
/// </summary>
/// <remarks>
/// <para>
/// The launch sequence calls <see cref="Check"/> once saved settings are read. The GUI calls
/// <see cref="Take"/> when the shell opens and whenever <see cref="AppEnvironment.Changed"/> fires,
/// and shows the notice the one time it returns anything.
/// </para>
/// <para>
/// Once shown, <see cref="UserConfigKeys.LegacyGitHubTokenNoticeShown"/> is saved, so the notice
/// never shows again on this PC - even if the variable is left in place. That setting stays on this
/// PC: a settings export leaves it out, and an import keeps this PC's.
/// </para>
/// </remarks>
public static class LegacyGitHubTokenNotice
{
	private const string LogSource = "Launch";

	private static IReadOnlyList<EnvironmentVariableTarget> _pending = [];
	private static Func<IReadOnlyList<EnvironmentVariableTarget>>? _findForTesting;

	/// <summary>Whether the notice has already been shown on this PC.</summary>
	public static bool HasBeenShown => UserConfigFile.GetValue(UserConfigKeys.LegacyGitHubTokenNoticeShown) == "Y";

	/// <summary>
	/// Works out whether the notice is due: the variable is set, and the notice has not been shown on
	/// this PC. Looks up the variable only when it has not, and only by its name - never its value.
	/// </summary>
	/// <returns>Where the variable is set, when the notice is due; otherwise empty.</returns>
	internal static IReadOnlyList<EnvironmentVariableTarget> Check()
	{
		IReadOnlyList<EnvironmentVariableTarget> due = HasBeenShown
			? []
			: (_findForTesting ?? LegacyGitHubTokenVariable.FindTargets)();

		if (due.Count > 0)
		{
			AppLog.Warning(LogSource,
				$"FE-Buddy 2.x's {LegacyGitHubTokenVariable.Name} environment variable is still set ({string.Join(" and ", due)}); FE-Buddy will say so once.");
		}

		Interlocked.Exchange(ref _pending, due);
		return due;
	}

	/// <summary>
	/// Hands the notice to the GUI, once: the first call after <see cref="Check"/> found the variable
	/// returns where it is set and records the notice as shown; every other call returns nothing.
	/// </summary>
	/// <returns>Where the variable is set, when the notice should be shown now; otherwise empty.</returns>
	public static IReadOnlyList<EnvironmentVariableTarget> Take()
	{
		IReadOnlyList<EnvironmentVariableTarget> due = Interlocked.Exchange(ref _pending, []);

		if (due.Count > 0)
		{
			UserConfigFile.TrySetValue(UserConfigKeys.LegacyGitHubTokenNoticeShown, "Y");

			try
			{
				UserConfigFile.Save(UserConfigKeys.LegacyGitHubTokenNoticeShown);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// The notice still shows now; it may just show once more next launch.
				AppLog.Warning(LogSource, $"Could not record that the {LegacyGitHubTokenVariable.Name} notice was shown: {ex.Message}");
			}
		}

		return due;
	}

	/// <summary>
	/// Replaces the variable lookup, or restores the real one with <see langword="null"/>, and clears
	/// any pending notice. Unit tests only.
	/// </summary>
	/// <param name="find">Stands in for <see cref="LegacyGitHubTokenVariable.FindTargets()"/>.</param>
	internal static void ConfigureForTesting(Func<IReadOnlyList<EnvironmentVariableTarget>>? find)
	{
		_findForTesting = find;
		Interlocked.Exchange(ref _pending, []);
	}
}
