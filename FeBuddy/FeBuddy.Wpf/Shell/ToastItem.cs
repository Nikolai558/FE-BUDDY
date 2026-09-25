using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell.Models;

namespace FeBuddy.Wpf.Shell;

/// <summary>One transient notification. Created via <see cref="Toast"/>.</summary>
public sealed class ToastItem : ObservableObject
{
	internal ToastItem(string title, string? message, ToastKind kind)
	{
		Title = title;
		Message = message;
		Kind = kind;
		DismissCommand = new RelayCommand(() => Toast.Dismiss(this));
	}

	/// <summary>The bold first line.</summary>
	public string Title { get; }

	/// <summary>Optional detail under the title.</summary>
	public string? Message { get; }

	/// <summary>The severity.</summary>
	public ToastKind Kind { get; }

	/// <summary>Whether there is a <see cref="Message"/> to show.</summary>
	public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

	/// <summary>Closes the toast before it times out.</summary>
	public ICommand DismissCommand { get; }
}
