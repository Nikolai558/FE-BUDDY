namespace FeBuddy.Core.Infrastructure.FileSystem;

/// <summary>
/// Where a service writes its files, honouring the "Add FE-Buddy_Output folder" preference.
/// Shared by every service that writes output (the AIRAC Service's <c>AIRAC_&lt;cycle&gt;</c>
/// folders and the file conversions), so they all sit side by side in the same place.
/// </summary>
internal static class ServiceOutputPaths
{
	/// <summary>The folder every service's output goes in when the preference is on.</summary>
	public const string FeBuddyOutputFolder = "FE-Buddy_Output";

	/// <summary>
	/// A folder inside the output directory: <c>&lt;output&gt;\FE-Buddy_Output\&lt;folders&gt;</c>
	/// when <paramref name="addFeBuddyOutputFolder"/> is set, otherwise <c>&lt;output&gt;\&lt;folders&gt;</c>.
	/// </summary>
	/// <param name="outputDirectory">The folder the user pointed output at.</param>
	/// <param name="addFeBuddyOutputFolder">Whether to put everything inside a <c>FE-Buddy_Output</c> folder.</param>
	/// <param name="folders">
	/// The service's own folder first (it keeps services apart), then any below it, e.g.
	/// <c>"AIRAC_2610"</c> or <c>"DAT to GeoJSON"</c>.
	/// </param>
	/// <returns>The folder's full path.</returns>
	public static string Resolve(string outputDirectory, bool addFeBuddyOutputFolder, params string[] folders)
	{
		string root = addFeBuddyOutputFolder ? Path.Combine(outputDirectory, FeBuddyOutputFolder) : outputDirectory;
		return Path.Combine([root, .. folders]);
	}
}
