namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>
/// Whether a <c>UserConfig</c> setting can travel to another PC in a settings export, as decided by
/// <see cref="UserConfigPortability.Classify(string)"/>.
/// </summary>
public enum ConfigKeyScope
{
	/// <summary>A preference that means the same on any PC (the ARTCC, the ROI, every GeoJSON option). Exported and imported as it is.</summary>
	Shared = 0,

	/// <summary>
	/// A folder or file on this PC (a key ending in <c>Folder</c>, <c>Directory</c> or <c>FilePath</c>).
	/// Exported with the user's own folders swapped for tokens (see <see cref="PortablePathTokens"/>),
	/// and imported only where it works on the importing PC: an output folder needs only its drive, a
	/// folder FE-Buddy reads from must exist, and so must a file.
	/// </summary>
	MachinePath = 1,

	/// <summary>State that only makes sense on this PC (the update channel, the News post last read). Never exported; an import keeps this PC's value.</summary>
	Local = 2,

	/// <summary>A credential (a token, a password). Never exported, and ignored in any file that carries one.</summary>
	Secret = 3,

	/// <summary>
	/// Which of this PC's saved credentials a setting uses (a key ending in <c>CredentialId</c>, such as
	/// a custom alias file's). The id only means something to the Credential Manager that holds it, so
	/// it is never exported, and ignored in any file that carries one. An import keeps this PC's
	/// choice where it leaves the settings beside it unchanged - the same custom alias file at the same
	/// address - and clears it otherwise, so a choice never ends up on a different file.
	/// </summary>
	CredentialChoice = 4,
}
