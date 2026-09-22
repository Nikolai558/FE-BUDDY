using FeBuddy.Core.Helpers;

namespace FeBuddy.Core.Configuration;

/// <summary>
/// App-wide choices about how FE-Buddy lays out the files it writes. Applies to every GeoJSON
/// file, whichever service produces it - the AIRAC Service today, converters and other
/// services later - because they all write through <c>GeojsonFileWriter</c>.
/// </summary>
/// <remarks>
/// Like <see cref="DevMode"/>, this is a plain static the caller sets before running a service:
/// <c>LaunchSequence</c> loads it from <c>UserConfig</c> at start-up, the Settings screen sets it
/// when the user saves, and <c>FeBuddy.Harness</c> can set it directly.
/// </remarks>
public static class OutputFormatting
{
	/// <summary>The <c>UserConfig</c> key the preference is saved under (<c>Y</c> / <c>N</c>).</summary>
	public const string PrettyPrintGeojsonKey = "General.PrettyPrintGeojson";

	/// <summary>
	/// The user's preference: <see langword="true"/> writes GeoJSON indented, one property per
	/// line; <see langword="false"/> (the default) writes each file on a single line, which is
	/// considerably smaller.
	/// </summary>
	public static bool PrettyPrintGeojson { get; set; }

	/// <summary>
	/// Whether GeoJSON is actually written indented: when the user asked for it, or whenever
	/// <see cref="DevMode.IsEnabled"/> is on - developer mode always pretty prints, whatever the
	/// saved preference.
	/// </summary>
	public static bool WriteIndentedGeojson => DevMode.IsEnabled || PrettyPrintGeojson;

	/// <summary>
	/// Loads <see cref="PrettyPrintGeojson"/> from <c>UserConfig</c>. Anything other than
	/// <c>Y</c> - including no saved value - means single line.
	/// </summary>
	public static void LoadFromUserConfig() =>
		PrettyPrintGeojson = string.Equals(
			UserConfigFile.GetValue(PrettyPrintGeojsonKey)?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);
}
