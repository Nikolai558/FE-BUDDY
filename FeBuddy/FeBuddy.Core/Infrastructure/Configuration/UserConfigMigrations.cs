using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Brings settings saved in an older <c>UserConfig</c> layout up to this version's
/// (<see cref="UserConfigVersion.Current"/>), one step per layout: at launch, for the active
/// profile's <c>UserConfig.&lt;Profile&gt;.json</c> and for <c>Shared.json</c>
/// (<see cref="UserConfigFile.ReadAll"/>), and for a settings file being imported
/// (<see cref="UserConfigTransfer.Read(string)"/>).
/// </summary>
/// <remarks>
/// <para>
/// Changing the layout: bump <see cref="UserConfigVersion.Current"/>, add the step that turns the old
/// layout into the new one to <see cref="All"/>, and add a sample file of the new layout to the unit
/// tests (<c>Fixtures\UserConfig</c>). The tests load every sample, bring it forward and save it
/// through every settings page; a setting a page no longer reads shows up there.
/// </para>
/// <para>
/// The settings every profile shares (<see cref="UserConfigPortability.SharedByProfiles"/>) go through
/// the same steps: <c>Shared.json</c> is brought forward by its own stamp, so a step that renames a shared
/// setting renames it there too. At launch, <c>Shared.json</c> and every profile are brought forward
/// together (<see cref="MigrateTogether(UserConfigMigratingFile?, IReadOnlyList{UserConfigMigratingFile})"/>),
/// so a step can also move a setting between a profile and <c>Shared.json</c>.
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
	/// layout N - 1 file into layout N. There is one for each layout after <see cref="UserConfigVersion.Oldest"/>,
	/// so none while that is <see cref="UserConfigVersion.Current"/>.
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

		UserConfigMigratingFile file = new("settings", values, fromVersion);
		MigrateTogether(shared: null, [file], toVersion, steps);

		return file.Error is { } error
			? throw error
			: new UserConfigMigrationResult(file.Values, toVersion, file.Applied);
	}

	/// <summary>
	/// Brings <c>Shared.json</c> and every profile up to the current layout together, one layout at a
	/// time: each step runs on every profile still in the layout before it - the active profile first -
	/// then on <c>Shared.json</c>, then its <see cref="UserConfigMigration.ThenShared"/> part. So a step can
	/// move a setting between a profile and <c>Shared.json</c> (<see cref="UserConfigMigrationContext.MoveToShared"/>,
	/// <see cref="UserConfigMigrationContext.CopyFromShared"/>), though the two can carry different stamps.
	/// </summary>
	/// <param name="shared"><c>Shared.json</c>, or <see langword="null"/> when there is none to bring forward.</param>
	/// <param name="profiles">Every profile to bring forward, the active one first.</param>
	/// <remarks>
	/// A profile behind <c>Shared.json</c> - copied in by hand later, say - sees <c>Shared.json</c> as it is
	/// now: a setting <c>Shared.json</c> has already taken from another profile stays as it is, and one it
	/// has already handed to the profiles and dropped is gone, so that setting gets its default. A file a
	/// step is missing for, or fails on, stops where it was read (<see cref="UserConfigMigratingFile.Error"/>);
	/// the others carry on.
	/// </remarks>
	internal static void MigrateTogether(UserConfigMigratingFile? shared, IReadOnlyList<UserConfigMigratingFile> profiles) =>
		MigrateTogether(shared, profiles, _currentVersion, _steps);

	/// <summary>
	/// Brings files up to <paramref name="toVersion"/> together with the steps given (see
	/// <see cref="MigrateTogether(UserConfigMigratingFile?, IReadOnlyList{UserConfigMigratingFile})"/>).
	/// </summary>
	/// <param name="shared"><c>Shared.json</c>, or <see langword="null"/>.</param>
	/// <param name="profiles">The profiles, the active one first.</param>
	/// <param name="toVersion">The layout to bring them to.</param>
	/// <param name="steps">The steps to choose from.</param>
	/// <exception cref="ArgumentOutOfRangeException">A file is in a layout older than <see cref="UserConfigVersion.Oldest"/> or newer than <paramref name="toVersion"/>.</exception>
	internal static void MigrateTogether(
		UserConfigMigratingFile? shared,
		IReadOnlyList<UserConfigMigratingFile> profiles,
		int toVersion,
		IReadOnlyList<UserConfigMigration> steps)
	{
		ArgumentNullException.ThrowIfNull(profiles);
		ArgumentNullException.ThrowIfNull(steps);

		UserConfigMigratingFile[] files = shared is null ? [.. profiles] : [.. profiles, shared];

		foreach (UserConfigMigratingFile file in files)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(file.Version, UserConfigVersion.Oldest);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(file.Version, toVersion);
		}

		if (files.Length == 0)
		{
			return;
		}

		for (int version = files.Min(file => file.Version) + 1; version <= toVersion; version++)
		{
			UserConfigMigration? step = steps.SingleOrDefault(s => s.ToVersion == version);

			foreach (UserConfigMigratingFile profile in profiles.Where(p => p.Error is null && p.Version == version - 1))
			{
				Dictionary<string, string>? sharedValues = shared is { Error: null } ? shared.Values : null;
				Run(profile, step, version, s => s.Apply(new UserConfigMigrationContext(profile.Values, sharedValues)));
			}

			if (shared is { Error: null } && shared.Version == version - 1)
			{
				Run(shared, step, version, s =>
				{
					UserConfigMigrationContext context = new(shared.Values);
					s.Apply(context);
					s.ThenShared?.Invoke(context);
				});
			}
		}
	}

	/// <summary>Runs one step on one file, or stops the file when the step is missing or fails.</summary>
	private static void Run(UserConfigMigratingFile file, UserConfigMigration? step, int version, Action<UserConfigMigration> apply)
	{
		if (step is null)
		{
			file.Fail(new InvalidOperationException($"No step brings settings to layout {version}."));
			return;
		}

		try
		{
			apply(step);
		}
		catch (Exception ex)
		{
			file.Fail(new InvalidOperationException($"Bringing settings to layout {version} failed ({step.Description}): {ex.Message}", ex));
			return;
		}

		file.Stepped(version, step.Description);
	}
}
