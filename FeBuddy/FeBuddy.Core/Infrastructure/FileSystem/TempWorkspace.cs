using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.FileSystem;

/// <summary>
/// Owns FE-Buddy's scratch area under the OS temp folder: <c>%TEMP%\FE-Buddy</c>. Downloads
/// land here before their useful contents are extracted into the permanent cache under
/// <c>%APPDATA%\FE-Buddy</c>, and the whole tree is wiped on launch.
/// </summary>
/// <remarks>
/// Everything under <c>%TEMP%\FE-Buddy</c> is disposable - including legacy content left by
/// FE-Buddy v2.x - so <see cref="ClearOnLaunch"/> deletes the entire tree, along with the crash
/// report 3.0.0 alphas left beside it (<see cref="LegacyCrashReportPath"/>). It is best-effort
/// and never throws: a file locked by another process simply survives to the next launch.
/// </remarks>
public static class TempWorkspace
{
	private const string LogSource = "TempWorkspace";

	private static string? _rootOverride;

	/// <summary>The scratch root: <c>%TEMP%\FE-Buddy</c>.</summary>
	public static string RootDirectory =>
		_rootOverride ?? AppPaths.TempDirectory;

	/// <summary>Where cycle archives are downloaded before extraction: <c>%TEMP%\FE-Buddy\Downloads</c>.</summary>
	public static string DownloadsDirectory =>
		Path.Combine(RootDirectory, "Downloads");

	/// <summary>
	/// Where 3.0.0 alphas wrote their crash report: <c>%TEMP%\febuddy-wpf-crash.txt</c>, beside
	/// <see cref="RootDirectory"/> where neither this class nor the uninstaller cleaned it up. The
	/// report now goes to the log folder.
	/// </summary>
	internal static string LegacyCrashReportPath =>
		Path.Combine(Path.GetDirectoryName(RootDirectory)!, "febuddy-wpf-crash.txt");

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
	/// Recursively empties <see cref="RootDirectory"/> and deletes <see cref="LegacyCrashReportPath"/>.
	/// Best-effort - individual files or folders that cannot be removed are logged and skipped; the
	/// method never throws.
	/// </summary>
	/// <returns>The number of top-level entries that could not be removed.</returns>
	public static int ClearOnLaunch()
	{
		string root = RootDirectory;
		int failures = 0;

		string legacyCrashReport = LegacyCrashReportPath;
		if (File.Exists(legacyCrashReport))
		{
			try
			{
				File.Delete(legacyCrashReport);
			}
			catch (Exception ex)
			{
				failures++;
				AppLog.Warning(LogSource, $"Could not delete the old crash report '{legacyCrashReport}': {ex.Message}");
			}
		}

		if (!Directory.Exists(root))
		{
			return failures;
		}

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
			return [.. enumerate()];
		}
		catch
		{
			return [];
		}
	}
}
