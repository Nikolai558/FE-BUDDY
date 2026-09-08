using FEBuddyLibrary.Models.Services.Airac.Airways;

namespace FEBuddyLibrary.Services.Airac.Airways;

/// <summary>
/// Builds the on-disk output directory for an Airways sub-service, honouring the
/// "Add FE-Buddy_Output folder" preference (remediation plan 3.7).
/// </summary>
internal static class AirwayOutputPaths
{
	/// <summary>
	/// Resolves the directory an Airways output leaf (<c>Geojson</c> or <c>Alias</c>) is
	/// written to.
	/// </summary>
	/// <param name="settings">The parsed Airways settings.</param>
	/// <param name="leafFolder">The leaf folder name, e.g. <c>"Geojson"</c> or <c>"Alias"</c>.</param>
	/// <returns>
	/// <c>&lt;OutputDirectory&gt;\FE-Buddy_Output\Airways\&lt;leaf&gt;</c> when
	/// <see cref="AirwaySettings.AddFeBuddyOutputFolder"/> is <see langword="true"/>, otherwise
	/// <c>&lt;OutputDirectory&gt;\Airways\&lt;leaf&gt;</c>. The <c>Airways</c> sub-folder is
	/// always kept - it separates sub-service output.
	/// </returns>
	public static string Resolve(AirwaySettings settings, string leafFolder) =>
		settings.AddFeBuddyOutputFolder
			? Path.Combine(settings.OutputDirectory, "FE-Buddy_Output", "Airways", leafFolder)
			: Path.Combine(settings.OutputDirectory, "Airways", leafFolder);
}
