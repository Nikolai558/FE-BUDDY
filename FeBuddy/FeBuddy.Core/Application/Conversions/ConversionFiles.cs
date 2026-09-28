using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Conversions;

/// <summary>
/// The file handling every conversion shares: finding the source files, and converting them one
/// at a time so a file that cannot be read never stops the rest.
/// </summary>
internal static class ConversionFiles
{
	/// <summary>
	/// The files to convert: those named, or every file in the folder (not its sub-folders) with
	/// one of <paramref name="extensions"/>, by name.
	/// </summary>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="extensions">The extensions a folder's files must have, e.g. <c>.dat</c>; matched ignoring case.</param>
	/// <param name="logSource">The source for the message when the folder has nothing to convert.</param>
	/// <param name="messages">Where that message goes.</param>
	/// <param name="isSource">
	/// Optionally, a closer look at a folder's files with the right extension: only those it accepts
	/// are converted, and the rest are named in one message. Files picked one by one are never
	/// filtered - picking a file says it is meant.
	/// </param>
	/// <returns>The source files, in the order they will be converted.</returns>
	/// <exception cref="ArgumentException">Thrown when the source folder does not exist.</exception>
	public static IReadOnlyList<string> Resolve(
		ConversionSettings settings,
		IReadOnlyList<string> extensions,
		string logSource,
		List<ServiceMessage> messages,
		Func<string, bool>? isSource = null)
	{
		if (settings.SourceFolder is not { } folder)
		{
			return settings.SourceFiles;
		}

		if (!Directory.Exists(folder))
		{
			throw new ArgumentException($"'SourceFolder' '{folder}' does not exist.");
		}

		// Filtered here rather than by a search pattern, whose Windows wildcard rules differ
		// from what a user expects of an extension.
		string[] found = [.. Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
			.Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
			.Order(StringComparer.OrdinalIgnoreCase)];

		if (isSource is not null && found.Length > 0)
		{
			string[] others = [.. found.Where(path => !isSource(path))];

			if (others.Length > 0)
			{
				found = [.. found.Except(others)];
				messages.Add(new ServiceMessage(LogLevel.Info, logSource,
					$"{others.Length:N0} other file(s) in {folder} are not what this conversion reads and were left alone: " +
					string.Join(", ", others.Select(Path.GetFileName))));
			}
		}

		if (found.Length == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, logSource,
				$"There are no {string.Join(" or ", extensions)} files in {folder}{(isSource is null ? string.Empty : " that this conversion reads")}, so nothing was converted.")
			{
				IsAdvisory = true
			});
		}

		return found;
	}

	/// <summary>A name made safe for a file or folder name: characters Windows forbids become <c>-</c>.</summary>
	/// <param name="name">The name.</param>
	/// <returns>The file name, or <c>Unnamed</c> for a blank one.</returns>
	public static string SafeFileName(string name)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		string safe = new([.. name.Trim().Select(c => invalid.Contains(c) ? '-' : c)]);
		return safe.Length == 0 ? "Unnamed" : safe;
	}

	/// <summary>
	/// A file-safe name not yet in <paramref name="used"/>: the name itself, or the name with
	/// <c> (2)</c>, <c> (3)</c>… when it is taken. The name returned is added to <paramref name="used"/>.
	/// </summary>
	/// <param name="name">The name.</param>
	/// <param name="used">The names already given out in this folder; compare them ignoring case.</param>
	/// <returns>The unique, file-safe name.</returns>
	public static string UniqueFileName(string name, ISet<string> used)
	{
		string safe = SafeFileName(name);
		string unique = safe;

		for (int copy = 2; !used.Add(unique); copy++)
		{
			unique = $"{safe} ({copy})";
		}

		return unique;
	}

	/// <summary>
	/// Converts each source file in turn, reporting progress as each starts and finishes. A file
	/// that cannot be read or written - or whose content is not the format expected
	/// (<see cref="InvalidDataException"/>) - becomes an error message and a failed result; the
	/// rest still convert.
	/// </summary>
	/// <typeparam name="TFile">The conversion's per-file result.</typeparam>
	/// <param name="sources">The files to convert.</param>
	/// <param name="convert">Converts one file.</param>
	/// <param name="fail">Builds the failed result for a file, from its path and the error.</param>
	/// <param name="describe">One line on how a file went, for the run feed.</param>
	/// <param name="progress">Told as each file starts and finishes; optional.</param>
	/// <param name="logSource">The source for the error messages.</param>
	/// <param name="messages">Where the error messages go.</param>
	/// <returns>One result per file, in order.</returns>
	public static IReadOnlyList<TFile> ConvertEach<TFile>(
		IReadOnlyList<string> sources,
		Func<string, TFile> convert,
		Func<string, string, TFile> fail,
		Func<TFile, string> describe,
		IProgress<ConversionProgress>? progress,
		string logSource,
		List<ServiceMessage> messages)
	{
		List<TFile> results = [];

		foreach (string source in sources)
		{
			string name = Path.GetFileName(source);
			progress?.Report(new ConversionProgress(name, "Converting…", IsComplete: false));

			TFile result;

			try
			{
				result = convert(source);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
			{
				string error = $"{name} could not be converted: {ex.Message}";
				messages.Add(new ServiceMessage(LogLevel.Error, logSource, error));
				result = fail(source, error);
			}

			results.Add(result);
			progress?.Report(new ConversionProgress(name, describe(result), IsComplete: true));
		}

		return results;
	}
}
