using System.Windows.Input;

namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// A basic <see cref="ICommand"/> backed by delegates. Parameterless variant.
/// <para>
/// <see cref="CommandManager.RequerySuggested"/> is used for
/// <see cref="CanExecuteChanged"/>, so WPF re-queries <see cref="CanExecute"/>
/// automatically on focus / input changes - good enough for a UI shell.
/// </para>
/// </summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

/// <summary>Delegate <see cref="ICommand"/> that receives a typed parameter.</summary>
public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(Cast(parameter)) ?? true;

    public void Execute(object? parameter) => _execute(Cast(parameter));

    private static T? Cast(object? parameter) => parameter is T t ? t : default;
}
