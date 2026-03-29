using System.Windows.Input;

namespace LanguageShadowing.Application.Common;

/// <summary>
/// Small asynchronous <see cref="ICommand"/> implementation for view-model actions.
/// </summary>
/// <remarks>
/// <para>
/// MAUI commands are invoked by the UI synchronously, but many real actions in the application are asynchronous:
/// loading voices, preparing speech, starting recognition, opening settings, and so on. This type adapts a
/// <see cref="Func{Task}"/> delegate to the <see cref="ICommand"/> shape expected by XAML bindings.
/// </para>
/// <para>
/// Two design constraints matter here:
/// </para>
/// <list type="bullet">
/// <item><description>the same button must not execute the same asynchronous operation twice concurrently, and</description></item>
/// <item><description><see cref="CanExecuteChanged"/> must be raised on the UI thread even when the awaited task resumes elsewhere.</description></item>
/// </list>
/// <para>
/// The private <c>_isRunning</c> flag solves the first requirement, and the captured <see cref="SynchronizationContext"/>
/// solves the second one.
/// </para>
/// </remarks>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly SynchronizationContext? _synchronizationContext;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The asynchronous operation to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate used by the UI to determine whether the command is currently available.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Raised when the result of <see cref="CanExecute"/> may have changed.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Returns <see langword="true"/> only when the command is not already running and the optional predicate also approves execution.
    /// </summary>
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <summary>
    /// Executes the asynchronous delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method must be <see langword="void"/> because <see cref="ICommand.Execute(object?)"/> is defined that way.
    /// That means the UI framework cannot await it directly. Because of that limitation, this method is responsible for
    /// maintaining its own command state and for guaranteeing that cleanup always runs in <c>finally</c>.
    /// </para>
    /// <para>
    /// <c>ConfigureAwait(false)</c> is used intentionally. The command does not need to resume on the UI thread after the
    /// awaited delegate finishes, because any UI-facing notifications are explicitly marshalled by <see cref="NotifyCanExecuteChanged"/>.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Raises <see cref="CanExecuteChanged"/> on the captured synchronization context when one is available.
    /// </summary>
    /// <remarks>
    /// If the command was created on the UI thread, the captured context keeps command notifications safe for MAUI
    /// controls even when the triggering workflow finished on a worker thread.
    /// </remarks>
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
