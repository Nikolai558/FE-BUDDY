using System.Globalization;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// How a run writes its output, as chosen in Settings: the output directory, whether to wrap
/// everything in a <c>FE-Buddy_Output</c> folder, and how many decimal places GeoJSON coordinates keep.
/// </summary>
/// <remarks>
/// Every service screen reads these here, at the moment it runs, so a change in Settings applies
/// to the next run on every screen without any of them having to listen for it. Each AIRAC Service
/// run of a cycle goes in its own <c>AIRAC_&lt;cycle&gt;</c> folder inside (see
/// <see cref="AiracOutputPaths"/>).
/// </remarks>
public static class OutputPreferences
{
	/// <summary>The coordinate precision used when none is saved.</summary>
	private const int DefaultCoordinatePrecision = 6;

	/// <summary>The output directory used when none is saved: the user's Desktop.</summary>
	public static string DefaultDirectory =>
		Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

	/// <summary>The saved output directory, or <see cref="DefaultDirectory"/> when none is saved.</summary>
	public static string Directory =>
		UserConfigFile.GetValue(UserConfigKeys.DefaultOutputDirectory) is { } saved && !string.IsNullOrWhiteSpace(saved)
			? saved
			: DefaultDirectory;

	/// <summary>
	/// Where a file or folder picker opens: <paramref name="preferred"/> - the folder the picker's
	/// field already names - while it exists, else the output directory while it exists, else the Desktop.
	/// </summary>
	/// <param name="preferred">A more specific folder to open in, or <see langword="null"/>.</param>
	/// <returns>The folder's full path.</returns>
	public static string BrowseDirectory(string? preferred = null) =>
		!string.IsNullOrWhiteSpace(preferred) && System.IO.Directory.Exists(preferred) ? preferred
		: System.IO.Directory.Exists(Directory) ? Directory
		: DefaultDirectory;

	/// <summary>Whether to wrap output in a <c>FE-Buddy_Output</c> folder. On unless saved as <c>N</c>.</summary>
	public static bool AddFeBuddyOutputFolder =>
		!string.Equals(UserConfigFile.GetValue(UserConfigKeys.AddFeBuddyOutputFolder), "N", StringComparison.OrdinalIgnoreCase);

	/// <summary>Decimal places kept per GeoJSON coordinate: the saved value when it is 0-15, otherwise 6.</summary>
	public static int CoordinatePrecision =>
		int.TryParse(UserConfigFile.GetValue(UserConfigKeys.CoordinatePrecision), NumberStyles.Integer, CultureInfo.InvariantCulture, out int saved)
		&& saved is >= 0 and <= 15
			? saved
			: DefaultCoordinatePrecision;

	/// <summary>The folder a run of a cycle writes into, e.g. <c>…\Desktop\FE-Buddy_Output\AIRAC_2610</c>.</summary>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The folder's full path.</returns>
	public static string CycleDirectory(string cycleId) =>
		AiracOutputPaths.CycleDirectory(Directory, AddFeBuddyOutputFolder, cycleId);
}
