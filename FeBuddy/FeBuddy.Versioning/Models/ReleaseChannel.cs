namespace FeBuddy.Versioning.Models;

/// <summary>
/// The release channel a version belongs to, from its pre-release tag - and the channel a user
/// opts into for updates, stored by name in <c>UserConfig.json</c> at <c>General.UpdateChannel</c>.
/// Ordered low to high, matching SemVer precedence (a stable release outranks its pre-releases).
/// </summary>
/// <remarks>
/// A user's channel is the <b>lowest</b> channel they accept: Stable sees only stable releases,
/// ReleaseCandidate sees -rc and stable, Beta sees -beta, -rc and stable, and Alpha sees
/// everything. The names and order match FE-Buddy 2.x's, so the two lines agree on which
/// release belongs where.
/// </remarks>
public enum ReleaseChannel
{
	/// <summary>An <c>-alpha.N</c> release (or any pre-release tag not recognised below). Early, may be broken.</summary>
	Alpha = 0,

	/// <summary>A <c>-beta.N</c> release. Feature-complete, being tested.</summary>
	Beta = 1,

	/// <summary>An <c>-rc.N</c> release. Believed ready to ship.</summary>
	ReleaseCandidate = 2,

	/// <summary>A release with no pre-release tag. The default channel.</summary>
	Stable = 3,
}
