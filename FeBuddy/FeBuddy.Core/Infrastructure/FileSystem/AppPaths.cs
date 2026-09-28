namespace FeBuddy.Core.Infrastructure.FileSystem;

/// <summary>
/// The two folders FE-Buddy keeps on the user's machine. Everything FE-Buddy stores lives under
/// one of them, so this is the one place their locations are spelled out.
/// </summary>
public static class AppPaths
{
	private const string FolderName = "FE-Buddy";

	/// <summary>
	/// <c>%APPDATA%\FE-Buddy</c>: settings, logs and cached AIRAC cycles - everything kept between runs.
	/// </summary>
	public static string AppDataDirectory =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName);

	/// <summary>
	/// <c>%TEMP%\FE-Buddy</c>: scratch space for downloads, wiped on every launch (see <see cref="TempWorkspace"/>).
	/// </summary>
	public static string TempDirectory =>
		Path.Combine(Path.GetTempPath(), FolderName);
}
