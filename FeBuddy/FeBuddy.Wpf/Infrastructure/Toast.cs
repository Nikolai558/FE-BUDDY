using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace FeBuddy.Wpf.Infrastructure;

public enum ToastKind
{
	Info,
	Success,
	Warn,
	Error,
}

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

	public string Title { get; }

	public string? Message { get; }

	public ToastKind Kind { get; }

	public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

	public ICommand DismissCommand { get; }
}

/// <summary>
/// App-wide transient notifications. No DI - a static store the shell's toast
/// host binds to (<c>{x:Static infra:Toast.Items}</c>). Newest is inserted first
/// and auto-dismisses after a few seconds; the stack is capped so a burst can't
/// fill the screen.
/// </summary>
public static class Toast
{
	private const int MaxVisible = 4;
	private static readonly TimeSpan Linger = TimeSpan.FromSeconds(4.5);

	public static ObservableCollection<ToastItem> Items { get; } = [];

	public static void Info(string title, string? message = null) => Show(title, message, ToastKind.Info);

	public static void Success(string title, string? message = null) => Show(title, message, ToastKind.Success);

	public static void Warn(string title, string? message = null) => Show(title, message, ToastKind.Warn);

	public static void Error(string title, string? message = null) => Show(title, message, ToastKind.Error);

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

	public static void Dismiss(ToastItem item) => Items.Remove(item);
}
