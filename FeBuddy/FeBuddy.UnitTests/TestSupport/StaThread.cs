using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace FeBuddy.UnitTests.TestSupport;

/// <summary>
/// Runs a test's body on a thread of its own set up the way WPF needs (single-threaded apartment),
/// as a WPF control can only be created on one. The thread's dispatcher is shut down afterwards.
/// </summary>
internal static class StaThread
{
	/// <summary>Runs <paramref name="action"/> on a new STA thread and waits for it; anything it throws is rethrown here.</summary>
	/// <param name="action">The test's body.</param>
	public static void Run(Action action)
	{
		ExceptionDispatchInfo? failure = null;

		Thread thread = new(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				failure = ExceptionDispatchInfo.Capture(ex);
			}
			finally
			{
				Dispatcher.CurrentDispatcher.InvokeShutdown();
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();

		failure?.Throw();
	}
}
