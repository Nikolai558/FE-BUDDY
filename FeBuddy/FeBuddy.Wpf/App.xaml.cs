using System.IO;
using System.Windows;
using System.Windows.Threading;

using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.FileSystem.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Wpf;

/// <summary>
/// Application entry point. Carries out a reset asked for in Settings (<see cref="AppDataReset"/>),
/// starts the shared application log's file sink and runs <see cref="LaunchSequence"/> off the UI
/// thread (see <c>Developer_Notes.md</c> -&gt; LAUNCH PROCESSES), then keeps a last-chance handler
/// that writes an unhandled exception to disk so a crash on a user's machine leaves a trace.
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

	/// <summary>How long a reset waits for the FE-Buddy that asked for it to close.</summary>
	private static readonly TimeSpan ResetWaitForExit = TimeSpan.FromSeconds(15);

	/// <inheritdoc />
	protected override void OnStartup(StartupEventArgs e)
	{
		// First, so everything that follows - the log sink included - sees the right mode.
		DevMode.IsEnabled = DevModeEnabled;

		DispatcherUnhandledException += OnUnhandled;

		// Before the log, the settings or any cycle is opened: a reset deletes them all.
		AppDataResetResult? reset = AppDataReset.RunPending(CredentialStore.Default, ResetWaitForExit);

		// One shared log stream for the whole app; the file sink also prunes stale log files.
		AppLog.StartFileSink();

		base.OnStartup(e);

		if (reset is not null)
		{
			// At idle, so the main window and its toast host are on screen.
			Dispatcher.InvokeAsync(() => ReportReset(reset), DispatcherPriority.ApplicationIdle);
		}

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

	/// <summary>Says the reset is done, and what it could not delete, if anything.</summary>
	private static void ReportReset(AppDataResetResult reset)
	{
		string settings = reset.SettingsKept ? "Your settings were kept." : "FE-Buddy is back to its default settings.";
		string credentials = reset.CredentialsRemoved is { } removed ? $" {removed} saved credential(s) were removed." : string.Empty;

		if (reset.NotDeleted.Count == 0)
		{
			Toast.Success("FE-Buddy was reset", $"{settings}{credentials} It is downloading the AIRAC data again.");
		}
		else
		{
			Toast.Warn("FE-Buddy was reset, but not completely",
				$"Could not delete {string.Join(", ", reset.NotDeleted)}. See the log for why.{credentials}");
		}
	}

	private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		try
		{
			// Beside the day's log, so it is attached with it and removed on uninstall.
			string directory = AppLog.LogDirectory;
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, "febuddy-wpf-crash.txt");
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
