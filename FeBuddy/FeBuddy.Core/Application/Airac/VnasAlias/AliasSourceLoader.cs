using System.Net;

using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;
using FeBuddy.Core.Infrastructure.GitHub;
using FeBuddy.Core.Infrastructure.Http;

namespace FeBuddy.Core.Application.Airac.VnasAlias;

/// <summary>
/// Reads the user's custom alias files: from this PC, or downloaded - with a saved credential when
/// the file is private.
/// </summary>
/// <remarks>
/// <para>
/// A file that cannot be read never throws: the result says why, in words the user can act on
/// (a mistyped path, a token GitHub refused, a private repository and no credential, and so on).
/// The vNAS Alias Upload tab's <b>Check</b> button and the AIRAC run both use it.
/// </para>
/// <para>
/// A credential is added by <see cref="CredentialStore.Authorize"/> only, so it goes over HTTPS to
/// one of its own websites or not at all. When it is not allowed there, the file is not downloaded
/// without it either: a surprise anonymous request would only fail in a more confusing way. A file
/// on GitHub is downloaded through the GitHub API (see <see cref="GitHubFileUrl"/>), which honours a
/// token for private repositories.
/// </para>
/// </remarks>
public static class AliasSourceLoader
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	/// <summary>Reads every custom alias file, one after another, in the order given.</summary>
	/// <param name="sources">The files.</param>
	/// <param name="store">The credentials to download with.</param>
	/// <param name="httpClient">The client to use; <see langword="null"/> creates one.</param>
	/// <param name="cancellationToken">Cancels the reads.</param>
	/// <returns>One result per file, in the same order.</returns>
	public static async Task<IReadOnlyList<AliasSourceLoad>> LoadAllAsync(
		IReadOnlyList<AliasSource> sources,
		CredentialStore store,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(sources);
		ArgumentNullException.ThrowIfNull(store);

		using HttpClient? owned = httpClient is null && sources.Any(s => s.Kind == AliasSourceKind.Url) ? FeBuddyHttp.CreateClient(Timeout) : null;
		HttpClient? client = httpClient ?? owned;

		List<AliasSourceLoad> loads = [];

		foreach (AliasSource source in sources)
		{
			loads.Add(await LoadAsync(source, store, client, cancellationToken).ConfigureAwait(false));
		}

		return loads;
	}

	/// <summary>Reads one custom alias file.</summary>
	/// <param name="source">The file.</param>
	/// <param name="store">The credentials to download with.</param>
	/// <param name="httpClient">The client to use; <see langword="null"/> creates one.</param>
	/// <param name="cancellationToken">Cancels the read.</param>
	/// <returns>Its text, or why it could not be read.</returns>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
	public static async Task<AliasSourceLoad> LoadAsync(
		AliasSource source,
		CredentialStore store,
		HttpClient? httpClient = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(store);

		if (source.Kind == AliasSourceKind.File)
		{
			return await ReadFileAsync(source, cancellationToken).ConfigureAwait(false);
		}

		using HttpClient? owned = httpClient is null ? FeBuddyHttp.CreateClient(Timeout) : null;
		return await DownloadAsync(source, store, httpClient ?? owned!, cancellationToken).ConfigureAwait(false);
	}

	private static async Task<AliasSourceLoad> ReadFileAsync(AliasSource source, CancellationToken cancellationToken)
	{
		if (!File.Exists(source.Location))
		{
			return AliasSourceLoad.Failed(source, $"{source.Location} was not found. It may have been moved, renamed or deleted.");
		}

		try
		{
			string text = await File.ReadAllTextAsync(source.Location, cancellationToken).ConfigureAwait(false);
			return Checked(source, text);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return AliasSourceLoad.Failed(source, $"{source.Location} could not be read: {ex.Message}");
		}
	}

	private static async Task<AliasSourceLoad> DownloadAsync(AliasSource source, CredentialStore store, HttpClient client, CancellationToken cancellationToken)
	{
		if (!Uri.TryCreate(source.Location, UriKind.Absolute, out Uri? url) || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
		{
			return AliasSourceLoad.Failed(source, $"'{source.Location}' is not a web address. It should start with https://.");
		}

		if (GitHubFileUrl.IsPageButNotFile(url))
		{
			return AliasSourceLoad.Failed(source,
				"The address is a GitHub page, not a file. Open the alias file on GitHub and copy the address of its page (it has /blob/ in it).");
		}

		Uri? gitHubApi = GitHubFileUrl.ToContentsApi(url);
		using HttpRequestMessage request = new(HttpMethod.Get, gitHubApi ?? url);

		if (gitHubApi is not null)
		{
			request.Headers.Accept.ParseAdd(GitHubFileUrl.RawMediaType);
		}

		string? credentialName = null;

		if (source.CredentialId is { } credentialId)
		{
			credentialName = store.Find(credentialId)?.Name;
			string host = request.RequestUri!.Host;

			switch (store.Authorize(request, credentialId))
			{
				case CredentialUseResult.NotFound:
					return AliasSourceLoad.Failed(source,
						"Its credential is not on this PC: it was removed, or the settings came from another PC. Choose one of your credentials for it.");
				case CredentialUseResult.NotHttps:
					return AliasSourceLoad.Failed(source,
						$"The address is not https://, so FE-Buddy will not send the credential '{credentialName}' to it. Use the https:// address.");
				case CredentialUseResult.HostNotAllowed:
					return AliasSourceLoad.Failed(source,
						$"The credential '{credentialName}' is not allowed to be sent to {host}. Add {host} to its websites in Settings ▸ Credentials, or choose another credential.");
			}
		}

		string site = gitHubApi is not null ? "GitHub" : url.Host;

		try
		{
			using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

				return IsWebPage(response, text)
					? AliasSourceLoad.Failed(source,
						$"{site} sent a web page, not an alias file. Use the address of the file itself" +
						(url.Host.Contains("github", StringComparison.OrdinalIgnoreCase) ? " (on GitHub, the file's page, with /blob/ in its address)." : "."))
					: Checked(source, text);
			}

			return AliasSourceLoad.Failed(source, DescribeRefusal(response, site, credentialName, gitHubApi is not null));
		}
		catch (HttpRequestException ex)
		{
			return AliasSourceLoad.Failed(source, $"Could not reach {url.Host}: {ex.Message}");
		}
		catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return AliasSourceLoad.Failed(source, $"{url.Host} did not answer within {Timeout.TotalSeconds:0} seconds.");
		}
	}

	/// <summary>Why a website refused the download, and what to do about it.</summary>
	private static string DescribeRefusal(HttpResponseMessage response, string site, string? credentialName, bool isGitHub)
	{
		bool rateLimited = response.Headers.TryGetValues("x-ratelimit-remaining", out IEnumerable<string>? remaining)
			&& remaining.FirstOrDefault() == "0";

		return response.StatusCode switch
		{
			HttpStatusCode.Unauthorized when credentialName is not null =>
				$"{site} refused the credential '{credentialName}'. It may be mistyped, expired or revoked: edit it in Settings ▸ Credentials.",
			HttpStatusCode.Unauthorized =>
				$"{site} needs a credential to download it. Choose one for it (add it in Settings ▸ Credentials first).",
			HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests when rateLimited && credentialName is null =>
				$"{site} limits downloads made without a token, and the limit has been reached. Choose a GitHub credential for it, or try again in an hour.",
			HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests when rateLimited =>
				$"{site}'s download limit for the credential '{credentialName}' has been reached. Try again later.",
			HttpStatusCode.Forbidden when credentialName is not null =>
				$"{site} does not let the credential '{credentialName}' read it." +
				(isGitHub ? " A fine-grained GitHub token needs this repository, with Contents: Read-only." : string.Empty),
			HttpStatusCode.Forbidden =>
				$"{site} refused the download (403 Forbidden). It may need a credential.",
			HttpStatusCode.NotFound when isGitHub && credentialName is null =>
				"GitHub could not find it. Check the address - and if the repository is private, choose a GitHub credential that can read it.",
			HttpStatusCode.NotFound when isGitHub =>
				$"GitHub could not find it, or the credential '{credentialName}' cannot see it. Check the address, and that the token can read this repository.",
			HttpStatusCode.NotFound =>
				$"{site} could not find it (404 Not Found). Check the address.",
			_ => $"{site} answered {(int)response.StatusCode} {response.ReasonPhrase}.",
		};
	}

	/// <summary>Whether a download is a web page (a sign-in page, a file's HTML view) rather than a text file.</summary>
	private static bool IsWebPage(HttpResponseMessage response, string text)
	{
		if (response.Content.Headers.ContentType?.MediaType is { } mediaType
			&& mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		string start = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
		return start.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
			|| start.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>A file that was read - unless it holds no alias command at all, which means it is not an alias file.</summary>
	private static AliasSourceLoad Checked(AliasSource source, string text)
	{
		text = text.TrimStart('﻿');

		return VnasAliasFileWriter.CountCommands(text) > 0
			? AliasSourceLoad.Read(source, text)
			: AliasSourceLoad.Failed(source, "It has no alias commands (lines starting with a dot), so it does not look like an alias file.");
	}
}
