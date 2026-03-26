using System.Windows.Input;

namespace LanguageShadowing.Application.Common;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _synchronizationContext;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _synchronizationContext = SynchronizationContext.Current;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

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
