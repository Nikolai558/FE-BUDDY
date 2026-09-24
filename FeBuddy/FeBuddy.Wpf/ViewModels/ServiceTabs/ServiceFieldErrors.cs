using System.ComponentModel;
using System.Windows.Data;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// An indexable, bindable view over a tab's per-field validation messages.
/// </summary>
/// <remarks>
/// A plain dictionary cannot be bound to by key and re-read when it changes; this raises the
/// <c>Item[]</c> change so every <c>{Binding FieldErrors[Whatever]}</c> refreshes at once.
/// </remarks>
public sealed class ServiceFieldErrors : INotifyPropertyChanged
{
	private IReadOnlyDictionary<string, string> _map =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Whether any field currently has a validation message.</summary>
	public bool Any => _map.Count > 0;

	/// <summary>The message for a field key, or <see langword="null"/> when that field is fine.</summary>
	/// <param name="fieldKey">The field key.</param>
	public string? this[string fieldKey] =>
		fieldKey is not null && _map.TryGetValue(fieldKey, out string? message) ? message : null;

	/// <summary>Replaces the whole set and notifies every key binding.</summary>
	/// <param name="map">The new field-to-message map.</param>
	internal void Replace(IReadOnlyDictionary<string, string> map)
	{
		_map = map;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Any)));
	}
}
