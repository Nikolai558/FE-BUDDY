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
	/// A folder on this PC. Exported with the user's own folders swapped for tokens (see
	/// <see cref="PortablePathTokens"/>), and imported only when the folder exists on the importing PC.
	/// </summary>
	MachinePath = 1,

	/// <summary>State that only makes sense on this PC (the update channel, the News post last read). Never exported; an import keeps this PC's value.</summary>
	Local = 2,

	/// <summary>A credential (a token, a password). Never exported, and ignored in any file that carries one.</summary>
	Secret = 3,
}
