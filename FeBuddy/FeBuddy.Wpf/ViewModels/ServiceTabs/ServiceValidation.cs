namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// Collects a tab's validation failures during <see cref="ServiceTabViewModel.Revalidate"/>.
/// </summary>
public sealed class ServiceValidation
{
	private readonly List<string> _messages = [];
	private readonly Dictionary<string, string> _fieldErrors = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Every failure message, in the order they were added.</summary>
	public IReadOnlyList<string> Messages => _messages;

	/// <summary>Failures that belong to a specific input, keyed by field key.</summary>
	public IReadOnlyDictionary<string, string> FieldErrors => _fieldErrors;

	/// <summary>Whether anything failed.</summary>
	public bool HasErrors => _messages.Count > 0;

	/// <summary>
	/// Records a failure that belongs to a part of the tab rather than one input, such as no
	/// GeoJSON files ticked. The view outlines that part by binding its card's
	/// <c>FieldState.Error</c> to <c>FieldErrors[areaKey]</c>. Unlike <see cref="AddField"/>, every
	/// message is kept, since one area can have several things wrong.
	/// </summary>
	/// <param name="areaKey">The key the area's card binds to, e.g. <c>GeojsonFiles</c>.</param>
	/// <param name="message">The message shown at the top of the tab.</param>
	public void AddArea(string areaKey, string message)
	{
		_fieldErrors.TryAdd(areaKey, message);
		_messages.Add(message);
	}

	/// <summary>
	/// Records a failure against one input, so that box highlights and carries the message as its
	/// tool-tip. The first failure recorded for a key wins.
	/// </summary>
	/// <param name="fieldKey">The key the view passes to <c>FieldState.Error</c>, e.g. <c>SwLat</c>.</param>
	/// <param name="message">The message for that input.</param>
	public void AddField(string fieldKey, string message)
	{
		if (_fieldErrors.TryAdd(fieldKey, message))
		{
			_messages.Add(message);
		}
	}

	/// <summary>
	/// Records a failure against one input when <paramref name="value"/> is blank - the
	/// "required field with nothing in it" case.
	/// </summary>
	/// <param name="fieldKey">The field key.</param>
	/// <param name="value">The current value.</param>
	/// <param name="message">The message for that input.</param>
	/// <returns><see langword="true"/> when the value was present.</returns>
	public bool RequireValue(string fieldKey, string? value, string message)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		AddField(fieldKey, message);
		return false;
	}
}
