using FeBuddy.Core.Application.Settings;

namespace FeBuddy.Core.Application.Conversions;

/// <summary>
/// Reads the settings every file conversion shares - the source (a folder or a list of files)
/// and the output - so each conversion's parser only handles what is its own.
/// </summary>
public static class ConversionSettingsReader
{
	/// <summary>
	/// What separates the paths in <c>SourceFiles</c>. A comma cannot be used, as it is legal in
	/// a Windows path; <c>|</c> is not.
	/// </summary>
	public const char SourceFileSeparator = '|';

	/// <summary>
	/// The source keys, which every conversion reads on top of
	/// <see cref="SubServiceSettingsReader.CommonKeys"/>.
	/// </summary>
	public static readonly IReadOnlySet<string> SourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"SourceFolder", "SourceFiles",
	};

	/// <summary>
	/// Reads <c>SourceFolder</c> or <c>SourceFiles</c> - exactly one is required.
	/// </summary>
	/// <param name="settings">The raw settings block.</param>
	/// <returns>The folder, or <see langword="null"/> and the files.</returns>
	/// <exception cref="ArgumentException">Thrown when both or neither are given.</exception>
	public static (string? Folder, IReadOnlyList<string> Files) ReadSource(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		string? folder = SettingsValueReader.OptionalString(settings, "SourceFolder");
		string[] files = SettingsValueReader.OptionalString(settings, "SourceFiles")
			?.Split(SourceFileSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			?? [];

		bool hasFolder = folder is not null;
		bool hasFiles = files.Length > 0;

		if (hasFolder == hasFiles)
		{
			throw new ArgumentException(
				"Give either 'SourceFolder' (convert every matching file in a folder) or 'SourceFiles' " +
				$"(the files to convert, separated by '{SourceFileSeparator}'), but not both.");
		}

		return (folder, files);
	}

	/// <summary>Reads the required <c>OutputDirectory</c>.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <returns>The directory.</returns>
	/// <exception cref="ArgumentException">Thrown when it is missing or blank.</exception>
	public static string ReadOutputDirectory(IReadOnlyDictionary<string, string> settings) =>
		SettingsValueReader.RequiredString(settings, "OutputDirectory");

	/// <summary>Reads <c>AddFeBuddyOutputFolder</c>, default <c>Y</c>.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <returns>Whether to wrap output in a <c>FE-Buddy_Output</c> folder.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not <c>Y</c> or <c>N</c>.</exception>
	public static bool ReadAddFeBuddyOutputFolder(IReadOnlyDictionary<string, string> settings) =>
		SettingsValueReader.YesNo(settings, "AddFeBuddyOutputFolder", defaultValue: true);
}
