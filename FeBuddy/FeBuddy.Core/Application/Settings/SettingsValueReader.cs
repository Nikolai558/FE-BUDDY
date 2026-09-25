using System.Globalization;

namespace FeBuddy.Core.Application.Settings;

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
	public static string RequiredString(IReadOnlyDictionary<string, string> settings, string key)
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

	/// <summary>Reads an integer that must be present.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed integer.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or not an integer.</exception>
	public static int RequiredInt(IReadOnlyDictionary<string, string> settings, string key) =>
		OptionalInt(settings, key)
			?? throw new ArgumentException($"Settings must contain a non-empty '{key}' value.");

	/// <summary>Reads an optional decimal number that must be greater than zero.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <param name="maximum">Highest accepted value, inclusive.</param>
	/// <returns>The parsed number, or <see langword="null"/> when absent or blank.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not a number greater than zero and at most <paramref name="maximum"/>.</exception>
	public static double? OptionalPositiveDecimal(IReadOnlyDictionary<string, string> settings, string key, double maximum)
	{
		if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (!double.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsed)
			|| parsed <= 0
			|| parsed > maximum)
		{
			throw new ArgumentException(
				$"'{key}' value '{value}' is not valid. Must be a number greater than 0 and at most {maximum.ToString(CultureInfo.InvariantCulture)}.");
		}

		return parsed;
	}

	/// <summary>Reads a <c>Y</c>/<c>N</c> flag that must be present.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed flag.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or neither <c>Y</c> nor <c>N</c>.</exception>
	public static bool RequiredYesNo(IReadOnlyDictionary<string, string> settings, string key) =>
		OptionalYesNo(settings, key)
			?? throw new ArgumentException($"Settings must contain a non-empty '{key}' value (Y or N).");

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

		return RequireInRange(key, parsed.Value, minimum, maximum);
	}

	/// <summary>Reads an integer constrained to a range that must be present.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <param name="minimum">Lowest accepted value, inclusive.</param>
	/// <param name="maximum">Highest accepted value, inclusive.</param>
	/// <returns>The parsed integer.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or not an integer in range.</exception>
	public static int RequiredIntInRange(
		IReadOnlyDictionary<string, string> settings,
		string key,
		int minimum,
		int maximum) =>
		RequireInRange(key, RequiredInt(settings, key), minimum, maximum);

	/// <summary>Reads a comma-separated list that must contain at least one integer.</summary>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed integers, in the order given.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or holds a non-integer entry.</exception>
	public static IReadOnlyList<int> RequiredIntList(IReadOnlyDictionary<string, string> settings, string key)
	{
		string value = RequiredString(settings, key);

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
			? []
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	/// <summary>Reads an optional enum value, matched by name ignoring case.</summary>
	/// <typeparam name="TEnum">The enum.</typeparam>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <param name="defaultValue">The value to use when the key is absent or blank.</param>
	/// <param name="hint">What to tell the user when the value is not valid; defaults to listing every name.</param>
	/// <returns>The parsed value.</returns>
	/// <exception cref="ArgumentException">Thrown when the value is not one of the enum's names.</exception>
	public static TEnum OptionalEnum<TEnum>(
		IReadOnlyDictionary<string, string> settings,
		string key,
		TEnum defaultValue,
		string? hint = null)
		where TEnum : struct, Enum
	{
		string? value = OptionalString(settings, key);
		return value is null ? defaultValue : EnumValue<TEnum>(key, value, hint);
	}

	/// <summary>Reads an enum value that must be present, matched by name ignoring case.</summary>
	/// <typeparam name="TEnum">The enum.</typeparam>
	/// <param name="settings">The raw settings block.</param>
	/// <param name="key">The key to read.</param>
	/// <returns>The parsed value.</returns>
	/// <exception cref="ArgumentException">Thrown when the key is missing, blank, or not one of the enum's names.</exception>
	public static TEnum RequiredEnum<TEnum>(IReadOnlyDictionary<string, string> settings, string key)
		where TEnum : struct, Enum =>
		EnumValue<TEnum>(key, RequiredString(settings, key), hint: null);

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

	private static int RequireInRange(string key, int value, int minimum, int maximum)
	{
		if (value < minimum || value > maximum)
		{
			throw new ArgumentException(
				$"'{key}' value '{value}' is out of range. Must be an integer from {minimum} to {maximum}.");
		}

		return value;
	}

	// Names only: Enum.TryParse would also accept "1" or "A,B".
	private static TEnum EnumValue<TEnum>(string key, string value, string? hint) where TEnum : struct, Enum
	{
		foreach (TEnum candidate in Enum.GetValues<TEnum>())
		{
			if (candidate.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				return candidate;
			}
		}

		throw new ArgumentException(
			$"'{key}' value '{value}' is not valid. " +
			(hint ?? $"Use one of: {string.Join(", ", Enum.GetNames<TEnum>())}."));
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
