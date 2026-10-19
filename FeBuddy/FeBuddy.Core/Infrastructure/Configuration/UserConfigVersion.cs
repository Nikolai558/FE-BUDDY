namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Which layout of <c>UserConfig</c> a settings file is in: what its settings are called, where they
/// sit and how their values are written. Every <c>UserConfig.json</c> and every settings export is
/// stamped with it, so a later FE-Buddy knows which <see cref="UserConfigMigrations"/> steps bring
/// the file up to its own layout.
/// </summary>
/// <remarks>
/// <para>
/// Bump <see cref="Current"/> - and add the step from the old layout, and a sample file of the new
/// one - whenever a change renames, moves, re-formats or drops a saved setting. A new setting that
/// has a default needs neither: a file without it gets the default.
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

	/// <summary>The oldest layout FE-Buddy brings forward, and the one a file with no stamp, or an older one, is read as.</summary>
	public const int Oldest = 2;

	/// <summary>The layout this version of FE-Buddy reads and writes.</summary>
	public const int Current = 2;
}
