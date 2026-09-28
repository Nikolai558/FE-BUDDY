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

	/// <summary>Records a tab-level failure with no single input to blame.</summary>
	/// <param name="message">The message shown above the tab's content.</param>
	public void Add(string message) => _messages.Add(message);

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
