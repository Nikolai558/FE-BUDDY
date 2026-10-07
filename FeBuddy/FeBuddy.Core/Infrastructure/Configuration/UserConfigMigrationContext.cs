namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The settings a <see cref="UserConfigMigration"/> changes, by dotted path, and the edits a
/// layout change is made of: move, change a value, set, remove.
/// </summary>
/// <remarks>
/// Every edit can run twice without harm, so a step that stopped part-way, or a file that already
/// has some of the new layout, comes out right: moving or changing a setting that isn't there does
/// nothing, and a move never overwrites a setting already in the new place.
/// </remarks>
public sealed class UserConfigMigrationContext
{
	private readonly Dictionary<string, string> _values;

	/// <summary>Wraps the settings being brought forward. <see cref="UserConfigMigrations"/> builds it.</summary>
	/// <param name="values">The settings, edited in place.</param>
	internal UserConfigMigrationContext(Dictionary<string, string> values)
	{
		_values = values;
	}

	/// <summary>Every setting, by dotted path.</summary>
	public IReadOnlyDictionary<string, string> Values => _values;

	/// <summary>A setting's value.</summary>
	/// <param name="key">The dotted path, e.g. <c>General.UpdateChannel</c>.</param>
	/// <returns>The value, or <see langword="null"/> when it isn't set.</returns>
	public string? Get(string key) => _values.GetValueOrDefault(key);

	/// <summary>Sets a setting, replacing any value it has.</summary>
	/// <param name="key">The dotted path.</param>
	/// <param name="value">The value.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> cannot name a setting.</exception>
	public void Set(string key, string value)
	{
		ThrowIfInvalid(key);
		ArgumentNullException.ThrowIfNull(value);

		_values[key] = value;
	}

	/// <summary>
	/// Moves a setting, and every setting below it, to a new path: a rename
	/// (<c>…Airways.FixBufferNm</c> to <c>…Airways.Buffer.FixNm</c>) or a whole section
	/// (<c>Services.AiracService.VnasAlias</c> to <c>Services.AiracService.ConcatenateAliases</c>).
	/// </summary>
	/// <remarks>
	/// A setting already at its new path keeps its value, and the old one is dropped. Nothing happens
	/// when there is nothing at <paramref name="from"/>.
	/// </remarks>
	/// <param name="from">The old dotted path.</param>
	/// <param name="to">The new dotted path.</param>
	/// <exception cref="ArgumentException">Either path cannot name a setting, or <paramref name="to"/> is below <paramref name="from"/>.</exception>
	public void Move(string from, string to)
	{
		ThrowIfInvalid(from);
		ThrowIfInvalid(to);

		if (IsAtOrBelow(to, from))
		{
			throw new ArgumentException($"'{to}' is inside '{from}', so it can't be moved there.", nameof(to));
		}

		foreach (string key in KeysAtOrBelow(from))
		{
			string moved = to + key[from.Length..];

			_values.TryAdd(moved, _values[key]);
			_values.Remove(key);
		}
	}

	/// <summary>Rewrites a setting's value, e.g. <c>true</c> / <c>false</c> to <c>Y</c> / <c>N</c>. Nothing happens when it isn't set.</summary>
	/// <param name="key">The dotted path.</param>
	/// <param name="change">Gives the new value from the old.</param>
	public void ChangeValue(string key, Func<string, string> change)
	{
		ArgumentNullException.ThrowIfNull(change);

		if (_values.TryGetValue(key, out string? value))
		{
			_values[key] = change(value) ?? throw new InvalidOperationException($"The new value for '{key}' is null.");
		}
	}

	/// <summary>Drops a setting and every setting below it.</summary>
	/// <param name="key">The dotted path.</param>
	public void Remove(string key)
	{
		foreach (string found in KeysAtOrBelow(key))
		{
			_values.Remove(found);
		}
	}

	/// <summary>The settings at <paramref name="key"/> or below it, in no particular order.</summary>
	/// <param name="key">The dotted path.</param>
	/// <returns>Their dotted paths.</returns>
	public IReadOnlyList<string> KeysAtOrBelow(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		return [.. _values.Keys.Where(found => IsAtOrBelow(found, key))];
	}

	private static bool IsAtOrBelow(string key, string node) =>
		key == node || key.StartsWith(node + ".", StringComparison.Ordinal);

	private static void ThrowIfInvalid(string key)
	{
		if (!UserConfigFile.IsValidKey(key))
		{
			throw new ArgumentException($"'{key}' cannot name a setting.", nameof(key));
		}
	}
}
