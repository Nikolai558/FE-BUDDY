namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// A frozen copy of a screen's saved settings, so the screen can tell whether its current
/// values differ from what was last saved - rather than latching "unsaved changes" on the
/// first edit and keeping it after the user puts every value back.
/// </summary>
/// <remarks>
/// <para>
/// This is the one rule every settings screen in the app follows: a screen is dirty exactly
/// when its current values are not the values it last saved (or loaded). A screen takes a
/// snapshot when it loads and after every successful save, and from each setter asks
/// <see cref="Matches"/> instead of setting a dirty flag.
/// </para>
/// <para>
/// Values are compared as the strings the screen would persist, so "the same" means "would
/// save the same thing" - which is exactly what the warning is about.
/// </para>
/// </remarks>
public sealed class SavedStateSnapshot
{
    private readonly IReadOnlyDictionary<string, string> _values;

    private SavedStateSnapshot(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>Freezes a copy of <paramref name="values"/>.</summary>
    /// <param name="values">Every value the screen persists, by key.</param>
    /// <returns>The snapshot.</returns>
    public static SavedStateSnapshot Of(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new SavedStateSnapshot(new Dictionary<string, string>(values, StringComparer.Ordinal));
    }

    /// <summary>Whether <paramref name="current"/> holds exactly the snapshot's keys and values.</summary>
    /// <param name="current">The screen's values right now, keyed as when the snapshot was taken.</param>
    /// <returns><see langword="true"/> when nothing differs.</returns>
    public bool Matches(IReadOnlyDictionary<string, string> current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (current.Count != _values.Count)
        {
            return false;
        }

        foreach ((string key, string value) in current)
        {
            if (!_values.TryGetValue(key, out string? saved) || !string.Equals(saved, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
