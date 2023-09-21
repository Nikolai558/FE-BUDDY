using System;
using System.Windows.Input;

namespace WPF.ViewModel;

/// <summary>
/// Public class meant to encapsulate a command that can be bound to UI elements in WPF.
/// The class allows you to pass an action to execute and an optional condition (predicate) to determine if the action can be executed
/// </summary>
public class ViewModelCommand : ICommand
{
    // Private fields to store the action to be executed and the condition to check if the action can be executed.
    private readonly Action<object> _execute;
    private readonly Predicate<object>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelCommand"/> class.
    /// </summary>
    /// <param name="execute">The action to execute when the command is invoked.</param>
    /// <param name="canExecute">A predicate that determines if the command can be executed.</param>
    public ViewModelCommand(Action<object> execute, Predicate<object> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelCommand"/> class. 
    /// This constructor that takes only an execute action (command) that can always be executed.
    /// </summary>
    /// <param name="execute">The action to execute when the command is invoked.</param>
    public ViewModelCommand(Action<object> execute)
    {
        _execute = execute;
        _canExecute = null;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command can execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; } // Subscribe to the CommandManager's RequerySuggested event.
        remove { CommandManager.RequerySuggested -= value; } // Unsubscribe from the CommandManager's RequerySuggested event.
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter)
    {
        // If _canExecute is null, the command can always be executed.
        // Otherwise, the result of the _canExecute predicate is returned.
        return _canExecute == null ? true : _canExecute(parameter);
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public void Execute(object? parameter)
    {
        // Call the _execute action with the provided parameter.
        _execute(parameter);
    }
}
