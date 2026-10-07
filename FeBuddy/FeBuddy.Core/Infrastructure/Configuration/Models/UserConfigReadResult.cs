namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>What <see cref="UserConfigFile.ReadAll"/> found on disk.</summary>
public enum UserConfigReadResult
{
	/// <summary>The profile in use has no file yet, as on a new install. Every setting but the shared ones is at its default.</summary>
	Missing = 0,

	/// <summary>The profile's file was read.</summary>
	Read = 1,

	/// <summary>
	/// The profile's file is there but isn't a JSON object FE-Buddy can read. Every setting but the
	/// shared ones is at its default, and nothing should be written to the file without the user
	/// asking, since a write replaces it.
	/// </summary>
	Unreadable = 2,
}
