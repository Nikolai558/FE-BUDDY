using FeBuddy.Versioning;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Which layout of <c>UserConfig</c> a settings file is in: what its settings are called, where they
/// sit and how their values are written. Every <c>UserConfig.json</c> and every settings export is
/// stamped with it, so a later FE-Buddy knows which <see cref="UserConfigMigrations"/> steps bring
/// the file up to its own layout.
/// </summary>
/// <remarks>
/// <para>
/// Bump <see cref="Current"/> - and add the step from the old layout, a sample file of the new one and
/// its row in <see cref="Layouts"/> - whenever a change renames, moves, re-formats or drops a saved
/// setting, in a pre-release too. A layout number is never used twice. A new setting that has a
/// default needs none of this: a file without it gets the default.
/// </para>
/// <para>
/// A file with no stamp, or one stamped with a layout older than <see cref="Oldest"/>, is read as
/// <see cref="Oldest"/>: a setting it no longer uses is ignored, and one it lacks gets its default.
/// </para>
/// </remarks>
public static class UserConfigVersion
{
	/// <summary>
	/// The top-level entry each <c>UserConfig.json</c> carries its layout version in. It is not a
	/// setting: <see cref="UserConfigFile"/> keeps it out of the settings it reads, and writes it with
	/// every save.
	/// </summary>
	public const string FileKey = "ConfigVersion";

	/// <summary>
	/// The oldest layout FE-Buddy brings forward, and the one a file with no stamp, or an older one, is read as.
	/// It is 3.0.0's layout and never goes up, so a user who skipped releases is never cut off.
	/// </summary>
	public const int Oldest = 2;

	/// <summary>The layout this version of FE-Buddy reads and writes.</summary>
	public const int Current = 2;

	/// <summary>
	/// Each layout, oldest first, with the first FE-Buddy release that saved it: the layout table in
	/// <c>docs/Developers/Settings-Reference.md</c>. A new layout adds a row here with its step, so
	/// <see cref="ForRelease"/> knows which settings an older release can read.
	/// </summary>
	public static IReadOnlyList<(int Layout, string FirstSavedBy)> Layouts { get; } =
	[
		(2, "3.0.0-beta.6"),
	];

	/// <summary>The settings layout a release of FE-Buddy reads and writes, from <see cref="Layouts"/>.</summary>
	/// <param name="version">The release, e.g. <c>3.0.0-beta.13</c>, or its tag, e.g. <c>v3.0.0</c>.</param>
	/// <returns>
	/// The layout; or <see langword="null"/> for a release older than the first in <see cref="Layouts"/> -
	/// FE-Buddy 2.x, which has no <c>UserConfig</c> - or a version that can't be read, either of which has
	/// nothing to do with these settings.
	/// </returns>
	public static int? ForRelease(string? version)
	{
		if (!ProductVersion.TryParseTag(version, out ProductVersion? release))
		{
			return null;
		}

		int? layout = null;

		foreach ((int candidate, string firstSavedBy) in Layouts)
		{
			if (ProductVersion.Parse(firstSavedBy).ComparePrecedenceTo(release!) <= 0)
			{
				layout = candidate;
			}
		}

		return layout;
	}
}
