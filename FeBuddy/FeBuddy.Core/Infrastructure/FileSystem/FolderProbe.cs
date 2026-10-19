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

	/// <summary>Whether <paramref name="path"/> is a folder that answers within <paramref name="timeout"/>.</summary>
	/// <param name="path">The folder, or <see langword="null"/>.</param>
	/// <param name="timeout">How long to wait for it.</param>
	/// <returns><see langword="true"/> when it is there and answered in time.</returns>
	public static bool Exists(string? path, TimeSpan timeout) => Exists(path, timeout, Directory.Exists);

	/// <summary>Same as <see cref="Exists(string?, TimeSpan)"/>, with the check supplied. Unit tests stand in for a slow share.</summary>
	internal static bool Exists(string? path, TimeSpan timeout, Func<string, bool> exists)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		// Off the caller's thread, so a share that doesn't answer is left to time out on its own.
		Task<bool> check = Task.Run(() => exists(path));
		return check.Wait(timeout) && check.Result;
	}
}
