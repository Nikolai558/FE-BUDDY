using System.Text.Json;
using System.Text.Json.Nodes;

using FEBuddyLibrary.Services.General;

namespace FEBuddyLibrary.Helpers;

/// <summary>
/// Reads and writes <c>%APPDATA%\FE-Buddy\UserConfig.json</c>, the file that stores every
/// user preference FE-Buddy needs between runs (selected AIRAC cycle, ARTCC ID, output
/// directory, ROI, GeoJSON options, CRC ERAM defaults, and so on).
/// </summary>
/// <remarks>
/// <para>
/// On disk the file is a nested JSON object mirroring the tree in <c>Developer_Notes.md</c>.
/// In memory it is exposed as a flat dictionary keyed by dotted path
/// (e.g. <c>Services.AiracService.UserArtccId</c>), because that is the shape the GUI hands
/// to <c>AirwaySettingsParser</c>. Every leaf value is a string.
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
/// </remarks>
public static class UserConfigFile
{
	private const string LogSource = "UserConfig";
	private const string ConfigFileName = "UserConfig.json";
	private const string PreviousFileName = "UserConfig.previous.json";

	private static readonly object _gate = new();
	private static readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
	private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

	private static string _directory = GetDefaultDirectory();

	/// <summary>
	/// The directory holding <c>UserConfig.json</c>: <c>%APPDATA%\FE-Buddy</c> by default.
	/// </summary>
	public static string Directory
	{
		get
		{
			lock (_gate)
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
	/// Reads <c>UserConfig.json</c> from disk into the in-memory dictionary, replacing whatever
	/// was there. A missing or unreadable file leaves the dictionary empty and logs a warning
	/// rather than throwing - this is the launch read path.
	/// </summary>
	public static void ReadAll()
	{
		lock (_gate)
		{
			_values.Clear();

			string path = ConfigFilePath;

			if (!File.Exists(path))
			{
				AppLog.Warning(LogSource, $"No config file at '{path}'. Using defaults for every setting.");
				return;
			}

			try
			{
				string json = File.ReadAllText(path);
				JsonNode? root = JsonNode.Parse(json);

				if (root is JsonObject obj)
				{
					FlattenInto(obj, prefix: string.Empty, _values);
				}
				else
				{
					AppLog.Warning(LogSource, $"Config file '{path}' is not a JSON object. Using defaults.");
				}
			}
			catch (Exception ex)
			{
				_values.Clear();
				AppLog.Warning(LogSource, $"Could not read config file '{path}': {ex.Message}. Using defaults.");
			}
		}
	}

	/// <summary>
	/// Writes the entire in-memory dictionary to <c>UserConfig.json</c> as a nested JSON tree,
	/// then re-reads it so the dictionary reflects exactly what is on disk.
	/// </summary>
	public static void Write()
	{
		lock (_gate)
		{
			JsonObject root = BuildTree(_values);
			WriteObject(ConfigFilePath, root);
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

		lock (_gate)
		{
			if (_values.TryGetValue(dottedPath, out string? value))
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
		if (string.IsNullOrWhiteSpace(dottedPath) || dottedPath.StartsWith('.') || dottedPath.EndsWith('.') || dottedPath.Contains(".."))
		{
			return false;
		}

		lock (_gate)
		{
			_values[dottedPath] = value ?? string.Empty;
		}

		return true;
	}

	/// <summary>
	/// Persists only the subtree under <paramref name="nodePath"/> to <c>UserConfig.json</c>,
	/// leaving every sibling section on disk untouched. The previous on-disk state of that one
	/// subtree is first snapshotted to <c>UserConfig.previous.json</c> so it can be restored
	/// once with <see cref="Undo(string)"/>. After the write the in-memory dictionary is
	/// refreshed from disk.
	/// </summary>
	/// <param name="nodePath">
	/// The dotted path of the node to save, e.g. <c>Services.AiracService.Geojson.Airways</c>.
	/// </param>
	public static void Save(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			throw new ArgumentException("A node path is required.", nameof(nodePath));
		}

		lock (_gate)
		{
			JsonObject onDisk = LoadObjectOrEmpty(ConfigFilePath);

			// Snapshot the CURRENT on-disk subtree into the previous file, at the same path.
			JsonObject previous = LoadObjectOrEmpty(PreviousFilePath);
			JsonNode? currentSubtree = GetNodeAtPath(onDisk, nodePath);
			SetNodeAtPath(previous, nodePath, currentSubtree?.DeepClone());
			WriteObject(PreviousFilePath, previous);

			// Replace the subtree on disk with the in-memory version built from the flat dict.
			JsonNode? newSubtree = BuildSubtree(_values, nodePath);
			SetNodeAtPath(onDisk, nodePath, newSubtree);
			WriteObject(ConfigFilePath, onDisk);
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

		lock (_gate)
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

		lock (_gate)
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
			WriteObject(ConfigFilePath, onDisk);

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
		lock (_gate)
		{
			_values.Clear();
			_directory = directory ?? GetDefaultDirectory();
		}
	}

	/// <summary>
	/// A snapshot of the current in-memory dictionary. Unit tests only.
	/// </summary>
	internal static IReadOnlyDictionary<string, string> SnapshotValues()
	{
		lock (_gate)
		{
			return new Dictionary<string, string>(_values, StringComparer.Ordinal);
		}
	}

	private static string GetDefaultDirectory() =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FE-Buddy");

	/// <summary>
	/// Recursively walks a JSON object, adding every leaf (non-object) it reaches to
	/// <paramref name="dest"/> keyed by its dotted path. Arrays and nulls are stored as their
	/// JSON text; every other scalar is stored as its string form.
	/// </summary>
	private static void FlattenInto(JsonObject obj, string prefix, Dictionary<string, string> dest)
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

	/// <summary>Builds a full nested JSON object from every entry in the flat dictionary.</summary>
	private static JsonObject BuildTree(IReadOnlyDictionary<string, string> values)
	{
		JsonObject root = new();

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
		JsonObject subtree = new();
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
			return new JsonObject();
		}

		try
		{
			return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
		}
		catch
		{
			return new JsonObject();
		}
	}

	private static void WriteObject(string path, JsonObject root)
	{
		System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, root.ToJsonString(_writeOptions));
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
				child = new JsonObject();
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
