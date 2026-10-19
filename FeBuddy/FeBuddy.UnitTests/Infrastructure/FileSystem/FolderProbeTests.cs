using FeBuddy.Core.Infrastructure.FileSystem;

namespace FeBuddy.UnitTests.Infrastructure.FileSystem;

/// <summary>
/// Covers <see cref="FolderProbe"/>: a folder counts as there only when it is, and answers in time - one
/// on a share that doesn't answer counts as not there, rather than holding up the picker that asked.
/// </summary>
public sealed class FolderProbeTests
{
	/// <summary>A folder that is there is; a missing one, or none at all, isn't.</summary>
	[Fact]
	public void a_folder_is_there_when_it_is()
	{
		Assert.True(FolderProbe.Exists(Path.GetTempPath(), FolderProbe.DefaultTimeout));
		Assert.False(FolderProbe.Exists(Path.Combine(Path.GetTempPath(), "FeBuddyTests_NoSuchFolder_" + Guid.NewGuid().ToString("N")), FolderProbe.DefaultTimeout));
		Assert.False(FolderProbe.Exists(null, FolderProbe.DefaultTimeout));
		Assert.False(FolderProbe.Exists("  ", FolderProbe.DefaultTimeout));
	}

	/// <summary>A folder that doesn't answer in time counts as not there, and the caller isn't held for the share's own timeout.</summary>
	[Fact]
	public void a_folder_that_does_not_answer_in_time_is_not_there()
	{
		using ManualResetEventSlim release = new();
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		bool exists = FolderProbe.Exists(@"\offline\share", TimeSpan.FromMilliseconds(100), _ =>
		{
			release.Wait(TimeSpan.FromSeconds(10));
			return true;
		});

		release.Set();
		Assert.False(exists);
		Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}.");
	}

	/// <summary>
	/// A folder still being asked about - a share that hasn't answered - isn't asked again: each question would hold
	/// another thread until the share times out.
	/// </summary>
	[Fact]
	public void a_folder_still_being_asked_is_not_asked_again()
	{
		using ManualResetEventSlim release = new();
		string share = @"\\offline\share-" + Guid.NewGuid().ToString("N");
		int asked = 0;

		bool Exists(string _)
		{
			Interlocked.Increment(ref asked);

			// The ten seconds are only a guard against hanging: the test lets it answer.
			release.Wait(TimeSpan.FromSeconds(10));
			return true;
		}

		Assert.False(FolderProbe.Exists(share, TimeSpan.FromMilliseconds(100), Exists));
		Assert.False(FolderProbe.Exists(share, TimeSpan.FromMilliseconds(100), Exists));
		Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref asked) > 0, TimeSpan.FromSeconds(10)));

		Assert.Equal(1, Volatile.Read(ref asked));
		release.Set();
	}
}
