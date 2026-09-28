namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The names the user gave some of a run's output files in place of FE-Buddy's own (the File Names
/// tab), by file key. Every other file keeps FE-Buddy's name.
/// </summary>
/// <remarks>
/// <para>
/// A file is named by its key, as for <see cref="VnasFileChoices"/>: a GeoJSON file's name without
/// <c>.geojson</c> (e.g. <c>Airways_High_Lines</c>), or any other file's whole name (e.g.
/// <c>Airways.txt</c>, <c>Procedure_Changes.md</c>, <c>vNAS_Alias.txt</c>). A new name has no
/// extension: the file keeps its own, so <c>Airways_High_Lines</c> renamed <c>ZOB High</c> is written
/// as <c>ZOB High.geojson</c>. Only the name changes. The file stays in its folder, and its key still
/// names it everywhere else (<c>UploadToVnas</c>, <c>CrcDefaultsFor</c>).
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

	// Every extension an output file has, .geojson before .json so a name ending in .geojson is
	// reported as that.
	private static readonly string[] OutputExtensions = [GeojsonExtension, .. KeyExtensions];

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

		// Two files given one name would be written over each other.
		if (_newNames.Keys.GroupBy(FileName, StringComparer.OrdinalIgnoreCase).FirstOrDefault(names => names.Count() > 1) is { } clash)
		{
			throw new ArgumentException(
				$"{string.Join(" and ", clash.Select(DefaultFileName))} would both be named {clash.Key}. Give each file its own name.",
				nameof(newNames));
		}
	}

	/// <summary>Every file keeps FE-Buddy's name.</summary>
	public static OutputFileNames None { get; } = new(new Dictionary<string, string>());

	/// <summary>Each renamed file's key and its new name, without an extension.</summary>
	public IReadOnlyDictionary<string, string> NewNames => _newNames;

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
