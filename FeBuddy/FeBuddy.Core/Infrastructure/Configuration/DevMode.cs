namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The developer-mode switch: extra output that makes troubleshooting easier.
/// </summary>
/// <remarks>
/// <para>
/// A plain static flag, never a user setting. The host sets <see cref="IsEnabled"/> from a code
/// constant before running anything: <c>App.DevModeEnabled</c> in the GUI,
/// <c>HarnessSettings.DevMode</c> in <c>FeBuddy.Harness</c>.
/// </para>
/// <para>When it is on:</para>
/// <list type="bullet">
///   <item>GeoJSON is always written indented, whatever the user's Settings preference
///   (<see cref="OutputFormatting.WriteIndentedGeojson"/>).</item>
///   <item><see cref="Logging.AppLog"/> records <c>Debug</c> entries.</item>
///   <item><c>FeBuddy.Harness</c> prints every message instead of a summary.</item>
/// </list>
/// </remarks>
public static class DevMode
{
	/// <summary>Whether developer mode is on. Off by default.</summary>
	public static bool IsEnabled { get; set; }
}
