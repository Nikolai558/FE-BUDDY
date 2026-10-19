using FeBuddy.Core.Application.Conversions.Models;

namespace FeBuddy.Core.Application.Conversions;

/// <summary>
/// Counts the files a conversion would read in its source folder, for the page to say how many there
/// are before a run. Touches the disk - a folder on a network share or OneDrive can take seconds to
/// answer, and a page may open every candidate file - so the GUI calls it off the UI thread.
/// </summary>
public static class ConversionSourceFolder
{
	/// <summary>Counts the files in <paramref name="folder"/>, not its subfolders, that the conversion reads.</summary>
	/// <param name="folder">The folder.</param>
	/// <param name="extensions">The extensions a file must have, e.g. <c>.dat</c>, ignoring case.</param>
	/// <param name="isSourceFile">Whether a file with one of them is one the conversion reads (ERAM opens each to look).</param>
	/// <param name="cancellationToken">Stops the count; a newer one has started.</param>
	/// <returns>Whether the folder is there, how many files it has, or why it couldn't be read.</returns>
	/// <exception cref="OperationCanceledException">The count was stopped.</exception>
	public static SourceFolderCount Count(
		string folder,
		IReadOnlyCollection<string> extensions,
		Func<string, bool> isSourceFile,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(folder);
		ArgumentNullException.ThrowIfNull(extensions);
		ArgumentNullException.ThrowIfNull(isSourceFile);

		string path = folder.Trim();

		if (path.Length == 0 || !Directory.Exists(path))
		{
			return SourceFolderCount.Missing;
		}

		try
		{
			int files = 0;

			foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (ConversionFiles.HasExtension(file, extensions) && isSourceFile(file))
				{
					files++;
				}
			}

			return new SourceFolderCount(true, files, null);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return new SourceFolderCount(true, 0, ex.Message);
		}
	}
}
