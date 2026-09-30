using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

using FeBuddy.Wpf.Views;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Wpf.Shell;

/// <summary>Closes FE-Buddy and starts it again: how a reset from Settings is carried out.</summary>
public static class AppRestart
{
	private const string LogSource = "Restart";

	/// <summary>
	/// Starts a new FE-Buddy, then closes this one. If a new one cannot be started, says so and
	/// lets the user close this one and start it by hand - what a restart was for (a reset, say)
	/// happens then.
	/// </summary>
	/// <param name="owner">The window to centre the "start it again" message on, if one is needed.</param>
	/// <param name="purpose">What starting again finishes, for that message, e.g. <c>the reset</c>.</param>
	public static void Restart(Window? owner, string purpose)
	{
		try
		{
			string path = Environment.ProcessPath ?? throw new InvalidOperationException("This program's path is not known.");
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = false })?.Dispose();
			AppLog.Info(LogSource, $"Restarting FE-Buddy to finish {purpose}.");
		}
		catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
		{
			AppLog.Warning(LogSource, $"Could not start FE-Buddy again: {ex.Message}");

			if (!ConfirmWindow.Show(owner, "Start FE-Buddy again",
				$"FE-Buddy could not start itself again. Close it and start it from the Start menu to finish {purpose}.",
				confirmText: "Close FE-Buddy", cancelText: "Later"))
			{
				return;
			}
		}

		Application.Current?.Shutdown();
	}
}
