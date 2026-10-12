using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>The setup window's view-model base: <see cref="INotifyPropertyChanged"/> with a setter helper.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>Sets <paramref name="field"/> and raises <see cref="PropertyChanged"/> when it changed.</summary>
	/// <typeparam name="T">The property's type.</typeparam>
	/// <param name="field">The backing field.</param>
	/// <param name="value">The new value.</param>
	/// <param name="name">The property's name (filled in by the compiler).</param>
	/// <returns><see langword="true"/> when the value changed.</returns>
	protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		OnPropertyChanged(name);
		return true;
	}

	/// <summary>Raises <see cref="PropertyChanged"/>.</summary>
	/// <param name="name">The property's name, or <see langword="null"/> for all of them.</param>
	protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
