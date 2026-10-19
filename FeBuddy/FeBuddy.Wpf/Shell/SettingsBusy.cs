using System.Windows.Input;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// Whether a settings profile change or a settings import is running. Their file work runs off the UI thread,
/// so the window stays live meanwhile; until it is done, the profile in use and the settings in memory are
/// about to change, so nothing else may change or save them: the profile list, Import, New, Rename and
/// Delete are off, and <see cref="SettingsWrites"/> refuses a page's save.
/// </summary>
/// <remarks>
/// Set and cleared on the UI thread. A profile change and an import never run together: each is off while
/// the other runs.
/// </remarks>
internal static class SettingsBusy
{
	private static Task _fileWork = Task.CompletedTask;

	/// <summary>Raised on the UI thread when <see cref="IsBusy"/> changes.</summary>
	internal static event EventHandler? Changed;

	/// <summary>Whether a profile change or an import is running.</summary>
	internal static bool IsBusy { get; private set; }

	/// <summary>Marks a profile change or an import as started. Call on the UI thread.</summary>
	internal static void Start() => Set(true);

	/// <summary>
	/// Records the file work of the profile change or import running, which FE-Buddy waits for as it closes
	/// (<see cref="WaitForFileWork"/>).
	/// </summary>
	/// <param name="fileWork">The work, running off the UI thread.</param>
	internal static void Track(Task fileWork) => _fileWork = fileWork;

	/// <summary>Marks the profile change or import as finished, whether or not it worked. Call on the UI thread.</summary>
	internal static void Finish()
	{
		_fileWork = Task.CompletedTask;
		Set(false);
	}

	/// <summary>
	/// Waits for the file work running, so FE-Buddy doesn't close half way through writing a profile. For
	/// FE-Buddy closing: it blocks the calling thread.
	/// </summary>
	/// <param name="timeout">How long to wait at most.</param>
	/// <returns><see langword="true"/> when nothing is running any more, whether it worked or failed.</returns>
	internal static bool WaitForFileWork(TimeSpan timeout)
	{
		try
		{
			return _fileWork.Wait(timeout);
		}
		catch (AggregateException)
		{
			// It failed, so it is no longer running: what failed was reported where it ran.
			return true;
		}
	}

	private static void Set(bool busy)
	{
		if (IsBusy == busy)
		{
			return;
		}

		IsBusy = busy;
		Changed?.Invoke(null, EventArgs.Empty);
		CommandManager.InvalidateRequerySuggested();
	}
}
