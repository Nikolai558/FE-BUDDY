using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Platform;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Exports <c>UserConfig</c> to a settings file that another FE-Buddy user can import, and imports
/// such a file, keeping everything that belongs to the importing PC.
/// </summary>
/// <remarks>
/// <para>
/// The file is the same nested JSON as <c>UserConfig.json</c>, wrapped in a small header:
/// <code>
/// { "format": "FE-Buddy.UserConfig", "formatVersion": 1, "appVersion": "3.0.0",
///   "exportedUtc": "2026-09-27T12:00:00Z", "settings": { "General": { ... }, "Services": { ... } } }
/// </code>
/// Each setting goes by its <see cref="UserConfigPortability.Classify(string)"/> scope: shared
/// settings travel as they are, folders travel tokenized (<see cref="PortablePathTokens"/>), and
/// PC-only state and credentials never go into the file.
/// </para>
/// <para>
/// An import makes this PC's settings match the file's exactly: settings the file leaves out go
/// back to their defaults, folders included. Only two things are kept: this PC's own state (the
/// update channel and the like), which is never touched; and this PC's folder wherever the file's
/// folder cannot work here (<see cref="Plan(UserConfigPackage)"/> says which). Folders are made
/// this user's (<see cref="PortablePathTokens.Localize(string)"/>), so the other user's name is
/// never imported. A plain <c>UserConfig.json</c> copied from another PC imports the same way.
/// </para>
/// <para>
/// Import is two steps so the user sees what will happen first: <see cref="Plan(UserConfigPackage)"/>
/// works it out without writing anything, and <see cref="Apply(UserConfigImportPlan)"/> writes it.
/// </para>
/// </remarks>
public static class UserConfigTransfer
{
	/// <summary>The <c>format</c> every export is stamped with.</summary>
	public const string FormatId = "FE-Buddy.UserConfig";

	/// <summary>The export format this version writes, and the newest it can read.</summary>
	public const int FormatVersion = 1;

	/// <summary>Anything larger is not a settings file.</summary>
	internal const long MaxFileBytes = 2 * 1024 * 1024;

	private const string LogSource = "UserConfig";

	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

	/// <summary>Writes every saved setting that can leave this PC to <paramref name="path"/>.</summary>
	/// <param name="path">The file to write. An existing file is replaced.</param>
	/// <returns>What was written.</returns>
	public static UserConfigExportResult Export(string path) =>
		Export(path, UserConfigFile.SnapshotValues(), AppVersion.Current, DateTimeOffset.UtcNow, PortablePathTokens.ForCurrentUser());

	/// <summary>Writes a settings file from <paramref name="values"/>. The public overload passes this PC's.</summary>
	/// <param name="path">The file to write.</param>
	/// <param name="values">The settings, by dotted path.</param>
	/// <param name="appVersion">The FE-Buddy version to stamp the file with.</param>
	/// <param name="exportedUtc">When the export happened.</param>
	/// <param name="tokens">How to tokenize folders.</param>
	/// <returns>What was written.</returns>
	internal static UserConfigExportResult Export(
		string path,
		IReadOnlyDictionary<string, string> values,
		string appVersion,
		DateTimeOffset exportedUtc,
		PortablePathTokens tokens)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		Dictionary<string, string> exported = new(StringComparer.Ordinal);
		int folders = 0;
		int leftOut = 0;

		foreach (KeyValuePair<string, string> entry in values.OrderBy(e => e.Key, StringComparer.Ordinal))
		{
			switch (UserConfigPortability.Classify(entry.Key))
			{
				case ConfigKeyScope.Shared:
					exported[entry.Key] = entry.Value;
					break;

				case ConfigKeyScope.MachinePath when !string.IsNullOrWhiteSpace(entry.Value):
					exported[entry.Key] = tokens.Tokenize(entry.Value);
					folders++;
					break;

				case ConfigKeyScope.MachinePath:
					// No folder chosen: nothing to share, and the importing PC keeps its own.
					break;

				default:
					leftOut++;
					break;
			}
		}

		JsonObject root = new()
		{
			["format"] = FormatId,
			["formatVersion"] = FormatVersion,
			["appVersion"] = appVersion,
			["exportedUtc"] = exportedUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
			["settings"] = UserConfigFile.BuildTree(exported),
		};

		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
		File.WriteAllText(path, root.ToJsonString(WriteOptions));

		AppLog.Info(LogSource, $"Exported {exported.Count} settings to '{path}' ({folders} folders, {leftOut} PC-only settings left out).");

		return new UserConfigExportResult(path, exported.Count, folders, leftOut);
	}

	/// <summary>Reads a settings file: an FE-Buddy export, or a plain <c>UserConfig.json</c>.</summary>
	/// <param name="path">The file to read.</param>
	/// <returns>The file's settings, not yet imported.</returns>
	/// <exception cref="UserConfigTransferException">The file is missing, unreadable, or not a settings file this version can import.</exception>
	public static UserConfigPackage Read(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		FileInfo file = new(path);

		if (!file.Exists)
		{
			throw new UserConfigTransferException($"'{file.Name}' could not be found.");
		}

		if (file.Length > MaxFileBytes)
		{
			throw new UserConfigTransferException($"'{file.Name}' is too large to be an FE-Buddy settings file.");
		}

		string json;

		try
		{
			json = File.ReadAllText(file.FullName);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			throw new UserConfigTransferException($"'{file.Name}' could not be read: {ex.Message}", ex);
		}

		return Parse(json, file.Name);
	}

	/// <summary>Parses a settings file's text.</summary>
	/// <param name="json">The file's contents.</param>
	/// <param name="fileName">The file's name, for messages.</param>
	/// <returns>The file's settings.</returns>
	/// <exception cref="UserConfigTransferException">The text is not a settings file this version can import.</exception>
	internal static UserConfigPackage Parse(string json, string fileName)
	{
		JsonNode? root;

		try
		{
			root = JsonNode.Parse(json);
		}
		catch (JsonException ex)
		{
			throw new UserConfigTransferException($"'{fileName}' is not an FE-Buddy settings file: it is not valid JSON.", ex);
		}

		if (root is not JsonObject obj)
		{
			throw NotASettingsFile(fileName);
		}

		if (!obj.ContainsKey("format"))
		{
			// A plain UserConfig.json copied out of someone's %APPDATA%\FE-Buddy imports as it is.
			if (obj["General"] is JsonObject || obj["Services"] is JsonObject)
			{
				return new UserConfigPackage(fileName, FormatVersion: 0, AppVersion: null, ExportedUtc: null, Flatten(obj));
			}

			throw NotASettingsFile(fileName);
		}

		if (!string.Equals(ReadString(obj, "format"), FormatId, StringComparison.Ordinal))
		{
			throw NotASettingsFile(fileName);
		}

		string? appVersion = ReadString(obj, "appVersion");
		int version = obj["formatVersion"] is JsonValue v && v.TryGetValue(out int n) ? n : 0;

		if (version < 1)
		{
			throw new UserConfigTransferException($"'{fileName}' does not say which settings format it uses, so it cannot be imported.");
		}

		if (version > FormatVersion)
		{
			string from = appVersion is null ? "a newer FE-Buddy" : $"FE-Buddy v{appVersion.TrimStart('v', 'V')}";
			throw new UserConfigTransferException($"'{fileName}' was exported by {from}, which is newer than this one. Update FE-Buddy, then import it again.");
		}

		if (obj["settings"] is not JsonObject settings)
		{
			throw new UserConfigTransferException($"'{fileName}' has no settings in it.");
		}

		DateTimeOffset? exportedUtc =
			DateTimeOffset.TryParse(ReadString(obj, "exportedUtc"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset when)
				? when
				: null;

		return new UserConfigPackage(fileName, version, appVersion, exportedUtc, Flatten(settings));
	}

	/// <summary>Works out what importing <paramref name="package"/> would do to this PC's settings, without writing anything.</summary>
	/// <param name="package">A file read by <see cref="Read(string)"/>.</param>
	/// <returns>The import, ready for <see cref="Apply(UserConfigImportPlan)"/>.</returns>
	public static UserConfigImportPlan Plan(UserConfigPackage package) =>
		Plan(package, UserConfigFile.SnapshotValues(), PortablePathTokens.ForCurrentUser(), Directory.Exists, File.Exists);

	/// <summary>Works out an import against <paramref name="current"/>. The public overload passes this PC's.</summary>
	/// <param name="package">The file being imported.</param>
	/// <param name="current">This PC's settings, by dotted path.</param>
	/// <param name="tokens">How to expand the file's folder tokens.</param>
	/// <param name="directoryExists">Whether a folder exists on this PC.</param>
	/// <param name="fileExists">Whether a file exists on this PC; <see langword="null"/> uses <see cref="File.Exists(string)"/>.</param>
	/// <returns>The import.</returns>
	internal static UserConfigImportPlan Plan(
		UserConfigPackage package,
		IReadOnlyDictionary<string, string> current,
		PortablePathTokens tokens,
		Func<string, bool> directoryExists,
		Func<string, bool>? fileExists = null)
	{
		ArgumentNullException.ThrowIfNull(package);

		Dictionary<string, string> settings = new(StringComparer.Ordinal);
		List<ImportedFolder> applied = [];
		List<ImportedFolder> skipped = [];
		List<string> ignored = [];

		// Start from what only this PC has; everything else, folders included, comes from the file.
		foreach (KeyValuePair<string, string> entry in current)
		{
			if (UserConfigPortability.Classify(entry.Key) is ConfigKeyScope.Local or ConfigKeyScope.Secret)
			{
				settings[entry.Key] = entry.Value;
			}
		}

		// A folder the file leaves out is cleared like any other setting, so this PC's folders are walked too.
		SortedSet<string> folderKeys = new(
			current.Keys.Where(key => UserConfigPortability.Classify(key) == ConfigKeyScope.MachinePath),
			StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> entry in package.Values.OrderBy(e => e.Key, StringComparer.Ordinal))
		{
			ConfigKeyScope scope = UserConfigFile.IsValidKey(entry.Key)
				? UserConfigPortability.Classify(entry.Key)
				: ConfigKeyScope.Local;

			switch (scope)
			{
				case ConfigKeyScope.Shared:
					settings[entry.Key] = entry.Value;
					break;

				case ConfigKeyScope.MachinePath:
					folderKeys.Add(entry.Key);
					break;

				default:
					ignored.Add(entry.Key);
					break;
			}
		}

		foreach (string key in folderKeys)
		{
			PlanFolder(key, package.Values.GetValueOrDefault(key), current.GetValueOrDefault(key), tokens, directoryExists, fileExists ?? File.Exists, settings, applied, skipped);
		}

		// An unset setting and a blank one both mean "the default", so neither counts as a change from the other.
		int changed = current.Keys
			.Union(settings.Keys, StringComparer.Ordinal)
			.Count(key => (current.GetValueOrDefault(key) ?? string.Empty) != (settings.GetValueOrDefault(key) ?? string.Empty));

		// Named for the summary only when FE-Buddy knows what they are; the rest are kept all the same.
		List<string> keptForThisPc =
		[
			.. current.Keys
				.Where(key => UserConfigPortability.Classify(key) == ConfigKeyScope.Local)
				.Order(StringComparer.Ordinal)
				.Select(key => (Key: key, Label: UserConfigPortability.Describe(key)))
				.Where(k => k.Label != k.Key)
				.Select(k => k.Label)
				.Distinct(StringComparer.Ordinal),
		];

		return new UserConfigImportPlan(package, settings, changed, applied, skipped, keptForThisPc, ignored);
	}

	/// <summary>Writes an import worked out by <see cref="Plan(UserConfigPackage)"/>, keeping the replaced file as <see cref="UserConfigFile.BeforeImportFilePath"/>.</summary>
	/// <param name="plan">The import.</param>
	public static void Apply(UserConfigImportPlan plan)
	{
		ArgumentNullException.ThrowIfNull(plan);

		UserConfigFile.ReplaceAll(plan.Settings);

		AppLog.Info(
			LogSource,
			$"Imported settings from '{plan.Package.FileName}': {plan.ChangedCount} changed, "
			+ $"{plan.AppliedFolders.Count} folders taken, {plan.SkippedFolders.Count} folders kept, {plan.IgnoredKeys.Count} entries ignored.");
	}

	/// <summary>
	/// Matches one folder setting to the file's. The file's folder, made this user's, is taken when
	/// it works here: an output folder needs only its drive (FE-Buddy creates the folder when it
	/// writes), a folder FE-Buddy reads from must exist, and so must a file (a custom alias file).
	/// When it cannot work here, this PC keeps its own and the folder is listed as skipped. A folder
	/// the file does not set goes back to the default.
	/// </summary>
	private static void PlanFolder(
		string key,
		string? packaged,
		string? mine,
		PortablePathTokens tokens,
		Func<string, bool> directoryExists,
		Func<string, bool> fileExists,
		Dictionary<string, string> settings,
		List<ImportedFolder> applied,
		List<ImportedFolder> skipped)
	{
		string label = UserConfigPortability.Describe(key);

		if (string.IsNullOrWhiteSpace(packaged))
		{
			if (!string.IsNullOrWhiteSpace(mine))
			{
				applied.Add(new ImportedFolder(key, label, string.Empty, "not set in the file, so the default is used"));
			}

			return;
		}

		string folder = tokens.Localize(packaged);
		bool isOutput = UserConfigPortability.IsOutputFolder(key);

		bool isFile = UserConfigPortability.IsFile(key);

		string? problem =
			!IsLocalDrivePath(folder) ? (isFile ? "is not a file on a drive of this PC" : "is not a folder on a drive of this PC")
			: isFile ? (fileExists(folder) ? null : "is not found on this PC")
			: isOutput ? (directoryExists(Path.GetPathRoot(folder)!) ? null : "is on a drive this PC does not have")
			: directoryExists(folder) ? null : "is not found on this PC";

		if (problem is not null)
		{
			// Nothing here to match it with: keep this PC's own folder, if it has one.
			if (mine is not null)
			{
				settings[key] = mine;
			}

			skipped.Add(new ImportedFolder(key, label, folder, problem));
			return;
		}

		settings[key] = folder;

		if (!string.Equals(mine, folder, StringComparison.OrdinalIgnoreCase))
		{
			string? note = isOutput && !directoryExists(folder) ? "created when FE-Buddy first writes to it" : null;
			applied.Add(new ImportedFolder(key, label, folder, note));
		}
	}

	/// <summary>
	/// Whether <paramref name="path"/> is a full path on a drive letter (<c>C:\...</c>). Network
	/// (<c>\\server\share</c>) and device paths are refused before they are ever looked up: just
	/// checking whether a network folder exists makes Windows sign in to that server, and a
	/// settings file can come from anyone.
	/// </summary>
	private static bool IsLocalDrivePath(string path) =>
		path.Length >= 3
		&& char.IsAsciiLetter(path[0])
		&& path[1] == ':'
		&& path[2] is '\\' or '/';

	private static Dictionary<string, string> Flatten(JsonObject obj)
	{
		Dictionary<string, string> values = new(StringComparer.Ordinal);
		UserConfigFile.FlattenInto(obj, prefix: string.Empty, values);
		return values;
	}

	private static string? ReadString(JsonObject obj, string name) =>
		obj[name] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

	private static UserConfigTransferException NotASettingsFile(string fileName) =>
		new($"'{fileName}' is not an FE-Buddy settings file.");
}
