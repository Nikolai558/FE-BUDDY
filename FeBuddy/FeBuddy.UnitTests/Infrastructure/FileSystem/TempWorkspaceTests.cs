using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Infrastructure.FileSystem;

/// <summary>
/// Covers <see cref="TempWorkspace"/>: clearing the scratch folder on launch.
/// </summary>
[Collection("AppLog")]
public sealed class TempWorkspaceTests : IDisposable
{
	private readonly string _tempRoot =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_TempWs_" + Guid.NewGuid().ToString("N"));

	/// <summary>Redirects the workspace and the log to a throwaway folder.</summary>
	public TempWorkspaceTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_tempRoot, "logs"));
		TempWorkspace.ConfigureForTesting(_tempRoot);
	}

	/// <summary>Restores defaults and cleans up.</summary>
	public void Dispose()
	{
		TempWorkspace.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			if (Directory.Exists(_tempRoot))
			{
				Directory.Delete(_tempRoot, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	/// <summary><see cref="TempWorkspace.ClearOnLaunch"/> empties the tree and does not throw on a missing root.</summary>
	[Fact]
	public void temp_workspace_clear_on_launch_empties_tree()
	{
		Assert.Equal(0, TempWorkspace.ClearOnLaunch()); // root does not exist yet

		Directory.CreateDirectory(Path.Combine(_tempRoot, "Downloads", "nested"));
		File.WriteAllText(Path.Combine(_tempRoot, "a.zip"), "x");
		File.WriteAllText(Path.Combine(_tempRoot, "Downloads", "b.csv"), "y");

		int failures = TempWorkspace.ClearOnLaunch();

		Assert.Equal(0, failures);
		Assert.True(Directory.Exists(_tempRoot));
		Assert.Empty(Directory.EnumerateFileSystemEntries(_tempRoot));
	}

	/// <summary>An entry that is still in use is counted and logged, and the rest are still cleared.</summary>
	[Fact]
	public void temp_workspace_clear_on_launch_counts_what_it_cannot_delete()
	{
		Directory.CreateDirectory(Path.Combine(_tempRoot, "busy"));
		Directory.CreateDirectory(Path.Combine(_tempRoot, "idle"));
		File.WriteAllText(Path.Combine(_tempRoot, "idle.txt"), "x");

		// On Windows an open handle without FileShare.Delete blocks deleting the file, and so its folder.
		using FileStream busyFile = new(Path.Combine(_tempRoot, "busy.txt"), FileMode.Create, FileAccess.Write, FileShare.None);
		using FileStream busyNested = new(Path.Combine(_tempRoot, "busy", "nested.txt"), FileMode.Create, FileAccess.Write, FileShare.None);

		int failures = TempWorkspace.ClearOnLaunch();

		Assert.Equal(2, failures);
		Assert.False(Directory.Exists(Path.Combine(_tempRoot, "idle")));
		Assert.False(File.Exists(Path.Combine(_tempRoot, "idle.txt")));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not delete temp folder", StringComparison.Ordinal));
		Assert.Contains(AppLog.Entries, e => e.Message.StartsWith("Could not delete temp file", StringComparison.Ordinal));
	}

	/// <summary>Without an override the workspace lives in the system temp folder.</summary>
	[Fact]
	public void temp_workspace_default_root_is_under_the_system_temp_folder()
	{
		TempWorkspace.ConfigureForTesting(null);

		Assert.Equal(Path.Combine(Path.GetTempPath(), "FE-Buddy"), TempWorkspace.RootDirectory);
	}
}
