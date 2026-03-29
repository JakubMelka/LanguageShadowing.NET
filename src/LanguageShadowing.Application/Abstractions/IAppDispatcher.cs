namespace LanguageShadowing.Application.Abstractions;

/// <summary>
/// Abstracts marshalling work onto the UI thread.
/// </summary>
public interface IAppDispatcher
{
    /// <summary>
    /// Schedules <paramref name="action"/> on the UI thread and completes when the action has finished executing.
    /// </summary>
    Task RunAsync(Action action);
}
