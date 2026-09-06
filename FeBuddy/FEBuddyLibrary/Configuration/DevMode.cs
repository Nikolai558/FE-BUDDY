namespace FEBuddyLibrary.Configuration;

/// <summary>
/// Global developer-mode switch used to make troubleshooting easier during development.
/// </summary>
/// <remarks>
/// This is intentionally a simple static flag rather than a configuration object. The
/// caller (currently <c>FEBuddyTest.Program</c>, later the GUI) is responsible for setting
/// <see cref="IsEnabled"/> before invoking any library service.
///
/// Effects of <see cref="IsEnabled"/> being <see langword="true"/>:
/// <list type="bullet">
///   <item>GeoJSON files are written pretty-printed instead of single-line.</item>
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
