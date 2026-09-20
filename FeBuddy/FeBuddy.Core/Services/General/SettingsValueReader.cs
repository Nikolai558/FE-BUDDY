using System.Globalization;

namespace FeBuddy.Core.Services.General;

/// <summary>
/// Reads and validates values out of the raw <c>Dictionary&lt;string, string&gt;</c> settings
/// block a sub-service receives from the GUI (or <c>FeBuddy.Harness</c>).
/// </summary>
/// <remarks>
/// Every sub-service takes its settings in the same dictionary form, so the "is this Y or N",
/// "is this an integer", "is this a comma-separated list" questions are identical for all of
/// them. They live here once, and each sub-service's parser is left to express only what is
/// actually specific to it.
/// <para>
/// Every failure throws <see cref="ArgumentException"/> naming the offending key, which the
/// service's entry point surfaces to the user unchanged.
/// </para>
/// </remarks>
public static class SettingsValueReader
{
	/// <summary>Reads a value that must be present and non-blank.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The trimmed value.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing or blank.</exception>
	public static string RequireNonEmpty(IReadOnlyDictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException($"Settings must contain a non-empty '{key}' value.");
		}

		return value.Trim();
	}

	/// <summary>Reads an optional string.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The trimmed value, or <see langword="null"/> when absent or blank.</returns>
	public static string? OptionalString(IReadOnlyDictionary<string, string> settings, string key) =>
		settings.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
			? value.Trim()
			: null;

	/// <summary>Reads a <c>Y</c>/<c>N</c> flag.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <param name="defaultValue">The value to use when the key is absent or blank.</param>
	/// <returns>The parsed flag.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is neither <c>Y</c> nor <c>N</c>.</exception>
	public static bool YesNo(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return YesNoValue(key, value);
	}

	/// <summary>Reads an optional <c>Y</c>/<c>N</c> flag.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed flag, or <see langword="null"/> when absent or blank.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is neither <c>Y</c> nor <c>N</c>.</exception>
	public static bool? OptionalYesNo(IReadOnlyDictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		return YesNoValue(key, value);
	}

	/// <summary>Reads an optional integer.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed integer, or <see langword="null"/> when absent or blank.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not an integer.</exception>
	public static int? OptionalInt(IReadOnlyDictionary<string, string> settings, string key)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
		{
			throw new ArgumentException($"'{key}' value '{value}' is not a valid integer.");
		}

		return parsed;
	}

	/// <summary>Reads an integer constrained to a range, falling back to a default.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <param name="defaultValue">The value to use when the key is absent or blank.</param>
	/// <param name="minimum">Lowest accepted value, inclusive.</param>
	/// <param name="maximum">Highest accepted value, inclusive.</param>
	/// <returns>The parsed integer.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not an integer in range.</exception>
	public static int IntInRange(
		IReadOnlyDictionary<string, string> settings,
		string key,
		int defaultValue,
		int minimum,
		int maximum)
	{
		int? parsed = OptionalInt(settings, key);

		if (parsed is null)
		{
			return defaultValue;
		}

		if (parsed < minimum || parsed > maximum)
		{
			throw new ArgumentException(
				$"'{key}' value '{parsed}' is out of range. Must be an integer from {minimum} to {maximum}.");
		}

		return parsed.Value;
	}

	/// <summary>Reads a comma-separated list that must contain at least one integer.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed integers, in the order given.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or holds a non-integer entry.</exception>
	public static IReadOnlyList<int> RequiredIntList(IReadOnlyDictionary<string, string> settings, string key)
	{
		string value = RequireNonEmpty(settings, key);

		string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (parts.Length == 0)
		{
			throw new ArgumentException($"'{key}' must contain at least one comma-separated integer value.");
		}

		int[] result = new int[parts.Length];

		for (int i = 0; i < parts.Length; i++)
		{
			if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]))
			{
				throw new ArgumentException($"'{key}' entry '{parts[i]}' is not a valid integer.");
			}
		}

		return result;
	}

	/// <summary>Reads a comma-separated list of strings.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The trimmed entries, or an empty list when absent or blank.</returns>
	public static IReadOnlyList<string> StringList(IReadOnlyDictionary<string, string> settings, string key)
	{
		string? value = OptionalString(settings, key);

		return value is null
			? Array.Empty<string>()
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	/// <summary>
	/// Matches a user-supplied CRC style value against the valid list case-insensitively and
	/// returns it in its canonical spelling.
	/// </summary>
	/// <param name="raw">The user's value, or <see langword="null"/>.</param>
	/// <param name="validValues">The styles CRC accepts for this feature kind.</param>
	/// <returns>
	/// The canonical spelling when recognized; the value unchanged when not, so the CRC
	/// property validator reports it with the full list of valid options.
	/// </returns>
	public static string? NormalizeStyle(string? raw, IReadOnlyList<string> validValues)
	{
		if (raw is null)
		{
			return null;
		}

		foreach (string candidate in validValues)
		{
			if (string.Equals(candidate, raw, StringComparison.OrdinalIgnoreCase))
			{
				return candidate;
			}
		}

		return raw;
	}

	private static bool YesNoValue(string key, string value)
	{
		value = value.Trim();

		if (value.Equals("Y", StringComparison.OrdinalIgnoreCase))
			return true;

		if (value.Equals("N", StringComparison.OrdinalIgnoreCase))
			return false;

		throw new ArgumentException($"'{key}' must be either \"Y\" or \"N\", but was '{value}'.");
	}
}
