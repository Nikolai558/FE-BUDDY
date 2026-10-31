namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The settings a <see cref="UserConfigMigration"/> changes, by dotted path, and the edits a
/// layout change is made of: move, change a value, set, remove - and, between a profile and
/// <c>Shared.json</c>, move to it or copy from it.
/// </summary>
/// <remarks>
/// Every edit can run twice without harm, so a step that stopped part-way, or a file that already
/// has some of the new layout, comes out right: moving or changing a setting that isn't there does
/// nothing, and a move never overwrites a setting already in the new place.
/// </remarks>
public sealed class UserConfigMigrationContext
{
	private readonly Dictionary<string, string> _values;
	private readonly Dictionary<string, string>? _shared;

	/// <summary>Wraps the settings being brought forward. <see cref="UserConfigMigrations"/> builds it.</summary>
	/// <param name="values">The settings, edited in place.</param>
	/// <param name="shared">
	/// <c>Shared.json</c>'s settings, edited in place, while a profile is brought forward with it; otherwise
	/// <see langword="null"/>: an import, or <c>Shared.json</c> itself.
	/// </param>
	internal UserConfigMigrationContext(Dictionary<string, string> values, Dictionary<string, string>? shared = null)
	{
		_values = values;
		_shared = shared;
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

	/// <summary>
	/// Moves a setting, and every setting below it, out of this profile into <c>Shared.json</c>, where every
	/// profile shares it: a setting that becomes this PC's rather than a profile's.
	/// </summary>
	/// <remarks>
	/// <c>Shared.json</c> keeps a value it already has. The active profile is brought forward first, so
	/// its value is the one kept; every other profile's copy just goes. With no <c>Shared.json</c> beside
	/// the file - an import, or <c>Shared.json</c> itself - the setting is dropped: an import never carries
	/// <c>Shared.json</c>'s settings.
	/// </remarks>
	/// <param name="from">The dotted path in the profile.</param>
	/// <param name="to">The dotted path in <c>Shared.json</c>.</param>
	/// <exception cref="ArgumentException">Either path cannot name a setting.</exception>
	public void MoveToShared(string from, string to)
	{
		ThrowIfInvalid(from);
		ThrowIfInvalid(to);

		foreach (string key in KeysAtOrBelow(from))
		{
			_shared?.TryAdd(to + key[from.Length..], _values[key]);
			_values.Remove(key);
		}
	}

	/// <summary>
	/// Copies a setting, and every setting below it, from <c>Shared.json</c> into this profile: a setting
	/// that becomes each profile's own. A value the profile already has at <paramref name="to"/> is kept.
	/// </summary>
	/// <remarks>
	/// <c>Shared.json</c> keeps its copy, so every profile gets one; the step's
	/// <see cref="UserConfigMigration.ThenShared"/> takes it out once they all have. Nothing happens with no
	/// <c>Shared.json</c> beside the file.
	/// </remarks>
	/// <param name="from">The dotted path in <c>Shared.json</c>.</param>
	/// <param name="to">The dotted path in the profile.</param>
	/// <exception cref="ArgumentException">Either path cannot name a setting.</exception>
	public void CopyFromShared(string from, string to)
	{
		ThrowIfInvalid(from);
		ThrowIfInvalid(to);

		if (_shared is null)
		{
			return;
		}

		foreach (KeyValuePair<string, string> entry in _shared.Where(entry => IsAtOrBelow(entry.Key, from)).ToList())
		{
			_values.TryAdd(to + entry.Key[from.Length..], entry.Value);
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
