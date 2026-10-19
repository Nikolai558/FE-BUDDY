using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using FeBuddy.Wpf.Views;

using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// An update installed on this PC while this copy of FE-Buddy is open - by another Windows account, whose open
/// copy Windows Installer can't close. This copy would then run a mix of old and new files, so as soon as the
/// update has finished (<see cref="InstalledVersionWatcher"/>) it offers a restart, and a run waits for one.
/// </summary>
public static class InstalledUpdate
{
	private const string LogSource = "Update";

	private static InstalledVersionWatcher? _watcher;
	private static Func<IReadOnlyList<string>>? _describeUnfinishedWork;
	private static bool _offering;

	/// <summary>The version installed while this copy was open, or <see langword="null"/> while none has been.</summary>
	public static string? NewVersion { get; private set; }

	/// <summary>Starts watching FE-Buddy's own program file. Called once, by the main window's view-model.</summary>
	/// <param name="dispatcher">The UI thread's dispatcher, which the restart is offered on.</param>
	/// <param name="describeUnfinishedWork">What closing FE-Buddy now would lose, for the restart's warning.</param>
	public static void Start(Dispatcher dispatcher, Func<IReadOnlyList<string>> describeUnfinishedWork)
	{
		ArgumentNullException.ThrowIfNull(dispatcher);
		ArgumentNullException.ThrowIfNull(describeUnfinishedWork);

		if (_watcher is not null || Assembly.GetEntryAssembly()?.Location is not { Length: > 0 } path)
		{
			return;
		}

		_describeUnfinishedWork = describeUnfinishedWork;

		try
		{
			_watcher = new InstalledVersionWatcher(path, AppVersion.Current);
		}
		catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"FE-Buddy can't watch its own files for an update installed while it's open: {ex.Message}");
			return;
		}

		_watcher.Updated += (_, version) => dispatcher.BeginInvoke(() =>
		{
			NewVersion = version;
			AppLog.Warning(LogSource, $"FE-Buddy v{version} was installed while this copy, v{AppVersion.Current}, was open. Restart FE-Buddy to use it.");
			Offer();
		});
	}

	/// <summary>
	/// Before a run starts: whether it may. Once an update has been installed under this copy it may not, and
	/// the restart is offered again.
	/// </summary>
	/// <returns><see langword="true"/> when the run may start.</returns>
	public static bool AllowsRun()
	{
		if (NewVersion is null)
		{
			return true;
		}

		Offer();
		return false;
	}

	/// <summary>Offers the restart, warning first about anything unfinished it would lose.</summary>
	private static void Offer()
	{
		if (_offering || NewVersion is not { } version)
		{
			return;
		}

		_offering = true;

		try
		{
			Window? owner = Application.Current?.MainWindow is { IsVisible: true } main ? main : null;

			if (!ConfirmWindow.Show(owner, "FE-Buddy was updated",
				$"FE-Buddy v{version} was installed on this PC while this copy, v{AppVersion.Current}, was open. " +
				"The two don't mix, so runs wait until FE-Buddy restarts.",
				confirmText: "Restart now", cancelText: "Later"))
			{
				return;
			}

			IReadOnlyList<string> unfinished = _describeUnfinishedWork?.Invoke() ?? [];

			if (unfinished.Count > 0
				&& !ConfirmWindow.Show(
					owner,
					"Restart FE-Buddy?",
					"FE-Buddy closes to start the new version, and this will be lost:\n\n" + string.Join("\n", unfinished.Select(item => "•  " + item)),
					confirmText: "Restart anyway"))
			{
				return;
			}

			AppRestart.Restart(owner, "the update");
		}
		finally
		{
			_offering = false;
		}
	}
}
