using FEBuddyLibrary.Services.General;

namespace FEBuddyLibrary.Helpers;

/// <summary>
/// Owns FE-Buddy's scratch area under the OS temp folder: <c>%TEMP%\FE-Buddy</c>. Downloads
/// land here before their useful contents are extracted into the permanent cache under
/// <c>%APPDATA%\FE-Buddy</c>, and the whole tree is wiped on launch.
/// </summary>
/// <remarks>
/// Everything under <c>%TEMP%\FE-Buddy</c> is disposable - including legacy content left by
/// FE-Buddy v2.x - so <see cref="ClearOnLaunch"/> deletes the entire tree. It is best-effort
/// and never throws: a file locked by another process simply survives to the next launch.
/// </remarks>
public static class TempWorkspace
{
	private const string LogSource = "TempWorkspace";

	private static string? _rootOverride;

	/// <summary>The scratch root: <c>%TEMP%\FE-Buddy</c>.</summary>
	public static string RootDirectory =>
		_rootOverride ?? Path.Combine(Path.GetTempPath(), "FE-Buddy");

	/// <summary>Where cycle archives are downloaded before extraction: <c>%TEMP%\FE-Buddy\Downloads</c>.</summary>
	public static string DownloadsDirectory =>
		Path.Combine(RootDirectory, "Downloads");

	/// <summary>
	/// Ensures <see cref="DownloadsDirectory"/> exists and returns it.
	/// </summary>
	/// <returns>The absolute path of the downloads directory.</returns>
	public static string EnsureDownloadsDirectory()
	{
		Directory.CreateDirectory(DownloadsDirectory);
		return DownloadsDirectory;
	}

	/// <summary>
	/// Recursively empties <see cref="RootDirectory"/>. Best-effort - individual files or
	/// folders that cannot be removed are logged and skipped; the method never throws.
	/// </summary>
	/// <returns>The number of top-level entries that could not be removed.</returns>
	public static int ClearOnLaunch()
	{
		string root = RootDirectory;

		if (!Directory.Exists(root))
		{
			return 0;
		}

		int failures = 0;

		foreach (string directory in SafeEnumerate(() => Directory.EnumerateDirectories(root)))
		{
			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch (Exception ex)
			{
				failures++;
				AppLog.Warning(LogSource, $"Could not delete temp folder '{directory}': {ex.Message}");
			}
		}

		foreach (string file in SafeEnumerate(() => Directory.EnumerateFiles(root)))
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception ex)
			{
				failures++;
				AppLog.Warning(LogSource, $"Could not delete temp file '{file}': {ex.Message}");
			}
		}

		return failures;
	}

	/// <summary>Points the scratch root somewhere else. Unit tests only.</summary>
	/// <param name="root">A throwaway directory, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(string? root) => _rootOverride = root;

	private static IEnumerable<string> SafeEnumerate(Func<IEnumerable<string>> enumerate)
	{
		try
		{
			return enumerate().ToArray();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}
}
