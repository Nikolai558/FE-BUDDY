using System.IO;
using System.Windows;
using System.Windows.Threading;

using FeBuddy.Core.Configuration;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf;

/// <summary>
/// Application entry point. Starts the shared application log's file sink and kicks off the
/// off-UI-thread launch sequence (see <c>Developer_Notes.md</c> -&gt; LAUNCH PROCESSES), then
/// keeps a last-chance handler that writes an unhandled exception to disk so a crash on a
/// user's machine leaves a trace.
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// Developer mode for the app (<see cref="DevMode.IsEnabled"/>). Deliberately a code
	/// constant and never a user setting: flip it here for a troubleshooting build, and keep it
	/// <see langword="false"/> in anything released. While on, every GeoJSON file is pretty
	/// printed whatever Settings says, and Debug-level log entries are recorded.
	/// </summary>
	/// <remarks><c>FeBuddy.Harness</c> has its own equivalent in <c>HarnessSettings.DevMode</c>.</remarks>
	private const bool DevModeEnabled = false;

	/// <inheritdoc />
	protected override void OnStartup(StartupEventArgs e)
	{
		// First, so everything that follows - the log sink included - sees the right mode.
		DevMode.IsEnabled = DevModeEnabled;

		DispatcherUnhandledException += OnUnhandled;

		// One shared log stream for the whole app; the file sink also prunes stale log files.
		AppLog.StartFileSink();

		base.OnStartup(e);

		// The launch sequence runs off the UI thread. A failure in any step degrades the
		// dependent feature and is logged - it never blocks the window from showing.
		_ = Task.Run(RunLaunchSequenceAsync);
	}

	private static async Task RunLaunchSequenceAsync()
	{
		try
		{
			string version = AppVersion.Current;
			await LaunchSequence.RunAsync(version);
		}
		catch (Exception ex)
		{
			// LaunchSequence is designed not to throw; this is belt-and-braces so a bug there
			// can never take the app down at startup.
			AppLog.Error("Launch", $"Launch sequence threw unexpectedly: {ex.Message}");
		}
	}

	private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		try
		{
			var path = Path.Combine(Path.GetTempPath(), "febuddy-wpf-crash.txt");
			var sb = new System.Text.StringBuilder();
			for (Exception? ex = e.Exception; ex is not null; ex = ex.InnerException)
			{
				sb.AppendLine(ex.GetType().FullName + ": " + ex.Message);
				if (ex is System.Windows.Markup.XamlParseException xpe)
				{
					sb.AppendLine($"  BaseUri={xpe.BaseUri}  Line={xpe.LineNumber}  Pos={xpe.LinePosition}");
				}
			}
			sb.AppendLine();
			sb.AppendLine(e.Exception.ToString());
			File.WriteAllText(path, sb.ToString());

			AppLog.Error("App", "Unhandled dispatcher exception: " + e.Exception.Message);
		}
		catch
		{
			// Nothing useful to do if even logging fails.
		}
	}
}
