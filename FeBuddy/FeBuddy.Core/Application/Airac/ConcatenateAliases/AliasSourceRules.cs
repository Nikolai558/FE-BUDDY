using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.GitHub;

namespace FeBuddy.Core.Application.Airac.ConcatenateAliases;

/// <summary>
/// What makes a custom alias file one Add Custom Aliases can take, before anything is read: the rules the
/// settings parser refuses a run by, and the tab shows under a row as it is typed.
/// </summary>
public static class AliasSourceRules
{
	/// <summary>Why a file read from this PC or the web isn't an alias file: it has no alias command in it.</summary>
	public const string NoAliasCommands = "It has no alias commands (lines starting with a dot), so it isn't an alias file.";

	/// <summary>
	/// Why a custom alias file can't be used as given, or <see langword="null"/>: a file needs its full path;
	/// a web address must be http or https, the file itself rather than a GitHub page, and hold no secret
	/// (it is saved in the settings and their exports); and a credential is never sent to a plain http address,
	/// unless it is a GitHub file, which is fetched from GitHub's API over https.
	/// </summary>
	/// <param name="kind">A file on this PC, or a web address.</param>
	/// <param name="location">The path or the address.</param>
	/// <param name="hasCredential">Whether a credential is chosen for it.</param>
	/// <returns>A sentence saying what to do, or <see langword="null"/>.</returns>
	public static string? Problem(AliasSourceKind kind, string? location, bool hasCredential)
	{
		string trimmed = location?.Trim() ?? string.Empty;

		if (kind == AliasSourceKind.File)
		{
			return trimmed.Length == 0 ? "Choose the file."
				: !Path.IsPathFullyQualified(trimmed) ? @"Give the file's full path, such as C:\Users\me\Documents\ZOB-Alias.txt."
				: null;
		}

		if (trimmed.Length == 0)
		{
			return "Enter the file's web address.";
		}

		if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? url) || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
		{
			return "Enter a web address starting with https://.";
		}

		if (GitHubFileUrl.IsPageButNotFile(url))
		{
			return "This is a GitHub page, not a file. Open the alias file itself on GitHub and copy its address, or its Raw link.";
		}

		// Never echo the address: it holds the secret.
		if (UrlSecrets.Describe(url) is { } secret)
		{
			return $"This address has {secret} in it. Remove it and choose a credential instead: web addresses are saved " +
				"in FE-Buddy's settings and in settings exports.";
		}

		if (hasCredential && url.Scheme != Uri.UriSchemeHttps && GitHubFileUrl.ToContentsApi(url) is null)
		{
			return "A credential is only ever sent to an https:// address.";
		}

		return null;
	}
}
