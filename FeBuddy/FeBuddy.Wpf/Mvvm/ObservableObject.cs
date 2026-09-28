using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FeBuddy.Wpf.Mvvm;

/// <summary>
/// Minimal <see cref="INotifyPropertyChanged"/> base for view-models. Hand-rolled so the
/// project needs no MVVM package; its API is a subset of CommunityToolkit.Mvvm's
/// <c>ObservableObject</c>, so switching to that later changes only the base class.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Raises <see cref="PropertyChanged"/>.</summary>
	/// <param name="propertyName">The property that changed; defaults to the calling member.</param>
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	/// <summary>
	/// Assigns <paramref name="value"/> to <paramref name="field"/> and raises a change
	/// notification, but only if the value actually changed.
	/// </summary>
	/// <typeparam name="T">The property type.</typeparam>
	/// <param name="field">The backing field.</param>
	/// <param name="value">The new value.</param>
	/// <param name="propertyName">The property that changed; defaults to the calling member.</param>
	/// <returns><see langword="true"/> when the value changed.</returns>
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
