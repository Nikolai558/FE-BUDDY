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
/// { "format": "FE-Buddy.UserConfig", "formatVersion": 1, "configVersion": 1, "appVersion": "3.0.0",
///   "exportedUtc": "2026-09-27T12:00:00Z", "settings": { "General": { ... }, "Services": { ... } } }
/// </code>
/// <c>formatVersion</c> is this wrapper's; <c>configVersion</c> is the settings layout inside it
/// (<see cref="UserConfigVersion"/>), which <see cref="Read(string)"/> brings forward
/// (<see cref="UserConfigMigrations"/>). An export with no <c>configVersion</c> (3.0.0-beta.3 and
/// earlier) is taken to be in the oldest layout.
/// Each setting goes by its <see cref="UserConfigPortability.Classify(string)"/> scope: shared
/// settings travel as they are, folders travel tokenized (<see cref="PortablePathTokens"/>), and
/// PC-only state, credentials and credential choices never go into the file.
/// </para>
/// <para>
/// An import makes this PC's settings match the file's exactly: settings the file leaves out go
/// back to their defaults, folders included. Only three things are kept: this PC's own state (the
/// update channel and the like), which is never touched - a setting in the file that would nest
/// under one of these, or they under it, is ignored; this PC's folder wherever the file's folder
/// cannot work here (a custom alias file that is not on this PC is left out instead;
/// <see cref="Plan(UserConfigPackage, UserConfigImportMode)"/> says which); and this PC's
/// credential choice for a setting the import leaves unchanged - a custom alias file at the same
/// address keeps its credential, any other loses it. Folders are made this user's
/// (<see cref="PortablePathTokens.Localize(string)"/>), so the other user's name is never
/// imported. A plain <c>UserConfig.json</c> copied from another PC imports the same way.
/// </para>
/// <para>
/// Import is two steps so the user sees what will happen first: <see cref="Plan(UserConfigPackage, UserConfigImportMode)"/>
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
	/// <param name="path">The file to write. An existing file is replaced - but never one of FE-Buddy's own settings files.</param>
	/// <returns>What was written.</returns>
	/// <exception cref="UserConfigTransferException"><paramref name="path"/> is one of FE-Buddy's own settings files.</exception>
	public static UserConfigExportResult Export(string path) =>
		Export(path, UserConfigFile.SnapshotValues(), AppVersion.Current, DateTimeOffset.UtcNow, PortablePathTokens.ForCurrentUser());

	/// <summary>Writes a settings file from <paramref name="values"/>. The public overload passes this PC's.</summary>
	/// <param name="path">The file to write.</param>
	/// <param name="values">The settings, by dotted path.</param>
	/// <param name="appVersion">The FE-Buddy version to stamp the file with.</param>
	/// <param name="exportedUtc">When the export happened.</param>
	/// <param name="tokens">How to tokenize folders.</param>
	/// <returns>What was written.</returns>
	/// <exception cref="UserConfigTransferException"><paramref name="path"/> is one of FE-Buddy's own settings files.</exception>
	internal static UserConfigExportResult Export(
		string path,
		IReadOnlyDictionary<string, string> values,
		string appVersion,
		DateTimeOffset exportedUtc,
		PortablePathTokens tokens)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		// An export over a profile would turn live settings into an export file.
		if (IsOwnSettingsFile(path))
		{
			throw new UserConfigTransferException($"'{Path.GetFileName(path)}' is one of FE-Buddy's own settings files. Export to a different file.");
		}

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
					// No folder chosen: nothing to share. An import of the file puts the importing PC's
					// folder back to the default, just as it is here.
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
			["configVersion"] = UserConfigMigrations.CurrentVersion,
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
		try
		{
			return ParseJson(json, fileName);
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
		{
			// Some damage only shows while the file is read through, not when it is parsed: a name
			// used twice in one object, text that is not valid Unicode, and the like.
			throw new UserConfigTransferException($"'{fileName}' is not an FE-Buddy settings file: it is not valid JSON.", ex);
		}
	}

	/// <summary><see cref="Parse"/> itself; any JSON damage it finds is thrown as the reader throws it.</summary>
	private static UserConfigPackage ParseJson(string json, string fileName)
	{
		if (JsonNode.Parse(json) is not JsonObject obj)
		{
			throw NotASettingsFile(fileName);
		}

		if (!obj.ContainsKey("format"))
		{
			// A plain UserConfig.json copied out of someone's %APPDATA%\FE-Buddy imports as it is.
			if (obj["General"] is JsonObject || obj["Services"] is JsonObject)
			{
				int plainVersion = UserConfigFile.TakeVersion(obj, fileName);
				return Package(fileName, formatVersion: 0, appVersion: null, exportedUtc: null, Flatten(obj), plainVersion);
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

		int configVersion = obj["configVersion"] is JsonValue c && c.TryGetValue(out int layout) && layout >= UserConfigVersion.Oldest
			? layout
			: UserConfigVersion.Oldest;

		return Package(fileName, version, appVersion, exportedUtc, Flatten(settings), configVersion);
	}

	/// <summary>
	/// A package of a file's settings, brought up to this version's layout. A file saved in a newer
	/// layout is refused: this FE-Buddy can't know what its settings mean.
	/// </summary>
	/// <exception cref="UserConfigTransferException">The file is in a newer layout, or its settings can't be brought forward.</exception>
	private static UserConfigPackage Package(
		string fileName,
		int formatVersion,
		string? appVersion,
		DateTimeOffset? exportedUtc,
		Dictionary<string, string> values,
		int configVersion)
	{
		if (configVersion > UserConfigMigrations.CurrentVersion)
		{
			string from = appVersion is null ? "a newer FE-Buddy" : $"FE-Buddy v{appVersion.TrimStart('v', 'V')}";
			throw new UserConfigTransferException($"'{fileName}' was saved by {from}, which keeps its settings differently from this one. Update FE-Buddy, then import it again.");
		}

		UserConfigMigrationResult migrated;

		try
		{
			migrated = UserConfigMigrations.Migrate(values, configVersion);
		}
		catch (InvalidOperationException ex)
		{
			throw new UserConfigTransferException($"The settings in '{fileName}' could not be brought up to this version of FE-Buddy: {ex.Message}", ex);
		}

		if (migrated.Migrated)
		{
			AppLog.Info(LogSource, $"Brought '{fileName}' up from settings layout {configVersion} to layout {migrated.ToVersion}: {string.Join("; ", migrated.Applied)}.");
		}

		return new UserConfigPackage(fileName, formatVersion, appVersion, exportedUtc, migrated.Values, configVersion);
	}

	/// <summary>Works out what importing <paramref name="package"/> would do to this PC's settings, without writing anything.</summary>
	/// <param name="package">A file read by <see cref="Read(string)"/>.</param>
	/// <param name="mode">Whether the file's settings replace the profile's or merge into them.</param>
	/// <returns>The import, ready for <see cref="Apply(UserConfigImportPlan)"/> or <see cref="ApplyAsNewProfile"/>.</returns>
	public static UserConfigImportPlan Plan(UserConfigPackage package, UserConfigImportMode mode = UserConfigImportMode.Replace) =>
		Plan(package, UserConfigFile.SnapshotValues(), PortablePathTokens.ForCurrentUser(), Directory.Exists, File.Exists, mode);

	/// <summary>Works out an import against <paramref name="current"/>. The public overload passes this PC's.</summary>
	/// <param name="package">The file being imported.</param>
	/// <param name="current">This PC's settings, by dotted path.</param>
	/// <param name="tokens">How to expand the file's folder tokens.</param>
	/// <param name="directoryExists">Whether a folder exists on this PC.</param>
	/// <param name="fileExists">Whether a file exists on this PC; <see langword="null"/> uses <see cref="File.Exists(string)"/>.</param>
	/// <param name="mode">Whether the file's settings replace the profile's or merge into them.</param>
	/// <returns>The import.</returns>
	internal static UserConfigImportPlan Plan(
		UserConfigPackage package,
		IReadOnlyDictionary<string, string> current,
		PortablePathTokens tokens,
		Func<string, bool> directoryExists,
		Func<string, bool>? fileExists = null,
		UserConfigImportMode mode = UserConfigImportMode.Replace)
	{
		ArgumentNullException.ThrowIfNull(package);

		Dictionary<string, string> settings = new(StringComparer.Ordinal);
		List<ImportedFolder> applied = [];
		List<ImportedFolder> skipped = [];
		List<string> ignored = [];
		bool merge = mode == UserConfigImportMode.Merge;

		// Start from what only this PC has; everything else, folders included, comes from the file.
		foreach (KeyValuePair<string, string> entry in current)
		{
			if (UserConfigPortability.Classify(entry.Key) is ConfigKeyScope.Local or ConfigKeyScope.Secret)
			{
				settings[entry.Key] = entry.Value;
			}
		}

		HashSet<string> keptForPc = [.. settings.Keys];

		if (merge)
		{
			// Merging, the profile's own settings stay too - but not a list the file brings a whole new copy
			// of, nor one that would nest under one of the file's settings, or it under them.
			HashSet<string> listsInFile = [.. package.Values.Keys.Select(ListOf).OfType<string>()];
			HashSet<string> fileKeys = [.. package.Values.Keys];

			foreach (KeyValuePair<string, string> entry in current)
			{
				if (UserConfigPortability.Classify(entry.Key) is ConfigKeyScope.Shared or ConfigKeyScope.MachinePath
					&& (ListOf(entry.Key) is not { } list || !listsInFile.Contains(list))
					&& !Overlaps(entry.Key, fileKeys))
				{
					settings[entry.Key] = entry.Value;
				}
			}
		}

		// A folder the file leaves out is cleared like any other setting, so this PC's folders are walked
		// too - unless merging, which keeps them as they are.
		SortedSet<string> folderKeys = new(
			merge ? [] : current.Keys.Where(key => UserConfigPortability.Classify(key) == ConfigKeyScope.MachinePath),
			StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> entry in package.Values.OrderBy(e => e.Key, StringComparer.Ordinal))
		{
			// A key nested under one this PC keeps, or one this PC's kept key nests under, would
			// overwrite it when the file is written: General.UpdateChannel.X would wipe the channel.
			ConfigKeyScope scope = UserConfigFile.IsValidKey(entry.Key) && !Overlaps(entry.Key, keptForPc)
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

		// A credential choice never comes from the file. This PC's stays only with a setting the import
		// leaves as it is - the same custom alias file at the same address - so it never lands on another.
		foreach (KeyValuePair<string, string> entry in current)
		{
			if (UserConfigPortability.Classify(entry.Key) == ConfigKeyScope.CredentialChoice && IsUnchangedBeside(entry.Key, current, settings))
			{
				settings[entry.Key] = entry.Value;
			}
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

		return new UserConfigImportPlan(package, settings, changed, applied, skipped, keptForThisPc, ignored) { Mode = mode };
	}

	/// <summary>
	/// Writes an import worked out by <see cref="Plan(UserConfigPackage, UserConfigImportMode)"/> into the
	/// profile in use, keeping the replaced file as <see cref="UserConfigFile.BeforeImportFilePath"/>.
	/// </summary>
	/// <param name="plan">The import.</param>
	public static void Apply(UserConfigImportPlan plan)
	{
		ArgumentNullException.ThrowIfNull(plan);

		UserConfigFile.ReplaceAll(plan.Settings);

		AppLog.Info(
			LogSource,
			$"Imported settings from '{plan.Package.FileName}' into profile '{UserConfigFile.ActiveProfile}' ({plan.Mode}): {plan.ChangedCount} changed, "
			+ $"{plan.AppliedFolders.Count} folders taken, {plan.SkippedFolders.Count} folders or files not taken, {plan.IgnoredKeys.Count} entries ignored.");
	}

	/// <summary>
	/// Writes an import (planned to replace) into a new profile of its own, and puts that profile to
	/// use. The profile that was in use is left as it is.
	/// </summary>
	/// <param name="plan">The import, planned with <see cref="UserConfigImportMode.Replace"/>.</param>
	/// <param name="profile">The new profile's name.</param>
	/// <exception cref="ArgumentException">The name can't be used, or another profile has it.</exception>
	/// <exception cref="IOException">The profile could not be written or put to use.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void ApplyAsNewProfile(UserConfigImportPlan plan, string profile)
	{
		ArgumentNullException.ThrowIfNull(plan);

		UserConfigFile.CreateProfile(profile, plan.Settings);
		UserConfigFile.SwitchProfile(profile);

		AppLog.Info(LogSource, $"Imported settings from '{plan.Package.FileName}' as the new profile '{UserConfigFile.ActiveProfile}'.");
	}

	/// <summary>Whether <paramref name="path"/> is one of FE-Buddy's own settings files: a profile, a backup, or the shared file.</summary>
	private static bool IsOwnSettingsFile(string path)
	{
		string target = Path.GetFullPath(path);
		string folder = Path.GetFullPath(UserConfigFile.Directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		string legacy = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(folder.TrimEnd(Path.DirectorySeparatorChar))!, UserConfigFile.LegacyConfigFileName));

		return target.StartsWith(folder, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(target, legacy, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// The numbered list <paramref name="key"/> is an entry of - <c>...ConcatenateAliases.Sources</c> for
	/// <c>...ConcatenateAliases.Sources.2.FilePath</c> - or <see langword="null"/> when it is in none.
	/// </summary>
	private static string? ListOf(string key)
	{
		string[] segments = key.Split('.');

		for (int i = 1; i < segments.Length; i++)
		{
			if (segments[i].Length > 0 && segments[i].All(char.IsAsciiDigit))
			{
				return string.Join('.', segments[..i]);
			}
		}

		return null;
	}

	/// <summary>
	/// Matches one folder setting to the file's. The file's folder, made this user's, is taken when
	/// it works here: an output folder needs only its drive (FE-Buddy creates the folder when it
	/// writes), a folder FE-Buddy reads from must exist, and so must a file (a custom alias file).
	/// When it cannot work here, it is listed as skipped, saying what happens instead: this PC keeps
	/// its own folder, or the default when it has none; a custom alias file - one entry in a numbered
	/// list, where this PC's entry at the same number is a different file - is left out. A folder the
	/// file does not set goes back to the default.
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

		// A token stands for this user's own Desktop, Documents or profile, which stays theirs even when
		// Windows keeps it on a network share (folder redirection). Any other network path is refused.
		bool isOnThisPc = PortablePathTokens.StartsWithToken(packaged) ? Path.IsPathFullyQualified(folder) : IsLocalDrivePath(folder);

		string? problem =
			!isOnThisPc ? (isFile ? "is not a file on a drive of this PC" : "is not a folder on a drive of this PC")
			: isFile ? (fileExists(folder) ? null : "is not found on this PC")
			: isOutput ? (directoryExists(Path.GetPathRoot(folder)!) ? null : "is on a drive this PC does not have")
			: directoryExists(folder) ? null : "is not found on this PC";

		if (problem is not null)
		{
			string instead;

			if (IsListEntry(key))
			{
				instead = "so it is left out";
			}
			else if (!string.IsNullOrWhiteSpace(mine))
			{
				settings[key] = mine;
				instead = isFile ? "so this PC's file is kept" : "so this PC's folder is kept";
			}
			else
			{
				instead = "so the default is used";
			}

			skipped.Add(new ImportedFolder(key, label, folder, $"{problem}, {instead}"));
			return;
		}

		// The same folder written differently (D:\feb, D:\FEB) is not a change.
		if (string.Equals(mine, folder, StringComparison.OrdinalIgnoreCase))
		{
			settings[key] = mine!;
			return;
		}

		settings[key] = folder;

		string? note = isOutput && !directoryExists(folder) ? "created when FE-Buddy first writes to it" : null;
		applied.Add(new ImportedFolder(key, label, folder, note));
	}

	/// <summary>Whether <paramref name="key"/> is one entry of a numbered list, such as <c>Sources.2.FilePath</c>.</summary>
	private static bool IsListEntry(string key)
	{
		string[] segments = key.Split('.');
		return segments.Length >= 2 && segments[^2].Length > 0 && segments[^2].All(char.IsAsciiDigit);
	}

	/// <summary>
	/// Whether <paramref name="key"/> is nested under one of <paramref name="kept"/>, or one of them
	/// under it - so writing both would make one replace the other.
	/// </summary>
	private static bool Overlaps(string key, HashSet<string> kept) =>
		kept.Any(k => key.StartsWith(k + ".", StringComparison.Ordinal) || k.StartsWith(key + ".", StringComparison.Ordinal));

	/// <summary>
	/// Whether the import leaves every setting beside <paramref name="key"/> - under the same node,
	/// such as the rest of <c>Sources.2</c> - as this PC has it.
	/// </summary>
	private static bool IsUnchangedBeside(string key, IReadOnlyDictionary<string, string> current, Dictionary<string, string> planned)
	{
		string node = key[..(key.LastIndexOf('.') + 1)];

		return current.Keys
			.Union(planned.Keys, StringComparer.Ordinal)
			.Where(other => other.StartsWith(node, StringComparison.Ordinal) && other != key)
			.All(other => (current.GetValueOrDefault(other) ?? string.Empty) == (planned.GetValueOrDefault(other) ?? string.Empty));
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
