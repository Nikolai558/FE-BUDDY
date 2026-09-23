using FeBuddy.Core.Models.Services.Airac.Airports;

namespace FeBuddy.Core.Services.Airac.Airports;

/// <summary>
/// Builds the on-disk output directory for the Airports sub-service, honouring the
/// "Add FE-Buddy_Output folder" preference.
/// </summary>
internal static class AirportOutputPaths
{
	/// <summary>
	/// Resolves the directory an Airports output leaf (<c>Geojson</c> or <c>Alias</c>) is
	/// written to.
	/// </summary>
	/// <param name="settings">The parsed Airports settings.</param>
	/// <param name="leafFolder">The leaf folder name, e.g. <c>"Geojson"</c> or <c>"Alias"</c>.</param>
	/// <returns>
	/// <c>&lt;OutputDirectory&gt;\FE-Buddy_Output\Airports\&lt;leaf&gt;</c> when
	/// <see cref="AirportSettings.AddFeBuddyOutputFolder"/> is <see langword="true"/>, otherwise
	/// <c>&lt;OutputDirectory&gt;\Airports\&lt;leaf&gt;</c>. The <c>Airports</c> sub-folder is
	/// always kept - it separates sub-service output.
	/// </returns>
	public static string Resolve(AirportSettings settings, string leafFolder) =>
		settings.AddFeBuddyOutputFolder
			? Path.Combine(settings.OutputDirectory, "FE-Buddy_Output", "Airports", leafFolder)
			: Path.Combine(settings.OutputDirectory, "Airports", leafFolder);
}
