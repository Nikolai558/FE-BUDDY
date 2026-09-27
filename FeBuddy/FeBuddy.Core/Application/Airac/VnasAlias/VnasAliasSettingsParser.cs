using System.Globalization;

using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.VnasAlias;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the vNAS Alias Upload sub-service into the user's custom alias files.
/// </summary>
/// <remarks>
/// <para>
/// Each custom alias file is numbered, and has either a file on this PC or a web address:
/// </para>
/// <code>
/// Sources.1.FilePath     = C:\Users\me\Documents\ZOB-Alias.txt
/// Sources.2.Url          = https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt
/// Sources.2.CredentialId = 0f8fad5bd9cb469fa16570867728950e
/// </code>
/// <para>
/// They are merged in number order. <c>CredentialId</c> names a saved credential (see
/// <c>CredentialStore</c>) and is only ever an id: the secret never passes through settings.
/// </para>
/// </remarks>
public static class VnasAliasSettingsParser
{
	/// <summary>The start of every custom alias file's keys.</summary>
	public const string SourcesPrefix = "Sources.";

	/// <summary>A custom alias file's full path, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string FilePathKey = "FilePath";

	/// <summary>A custom alias file's web address, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string UrlKey = "Url";

	/// <summary>The id of the saved credential to download a web address with, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string CredentialIdKey = "CredentialId";

	private const string LogSource = "VnasAliasSettingsParser";

	/// <summary>Keys the AIRAC Service sets on every block, which this sub-service does not need.</summary>
	private static readonly HashSet<string> IgnoredKeys = new(StringComparer.OrdinalIgnoreCase) { "OutputDirectory" };

	/// <summary>
	/// Parses and validates <paramref name="settings"/> into the user's custom alias files.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns>The custom alias files, in merge order, plus any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a custom alias file has both or neither of a path and a web address, its path is
	/// not a full path, its web address is not an <c>http</c> or <c>https</c> address, or its
	/// credential id is not an id.
	/// </exception>
	public static VnasAliasSettingsParseResult Parse(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		List<ServiceMessage> messages = [];
		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach (KeyValuePair<string, string> entry in settings)
		{
			if (IgnoredKeys.Contains(entry.Key))
			{
				continue;
			}

			if (TrySplitSourceKey(entry.Key, out int number, out string field))
			{
				if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
				{
					fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					byNumber[number] = fields;
				}

				fields[field] = entry.Value?.Trim() ?? string.Empty;
				continue;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Unknown vNAS Alias Upload setting '{entry.Key}' was ignored. Custom alias files look like " +
				$"'{SourcesPrefix}1.{FilePathKey}' or '{SourcesPrefix}1.{UrlKey}'."));
		}

		List<AliasSource> sources = [];

		foreach ((int number, Dictionary<string, string> fields) in byNumber)
		{
			if (ParseSource(number, fields, messages) is { } source)
			{
				sources.Add(source);
			}
		}

		if (sources.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"No custom alias files are set, so {AiracOutputPaths.VnasAliasFileName} holds only FE-Buddy's aliases."));
		}

		return new VnasAliasSettingsParseResult(sources, messages);
	}

	/// <summary>Reads one custom alias file's fields.</summary>
	private static AliasSource? ParseSource(int number, Dictionary<string, string> fields, List<ServiceMessage> messages)
	{
		string filePath = fields.GetValueOrDefault(FilePathKey, string.Empty);
		string url = fields.GetValueOrDefault(UrlKey, string.Empty);
		string credential = fields.GetValueOrDefault(CredentialIdKey, string.Empty);
		string label = $"Custom alias file {number}";

		foreach (string field in fields.Keys.Where(f => !f.Equals(FilePathKey, StringComparison.OrdinalIgnoreCase)
			&& !f.Equals(UrlKey, StringComparison.OrdinalIgnoreCase)
			&& !f.Equals(CredentialIdKey, StringComparison.OrdinalIgnoreCase)))
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Unknown vNAS Alias Upload setting '{SourcesPrefix}{number}.{field}' was ignored."));
		}

		if (filePath.Length > 0 && url.Length > 0)
		{
			throw new ArgumentException($"{label} has both a file path and a web address. Give it one or the other.");
		}

		if (filePath.Length == 0 && url.Length == 0)
		{
			// A row with only a credential left on it: nothing to read, so nothing to merge.
			if (credential.Length == 0)
			{
				return null;
			}

			throw new ArgumentException($"{label} has no file path or web address.");
		}

		if (filePath.Length > 0)
		{
			if (!Path.IsPathFullyQualified(filePath))
			{
				throw new ArgumentException($"{label}'s path '{filePath}' is not a full path, such as C:\\Users\\me\\Documents\\Alias.txt.");
			}

			if (credential.Length > 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"{label} is a file on this PC, so its credential is not needed and was ignored."));
			}

			return new AliasSource(number, AliasSourceKind.File, filePath);
		}

		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
		{
			throw new ArgumentException($"{label}'s web address '{url}' is not an http:// or https:// address.");
		}

		Guid? credentialId = null;

		if (credential.Length > 0)
		{
			if (!Guid.TryParse(credential, out Guid id))
			{
				throw new ArgumentException($"{label}'s '{CredentialIdKey}' is not a credential id.");
			}

			credentialId = id;
		}

		return new AliasSource(number, AliasSourceKind.Url, url, credentialId);
	}

	/// <summary>Splits <c>Sources.2.Url</c> into <c>2</c> and <c>Url</c>.</summary>
	private static bool TrySplitSourceKey(string key, out int number, out string field)
	{
		number = 0;
		field = string.Empty;

		if (!key.StartsWith(SourcesPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] parts = key[SourcesPrefix.Length..].Split('.');

		if (parts.Length != 2
			|| !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out number)
			|| number < 1
			|| parts[1].Length == 0)
		{
			return false;
		}

		field = parts[1];
		return true;
	}
}
