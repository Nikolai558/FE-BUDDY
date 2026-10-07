using System.Globalization;
using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Settings profiles (issue #324): each a whole set of settings in its own file,
/// <c>User Configurations\UserConfig.&lt;Profile&gt;.json</c>, one of them in use at a time. The few
/// settings every profile shares, and which profile is in use, are in <c>Shared.json</c> beside them.
/// </summary>
/// <remarks>
/// <para>
/// A profile's name is used in its file's name, so it follows Windows' rules for one
/// (<see cref="ProfileNameProblem"/>), and two names that differ only in case are the same profile.
/// Its backups - the undo snapshot, the file before an import, a file brought forward from an older
/// layout - are named <c>UserConfig-&lt;kind&gt;.&lt;Profile&gt;.json</c>, so they can never be mistaken
/// for a profile, and they move and go with it.
/// </para>
/// </remarks>
public static partial class UserConfigFile
{
	/// <summary>The longest profile name allowed.</summary>
	public const int MaxProfileNameLength = 64;

	private static readonly char[] InvalidProfileNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

	// The device names Windows will not give a file, with any extension.
	private static readonly HashSet<string> ReservedNames = new(
		["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(1, 9).SelectMany(n => new[] { $"COM{n}", $"LPT{n}" })],
		StringComparer.OrdinalIgnoreCase);

	/// <summary>The backups an older FE-Buddy kept beside <c>UserConfig.json</c>, and their kind now.</summary>
	private static readonly Regex LegacyBackupName = new(@"^UserConfig\.(previous|before-import|v\d+)\.json$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	/// <summary>The kinds of backup a profile has.</summary>
	private static readonly Regex BackupKind = new(@"^(previous|before-import|v\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	/// <summary>Whether any settings are saved: a profile, or an older FE-Buddy's file not yet moved in.</summary>
	public static bool HasSettings =>
		ProfileNames().Count > 0 || File.Exists(Path.Combine(RootDirectory, LegacyConfigFileName));

	/// <summary>The folder that stands for <c>%APPDATA%\FE-Buddy</c>.</summary>
	private static string RootDirectory
	{
		get
		{
			lock (Gate)
			{
				return _root;
			}
		}
	}

	/// <summary>Every profile, in name order: each one with a file, and the one in use even before it has one.</summary>
	/// <returns>The names.</returns>
	public static IReadOnlyList<string> Profiles()
	{
		List<string> names = [.. ProfileNames()];
		string active = ActiveProfile;

		if (!names.Contains(active, StringComparer.OrdinalIgnoreCase))
		{
			names.Add(active);
		}

		return [.. names.Order(StringComparer.OrdinalIgnoreCase)];
	}

	/// <summary>Whether a profile by this name (in any case) has a file.</summary>
	/// <param name="name">The name.</param>
	/// <returns><see langword="true"/> when it has.</returns>
	public static bool ProfileExists(string name) =>
		ProfileNames().Contains(name?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Why a profile can't have <paramref name="name"/>, or <see langword="null"/> when it can. It
	/// becomes part of a file's name, so Windows' rules for one apply. Surrounding spaces are ignored:
	/// the name is trimmed before it is used. Whether another profile has the name is not checked.
	/// </summary>
	/// <param name="name">The name.</param>
	/// <returns>A sentence saying what is wrong, or <see langword="null"/>.</returns>
	public static string? ProfileNameProblem(string? name)
	{
		string trimmed = name?.Trim() ?? string.Empty;

		if (trimmed.Length == 0)
		{
			return "Type a name for the profile.";
		}

		if (trimmed.IndexOfAny(InvalidProfileNameChars) >= 0 || trimmed.Any(char.IsControl))
		{
			return @"A profile name can't contain \ / : * ? "" < > |.";
		}

		if (trimmed.EndsWith('.'))
		{
			return "A profile name can't end with a dot.";
		}

		if (ReservedNames.Contains(trimmed.Split('.')[0].TrimEnd()))
		{
			return $"Windows keeps {trimmed.Split('.')[0].TrimEnd().ToUpperInvariant()} as a device name, so no file can have it. Choose another name.";
		}

		return trimmed.Length > MaxProfileNameLength
			? $"Keep it to {MaxProfileNameLength} characters or fewer."
			: null;
	}

	/// <summary>Creates a profile. It isn't put to use: see <see cref="SwitchProfile"/>.</summary>
	/// <param name="name">Its name.</param>
	/// <param name="values">
	/// Its settings, by dotted path - a copy of another profile's, or an import's - or
	/// <see langword="null"/> to start from every default. Shared settings in them are left out: every
	/// profile has the same.
	/// </param>
	/// <exception cref="ArgumentException">The name can't be used, or another profile has it.</exception>
	/// <exception cref="IOException">The file could not be written.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void CreateProfile(string name, IReadOnlyDictionary<string, string>? values)
	{
		string profile = CheckNewName(name, renaming: null);
		WriteActiveProfileIfMissing();

		lock (Gate)
		{
			WriteConfig(ProfileFilePath(profile), BuildTree(ProfileValues(ValidValues(values ?? new Dictionary<string, string>()))), UserConfigMigrations.CurrentVersion);
		}

		AppLog.Info(LogSource, $"Created settings profile '{profile}'" + (values is null ? " with every setting at its default." : " from a copy of settings."));
	}

	/// <summary>Puts another profile to use, and reads it.</summary>
	/// <param name="name">The profile, as <see cref="Profiles"/> names it.</param>
	/// <exception cref="ArgumentException">No profile has that name.</exception>
	/// <exception cref="IOException"><c>Shared.json</c> could not be written; the profile in use is unchanged.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void SwitchProfile(string name)
	{
		string profile = ExistingName(name) ?? throw new ArgumentException($"There is no settings profile called '{name}'.", nameof(name));

		lock (Gate)
		{
			string was = _profile;
			_profile = profile;

			try
			{
				UpdateSharedFile([]);
			}
			catch
			{
				_profile = was;
				throw;
			}
		}

		ReadAll();
		AppLog.Info(LogSource, $"Switched to settings profile '{profile}'.");
	}

	/// <summary>Renames a profile, its backups with it.</summary>
	/// <param name="name">The profile.</param>
	/// <param name="newName">Its new name; it may differ from the old only in case.</param>
	/// <exception cref="ArgumentException">No profile has <paramref name="name"/>, or <paramref name="newName"/> can't be used or is another profile's.</exception>
	/// <exception cref="IOException">The file could not be renamed.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void RenameProfile(string name, string newName)
	{
		WriteActiveProfileIfMissing();

		string profile = ExistingName(name) ?? throw new ArgumentException($"There is no settings profile called '{name}'.", nameof(name));
		string renamed = CheckNewName(newName, renaming: profile);

		lock (Gate)
		{
			File.Move(ProfileFilePath(profile), ProfileFilePath(renamed));

			foreach ((string kind, string path) in Backups(profile))
			{
				MoveQuietly(path, BackupFilePath(renamed, kind));
			}

			if (_profile.Equals(profile, StringComparison.OrdinalIgnoreCase))
			{
				_profile = renamed;
				UpdateSharedFile([]);
			}
		}

		AppLog.Info(LogSource, $"Renamed settings profile '{profile}' to '{renamed}'.");
	}

	/// <summary>Deletes a profile that isn't in use, its backups with it.</summary>
	/// <param name="name">The profile.</param>
	/// <exception cref="ArgumentException">No profile has that name.</exception>
	/// <exception cref="InvalidOperationException">It is the profile in use: switch to another first.</exception>
	/// <exception cref="IOException">Its file could not be deleted.</exception>
	/// <exception cref="UnauthorizedAccessException">The same, for want of permission.</exception>
	public static void DeleteProfile(string name)
	{
		string profile = ExistingName(name) ?? throw new ArgumentException($"There is no settings profile called '{name}'.", nameof(name));

		lock (Gate)
		{
			if (_profile.Equals(profile, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException($"'{profile}' is the settings profile in use. Switch to another before deleting it.");
			}

			File.Delete(ProfileFilePath(profile));

			foreach ((_, string path) in Backups(profile))
			{
				DeleteIfPresent(path);
			}
		}

		AppLog.Info(LogSource, $"Deleted settings profile '{profile}'.");
	}

	/// <summary>
	/// Writes the profile in use's file when it has none yet - nothing saved since the first launch - so
	/// it is still there to switch back to once another profile is created, and can be renamed.
	/// </summary>
	private static void WriteActiveProfileIfMissing()
	{
		if (!File.Exists(ConfigFilePath))
		{
			Write();
		}
	}

	/// <summary>The name of every profile with a file, as its file spells it.</summary>
	private static List<string> ProfileNames()
	{
		string folder = Directory;

		if (!System.IO.Directory.Exists(folder))
		{
			return [];
		}

		try
		{
			return
			[
				.. System.IO.Directory.EnumerateFiles(folder, ProfileFilePrefix + "*" + ProfileFileExtension)
					.Select(Path.GetFileName)
					.OfType<string>()
					.Where(file => file.Length > ProfileFilePrefix.Length + ProfileFileExtension.Length
						&& file.StartsWith(ProfileFilePrefix, StringComparison.OrdinalIgnoreCase)
						&& file.EndsWith(ProfileFileExtension, StringComparison.OrdinalIgnoreCase))
					.Select(file => file[ProfileFilePrefix.Length..^ProfileFileExtension.Length])
					.Where(profile => ProfileNameProblem(profile) is null),
			];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not list the settings profiles in '{folder}': {ex.Message}");
			return [];
		}
	}

	/// <summary>A profile's name as its file spells it, or <see langword="null"/> when it has no file.</summary>
	private static string? ExistingName(string? name) =>
		ProfileNames().FirstOrDefault(profile => profile.Equals(name?.Trim(), StringComparison.OrdinalIgnoreCase));

	/// <summary>The trimmed new name, once it is checked: usable, and no other profile's.</summary>
	/// <exception cref="ArgumentException">It isn't.</exception>
	private static string CheckNewName(string name, string? renaming)
	{
		if (ProfileNameProblem(name) is { } problem)
		{
			throw new ArgumentException(problem, nameof(name));
		}

		string trimmed = name.Trim();

		if (ExistingName(trimmed) is { } taken && !taken.Equals(renaming, StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException($"There is already a settings profile called '{taken}'.", nameof(name));
		}

		return trimmed;
	}

	/// <summary>A profile's backup files that exist, with the kind each is.</summary>
	/// <remarks>
	/// Only the kinds FE-Buddy writes count, so profile <c>B</c>'s are never mistaken for those of a
	/// profile called <c>A.B</c> (<c>UserConfig-previous.A.B.json</c>).
	/// </remarks>
	private static List<(string Kind, string Path)> Backups(string profile)
	{
		string suffix = "." + profile + ProfileFileExtension;

		return System.IO.Directory.Exists(Directory)
			? [.. System.IO.Directory.EnumerateFiles(Directory, "UserConfig-*" + suffix)
				.Select(path => (Name: Path.GetFileName(path), Path: path))
				.Where(file => file.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				.Select(file => (Kind: file.Name["UserConfig-".Length..^suffix.Length], file.Path))
				.Where(file => BackupKind.IsMatch(file.Kind))]
			: [];
	}

	/// <summary>The path of one of a profile's backups, e.g. <c>UserConfig-previous.Default.json</c>.</summary>
	private static string BackupFilePath(string profile, string kind) =>
		Path.Combine(Directory, $"UserConfig-{kind}.{profile}{ProfileFileExtension}");

	/// <summary>
	/// The profile to use: the one <c>Shared.json</c> names, when it has a file - or, before any has,
	/// that one (or <see cref="DefaultProfile"/>), whose file the first save writes. When the one named
	/// has gone, <see cref="DefaultProfile"/>, else the first there is.
	/// </summary>
	private static string PickProfile(string? named)
	{
		string wanted = named is not null && ProfileNameProblem(named) is null ? named.Trim() : DefaultProfile;
		List<string> existing = ProfileNames();

		if (existing.Count == 0)
		{
			return wanted;
		}

		if (existing.FirstOrDefault(profile => profile.Equals(wanted, StringComparison.OrdinalIgnoreCase)) is { } found)
		{
			return found;
		}

		string fallback = existing.FirstOrDefault(profile => profile.Equals(DefaultProfile, StringComparison.OrdinalIgnoreCase))
			?? existing.Order(StringComparer.OrdinalIgnoreCase).First();

		AppLog.Warning(LogSource, $"The settings profile '{wanted}' has no file. Using '{fallback}' instead.");
		return fallback;
	}

	/// <summary>
	/// Moves an older FE-Buddy's one settings file, <c>%APPDATA%\FE-Buddy\UserConfig.json</c>, in as the
	/// <see cref="DefaultProfile"/> profile, with its backups - once, while there is no profile yet - so
	/// the old file is gone once its settings are in. Its shared settings move into <c>Shared.json</c>
	/// as it is read. Never throws: when it can't be moved, it is left where it is, and the next launch
	/// tries again.
	/// </summary>
	private static void MoveLegacyFileIn()
	{
		string legacy = Path.Combine(_root, LegacyConfigFileName);

		if (!File.Exists(legacy) || ProfileNames().Count > 0)
		{
			return;
		}

		try
		{
			System.IO.Directory.CreateDirectory(Directory);
			File.Move(legacy, ProfileFilePath(DefaultProfile));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not move '{legacy}' into '{ProfilesFolderName}': {ex.Message} FE-Buddy tries again at the next launch.");
			return;
		}

		foreach (string path in System.IO.Directory.EnumerateFiles(_root, "UserConfig.*.json"))
		{
			if (LegacyBackupName.Match(Path.GetFileName(path)) is { Success: true } match)
			{
				MoveQuietly(path, BackupFilePath(DefaultProfile, match.Groups[1].Value.ToLowerInvariant()));
			}
		}

		AppLog.Info(LogSource, string.Create(CultureInfo.InvariantCulture,
			$"Moved '{LegacyConfigFileName}' into '{ProfilesFolderName}' as the settings profile '{DefaultProfile}'."));
	}

	/// <summary>Moves a file, logging rather than throwing when it can't: a backup is never worth failing for.</summary>
	private static void MoveQuietly(string from, string to)
	{
		try
		{
			File.Move(from, to, overwrite: true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			AppLog.Warning(LogSource, $"Could not move '{Path.GetFileName(from)}' to '{Path.GetFileName(to)}': {ex.Message}");
		}
	}
}
