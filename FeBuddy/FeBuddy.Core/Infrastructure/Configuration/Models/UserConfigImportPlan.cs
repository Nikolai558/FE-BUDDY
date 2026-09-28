namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>
/// What importing a <see cref="UserConfigPackage"/> would do to this PC's settings, worked out by
/// <see cref="UserConfigTransfer.Plan(UserConfigPackage)"/> so the user can see it before
/// <see cref="UserConfigTransfer.Apply(UserConfigImportPlan)"/> writes anything.
/// </summary>
/// <param name="Package">The file being imported.</param>
/// <param name="Settings">Every setting the config holds after the import, by dotted path.</param>
/// <param name="ChangedCount">How many settings the import adds, changes or removes.</param>
/// <param name="AppliedFolders">The folder settings the import changes: taken from the file, or cleared because the file has none.</param>
/// <param name="SkippedFolders">
/// The file's folders and files that cannot work on this PC, each with why and what happens instead:
/// this PC keeps its own, the default is used, or - a custom alias file - it is left out.
/// </param>
/// <param name="KeptForThisPc">The names of this PC's own settings that the import leaves alone.</param>
/// <param name="IgnoredKeys">Keys in the file that are never imported: PC-only state, credentials, unknown sections.</param>
public sealed record UserConfigImportPlan(
	UserConfigPackage Package,
	IReadOnlyDictionary<string, string> Settings,
	int ChangedCount,
	IReadOnlyList<ImportedFolder> AppliedFolders,
	IReadOnlyList<ImportedFolder> SkippedFolders,
	IReadOnlyList<string> KeptForThisPc,
	IReadOnlyList<string> IgnoredKeys)
{
	/// <summary>Whether importing would change anything at all.</summary>
	public bool HasChanges => ChangedCount > 0;
}
