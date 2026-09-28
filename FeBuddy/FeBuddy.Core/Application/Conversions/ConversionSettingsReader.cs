using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Conversions;

/// <summary>
/// Reads the settings every file conversion shares - the source (a folder or a list of files),
/// the output and which kinds get CRC-ERAM defaults - so each conversion's parser only handles
/// what is its own.
/// </summary>
public static class ConversionSettingsReader
{
	/// <summary>
	/// What separates the paths in <c>SourceFiles</c>. A comma cannot be used, as it is legal in
	/// a Windows path; <c>|</c> is not.
	/// </summary>
	public const char SourceFileSeparator = '|';

	/// <summary>Setting that asks for the Line defaults to be written (<c>Y</c>/<c>N</c>, default <c>N</c>).</summary>
	public const string IncludeLineKey = "IncludeCrcLineDefaults";

	/// <summary>Setting that asks for the Symbol defaults to be written (<c>Y</c>/<c>N</c>, default <c>N</c>).</summary>
	public const string IncludeSymbolKey = "IncludeCrcSymbolDefaults";

	/// <summary>Setting that asks for the Text defaults to be written (<c>Y</c>/<c>N</c>, default <c>N</c>).</summary>
	public const string IncludeTextKey = "IncludeCrcTextDefaults";

	/// <summary>
	/// The keys every conversion reads on top of <see cref="SubServiceSettingsReader.CommonKeys"/>:
	/// the source, the <c>FE-Buddy_Output</c> folder and the CRC-ERAM defaults Include flags.
	/// </summary>
	public static readonly IReadOnlySet<string> ConversionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"SourceFolder", "SourceFiles", "AddFeBuddyOutputFolder",
		IncludeLineKey, IncludeSymbolKey, IncludeTextKey,
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

	/// <summary>
	/// Reads whether the user asked for <paramref name="kind"/>'s defaults to be written.
	/// </summary>
	/// <remarks>
	/// Each kind is chosen on its own (the Include box on its CRC ERAM Defaults panel). The caller
	/// still ANDs this with "is that file being produced": asking for defaults on a file that is
	/// never written means nothing, and its values are then not required.
	/// </remarks>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="kind">Which kind's include flag to read.</param>
	/// <returns><see langword="true"/> when the flag is <c>Y</c>; <see langword="false"/> when <c>N</c> or absent.</returns>
	/// <exception cref="ArgumentException">Thrown when the flag is present but not <c>Y</c>/<c>N</c>.</exception>
	public static bool ReadCrcInclude(IReadOnlyDictionary<string, string> settings, CrcFeatureKind kind)
	{
		string key = kind switch
		{
			CrcFeatureKind.Line => IncludeLineKey,
			CrcFeatureKind.Symbol => IncludeSymbolKey,
			CrcFeatureKind.Text => IncludeTextKey,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown CRC feature kind.")
		};

		return SettingsValueReader.YesNo(settings, key, defaultValue: false);
	}
}
