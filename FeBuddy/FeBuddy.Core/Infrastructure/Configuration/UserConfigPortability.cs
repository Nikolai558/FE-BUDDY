using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Decides which <c>UserConfig</c> settings can be handed to another FE-Buddy user, so a settings
/// export carries a facility's setup without this PC's folders, state or credentials.
/// </summary>
/// <remarks>
/// <para>
/// The rules work on key shape, so a setting added later is classified without anyone having to
/// list it here: any key under <c>General</c> or <c>Services</c> is <see cref="ConfigKeyScope.Shared"/>
/// unless it is named below, ends in <c>Folder</c>, <c>Directory</c> or <c>FilePath</c>
/// (<see cref="ConfigKeyScope.MachinePath"/>), or looks like a credential. A yes/no setting whose
/// name happens to end that way (<c>AddFeBuddyOutputFolder</c>) must be listed as not a folder.
/// </para>
/// <para>
/// Credentials (a GitHub PAT, vNAS, VATSIM or VATUSA logins) are meant to live outside
/// <c>UserConfig.json</c> - in their own store or in environment variables - so an export never
/// sees them. As a safety net, a key that looks like one is classified
/// <see cref="ConfigKeyScope.Secret"/> from its name alone (ending in <c>Token</c>,
/// <c>Password</c> and so on, called <c>Pat</c>, or under <c>Secrets</c>): it is never exported,
/// and ignored in any file that carries one.
/// </para>
/// </remarks>
public static class UserConfigPortability
{
	/// <summary>The top-level section credentials belong under.</summary>
	public const string SecretsRoot = "Secrets";

	/// <summary>How the name of a setting that holds a file's path ends.</summary>
	private const string FileSuffix = "FilePath";

	private static readonly string[] SharedRoots = ["General", "Services"];

	private static readonly HashSet<string> LocalKeys = new(StringComparer.Ordinal)
	{
		UserConfigKeys.UpdateChannel,
		UserConfigKeys.NewsLastOpen,
	};

	private static readonly string[] SecretSuffixes = ["Token", "Password", "Secret", "ApiKey", "Credential", "Credentials"];

	private static readonly string[] SecretNames = ["Pat"];

	private static readonly string[] FolderSuffixes = ["Folder", "Directory", FileSuffix];

	private static readonly HashSet<string> NotFolderKeys = new(StringComparer.Ordinal)
	{
		UserConfigKeys.AddFeBuddyOutputFolder,
	};

	private static readonly Dictionary<string, string> ConversionNames = new(StringComparer.Ordinal)
	{
		["DatToGeojson"] = "DAT to GeoJSON",
		["SctToGeojson"] = "SCT2 to GeoJSON",
		["EramToGeojson"] = "ERAM to GeoJSON",
	};

	/// <summary>Whether <paramref name="key"/> can travel to another PC, and how.</summary>
	/// <param name="key">A dotted <c>UserConfig</c> key.</param>
	/// <returns>The key's scope. A key outside the known sections is <see cref="ConfigKeyScope.Local"/>.</returns>
	public static ConfigKeyScope Classify(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		string[] segments = key.Split('.');
		string root = segments[0];
		string leaf = segments[^1];

		if (root.Equals(SecretsRoot, StringComparison.OrdinalIgnoreCase)
			|| SecretSuffixes.Any(s => leaf.EndsWith(s, StringComparison.OrdinalIgnoreCase))
			|| SecretNames.Contains(leaf, StringComparer.OrdinalIgnoreCase))
		{
			return ConfigKeyScope.Secret;
		}

		if (!SharedRoots.Contains(root, StringComparer.Ordinal) || LocalKeys.Contains(key))
		{
			return ConfigKeyScope.Local;
		}

		return FolderSuffixes.Any(s => leaf.EndsWith(s, StringComparison.Ordinal)) && !NotFolderKeys.Contains(key)
			? ConfigKeyScope.MachinePath
			: ConfigKeyScope.Shared;
	}

	/// <summary>
	/// Whether a <see cref="ConfigKeyScope.MachinePath"/> setting is a folder FE-Buddy writes into
	/// (created as needed, so it need not exist yet) rather than one it reads from (which must).
	/// </summary>
	/// <param name="key">A dotted <c>UserConfig</c> key.</param>
	/// <returns><see langword="true"/> for an output folder, such as the default output directory.</returns>
	public static bool IsOutputFolder(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		return key.Split('.')[^1].Contains("Output", StringComparison.Ordinal);
	}

	/// <summary>
	/// Whether a <see cref="ConfigKeyScope.MachinePath"/> setting is a file (its name ends in
	/// <c>FilePath</c>, such as a custom alias file) rather than a folder. An import only takes it when
	/// the file exists on the importing PC.
	/// </summary>
	/// <param name="key">A dotted <c>UserConfig</c> key.</param>
	/// <returns><see langword="true"/> for a file.</returns>
	public static bool IsFile(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		return key.Split('.')[^1].EndsWith(FileSuffix, StringComparison.Ordinal);
	}

	/// <summary>A setting's name for people, for the import summary.</summary>
	/// <param name="key">A dotted <c>UserConfig</c> key.</param>
	/// <returns>e.g. <c>Default output directory</c>; the key itself when it has no friendlier name.</returns>
	public static string Describe(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		switch (key)
		{
			case UserConfigKeys.DefaultOutputDirectory:
				return "Default output directory";
			case UserConfigKeys.UpdateChannel:
				return "Update channel";
			case UserConfigKeys.NewsLastOpen:
				return "News read status";
		}

		string[] segments = key.Split('.');

		if (segments.Length >= 4 && segments[^1] == FileSuffix && segments[^3] == "Sources")
		{
			return $"Custom alias file {segments[^2]}";
		}

		if (segments.Length >= 2 && segments[^1] == "SourceFolder")
		{
			string owner = segments[^2];
			return $"{ConversionNames.GetValueOrDefault(owner, owner)} source folder";
		}

		return key;
	}
}
