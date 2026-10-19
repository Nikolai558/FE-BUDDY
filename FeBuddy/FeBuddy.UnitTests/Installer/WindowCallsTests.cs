using System.Windows.Threading;

using FeBuddy.Installer.UI.Setup;

namespace FeBuddy.UnitTests.Installer;

/// <summary>
/// Covers <see cref="WindowCalls"/>, compiled in from the setup window's source: once the window's loop has
/// failed, a call Windows Installer's thread is waiting on, and any made after, end at once with the answer for
/// no window, rather than holding the install, and Windows Installer's lock, for good.
/// </summary>
public sealed class WindowCallsTests
{
	[Fact]
	public async Task calls_to_a_window_whose_loop_has_failed_end_at_once()
	{
		Dispatcher? window = null;
		using ManualResetEventSlim busy = new();
		using ManualResetEventSlim fail = new();

		Thread thread = new(() =>
		{
			window = Dispatcher.CurrentDispatcher;
			window.BeginInvoke(() =>
			{
				busy.Set();
				fail.Wait();
				throw new InvalidOperationException("The window failed.");
			});

			try
			{
				Dispatcher.Run();
			}
			catch (InvalidOperationException)
			{
				// What the setup window does when its loop fails.
				WindowCalls.EndWindowThread();
			}
		})
		{
			IsBackground = true,
		};

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(busy.Wait(TimeSpan.FromSeconds(10)));

		// A question asked while the window is busy, which then fails.
		Task<int> asked = Task.Run(() => WindowCalls.Invoke(window!, () => 1, -1));
		fail.Set();

		// The ten seconds are only a guard against hanging: the call ends as soon as the loop has failed.
		Assert.True(await Task.WhenAny(asked, Task.Delay(TimeSpan.FromSeconds(10))) == asked, "The call waiting for the window never ended.");
		Assert.Equal(-1, await asked);
		Assert.Equal(-1, WindowCalls.Invoke(window!, () => 1, -1));
		Assert.False(WindowCalls.Invoke(window!, () => { }));
	}
}
