using System;
using System.Windows.Input;

namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>An <see cref="ICommand"/> that runs a delegate.</summary>
/// <param name="execute">What the command does.</param>
/// <param name="canExecute">Whether it can run now; always, when omitted.</param>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
	/// <inheritdoc />
	public event EventHandler? CanExecuteChanged
	{
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}

	/// <inheritdoc />
	public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

	/// <inheritdoc />
	public void Execute(object? parameter) => execute();
}
