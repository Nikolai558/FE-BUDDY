using FeBuddy.Core.Application.Conversions;
using FeBuddy.Core.Application.Conversions.Models;

namespace FeBuddy.UnitTests.Application.Conversions;

/// <summary>Tests for <see cref="ConversionSourceFolder"/>: counting a conversion's source files off the UI thread.</summary>
public sealed class ConversionSourceFolderTests : IDisposable
{
	private static readonly string[] Dat = [".dat"];

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_SourceFolder_" + Guid.NewGuid().ToString("N"));

	public ConversionSourceFolderTests() => Directory.CreateDirectory(_root);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	/// <summary>No path, or one that isn't there, is a missing folder.</summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("Missing")]
	public void a_blank_or_missing_folder_is_missing(string folder)
	{
		string path = folder == "Missing" ? Path.Combine(_root, folder) : folder;

		SourceFolderCount count = ConversionSourceFolder.Count(path, Dat, _ => true);

		Assert.Same(SourceFolderCount.Missing, count);
		Assert.False(count.Counted);
	}

	/// <summary>Only files in the folder itself, with a matching extension in any case, that the conversion reads, are counted.</summary>
	[Fact]
	public void only_the_files_the_conversion_reads_are_counted()
	{
		File.WriteAllText(Path.Combine(_root, "A.dat"), "a");
		File.WriteAllText(Path.Combine(_root, "B.DAT"), "b");
		File.WriteAllText(Path.Combine(_root, "Skip.dat"), "skip");
		File.WriteAllText(Path.Combine(_root, "Notes.txt"), "notes");
		Directory.CreateDirectory(Path.Combine(_root, "Sub"));
		File.WriteAllText(Path.Combine(_root, "Sub", "C.dat"), "c");

		SourceFolderCount count = ConversionSourceFolder.Count($"  {_root}  ", Dat, path => !path.EndsWith("Skip.dat", StringComparison.Ordinal));

		Assert.Equal(new SourceFolderCount(true, 2, null), count);
		Assert.True(count.Counted);
	}

	/// <summary>A folder that can't be read says why, rather than looking missing or empty.</summary>
	[Fact]
	public void a_folder_that_cannot_be_read_says_why()
	{
		File.WriteAllText(Path.Combine(_root, "A.dat"), "a");

		SourceFolderCount count = ConversionSourceFolder.Count(_root, Dat, _ => throw new UnauthorizedAccessException("Access denied."));

		Assert.Equal(new SourceFolderCount(true, 0, "Access denied."), count);
		Assert.False(count.Counted);
	}

	/// <summary>A count a newer one stopped throws, so its answer is never shown.</summary>
	[Fact]
	public void a_stopped_count_throws()
	{
		File.WriteAllText(Path.Combine(_root, "A.dat"), "a");
		using CancellationTokenSource stop = new();
		stop.Cancel();

		Assert.Throws<OperationCanceledException>(() => ConversionSourceFolder.Count(_root, Dat, _ => true, stop.Token));
	}

	/// <summary>Missing arguments throw.</summary>
	[Fact]
	public void missing_arguments_throw()
	{
		Assert.Throws<ArgumentNullException>(() => ConversionSourceFolder.Count(null!, Dat, _ => true));
		Assert.Throws<ArgumentNullException>(() => ConversionSourceFolder.Count(_root, null!, _ => true));
		Assert.Throws<ArgumentNullException>(() => ConversionSourceFolder.Count(_root, Dat, null!));
	}
}
