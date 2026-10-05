namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>What <see cref="UserConfigFile.ReadAll"/> found on disk.</summary>
public enum UserConfigReadResult
{
	/// <summary>There is no <c>UserConfig.json</c> yet, as on a new install. Every setting is at its default.</summary>
	Missing = 0,

	/// <summary>The file was read.</summary>
	Read = 1,

	/// <summary>
	/// The file is there but isn't a JSON object FE-Buddy can read. Every setting is at its default,
	/// and nothing should be written to the file without the user asking, since a write replaces it.
	/// </summary>
	Unreadable = 2,
}
