using System.Collections.ObjectModel;
using System.Windows.Threading;

using FeBuddy.Wpf.Shell.Models;

namespace FeBuddy.Wpf.Shell;

/// <summary>
/// App-wide transient notifications. No DI - a static store the shell's toast
/// host binds to (<c>{x:Static app:Toast.Items}</c>). Newest is inserted first
/// and auto-dismisses after a few seconds; the stack is capped so a burst can't
/// fill the screen.
/// </summary>
public static class Toast
{
	private const int MaxVisible = 4;
	private static readonly TimeSpan Linger = TimeSpan.FromSeconds(4.5);

	/// <summary>The toasts on screen, newest first.</summary>
	public static ObservableCollection<ToastItem> Items { get; } = [];

	/// <summary>Shows an <see cref="ToastKind.Info"/> toast.</summary>
	/// <param name="title">The bold first line.</param>
	/// <param name="message">Optional detail.</param>
	public static void Info(string title, string? message = null) => Show(title, message, ToastKind.Info);

	/// <summary>Shows a <see cref="ToastKind.Success"/> toast.</summary>
	/// <param name="title">The bold first line.</param>
	/// <param name="message">Optional detail.</param>
	public static void Success(string title, string? message = null) => Show(title, message, ToastKind.Success);

	/// <summary>Shows a <see cref="ToastKind.Warn"/> toast.</summary>
	/// <param name="title">The bold first line.</param>
	/// <param name="message">Optional detail.</param>
	public static void Warn(string title, string? message = null) => Show(title, message, ToastKind.Warn);

	/// <summary>Shows an <see cref="ToastKind.Error"/> toast.</summary>
	/// <param name="title">The bold first line.</param>
	/// <param name="message">Optional detail.</param>
	public static void Error(string title, string? message = null) => Show(title, message, ToastKind.Error);

	/// <summary>Shows a toast, dropping the oldest when more than a few are on screen.</summary>
	/// <param name="title">The bold first line.</param>
	/// <param name="message">Optional detail.</param>
	/// <param name="kind">The severity.</param>
	public static void Show(string title, string? message, ToastKind kind)
	{
		var item = new ToastItem(title, message, kind);
		Items.Insert(0, item);

		while (Items.Count > MaxVisible)
		{
			Items.RemoveAt(Items.Count - 1);
		}

		var timer = new DispatcherTimer { Interval = Linger };
		timer.Tick += (_, _) =>
		{
			timer.Stop();
			Dismiss(item);
		};
		timer.Start();
	}

	/// <summary>Removes a toast now.</summary>
	/// <param name="item">The toast to remove.</param>
	public static void Dismiss(ToastItem item) => Items.Remove(item);
}
