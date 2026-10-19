using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="SettingsBusy"/>'s wait as FE-Buddy closes: for the file work of a profile change or an
/// import still running, up to the time given.
/// </summary>
[Collection("AppLog")]
public sealed class SettingsBusyTests
{
	/// <summary>
	/// Closing waits for the file work still running, so FE-Buddy doesn't close half way through writing a
	/// profile; work that failed is over too, and with none running there is nothing to wait for.
	/// </summary>
	[Fact]
	public async Task closing_waits_for_the_file_work_running()
	{
		TaskCompletionSource running = new();
		SettingsBusy.Start();
		SettingsBusy.Track(running.Task);

		try
		{
			Assert.False(SettingsBusy.WaitForFileWork(TimeSpan.Zero));

			// The minute is only a guard against hanging: the work finishes at once.
			Task<bool> closing = Task.Run(() => SettingsBusy.WaitForFileWork(TimeSpan.FromMinutes(1)));
			running.SetResult();
			Assert.True(await closing);

			SettingsBusy.Track(Task.FromException(new IOException("locked")));
			Assert.True(SettingsBusy.WaitForFileWork(TimeSpan.Zero));
		}
		finally
		{
			SettingsBusy.Finish();
		}

		Assert.False(SettingsBusy.IsBusy);
		Assert.True(SettingsBusy.WaitForFileWork(TimeSpan.Zero));
	}
}
