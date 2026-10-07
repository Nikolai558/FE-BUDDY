using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Brings settings saved in an older <c>UserConfig</c> layout up to this version's
/// (<see cref="UserConfigVersion.Current"/>), one step per layout: at launch, for
/// <c>UserConfig.json</c> (<see cref="UserConfigFile.ReadAll"/>), and for a settings file being
/// imported (<see cref="UserConfigTransfer.Read(string)"/>).
/// </summary>
/// <remarks>
/// <para>
/// Changing the layout: bump <see cref="UserConfigVersion.Current"/>, add the step that turns the old
/// layout into the new one to <see cref="All"/>, and add a sample file of the new layout to the unit
/// tests (<c>Fixtures\UserConfig</c>). The tests load every sample, bring it forward and save it
/// through every settings page; a setting a page no longer reads shows up there.
/// </para>
/// <para>
/// A change that moves the settings file itself (a new folder, or one file split into two) is also a
/// new layout: <see cref="UserConfigFile"/> then looks for a file in an older layout where that
/// layout kept it, and the step moves the settings between the parts of the new one.
/// </para>
/// </remarks>
public static class UserConfigMigrations
{
	/// <summary>
	/// Every step, oldest first: the step with <see cref="UserConfigMigration.ToVersion"/> N turns a
	/// layout N - 1 file into layout N. There is one for each layout after <see cref="UserConfigVersion.Oldest"/>.
	/// </summary>
	public static IReadOnlyList<UserConfigMigration> All { get; } = [];

	private static int _currentVersion = UserConfigVersion.Current;
	private static IReadOnlyList<UserConfigMigration> _steps = All;

	/// <summary>
	/// The layout settings are brought to and saved in: <see cref="UserConfigVersion.Current"/>, except
	/// while a unit test stands in a layout of its own (<see cref="ConfigureForTesting"/>).
	/// </summary>
	internal static int CurrentVersion => _currentVersion;

	/// <summary>Brings settings up to the current layout (<see cref="UserConfigVersion.Current"/>).</summary>
	/// <param name="values">The settings, by dotted path. Not changed.</param>
	/// <param name="fromVersion">The layout they are in.</param>
	/// <returns>The settings in the current layout, and what changed.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="fromVersion"/> is older than <see cref="UserConfigVersion.Oldest"/> or newer than <see cref="UserConfigVersion.Current"/>.</exception>
	/// <exception cref="InvalidOperationException">A step is missing, or one failed.</exception>
	public static UserConfigMigrationResult Migrate(IReadOnlyDictionary<string, string> values, int fromVersion) =>
		Migrate(values, fromVersion, _currentVersion, _steps);

	/// <summary>
	/// Stands in a current layout and its steps, so the bringing-forward of <c>UserConfig.json</c> and
	/// of imports can be tested before FE-Buddy has a second layout. Unit tests only.
	/// </summary>
	/// <param name="currentVersion">The layout to treat as current, or <see langword="null"/> to restore <see cref="UserConfigVersion.Current"/>.</param>
	/// <param name="steps">The steps to use, or <see langword="null"/> to restore <see cref="All"/>.</param>
	internal static void ConfigureForTesting(int? currentVersion, IReadOnlyList<UserConfigMigration>? steps)
	{
		_currentVersion = currentVersion ?? UserConfigVersion.Current;
		_steps = steps ?? All;
	}

	/// <summary>Brings settings up to <paramref name="toVersion"/> with the steps given. The public overload uses this version's.</summary>
	/// <param name="values">The settings, by dotted path. Not changed.</param>
	/// <param name="fromVersion">The layout they are in.</param>
	/// <param name="toVersion">The layout to bring them to.</param>
	/// <param name="steps">The steps to choose from.</param>
	/// <returns>The settings in <paramref name="toVersion"/>, and what changed.</returns>
	internal static UserConfigMigrationResult Migrate(
		IReadOnlyDictionary<string, string> values,
		int fromVersion,
		int toVersion,
		IReadOnlyList<UserConfigMigration> steps)
	{
		ArgumentNullException.ThrowIfNull(values);
		ArgumentNullException.ThrowIfNull(steps);
		ArgumentOutOfRangeException.ThrowIfLessThan(fromVersion, UserConfigVersion.Oldest);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(fromVersion, toVersion);

		Dictionary<string, string> migrated = new(values, StringComparer.Ordinal);
		UserConfigMigrationContext context = new(migrated);
		List<string> applied = [];

		for (int version = fromVersion + 1; version <= toVersion; version++)
		{
			UserConfigMigration step = steps.SingleOrDefault(s => s.ToVersion == version)
				?? throw new InvalidOperationException($"No step brings settings to layout {version}.");

			try
			{
				step.Apply(context);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Bringing settings to layout {version} failed ({step.Description}): {ex.Message}", ex);
			}

			applied.Add(step.Description);
		}

		return new UserConfigMigrationResult(migrated, fromVersion, toVersion, applied);
	}
}
