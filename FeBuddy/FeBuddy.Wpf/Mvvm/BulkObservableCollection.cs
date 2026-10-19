using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FeBuddy.Wpf.Mvvm;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> whose items can be replaced all at once, with one
/// <see cref="NotifyCollectionChangedAction.Reset"/> rather than a change per item: a list that a burst of
/// items reaches (the Dashboard's activity log) would otherwise have its view handle each one in turn.
/// </summary>
/// <typeparam name="T">The items.</typeparam>
internal sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
	/// <summary>Replaces every item with <paramref name="items"/>, in their order, as one change.</summary>
	/// <param name="items">The new items.</param>
	internal void ReplaceAll(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		CheckReentrancy();

		List<T> replacement = [.. items];
		Items.Clear();

		foreach (T item in replacement)
		{
			Items.Add(item);
		}

		OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
		OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}
}
