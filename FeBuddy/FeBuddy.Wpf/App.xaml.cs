using System.IO;
using System.Windows;
using System.Windows.Threading;

using FeBuddy.Wpf.Shell;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.FileSystem.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Wpf;

/// <summary>
/// Application entry point. Carries out a reset asked for in Settings (<see cref="AppDataReset"/>),
/// starts the shared application log's file sink, reads the saved settings before any window is
/// built, and runs the rest of <see cref="LaunchSequence"/> off the UI thread (see
/// docs/Developers/Architecture.md, Launch), then keeps last-chance handlers that
/// write an exception nothing caught, on any thread, to disk (<see cref="CrashReport"/>) so a
/// crash on a user's machine leaves a trace.
/// </summary>
public partial class App : Application
{
	/// <summary>
	/// Developer mode for the app (<see cref="DevMode.IsEnabled"/>). Deliberately a code
	/// constant and never a user setting: flip it here for a troubleshooting build, and keep it
	/// <see langword="false"/> in anything released. While on, every GeoJSON file is pretty
	/// printed whatever Settings says, and Debug-level log entries are recorded. A dev launch
	/// (<see cref="DevLaunchArgs"/>) turns developer mode on too, without touching this.
	/// </summary>
	/// <remarks><c>FeBuddy.Harness</c> has its own equivalent in <c>HarnessSettings.DevMode</c>.</remarks>
	private const bool DevModeEnabled = false;

	/// <summary>How long a reset waits for the FE-Buddy that asked for it to close.</summary>
	private static readonly TimeSpan ResetWaitForExit = TimeSpan.FromSeconds(15);

	/// <inheritdoc />
	protected override void OnStartup(StartupEventArgs e)
	{
		// A dev launch (--dev, Debug builds only) runs just the launch steps chosen on Settings'
		// Developer card. A Release build says it ignored the argument, once the log is open.
		bool devLaunchAsked = DevLaunchArgs.IsRequested(e.Args);
#if DEBUG
		bool devLaunch = devLaunchAsked;
#else
		bool devLaunch = false;
#endif

		// First, so everything that follows - the log sink included - sees the right mode.
		DevMode.IsEnabled = DevModeEnabled || devLaunch;

		DispatcherUnhandledException += OnUnhandled;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledElsewhere;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

		// Before the log, the settings or any cycle is opened: a reset deletes them all. It can take
		// seconds (waiting for the FE-Buddy that asked for it to close, then deleting every cycle), and no
		// window can open before it: FE-Buddy's mark is shown meanwhile, so the click on Reset isn't
		// followed by nothing.
		SplashScreen? splash = AppDataReset.IsPending ? ShowSplash() : null;
		AppDataResetResult? reset = AppDataReset.RunPending(CredentialStore.Default, ResetWaitForExit);
		splash?.Close(TimeSpan.Zero);

		// One shared log stream for the whole app; the file sink also prunes stale log files.
		AppLog.StartFileSink();

		if (devLaunchAsked && !devLaunch)
		{
			AppLog.Info("Launch", $"Ignored {DevLaunchArgs.Argument}: a dev launch works in Debug builds only.");
		}

		// Published before the window is built, so the shell and every page know a dev launch.
		LaunchOptions options = devLaunch ? LaunchOptions.DevLaunch(DevLaunchSettings.Read()) : LaunchOptions.Normal;

		// Before the window: every page reads its settings as it is built, so they must be in memory
		// first, or a page shows defaults and saving it writes them over the user's settings.
		LaunchSequence.ReadSettings(AppVersion.Current, options);
		options = options with { SettingsRead = true };
		LaunchSequence.UseOptions(options);

		base.OnStartup(e);

		if (reset is not null)
		{
			// At idle, so the main window and its toast host are on screen.
			Dispatcher.InvokeAsync(() => ReportReset(reset), DispatcherPriority.ApplicationIdle);
		}

		// The launch sequence runs off the UI thread. A failure in any step degrades the
		// dependent feature and is logged - it never blocks the window from showing.
		_ = Task.Run(() => RunLaunchSequenceAsync(options));
	}

	private static async Task RunLaunchSequenceAsync(LaunchOptions options)
	{
		try
		{
			string version = AppVersion.Current;
			await LaunchSequence.RunAsync(version, options);
		}
		catch (Exception ex)
		{
			// LaunchSequence is designed not to throw; this is belt-and-braces so a bug there
			// can never take the app down at startup.
			AppLog.Error("Launch", $"Launch sequence threw unexpectedly: {ex.Message}");
		}
	}

	/// <summary>Says the reset is done, and what it could not delete, if anything.</summary>
	/// <summary>
	/// FE-Buddy's mark on screen while a reset runs: WPF's own splash, drawn without the dispatcher, so it
	/// shows before any window can. <see langword="null"/> when it can't be shown; the reset goes ahead.
	/// </summary>
	private static SplashScreen? ShowSplash()
	{
		try
		{
			SplashScreen splash = new(typeof(App).Assembly, "Assets/Brand/FEB_96.png");
			splash.Show(autoClose: false, topMost: true);
			return splash;
		}
		catch (Exception ex) when (ex is IOException or InvalidOperationException)
		{
			return null;
		}
	}

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

	private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e) =>
		CrashReport.Write(e.Exception, "on the UI thread");

	/// <summary>
	/// An exception on any other thread - an <see langword="async"/> <see langword="void"/>
	/// continuation off the UI thread, a timer, a thread - which ends the process: the report is all
	/// that is left of it.
	/// </summary>
	private static void OnUnhandledElsewhere(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
		{
			CrashReport.Write(exception, "off the UI thread");
		}
	}

	/// <summary>A failed task nothing waited for. It doesn't end the process, but would otherwise go unseen.</summary>
	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) =>
		AppLog.Error("App", $"A background task failed and nothing waited for it: {e.Exception.GetBaseException().Message}");
}
