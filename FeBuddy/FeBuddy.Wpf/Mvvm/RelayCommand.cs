using System.Windows.Input;

namespace FeBuddy.Wpf.Mvvm;

/// <summary>
/// A basic <see cref="ICommand"/> backed by delegates, for a command with no parameter.
/// <para>
/// <see cref="CanExecuteChanged"/> is <see cref="CommandManager.RequerySuggested"/>, so WPF
/// re-queries <see cref="CanExecute"/> on focus and input changes without being told.
/// </para>
/// </summary>
/// <param name="execute">What the command does.</param>
/// <param name="canExecute">Whether it can run now; <see langword="null"/> means always.</param>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
	private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

	/// <summary>Raised whenever WPF re-queries commands (<see cref="CommandManager.RequerySuggested"/>).</summary>
	public event EventHandler? CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}

	/// <inheritdoc />
	public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

	/// <inheritdoc />
	public void Execute(object? parameter) => _execute();
}

/// <summary>Delegate <see cref="ICommand"/> that receives a typed parameter; see <see cref="RelayCommand"/>.</summary>
/// <typeparam name="T">The command parameter's type. A parameter of any other type arrives as <see langword="default"/>.</typeparam>
/// <param name="execute">What the command does with the parameter.</param>
/// <param name="canExecute">Whether it can run for the parameter; <see langword="null"/> means always.</param>
public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
	private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

	/// <summary>Raised whenever WPF re-queries commands (<see cref="CommandManager.RequerySuggested"/>).</summary>
	public event EventHandler? CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}

	/// <inheritdoc />
	public bool CanExecute(object? parameter) => canExecute?.Invoke(Cast(parameter)) ?? true;

	/// <inheritdoc />
	public void Execute(object? parameter) => _execute(Cast(parameter));

	private static T? Cast(object? parameter) => parameter is T t ? t : default;
}
