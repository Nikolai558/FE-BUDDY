namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>Settings brought forward by <see cref="UserConfigMigrations.Migrate(IReadOnlyDictionary{string, string}, int)"/>.</summary>
/// <param name="Values">The settings in layout <paramref name="ToVersion"/>, by dotted path.</param>
/// <param name="FromVersion">The layout they were in.</param>
/// <param name="ToVersion">The layout they are in now.</param>
/// <param name="Applied">What each step changed, oldest first; empty when they were already in <paramref name="ToVersion"/>.</param>
public sealed record UserConfigMigrationResult(
	IReadOnlyDictionary<string, string> Values,
	int FromVersion,
	int ToVersion,
	IReadOnlyList<string> Applied)
{
	/// <summary>Whether any step ran.</summary>
	public bool Migrated => Applied.Count > 0;
}
