using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.ConcatenateAliases;

/// <summary>
/// Parses the raw <c>Dictionary&lt;string, string&gt;</c> the GUI (or <c>FeBuddy.Harness</c>)
/// supplies for the Add Custom Aliases sub-service (Concatenate Aliases, as the code still calls
/// it): whether to add the user's custom alias files to <c>Combined_Alias.txt</c>, after the run's
/// own alias files, and which files. Every run that writes an alias file writes
/// <c>Combined_Alias.txt</c>, with or without them (see <see cref="AiracService"/>).
/// </summary>
/// <remarks>
/// <para>
/// The custom alias files are added unless <c>AddCustomAliasFiles</c> is <c>N</c>. Each is numbered,
/// and has either a file on this PC or a web address:
/// </para>
/// <code>
/// AddCustomAliasFiles    = Y
/// Sources.1.FilePath     = C:\Users\me\Documents\ZOB-Alias.txt
/// Sources.2.Url          = https://github.com/vZOB/facility/blob/main/ZOB-Alias.txt
/// Sources.2.CredentialId = 0f8fad5bd9cb469fa16570867728950e
/// </code>
/// <para>
/// They are merged in number order. <c>CredentialId</c> names a saved credential (see
/// <c>CredentialStore</c>) and is only ever an id: the secret never passes through settings. While
/// they aren't added, the custom alias files are not read or checked: nothing would use them.
/// </para>
/// </remarks>
public static class ConcatenateAliasesSettingsParser
{
	/// <summary>Whether to add the custom alias files to <c>Combined_Alias.txt</c>: <c>Y</c> (the default) or <c>N</c>.</summary>
	public const string AddCustomAliasFilesKey = "AddCustomAliasFiles";

	/// <summary>The start of every custom alias file's keys.</summary>
	public const string SourcesPrefix = "Sources.";

	/// <summary>A custom alias file's full path, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string FilePathKey = "FilePath";

	/// <summary>A custom alias file's web address, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string UrlKey = "Url";

	/// <summary>The id of the saved credential to download a web address with, under <see cref="SourcesPrefix"/> and its number.</summary>
	public const string CredentialIdKey = "CredentialId";

	private const string LogSource = "ConcatenateAliasesSettingsParser";

	/// <summary>Keys the AIRAC Service sets on every block, which this sub-service does not need.</summary>
	private static readonly HashSet<string> IgnoredKeys = new(StringComparer.OrdinalIgnoreCase) { "OutputDirectory" };

	/// <summary>Whether the block adds the custom alias files: <c>AddCustomAliasFiles</c>, on unless it is <c>N</c>.</summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns><see langword="true"/> unless adding them is turned off.</returns>
	/// <exception cref="ArgumentException">Thrown when <c>AddCustomAliasFiles</c> is neither <c>Y</c> nor <c>N</c>.</exception>
	public static bool AddsCustomAliasFiles(IReadOnlyDictionary<string, string> settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		// The block's keys match ignoring case, whatever dictionary the caller built.
		return SettingsValueReader.YesNo(new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase), AddCustomAliasFilesKey, defaultValue: true);
	}

	/// <summary>
	/// Parses and validates <paramref name="settings"/>: whether to add the custom alias files, and
	/// which.
	/// </summary>
	/// <param name="settings">The raw settings dictionary.</param>
	/// <returns>Whether to add them, the custom alias files in merge order (none while they aren't added), and any non-fatal parsing messages.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <c>AddCustomAliasFiles</c> is neither <c>Y</c> nor <c>N</c>; or, while adding them, when
	/// a custom alias file has both a path and a web address, or only a credential; its path is not a
	/// full path; its web address is not an <c>http</c> or <c>https</c> address, or has a secret in it
	/// (<see cref="UrlSecrets"/>); or its credential id is not an id.
	/// </exception>
	public static ConcatenateAliasesSettingsParseResult Parse(IReadOnlyDictionary<string, string> settings)
	{
		bool combine = AddsCustomAliasFiles(settings);

		List<ServiceMessage> messages = [];
		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach (KeyValuePair<string, string> entry in settings)
		{
			if (IgnoredKeys.Contains(entry.Key) || entry.Key.Equals(AddCustomAliasFilesKey, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (NumberedSettings.TrySplit(entry.Key, SourcesPrefix, out int number, out string field))
			{
				if (!combine)
				{
					continue;
				}

				if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
				{
					fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					byNumber[number] = fields;
				}

				fields[field] = entry.Value?.Trim() ?? string.Empty;
				continue;
			}

			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"Unknown Add Custom Aliases setting '{entry.Key}' was ignored. Custom alias files look like " +
				$"'{SourcesPrefix}1.{FilePathKey}' or '{SourcesPrefix}1.{UrlKey}'."));
		}

		if (!combine)
		{
			return new ConcatenateAliasesSettingsParseResult([], messages, AddsCustomFiles: false);
		}

		List<AliasSource> sources = [];

		foreach ((int number, Dictionary<string, string> fields) in byNumber)
		{
			if (ParseSource(number, fields, messages) is { } source)
			{
				sources.Add(source);
			}
		}

		// vNAS takes one alias file per facility, so uploading FE-Buddy's alone would remove the
		// facility's own aliases from vNAS.
		if (sources.Count == 0)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"No custom alias files are set, so {AiracOutputPaths.CombinedAliasFileName} holds only FE-Buddy's aliases. " +
				"vNAS takes one alias file per facility, so uploading it would remove your facility's own aliases from vNAS. " +
				"To keep them, add your facility's alias file on the Add Custom Aliases tab.")
			{ IsAdvisory = true });
		}

		return new ConcatenateAliasesSettingsParseResult(sources, messages);
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
				$"Unknown Add Custom Aliases setting '{SourcesPrefix}{number}.{field}' was ignored."));
		}

		if (filePath.Length > 0 && url.Length > 0)
		{
			throw new ArgumentException($"{label} has both a file path and a web address. Give it one or the other.");
		}

		if (filePath.Length == 0 && url.Length == 0)
		{
			// A row with nothing in it at all: nothing to read, so nothing to merge. A row left with
			// only a credential is a mistake worth saying, so it throws below.
			if (credential.Length == 0)
			{
				return null;
			}

			throw new ArgumentException($"{label} has no file path or web address.");
		}

		AliasSourceKind kind = filePath.Length > 0 ? AliasSourceKind.File : AliasSourceKind.Url;

		// The rules the tab shows under a row as it is typed (AliasSourceRules), so a run refuses just what it does.
		if (AliasSourceRules.Problem(kind, kind == AliasSourceKind.File ? filePath : url, hasCredential: kind == AliasSourceKind.Url && credential.Length > 0) is { } problem)
		{
			throw new ArgumentException($"{label}: {problem}");
		}

		if (kind == AliasSourceKind.File)
		{
			if (credential.Length > 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"{label} is a file on this PC, so its credential is not needed and was ignored."));
			}

			return new AliasSource(number, AliasSourceKind.File, filePath);
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

}
