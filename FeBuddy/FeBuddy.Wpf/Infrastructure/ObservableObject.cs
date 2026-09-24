using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base for view-models.
/// <para>
/// Hand-rolled on purpose so this project pulls in no MVVM package. If the real
/// app later adds CommunityToolkit.Mvvm, delete this file and change the base
/// class to <c>ObservableObject</c> from that package - the API here is a strict
/// subset, so nothing else needs to change.
/// </para>
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Raises <see cref="PropertyChanged"/> for the calling property.</summary>
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	/// <summary>
	/// Assigns <paramref name="value"/> to <paramref name="field"/> and raises a
	/// change notification, but only if the value actually changed. Returns
	/// <see langword="true"/> when a change was made.
	/// </summary>
	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
