using System.Windows.Input;

namespace LanguageShadowing.Application.Common;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _synchronizationContext;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _synchronizationContext = SynchronizationContext.Current;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        NotifyCanExecuteChanged();

        try
        {
            await _execute().ConfigureAwait(false);
        }
        finally
        {
            _isRunning = false;
            NotifyCanExecuteChanged();
        }
    }

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
