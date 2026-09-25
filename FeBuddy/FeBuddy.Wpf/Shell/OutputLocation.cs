using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Where runs write: the saved output folder (Settings ▸ Default Output Directory) and the "Add a
/// FE-Buddy_Output folder" preference, read the same way everywhere.
/// </summary>
/// <remarks>
/// Settings edits and saves them; the AIRAC Service reads them for a run and the Preview Settings
/// tab to say where it will write. Each run of a cycle goes in its own
/// <c>AIRAC_&lt;cycle&gt;</c> folder inside (see <see cref="AiracOutputPaths"/>).
/// </remarks>
public static class OutputLocation
{
	/// <summary>The output folder when none is saved: the user's Desktop.</summary>
	public static string DefaultDirectory =>
		Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

	/// <summary>The saved output folder, or <see cref="DefaultDirectory"/> when none is saved.</summary>
	public static string Directory =>
		UserConfigFile.GetValue(UserConfigKeys.DefaultOutputDirectory) is { } saved && !string.IsNullOrWhiteSpace(saved)
			? saved
			: DefaultDirectory;

	/// <summary>Whether output goes in a <c>FE-Buddy_Output</c> folder. On unless saved as <c>N</c>.</summary>
	public static bool AddFeBuddyOutputFolder =>
		!string.Equals(UserConfigFile.GetValue(UserConfigKeys.AddFeBuddyOutputFolder), "N", StringComparison.OrdinalIgnoreCase);

	/// <summary>The folder a run of a cycle writes into, e.g. <c>…\Desktop\FE-Buddy_Output\AIRAC_2610</c>.</summary>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The folder's full path.</returns>
	public static string CycleDirectory(string cycleId) =>
		AiracOutputPaths.CycleDirectory(Directory, AddFeBuddyOutputFolder, cycleId);
}
