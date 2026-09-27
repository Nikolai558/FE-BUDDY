namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// Rewrites folder paths so they survive the move to another PC: a folder inside the user's own
/// Desktop, Documents or profile is exported as a token (<c>%DESKTOP%\FE-Buddy</c>), and an
/// import expands that token to the importing user's folder.
/// </summary>
/// <remarks>
/// A folder anywhere else (<c>D:\VATSIM\Files</c>) stays as it is, and an import only takes it if
/// it works on the importing PC. Only these three tokens are ever expanded; any other
/// <c>%NAME%</c> is left alone. <see cref="Localize(string)"/> also moves a path under another
/// user's profile into this user's, for files that were never tokenized.
/// </remarks>
public sealed class PortablePathTokens
{
	/// <summary>Stands for the user's Desktop.</summary>
	public const string DesktopToken = "%DESKTOP%";

	/// <summary>Stands for the user's Documents folder.</summary>
	public const string DocumentsToken = "%DOCUMENTS%";

	/// <summary>Stands for the user's profile folder, e.g. <c>C:\Users\name</c>.</summary>
	public const string UserProfileToken = "%USERPROFILE%";

	/// <summary>Profiles under <c>C:\Users</c> that belong to no one person, so are never moved to this user's.</summary>
	private static readonly string[] SharedProfiles = ["Public", "Default", "Default User", "All Users"];

	private readonly (string Token, string Folder)[] _folders;
	private readonly string? _profile;

	/// <summary>Uses the given folder for each token.</summary>
	/// <param name="folders">Each token and the folder it stands for. A blank folder leaves that token unexpanded.</param>
	internal PortablePathTokens(IEnumerable<(string Token, string Folder)> folders)
	{
		// Longest folder first: the Desktop lives inside the profile, and must win over it.
		_folders =
		[
			.. folders
				.Where(f => !string.IsNullOrWhiteSpace(f.Folder))
				.Select(f => (f.Token, Folder: Path.TrimEndingDirectorySeparator(f.Folder.Trim())))
				.OrderByDescending(f => f.Folder.Length),
		];

		_profile = _folders.Where(f => f.Token == UserProfileToken).Select(f => f.Folder).FirstOrDefault();
	}

	/// <summary>The tokens for the user running FE-Buddy.</summary>
	/// <returns>The current user's Desktop, Documents and profile folders.</returns>
	public static PortablePathTokens ForCurrentUser() => new(
	[
		(DesktopToken, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
		(DocumentsToken, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
		(UserProfileToken, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
	]);

	/// <summary>Swaps the start of <paramref name="path"/> for a token when it is inside one of the user's folders.</summary>
	/// <param name="path">A folder path on this PC.</param>
	/// <returns>The tokenized path, e.g. <c>%DESKTOP%\Output</c>; or the path unchanged when no folder contains it.</returns>
	public string Tokenize(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		string trimmed = path.Trim();

		foreach ((string token, string folder) in _folders)
		{
			if (TryStripPrefix(trimmed, folder, out string rest))
			{
				return token + rest;
			}
		}

		return trimmed;
	}

	/// <summary>Swaps a leading token in <paramref name="value"/> for this user's folder.</summary>
	/// <param name="value">A path that may start with a token.</param>
	/// <returns>The expanded path; or the value unchanged when it starts with no known token.</returns>
	public string Expand(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		string trimmed = value.Trim();

		foreach ((string token, string folder) in _folders)
		{
			if (TryStripPrefix(trimmed, token, out string rest))
			{
				return folder + rest;
			}
		}

		return trimmed;
	}

	/// <summary>
	/// Turns a folder from someone else's settings file into the matching folder for this user:
	/// expands a token, and moves a path under another user's profile (<c>C:\Users\alice\...</c>,
	/// as a plain <c>UserConfig.json</c> has) into this user's own, so another person's user name
	/// never ends up in this PC's settings.
	/// </summary>
	/// <param name="value">A folder as it appears in a settings file.</param>
	/// <returns>
	/// The folder for this user: <c>C:\Users\alice\OneDrive\Desktop\FEB</c> becomes this user's
	/// Desktop plus <c>FEB</c>, and anything else under <c>C:\Users\alice</c> moves under this
	/// user's profile. A path under this user's own profile, a shared profile such as
	/// <c>Public</c>, or outside <c>Users</c> is only token-expanded.
	/// </returns>
	public string Localize(string value)
	{
		string path = Expand(value);

		if (string.IsNullOrWhiteSpace(path) || _profile is null)
		{
			return path;
		}

		// X:\Users\<name>\... - the profile root is the first three segments.
		string[] segments = path.Split('\\', '/');

		if (segments.Length < 3
			|| segments[0].Length != 2
			|| segments[0][1] != ':'
			|| !segments[1].Equals("Users", StringComparison.OrdinalIgnoreCase)
			|| segments[2].Length == 0
			|| SharedProfiles.Contains(segments[2], StringComparer.OrdinalIgnoreCase)
			|| segments[2].Equals(Path.GetFileName(_profile), StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}

		string root = path[..(segments[0].Length + 1 + segments[1].Length + 1 + segments[2].Length)];

		PortablePathTokens theirs = new(
		[
			(DesktopToken, root + @"\OneDrive\Desktop"),
			(DesktopToken, root + @"\Desktop"),
			(DocumentsToken, root + @"\OneDrive\Documents"),
			(DocumentsToken, root + @"\Documents"),
			(UserProfileToken, root),
		]);

		return Expand(theirs.Tokenize(path));
	}

	/// <summary>
	/// Whether <paramref name="path"/> is <paramref name="prefix"/> itself or something inside it:
	/// the prefix must end at a separator, so <c>C:\Users\bob</c> does not match <c>C:\Users\bobby</c>.
	/// </summary>
	private static bool TryStripPrefix(string path, string prefix, out string rest)
	{
		if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			&& (path.Length == prefix.Length || path[prefix.Length] is '\\' or '/'))
		{
			rest = path[prefix.Length..];
			return true;
		}

		rest = string.Empty;
		return false;
	}
}
