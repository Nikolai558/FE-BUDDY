using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Reads and writes <c>%APPDATA%\FE-Buddy\UserConfig.json</c>, the file that stores every
/// user preference FE-Buddy needs between runs (selected AIRAC cycle, ARTCC ID, output
/// directory, ROI, GeoJSON options, CRC ERAM defaults, and so on).
/// </summary>
/// <remarks>
/// <para>
/// On disk the file is a nested JSON object. In memory it is a flat dictionary keyed by dotted
/// path (e.g. <c>Services.AiracService.UserArtccId</c>), the same shape as the settings blocks
/// the sub-services read. Every leaf value is a string.
/// </para>
/// <para>
/// Saves are per sub-service: <see cref="Save(string)"/> writes only the subtree at one node
/// path and leaves every sibling section untouched. Before each save the previous state of
/// that one subtree is snapshotted to <c>UserConfig.previous.json</c>, giving a single level
/// of undo per node (<see cref="CanUndo(string)"/> / <see cref="Undo(string)"/>). A second
/// save of the same node overwrites the snapshot - there is no deeper history, and there is
/// deliberately no whole-file undo.
/// </para>
/// <para>
/// The launch read path never throws: a missing file or a missing key yields defaults and a
/// log entry, so a first run with no config behaves exactly like a run with an empty config.
/// </para>
/// <para>
/// The file carries its layout version (<see cref="UserConfigVersion"/>) at the top, outside the
/// settings, and every write stamps it. A file read in an older layout is brought up to the current
/// one (<see cref="UserConfigMigrations"/>) and written back at once, the file as it was kept beside
/// it as <c>UserConfig.v&lt;old version&gt;.json</c>.
/// </para>
/// </remarks>
public static class UserConfigFile
{
	private const string LogSource = "UserConfig";
	private const string ConfigFileName = "UserConfig.json";
	private const string PreviousFileName = "UserConfig.previous.json";
	private const string BeforeImportFileName = "UserConfig.before-import.json";

	/// <summary>
	/// The most dotted parts a key may have. FE-Buddy's own keys have fewer than ten; the limit keeps
	/// a settings file from another PC from nesting the JSON deeper than it can be written.
	/// </summary>
	private const int MaxKeyParts = 32;

	private static readonly Lock Gate = new();
	private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal);
	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

	private static string _directory = AppPaths.AppDataDirectory;

	/// <summary>
	/// The layout every write stamps the file with: the current one, unless the file was read in a
	/// layout it could not be fully brought out of - an older one whose update could not be written
	/// (so the next launch tries again), or a newer one this FE-Buddy does not know.
	/// </summary>
	private static int _stampVersion = UserConfigVersion.Current;

	/// <summary>
	/// The directory holding <c>UserConfig.json</c>: <c>%APPDATA%\FE-Buddy</c> by default.
	/// </summary>
	public static string Directory
	{
		get
		{
			lock (Gate)
			{
				return _directory;
			}
		}
	}

	/// <summary>The full path of <c>UserConfig.json</c>.</summary>
	public static string ConfigFilePath => Path.Combine(Directory, ConfigFileName);

	/// <summary>The full path of the one-step undo snapshot file, <c>UserConfig.previous.json</c>.</summary>
	public static string PreviousFilePath => Path.Combine(Directory, PreviousFileName);

	/// <summary>
	/// The full path of <c>UserConfig.before-import.json</c>: the whole file as it was before the
	/// last <see cref="ReplaceAll"/>, kept so an import can be taken back by hand.
	/// </summary>
	public static string BeforeImportFilePath => Path.Combine(Directory, BeforeImportFileName);

	/// <summary>
	/// The full path a file in an older layout is kept at once it has been brought forward, e.g.
	/// <c>UserConfig.v1.json</c>.
	/// </summary>
	/// <param name="version">The layout the file was in.</param>
	/// <returns>The path.</returns>
	public static string BroughtForwardFilePath(int version) =>
		Path.Combine(Directory, string.Create(CultureInfo.InvariantCulture, $"UserConfig.v{version}.json"));

	/// <summary>
	/// Reads <c>UserConfig.json</c> from disk into the in-memory dictionary, replacing whatever
	/// was there. A missing or unreadable file leaves the dictionary empty and logs a warning
	/// rather than throwing - this is the launch read path. A file in an older layout is brought
	/// forward (see the class remarks).
	/// </summary>
	/// <returns>Whether the file was read, missing or unreadable.</returns>
	public static UserConfigReadResult ReadAll()
	{
		lock (Gate)
		{
			Values.Clear();
			_stampVersion = UserConfigMigrations.CurrentVersion;

			string path = ConfigFilePath;

			if (!File.Exists(path))
			{
				AppLog.Warning(LogSource, $"No config file at '{path}'. Using defaults for every setting.");
				return UserConfigReadResult.Missing;
			}

			try
			{
				string json = File.ReadAllText(path);
				JsonNode? root = JsonNode.Parse(json);

				if (root is JsonObject obj)
				{
					int version = TakeVersion(obj, path);
					FlattenInto(obj, prefix: string.Empty, Values);

					if (version < UserConfigMigrations.CurrentVersion)
					{
						BringForward(path, version);
					}
					else if (version > UserConfigMigrations.CurrentVersion)
					{
						// Saves keep its stamp, so the newer FE-Buddy never takes its own settings for old ones.
						_stampVersion = version;
						AppLog.Warning(LogSource, $"'{ConfigFileName}' was saved by a newer FE-Buddy (settings layout {version}; this one knows up to " +
							$"{UserConfigMigrations.CurrentVersion}). Settings it doesn't know are kept as they are.");
					}

					return UserConfigReadResult.Read;
				}

				AppLog.Warning(LogSource, $"Config file '{path}' is not a JSON object. Using defaults.");
				return UserConfigReadResult.Unreadable;
			}
			catch (Exception ex)
			{
				Values.Clear();
				AppLog.Warning(LogSource, $"Could not read config file '{path}': {ex.Message}. Using defaults.");
				return UserConfigReadResult.Unreadable;
			}
		}
	}

	/// <summary>
	/// Writes the entire in-memory dictionary to <c>UserConfig.json</c> as a nested JSON tree,
	/// then re-reads it so the dictionary reflects exactly what is on disk.
	/// </summary>
	public static void Write()
	{
		lock (Gate)
		{
			WriteConfig(ConfigFilePath, BuildTree(Values), _stampVersion);
		}

		ReadAll();
	}

	/// <summary>
	/// Gets a single value by dotted path (e.g. <c>Services.AiracService.UserArtccId</c>).
	/// </summary>
	/// <param name="dottedPath">The dotted path of the leaf value.</param>
	/// <returns>The stored string, or <see langword="null"/> if the path is not set.</returns>
	public static string? GetValue(string dottedPath)
	{
		if (string.IsNullOrWhiteSpace(dottedPath))
		{
			return null;
		}

		lock (Gate)
		{
			if (Values.TryGetValue(dottedPath, out string? value))
			{
				return value;
			}
		}

		AppLog.Debug(LogSource, $"Config key '{dottedPath}' is not set; caller will use its default.");
		return null;
	}

	/// <summary>
	/// Sets a value in the in-memory dictionary. Does <b>not</b> persist - call
	/// <see cref="Save(string)"/> (or <see cref="Write"/>) to write it to disk.
	/// </summary>
	/// <param name="dottedPath">The dotted path of the leaf value.</param>
	/// <param name="value">The value to store. <see langword="null"/> is stored as an empty string.</param>
	/// <returns><see langword="true"/> if the value was set; <see langword="false"/> if the path was invalid.</returns>
	public static bool TrySetValue(string dottedPath, string value)
	{
		if (!IsValidKey(dottedPath))
		{
			return false;
		}

		lock (Gate)
		{
			Values[dottedPath] = value ?? string.Empty;
		}

		return true;
	}

	/// <summary>
	/// Removes a value and every value below it from the in-memory dictionary - e.g. a list whose
	/// entries are numbered keys, before it is written again with fewer entries. Does <b>not</b>
	/// persist - call <see cref="Save(string)"/> to write it to disk.
	/// </summary>
	/// <param name="dottedPath">The dotted path to remove, e.g. <c>Services.AiracService.ConcatenateAliases.Sources</c>.</param>
	/// <returns>How many values were removed.</returns>
	public static int RemoveValues(string dottedPath)
	{
		if (!IsValidKey(dottedPath))
		{
			return 0;
		}

		string below = dottedPath + ".";

		lock (Gate)
		{
			string[] keys = [.. Values.Keys.Where(key => key == dottedPath || key.StartsWith(below, StringComparison.Ordinal))];

			foreach (string key in keys)
			{
				Values.Remove(key);
			}

			return keys.Length;
		}
	}

	/// <summary>
	/// Replaces every setting at once with <paramref name="values"/> and writes the whole file -
	/// the one write that is not per node, used by a settings import. The values must be in the
	/// current layout (<see cref="UserConfigTransfer.Read(string)"/> brings an import's forward).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The new file is written beside the old one first, then swapped in with
	/// <see cref="File.Replace(string, string, string?)"/>, which keeps the file being replaced as
	/// <see cref="BeforeImportFilePath"/> in the same step. So a write that fails - a full disk, say -
	/// leaves <c>UserConfig.json</c>, and the settings in memory, exactly as they were.
	/// </para>
	/// <para>
	/// Once the new file is in place, the per-node undo snapshots are deleted: they describe settings
	/// that no longer exist, so an "undo last save" would otherwise put a pre-import subtree back into
	/// the imported file. That, and re-reading the new file, always happen once the swap has. Keys
	/// that <see cref="TrySetValue"/> would refuse are dropped.
	/// </para>
	/// </remarks>
	/// <param name="values">Every setting the file should hold afterwards, by dotted path.</param>
	/// <exception cref="IOException">The new file could not be written or swapped in; nothing was changed.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void ReplaceAll(IReadOnlyDictionary<string, string> values)
	{
		ArgumentNullException.ThrowIfNull(values);

		Dictionary<string, string> replacement = new(StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> entry in values)
		{
			if (IsValidKey(entry.Key))
			{
				replacement[entry.Key] = entry.Value ?? string.Empty;
			}
		}

		lock (Gate)
		{
			string incoming = ConfigFilePath + ".importing";

			try
			{
				WriteConfig(incoming, BuildTree(replacement), UserConfigMigrations.CurrentVersion);

				if (File.Exists(ConfigFilePath))
				{
					File.Replace(incoming, ConfigFilePath, BeforeImportFilePath, ignoreMetadataErrors: true);
				}
				else
				{
					File.Move(incoming, ConfigFilePath);
				}
			}
			catch
			{
				DeleteIfPresent(incoming);
				throw;
			}

			if (!DeleteIfPresent(PreviousFilePath))
			{
				AppLog.Warning(LogSource, $"Could not delete '{PreviousFileName}', so Undo last save may put back a setting from before the import.");
			}
		}

		ReadAll();

		AppLog.Info(LogSource, $"Replaced every setting ({replacement.Count} values); the previous file is kept as '{BeforeImportFileName}'.");
	}

	/// <summary>Deletes a file if it is there.</summary>
	/// <returns><see langword="false"/> when it is there and could not be deleted.</returns>
	private static bool DeleteIfPresent(string path)
	{
		try
		{
			File.Delete(path);
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>
	/// Persists only the subtree under <paramref name="nodePath"/> to <c>UserConfig.json</c>,
	/// leaving every sibling section on disk untouched. The previous on-disk state of that one
	/// subtree is first snapshotted to <c>UserConfig.previous.json</c> so it can be restored
	/// once with <see cref="Undo(string)"/>. After the write the in-memory dictionary is
	/// refreshed from disk.
	/// </summary>
	/// <param name="nodePath">
	/// The dotted path of the node to save, e.g. <c>Services.AiracService.Airways</c>.
	/// </param>
	public static void Save(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			throw new ArgumentException("A node path is required.", nameof(nodePath));
		}

		lock (Gate)
		{
			JsonObject onDisk = LoadObjectOrEmpty(ConfigFilePath);

			// Snapshot the CURRENT on-disk subtree into the previous file, at the same path.
			JsonObject previous = LoadObjectOrEmpty(PreviousFilePath);
			JsonNode? currentSubtree = GetNodeAtPath(onDisk, nodePath);
			SetNodeAtPath(previous, nodePath, currentSubtree?.DeepClone());
			WriteObject(PreviousFilePath, previous);

			// Replace the subtree on disk with the in-memory version built from the flat dict.
			JsonNode? newSubtree = BuildSubtree(Values, nodePath);
			SetNodeAtPath(onDisk, nodePath, newSubtree);
			WriteConfig(ConfigFilePath, onDisk, _stampVersion);
		}

		ReadAll();

		AppLog.Info(LogSource, $"Saved '{nodePath}'.");
	}

	/// <summary>
	/// Indicates whether <see cref="Undo(string)"/> can restore a previous value for
	/// <paramref name="nodePath"/> - i.e. a snapshot for that exact node exists.
	/// </summary>
	/// <param name="nodePath">The dotted path that was previously saved.</param>
	/// <returns><see langword="true"/> if a one-step undo is available for that node.</returns>
	public static bool CanUndo(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath) || !File.Exists(PreviousFilePath))
		{
			return false;
		}

		lock (Gate)
		{
			JsonObject previous = LoadObjectOrEmpty(PreviousFilePath);
			return NodeExistsAtPath(previous, nodePath);
		}
	}

	/// <summary>
	/// Restores the one snapshotted previous state of <paramref name="nodePath"/> to
	/// <c>UserConfig.json</c> and consumes the snapshot, so a further undo of the same node is
	/// not possible until it is saved again. A no-op (returns <see langword="false"/>) when
	/// there is no snapshot for that node.
	/// </summary>
	/// <param name="nodePath">The dotted path to restore.</param>
	/// <returns><see langword="true"/> if a previous value was restored.</returns>
	public static bool Undo(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			return false;
		}

		lock (Gate)
		{
			if (!File.Exists(PreviousFilePath))
			{
				return false;
			}

			JsonObject previous = LoadObjectOrEmpty(PreviousFilePath);

			if (!NodeExistsAtPath(previous, nodePath))
			{
				return false;
			}

			JsonNode? snapshot = GetNodeAtPath(previous, nodePath);

			JsonObject onDisk = LoadObjectOrEmpty(ConfigFilePath);
			SetNodeAtPath(onDisk, nodePath, snapshot?.DeepClone());
			WriteConfig(ConfigFilePath, onDisk, _stampVersion);

			// Consume the snapshot: undo depth is exactly one.
			RemoveNodeAtPath(previous, nodePath);
			WriteObject(PreviousFilePath, previous);
		}

		ReadAll();

		AppLog.Info(LogSource, $"Reverted '{nodePath}' to its previous saved value.");
		return true;
	}

	/// <summary>
	/// Points the config directory somewhere else and clears in-memory state. Unit tests only.
	/// </summary>
	/// <param name="directory">A throwaway directory, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(string? directory)
	{
		lock (Gate)
		{
			Values.Clear();
			_directory = directory ?? AppPaths.AppDataDirectory;
			_stampVersion = UserConfigMigrations.CurrentVersion;
		}
	}

	/// <summary>A copy of every setting currently in memory, by dotted path.</summary>
	/// <returns>A copy of every key and value.</returns>
	public static IReadOnlyDictionary<string, string> SnapshotValues()
	{
		lock (Gate)
		{
			return new Dictionary<string, string>(Values, StringComparer.Ordinal);
		}
	}

	/// <summary>
	/// Recursively walks a JSON object, adding every leaf (non-object) it reaches to
	/// <paramref name="dest"/> keyed by its dotted path. Arrays and nulls are stored as their
	/// JSON text; every other scalar is stored as its string form.
	/// </summary>
	internal static void FlattenInto(JsonObject obj, string prefix, Dictionary<string, string> dest)
	{
		foreach (KeyValuePair<string, JsonNode?> pair in obj)
		{
			string path = prefix.Length == 0 ? pair.Key : $"{prefix}.{pair.Key}";

			switch (pair.Value)
			{
				case JsonObject childObject:
					FlattenInto(childObject, path, dest);
					break;

				case JsonValue value:
					dest[path] = value.ToString();
					break;

				case null:
					dest[path] = string.Empty;
					break;

				default:
					// Arrays (rare in this config) are round-tripped as their JSON text.
					dest[path] = pair.Value.ToJsonString();
					break;
			}
		}
	}

	/// <summary>
	/// Whether <paramref name="dottedPath"/> can name a setting: not blank, no empty segment (no
	/// leading, trailing or doubled dot), and at most <see cref="MaxKeyParts"/> parts.
	/// </summary>
	/// <param name="dottedPath">The candidate key.</param>
	/// <returns><see langword="true"/> when the key is usable.</returns>
	internal static bool IsValidKey(string? dottedPath) =>
		!string.IsNullOrWhiteSpace(dottedPath)
		&& !dottedPath.StartsWith('.')
		&& !dottedPath.EndsWith('.')
		&& !dottedPath.Contains("..", StringComparison.Ordinal)
		&& dottedPath.Count(c => c == '.') < MaxKeyParts;

	/// <summary>Builds a full nested JSON object from every entry in the flat dictionary.</summary>
	internal static JsonObject BuildTree(IReadOnlyDictionary<string, string> values)
	{
		JsonObject root = [];

		foreach (KeyValuePair<string, string> entry in values)
		{
			SetNodeAtPath(root, entry.Key, JsonValue.Create(entry.Value));
		}

		return root;
	}

	/// <summary>
	/// Builds the JSON node for a single sub-service subtree from the flat dictionary: every
	/// entry whose key equals <paramref name="nodePath"/> or starts with <c>nodePath + "."</c>.
	/// Returns a <see cref="JsonObject"/> for a container node, a <see cref="JsonValue"/> for a
	/// bare leaf, or <see langword="null"/> when the dictionary holds nothing under that path.
	/// </summary>
	private static JsonNode? BuildSubtree(IReadOnlyDictionary<string, string> values, string nodePath)
	{
		string childPrefix = nodePath + ".";
		JsonObject subtree = [];
		bool anyChild = false;

		foreach (KeyValuePair<string, string> entry in values)
		{
			if (entry.Key == nodePath)
			{
				// The node path itself is a leaf value.
				return JsonValue.Create(entry.Value);
			}

			if (entry.Key.StartsWith(childPrefix, StringComparison.Ordinal))
			{
				string relative = entry.Key[childPrefix.Length..];
				SetNodeAtPath(subtree, relative, JsonValue.Create(entry.Value));
				anyChild = true;
			}
		}

		return anyChild ? subtree : null;
	}

	private static JsonObject LoadObjectOrEmpty(string path)
	{
		if (!File.Exists(path))
		{
			return [];
		}

		try
		{
			return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [];
		}
		catch
		{
			return [];
		}
	}

	private static void WriteObject(string path, JsonObject root)
	{
		System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, root.ToJsonString(WriteOptions));
	}

	/// <summary>Writes a whole settings file, stamped with its layout version at the top.</summary>
	private static void WriteConfig(string path, JsonObject root, int version)
	{
		JsonObject stamped = new() { [UserConfigVersion.FileKey] = version };

		foreach (KeyValuePair<string, JsonNode?> entry in root.Where(entry => entry.Key != UserConfigVersion.FileKey).ToList())
		{
			root.Remove(entry.Key);
			stamped[entry.Key] = entry.Value;
		}

		WriteObject(path, stamped);
	}

	/// <summary>
	/// Takes a settings file's layout stamp out of its top level, leaving only settings: the version,
	/// or <see cref="UserConfigVersion.Oldest"/> when the file has none (beta.2 and beta.3 wrote none)
	/// or one that can't be read.
	/// </summary>
	/// <param name="root">The file's top level; the stamp is removed from it.</param>
	/// <param name="fileName">The file, for the log.</param>
	/// <returns>The layout the file is in.</returns>
	internal static int TakeVersion(JsonObject root, string fileName)
	{
		if (!root.TryGetPropertyValue(UserConfigVersion.FileKey, out JsonNode? stamp))
		{
			return UserConfigVersion.Oldest;
		}

		root.Remove(UserConfigVersion.FileKey);

		if (stamp is JsonValue value && value.TryGetValue(out int version) && version >= UserConfigVersion.Oldest)
		{
			return version;
		}

		AppLog.Warning(LogSource, $"'{fileName}' has a settings layout stamp FE-Buddy can't read ({stamp?.ToJsonString() ?? "null"}). " +
			$"Its settings are taken to be in layout {UserConfigVersion.Oldest}.");
		return UserConfigVersion.Oldest;
	}

	/// <summary>
	/// Brings the settings just read in layout <paramref name="version"/> up to the current one and
	/// writes them back, keeping the file as it was (<see cref="BroughtForwardFilePath(int)"/>). The
	/// per-node undo snapshots are of the old layout, so they go. When the update can't be worked out,
	/// the settings are used as they are and the file is left alone; when it can't be written, the
	/// file keeps its old stamp so the next launch tries again. Never throws.
	/// </summary>
	private static void BringForward(string path, int version)
	{
		UserConfigMigrationResult result;

		try
		{
			result = UserConfigMigrations.Migrate(Values, version);
		}
		catch (InvalidOperationException ex)
		{
			_stampVersion = version;
			AppLog.Warning(LogSource, $"Could not bring the settings in '{ConfigFileName}' up from layout {version} to layout {UserConfigMigrations.CurrentVersion}: " +
				$"{ex.Message} They are used as they are, and the file is left unchanged.");
			return;
		}

		Values.Clear();

		foreach (KeyValuePair<string, string> entry in result.Values)
		{
			Values[entry.Key] = entry.Value;
		}

		string keptAs = BroughtForwardFilePath(version);

		try
		{
			File.Copy(path, keptAs, overwrite: true);
			WriteConfig(path, BuildTree(Values), UserConfigMigrations.CurrentVersion);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_stampVersion = version;
			AppLog.Warning(LogSource, $"Brought the settings up from layout {version} to layout {UserConfigMigrations.CurrentVersion}, but could not write " +
				$"'{ConfigFileName}': {ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		if (!DeleteIfPresent(PreviousFilePath))
		{
			AppLog.Warning(LogSource, $"Could not delete '{PreviousFileName}', so Undo last save may put back a setting in the old layout.");
		}

		AppLog.Info(LogSource, $"Brought the settings up from layout {version} to layout {UserConfigMigrations.CurrentVersion}: " +
			$"{string.Join("; ", result.Applied)}. The file as it was is kept as '{Path.GetFileName(keptAs)}'.");
	}

	private static JsonNode? GetNodeAtPath(JsonObject root, string dottedPath)
	{
		string[] segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
		JsonNode? current = root;

		foreach (string segment in segments)
		{
			if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out JsonNode? next))
			{
				return null;
			}

			current = next;
		}

		return current;
	}

	private static bool NodeExistsAtPath(JsonObject root, string dottedPath)
	{
		string[] segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
		JsonNode? current = root;

		foreach (string segment in segments)
		{
			if (current is not JsonObject obj || !obj.ContainsKey(segment))
			{
				return false;
			}

			current = obj[segment];
		}

		return true;
	}

	/// <summary>
	/// Sets (or replaces) the node at <paramref name="dottedPath"/>, creating intermediate
	/// objects as needed. A <see langword="null"/> <paramref name="value"/> removes the node.
	/// </summary>
	private static void SetNodeAtPath(JsonObject root, string dottedPath, JsonNode? value)
	{
		string[] segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

		if (segments.Length == 0)
		{
			return;
		}

		JsonObject parent = root;

		for (int i = 0; i < segments.Length - 1; i++)
		{
			string segment = segments[i];

			if (parent[segment] is not JsonObject child)
			{
				child = [];
				parent[segment] = child;
			}

			parent = child;
		}

		string leaf = segments[^1];

		if (value is null)
		{
			parent.Remove(leaf);
		}
		else
		{
			parent[leaf] = value;
		}
	}

	private static void RemoveNodeAtPath(JsonObject root, string dottedPath) =>
		SetNodeAtPath(root, dottedPath, value: null);
}
