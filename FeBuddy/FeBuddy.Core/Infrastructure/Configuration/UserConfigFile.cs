using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.FileSystem;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Reads and writes the settings files: the active settings profile,
/// <c>%APPDATA%\FE-Buddy\User Configurations\UserConfig.&lt;Profile&gt;.json</c>, which stores every
/// preference FE-Buddy needs between runs (selected AIRAC cycle, ARTCC ID, output directory, ROI,
/// GeoJSON options, CRC ERAM defaults, and so on), and <c>Shared.json</c> beside it, which holds the
/// few every profile shares.
/// </summary>
/// <remarks>
/// <para>
/// On disk each file is a nested JSON object. In memory the two are one flat dictionary keyed by
/// dotted path (e.g. <c>Services.AiracService.UserArtccId</c>), the same shape as the settings blocks
/// the sub-services read. Every leaf value is a string. A key in
/// <see cref="UserConfigPortability.SharedByProfiles"/> - the update channel, which News has been read,
/// FE-Buddy's GitHub token - is kept in <c>Shared.json</c>, with the name of the active profile; every
/// other key in the profile's file. Callers never need to know which.
/// </para>
/// <para>
/// Saves are per sub-service: <see cref="Save(string)"/> writes only the subtree at one node
/// path and leaves every sibling section untouched. Before each save the previous state of
/// that one subtree is snapshotted to <c>UserConfig-previous.&lt;Profile&gt;.json</c>, giving a single
/// level of undo per node (<see cref="CanUndo(string)"/> / <see cref="Undo(string)"/>). A second
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
/// it as <c>UserConfig-v&lt;old version&gt;.&lt;Profile&gt;.json</c>.
/// </para>
/// <para>
/// FE-Buddy 3.0.0-beta.3 and earlier kept one file, <c>%APPDATA%\FE-Buddy\UserConfig.json</c>. The
/// first read moves it in as the <see cref="DefaultProfile"/> profile, its shared settings into
/// <c>Shared.json</c> (see <c>UserConfigFile.Profiles.cs</c> for the profiles themselves).
/// </para>
/// </remarks>
public static partial class UserConfigFile
{
	/// <summary>The folder in <c>%APPDATA%\FE-Buddy</c> that holds every profile and <c>Shared.json</c>.</summary>
	public const string ProfilesFolderName = "User Configurations";

	/// <summary>The profile FE-Buddy starts with, and the one an older FE-Buddy's settings are moved into.</summary>
	public const string DefaultProfile = "Default";

	/// <summary>FE-Buddy 3.0.0-beta.3's one settings file, in <c>%APPDATA%\FE-Buddy</c>, which the first read moves in.</summary>
	public const string LegacyConfigFileName = "UserConfig.json";

	private const string LogSource = "UserConfig";
	private const string SharedFileName = "Shared.json";
	private const string ActiveProfileKey = "ActiveProfile";
	private const string ProfileFilePrefix = "UserConfig.";
	private const string ProfileFileExtension = ".json";

	/// <summary>
	/// The most dotted parts a key may have. FE-Buddy's own keys have fewer than ten; the limit keeps
	/// a settings file from another PC from nesting the JSON deeper than it can be written.
	/// </summary>
	private const int MaxKeyParts = 32;

	private static readonly Lock Gate = new();
	private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal);
	private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

	private static string _root = AppPaths.AppDataDirectory;
	private static string _profile = DefaultProfile;

	/// <summary>
	/// The layout every write stamps the file with: the current one, unless the file was read in a
	/// layout it could not be fully brought out of - an older one whose update could not be written
	/// (so the next launch tries again), or a newer one this FE-Buddy does not know.
	/// </summary>
	private static int _stampVersion = UserConfigVersion.Current;

	/// <summary>
	/// The layout every write stamps <c>Shared.json</c> with: the current one, unless the last read found
	/// it in a layout it could not be fully brought out of, as <see cref="_stampVersion"/> is for the profile.
	/// </summary>
	private static int _sharedStampVersion = UserConfigVersion.Current;

	/// <summary>
	/// Whether the last <see cref="ReadAll"/> found the active profile's file but couldn't read it (a
	/// lock, a permission) and had nothing of it in memory to keep, so the settings in memory are
	/// defaults, not the user's: <see cref="Write"/> then refuses, rather than replace the file with them.
	/// </summary>
	private static bool _profileUnreadable;

	/// <summary>
	/// The profile whose settings memory holds from a good read (or that has no file yet), or
	/// <see langword="null"/>. A later read that can't get at that profile's file keeps them.
	/// </summary>
	private static string? _profileInMemory;

	/// <summary>
	/// The undo snapshot (<see cref="PreviousFilePath"/>) as it is on disk, so <see cref="CanUndo"/> - which
	/// WPF asks on every key press and click - never reads the file. Read once per profile, then kept up to
	/// date wherever this class writes or deletes the file; <see langword="null"/> until then.
	/// </summary>
	private static JsonObject? _undoSnapshot;

	/// <summary>The file <see cref="_undoSnapshot"/> was read from or written to.</summary>
	private static string? _undoSnapshotPath;

	/// <summary>
	/// The directory holding every settings file: <c>%APPDATA%\FE-Buddy\User Configurations</c> by default.
	/// </summary>
	public static string Directory
	{
		get
		{
			lock (Gate)
			{
				return Path.Combine(_root, ProfilesFolderName);
			}
		}
	}

	/// <summary>The settings profile in use, e.g. <c>Default</c>.</summary>
	public static string ActiveProfile
	{
		get
		{
			lock (Gate)
			{
				return _profile;
			}
		}
	}

	/// <summary>The full path of the active profile's settings file, e.g. <c>UserConfig.Default.json</c>.</summary>
	public static string ConfigFilePath => ProfileFilePath(ActiveProfile);

	/// <summary>The full path of <c>Shared.json</c>: the settings every profile shares, and the active profile's name.</summary>
	public static string SharedFilePath => Path.Combine(Directory, SharedFileName);

	/// <summary>The full path of the active profile's one-step undo snapshot, <c>UserConfig-previous.&lt;Profile&gt;.json</c>.</summary>
	public static string PreviousFilePath => BackupFilePath(ActiveProfile, "previous");

	/// <summary>
	/// The full path of <c>UserConfig-before-import.&lt;Profile&gt;.json</c>: the active profile as it was
	/// before the last <see cref="ReplaceAll"/>, kept so an import can be taken back by hand.
	/// </summary>
	public static string BeforeImportFilePath => BackupFilePath(ActiveProfile, "before-import");

	/// <summary>
	/// The full path a profile in an older layout is kept at once it has been brought forward, e.g.
	/// <c>UserConfig-v1.Default.json</c>.
	/// </summary>
	/// <param name="version">The layout the file was in.</param>
	/// <returns>The path.</returns>
	public static string BroughtForwardFilePath(int version) =>
		BackupFilePath(ActiveProfile, string.Create(CultureInfo.InvariantCulture, $"v{version}"));

	/// <summary>The full path of a profile's settings file.</summary>
	/// <param name="profile">The profile's name.</param>
	/// <returns>e.g. <c>...\User Configurations\UserConfig.ZOB.json</c>.</returns>
	public static string ProfileFilePath(string profile) => Path.Combine(Directory, ProfileFilePrefix + profile + ProfileFileExtension);

	/// <summary>
	/// Reads the active profile and <c>Shared.json</c> from disk into the in-memory dictionary,
	/// replacing whatever was there. A missing or unreadable profile leaves only the shared settings
	/// and logs a warning rather than throwing - this is the launch read path. A file in an older
	/// layout is brought forward, and an older FE-Buddy's one file moved in (see the class remarks).
	/// </summary>
	/// <remarks>
	/// A profile already read that can't be read again for a moment (locked by antivirus or a sync tool)
	/// keeps what memory holds of it, rather than drop every setting to its default until the next read.
	/// </remarks>
	/// <returns>Whether the profile was read, missing or unreadable.</returns>
	public static UserConfigReadResult ReadAll()
	{
		lock (Gate)
		{
			Dictionary<string, string> held = new(Values, StringComparer.Ordinal);
			int heldStamp = _stampVersion;
			string? heldProfile = _profileInMemory;

			Values.Clear();
			_stampVersion = UserConfigMigrations.CurrentVersion;
			_profileUnreadable = false;
			_profileInMemory = null;

			MoveLegacyFileIn();

			SharedRead sharedRead = ReadShared();
			_profile = PickProfile(sharedRead.ActiveProfile);
			BringSharedForward(sharedRead);
			Dictionary<string, string> shared = sharedRead.Values;

			string path = ConfigFilePath;

			if (!File.Exists(path))
			{
				AddAll(shared);
				_profileInMemory = _profile;
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
					_profileInMemory = _profile;

					// A profile from before Shared.json (or copied in by hand) still holds them: Shared.json
					// takes any it lacks, and its own win over the profile's from then on.
					AdoptSharedSettings(shared);
					AddAll(shared);

					if (version < UserConfigMigrations.CurrentVersion)
					{
						BringForward(path, version);
					}
					else if (version > UserConfigMigrations.CurrentVersion)
					{
						// Saves keep its stamp, so the newer FE-Buddy never takes its own settings for old ones.
						_stampVersion = version;
						AppLog.Warning(LogSource, $"'{Path.GetFileName(path)}' was saved by a newer FE-Buddy (settings layout {version}; this one knows up to " +
							$"{UserConfigMigrations.CurrentVersion}). Settings it doesn't know are kept as they are.");
					}

					return UserConfigReadResult.Read;
				}

				Values.Clear();
				AddAll(shared);
				AppLog.Warning(LogSource, $"Config file '{path}' is not a JSON object. Using defaults.");
				return UserConfigReadResult.Unreadable;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
				&& string.Equals(heldProfile, _profile, StringComparison.OrdinalIgnoreCase))
			{
				Values.Clear();
				AddAll(held);
				_stampVersion = heldStamp;
				_profileInMemory = heldProfile;
				AppLog.Warning(LogSource, $"Could not read config file '{path}': {ex.Message}. Keeping the settings read before.");
				return UserConfigReadResult.Unreadable;
			}
			catch (Exception ex)
			{
				Values.Clear();
				AddAll(shared);
				_profileUnreadable = ex is IOException or UnauthorizedAccessException;
				AppLog.Warning(LogSource, $"Could not read config file '{path}': {ex.Message}. Using defaults.");
				return UserConfigReadResult.Unreadable;
			}
		}
	}

	/// <summary>
	/// Writes the entire in-memory dictionary - the active profile's settings to its file, the shared
	/// ones to <c>Shared.json</c> - then re-reads them so the dictionary reflects exactly what is on disk.
	/// </summary>
	/// <exception cref="IOException">
	/// A file could not be written or read, or the profile's file couldn't be read when the settings
	/// were last read, so memory holds defaults that would replace every setting in it. Nothing is written
	/// in that case.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">A file could not be written, for want of permission.</exception>
	public static void Write()
	{
		lock (Gate)
		{
			if (_profileUnreadable && File.Exists(ConfigFilePath))
			{
				throw new IOException(
					$"'{Path.GetFileName(ConfigFilePath)}' couldn't be read, so it wasn't written over: that would replace every setting in it.");
			}

			UpdateSharedFile(UserConfigPortability.SharedByProfiles);
			WriteConfig(ConfigFilePath, BuildTree(ProfileValues(Values)), _stampVersion);
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
	/// Replaces every setting of the active profile at once with <paramref name="values"/> and writes
	/// its whole file - the one write that is not per node, used by a settings import. The values must
	/// be in the current layout (<see cref="UserConfigTransfer.Read(string)"/> brings an import's
	/// forward). <c>Shared.json</c> is left as it is: the shared settings in <paramref name="values"/>
	/// are not written.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The new file is written beside the old one first, then swapped in with
	/// <see cref="File.Replace(string, string, string?)"/>, which keeps the file being replaced as
	/// <see cref="BeforeImportFilePath"/> in the same step. So a write that fails - a full disk, say -
	/// leaves the profile, and the settings in memory, exactly as they were.
	/// </para>
	/// <para>
	/// Once the new file is in place, the per-node undo snapshots are deleted: they describe settings
	/// that no longer exist, so an "undo last save" would otherwise put a pre-import subtree back into
	/// the imported file. That, and re-reading the new file, always happen once the swap has. Keys
	/// that <see cref="TrySetValue"/> would refuse are dropped.
	/// </para>
	/// </remarks>
	/// <param name="values">Every setting the profile should hold afterwards, by dotted path.</param>
	/// <exception cref="IOException">The new file could not be written or swapped in; nothing was changed.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void ReplaceAll(IReadOnlyDictionary<string, string> values)
	{
		ArgumentNullException.ThrowIfNull(values);

		Dictionary<string, string> replacement = ValidValues(values);

		lock (Gate)
		{
			string incoming = ConfigFilePath + ".importing";

			try
			{
				WriteConfig(incoming, BuildTree(ProfileValues(replacement)), UserConfigMigrations.CurrentVersion);

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
				AppLog.Warning(LogSource, $"Could not delete '{Path.GetFileName(PreviousFilePath)}', so Undo last save may put back a setting from before the import.");
			}

			ForgetUndoSnapshot();
		}

		ReadAll();

		AppLog.Info(LogSource, $"Replaced every setting of profile '{ActiveProfile}' ({replacement.Count} values); the previous file is kept as '{Path.GetFileName(BeforeImportFilePath)}'.");
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
	/// Persists only the subtree under <paramref name="nodePath"/>, leaving every sibling section on
	/// disk untouched: the profile's part to its file, and any shared setting under it to
	/// <c>Shared.json</c>. The previous on-disk state of the profile's subtree is first snapshotted to
	/// <see cref="PreviousFilePath"/> so it can be restored once with <see cref="Undo(string)"/>. After
	/// the write the in-memory dictionary is refreshed from disk.
	/// </summary>
	/// <param name="nodePath">
	/// The dotted path of the node to save, e.g. <c>Services.AiracService.Airways</c>.
	/// </param>
	/// <exception cref="IOException">
	/// A file could not be written, or is there but could not be read (written back, it would lose every
	/// other setting in it). The setting stays in memory, unsaved.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void Save(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			throw new ArgumentException("A node path is required.", nameof(nodePath));
		}

		lock (Gate)
		{
			WriteNode(nodePath);
		}

		ReadAll();

		AppLog.Info(LogSource, $"Saved '{nodePath}'.");
	}

	/// <summary>
	/// Sets <paramref name="values"/> in memory and saves <paramref name="nodePath"/> with them, as one step -
	/// for a save made off the UI thread, such as the map's after a click. Unlike <see cref="Save(string)"/>,
	/// it doesn't read the files again afterwards: that would drop a setting another page has set but not
	/// yet saved, while this ran.
	/// </summary>
	/// <param name="nodePath">The dotted path of the node to save, e.g. <c>Services.MapService</c>.</param>
	/// <param name="values">The settings to set first, by dotted path, each under <paramref name="nodePath"/>.</param>
	/// <param name="forProfile">
	/// The profile they were chosen in: when another is in use by the time this runs, nothing is set or saved.
	/// </param>
	/// <param name="quietly"><see langword="true"/> to log the save at Debug level, for settings saved on every click.</param>
	/// <returns><see langword="false"/> when another profile is in use, so nothing was saved.</returns>
	/// <exception cref="ArgumentException">A key is not one <see cref="TrySetValue"/> takes, or not under the node.</exception>
	/// <exception cref="IOException">
	/// A file could not be written, or is there but could not be read. Nothing is set in memory.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static bool SaveValues(string nodePath, IReadOnlyDictionary<string, string> values, string forProfile, bool quietly = false)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			throw new ArgumentException("A node path is required.", nameof(nodePath));
		}

		ArgumentNullException.ThrowIfNull(values);
		ArgumentNullException.ThrowIfNull(forProfile);

		if (values.Keys.FirstOrDefault(key => !IsValidKey(key) || !key.StartsWith(nodePath + ".", StringComparison.Ordinal)) is { } wrong)
		{
			throw new ArgumentException($"'{wrong}' isn't a setting under '{nodePath}'.", nameof(values));
		}

		lock (Gate)
		{
			if (!string.Equals(_profile, forProfile, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			Dictionary<string, string?> before = values.Keys.ToDictionary(key => key, key => Values.GetValueOrDefault(key), StringComparer.Ordinal);

			foreach (KeyValuePair<string, string> value in values)
			{
				Values[value.Key] = value.Value ?? string.Empty;
			}

			try
			{
				WriteNode(nodePath);
			}
			catch
			{
				// Nothing is taken for saved: memory goes back to what the file still holds.
				foreach (KeyValuePair<string, string?> value in before)
				{
					if (value.Value is null)
					{
						Values.Remove(value.Key);
					}
					else
					{
						Values[value.Key] = value.Value;
					}
				}

				throw;
			}
		}

		AppLog.Write(quietly ? LogLevel.Debug : LogLevel.Info, LogSource, $"Saved '{nodePath}'.");
		return true;
	}

	/// <summary>
	/// Writes the node's subtree from memory to the profile's file (and any shared setting under it to
	/// <c>Shared.json</c>), after keeping the file's own as the node's undo snapshot. Call under <see cref="Gate"/>.
	/// </summary>
	private static void WriteNode(string nodePath)
	{
		JsonObject onDisk = LoadObjectToUpdate(ConfigFilePath);

		// Snapshot the CURRENT on-disk subtree into the previous file, at the same path.
		JsonObject previous = LoadObjectOrEmpty(PreviousFilePath);
		JsonNode? currentSubtree = GetNodeAtPath(onDisk, nodePath);
		SetNodeAtPath(previous, nodePath, currentSubtree?.DeepClone());
		WriteObject(PreviousFilePath, previous);
		KeepUndoSnapshot(previous);

		// Replace the subtree on disk with the in-memory version built from the flat dict.
		JsonNode? newSubtree = BuildSubtree(ProfileValues(Values), nodePath);
		SetNodeAtPath(onDisk, nodePath, newSubtree);
		RemoveSharedSettings(onDisk);

		string below = nodePath + ".";
		string[] sharedHere = [.. UserConfigPortability.SharedByProfiles.Where(key => key == nodePath || key.StartsWith(below, StringComparison.Ordinal))];

		if (sharedHere.Length > 0)
		{
			UpdateSharedFile(sharedHere);
		}

		WriteConfig(ConfigFilePath, onDisk, _stampVersion);
	}

	/// <summary>
	/// One setting every profile shares, read straight from <c>Shared.json</c> rather than from memory:
	/// for what FE-Buddy needs before its launch has read the settings (<see cref="ReadAll"/>), such as
	/// where to open its window. Never throws.
	/// </summary>
	/// <param name="key">A key in <see cref="UserConfigPortability.SharedByProfiles"/>.</param>
	/// <returns>Its value, or <see langword="null"/> when it is not set or is not a shared setting.</returns>
	public static string? ReadSharedValue(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}

		lock (Gate)
		{
			return ReadShared().Values.GetValueOrDefault(key);
		}
	}

	/// <summary>
	/// Persists settings every profile shares (<see cref="UserConfigPortability.SharedByProfiles"/>) to
	/// <c>Shared.json</c> only, as they are in memory - removed when unset. Unlike
	/// <see cref="Save(string)"/>, it leaves the profile's file and its undo snapshot alone and does not
	/// read the files again, so a setting set but not yet saved elsewhere stays as it is; and it keeps
	/// the active profile the file names, so it is safe before the launch has read the settings. For
	/// the settings FE-Buddy keeps as it goes, such as where its window was when it closed.
	/// </summary>
	/// <param name="keys">The shared settings to write.</param>
	/// <exception cref="ArgumentException">A key is not one every profile shares.</exception>
	public static void SaveShared(params string[] keys)
	{
		ArgumentNullException.ThrowIfNull(keys);

		if (keys.FirstOrDefault(key => !UserConfigPortability.SharedByProfiles.Contains(key)) is { } notShared)
		{
			throw new ArgumentException($"'{notShared}' is not a setting every profile shares.", nameof(keys));
		}

		lock (Gate)
		{
			UpdateSharedFile(keys, keepActiveProfile: true);
		}

		AppLog.Debug(LogSource, $"Saved {string.Join(", ", keys.Select(key => $"'{key}'"))} to '{SharedFileName}'.");
	}

	/// <summary>
	/// Indicates whether <see cref="Undo(string)"/> can restore a previous value for
	/// <paramref name="nodePath"/> - i.e. a snapshot for that exact node exists.
	/// </summary>
	/// <remarks>
	/// Answers from memory: the snapshot is read once per profile, and kept up to date by
	/// <see cref="Save(string)"/>, <see cref="Undo(string)"/> and the writes that delete it. WPF asks on
	/// every key press and click, so this must not touch the disk.
	/// </remarks>
	/// <param name="nodePath">The dotted path that was previously saved.</param>
	/// <returns><see langword="true"/> if a one-step undo is available for that node.</returns>
	public static bool CanUndo(string nodePath)
	{
		if (string.IsNullOrWhiteSpace(nodePath))
		{
			return false;
		}

		lock (Gate)
		{
			return NodeExistsAtPath(UndoSnapshot(), nodePath);
		}
	}

	/// <summary>
	/// Restores the one snapshotted previous state of <paramref name="nodePath"/> to the active
	/// profile and consumes the snapshot, so a further undo of the same node is not possible until it
	/// is saved again. A no-op (returns <see langword="false"/>) when there is no snapshot for that
	/// node. Shared settings have no undo.
	/// </summary>
	/// <param name="nodePath">The dotted path to restore.</param>
	/// <returns><see langword="true"/> if a previous value was restored.</returns>
	/// <exception cref="IOException">
	/// The profile's file could not be written, or is there but could not be read. Nothing was restored
	/// unless the profile was written and only the snapshot couldn't be updated.
	/// </exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
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

			JsonObject onDisk = LoadObjectToUpdate(ConfigFilePath);
			SetNodeAtPath(onDisk, nodePath, snapshot?.DeepClone());
			RemoveSharedSettings(onDisk);
			WriteConfig(ConfigFilePath, onDisk, _stampVersion);

			// Consume the snapshot: undo depth is exactly one.
			RemoveNodeAtPath(previous, nodePath);
			WriteObject(PreviousFilePath, previous);
			KeepUndoSnapshot(previous);
		}

		ReadAll();

		AppLog.Info(LogSource, $"Reverted '{nodePath}' to its previous saved value.");
		return true;
	}

	/// <summary>
	/// Points the settings somewhere else (the folder that stands for <c>%APPDATA%\FE-Buddy</c>), back on
	/// the <see cref="DefaultProfile"/> profile, and clears in-memory state. Unit tests only.
	/// </summary>
	/// <param name="directory">A throwaway directory, or <see langword="null"/> to restore the default.</param>
	internal static void ConfigureForTesting(string? directory)
	{
		lock (Gate)
		{
			Values.Clear();
			_root = directory ?? AppPaths.AppDataDirectory;
			_profile = DefaultProfile;
			_stampVersion = UserConfigMigrations.CurrentVersion;
			_sharedStampVersion = UserConfigMigrations.CurrentVersion;
			_profileUnreadable = false;
			_profileInMemory = null;
			ForgetUndoSnapshot();
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

	/// <summary>The settings a profile's own file holds: all but the shared ones.</summary>
	private static Dictionary<string, string> ProfileValues(IReadOnlyDictionary<string, string> values) =>
		values.Where(entry => !UserConfigPortability.SharedByProfiles.Contains(entry.Key))
			.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

	/// <summary>The values whose keys <see cref="TrySetValue"/> would take.</summary>
	private static Dictionary<string, string> ValidValues(IReadOnlyDictionary<string, string> values)
	{
		Dictionary<string, string> valid = new(StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> entry in values)
		{
			if (IsValidKey(entry.Key))
			{
				valid[entry.Key] = entry.Value ?? string.Empty;
			}
		}

		return valid;
	}

	private static void AddAll(Dictionary<string, string> values)
	{
		foreach (KeyValuePair<string, string> entry in values)
		{
			Values[entry.Key] = entry.Value;
		}
	}

	/// <summary>What <see cref="ReadShared"/> found in <c>Shared.json</c>.</summary>
	/// <param name="Values">The shared settings, in the current layout unless they couldn't be brought to it.</param>
	/// <param name="ActiveProfile">The name of the active profile, or <see langword="null"/>.</param>
	/// <param name="Version">The layout the file was in: the current one when it is missing, empty or unreadable.</param>
	/// <param name="Applied">The steps that brought its settings forward; empty when none ran.</param>
	/// <param name="MigrationError">Why its settings couldn't be brought forward, or <see langword="null"/>.</param>
	private sealed record SharedRead(
		Dictionary<string, string> Values,
		string? ActiveProfile,
		int Version,
		IReadOnlyList<string> Applied,
		string? MigrationError);

	/// <summary>
	/// Reads <c>Shared.json</c>: the shared settings in it, brought up to the current layout in memory
	/// (<see cref="UserConfigMigrations"/>), and the name of the active profile. A missing or unreadable
	/// file has neither. Nothing is written: <see cref="BringSharedForward"/> does that at launch.
	/// </summary>
	private static SharedRead ReadShared()
	{
		JsonObject root = LoadObjectOrEmpty(SharedFilePath);

		if (root.Count == 0)
		{
			return new SharedRead(new(StringComparer.Ordinal), null, UserConfigMigrations.CurrentVersion, [], null);
		}

		int version = TakeVersion(root, SharedFileName);

		string? activeProfile = root[ActiveProfileKey] is JsonValue value && value.TryGetValue(out string? name) ? name : null;
		root.Remove(ActiveProfileKey);

		Dictionary<string, string> all = new(StringComparer.Ordinal);
		FlattenInto(root, prefix: string.Empty, all);

		IReadOnlyList<string> applied = [];
		string? error = null;

		// Brought forward before the shared keys are picked out, so a step that renames one is followed.
		if (version < UserConfigMigrations.CurrentVersion)
		{
			try
			{
				UserConfigMigrationResult result = UserConfigMigrations.Migrate(all, version);
				all = new(result.Values, StringComparer.Ordinal);
				applied = result.Applied;
			}
			catch (InvalidOperationException ex)
			{
				error = ex.Message;
			}
		}

		Dictionary<string, string> shared = all.Where(entry => UserConfigPortability.SharedByProfiles.Contains(entry.Key))
			.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

		return new SharedRead(shared, activeProfile, version, applied, error);
	}

	/// <summary>
	/// Writes <c>Shared.json</c> back in the current layout once <see cref="ReadShared"/> has brought its
	/// settings up from an older one, as <see cref="BringForward"/> does the profile. A file saved by a
	/// newer FE-Buddy, or one whose update can't be worked out, is used as it is, left alone and keeps
	/// its stamp; one whose update can't be written keeps its old stamp, so the next launch tries again.
	/// Never throws.
	/// </summary>
	private static void BringSharedForward(SharedRead read)
	{
		_sharedStampVersion = UserConfigMigrations.CurrentVersion;

		if (read.Version > UserConfigMigrations.CurrentVersion)
		{
			_sharedStampVersion = read.Version;
			AppLog.Warning(LogSource, $"'{SharedFileName}' was saved by a newer FE-Buddy (settings layout {read.Version}; this one knows up to " +
				$"{UserConfigMigrations.CurrentVersion}). Settings it doesn't know are kept as they are.");
			return;
		}

		if (read.MigrationError is { } error)
		{
			_sharedStampVersion = read.Version;
			AppLog.Warning(LogSource, $"Could not bring the settings in '{SharedFileName}' up from layout {read.Version} to layout " +
				$"{UserConfigMigrations.CurrentVersion}: {error} They are used as they are, and the file is left unchanged.");
			return;
		}

		if (read.Version == UserConfigMigrations.CurrentVersion)
		{
			return;
		}

		try
		{
			// The active profile's name is kept as the file had it, not as PickProfile read it.
			JsonObject tree = BuildTree(read.Values);
			JsonObject root = read.ActiveProfile is { } activeProfile ? new() { [ActiveProfileKey] = activeProfile } : [];

			foreach (KeyValuePair<string, JsonNode?> entry in tree.ToList())
			{
				tree.Remove(entry.Key);
				root[entry.Key] = entry.Value;
			}

			WriteConfig(SharedFilePath, root, UserConfigMigrations.CurrentVersion);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_sharedStampVersion = read.Version;
			AppLog.Warning(LogSource, $"Brought the settings in '{SharedFileName}' up from layout {read.Version} to layout " +
				$"{UserConfigMigrations.CurrentVersion}, but could not write it: {ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		AppLog.Info(LogSource, $"Brought the settings in '{SharedFileName}' up from layout {read.Version} to layout " +
			$"{UserConfigMigrations.CurrentVersion}: {string.Join("; ", read.Applied)}.");
	}

	/// <summary>
	/// Writes <paramref name="keys"/> to <c>Shared.json</c> as they are in memory - removed when unset -
	/// with the active profile's name, leaving every other shared setting there as it is.
	/// </summary>
	/// <param name="keys">The shared settings to write.</param>
	/// <param name="keepActiveProfile">
	/// Whether to keep the active profile's name the file already has, for a write that may come before
	/// the settings are read, when the profile in memory is not yet the active one.
	/// </param>
	private static void UpdateSharedFile(IEnumerable<string> keys, bool keepActiveProfile = false)
	{
		JsonObject shared = LoadObjectToUpdate(SharedFilePath);
		shared.Remove(UserConfigVersion.FileKey);

		string profile = keepActiveProfile && shared[ActiveProfileKey] is JsonValue onDisk && onDisk.TryGetValue(out string? name)
			? name
			: _profile;

		shared.Remove(ActiveProfileKey);

		foreach (string key in keys)
		{
			if (Values.TryGetValue(key, out string? value))
			{
				SetNodeAtPath(shared, key, JsonValue.Create(value));
			}
			else
			{
				RemoveLeaf(shared, key);
			}
		}

		JsonObject root = new() { [ActiveProfileKey] = profile };

		foreach (KeyValuePair<string, JsonNode?> entry in shared.ToList())
		{
			shared.Remove(entry.Key);
			root[entry.Key] = entry.Value;
		}

		WriteConfig(SharedFilePath, root, _sharedStampVersion);
	}

	/// <summary>
	/// Moves the shared settings a profile holds and <c>Shared.json</c> lacks into <c>Shared.json</c>: a
	/// profile written before it existed - the file an older FE-Buddy kept - or copied in by hand. The
	/// profile keeps its copies until its next save; <c>Shared.json</c>'s win from now on. Never throws.
	/// </summary>
	private static void AdoptSharedSettings(Dictionary<string, string> shared)
	{
		string[] adopt = [.. Values.Keys.Where(key => UserConfigPortability.SharedByProfiles.Contains(key) && !shared.ContainsKey(key))];

		if (adopt.Length == 0)
		{
			return;
		}

		foreach (string key in adopt)
		{
			shared[key] = Values[key];
		}

		try
		{
			UpdateSharedFile(adopt);
			AppLog.Info(LogSource, $"Moved {adopt.Length} setting(s) every profile shares into '{SharedFileName}'.");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not write '{SharedFileName}': {ex.Message} FE-Buddy tries again at the next launch.");
		}
	}

	/// <summary>Takes the shared settings out of a profile's tree, so they are only ever in <c>Shared.json</c>.</summary>
	private static void RemoveSharedSettings(JsonObject profile)
	{
		foreach (string key in UserConfigPortability.SharedByProfiles)
		{
			RemoveLeaf(profile, key);
		}
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

	/// <summary>
	/// Reads a settings file that is about to be written back with a change: empty when it is missing
	/// or isn't valid JSON (writing replaces it), but an error when it is there and can't be read - locked
	/// for a moment by antivirus or a sync tool, say - since writing back what little was read would
	/// lose every other setting in it.
	/// </summary>
	/// <exception cref="IOException">The file is there but could not be read.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	private static JsonObject LoadObjectToUpdate(string path)
	{
		if (!File.Exists(path))
		{
			return [];
		}

		string json = File.ReadAllText(path);

		try
		{
			return JsonNode.Parse(json) as JsonObject ?? [];
		}
		catch (JsonException)
		{
			return [];
		}
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

	/// <summary>The active profile's undo snapshot, read from disk only the first time it is asked for. Call under <see cref="Gate"/>.</summary>
	private static JsonObject UndoSnapshot()
	{
		string path = PreviousFilePath;

		if (_undoSnapshot is null || !string.Equals(_undoSnapshotPath, path, StringComparison.Ordinal))
		{
			_undoSnapshot = LoadObjectOrEmpty(path);
			_undoSnapshotPath = path;
		}

		return _undoSnapshot;
	}

	/// <summary>
	/// Keeps what was just written to <see cref="PreviousFilePath"/>, as reading it back would give it. Call
	/// under <see cref="Gate"/>, after the write.
	/// </summary>
	private static void KeepUndoSnapshot(JsonObject written)
	{
		_undoSnapshot = JsonNode.Parse(written.ToJsonString()) as JsonObject;
		_undoSnapshotPath = PreviousFilePath;
	}

	/// <summary>Has the next <see cref="CanUndo"/> read the snapshot again: it was deleted, or couldn't be. Call under <see cref="Gate"/>.</summary>
	private static void ForgetUndoSnapshot()
	{
		_undoSnapshot = null;
		_undoSnapshotPath = null;
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
			AppLog.Warning(LogSource, $"Could not bring the settings in '{Path.GetFileName(path)}' up from layout {version} to layout {UserConfigMigrations.CurrentVersion}: " +
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
			WriteConfig(path, BuildTree(ProfileValues(Values)), UserConfigMigrations.CurrentVersion);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_stampVersion = version;
			AppLog.Warning(LogSource, $"Brought the settings up from layout {version} to layout {UserConfigMigrations.CurrentVersion}, but could not write " +
				$"'{Path.GetFileName(path)}': {ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		if (!DeleteIfPresent(PreviousFilePath))
		{
			AppLog.Warning(LogSource, $"Could not delete '{Path.GetFileName(PreviousFilePath)}', so Undo last save may put back a setting in the old layout.");
		}

		ForgetUndoSnapshot();

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

	/// <summary>Removes a leaf, and any object it leaves empty, without creating anything on the way.</summary>
	private static void RemoveLeaf(JsonObject root, string dottedPath)
	{
		string[] segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
		List<JsonObject> parents = [root];

		for (int i = 0; i < segments.Length - 1; i++)
		{
			if (parents[^1][segments[i]] is not JsonObject child)
			{
				return;
			}

			parents.Add(child);
		}

		parents[^1].Remove(segments[^1]);

		for (int i = parents.Count - 1; i > 0 && parents[i].Count == 0; i--)
		{
			parents[i - 1].Remove(segments[i - 1]);
		}
	}
}
