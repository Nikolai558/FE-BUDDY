namespace FEBuddyLibrary.Models.Services.General;

/// <summary>
/// The release channel a user opts into for updates. Persisted as its name in
/// <c>UserConfig.json</c> at <c>General.UpdateChannel</c>; <see cref="Stable"/> is the default
/// and the only channel a normal user should pick.
/// </summary>
public enum UpdateChannel
{
	/// <summary>Production releases only. The default.</summary>
	Stable = 0,

	/// <summary>Stable releases plus beta pre-releases.</summary>
	Beta = 1,

	/// <summary>Stable releases plus all pre-releases, including alpha.</summary>
	Alpha = 2,
}

/// <summary>
/// The outcome of the launch-time application version check.
/// </summary>
/// <param name="CurrentVersion">The running application's version string.</param>
/// <param name="LatestVersion">
/// The newest version found on <paramref name="Channel"/>, or <see langword="null"/> when the
/// check did not complete.
/// </param>
/// <param name="UpdateAvailable"><see langword="true"/> if <paramref name="LatestVersion"/> is newer than <paramref name="CurrentVersion"/>.</param>
/// <param name="Channel">The channel that was queried.</param>
/// <param name="CheckSucceeded">
/// <see langword="false"/> when offline or the GitHub API could not be reached/parsed - the
/// GUI shows update state as "unknown" rather than "up to date".
/// </param>
/// <param name="Message">A short human-readable note about the result or the failure.</param>
/// <param name="LatestReleaseNotes">The GitHub release body for <paramref name="LatestVersion"/>, when available.</param>
/// <param name="LatestReleaseUrl">The GitHub release page URL for <paramref name="LatestVersion"/>, when available.</param>
public record VersionCheckResult(
	string CurrentVersion,
	string? LatestVersion,
	bool UpdateAvailable,
	UpdateChannel Channel,
	bool CheckSucceeded,
	string? Message,
	string? LatestReleaseNotes = null,
	string? LatestReleaseUrl = null)
{
	/// <summary>
	/// Parses an <see cref="UpdateChannel"/> from its stored name, falling back to
	/// <see cref="UpdateChannel.Stable"/> for a missing or unrecognized value.
	/// </summary>
	/// <param name="value">The stored channel name (case-insensitive).</param>
	/// <returns>The parsed channel, or <see cref="UpdateChannel.Stable"/>.</returns>
	public static UpdateChannel ParseChannel(string? value) =>
		Enum.TryParse(value, ignoreCase: true, out UpdateChannel channel) ? channel : UpdateChannel.Stable;
}
