namespace FeBuddy.Core.Infrastructure.Platform.Models;

/// <summary>
/// What <see cref="LegacySquirrelInstall.Remove"/> found and did about FE-Buddy 2.x's Squirrel copy.
/// </summary>
public enum LegacySquirrelCleanupResult
{
	/// <summary>No Squirrel copy and no Installed apps entry left from one - the usual case.</summary>
	NotFound = 0,

	/// <summary>Squirrel's own uninstaller ran and removed the old copy.</summary>
	Uninstalled = 1,

	/// <summary>The uninstaller could not start, failed or timed out. It runs again next launch.</summary>
	UninstallFailed = 2,

	/// <summary>The old copy's files were already gone; its stranded Installed apps entry was removed.</summary>
	OrphanedEntryRemoved = 3,

	/// <summary>This FE-Buddy runs from inside the old copy's folder, so the uninstaller would delete it.</summary>
	Skipped = 4,
}
