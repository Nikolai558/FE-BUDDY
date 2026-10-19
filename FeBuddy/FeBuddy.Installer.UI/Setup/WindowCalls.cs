using System;
using System.Windows.Threading;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// The calls Windows Installer's thread makes to the setup window's - a question, a fatal error, the finish
/// page - and what ends them when the window's thread fails. msiexec waits for each one, so a call left waiting
/// on a thread whose loop has died would hold the install, and Windows Installer's lock, for good. Uses no
/// Windows Installer types, so the unit tests compile it in.
/// </summary>
public static class WindowCalls
{
	/// <summary>Runs <paramref name="call"/> on the window's thread and waits for its answer.</summary>
	/// <typeparam name="T">The answer.</typeparam>
	/// <param name="window">The window thread's dispatcher.</param>
	/// <param name="call">The call.</param>
	/// <param name="ended">The answer when the window's thread has ended, so nothing can answer.</param>
	/// <returns>The call's answer, or <paramref name="ended"/>.</returns>
	public static T Invoke<T>(Dispatcher window, Func<T> call, T ended)
	{
		try
		{
			return window.Invoke(call);
		}
		catch (OperationCanceledException)
		{
			return ended;
		}
	}

	/// <summary>Runs <paramref name="call"/> on the window's thread and waits for it.</summary>
	/// <param name="window">The window thread's dispatcher.</param>
	/// <param name="call">The call.</param>
	/// <returns>Whether it ran: <see langword="false"/> when the window's thread has ended.</returns>
	public static bool Invoke(Dispatcher window, Action call) =>
		Invoke(window, () =>
		{
			call();
			return true;
		}, false);

	/// <summary>
	/// Ends the calling thread's dispatcher once the window's loop has failed on it: every call waiting for it,
	/// and any made later, ends at once rather than waiting for good.
	/// </summary>
	public static void EndWindowThread()
	{
		try
		{
			Dispatcher.CurrentDispatcher.InvokeShutdown();
		}
		catch (Exception)
		{
			// The window has already failed: this is the last thing done for it, and must not fail in turn.
		}
	}
}
