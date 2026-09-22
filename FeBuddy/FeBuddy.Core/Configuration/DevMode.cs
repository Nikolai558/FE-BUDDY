namespace FeBuddy.Core.Configuration;

/// <summary>
/// Global developer-mode switch used to make troubleshooting easier during development.
/// </summary>
/// <remarks>
/// This is intentionally a simple static flag rather than a configuration object, and never a
/// user setting. The caller sets <see cref="IsEnabled"/> from a code constant before invoking
/// any library service: <c>App.DevModeEnabled</c> in the GUI, <c>HarnessSettings.DevMode</c> in
/// <c>FeBuddy.Harness</c>.
///
/// Effects of <see cref="IsEnabled"/> being <see langword="true"/>:
/// <list type="bullet">
///   <item>GeoJSON files are written pretty-printed instead of single-line, overriding the
///   user's Settings preference (<see cref="OutputFormatting.WriteIndentedGeojson"/>).</item>
///   <item>Service result objects (e.g. <c>AirwayServiceResult</c>) include the full
///   per-item warning list rather than a trimmed summary.</item>
/// </list>
/// </remarks>
public static class DevMode
{
	/// <summary>
	/// Gets or sets a value indicating whether developer-mode behavior is active.
	/// Defaults to <see langword="false"/> so production output (single-line GeoJSON,
	/// trimmed warnings) is the default behavior.
	/// </summary>
	public static bool IsEnabled { get; set; } = false;
}
