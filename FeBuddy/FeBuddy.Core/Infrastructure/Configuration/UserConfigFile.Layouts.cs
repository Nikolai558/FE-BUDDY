using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Configuration.Models;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The layout (<see cref="UserConfigVersion"/>) of every settings file at once - each profile and
/// <c>Shared.json</c>: bringing them forward together, and setting aside what a newer FE-Buddy saved.
/// </summary>
/// <remarks>
/// <para>
/// An older FE-Buddy can't read a file a newer one saved in a later layout: a setting the newer one moved
/// or renamed would fall back to its default, and what is changed here would be lost on going forward
/// again. So <see cref="CheckLayouts"/> sets such a file aside - moved, never deleted, to
/// <c>%APPDATA%\FE-Buddy\Reverted\&lt;time&gt;\</c> - and puts back the newest backup in a layout this
/// FE-Buddy reads (<c>UserConfig-v&lt;N&gt;.&lt;Profile&gt;.json</c>, <c>Shared-v&lt;N&gt;.json</c>), or a fresh
/// file when there is none. The GUI says what it did, once (<see cref="TakeSetAside"/>).
/// </para>
/// <para>
/// Files in an older layout are brought forward together, so a step can move a setting between a profile
/// and <c>Shared.json</c> (<see cref="UserConfigMigrations.MigrateTogether(UserConfigMigratingFile?, IReadOnlyList{UserConfigMigratingFile})"/>).
/// Each is kept as it was - a <c>v&lt;N&gt;</c> backup - before it is written back in the current layout.
/// </para>
/// </remarks>
public static partial class UserConfigFile
{
	/// <summary>The folder in <c>%APPDATA%\FE-Buddy</c> that files set aside go to, each check's in a folder of its own.</summary>
	public const string RevertedFolderName = "Reverted";

	private const string SharedBackupPrefix = "Shared-v";

	/// <summary>A layout backup's kind, <c>v&lt;N&gt;</c>.</summary>
	private static readonly Regex LayoutBackupKind = new(@"^v(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	/// <summary>What the checks have set aside since the GUI last took it. Under <see cref="Gate"/>.</summary>
	private static readonly List<UserConfigSetAside> PendingSetAside = [];

	/// <summary>Raised, outside any lock, after a check set files aside: the GUI then takes them (<see cref="TakeSetAside"/>).</summary>
	public static event EventHandler? FilesSetAside;

	/// <summary>Where settings files set aside go: <c>%APPDATA%\FE-Buddy\Reverted</c>.</summary>
	public static string RevertedDirectory => Path.Combine(_root, RevertedFolderName);

	/// <summary>
	/// The full path <c>Shared.json</c> in an older layout is kept at once it has been brought forward, e.g.
	/// <c>Shared-v2.json</c>.
	/// </summary>
	/// <param name="version">The layout the file was in.</param>
	/// <returns>The path.</returns>
	public static string SharedBroughtForwardFilePath(int version) =>
		Path.Combine(Directory, string.Create(CultureInfo.InvariantCulture, $"{SharedBackupPrefix}{version}.json"));

	/// <summary>
	/// Checks the layout of <c>Shared.json</c> and of every profile: a file a newer FE-Buddy saved is set aside
	/// and replaced (see the remarks above), and the files in an older layout are brought forward together.
	/// Launch calls it before the settings are read; <see cref="ReadAll"/> calls it whenever <c>Shared.json</c>
	/// or the profile in use isn't in the current layout. Never throws: a file it can't move or write is used
	/// as it is, and tried again at the next read.
	/// </summary>
	public static void CheckLayouts()
	{
		bool setAside;

		lock (Gate)
		{
			setAside = CheckLayoutsLocked();
		}

		if (setAside)
		{
			FilesSetAside?.Invoke(null, EventArgs.Empty);
		}
	}

	/// <summary>Hands what the checks set aside to the GUI, once: every call after returns nothing new.</summary>
	/// <returns>What each check set aside, oldest first; empty when nothing was.</returns>
	public static IReadOnlyList<UserConfigSetAside> TakeSetAside()
	{
		lock (Gate)
		{
			UserConfigSetAside[] taken = [.. PendingSetAside];
			PendingSetAside.Clear();
			return taken;
		}
	}

	/// <summary>
	/// The profiles, and whether <c>Shared.json</c>, that a release of FE-Buddy can't read: saved in a layout
	/// newer than the one it reads (<see cref="UserConfigVersion.ForRelease"/>). For going back to it.
	/// </summary>
	/// <param name="version">The release, e.g. <c>3.0.0-beta.13</c>.</param>
	/// <returns>
	/// The profiles, in name order, and whether <c>Shared.json</c> is one; none for a release that keeps no
	/// <c>UserConfig</c> (FE-Buddy 2.x).
	/// </returns>
	public static (IReadOnlyList<string> Profiles, bool Shared) UnreadableBy(string version)
	{
		if (UserConfigVersion.ForRelease(version) is not { } layout)
		{
			return ([], false);
		}

		lock (Gate)
		{
			List<string> profiles = [.. ProfileNames().Where(profile => LayoutOf(ProfileFilePath(profile)) > layout).Order(StringComparer.OrdinalIgnoreCase)];
			return (profiles, LayoutOf(SharedFilePath) > layout);
		}
	}

	/// <summary>
	/// A profile's saved settings in the current layout, read from its file, whether or not it is the one in
	/// use: for exporting it. The shared settings aren't in it - an export leaves them out anyway.
	/// </summary>
	/// <param name="profile">The profile.</param>
	/// <returns>Its settings, by dotted path.</returns>
	/// <exception cref="ArgumentException">No profile has that name.</exception>
	/// <exception cref="IOException">Its file could not be read, or isn't settings.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	/// <exception cref="InvalidOperationException">It is in an older layout that couldn't be brought forward.</exception>
	public static IReadOnlyDictionary<string, string> ReadProfile(string profile)
	{
		string name = ExistingName(profile) ?? throw new ArgumentException($"There is no settings profile called '{profile}'.", nameof(profile));
		string path = ProfileFilePath(name);

		lock (Gate)
		{
			JsonObject root;

			try
			{
				root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new IOException($"'{Path.GetFileName(path)}' isn't a settings file.");
			}
			catch (JsonException ex)
			{
				throw new IOException($"'{Path.GetFileName(path)}' isn't a settings file: {ex.Message}", ex);
			}

			int version = TakeVersion(root, Path.GetFileName(path));
			Dictionary<string, string> values = new(StringComparer.Ordinal);
			FlattenInto(root, prefix: string.Empty, values);

			return version < UserConfigMigrations.CurrentVersion
				? UserConfigMigrations.Migrate(values, version).Values
				: values;
		}
	}

	/// <summary>
	/// Whether <c>Shared.json</c> or the profile in use is in a layout other than the current one, so a
	/// <see cref="CheckLayoutsLocked"/> is due. Reads both without logging. Call under <see cref="Gate"/>.
	/// </summary>
	private static bool NeedsLayoutCheck()
	{
		int current = UserConfigMigrations.CurrentVersion;

		if (!TryLoadForLayouts(SharedFilePath, out JsonObject? shared))
		{
			shared = null;
		}

		if (shared is not null && StampOf(shared) != current)
		{
			return true;
		}

		string active = PickProfile(shared is null ? null : ActiveProfileOf(shared), quietly: true);
		return TryLoadForLayouts(ProfileFilePath(active), out JsonObject? profile) && profile is not null && StampOf(profile) != current;
	}

	/// <summary>
	/// The check itself (<see cref="CheckLayouts"/>). Call under <see cref="Gate"/>.
	/// </summary>
	/// <returns>Whether it set any file aside.</returns>
	private static bool CheckLayoutsLocked()
	{
		int current = UserConfigMigrations.CurrentVersion;
		string? folder = null;
		List<UserConfigSetAsideFile> setAside = [];
		UserConfigSetAsideFile? sharedSetAside = null;

		// Shared.json first: it names the profile in use, which is brought forward first.
		bool sharedReadable = TryLoadForLayouts(SharedFilePath, out JsonObject? shared);
		int sharedVersion = shared is null ? current : TakeVersion(shared, SharedFileName);
		string? activeName = shared is null ? null : ActiveProfileOf(shared);

		if (shared is not null && sharedVersion > current)
		{
			JsonObject fresh = activeName is null ? [] : new() { [ActiveProfileKey] = activeName };

			if (TrySetAside(SharedFilePath, [], SharedLayoutBackups(), fresh, ref folder, out int? restored))
			{
				sharedSetAside = new UserConfigSetAsideFile(null, sharedVersion, restored);
				sharedReadable = TryLoadForLayouts(SharedFilePath, out shared);
				sharedVersion = shared is null ? current : TakeVersion(shared, SharedFileName);
			}
		}

		string active = PickProfile(activeName, quietly: true);
		List<(string Profile, string Path, UserConfigMigratingFile File)> behind = [];

		foreach (string profile in ProfileNames()
			.OrderBy(name => name.Equals(active, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
			.ThenBy(name => name, StringComparer.OrdinalIgnoreCase))
		{
			string path = ProfileFilePath(profile);

			// A profile that can't be read is left to ReadAll, which keeps a damaged one before writing over it.
			if (!TryLoadForLayouts(path, out JsonObject? root) || root is null)
			{
				continue;
			}

			int version = TakeVersion(root, Path.GetFileName(path));

			if (version > current)
			{
				// Its undo snapshot is in the newer layout too.
				if (!TrySetAside(path, [BackupFilePath(profile, "previous")], ProfileLayoutBackups(profile), [], ref folder, out int? restored))
				{
					continue;
				}

				ForgetUndoSnapshot();
				setAside.Add(new UserConfigSetAsideFile(profile, version, restored));

				if (!TryLoadForLayouts(path, out root) || root is null)
				{
					continue;
				}

				version = TakeVersion(root, Path.GetFileName(path));
			}

			if (version < current)
			{
				Dictionary<string, string> values = new(StringComparer.Ordinal);
				FlattenInto(root, prefix: string.Empty, values);
				behind.Add((profile, path, new UserConfigMigratingFile(Path.GetFileName(path), values, version)));
			}
		}

		BringForwardTogether(shared, sharedReadable, sharedVersion, activeName, behind);

		if (sharedSetAside is not null)
		{
			setAside.Add(sharedSetAside);
		}

		if (setAside.Count == 0)
		{
			return false;
		}

		PendingSetAside.Add(new UserConfigSetAside(folder!, setAside));
		AppLog.Warning(LogSource, $"Settings a newer FE-Buddy saved can't be read by this one, so they were set aside in '{folder}': " +
			string.Join("; ", setAside.Select(file => (file.Profile is null ? SharedFileName : $"profile '{file.Profile}'") + (file.RestoredLayout is { } layout
				? $" (layout {file.Layout}; its layout {layout} backup is back in its place)"
				: $" (layout {file.Layout}; no backup this FE-Buddy reads, so it starts on its defaults)"))) + ".");
		return true;
	}

	/// <summary>
	/// Brings <c>Shared.json</c> and the profiles in an older layout forward together, and writes each back -
	/// <c>Shared.json</c> first, so a profile that can't be written can only have lost a setting
	/// <c>Shared.json</c> already holds. Call under <see cref="Gate"/>.
	/// </summary>
	/// <param name="shared">
	/// <c>Shared.json</c> as read, its stamp taken out, or <see langword="null"/> when it is missing or can't be read.
	/// </param>
	/// <param name="sharedReadable">Whether it can be read, or is missing; <see langword="false"/> leaves it out altogether.</param>
	/// <param name="sharedVersion">Its layout.</param>
	/// <param name="activeName">The profile it names, written back as it was.</param>
	/// <param name="behind">The profiles in an older layout, the one in use first.</param>
	private static void BringForwardTogether(
		JsonObject? shared,
		bool sharedReadable,
		int sharedVersion,
		string? activeName,
		List<(string Profile, string Path, UserConfigMigratingFile File)> behind)
	{
		int current = UserConfigMigrations.CurrentVersion;

		if (sharedVersion >= current && behind.Count == 0)
		{
			return;
		}

		UserConfigMigratingFile? sharedFile = null;
		Dictionary<string, string> sharedAsRead = new(StringComparer.Ordinal);

		// One a newer FE-Buddy saved, which couldn't be set aside, is left as it is.
		if (sharedReadable && sharedVersion <= current)
		{
			if (shared is not null)
			{
				JsonObject settings = shared.DeepClone().AsObject();
				settings.Remove(ActiveProfileKey);
				FlattenInto(settings, prefix: string.Empty, sharedAsRead);
			}

			sharedFile = new UserConfigMigratingFile(SharedFileName, sharedAsRead, sharedVersion);
		}

		UserConfigMigrations.MigrateTogether(sharedFile, [.. behind.Select(entry => entry.File)]);

		if (sharedFile is not null)
		{
			WriteSharedBroughtForward(sharedFile, sharedAsRead, activeName);
		}

		foreach ((string profile, string path, UserConfigMigratingFile file) in behind)
		{
			WriteProfileBroughtForward(profile, path, file);
		}
	}

	/// <summary>Writes <c>Shared.json</c> back once brought forward, or changed by a profile's step; kept first as it was.</summary>
	private static void WriteSharedBroughtForward(UserConfigMigratingFile file, Dictionary<string, string> asRead, string? activeName)
	{
		int current = UserConfigMigrations.CurrentVersion;

		if (file.Error is { } error)
		{
			AppLog.Warning(LogSource, $"Could not bring the settings in '{SharedFileName}' up from layout {file.FromVersion} to layout {current}: " +
				$"{error.Message} They are used as they are, and the file is left unchanged.");
			return;
		}

		bool changedByProfiles = file.Values.Count != asRead.Count
			|| file.Values.Any(entry => !asRead.TryGetValue(entry.Key, out string? value) || value != entry.Value);

		if (!file.Migrated && !changedByProfiles)
		{
			return;
		}

		string keptAs = SharedBroughtForwardFilePath(file.FromVersion);

		try
		{
			if (file.Migrated && File.Exists(SharedFilePath))
			{
				File.Copy(SharedFilePath, keptAs, overwrite: true);
			}

			// Only the shared settings, as ever: what a step moved out of it goes with the profiles.
			JsonObject tree = BuildTree(file.Values.Where(entry => UserConfigPortability.SharedByProfiles.Contains(entry.Key))
				.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal));
			JsonObject root = activeName is null ? [] : new() { [ActiveProfileKey] = activeName };

			foreach (KeyValuePair<string, JsonNode?> entry in tree.ToList())
			{
				tree.Remove(entry.Key);
				root[entry.Key] = entry.Value;
			}

			WriteConfig(SharedFilePath, root, current);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Brought the settings in '{SharedFileName}' up from layout {file.FromVersion} to layout {current}, but could not write it: " +
				$"{ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		if (file.Migrated)
		{
			AppLog.Info(LogSource, $"Brought the settings in '{SharedFileName}' up from layout {file.FromVersion} to layout {current}: " +
				$"{string.Join("; ", file.Applied)}. The file as it was is kept as '{Path.GetFileName(keptAs)}'.");
		}
	}

	/// <summary>
	/// Writes a profile back once brought forward, kept first as it was (<c>UserConfig-v&lt;N&gt;.&lt;Profile&gt;.json</c>).
	/// Its per-node undo snapshot is of the old layout, so it goes.
	/// </summary>
	private static void WriteProfileBroughtForward(string profile, string path, UserConfigMigratingFile file)
	{
		int current = UserConfigMigrations.CurrentVersion;

		if (file.Error is { } error)
		{
			AppLog.Warning(LogSource, $"Could not bring the settings in '{file.FileName}' up from layout {file.FromVersion} to layout {current}: " +
				$"{error.Message} They are used as they are, and the file is left unchanged.");
			return;
		}

		if (!file.Migrated)
		{
			return;
		}

		string keptAs = BackupFilePath(profile, string.Create(CultureInfo.InvariantCulture, $"v{file.FromVersion}"));

		try
		{
			File.Copy(path, keptAs, overwrite: true);
			WriteConfig(path, BuildTree(ProfileValues(file.Values)), current);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Brought the settings up from layout {file.FromVersion} to layout {current}, but could not write " +
				$"'{file.FileName}': {ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		string previous = BackupFilePath(profile, "previous");

		if (!DeleteIfPresent(previous))
		{
			AppLog.Warning(LogSource, $"Could not delete '{Path.GetFileName(previous)}', so Undo last save may put back a setting in the old layout.");
		}

		ForgetUndoSnapshot();

		AppLog.Info(LogSource, $"Brought the settings up from layout {file.FromVersion} to layout {current}: " +
			$"{string.Join("; ", file.Applied)}. The file as it was is kept as '{Path.GetFileName(keptAs)}'.");
	}

	/// <summary>
	/// Moves a file a newer FE-Buddy saved - and the files that go with it - into this check's folder under
	/// <see cref="RevertedDirectory"/>, never over one already there, and puts back the newest of its
	/// <paramref name="backups"/> this FE-Buddy reads; or writes <paramref name="fresh"/>, stamped current,
	/// when there is none. Call under <see cref="Gate"/>.
	/// </summary>
	/// <param name="path">The file.</param>
	/// <param name="alongWith">Files that go with it, when they are there.</param>
	/// <param name="backups">Its layout backups.</param>
	/// <param name="fresh">What it starts as with no backup.</param>
	/// <param name="folder">This check's folder: made by the first file set aside, then shared.</param>
	/// <param name="restored">The layout of the backup put back, or <see langword="null"/> when <paramref name="fresh"/> was written.</param>
	/// <returns><see langword="false"/> when the file couldn't be moved: it is used as it is.</returns>
	private static bool TrySetAside(
		string path,
		IReadOnlyList<string> alongWith,
		IEnumerable<(int Layout, string Path)> backups,
		JsonObject fresh,
		ref string? folder,
		out int? restored)
	{
		restored = null;
		int current = UserConfigMigrations.CurrentVersion;

		try
		{
			folder ??= NewRevertedFolder();
			System.IO.Directory.CreateDirectory(folder);
			File.Move(path, Path.Combine(folder, Path.GetFileName(path)));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"'{Path.GetFileName(path)}' was saved by a newer FE-Buddy, but could not be set aside: {ex.Message} " +
				"It is used as it is, and FE-Buddy tries again at the next launch.");
			return false;
		}

		foreach (string extra in alongWith.Where(File.Exists))
		{
			MoveQuietly(extra, Path.Combine(folder, Path.GetFileName(extra)));
		}

		(int Layout, string Path)[] readable = [.. backups.Where(backup => backup.Layout <= current).OrderByDescending(backup => backup.Layout)];

		try
		{
			if (readable.Length > 0)
			{
				File.Copy(readable[0].Path, path);
				restored = readable[0].Layout;
			}
			else
			{
				WriteConfig(path, fresh, current);
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Set '{Path.GetFileName(path)}' aside, but could not put a copy this FE-Buddy reads in its place: {ex.Message}");
		}

		return true;
	}

	/// <summary>A folder under <see cref="RevertedDirectory"/> named for now, in UTC, that no earlier check has used.</summary>
	private static string NewRevertedFolder()
	{
		string stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HHmmss'Z'", CultureInfo.InvariantCulture);
		string folder = Path.Combine(RevertedDirectory, stamp);

		for (int n = 2; System.IO.Directory.Exists(folder); n++)
		{
			folder = Path.Combine(RevertedDirectory, string.Create(CultureInfo.InvariantCulture, $"{stamp} ({n})"));
		}

		return folder;
	}

	/// <summary>A profile's layout backups, <c>UserConfig-v&lt;N&gt;.&lt;Profile&gt;.json</c>, with their layouts.</summary>
	private static IEnumerable<(int Layout, string Path)> ProfileLayoutBackups(string profile) =>
		Backups(profile)
			.Select(backup => (Match: LayoutBackupKind.Match(backup.Kind), backup.Path))
			.Where(backup => backup.Match.Success)
			.Select(backup => (int.Parse(backup.Match.Groups[1].Value, CultureInfo.InvariantCulture), backup.Path));

	/// <summary><c>Shared.json</c>'s layout backups, <c>Shared-v&lt;N&gt;.json</c>, with their layouts.</summary>
	private static IEnumerable<(int Layout, string Path)> SharedLayoutBackups() =>
		System.IO.Directory.Exists(Directory)
			? System.IO.Directory.EnumerateFiles(Directory, SharedBackupPrefix + "*.json")
				.Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: path))
				.Where(file => file.Name.Length > SharedBackupPrefix.Length && file.Name[SharedBackupPrefix.Length..].All(char.IsAsciiDigit))
				.Select(file => (int.Parse(file.Name[SharedBackupPrefix.Length..], CultureInfo.InvariantCulture), file.Path))
			: [];

	/// <summary>
	/// Reads a settings file to check its layout: <see langword="true"/> with the file - or with
	/// <see langword="null"/> when it is missing - and <see langword="false"/> when it is there but can't be
	/// read as settings (locked, or damaged), which the read then deals with.
	/// </summary>
	private static bool TryLoadForLayouts(string path, out JsonObject? root)
	{
		root = null;

		if (!File.Exists(path))
		{
			return true;
		}

		try
		{
			root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
			return root is not null;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
		{
			return false;
		}
	}

	/// <summary>A settings file's layout without taking its stamp out or logging (<see cref="TakeVersion"/>): <see langword="null"/> when it is missing or can't be read.</summary>
	private static int? LayoutOf(string path) =>
		TryLoadForLayouts(path, out JsonObject? root) && root is not null ? StampOf(root) : null;

	/// <summary>A file's layout as <see cref="TakeVersion"/> reads it, without taking the stamp out or logging.</summary>
	private static int StampOf(JsonObject root) =>
		root.TryGetPropertyValue(UserConfigVersion.FileKey, out JsonNode? stamp) && stamp is JsonValue value && value.TryGetValue(out int version) && version > 0
			? Math.Max(version, UserConfigVersion.Oldest)
			: UserConfigVersion.Oldest;

	/// <summary>The profile <c>Shared.json</c> names as the one in use, or <see langword="null"/>.</summary>
	private static string? ActiveProfileOf(JsonObject shared) =>
		shared[ActiveProfileKey] is JsonValue value && value.TryGetValue(out string? name) ? name : null;
}
