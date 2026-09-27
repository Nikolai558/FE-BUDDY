namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>A settings file read by <see cref="UserConfigTransfer.Read(string)"/>, not yet imported.</summary>
/// <param name="FileName">The file's name, for messages.</param>
/// <param name="FormatVersion">
/// The export format version, or <c>0</c> for a plain <c>UserConfig.json</c> copied from another PC.
/// </param>
/// <param name="AppVersion">The FE-Buddy version that exported it, when the file says.</param>
/// <param name="ExportedUtc">When it was exported, when the file says.</param>
/// <param name="Values">Every setting in the file, by dotted path, as it is in the file (folders still tokenized).</param>
public sealed record UserConfigPackage(
	string FileName,
	int FormatVersion,
	string? AppVersion,
	DateTimeOffset? ExportedUtc,
	IReadOnlyDictionary<string, string> Values)
{
	/// <summary>Whether the file is a plain <c>UserConfig.json</c> rather than an FE-Buddy export.</summary>
	public bool IsPlainConfigFile => FormatVersion == 0;
}
