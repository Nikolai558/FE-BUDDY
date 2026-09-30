namespace FeBuddy.UnitTests.Repository;

/// <summary>
/// Checks the repository's own files: every one is UTF-8 without a byte-order mark. Visual Studio
/// can add one when it saves a file (it did to <c>FeBuddy.Wpf.csproj</c>), and CI does not run
/// <c>dotnet format</c>, whose whitespace check catches it only in C# files.
/// </summary>
public sealed class FileEncodingTests
{
	/// <summary>Build output and tool folders: not the repository's own files.</summary>
	private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
	{
		".git", ".vs", "bin", "obj", "TestResults",
	};

	[Fact]
	public void no_file_in_the_repository_starts_with_a_byte_order_mark()
	{
		string root = FindRepositoryRoot();

		string[] withBom = [.. FilesUnder(root)
			.Where(StartsWithByteOrderMark)
			.Select(path => Path.GetRelativePath(root, path))
			.Order(StringComparer.OrdinalIgnoreCase)];

		Assert.True(withBom.Length == 0,
			"These files start with a UTF-8 byte-order mark; save them as UTF-8 without one:\n" +
			string.Join('\n', withBom));
	}

	/// <summary>The folder holding <c>FeBuddy\FeBuddy.sln</c>, found by walking up from the test's own folder.</summary>
	private static string FindRepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "FeBuddy", "FeBuddy.sln")))
			{
				return folder.FullName;
			}
		}

		throw new DirectoryNotFoundException($"No repository root (holding FeBuddy\\FeBuddy.sln) above {AppContext.BaseDirectory}.");
	}

	private static IEnumerable<string> FilesUnder(string folder)
	{
		foreach (string file in Directory.EnumerateFiles(folder))
		{
			yield return file;
		}

		foreach (string child in Directory.EnumerateDirectories(folder))
		{
			if (SkippedFolders.Contains(Path.GetFileName(child)))
			{
				continue;
			}

			foreach (string file in FilesUnder(child))
			{
				yield return file;
			}
		}
	}

	private static bool StartsWithByteOrderMark(string path)
	{
		Span<byte> start = stackalloc byte[3];

		using FileStream stream = File.OpenRead(path);
		return stream.ReadAtLeast(start, 3, throwOnEndOfStream: false) == 3
			&& start[0] == 0xEF && start[1] == 0xBB && start[2] == 0xBF;
	}
}
