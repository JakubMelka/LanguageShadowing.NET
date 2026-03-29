using System.Windows.Input;

namespace LanguageShadowing.Application.Common;

/// <summary>
/// Minimal synchronous <see cref="ICommand"/> implementation.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Raised when the command availability changes.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Returns <see langword="true"/> when the optional predicate allows execution.
    /// </summary>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// Executes the synchronous delegate.
    /// </summary>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Raises <see cref="CanExecuteChanged"/> on the captured synchronization context when possible.
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        if (_synchronizationContext is null)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _synchronizationContext.Post(_ => CanExecuteChanged?.Invoke(this, EventArgs.Empty), null);
    }
}
