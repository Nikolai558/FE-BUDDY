namespace FeBuddy.Core.Infrastructure.FileSystem;

/// <summary>
/// Whether a folder is there, answered within a time limit - for the GUI, which asks on the UI thread
/// right before a picker opens. A folder on a network share or mapped drive that is offline can take the
/// SMB timeout (often 20 seconds or more) to answer; past the limit it counts as not there.
/// </summary>
public static class FolderProbe
{
	/// <summary>How long a folder has to answer before it counts as not there.</summary>
	public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(300);

	private static readonly Lock Gate = new();

	// The folders Windows is still being asked about, by path: a share that doesn't answer holds its thread
	// until it times out, so a folder asked about again while one is waiting shares its answer.
	private static readonly Dictionary<string, Task<bool>> Asking = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Whether <paramref name="path"/> is a folder that answers within <paramref name="timeout"/>.</summary>
	/// <param name="path">The folder, or <see langword="null"/>.</param>
	/// <param name="timeout">How long to wait for it.</param>
	/// <returns><see langword="true"/> when it is there and answered in time.</returns>
	public static bool Exists(string? path, TimeSpan timeout) => Exists(path, timeout, Directory.Exists);

	/// <summary>
	/// Same as <see cref="Exists(string?, TimeSpan)"/>, with the check supplied. Unit tests stand in for a slow
	/// share. A folder still being asked about isn't asked again: the call waits for the same answer.
	/// </summary>
	internal static bool Exists(string? path, TimeSpan timeout, Func<string, bool> exists)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		Task<bool> check;
		bool started = false;

		lock (Gate)
		{
			if (!Asking.TryGetValue(path, out check!))
			{
				// On a thread of its own, not the pool's, so a share that doesn't answer is left to time out on its own.
				check = Task.Factory.StartNew(() => exists(path), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
				Asking[path] = check;
				started = true;
			}
		}

		if (started)
		{
			check.ContinueWith(_ => Forget(path, check), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		return check.Wait(timeout) && check.Result;
	}

	/// <summary>Stops sharing a question about a folder once Windows has answered it.</summary>
	private static void Forget(string path, Task<bool> answered)
	{
		lock (Gate)
		{
			if (Asking.TryGetValue(path, out Task<bool>? asking) && asking == answered)
			{
				Asking.Remove(path);
			}
		}
	}
}
