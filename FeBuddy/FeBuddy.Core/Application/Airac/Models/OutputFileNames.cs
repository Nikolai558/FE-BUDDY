namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The names the user gave some of a run's output files in place of FE-Buddy's own (the File Names
/// tab), by file key. Every other file keeps FE-Buddy's name.
/// </summary>
/// <remarks>
/// <para>
/// A file is named by its key, as for <see cref="CrcDefaultsFiles"/>: a GeoJSON file's name without
/// <c>.geojson</c> (e.g. <c>Airways_High_Lines</c>), or any other file's whole name (e.g.
/// <c>Airways.txt</c>, <c>Procedure_Changes.md</c>, <c>Combined_Alias.txt</c>). A new name has no
/// extension: the file keeps its own, so <c>Airways_High_Lines</c> renamed <c>ZOB High</c> is written
/// as <c>ZOB High.geojson</c>. Only the name changes. The file stays in its folder, and its key still
/// names it everywhere else (<c>CrcDefaultsFor</c>, say).
/// </para>
/// <para>
/// Departures and Arrivals write a GeoJSON file per procedure, named from the FAA's data, so those
/// files cannot be renamed; their alias files can (see <see cref="OutputFileNamesParser"/>).
/// </para>
/// </remarks>
public sealed class OutputFileNames
{
	/// <summary>The extension of a file whose key has none: every GeoJSON file.</summary>
	public const string GeojsonExtension = ".geojson";

	/// <summary>The longest new name allowed, not counting its extension.</summary>
	public const int MaxNameLength = 100;

	// The extensions a key can end in. Every other key is a GeoJSON file's.
	private static readonly string[] KeyExtensions = [".txt", ".md", ".json"];

	/// <summary>
	/// Every extension an AIRAC Service output file has: <c>.geojson</c>, then <c>.txt</c>, <c>.md</c> and
	/// <c>.json</c> - <c>.geojson</c> before <c>.json</c>, so a name ending in it is taken as that.
	/// </summary>
	public static IReadOnlyList<string> OutputExtensions { get; } = [GeojsonExtension, .. KeyExtensions];

	private static readonly char[] InvalidNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

	// The device names Windows will not give a file, with any extension.
	private static readonly HashSet<string> ReservedNames = new(
		["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(1, 9).SelectMany(n => new[] { $"COM{n}", $"LPT{n}" })],
		StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _newNames;

	/// <summary>Creates the names.</summary>
	/// <param name="newNames">Each renamed file's key and its new name, without an extension. Surrounding spaces are trimmed.</param>
	/// <exception cref="ArgumentException">
	/// Thrown when a new name is not a usable file name (see <see cref="Problem"/>), or two files would
	/// be given the same name.
	/// </exception>
	public OutputFileNames(IReadOnlyDictionary<string, string> newNames)
	{
		ArgumentNullException.ThrowIfNull(newNames);

		_newNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach ((string key, string name) in newNames)
		{
			if (Problem(name) is { } problem)
			{
				throw new ArgumentException($"The new name for {DefaultFileName(key)}, '{name}', can't be used. {problem}", nameof(newNames));
			}

			_newNames[key] = name.Trim();
		}

		// Two files in one folder given one name would be written over each other.
		if (Clashes(_newNames, []).FirstOrDefault() is { Key: { } clashing } clash)
		{
			throw new ArgumentException($"The new name for {DefaultFileName(clashing)}, '{_newNames[clashing]}', can't be used. {clash.Value}", nameof(newNames));
		}
	}

	/// <summary>Every file keeps FE-Buddy's name.</summary>
	public static OutputFileNames None { get; } = new(new Dictionary<string, string>());

	/// <summary>
	/// The folder each sub-service's own alias file goes in while the run doesn't export it
	/// (<see cref="AiracServiceSettings.ExportIndividualAliasFiles"/>): the run's folder in FE-Buddy's scratch
	/// area. <see langword="null"/> when they are exported, to the cycle's <c>Aliases</c> folder
	/// (<see cref="AiracOutputPaths.AliasFilePath"/>).
	/// </summary>
	public string? UnexportedAliasDirectory { get; private init; }

	/// <summary>
	/// These names for a run that doesn't export the sub-services' own alias files
	/// (<see cref="AiracOutputPaths.SubServiceAliasFileKeys"/>): those go in <paramref name="directory"/> under
	/// FE-Buddy's names, so <c>Combined_Alias.txt</c> heads each section with it. Every other file keeps the name
	/// given it here, <c>Combined_Alias.txt</c> included.
	/// </summary>
	/// <param name="directory">The run's folder in FE-Buddy's scratch area.</param>
	/// <returns>The names to run with. The renames left out stay saved, for a run that exports the files.</returns>
	public OutputFileNames WithUnexportedAliasFilesIn(string directory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);

		Dictionary<string, string> kept = _newNames
			.Where(entry => !AiracOutputPaths.SubServiceAliasFileKeys.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
			.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

		return new OutputFileNames(kept) { UnexportedAliasDirectory = directory };
	}

	/// <summary>The name a file is written under: its new name and its own extension, or FE-Buddy's name for it.</summary>
	/// <param name="fileKey">The file's key, e.g. <c>Airways_High_Lines</c> or <c>Airways.txt</c>.</param>
	/// <returns>e.g. <c>ZOB High.geojson</c>, or <c>Airways_High_Lines.geojson</c> when it was not renamed.</returns>
	public string FileName(string fileKey)
	{
		ArgumentNullException.ThrowIfNull(fileKey);

		return _newNames.TryGetValue(fileKey, out string? name) ? name + ExtensionOf(fileKey) : DefaultFileName(fileKey);
	}

	/// <summary>The extension a file keeps whatever it is named.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns>The key's own extension (<c>.txt</c>, <c>.md</c> or <c>.json</c>), otherwise <c>.geojson</c>.</returns>
	public static string ExtensionOf(string fileKey)
	{
		ArgumentNullException.ThrowIfNull(fileKey);

		return KeyExtensions.FirstOrDefault(extension => fileKey.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) ?? GeojsonExtension;
	}

	/// <summary>FE-Buddy's name for a file.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns>The key itself for a file whose key has its extension, otherwise the key and <c>.geojson</c>.</returns>
	public static string DefaultFileName(string fileKey)
	{
		ArgumentNullException.ThrowIfNull(fileKey);

		return ExtensionOf(fileKey) == GeojsonExtension ? fileKey + GeojsonExtension : fileKey;
	}

	/// <summary>
	/// The folder a file is written in, inside the cycle folder: every GeoJSON file in <c>Geojson</c>, the
	/// alias files in <c>Aliases</c>, the Procedures documents in <c>Publication_Docs</c>, and the duplicate
	/// alias report in the cycle folder itself. Two files clash only in one folder.
	/// </summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns>The folder, relative to the cycle folder; empty for the cycle folder.</returns>
	public static string FolderOf(string fileKey)
	{
		ArgumentNullException.ThrowIfNull(fileKey);

		if (fileKey.Equals(AiracOutputPaths.DuplicateAliasReportFileName, StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}

		return ExtensionOf(fileKey) switch
		{
			GeojsonExtension => AiracOutputPaths.GeojsonFolder,
			".txt" => AiracOutputPaths.AliasFolder,
			_ => AiracOutputPaths.PublicationDocsFolder,
		};
	}

	/// <summary>
	/// The new names that would write one file over another, each with why: a name another renamed file in the
	/// same folder is given too, or the name of a file there that keeps FE-Buddy's. The File Names tab shows
	/// these as the names are typed, and a run refuses them.
	/// </summary>
	/// <param name="newNames">Each renamed file's key and its new name, without an extension.</param>
	/// <param name="otherKeys">Every other file written beside them, which keeps FE-Buddy's name unless it is renamed too.</param>
	/// <returns>Why each clashing file's new name can't be used, by its key; empty when none clash.</returns>
	public static IReadOnlyDictionary<string, string> Clashes(IReadOnlyDictionary<string, string> newNames, IEnumerable<string> otherKeys)
	{
		ArgumentNullException.ThrowIfNull(newNames);
		ArgumentNullException.ThrowIfNull(otherKeys);

		Dictionary<string, string> written = newNames.ToDictionary(
			entry => entry.Key, entry => entry.Value.Trim() + ExtensionOf(entry.Key), StringComparer.OrdinalIgnoreCase);
		string[] kept = [.. otherKeys.Where(key => !written.ContainsKey(key)).Distinct(StringComparer.OrdinalIgnoreCase)];
		Dictionary<string, string> problems = new(StringComparer.OrdinalIgnoreCase);

		foreach ((string key, string name) in written)
		{
			string folder = FolderOf(key);

			if (written.FirstOrDefault(other => !other.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
				&& FolderOf(other.Key).Equals(folder, StringComparison.OrdinalIgnoreCase)
				&& other.Value.Equals(name, StringComparison.OrdinalIgnoreCase)) is { Key: { } renamed } other)
			{
				problems[key] = $"{DefaultFileName(renamed)} is being renamed {other.Value} too. Give each file its own name.";
			}
			else if (kept.FirstOrDefault(other => FolderOf(other).Equals(folder, StringComparison.OrdinalIgnoreCase)
				&& DefaultFileName(other).Equals(name, StringComparison.OrdinalIgnoreCase)) is { } keeping)
			{
				problems[key] = $"{DefaultFileName(keeping)} is already the name of another file. Give this file a name of its own.";
			}
		}

		return problems;
	}

	/// <summary>
	/// Why a new name can't be used, or <see langword="null"/> when it can. Surrounding spaces are
	/// ignored: the name is trimmed before it is used. The File Names tab applies the same rules.
	/// </summary>
	/// <param name="name">The new name, without an extension.</param>
	/// <returns>A sentence saying what is wrong, or <see langword="null"/>.</returns>
	public static string? Problem(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		string trimmed = name.Trim();

		if (trimmed.Length == 0)
		{
			return "Type a new name.";
		}

		if (trimmed.IndexOfAny(InvalidNameChars) >= 0 || trimmed.Any(char.IsControl))
		{
			return @"A file name can't contain \ / : * ? "" < > |.";
		}

		if (trimmed.EndsWith('.'))
		{
			return "A file name can't end with a dot.";
		}

		if (OutputExtensions.FirstOrDefault(extension => trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) is { } typed)
		{
			return $"Leave off {typed}: FE-Buddy adds the extension itself.";
		}

		string device = trimmed.Split('.')[0].TrimEnd();

		if (ReservedNames.Contains(device))
		{
			return $"Windows keeps {device.ToUpperInvariant()} as a device name, so no file can have it. Choose another name.";
		}

		return trimmed.Length > MaxNameLength
			? $"Keep it to {MaxNameLength} characters or fewer."
			: null;
	}
}
