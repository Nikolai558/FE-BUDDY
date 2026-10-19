using System.Globalization;

namespace FeBuddy.Core.Application.Settings;

/// <summary>
/// Settings kept as a numbered list, one key per field of each entry: <c>&lt;Prefix&gt;&lt;n&gt;.&lt;Field&gt;</c>,
/// e.g. <c>Sources.2.Url</c> or <c>VirtualAirlines.1.Telephony</c>. Numbers start at 1; a key that isn't
/// one of these isn't part of the list. Read the same way by the settings parsers and the tabs that show the list.
/// </summary>
public static class NumberedSettings
{
	/// <summary>Splits <c>&lt;Prefix&gt;2.Url</c> into <c>2</c> and <c>Url</c>.</summary>
	/// <param name="key">The key.</param>
	/// <param name="prefix">The list's prefix, with its dot, e.g. <c>Sources.</c>; compared ignoring case.</param>
	/// <param name="number">The entry's number, 1 or more.</param>
	/// <param name="field">The field's name.</param>
	/// <returns><see langword="true"/> when the key is one of the list's.</returns>
	public static bool TrySplit(string key, string prefix, out int number, out string field)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(prefix);

		number = 0;
		field = string.Empty;

		if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] parts = key[prefix.Length..].Split('.');

		if (parts.Length != 2
			|| !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out number)
			|| number < 1
			|| parts[1].Length == 0)
		{
			number = 0;
			return false;
		}

		field = parts[1];
		return true;
	}

	/// <summary>Every entry of the list, in number order: each one's fields by name (ignoring case), values trimmed.</summary>
	/// <param name="values">The settings to read it from.</param>
	/// <param name="prefix">The list's prefix, with its dot, e.g. <c>Services.AiracService.Telephony.VirtualAirlines.</c>.</param>
	/// <param name="keysRead">Given, each key that is part of the list is added to it.</param>
	/// <returns>The entries by number.</returns>
	public static SortedDictionary<int, Dictionary<string, string>> Read(
		IEnumerable<KeyValuePair<string, string>> values,
		string prefix,
		ISet<string>? keysRead = null)
	{
		ArgumentNullException.ThrowIfNull(values);
		ArgumentNullException.ThrowIfNull(prefix);

		SortedDictionary<int, Dictionary<string, string>> byNumber = [];

		foreach (KeyValuePair<string, string> entry in values)
		{
			if (!TrySplit(entry.Key, prefix, out int number, out string field))
			{
				continue;
			}

			keysRead?.Add(entry.Key);

			if (!byNumber.TryGetValue(number, out Dictionary<string, string>? fields))
			{
				fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				byNumber[number] = fields;
			}

			fields[field] = entry.Value?.Trim() ?? string.Empty;
		}

		return byNumber;
	}
}
