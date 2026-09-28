namespace FeBuddy.Core.Infrastructure.Credentials;

/// <summary>
/// Finds a secret written into a web address: a user name and password before the host
/// (<c>https://user:pass@host/…</c>), or a sign-in token in the query - such as the <c>?token=</c>
/// GitHub adds to a private file's raw link. A web address is saved in <c>UserConfig.json</c>, shown on
/// screen and included in settings exports, so one that holds a secret is refused; a saved
/// credential (<see cref="CredentialStore"/>) is the place for it.
/// </summary>
public static class UrlSecrets
{
	/// <summary>Query parameters that carry a sign-in token or signature rather than a plain setting.</summary>
	private static readonly HashSet<string> SecretParameters = new(StringComparer.OrdinalIgnoreCase)
	{
		"token",
		"access_token",
		"auth",
		"sig",
		"signature",
		"x-amz-signature",
		"x-amz-credential",
		"x-goog-signature",
		"password",
		"pwd",
		"secret",
		"api_key",
		"apikey",
	};

	/// <summary>What secret <paramref name="url"/> holds, for the user.</summary>
	/// <param name="url">An absolute web address.</param>
	/// <returns>
	/// e.g. <c>a user name and password</c> or <c>a sign-in token (token=)</c>; <see langword="null"/>
	/// when it holds none.
	/// </returns>
	public static string? Describe(Uri url)
	{
		ArgumentNullException.ThrowIfNull(url);

		if (!url.IsAbsoluteUri)
		{
			return null;
		}

		if (url.UserInfo.Length > 0)
		{
			return "a user name and password";
		}

		foreach (string pair in url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			string name = Uri.UnescapeDataString(pair.Split('=', 2)[0]);

			if (SecretParameters.Contains(name))
			{
				return $"a sign-in token ({name}=)";
			}
		}

		return null;
	}
}
